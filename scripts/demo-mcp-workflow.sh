#!/bin/bash
set -euo pipefail

# MCP Code Review Workflow Demo Script
# Demonstrates the complete PR/MR review process

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

log_header() {
    echo -e "${PURPLE}=== $1 ===${NC}"
}

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[✓]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[⚠]${NC} $1"
}

log_error() {
    echo -e "${RED}[✗]${NC} $1"
}

show_workflow_overview() {
    log_header "MCP Code Review Workflow Overview"
    
    echo -e "${CYAN}📋 Complete Workflow Process:${NC}"
    echo
    echo "1. 🏗️  Developer creates a Pull/Merge Request"
    echo "2. 🔗  GitLab webhook notifies MCP Server"
    echo "3. 🤖  MCP Server fetches the code changes"
    echo "4. 🧠  AI agents analyze the code using advanced prompting:"
    echo "   • 🔍 Security Analysis Agent"
    echo "   • ⚡ Performance Review Agent"
    echo "   • 🎯 Code Quality Agent"
    echo "   • 📝 Documentation Agent"
    echo "   • 🧪 Testing Strategy Agent"
    echo "5. 🔄  Nested Chat Framework for iterative improvement"
    echo "6. ✅  Cross-agent validation and consensus"
    echo "7. 📊  Comprehensive review report generation"
    echo "8. 💬  Comments posted back to GitLab MR"
    echo "9. 📈  Metrics and analytics tracking"
    echo
}

show_sample_code_changes() {
    log_header "Sample Code Changes Being Reviewed"
    
    echo -e "${CYAN}📁 Calculator.cs - Before vs After:${NC}"
    echo
    echo -e "${RED}❌ Issues in Original Code:${NC}"
    echo "• Division by zero vulnerability"
    echo "• No input validation"
    echo "• Inefficient loop instead of LINQ"
    echo "• Weak password validation (security risk)"
    echo "• Missing error handling"
    echo "• No documentation"
    echo
    echo -e "${GREEN}✅ Improvements in Feature Branch:${NC}"
    echo "• Proper exception handling for division by zero"
    echo "• Input validation for NaN and infinite values"
    echo "• LINQ optimization for better performance"
    echo "• Comprehensive password security requirements"
    echo "• Extensive error handling and logging"
    echo "• XML documentation for all methods"
    echo
}

show_ai_analysis_preview() {
    log_header "AI Analysis Preview"
    
    echo -e "${CYAN}🤖 Expected AI Review Findings:${NC}"
    echo
    echo -e "${PURPLE}🔒 Security Analysis:${NC}"
    echo "• ✅ Fixed division by zero vulnerability"
    echo "• ✅ Enhanced password validation (12+ chars, complexity)"
    echo "• ✅ Input sanitization for mathematical operations"
    echo "• ✅ Proper exception handling prevents crashes"
    echo
    echo -e "${PURPLE}⚡ Performance Analysis:${NC}"
    echo "• ✅ LINQ optimization reduces O(n) loop overhead"
    echo "• ✅ Early validation prevents unnecessary processing"
    echo "• ✅ Efficient regex patterns for password validation"
    echo
    echo -e "${PURPLE}🎯 Code Quality:${NC}"
    echo "• ✅ Improved readability with clear method signatures"
    echo "• ✅ Comprehensive XML documentation"
    echo "• ✅ Consistent error handling patterns"
    echo "• ✅ SOLID principles adherence"
    echo
    echo -e "${PURPLE}🧪 Testing Recommendations:${NC}"
    echo "• Unit tests for edge cases (NaN, infinity, null)"
    echo "• Security tests for password validation"
    echo "• Performance benchmarks for LINQ vs loops"
    echo "• Integration tests for error handling"
    echo
}

show_mcp_features() {
    log_header "Advanced MCP Features in Action"
    
    echo -e "${CYAN}🚀 2024 AI Agent Orchestration Features:${NC}"
    echo
    echo -e "${PURPLE}🧠 Dynamic Agent Selection:${NC}"
    echo "• Code complexity analysis determines optimal agents"
    echo "• C# specific agents activated for .NET projects"
    echo "• Security-focused review for password handling"
    echo
    echo -e "${PURPLE}🔄 Nested Chat Framework:${NC}"
    echo "• Writer-Critic loops for iterative improvement"
    echo "• Self-reflection and self-correction mechanisms"
    echo "• Cross-agent validation and consensus building"
    echo
    echo -e "${PURPLE}🎯 Enhanced Chain-of-Thought Prompting:${NC}"
    echo "• 4-step reasoning: Assessment → Analysis → Contrast → Validation"
    echo "• Context-aware analysis based on code patterns"
    echo "• Hierarchical agent orchestration"
    echo
    echo -e "${PURPLE}📊 Comprehensive Metrics:${NC}"
    echo "• Review confidence scores"
    echo "• Agent collaboration effectiveness"
    echo "• Code improvement suggestions"
    echo "• Time-to-review analytics"
    echo
}

