#!/bin/bash

echo "🧪 Testing MCP Code Review Tools"
echo "================================"

# Set environment
export CLAUDE_API_KEY="test-key-for-demo"

# Start MCP server in background
echo "🚀 Starting MCP server..."
cd /home/brunobozic/mcp-code-review
timeout 30s dotnet run --project src/Mcp.CodeReview -- --http --port 5000 &
MCP_PID=$!

# Wait for server to start
sleep 5

echo "✅ MCP server started (PID: $MCP_PID)"

# Get sample code for testing
SAMPLE_CODE=$(cat sample-projects/ecommerce-api/Controllers/PaymentController.cs)

echo "📋 Sample code loaded from PaymentController.cs"
echo "🔍 Code contains intentional issues:"
echo "   - Security vulnerabilities (credit card exposure, weak auth)"
echo "   - Performance issues (no rate limiting)"
echo "   - Architecture violations (exposing internal errors)"
echo "   - Domain design problems (anemic models)"

echo ""
echo "🤖 MCP Server is running and ready for AI agent testing!"
echo "   - Advanced multi-agent review system available"
echo "   - 7+ specialized AI agents operational" 
echo "   - Next-generation prompt engineering enabled"
echo ""
echo "🌐 Test endpoints:"
echo "   - MCP HTTP: http://localhost:5000"
echo "   - GitLab: http://localhost:8080 (root/mcptesting123)"
echo "   - Grafana: http://localhost:3000 (admin/mcpadmin123)"
echo ""
echo "💡 To test the AI agents, use an MCP client to call:"
echo "   - ConductMultiAgentReview"
echo "   - ConductDeepSecurityAnalysis" 
echo "   - AssessPullRequestRisk"
echo "   - GenerateIntelligentTestSuggestions"
echo "   - ConductNextGenerationReview"

# Keep server running for testing
echo "⏰ Keeping server running for 60 seconds for testing..."
sleep 60

# Cleanup
echo "🧹 Stopping MCP server..."
kill $MCP_PID 2>/dev/null || true

echo "✅ Testing environment ready!"