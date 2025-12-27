#!/bin/bash

# Setup GitLab testing environment
set -e

GITLAB_URL=${GITLAB_URL:-"http://gitlab.local:8080"}
GITLAB_TOKEN=${GITLAB_TOKEN:-"glpat-testing123"}
MCP_URL=${MCP_URL:-"http://mcp-code-review:5000"}

echo "🚀 Setting up GitLab testing environment..."

# Wait for GitLab to be ready
echo "⏳ Waiting for GitLab to be ready..."
while ! curl -f -s "${GITLAB_URL}/api/v4/projects" -H "Authorization: Bearer ${GITLAB_TOKEN}" > /dev/null; do
    echo "Waiting for GitLab..."
    sleep 10
done

echo "✅ GitLab is ready!"

# Create test project
echo "📁 Creating test project..."
PROJECT_RESPONSE=$(curl -s -X POST "${GITLAB_URL}/api/v4/projects" \
    -H "Authorization: Bearer ${GITLAB_TOKEN}" \
    -H "Content-Type: application/json" \
    -d '{
        "name": "ecommerce-api-test",
        "description": "Test e-commerce API for MCP code review testing",
        "visibility": "private",
        "initialize_with_readme": true
    }')

PROJECT_ID=$(echo $PROJECT_RESPONSE | jq -r '.id')
PROJECT_HTTP_URL=$(echo $PROJECT_RESPONSE | jq -r '.http_url_to_repo')

echo "📋 Project created with ID: $PROJECT_ID"
echo "🔗 Repository URL: $PROJECT_HTTP_URL"

# Clone and push sample project
echo "📤 Pushing sample code to GitLab..."
cd /tmp
git clone $PROJECT_HTTP_URL ecommerce-test
cd ecommerce-test

# Copy sample project files
cp -r /app/sample-projects/ecommerce-api/* .

# Configure git
git config user.email "test@example.com"
git config user.name "MCP Test User"

# Add and commit files
git add .
git commit -m "Initial commit: E-commerce API with intentional issues for MCP testing

This commit includes:
- Basic e-commerce API structure
- Intentional security vulnerabilities
- Domain modeling issues  
- Performance problems
- Code quality issues

This will test our MCP AI agents:
- SecurityExpert should find auth issues, data exposure
- ArchitecturalReviewer should identify DDD violations
- PerformanceSpecialist should catch N+1 queries
- DeveloperMentor should suggest improvements
- FeatureSlicingAdvocate should recommend better domain design"

git push origin main

echo "✅ Sample code pushed to GitLab!"

# Create a webhook for MCP integration
echo "🔗 Setting up MCP webhook..."
curl -s -X POST "${GITLAB_URL}/api/v4/projects/${PROJECT_ID}/hooks" \
    -H "Authorization: Bearer ${GITLAB_TOKEN}" \
    -H "Content-Type: application/json" \
    -d "{
        \"url\": \"${MCP_URL}/webhooks/gitlab\",
        \"push_events\": true,
        \"merge_requests_events\": true,
        \"wiki_page_events\": false,
        \"deployment_events\": false,
        \"issues_events\": false,
        \"pipeline_events\": false,
        \"job_events\": false,
        \"enable_ssl_verification\": false
    }"

echo "✅ Webhook configured!"

# Save project info for tests
cat > /app/testing/project-info.json << EOF
{
    "project_id": $PROJECT_ID,
    "project_url": "$PROJECT_HTTP_URL",
    "gitlab_url": "$GITLAB_URL",
    "mcp_url": "$MCP_URL"
}
EOF

echo "📋 Project information saved to project-info.json"
echo "🎉 GitLab test environment setup complete!"
echo ""
echo "Next steps:"
echo "1. Run './create-test-pr.sh' to create a test pull request"
echo "2. Run './test-mcp-tools.sh' to test MCP tools directly"
echo "3. Check GitLab at: $GITLAB_URL"
echo "4. Check MCP metrics at: ${MCP_URL}/metrics"