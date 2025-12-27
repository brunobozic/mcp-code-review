#!/bin/bash

# Main System Orchestration Script
# Starts the complete MCP Code Review system with all dependencies

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }

usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --clean       Clean start (remove all containers and volumes)"
    echo "  --no-gitlab   Start without GitLab (MCP server only)"
    echo "  --help        Show this help message"
    echo ""
}

clean_system() {
    log_info "Cleaning existing containers and volumes..."
    docker compose down -v --remove-orphans 2>/dev/null || true
    docker system prune -f
    log_success "System cleaned"
}

start_infrastructure() {
    log_info "Starting infrastructure services..."
    
    # Start all services
    docker compose up -d postgresql redis chromadb
    
    # Wait for core services
    log_info "Waiting for PostgreSQL..."
    docker compose exec -T postgresql pg_isready -U gitlab || sleep 10
    
    log_info "Waiting for ChromaDB..."
    sleep 5
    
    # Start GitLab
    if [ "$SKIP_GITLAB" != "true" ]; then
        log_info "Starting GitLab..."
        docker compose up -d gitlab
        
        # Wait for GitLab to be ready
        log_info "Waiting for GitLab to initialize (this may take 2-3 minutes)..."
        sleep 120
        
        # Setup GitLab users and projects
        log_info "Setting up GitLab users and projects..."
        ./scripts/gitlab/setup-users-and-projects.sh
    fi
    
    log_success "Infrastructure services started"
}

start_mcp_server() {
    log_info "Starting MCP Code Review Server..."
    
    # Start MCP server in background
    ./scripts/start-mcp-server.sh &
    MCP_PID=$!
    echo $MCP_PID > .mcp-server.pid
    
    # Wait a moment for server to start
    sleep 10
    
    # Verify server is running
    if curl -s -f "http://localhost:5002/health" > /dev/null 2>&1; then
        log_success "MCP server started successfully on port 5002"
    else
        log_warning "MCP server may not be ready yet - check logs"
    fi
}

setup_test_data() {
    log_info "Setting up test data..."
    ./scripts/setup-test-data.sh
}

show_status() {
    echo ""
    echo "🎉 MCP Code Review System Status"
    echo "================================"
    echo ""
    
    # Check services
    if curl -s -f "http://localhost:19193/api/v1/heartbeat" > /dev/null 2>&1; then
        echo "✅ ChromaDB: Running (http://localhost:19193)"
    else
        echo "❌ ChromaDB: Not responding"
    fi
    
    if [ "$SKIP_GITLAB" != "true" ] && curl -s -f "http://localhost:9191/-/health" > /dev/null 2>&1; then
        echo "✅ GitLab: Running (http://localhost:9191)"
        echo "   👤 Root: root / Adm1nP@ssw0rd2025!"
        echo "   👤 Developer: developer@example.com / DevP@ssw0rd123!"
    elif [ "$SKIP_GITLAB" != "true" ]; then
        echo "❌ GitLab: Not responding"
    else
        echo "⏭️ GitLab: Skipped"
    fi
    
    if curl -s -f "http://localhost:5002/health" > /dev/null 2>&1; then
        echo "✅ MCP Server: Running (http://localhost:5002)"
    else
        echo "❌ MCP Server: Not responding"
    fi
    
    echo ""
    echo "🚀 Next Steps:"
    echo "1. Access GitLab: http://localhost:9191"
    echo "2. Create merge requests in ecommerce-api-demo project"
    echo "3. Watch AI code reviews appear automatically"
    echo ""
    echo "🛑 To stop: docker-compose down && kill \$(cat .mcp-server.pid 2>/dev/null || echo '')"
}

main() {
    local CLEAN=false
    local SKIP_GITLAB=false
    
    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --clean)
                CLEAN=true
                shift
                ;;
            --no-gitlab)
                SKIP_GITLAB=true
                shift
                ;;
            --help)
                usage
                exit 0
                ;;
            *)
                echo "Unknown option: $1"
                usage
                exit 1
                ;;
        esac
    done
    
    log_info "Starting MCP Code Review System..."
    
    if [ "$CLEAN" = true ]; then
        clean_system
    fi
    
    # Export SKIP_GITLAB for other scripts
    export SKIP_GITLAB
    
    start_infrastructure
    start_mcp_server
    setup_test_data
    show_status
}

main "$@"
