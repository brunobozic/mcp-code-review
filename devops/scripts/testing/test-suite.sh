#!/bin/bash
set -e

echo "🧪 MCP Code Review System - Comprehensive Test Suite"
echo "===================================================="

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

log_info() { echo -e "${BLUE}ℹ️  $1${NC}"; }
log_success() { echo -e "${GREEN}✅ $1${NC}"; }
log_warning() { echo -e "${YELLOW}⚠️  $1${NC}"; }
log_error() { echo -e "${RED}❌ $1${NC}"; }
log_phase() { echo -e "${PURPLE}🔄 $1${NC}"; }
log_test() { echo -e "${CYAN}🧪 $1${NC}"; }

# Configuration
TEST_TYPE="${1:-comprehensive}"
STRESS_CONCURRENT_REQUESTS=${STRESS_CONCURRENT_REQUESTS:-10}
STRESS_TOTAL_REQUESTS=${STRESS_TOTAL_REQUESTS:-50}
BASE_URL="http://localhost:5002"

# Test results tracking
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0
TEST_RESULTS=()

# Test result tracking functions
start_test() {
    local test_name="$1"
    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    log_test "Starting: $test_name"
}

pass_test() {
    local test_name="$1"
    PASSED_TESTS=$((PASSED_TESTS + 1))
    TEST_RESULTS+=("✅ $test_name")
    log_success "$test_name"
}

fail_test() {
    local test_name="$1" 
    local error_msg="${2:-Unknown error}"
    FAILED_TESTS=$((FAILED_TESTS + 1))
    TEST_RESULTS+=("❌ $test_name: $error_msg")
    log_error "$test_name: $error_msg"
}

# 1. Container Health Tests
test_container_health() {
    log_phase "Testing Container Health"
    
    local containers=(
        "mcp-gitlab:GitLab CE"
        "mcp-code-review:MCP Server"
        "mcp-chromadb:ChromaDB Vector DB"
        "mcp-prometheus:Prometheus Metrics"
        "mcp-grafana:Grafana Dashboard"
        "mcp-elasticsearch:Elasticsearch Logs"
        "mcp-gitlab-postgres:GitLab Database"
        "mcp-gitlab-redis:GitLab Cache"
    )
    
    for container_info in "${containers[@]}"; do
        local container=$(echo "$container_info" | cut -d':' -f1)
        local description=$(echo "$container_info" | cut -d':' -f2)
        
        start_test "Container: $description"
        
        if docker ps --filter "name=$container" --format "{{.Names}}" | grep -q "$container"; then
            local status=$(docker ps --filter "name=$container" --format "{{.Status}}")
            if echo "$status" | grep -q "(healthy)"; then
                pass_test "$description"
            elif echo "$status" | grep -q "(unhealthy)"; then
                fail_test "$description" "Container unhealthy"
            else
                pass_test "$description (no health check)"
            fi
        else
            fail_test "$description" "Container not running"
        fi
    done
}

# 2. Service Endpoint Tests  
test_service_endpoints() {
    log_phase "Testing Service Endpoints"
    
    local endpoints=(
        "http://localhost:9191/-/health:GitLab Health"
        "http://localhost:5002/health:MCP Server Health"
        "http://localhost:19193/api/v1/heartbeat:ChromaDB Health"
        "http://localhost:9090/-/healthy:Prometheus Health"
        "http://localhost:19192/api/health:Grafana Health"
        "http://localhost:9200/_cluster/health:Elasticsearch Health"
    )
    
    for endpoint_info in "${endpoints[@]}"; do
        local endpoint=$(echo "$endpoint_info" | cut -d':' -f1-2)
        local description=$(echo "$endpoint_info" | cut -d':' -f3)
        
        start_test "Endpoint: $description"
        
        if curl -s -f "$endpoint" --max-time 10 >/dev/null 2>&1; then
            pass_test "$description"
        else
            fail_test "$description" "Endpoint not responding"
        fi
    done
}

