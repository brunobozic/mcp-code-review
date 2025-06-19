#!/bin/bash

# GitLab Startup Script
# Professional setup script for GitLab with proper error handling and logging

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Configuration
COMPOSE_FILE="docker-compose-gitlab-fixed.yml"
ENV_FILE=".env.gitlab"
GITLAB_URL="http://localhost:8080"
HEALTH_CHECK_TIMEOUT=1200  # 20 minutes

# Function to check if Docker is running
check_docker() {
    log_info "Checking Docker status..."
    if ! docker info >/dev/null 2>&1; then
        log_error "Docker is not running. Please start Docker and try again."
        exit 1
    fi
    log_success "Docker is running"
}

# Function to check available resources
check_resources() {
    log_info "Checking system resources..."
    
    # Check available memory (require at least 6GB for GitLab)
    local available_memory=$(free -m | awk 'NR==2{printf "%.0f", $7}')
    if [ "$available_memory" -lt 6144 ]; then
        log_warning "Available memory: ${available_memory}MB. GitLab recommends at least 6GB."
        log_warning "GitLab may run slowly or fail to start properly."
    else
        log_success "Available memory: ${available_memory}MB"
    fi
    
    # Check available disk space (require at least 10GB)
    local available_disk=$(df -BG . | awk 'NR==2{gsub(/G/,"",$4); print $4}')
    if [ "$available_disk" -lt 10 ]; then
        log_warning "Available disk space: ${available_disk}GB. Recommend at least 10GB."
    else
        log_success "Available disk space: ${available_disk}GB"
    fi
}

# Function to setup environment
setup_environment() {
    log_info "Setting up environment..."
    
    # Copy environment file if it doesn't exist
    if [ ! -f .env ]; then
        if [ -f "$ENV_FILE" ]; then
            cp "$ENV_FILE" .env
            log_success "Environment file created from template"
        else
            log_warning "No environment template found, using defaults"
        fi
    else
        log_info "Environment file already exists"
    fi
    
    # Create necessary directories
    mkdir -p data logs scripts
    log_success "Directories created"
}

# Function to cleanup old containers and volumes (optional)
cleanup_old_installation() {
    log_info "Checking for existing GitLab containers..."
    
    if docker ps -a --format "table {{.Names}}" | grep -E "(gitlab|postgres|redis)" >/dev/null 2>&1; then
        log_warning "Found existing GitLab-related containers"
        read -p "Do you want to remove them and start fresh? (y/N): " -r
        if [[ $REPLY =~ ^[Yy]$ ]]; then
            log_info "Stopping and removing existing containers..."
            docker-compose -f "$COMPOSE_FILE" down -v --remove-orphans >/dev/null 2>&1 || true
            docker system prune -f >/dev/null 2>&1
            log_success "Cleanup completed"
        fi
    fi
}

# Function to start GitLab services
start_services() {
    log_info "Starting GitLab services..."
    
    # Pull latest images
    log_info "Pulling latest Docker images..."
    if ! docker-compose -f "$COMPOSE_FILE" pull; then
        log_error "Failed to pull Docker images"
        exit 1
    fi
    
    # Start services
    log_info "Starting containers..."
    if ! docker-compose -f "$COMPOSE_FILE" up -d; then
        log_error "Failed to start containers"
        exit 1
    fi
    
    log_success "Containers started successfully"
}

# Function to wait for services to be healthy
wait_for_services() {
    log_info "Waiting for services to become healthy..."
    
    local timeout=$HEALTH_CHECK_TIMEOUT
    local elapsed=0
    local check_interval=30
    
    while [ $elapsed -lt $timeout ]; do
        local all_healthy=true
        
        # Check PostgreSQL
        if ! docker-compose -f "$COMPOSE_FILE" exec -T gitlab-postgres pg_isready -U gitlab >/dev/null 2>&1; then
            all_healthy=false
        fi
        
        # Check Redis
        if ! docker-compose -f "$COMPOSE_FILE" exec -T gitlab-redis redis-cli ping >/dev/null 2>&1; then
            all_healthy=false
        fi
        
        # Check GitLab
        if ! curl -f -s "$GITLAB_URL/-/health" >/dev/null 2>&1; then
            all_healthy=false
        fi
        
        if [ "$all_healthy" = true ]; then
            log_success "All services are healthy!"
            return 0
        fi
        
        log_info "Services still starting... (${elapsed}s/${timeout}s)"
        sleep $check_interval
        elapsed=$((elapsed + check_interval))
    done
    
    log_error "Services failed to become healthy within ${timeout} seconds"
    return 1
}

