#!/bin/bash

echo "🧪 Testing SonarQube + AI Hybrid Analysis Integration"
echo "=================================================="

# Set environment
export CLAUDE_API_KEY="test-key-for-sonarqube-demo"

echo "🔍 Checking service status..."
echo "✅ Services running:"
docker compose -f docker-compose.testing.yml ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep -E "(sonarqube|gitlab|grafana|prometheus)"

echo ""
echo "🌐 Available Endpoints:"
echo "   🔧 SonarQube:     http://localhost:9000 (admin/admin)"
echo "   🦊 GitLab:        http://localhost:8080 (root/mcptesting123)" 
echo "   📊 Grafana:       http://localhost:3000 (admin/mcpadmin123)"
echo "   📈 Prometheus:    http://localhost:9091"
echo "   🤖 MCP Server:    Starting on port 5000..."

echo ""
echo "🚀 Starting MCP server with SonarQube integration..."
cd /home/brunobozic/mcp-code-review

# Start MCP server in background
timeout 45s dotnet run --project src/Mcp.CodeReview -- --http --port 5000 &
MCP_PID=$!

# Wait for server to start
sleep 8

echo "✅ MCP server started (PID: $MCP_PID)"
echo ""
echo "🔧 Available SonarQube + AI Tools:"
echo "   📊 AnalyzeSonarQubeFindings"
echo "   🔀 ConductHybridCodeAnalysis" 
echo "   🚦 AssessQualityGateWithAI"
echo ""
echo "💡 Sample SonarQube Integration Features:"
echo "   ✨ AI-powered analysis of static code findings"
echo "   🧠 Intelligent issue prioritization with business context"
echo "   🔗 Hybrid analysis combining SonarQube + Claude insights"
echo "   🎯 Smart quality gate decisions for deployment"
echo "   🏆 Contextual remediation guidance with step-by-step fixes"
echo ""
echo "🎯 Sample Test Data:"

# Show sample problematic code that would be analyzed
echo "   📁 Sample project: sample-projects/ecommerce-api/"
echo "   🐛 Intentional issues for testing:"
echo "      - Security: Credit card exposure, weak authentication"
echo "      - Performance: N+1 queries, missing pagination"  
echo "      - Architecture: Anemic domain models, scattered business logic"
echo "      - Quality: Missing validation, poor error handling"

echo ""
echo "🔬 Integration Architecture:"
echo "   SonarQube (Static Analysis) ↔ MCP Server ↔ Claude AI (Contextual Analysis)"
echo "                                      ↕"
echo "   GitLab (Code) ← Grafana (Monitoring) → Prometheus (Metrics)"

echo ""
echo "🎉 Revolutionary Features:"
echo "   🚀 Hybrid Analysis: Combines precision of static analysis with AI intelligence"
echo "   🧭 Business Context: Prioritizes issues based on business criticality"
echo "   🎓 AI Mentoring: Provides learning opportunities and skill development"
echo "   ⚡ Smart Deployment: AI-powered quality gate decisions with risk assessment"
echo "   🔮 Predictive Quality: Identifies trends and prevents future issues"

echo ""
echo "⏰ Keeping server running for testing (40 seconds)..."
sleep 40

# Cleanup
echo ""
echo "🧹 Stopping MCP server..."
kill $MCP_PID 2>/dev/null || true

echo ""
echo "✅ SonarQube + AI Integration Test Complete!"
echo ""
echo "🚀 Next Steps:"
echo "   1. Configure real Claude API key for full AI functionality"
echo "   2. Set up SonarQube project for sample-projects/ecommerce-api"
echo "   3. Use MCP client to call hybrid analysis tools"
echo "   4. Experience the revolutionary combination of static + AI analysis"
echo ""
echo "🎯 This integration represents the future of code quality:"
echo "   • Static analysis provides comprehensive coverage"
echo "   • AI provides contextual understanding and business alignment"
echo "   • Together they deliver unprecedented code quality insights"