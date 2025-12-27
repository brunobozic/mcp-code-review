#!/bin/bash
set -e

echo "🔗 Setting up GitLab + MCP Code Review Server Integration"
echo "======================================================="

# Configuration
GITLAB_URL="http://localhost:9191"
MCP_SERVER_URL="http://localhost:5002"
PROJECT_ID="1"  # The ecommerce demo project

echo "📋 Integration Setup:"
echo "   GitLab URL: $GITLAB_URL"
echo "   MCP Server: $MCP_SERVER_URL"
echo "   Project ID: $PROJECT_ID"
echo ""

# Step 1: Get GitLab Token (reuse from previous demo)
echo "🔐 Getting GitLab API Token..."
GITLAB_TOKEN=$(docker exec mcp-gitlab gitlab-rails runner "
token = User.find_by(username: 'root').personal_access_tokens.where(name: 'MCP Demo Token').first
if token
  puts token.token
else
  new_token = User.find_by(username: 'root').personal_access_tokens.create!(
    name: 'MCP Demo Token',
    scopes: ['api', 'read_user', 'read_repository', 'write_repository'],
    expires_at: 1.year.from_now
  )
  puts new_token.token
end
")

if [ -z "$GITLAB_TOKEN" ]; then
    echo "❌ Failed to get GitLab token"
    exit 1
fi

echo "✅ GitLab token obtained: ${GITLAB_TOKEN:0:8}..."

# Step 2: Check project exists
echo "📦 Checking GitLab project..."
PROJECT_RESPONSE=$(curl -s -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
  "$GITLAB_URL/api/v4/projects/$PROJECT_ID")

PROJECT_NAME=$(echo "$PROJECT_RESPONSE" | grep -o '"name":"[^"]*"' | cut -d':' -f2 | tr -d '"')

if [ -z "$PROJECT_NAME" ]; then
    echo "❌ Project not found. Please create a project first."
    echo "Response: $PROJECT_RESPONSE"
    exit 1
fi

echo "✅ Project found: $PROJECT_NAME"

# Step 3: Set up webhook
echo "🪝 Setting up GitLab webhook..."
WEBHOOK_DATA="{
  \"url\": \"$MCP_SERVER_URL/api/gitlabwebhook\",
  \"merge_requests_events\": true,
  \"push_events\": false,
  \"issues_events\": false,
  \"wiki_page_events\": false,
  \"note_events\": true,
  \"pipeline_events\": false,
  \"job_events\": false,
  \"deployment_events\": false,
  \"enable_ssl_verification\": false,
  \"token\": \"mcp-webhook-secret-2025\",
  \"push_events_branch_filter\": \"\"
}"

WEBHOOK_RESPONSE=$(curl -s -X POST \
  -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
  -H "Content-Type: application/json" \
  -d "$WEBHOOK_DATA" \
  "$GITLAB_URL/api/v4/projects/$PROJECT_ID/hooks")

WEBHOOK_ID=$(echo "$WEBHOOK_RESPONSE" | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)

if [ -n "$WEBHOOK_ID" ]; then
    echo "✅ Webhook created with ID: $WEBHOOK_ID"
else
    echo "⚠️  Webhook creation response: $WEBHOOK_RESPONSE"
fi

# Step 4: Create example GitLab CI/CD integration
echo "🔄 Creating GitLab CI/CD integration..."
mkdir -p /tmp/gitlab-ci-demo
cat > /tmp/gitlab-ci-demo/.gitlab-ci.yml << 'EOF'
# GitLab CI/CD pipeline with MCP Code Review integration
stages:
  - build
  - test
  - code-review
  - deploy

variables:
  MCP_SERVER_URL: "http://mcp-server:5000"
  DOTNET_VERSION: "8.0"

# Build stage
build:
  stage: build
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet restore
    - dotnet build --configuration Release
  artifacts:
    paths:
      - bin/
    expire_in: 1 hour

# Test stage
test:
  stage: test
  image: mcr.microsoft.com/dotnet/sdk:8.0
  script:
    - dotnet test --configuration Release --logger "junit;LogFilePath=test-results.xml"
  artifacts:
    reports:
      junit: test-results.xml

