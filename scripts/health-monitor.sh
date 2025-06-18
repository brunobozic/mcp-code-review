#!/bin/bash
set -euo pipefail

# Health Monitoring Script for MCP Code Review System
# Monitors all services and provides comprehensive status reporting

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

# Service configurations
declare -A SERVICES=(
    ["mcp-server"]="http://localhost:5002/health"
    ["grafana"]="http://localhost:3000/api/health"
    ["prometheus"]="http://localhost:9090/-/healthy"
    ["elasticsearch"]="http://localhost:9200/_cluster/health"
    ["kibana"]="http://localhost:5601/api/status"
    ["gitlab"]="http://localhost:8080/-/health"
    ["sonarqube"]="http://localhost:9000/api/system/status"
)

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

check_docker_status() {
    log_header "Docker Container Status"
    
    local containers=(
        "mcp-code-review"
        "mcp-gitlab"
        "mcp-grafana"
        "mcp-prometheus" 
        "mcp-elasticsearch"
        "mcp-kibana"
        "mcp-sonarqube"
        "mcp-fluentd"
        "mcp-gitlab-postgres"
        "mcp-gitlab-redis"
        "mcp-sonar-postgres"
    )
    
    for container in "${containers[@]}"; do
        if docker ps --format "table {{.Names}}\t{{.Status}}" | grep -q "$container"; then
            local status=$(docker ps --format "table {{.Names}}\t{{.Status}}" | grep "$container" | awk '{print $2,$3,$4}')
            if [[ "$status" == *"Up"* ]]; then
                log_success "$container: $status"
            else
                log_warning "$container: $status"
            fi
        else
            log_error "$container: Not running"
        fi
    done
}

check_service_health() {
    log_header "Service Health Checks"
    
    for service in "${!SERVICES[@]}"; do
        local url="${SERVICES[$service]}"
        local timeout=10
        
        log_info "Checking $service at $url..."
        
        if curl -f -s --max-time $timeout "$url" > /dev/null 2>&1; then
            log_success "$service: Healthy"
        else
            local http_code=$(curl -s -o /dev/null -w "%{http_code}" --max-time $timeout "$url" 2>/dev/null || echo "000")
            log_error "$service: Unhealthy (HTTP $http_code)"
        fi
    done
}

check_gitlab_specific() {
    log_header "GitLab Specific Checks"
    
    # Check if GitLab is responding
    if curl -f -s --max-time 10 "http://localhost:8080/users/sign_in" > /dev/null 2>&1; then
        log_success "GitLab web interface: Accessible"
        
        # Check if we can reach GitLab API
        if curl -f -s --max-time 10 "http://localhost:8080/api/v4/version" > /dev/null 2>&1; then
            log_success "GitLab API: Accessible"
        else
            log_warning "GitLab API: Not accessible yet (may still be starting)"
        fi
    else
        log_error "GitLab web interface: Not accessible"
        
        # Check GitLab logs for specific errors
        log_info "Checking GitLab logs for errors..."
        if docker logs mcp-gitlab --tail 20 2>/dev/null | grep -i error; then
            log_warning "GitLab errors found in logs (see above)"
        fi
    fi
}

check_logging_pipeline() {
    log_header "Logging Pipeline Status"
    
    # Check Elasticsearch
    if curl -f -s "http://localhost:9200/_cluster/health" > /dev/null 2>&1; then
        local es_health=$(curl -s "http://localhost:9200/_cluster/health" | jq -r '.status' 2>/dev/null || echo "unknown")
        if [ "$es_health" = "green" ] || [ "$es_health" = "yellow" ]; then
            log_success "Elasticsearch: $es_health"
        else
            log_warning "Elasticsearch: $es_health"
        fi
    else
        log_error "Elasticsearch: Not responding"
    fi
    
    # Check Fluentd
    if docker ps --format "table {{.Names}}\t{{.Status}}" | grep -q "mcp-fluentd"; then
        log_success "Fluentd: Running"
        
        # Check if fluentd is receiving logs
        local log_count=$(docker logs mcp-fluentd 2>&1 | wc -l)
        if [ "$log_count" -gt 0 ]; then
            log_success "Fluentd: Processing logs ($log_count log entries)"
        else
            log_warning "Fluentd: No log activity detected"
        fi
    else
        log_error "Fluentd: Not running"
    fi
    
    # Check Kibana
    if curl -f -s "http://localhost:5601/api/status" > /dev/null 2>&1; then
        log_success "Kibana: Accessible"
    else
        log_error "Kibana: Not accessible"
    fi
}

check_monitoring_stack() {
    log_header "Monitoring Stack Status"
    
    # Check Prometheus
    if curl -f -s "http://localhost:9090/-/healthy" > /dev/null 2>&1; then
        log_success "Prometheus: Healthy"
        
        # Check if Prometheus is scraping targets
        local targets=$(curl -s "http://localhost:9090/api/v1/targets" | jq '.data.activeTargets | length' 2>/dev/null || echo "0")
        log_info "Prometheus: Monitoring $targets targets"
    else
        log_error "Prometheus: Not healthy"
    fi
    
    # Check Grafana
    if curl -f -s "http://localhost:3000/api/health" > /dev/null 2>&1; then
        log_success "Grafana: Healthy"
    else
        log_error "Grafana: Not healthy"
    fi
}