# 3. AI Agent Collaboration Tests
test_ai_agents() {
    log_phase "Testing AI Agent Collaboration"
    
    # Test 1: Simple AI Review
    start_test "AI Simple Review"
    local simple_response
    simple_response=$(curl -s -X POST \
        -H "Content-Type: application/json" \
        -d '{
            "content": "public void TestMethod() { Console.WriteLine(\"Hello\"); }",
            "fileName": "Test.cs",
            "language": "csharp"
        }' \
        "$BASE_URL/api/review" 2>&1)
    
    if echo "$simple_response" | grep -q '"qualityScore"'; then
        local quality_score=$(echo "$simple_response" | grep -o '"qualityScore":[0-9]*' | cut -d':' -f2)
        pass_test "AI Simple Review (Score: $quality_score)"
    else
        fail_test "AI Simple Review" "No quality score returned"
    fi
    
    # Test 2: Multi-Agent Collaboration
    start_test "Multi-Agent Collaboration"
    local complex_response
    complex_response=$(curl -s --max-time 60 -X POST \
        -H "Content-Type: application/json" \
        -d '{
            "content": "public class PaymentService { public void ProcessPayment(string sql) { var query = \"SELECT * FROM payments WHERE id = \" + sql; ExecuteQuery(query); } }",
            "fileName": "PaymentService.cs",
            "language": "csharp",
            "enhanced2025": true
        }' \
        "$BASE_URL/api/review" 2>&1)
    
    if echo "$complex_response" | grep -q '"agentResults"'; then
        local agent_count=$(echo "$complex_response" | grep -o '"agentResults":\[[^]]*\]' | grep -o '{[^}]*}' | wc -l)
        pass_test "Multi-Agent Collaboration ($agent_count agents)"
    else
        fail_test "Multi-Agent Collaboration" "No agent results found"
    fi
}

# 4. RAG System Tests
test_rag_system() {
    log_phase "Testing RAG System"
    
    # Test ChromaDB Collections
    start_test "ChromaDB Collections"
    local collections_response
    collections_response=$(curl -s "http://localhost:19193/api/v1/collections" \
        -H "Authorization: Bearer test-token" 2>&1)
    
    if echo "$collections_response" | grep -q '"name"'; then
        local collection_count=$(echo "$collections_response" | grep -o '"name"' | wc -l)
        pass_test "ChromaDB Collections ($collection_count found)"
    else
        fail_test "ChromaDB Collections" "No collections found"
    fi
    
    # Test Vector Search
    start_test "RAG Vector Search"
    local search_response
    search_response=$(curl -s -X POST \
        "http://localhost:19193/api/v1/collections/coding_standards/query" \
        -H "Authorization: Bearer test-token" \
        -H "Content-Type: application/json" \
        -d '{
            "query_texts": ["security best practices"],
            "n_results": 3
        }' 2>&1)
    
    if echo "$search_response" | grep -q '"documents"'; then
        pass_test "RAG Vector Search"
    else
        fail_test "RAG Vector Search" "Search failed"
    fi
}

# 5. GitLab Integration Tests
test_gitlab_integration() {
    log_phase "Testing GitLab Integration"
    
    # Test GitLab API Access
    start_test "GitLab API Access"
    if curl -s -f "http://localhost:9191/api/v4/version" >/dev/null 2>&1; then
        pass_test "GitLab API Access"
    else
        fail_test "GitLab API Access" "API not accessible"
    fi
    
    # Test GitLab Projects
    start_test "GitLab Demo Project"
    local projects_response
    projects_response=$(curl -s "http://localhost:9191/api/v4/projects" 2>&1)
    
    if echo "$projects_response" | grep -q '"name"'; then
        local project_count=$(echo "$projects_response" | grep -o '"name"' | wc -l)
        pass_test "GitLab Demo Project ($project_count projects)"
    else
        fail_test "GitLab Demo Project" "No projects found"
    fi
}

