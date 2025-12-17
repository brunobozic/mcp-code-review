#!/bin/bash

echo "🔍 MCP Code Review System - Health Check"
echo "========================================"

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() { echo -e "${BLUE}ℹ️  $1${NC}"; }
log_success() { echo -e "${GREEN}✅ $1${NC}"; }
log_warning() { echo -e "${YELLOW}⚠️  $1${NC}"; }
log_error() { echo -e "${RED}❌ $1${NC}"; }

# Check Docker containers
check_containers() {
    log_info "Checking Docker containers..."
    
    containers=(
        "mcp-gitlab"
        "mcp-code-review"
        "mcp-chromadb"
        "mcp-prometheus"
        "mcp-grafana"
        "mcp-elasticsearch"
        "mcp-gitlab-postgres"
        "mcp-gitlab-redis"
    )
    
    for container in "${containers[@]}"; do
        if docker ps --filter "name=$container" --format "{{.Names}}" | grep -q "$container"; then
            status=$(docker ps --filter "name=$container" --format "{{.Status}}")
            if echo "$status" | grep -q "(healthy)"; then
                log_success "$container: Running (Healthy)"
            elif echo "$status" | grep -q "(unhealthy)"; then
                log_error "$container: Running (Unhealthy)"
            else
                log_warning "$container: Running (No health check)"
            fi
        else
            log_error "$container: Not running"
        fi
    done
}

# Check service endpoints
check_endpoints() {
    log_info "Checking service endpoints..."
    
    endpoints=(
        "GitLab:http://localhost:9191/-/health_check"
        "MCP Server:http://localhost:5002/health"
        "ChromaDB:http://localhost:19193/api/v1/heartbeat"
        "Prometheus:http://localhost:9090/-/healthy"
        "Grafana:http://localhost:19192/api/health"
        "Elasticsearch:http://localhost:9200/_cluster/health"
    )
    
    for endpoint in "${endpoints[@]}"; do
        name=$(echo "$endpoint" | cut -d':' -f1)
        url=$(echo "$endpoint" | cut -d':' -f2-3)
        
        if curl -s -f "$url" >/dev/null 2>&1; then
            log_success "$name endpoint is responding"
        else
            log_error "$name endpoint is not responding ($url)"
        fi
    done
}

# Check GitLab integration
check_gitlab_integration() {
    log_info "Checking GitLab integration..."
    
    # Check if we can access GitLab API
    if curl -s "http://localhost:9191/api/v4/version" >/dev/null 2>&1; then
        log_success "GitLab API is accessible"
        
        # Check for demo project
        if curl -s "http://localhost:9191/root/mcp-demo-project" | grep -q "mcp-demo-project"; then
            log_success "Demo project is accessible"
        else
            log_warning "Demo project may not exist"
        fi
    else
        log_error "GitLab API is not accessible"
    fi
}

# Check MCP server functionality
check_mcp_functionality() {
    log_info "Checking MCP server functionality..."
    
    # Test basic health endpoint
    if curl -s "http://localhost:5002/health" | grep -q "Healthy"; then
        log_success "MCP server is healthy"
        
        # Test info endpoint
        if curl -s "http://localhost:5002/" | grep -q "MCP Code Review System"; then
            log_success "MCP server info endpoint working"
        else
            log_warning "MCP server info endpoint may have issues"
        fi
        
        # Test enhanced 2025 info
        if curl -s "http://localhost:5002/api/review/enhanced-2025/info" | grep -q "Enhanced 2025"; then
            log_success "Enhanced 2025 features available"
        else
            log_warning "Enhanced 2025 features may not be available"
        fi
    else
        log_error "MCP server health check failed"
    fi
}

# Check environment configuration
check_environment() {
    log_info "Checking environment configuration..."
    
    if [ -f ".env" ]; then
        log_success ".env file exists"
        
        # Check for required variables (without exposing values)
        required_vars=(
            "OPENAI_API_KEY"
            "GITLAB_ROOT_PASSWORD"
            "GRAFANA_ADMIN_PASSWORD"
        )
        
        for var in "${required_vars[@]}"; do
            if grep -q "^${var}=" .env && ! grep -q "^${var}=placeholder" .env; then
                log_success "$var is configured"
            else
                log_warning "$var may not be properly configured"
            fi
        done
    else
        log_error ".env file is missing"
    fi
}

# Check disk space
check_disk_space() {
    log_info "Checking disk space..."
    
    # Check Docker disk usage
    docker_usage=$(docker system df --format "table {{.Type}}\t{{.TotalCount}}\t{{.Size}}" | tail -n +2)
    echo "$docker_usage"
    
    # Check available disk space
    available=$(df -h . | tail -1 | awk '{print $4}')
    log_info "Available disk space: $available"
}

# Main health check
main() {
    echo ""
    check_containers
    echo ""
    check_endpoints
    echo ""
    check_gitlab_integration
    echo ""
    check_mcp_functionality
    echo ""
    check_environment
    echo ""
    check_disk_space
    echo ""
    
    log_info "Health check complete!"
    echo ""
    echo "📋 Next Steps:"
    echo "   • Check any failed services above"
    echo "   • View logs: docker compose logs -f [service-name]"
    echo "   • Restart if needed: docker compose restart [service-name]"
    echo "   • Full restart: docker compose down && ./scripts/automation/deploy-full-stack.sh"
}

main "$@"