#!/bin/bash
set -euo pipefail

# System Startup Script - Starts all services in correct order
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
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

check_docker() {
    log_header "Checking Docker Environment"
    
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed or not in PATH"
        exit 1
    fi
    
    if ! docker info &> /dev/null; then
        log_error "Docker daemon is not running"
        exit 1
    fi
    
    log_success "Docker is available and running"
}

check_environment() {
    log_header "Checking Environment Configuration"
    
    local env_file="$PROJECT_ROOT/.env"
    if [ ! -f "$env_file" ]; then
        log_error "Environment file not found at $env_file"
        log_info "Please create .env file with required configuration"
        exit 1
    fi
    
    log_success "Environment file found"
    
    # Check for basic required variables
    local required_vars=("CLAUDE_API_KEY" "GITLAB_TOKEN" "GITHUB_TOKEN")
    local missing_vars=()
    
    for var in "${required_vars[@]}"; do
        if ! grep -q "^${var}=" "$env_file" || grep -q "^${var}=$" "$env_file"; then
            missing_vars+=("$var")
        fi
    done
    
    if [ ${#missing_vars[@]} -gt 0 ]; then
        log_warning "Missing or empty environment variables: ${missing_vars[*]}"
        log_info "These will be set to placeholder values for testing"
    else
        log_success "All required environment variables are set"
    fi
}

stop_existing_services() {
    log_header "Stopping Existing Services"
    
    cd "$PROJECT_ROOT"
    
    if docker compose ps -q | grep -q .; then
        log_info "Stopping existing Docker Compose services..."
        docker compose down --remove-orphans
        log_success "Stopped existing services"
    else
        log_info "No existing services to stop"
    fi
}

start_infrastructure() {
    log_header "Starting Infrastructure Services"
    
    cd "$PROJECT_ROOT"
    
    # Start infrastructure first (databases, message queues, etc.)
    log_info "Starting PostgreSQL databases..."
    docker compose up -d gitlab-postgres sonar-postgres
    
    log_info "Starting Redis..."
    docker compose up -d gitlab-redis
    
    log_info "Starting Elasticsearch..."
    docker compose up -d elasticsearch
    
    log_info "Waiting for Elasticsearch to be ready..."
    wait_for_service "elasticsearch" "http://localhost:9200/_cluster/health" 120
    
    log_success "Infrastructure services started"
}

start_logging_monitoring() {
    log_header "Starting Logging and Monitoring"
    
    cd "$PROJECT_ROOT"
    
    log_info "Starting Fluentd..."
    docker compose up -d fluentd
    
    log_info "Starting Prometheus..."
    docker compose up -d prometheus
    
    log_info "Starting Grafana..."
    docker compose up -d grafana
    
    log_info "Starting Kibana..."
    docker compose up -d kibana
    
    log_info "Waiting for monitoring services..."
    wait_for_service "grafana" "http://localhost:3000/api/health" 60
    wait_for_service "prometheus" "http://localhost:9090/-/healthy" 60
    
    log_success "Logging and monitoring services started"
}

start_applications() {
    log_header "Starting Application Services"
    
    cd "$PROJECT_ROOT"
    
    log_info "Starting SonarQube..."
    docker compose up -d sonarqube
    
    log_info "Starting GitLab..."
    docker compose up -d gitlab
    
    log_info "Starting MCP Server..."
    docker compose up -d mcp-server
    
    log_success "Application services started"
}

wait_for_service() {
    local service_name=$1
    local health_url=$2
    local timeout=${3:-60}
    local attempt=1
    local max_attempts=$((timeout / 5))
    
    log_info "Waiting for $service_name to be ready (timeout: ${timeout}s)..."
    
    while [ $attempt -le $max_attempts ]; do
        if curl -f -s --max-time 5 "$health_url" > /dev/null 2>&1; then
            log_success "$service_name is ready!"
            return 0
        fi
        
        log_info "Attempt $attempt/$max_attempts - $service_name not ready yet..."
        sleep 5
        attempt=$((attempt + 1))
    done
    
    log_warning "$service_name failed to become ready within ${timeout}s"
    return 1
}

wait_for_gitlab() {
    log_header "Waiting for GitLab to Initialize"
    
    log_info "GitLab takes 5-10 minutes to fully initialize on first start..."
    log_info "You can monitor progress with: docker logs mcp-gitlab -f"
    
    local max_attempts=120  # 10 minutes
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if curl -f -s --max-time 10 "http://localhost:8080/users/sign_in" > /dev/null 2>&1; then
            log_success "GitLab web interface is accessible!"
            
            # Check if API is also ready
            if curl -f -s --max-time 10 "http://localhost:8080/api/v4/version" > /dev/null 2>&1; then
                log_success "GitLab API is ready!"
                return 0
            else
                log_info "GitLab web ready, waiting for API..."
            fi
        fi
        
        if [ $((attempt % 12)) -eq 0 ]; then  # Every minute
            log_info "Still waiting for GitLab... (${attempt}0 seconds elapsed)"
        fi
        
        sleep 5
        attempt=$((attempt + 1))
    done
    
    log_warning "GitLab failed to become ready within 10 minutes"
    log_info "You can check logs with: docker logs mcp-gitlab"
    return 1
}

show_status() {
    log_header "System Status Summary"
    
    cd "$PROJECT_ROOT"
    
    # Run our health monitor
    if [ -x "$SCRIPT_DIR/health-monitor.sh" ]; then
        "$SCRIPT_DIR/health-monitor.sh"
    else
        # Simple status check
        docker compose ps
    fi
}

show_next_steps() {
    log_header "Next Steps"
    
    echo "🎉 System startup completed!"
    echo
    echo "📋 Manual steps required:"
    echo "1. Wait for GitLab to fully initialize (5-10 minutes)"
    echo "2. Run GitLab setup: ./devops/scripts/setup-gitlab.sh"
    echo "3. Configure actual API keys in .env file"
    echo "4. Test the system with a sample PR"
    echo
    echo "🌐 Access URLs:"
    echo "   GitLab:       http://localhost:8080"
    echo "   Grafana:      http://localhost:3000 (admin/SecureGrafanaPassword123!)"
    echo "   SonarQube:    http://localhost:9000 (admin/admin)"
    echo "   Kibana:       http://localhost:5601"
    echo "   Prometheus:   http://localhost:9090"
    echo
    echo "🔧 Useful commands:"
    echo "   Health check: ./devops/scripts/health-monitor.sh"
    echo "   View logs:    docker logs <container-name> -f"
    echo "   Stop system:  docker compose down"
    echo
}

main() {
    clear
    echo -e "${PURPLE}"
    echo "╔═══════════════════════════════════════════════════════════╗"
    echo "║                MCP Code Review System                     ║"
    echo "║                    System Startup                        ║"
    echo "╚═══════════════════════════════════════════════════════════╝"
    echo -e "${NC}"
    echo
    
    check_docker
    echo
    check_environment
    echo
    stop_existing_services
    echo
    start_infrastructure
    echo
    start_logging_monitoring
    echo
    start_applications
    echo
    wait_for_gitlab
    echo
    show_status
    echo
    show_next_steps
}

# Allow for partial startup
case "${1:-all}" in
    "infrastructure")
        check_docker
        start_infrastructure
        ;;
    "monitoring")
        start_logging_monitoring
        ;;
    "applications")
        start_applications
        ;;
    "gitlab-wait")
        wait_for_gitlab
        ;;
    "status")
        show_status
        ;;
    "all"|*)
        main
        ;;
esac