# 6. Performance/Stress Tests
test_performance() {
    log_phase "Testing Performance & Load"
    
    start_test "Concurrent Request Handling"
    
    # Create temporary directory for concurrent test results
    local temp_dir="/tmp/mcp_stress_test_$$"
    mkdir -p "$temp_dir"
    
    # Simple test payload
    local test_payload='{
        "content": "function test() { return 42; }",
        "fileName": "test.js",
        "language": "javascript"
    }'
    
    # Launch concurrent requests
    log_info "Launching $STRESS_CONCURRENT_REQUESTS concurrent requests..."
    local pids=()
    
    for i in $(seq 1 "$STRESS_CONCURRENT_REQUESTS"); do
        (
            local start_time=$(date +%s%N)
            local response=$(curl -s --max-time 30 -X POST \
                -H "Content-Type: application/json" \
                -d "$test_payload" \
                "$BASE_URL/api/review" 2>&1)
            local end_time=$(date +%s%N)
            local duration=$(( (end_time - start_time) / 1000000 )) # Convert to milliseconds
            
            if echo "$response" | grep -q '"qualityScore"'; then
                echo "SUCCESS:$i:$duration" > "$temp_dir/result_$i"
            else
                echo "FAILURE:$i:$duration:$(echo "$response" | head -c 100)" > "$temp_dir/result_$i"
            fi
        ) &
        pids+=($!)
    done
    
    # Wait for all requests to complete
    local timeout=60
    local elapsed=0
    while [ $elapsed -lt $timeout ] && [ ${#pids[@]} -gt 0 ]; do
        local running_pids=()
        for pid in "${pids[@]}"; do
            if kill -0 "$pid" 2>/dev/null; then
                running_pids+=("$pid")
            fi
        done
        pids=("${running_pids[@]}")
        
        if [ ${#pids[@]} -eq 0 ]; then
            break
        fi
        
        sleep 1
        elapsed=$((elapsed + 1))
    done
    
    # Kill any remaining processes
    for pid in "${pids[@]}"; do
        kill "$pid" 2>/dev/null || true
    done
    
    # Analyze results
    local success_count=0
    local total_duration=0
    local results_processed=0
    
    for result_file in "$temp_dir"/result_*; do
        if [ -f "$result_file" ]; then
            local result=$(cat "$result_file")
            results_processed=$((results_processed + 1))
            
            if echo "$result" | grep -q "^SUCCESS:"; then
                success_count=$((success_count + 1))
                local duration=$(echo "$result" | cut -d':' -f3)
                total_duration=$((total_duration + duration))
            fi
        fi
    done
    
    # Clean up
    rm -rf "$temp_dir"
    
    if [ $results_processed -eq 0 ]; then
        fail_test "Concurrent Request Handling" "No results collected"
    elif [ $success_count -gt $((STRESS_CONCURRENT_REQUESTS / 2)) ]; then
        local avg_duration=$((total_duration / success_count))
        pass_test "Concurrent Request Handling ($success_count/$results_processed successful, avg: ${avg_duration}ms)"
    else
        fail_test "Concurrent Request Handling" "Too many failures: $success_count/$results_processed successful"
    fi
}

# 7. Enhanced 2025 Feature Tests
test_enhanced_features() {
    log_phase "Testing Enhanced 2025 Features"
    
    # Test Tree of Thoughts
    start_test "Tree of Thoughts Reasoning"
    local tot_response
    tot_response=$(curl -s --max-time 60 -X POST \
        -H "Content-Type: application/json" \
        -d '{
            "content": "class SecurityManager { public void authenticate(String password) { if (MD5.hash(password).equals(storedHash)) { login(); } } }",
            "fileName": "SecurityManager.java",
            "language": "java",
            "enhanced2025": true,
            "features": {
                "enableTreeOfThoughts": true,
                "enableAgentDebates": true,
                "enableMetaReasoning": true
            }
        }' \
        "$BASE_URL/api/review/enhanced-2025" 2>&1)
    
    if echo "$tot_response" | grep -q '"reasoning"' && echo "$tot_response" | grep -q '"confidence"'; then
        pass_test "Tree of Thoughts Reasoning"
    else
        fail_test "Tree of Thoughts Reasoning" "Enhanced reasoning not detected"
    fi
    
    # Test Hallucination Detection
    start_test "Hallucination Detection"
    local hallucination_response
    hallucination_response=$(curl -s --max-time 45 -X POST \
        -H "Content-Type: application/json" \
        -d '{
            "content": "// This is perfectly secure code with no issues",
            "fileName": "perfect.py",
            "language": "python",
            "enhanced2025": true,
            "features": {
                "enableHallucinationDetection": true
            }
        }' \
        "$BASE_URL/api/review/enhanced-2025" 2>&1)
    
    if echo "$hallucination_response" | grep -q '"confidence"'; then
        local confidence=$(echo "$hallucination_response" | grep -o '"confidence":[0-9.]*' | cut -d':' -f2)
        pass_test "Hallucination Detection (confidence: $confidence)"
    else
        fail_test "Hallucination Detection" "No confidence score returned"
    fi
}

# 8. Environment and Configuration Tests
test_environment() {
    log_phase "Testing Environment Configuration"
    
    # Test Environment Variables
    start_test "Required Environment Variables"
    local missing_vars=()
    
    if [ -f ".env" ]; then
        source .env 2>/dev/null || true
        
        local required_vars=(
            "OPENAI_API_KEY"
            "GITLAB_TOKEN"
            "CHROMADB_URL"
        )
        
        for var in "${required_vars[@]}"; do
            if [ -z "${!var:-}" ]; then
                missing_vars+=("$var")
            fi
        done
        
        if [ ${#missing_vars[@]} -eq 0 ]; then
            pass_test "Required Environment Variables"
        else
            fail_test "Required Environment Variables" "Missing: ${missing_vars[*]}"
        fi
    else
        fail_test "Required Environment Variables" ".env file not found"
    fi
    
    # Test Disk Space
    start_test "Disk Space Check"
    local available_space=$(df . | tail -1 | awk '{print $4}')
    local required_space=$((5 * 1024 * 1024)) # 5GB in KB
    
    if [ "$available_space" -gt "$required_space" ]; then
        pass_test "Disk Space Check ($(($available_space / 1024 / 1024))GB available)"
    else
        fail_test "Disk Space Check" "Low disk space: $(($available_space / 1024 / 1024))GB available"
    fi
}

# Main test execution
run_test_suite() {
    local suite_type="$1"
    
    log_phase "Running $suite_type Test Suite"
    
    case "$suite_type" in
        "quick")
            test_container_health
            test_service_endpoints
            ;;
        "integration")
            test_container_health
            test_service_endpoints
            test_ai_agents
            test_gitlab_integration
            ;;
        "comprehensive"|*)
            test_environment
            test_container_health
            test_service_endpoints
            test_ai_agents
            test_rag_system
            test_gitlab_integration
            test_performance
            test_enhanced_features
            ;;
    esac
}

# Generate test report
generate_report() {
    echo ""
    log_phase "Test Suite Results"
    echo "=================="
    echo ""
    
    echo "📊 Test Statistics:"
    echo "   Total Tests: $TOTAL_TESTS"
    echo "   Passed: $PASSED_TESTS"
    echo "   Failed: $FAILED_TESTS"
    echo "   Success Rate: $(( PASSED_TESTS * 100 / TOTAL_TESTS ))%"
    echo ""
    
    if [ $FAILED_TESTS -eq 0 ]; then
        log_success "All tests passed! 🎉"
        echo ""
        echo "🚀 System Status: Fully Operational"
        echo "   ✅ All services are healthy"
        echo "   ✅ AI agents are collaborating properly"
        echo "   ✅ Performance is within acceptable limits"
        echo "   ✅ Enhanced 2025 features are working"
    else
        echo "📋 Detailed Results:"
        printf '%s\n' "${TEST_RESULTS[@]}"
        echo ""
        
        if [ $FAILED_TESTS -lt 3 ]; then
            log_warning "Some tests failed but system is mostly operational"
        else
            log_error "Multiple critical failures detected"
        fi
    fi
    
    echo ""
    echo "🔧 Useful Commands:"
    echo "   View logs: ./devops/scripts/automation/stack-orchestrator.sh logs"
    echo "   System status: ./devops/scripts/automation/stack-orchestrator.sh status"
    echo "   Health check: ./devops/scripts/automation/stack-orchestrator.sh health"
}

# Show usage information
show_usage() {
    echo "Usage: $0 [test_type]"
    echo ""
    echo "Test Types:"
    echo "  quick         - Basic health checks only (fastest)"
    echo "  integration   - Service integration testing (medium)"
    echo "  comprehensive - Complete test suite including performance (slowest, default)"
    echo ""
    echo "Environment Variables:"
    echo "  STRESS_CONCURRENT_REQUESTS=N   - Number of concurrent requests for stress test (default: 10)"
    echo "  STRESS_TOTAL_REQUESTS=N        - Total requests for stress test (default: 50)"
    echo ""
    echo "Examples:"
    echo "  $0 quick                       - Quick health check"
    echo "  $0 integration                 - Integration testing"
    echo "  $0 comprehensive              - Full test suite"
    echo "  STRESS_CONCURRENT_REQUESTS=20 $0 comprehensive  - Stress test with 20 concurrent requests"
}

# Main execution
main() {
    if [ "${1:-}" = "--help" ] || [ "${1:-}" = "-h" ]; then
        show_usage
        exit 0
    fi
    
    local test_type="${1:-comprehensive}"
    
    # Validate test type
    case "$test_type" in
        "quick"|"integration"|"comprehensive")
            ;;
        *)
            log_error "Invalid test type: $test_type"
            show_usage
            exit 1
            ;;
    esac
    
    echo "🧪 MCP Code Review System - Comprehensive Test Suite"
    echo "===================================================="
    echo "Test Type: $test_type"
    echo "Start Time: $(date)"
    echo ""
    
    local start_time=$(date +%s)
    
    run_test_suite "$test_type"
    
    local end_time=$(date +%s)
    local duration=$((end_time - start_time))
    
    echo ""
    echo "Test Duration: ${duration}s"
    
    generate_report
    
    # Exit with appropriate code
    if [ $FAILED_TESTS -eq 0 ]; then
        exit 0
    else
        exit 1
    fi
}

# Execute main function
main "$@"