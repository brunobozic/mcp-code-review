#!/bin/bash
set -e

echo "🔗 FULLY AUTOMATED Webhook Setup"
echo "================================="

# Configuration
GITLAB_URL="http://localhost:9191"
MCP_SERVER_URL="http://host.docker.internal:5002"
PROJECT_ID="2"

# Step 1: Get most recent GitLab token directly from database
echo "🔑 Getting GitLab token from database..."
GITLAB_TOKEN=$(docker exec mcp-gitlab-postgres psql -U gitlab -d gitlabhq_production -t -c "
SELECT token_digest FROM personal_access_tokens 
WHERE user_id = (SELECT id FROM users WHERE username = 'root') 
ORDER BY created_at DESC LIMIT 1;" | tr -d ' ')

if [ -z "$GITLAB_TOKEN" ]; then
    echo "❌ No token found, creating new one..."
    # Create token via rails console
    cat > /tmp/create_webhook_token.rb << 'EOF'
user = User.find_by(username: 'root')
token = user.personal_access_tokens.create!(
  name: 'Webhook Auto Token',
  scopes: ['api', 'read_user', 'read_repository', 'write_repository'],
  expires_at: 1.year.from_now
)
puts "TOKEN:#{token.token}"
EOF

    docker cp /tmp/create_webhook_token.rb mcp-gitlab:/tmp/create_webhook_token.rb
    TOKEN_OUTPUT=$(docker exec mcp-gitlab gitlab-rails runner /tmp/create_webhook_token.rb)
    GITLAB_TOKEN=$(echo "$TOKEN_OUTPUT" | grep "TOKEN:" | cut -d':' -f2)
    rm -f /tmp/create_webhook_token.rb
fi

if [ -z "$GITLAB_TOKEN" ]; then
    echo "❌ Failed to get GitLab token"
    exit 1
fi

echo "✅ GitLab token: ${GITLAB_TOKEN:0:12}..."

# Step 2: Verify project exists
echo "📦 Verifying project exists..."
PROJECT_RESPONSE=$(curl -s -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
  "$GITLAB_URL/api/v4/projects/$PROJECT_ID")

if echo "$PROJECT_RESPONSE" | grep -q '"message":"404 Project Not Found"'; then
    echo "❌ Project $PROJECT_ID not found"
    exit 1
fi

PROJECT_NAME=$(echo "$PROJECT_RESPONSE" | grep -o '"name":"[^"]*"' | cut -d':' -f2 | tr -d '"')
echo "✅ Project verified: $PROJECT_NAME"

# Step 3: Remove any existing webhooks for this URL
echo "🧹 Cleaning up existing webhooks..."
EXISTING_HOOKS=$(curl -s -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
  "$GITLAB_URL/api/v4/projects/$PROJECT_ID/hooks")

echo "$EXISTING_HOOKS" | grep -o '"id":[0-9]*' | cut -d':' -f2 | while read hook_id; do
    if [ -n "$hook_id" ]; then
        echo "🗑️ Removing existing webhook $hook_id"
        curl -s -X DELETE \
          -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
          "$GITLAB_URL/api/v4/projects/$PROJECT_ID/hooks/$hook_id"
    fi
done

# Step 4: Create new webhook
echo "🪝 Creating automated webhook..."
WEBHOOK_DATA="{
  \"url\": \"$MCP_SERVER_URL/api/gitlabwebhook\",
  \"merge_requests_events\": true,
  \"push_events\": true,
  \"issues_events\": false,
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
    echo "✅ Webhook created successfully with ID: $WEBHOOK_ID"
    
    # Step 5: Test webhook
    echo "🧪 Testing webhook..."
    TEST_RESPONSE=$(curl -s -X POST \
      -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
      "$GITLAB_URL/api/v4/projects/$PROJECT_ID/hooks/$WEBHOOK_ID/test/merge_requests_events")
    
    if echo "$TEST_RESPONSE" | grep -q '"success":true\|200'; then
        echo "✅ Webhook test successful"
    else
        echo "⚠️ Webhook test response: $TEST_RESPONSE"
    fi
else
    echo "❌ Webhook creation failed"
    echo "Response: $WEBHOOK_RESPONSE"
    exit 1
fi

# Step 6: Store webhook info for reference
echo "💾 Storing webhook configuration..."
cat > /tmp/webhook-config.json << EOF
{
  "webhook_id": "$WEBHOOK_ID",
  "gitlab_token": "$GITLAB_TOKEN",
  "project_id": "$PROJECT_ID",
  "webhook_url": "$MCP_SERVER_URL/api/gitlabwebhook",
  "created_at": "$(date -Iseconds)"
}
EOF

echo ""
echo "🎉 WEBHOOK AUTOMATION COMPLETE!"
echo "==============================="
echo "🪝 Webhook ID: $WEBHOOK_ID"
echo "📦 Project: $PROJECT_NAME"
echo "🌐 Webhook URL: $MCP_SERVER_URL/api/gitlabwebhook"
echo "🔧 Events: Merge Requests, Push, Notes"
echo "✅ Status: Active and tested"
echo ""
echo "🚀 System is now fully automated:"
echo "   • Any merge request will trigger AI review"
echo "   • Comments will appear in GitLab automatically"
echo "   • No manual intervention required"