# Function to show service status
show_status() {
    log_info "Current service status:"
    docker-compose -f "$COMPOSE_FILE" ps
    
    echo ""
    log_info "GitLab URLs:"
    echo "  Web Interface: $GITLAB_URL"
    echo "  SSH: ssh://git@localhost:2222"
    
    echo ""
    log_info "Default credentials:"
    echo "  Username: root"
    echo "  Password: Adm1nP@ssw0rd2025!"
    
    echo ""
    log_info "Additional users (created by setup script):"
    echo "  Developer: developer@example.com / DevP@ssw0rd123!"
    echo "  Reviewer: reviewer@example.com / RevP@ssw0rd123!"
}

# Function to follow logs
follow_logs() {
    log_info "Following GitLab logs (Ctrl+C to stop)..."
    docker-compose -f "$COMPOSE_FILE" logs -f gitlab
}

# Function to show help
show_help() {
    echo "GitLab Startup Script"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --help, -h          Show this help message"
    echo "  --cleanup, -c       Remove existing containers before starting"
    echo "  --logs, -l          Follow GitLab logs after startup"
    echo "  --status, -s        Show service status only"
    echo "  --stop              Stop all services"
    echo "  --restart           Restart all services"
    echo ""
    echo "Examples:"
    echo "  $0                  Start GitLab with default settings"
    echo "  $0 --cleanup        Clean up and start fresh"
    echo "  $0 --logs           Start and follow logs"
    echo "  $0 --status         Check current status"
}

# Main function
main() {
    local cleanup=false
    local follow_logs_flag=false
    local status_only=false
    local stop_services=false
    local restart_services=false
    
    # Parse command line arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --help|-h)
                show_help
                exit 0
                ;;
            --cleanup|-c)
                cleanup=true
                shift
                ;;
            --logs|-l)
                follow_logs_flag=true
                shift
                ;;
            --status|-s)
                status_only=true
                shift
                ;;
            --stop)
                stop_services=true
                shift
                ;;
            --restart)
                restart_services=true
                shift
                ;;
            *)
                log_error "Unknown option: $1"
                show_help
                exit 1
                ;;
        esac
    done
    
    # Handle special operations
    if [ "$stop_services" = true ]; then
        log_info "Stopping GitLab services..."
        docker-compose -f "$COMPOSE_FILE" down
        log_success "Services stopped"
        exit 0
    fi
    
    if [ "$restart_services" = true ]; then
        log_info "Restarting GitLab services..."
        docker-compose -f "$COMPOSE_FILE" restart
        log_success "Services restarted"
        exit 0
    fi
    
    if [ "$status_only" = true ]; then
        show_status
        exit 0
    fi
    
    # Main startup sequence
    echo "========================================"
    echo "      GitLab Professional Setup        "
    echo "========================================"
    echo ""
    
    # Pre-flight checks
    check_docker
    check_resources
    setup_environment
    
    # Optional cleanup
    if [ "$cleanup" = true ]; then
        cleanup_old_installation
    fi
    
    # Start services
    start_services
    
    # Wait for services to be ready
    if wait_for_services; then
        log_success "GitLab is ready!"
        show_status
        
        # Follow logs if requested
        if [ "$follow_logs_flag" = true ]; then
            echo ""
            follow_logs
        fi
    else
        log_error "GitLab failed to start properly"
        echo ""
        log_info "Checking logs for errors:"
        docker-compose -f "$COMPOSE_FILE" logs --tail=50 gitlab
        exit 1
    fi
}

# Run main function with all arguments
main "$@"