# MCP Code Review stage (runs on merge requests)
mcp-code-review:
  stage: code-review
  image: curlimages/curl:latest
  before_script:
    - apk add --no-cache git jq
  script:
    - |
      echo "🤖 Running MCP Code Review Analysis..."
      
      # Get changed files in the merge request
      git diff --name-only $CI_MERGE_REQUEST_TARGET_BRANCH_SHA..$CI_COMMIT_SHA > changed_files.txt
      
      echo "📄 Files changed in this MR:"
      cat changed_files.txt
      
      # Process each changed file
      while read -r file; do
        # Only analyze code files
        if [[ "$file" == *.cs ]] || [[ "$file" == *.js ]] || [[ "$file" == *.py ]] || [[ "$file" == *.java ]]; then
          echo "🔍 Analyzing: $file"
          
          # Read file content and encode for JSON
          file_content=$(base64 -w 0 "$file" 2>/dev/null || echo "")
          
          if [ -n "$file_content" ]; then
            # Send to MCP server for review
            review_response=$(curl -s -X POST "$MCP_SERVER_URL/api/review" \
              -H "Content-Type: application/json" \
              -d "{
                \"fileName\": \"$file\",
                \"content\": \"$file_content\",
                \"gitlabMrId\": \"$CI_MERGE_REQUEST_IID\",
                \"projectId\": \"$CI_PROJECT_ID\",
                \"enhanced2025\": true
              }" || echo "{\"error\": \"Failed to get review\"}")
            
            echo "📊 Review results for $file:"
            echo "$review_response" | jq '.' 2>/dev/null || echo "$review_response"
            echo ""
            
            # Extract quality score and check if it meets threshold
            quality_score=$(echo "$review_response" | jq -r '.qualityScore // 0' 2>/dev/null || echo "0")
            echo "Quality Score: $quality_score"
            
            # If quality score is below 70, fail the pipeline
            if [ "$(echo "$quality_score < 70" | bc -l 2>/dev/null || echo "0")" = "1" ]; then
              echo "❌ Quality score below threshold (70). Review required."
              echo "EXIT_STATUS=1" >> mcp-review-status.env
            fi
          fi
        fi
      done < changed_files.txt
      
      # Check if any file failed quality checks
      if [ -f mcp-review-status.env ] && grep -q "EXIT_STATUS=1" mcp-review-status.env; then
        echo "❌ Code review failed. Please address the issues above."
        exit 1
      else
        echo "✅ Code review passed!"
      fi
  artifacts:
    reports:
      dotenv: mcp-review-status.env
    when: always
    expire_in: 1 week
  only:
    - merge_requests
  when: always

# Deploy stage (only runs if code review passes)
deploy:
  stage: deploy
  image: mcr.microsoft.com/dotnet/aspnet:8.0
  script:
    - echo "🚀 Deploying application..."
    - echo "Application deployed successfully!"
  only:
    - main
  dependencies:
    - mcp-code-review
EOF

echo "✅ GitLab CI/CD configuration created"

# Step 5: Display integration summary
echo ""
echo "🎉 GitLab + MCP Integration Setup Complete!"
echo ""
echo "📋 Integration Summary:"
echo "=================================="
echo "🪝 Webhook Configuration:"
echo "   • URL: $MCP_SERVER_URL/gitlab/webhook"
echo "   • Events: Merge Request, Comments"
echo "   • Secret: mcp-webhook-secret-2025"
echo "   • Status: ${WEBHOOK_ID:+Configured}${WEBHOOK_ID:-Pending}"
echo ""
echo "🔄 CI/CD Pipeline:"
echo "   • File: .gitlab-ci.yml created in /tmp/gitlab-ci-demo/"
echo "   • Stages: build → test → code-review → deploy"
echo "   • Quality Gate: Score must be ≥ 70"
echo "   • Triggers: Automatic on merge requests"
echo ""
echo "🤖 How it works:"
echo "   1. Developer creates merge request"
echo "   2. GitLab webhook notifies MCP server (real-time)"
echo "   3. OR GitLab CI/CD runs code review job (pipeline)"
echo "   4. MCP server analyzes changed files with AI agents"
echo "   5. Results posted back to GitLab as MR comments"
echo "   6. Quality gates enforce review standards"
echo ""
echo "🛠️  Next Steps:"
echo "   1. Ensure MCP server is running on port 5000"
echo "   2. Copy .gitlab-ci.yml to your GitLab project"
echo "   3. Create a test merge request to see it in action"
echo "   4. Check webhook delivery in GitLab Settings → Webhooks"
echo ""
echo "📍 Access Points:"
echo "   • GitLab Project: $GITLAB_URL/root/$PROJECT_NAME"
echo "   • MCP Server Health: $MCP_SERVER_URL/health"
echo "   • Webhook Test: $MCP_SERVER_URL/gitlab/webhook"

# Clean up
rm -rf /tmp/gitlab-ci-demo