check_mcp_application() {
    log_header "MCP Application Status"
    
    if curl -f -s "http://localhost:5000/health" > /dev/null 2>&1; then
        log_success "MCP Server: Healthy"
        
        # Check MCP logs for recent activity
        if docker logs mcp-code-review --tail 50 2>/dev/null | grep -q "$(date '+%Y-%m-%d')"; then
            log_success "MCP Server: Recent activity detected"
        else
            log_warning "MCP Server: No recent activity in logs"
        fi
    else
        log_error "MCP Server: Not responding"
        
        # Check if container is running but health check is failing
        if docker ps --format "table {{.Names}}" | grep -q "mcp-code-review"; then
            log_warning "MCP Server: Container running but health check failing"
            log_info "Checking MCP application logs..."
            docker logs mcp-code-review --tail 10 2>/dev/null || log_error "Cannot access MCP logs"
        else
            log_error "MCP Server: Container not running"
        fi
    fi
}

check_environment_config() {
    log_header "Environment Configuration"
    
    local env_file="$PROJECT_ROOT/.env"
    if [ -f "$env_file" ]; then
        log_success "Environment file: Found"
        
        # Check for required environment variables
        local required_vars=("GITLAB_TOKEN" "CLAUDE_API_KEY" "GITHUB_TOKEN")
        for var in "${required_vars[@]}"; do
            if grep -q "^${var}=" "$env_file" && ! grep -q "^${var}=$" "$env_file"; then
                log_success "Environment: $var is set"
            else
                log_warning "Environment: $var is not set or empty"
            fi
        done
    else
        log_error "Environment file: Not found at $env_file"
    fi
}

check_network_connectivity() {
    log_header "Network Connectivity"
    
    # Check if Docker network exists
    if docker network ls | grep -q "mcp-network"; then
        log_success "Docker network: mcp-network exists"
    else
        log_warning "Docker network: mcp-network not found"
    fi
    
    # Check port availability
    local ports=("5000" "3000" "9090" "9200" "5601" "8080" "9000")
    for port in "${ports[@]}"; do
        if ss -tuln | grep -q ":$port "; then
            log_success "Port $port: In use"
        else
            log_warning "Port $port: Not in use"
        fi
    done
}

generate_summary() {
    log_header "System Status Summary"
    
    local total_services=${#SERVICES[@]}
    local healthy_services=0
    
    for service in "${!SERVICES[@]}"; do
        local url="${SERVICES[$service]}"
        if curl -f -s --max-time 5 "$url" > /dev/null 2>&1; then
            ((healthy_services++))
        fi
    done
    
    local health_percentage=$((healthy_services * 100 / total_services))
    
    echo -e "${CYAN}System Health: $healthy_services/$total_services services healthy ($health_percentage%)${NC}"
    
    if [ $health_percentage -ge 80 ]; then
        log_success "Overall system status: GOOD"
    elif [ $health_percentage -ge 60 ]; then
        log_warning "Overall system status: DEGRADED"
    else
        log_error "Overall system status: CRITICAL"
    fi
    
    echo
    echo "Quick fixes:"
    echo "- Restart unhealthy services: docker compose restart <service>"
    echo "- Check logs: docker logs <container-name>"
    echo "- Full system restart: docker compose down && docker compose up -d"
    echo "- Setup GitLab: ./scripts/setup-gitlab.sh"
}

show_useful_urls() {
    log_header "Useful URLs"
    
    echo "🌐 Web Interfaces:"
    echo "   GitLab:       http://localhost:8080"
    echo "   Grafana:      http://localhost:3000 (admin/SecureGrafanaPassword123!)"
    echo "   Prometheus:   http://localhost:9090"
    echo "   Kibana:       http://localhost:5601"
    echo "   SonarQube:    http://localhost:9000 (admin/admin)"
    echo
    echo "🔧 API Endpoints:"
    echo "   MCP Health:   http://localhost:5000/health"
    echo "   GitLab API:   http://localhost:8080/api/v4"
    echo "   Prometheus:   http://localhost:9090/api/v1"
    echo
    echo "📊 Monitoring:"
    echo "   Docker stats: docker stats"
    echo "   Service logs: docker logs <container-name> -f"
    echo "   System logs:  journalctl -f"
}

main() {
    clear
    echo -e "${PURPLE}"
    echo "╔═══════════════════════════════════════════════════════════╗"
    echo "║                MCP Code Review System                     ║"
    echo "║                   Health Monitor                          ║"
    echo "╚═══════════════════════════════════════════════════════════╝"
    echo -e "${NC}"
    echo
    
    check_docker_status
    echo
    check_service_health
    echo
    check_gitlab_specific
    echo
    check_logging_pipeline
    echo
    check_monitoring_stack
    echo
    check_mcp_application
    echo
    check_environment_config
    echo
    check_network_connectivity
    echo
    generate_summary
    echo
    show_useful_urls
    
    echo
    log_info "Health check completed at $(date)"
}

# Allow for specific checks
case "${1:-all}" in
    "docker")
        check_docker_status
        ;;
    "services")
        check_service_health
        ;;
    "gitlab")
        check_gitlab_specific
        ;;
    "logs")
        check_logging_pipeline
        ;;
    "monitoring")
        check_monitoring_stack
        ;;
    "mcp")
        check_mcp_application
        ;;
    "env")
        check_environment_config
        ;;
    "network")
        check_network_connectivity
        ;;
    "all"|*)
        main
        ;;
esac