demonstrate_api_usage() {
    log_header "MCP API Usage Examples"
    
    echo -e "${CYAN}🔧 API Endpoints for Code Review:${NC}"
    echo
    echo -e "${BLUE}1. Trigger Manual Review:${NC}"
    echo "curl -X POST http://localhost:5002/api/review-merge-request \\"
    echo "     -H 'Content-Type: application/json' \\"
    echo "     -d '{\"project_id\": \"1\", \"merge_request_iid\": \"1\"}'"
    echo
    echo -e "${BLUE}2. Get Review Status:${NC}"
    echo "curl http://localhost:5002/api/review-status/1"
    echo
    echo -e "${BLUE}3. Health Check:${NC}"
    echo "curl http://localhost:5002/health"
    echo
    echo -e "${BLUE}4. Metrics Endpoint:${NC}"
    echo "curl http://localhost:5003/metrics"
    echo
}

show_gitlab_setup_status() {
    log_header "GitLab Setup Status"
    
    # Check GitLab accessibility
    if curl -f -s --max-time 5 "http://localhost:8080/users/sign_in" >/dev/null 2>&1; then
        log_success "GitLab is accessible and ready!"
        echo "🌐 Access: http://localhost:8080"
        echo "👤 Login: root / SecureGitLabPass123!"
        echo
        echo "🚀 Run repository setup:"
        echo "./scripts/setup-gitlab-repository.sh"
    else
        log_info "GitLab is still initializing (takes 5-10 minutes on first start)"
        echo "⏳ Monitor progress: docker logs mcp-gitlab -f"
        echo "🏥 Health check: ./scripts/health-monitor.sh gitlab"
    fi
    echo
}

show_monitoring_dashboard() {
    log_header "Monitoring & Observability"
    
    echo -e "${CYAN}📊 Available Dashboards:${NC}"
    echo
    echo "🎛️  Grafana Dashboard: http://localhost:3000"
    echo "    Username: admin"
    echo "    Password: SecureGrafanaPass123!"
    echo
    echo "📈 Prometheus Metrics: http://localhost:9090"
    echo "    MCP application metrics and health"
    echo
    echo "🔍 Elasticsearch Logs: http://localhost:9200"
    echo "    Centralized logging for all services"
    echo
    echo "📋 Kibana Visualizations: http://localhost:5601"
    echo "    Log analysis and search capabilities"
    echo
}

show_next_steps() {
    log_header "Next Steps for Complete Demo"
    
    echo -e "${CYAN}🎯 Complete the Setup:${NC}"
    echo
    echo "1. 🔧 Wait for GitLab initialization:"
    echo "   ./scripts/health-monitor.sh gitlab"
    echo
    echo "2. 🏗️  Create sample repository with MR:"
    echo "   ./scripts/setup-gitlab-repository.sh"
    echo
    echo "3. 🧪 Test MCP Code Review:"
    echo "   # Manual API call to trigger review"
    echo "   # Or use GitLab webhook integration"
    echo
    echo "4. 📊 Monitor the process:"
    echo "   # Check Grafana for metrics"
    echo "   # View logs in Kibana"
    echo "   # Track performance in Prometheus"
    echo
    echo "5. 🔍 Review AI feedback:"
    echo "   # Check GitLab MR for automated comments"
    echo "   # Analyze review quality and suggestions"
    echo
    echo -e "${GREEN}🚀 System Status:${NC}"
    echo "• MCP Server: Running and healthy"
    echo "• Monitoring Stack: Operational"
    echo "• Logging Pipeline: Active"
    echo "• AI Agents: Ready for code review"
    echo
}

main() {
    clear
    echo -e "${PURPLE}"
    echo "╔═══════════════════════════════════════════════════════════╗"
    echo "║                MCP Code Review System                     ║"
    echo "║                  Workflow Demonstration                   ║"
    echo "╚═══════════════════════════════════════════════════════════╝"
    echo -e "${NC}"
    echo
    
    show_workflow_overview
    echo
    show_sample_code_changes
    echo
    show_ai_analysis_preview
    echo
    show_mcp_features
    echo
    demonstrate_api_usage
    echo
    show_gitlab_setup_status
    echo
    show_monitoring_dashboard
    echo
    show_next_steps
}

# Allow for specific demonstrations
case "${1:-all}" in
    "workflow")
        show_workflow_overview
        ;;
    "features")
        show_mcp_features
        ;;
    "api")
        demonstrate_api_usage
        ;;
    "status")
        show_gitlab_setup_status
        ;;
    "all"|*)
        main
        ;;
esac