#!/bin/bash

# Test the MCP code review system end-to-end
set -e

source /app/testing/project-info.json 2>/dev/null || {
    echo "❌ Project info not found. Run setup-gitlab-test.sh first."
    exit 1
}

source /app/testing/mr-info.json 2>/dev/null || {
    echo "❌ MR info not found. Run create-test-pr.sh first."
    exit 1
}

MCP_URL=$(jq -r '.mcp_url' /app/testing/project-info.json)
GITLAB_URL=$(jq -r '.gitlab_url' /app/testing/project-info.json)
PROJECT_ID=$(jq -r '.project_id' /app/testing/project-info.json)
MR_ID=$(jq -r '.mr_id' /app/testing/mr-info.json)
BRANCH_NAME=$(jq -r '.branch' /app/testing/mr-info.json)

echo "🧪 Testing MCP Code Review System"
echo "================================="
echo "MCP URL: $MCP_URL"
echo "GitLab URL: $GITLAB_URL"
echo "Project ID: $PROJECT_ID"
echo "MR ID: $MR_ID"
echo ""

# Test 1: Health Check
echo "🔍 Test 1: MCP Health Check"
echo "----------------------------"
HEALTH_RESPONSE=$(curl -s -f "${MCP_URL}/health" || echo "FAILED")
if [[ "$HEALTH_RESPONSE" == "FAILED" ]]; then
    echo "❌ MCP health check failed"
    exit 1
else
    echo "✅ MCP is healthy"
fi
echo ""

# Test 2: Get MR diff from GitLab
echo "🔍 Test 2: Fetch MR Diff from GitLab"
echo "-------------------------------------"
GITLAB_TOKEN=${GITLAB_TOKEN:-"glpat-testing123"}

DIFF_RESPONSE=$(curl -s "${GITLAB_URL}/api/v4/projects/${PROJECT_ID}/merge_requests/${MR_ID}/changes" \
    -H "Authorization: Bearer ${GITLAB_TOKEN}")

if [[ $(echo "$DIFF_RESPONSE" | jq -r '.changes | length') -gt 0 ]]; then
    echo "✅ Successfully fetched MR diff"
    CHANGES_COUNT=$(echo "$DIFF_RESPONSE" | jq -r '.changes | length')
    echo "   📄 Found $CHANGES_COUNT file changes"
else
    echo "❌ Failed to fetch MR diff"
    exit 1
fi

# Extract the diff content
DIFF_CONTENT=$(echo "$DIFF_RESPONSE" | jq -r '.changes[0].diff' | head -50)
echo "   📝 Sample diff content:"
echo "$DIFF_CONTENT" | head -10
echo "   ..."
echo ""

# Test 3: Test Basic MCP Tools via HTTP
echo "🔍 Test 3: Test MCP Tools via HTTP"
echo "-----------------------------------"

# Test the traditional multiAgentReview tool
echo "🤖 Testing multiAgentReview tool..."
REVIEW_PAYLOAD=$(cat << EOF
{
  "tool": "multiAgentReview",
  "arguments": {
    "diff": "$DIFF_CONTENT",
    "language": "csharp",
    "pullRequestTitle": "Add admin features and user reporting",
    "author": "test-developer",
    "filesChanged": ["Controllers/AdminController.cs", "Services/ReportService.cs"]
  }
}
EOF
)

# Note: This would typically be sent via MCP protocol, but for testing we'll use HTTP
REVIEW_RESPONSE=$(curl -s -X POST "${MCP_URL}/mcp" \
    -H "Content-Type: application/json" \
    -d "$REVIEW_PAYLOAD" || echo "FAILED")

if [[ "$REVIEW_RESPONSE" != "FAILED" ]]; then
    echo "✅ multiAgentReview tool responded"
    echo "   📊 Response length: $(echo "$REVIEW_RESPONSE" | wc -c) characters"
else
    echo "❌ multiAgentReview tool failed"
fi
echo ""

# Test 4: Test Next-Generation AI Review
echo "🔍 Test 4: Test Next-Generation AI Review"
echo "-------------------------------------------"

echo "🚀 Testing nextGenAIReview tool..."
NEXTGEN_PAYLOAD=$(cat << EOF
{
  "tool": "nextGenAIReview", 
  "arguments": {
    "diff": "$DIFF_CONTENT",
    "language": "csharp",
    "pullRequestTitle": "Add admin features and user reporting",
    "author": "junior-developer",
    "filesChanged": ["Controllers/AdminController.cs", "Services/ReportService.cs"],
    "businessContext": "E-commerce platform with payment processing and user management",
    "developerExperience": "2 years experience, learning security best practices",
    "includeCollaborativeChallenges": true
  }
}
EOF
)

NEXTGEN_RESPONSE=$(curl -s -X POST "${MCP_URL}/mcp" \
    -H "Content-Type: application/json" \
    -d "$NEXTGEN_PAYLOAD" || echo "FAILED")

if [[ "$NEXTGEN_RESPONSE" != "FAILED" ]]; then
    echo "✅ nextGenAIReview tool responded"
    echo "   📊 Response length: $(echo "$NEXTGEN_RESPONSE" | wc -c) characters"
    
    # Try to extract some insights
    if command -v jq >/dev/null; then
        CONFIDENCE=$(echo "$NEXTGEN_RESPONSE" | jq -r '.enhancedInsights.overallConfidence // "N/A"' 2>/dev/null)
        CONSENSUS=$(echo "$NEXTGEN_RESPONSE" | jq -r '.enhancedInsights.agentConsensusScore // "N/A"' 2>/dev/null)
        AGENTS=$(echo "$NEXTGEN_RESPONSE" | jq -r '.analysisMetadata.agentsUsed // [] | length' 2>/dev/null)
        
        echo "   🎯 Overall Confidence: $CONFIDENCE"
        echo "   🤝 Agent Consensus: $CONSENSUS" 
        echo "   👥 Agents Used: $AGENTS"
    fi
