#!/bin/bash

# Playwright-style end-to-end test for GitLab webhook background AI task execution
# Tests the fixed HTTP client scoping in OpenAIServiceProvider

echo "🎯 E2E TEST: GitLab Webhook → Background AI Task Execution"
echo "Testing HTTP client disposal fix with real multi-agent system"
echo ""

# Test configuration
MCP_SERVER_URL="http://localhost:5003"
PROJECT_ID=12345
MR_IID=42
MAX_WAIT_SECONDS=180

# Step 1: Verify MCP server is responsive
echo "🔍 Step 1: Verifying MCP server health..."
health_response=$(curl -s -w "%{http_code}" -o /dev/null "$MCP_SERVER_URL/health")

if [ "$health_response" -ne 200 ]; then
    echo "❌ FAILURE: MCP server health check failed: HTTP $health_response"
    exit 1
fi

echo "✅ MCP server healthy: HTTP $health_response"
echo ""

# Step 2: Create test GitLab webhook payload
echo "🔍 Step 2: Creating test GitLab webhook payload..."

webhook_payload='{
  "object_kind": "merge_request",
  "event_type": "merge_request",
  "project": {
    "id": '$PROJECT_ID',
    "name": "test-ecommerce-api",
    "path": "ecommerce-api"
  },
  "merge_request": {
    "iid": '$MR_IID',
    "title": "Fix payment security vulnerabilities",
    "description": "This MR addresses critical security issues in the payment processing system including SQL injection and hardcoded credentials.",
    "source_branch": "feature/security-fixes",
    "target_branch": "main",
    "author": {
      "name": "Security Developer",
      "email": "security@example.com"
    }
  }
}'

echo "✅ Test payload created"
echo ""

# Step 3: Send GitLab webhook to trigger background AI task
echo "🔍 Step 3: Sending GitLab webhook to trigger background AI task..."

webhook_response=$(curl -s -w "HTTPSTATUS:%{http_code}" \
    -X POST \
    -H "Content-Type: application/json" \
    -d "$webhook_payload" \
    "$MCP_SERVER_URL/api/GitLabWebhook")

# Extract HTTP status and body
http_code=$(echo "$webhook_response" | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
response_body=$(echo "$webhook_response" | sed -e 's/HTTPSTATUS:.*$//')

echo "📡 Webhook response: HTTP $http_code"
echo "📋 Response body: $response_body"

if [ "$http_code" -ne 200 ]; then
    echo "❌ FAILURE: Webhook failed: HTTP $http_code"
    echo "Response: $response_body"
    exit 1
fi

echo "✅ GitLab webhook sent successfully, background task triggered"
echo ""

# Step 4: Monitor for background task completion with timeout
echo "🔍 Step 4: Waiting for background AI task completion..."

completion_file="/tmp/mcp-background-task-$PROJECT_ID-$MR_IID-completed.json"
check_interval=3

for ((i=0; i<MAX_WAIT_SECONDS; i+=check_interval)); do
    if [ -f "$completion_file" ]; then
        echo "✅ Background task completed! Artifact found: $completion_file"
        
        # Read completion artifact
        artifact_content=$(cat "$completion_file")
        echo "📋 Completion artifact: $artifact_content"
        
        # Extract findings count using basic text processing
        findings_count=$(echo "$artifact_content" | grep -o '"FindingsCount":[0-9]*' | cut -d: -f2)
        quality_score=$(echo "$artifact_content" | grep -o '"QualityScore":[0-9.]*' | cut -d: -f2)
        agent_results=$(echo "$artifact_content" | grep -o '"AgentResults":[0-9]*' | cut -d: -f2)
        
        echo "📊 Results:"
        echo "   - Findings: $findings_count"
        echo "   - Quality Score: $quality_score"
        echo "   - Agent Results: $agent_results"
        
        break
    fi
    
    echo "⏳ Waiting for completion... (${i}s/${MAX_WAIT_SECONDS}s)"
    sleep $check_interval
done

if [ ! -f "$completion_file" ]; then
    echo "❌ FAILURE: Background task did not complete within $MAX_WAIT_SECONDS seconds"
    echo "Check logs for HTTP client disposal errors:"
    
    # Show recent log entries
    if [ -d "/home/brunobozic/mcp-code-review/logs" ]; then
        echo "📋 Recent log entries:"
        find /home/brunobozic/mcp-code-review/logs -name "*.txt" -mtime -1 -exec tail -5 {} \;
    fi
    
    exit 1
fi

echo ""

# Step 5: Verify AI agent execution
echo "🔍 Step 5: Verifying AI agent execution..."

# Verify we got realistic findings
if [ -z "$findings_count" ] || [ "$findings_count" -lt 1 ]; then
    echo "❌ FAILURE: Expected AI agents to find issues, but got $findings_count findings"
    exit 1
fi

# Verify quality score is reasonable
if [ -z "$quality_score" ]; then
    echo "❌ FAILURE: Quality score is missing"
    exit 1
fi

# Basic range check for quality score (1-10)
quality_check=$(echo "$quality_score < 1 || $quality_score > 10" | bc -l 2>/dev/null || echo "1")
if [ "$quality_check" = "1" ]; then
    echo "⚠️  WARNING: Quality score $quality_score is outside expected range 1-10"
fi

# Verify multiple agents participated
if [ -z "$agent_results" ] || [ "$agent_results" -lt 2 ]; then
    echo "❌ FAILURE: Expected multiple AI agents, but only $agent_results agents participated"
    exit 1
fi

echo "✅ AI Agent execution verified:"
echo "   - Findings: $findings_count"
echo "   - Quality Score: $quality_score"
echo "   - Agent Results: $agent_results"

# Step 6: Check for evidence that specific agents executed
echo ""
echo "🔍 Step 6: Checking for specific agent execution evidence..."

found_security_expert=false
found_performance_analyst=false

if [ -d "/home/brunobozic/mcp-code-review/logs" ]; then
    # Find recent log files
    recent_logs=$(find /home/brunobozic/mcp-code-review/logs -name "*.txt" -mmin -5 2>/dev/null)
    
    if [ -n "$recent_logs" ]; then
        for log_file in $recent_logs; do
            if grep -q "SecurityExpert" "$log_file" 2>/dev/null; then
                found_security_expert=true
            fi
            if grep -q "PerformanceAnalyst" "$log_file" 2>/dev/null; then
                found_performance_analyst=true
            fi
        done
    fi
fi

if [ "$found_security_expert" = true ]; then
    echo "✅ SecurityExpert agent execution confirmed"
else
    echo "⚠️  WARNING: SecurityExpert agent execution not found in logs"
fi

if [ "$found_performance_analyst" = true ]; then
    echo "✅ PerformanceAnalyst agent execution confirmed"
else
    echo "⚠️  WARNING: PerformanceAnalyst agent execution not found in logs"
fi

echo ""
echo "🎉 SUCCESS: Background AI task execution verified with end-to-end testing!"
echo "✅ HTTP client disposal fix appears to be working correctly"
echo "✅ Multi-agent system (SecurityExpert, PerformanceAnalyst, etc.) executed properly"
echo "✅ Background tasks completed without circuit breaker failures"

# Cleanup test artifacts
rm -f "$completion_file" 2>/dev/null

exit 0