else
    echo "❌ nextGenAIReview tool failed"
fi
echo ""

# Test 5: Test Security Analysis
echo "🔍 Test 5: Test Deep Security Analysis"
echo "---------------------------------------"

echo "🛡️ Testing deepSecurityAnalysis tool..."
SECURITY_PAYLOAD=$(cat << EOF
{
  "tool": "deepSecurityAnalysis",
  "arguments": {
    "code": "$(echo "$DIFF_CONTENT" | head -30 | tr '\n' ' ')",
    "language": "csharp",
    "dependencies": ["Microsoft.AspNetCore", "System.Data.SqlClient"],
    "applicationContext": "E-commerce payment processing API"
  }
}
EOF
)

SECURITY_RESPONSE=$(curl -s -X POST "${MCP_URL}/mcp" \
    -H "Content-Type: application/json" \
    -d "$SECURITY_PAYLOAD" || echo "FAILED")

if [[ "$SECURITY_RESPONSE" != "FAILED" ]]; then
    echo "✅ deepSecurityAnalysis tool responded"
    echo "   📊 Response length: $(echo "$SECURITY_RESPONSE" | wc -c) characters"
    
    if command -v jq >/dev/null; then
        VULN_SCORE=$(echo "$SECURITY_RESPONSE" | jq -r '.vulnerabilityScore // "N/A"' 2>/dev/null)
        RISK_LEVEL=$(echo "$SECURITY_RESPONSE" | jq -r '.riskLevel // "N/A"' 2>/dev/null)
        
        echo "   ⚠️  Vulnerability Score: $VULN_SCORE"
        echo "   🎯 Risk Level: $RISK_LEVEL"
    fi
else
    echo "❌ deepSecurityAnalysis tool failed"
fi
echo ""

# Test 6: Check Metrics
echo "🔍 Test 6: Check MCP Metrics"
echo "-----------------------------"

METRICS_RESPONSE=$(curl -s "${MCP_URL}/metrics" || echo "FAILED")
if [[ "$METRICS_RESPONSE" != "FAILED" ]]; then
    echo "✅ Metrics endpoint accessible"
    
    # Extract some key metrics
    REVIEW_COUNT=$(echo "$METRICS_RESPONSE" | grep "mcp_reviews_total" | tail -1 | awk '{print $2}' || echo "0")
    ERROR_COUNT=$(echo "$METRICS_RESPONSE" | grep "mcp_errors_total" | tail -1 | awk '{print $2}' || echo "0")
    
    echo "   📈 Total Reviews: $REVIEW_COUNT"
    echo "   ❌ Total Errors: $ERROR_COUNT"
else
    echo "❌ Metrics endpoint failed"
fi
echo ""

# Test 7: Integration Test Summary
echo "🔍 Test 7: Integration Test Summary"
echo "======================================"

echo "🎯 Test Results Summary:"
echo "-------------------------"

if [[ "$HEALTH_RESPONSE" != "FAILED" ]]; then
    echo "✅ MCP Server Health: PASS"
else
    echo "❌ MCP Server Health: FAIL"
fi

if [[ $(echo "$DIFF_RESPONSE" | jq -r '.changes | length') -gt 0 ]]; then
    echo "✅ GitLab Integration: PASS"
else
    echo "❌ GitLab Integration: FAIL"
fi

if [[ "$REVIEW_RESPONSE" != "FAILED" ]]; then
    echo "✅ Multi-Agent Review: PASS"
else
    echo "❌ Multi-Agent Review: FAIL"
fi

if [[ "$NEXTGEN_RESPONSE" != "FAILED" ]]; then
    echo "✅ Next-Gen AI Review: PASS"
else
    echo "❌ Next-Gen AI Review: FAIL"
fi

if [[ "$SECURITY_RESPONSE" != "FAILED" ]]; then
    echo "✅ Security Analysis: PASS"
else
    echo "❌ Security Analysis: FAIL"
fi

if [[ "$METRICS_RESPONSE" != "FAILED" ]]; then
    echo "✅ Metrics Collection: PASS"
else
    echo "❌ Metrics Collection: FAIL"
fi

echo ""
echo "🎉 End-to-End Testing Complete!"
echo ""
echo "🔗 Useful Links:"
echo "   📊 GitLab Project: ${GITLAB_URL}/project/${PROJECT_ID}"
echo "   🔀 Merge Request: ${MR_URL}"
echo "   📈 MCP Metrics: ${MCP_URL}/metrics"
echo "   🏥 MCP Health: ${MCP_URL}/health"
echo "   📊 Grafana Dashboard: http://localhost:3000"
echo "   🔍 Kibana Logs: http://localhost:5601"
echo ""
echo "💡 Next Steps:"
echo "   1. Review the AI analysis results above"
echo "   2. Check Grafana for performance metrics"
echo "   3. Examine Kibana for detailed logs"
echo "   4. Create additional test PRs with './create-test-pr.sh'"
echo "   5. Test different types of code issues"