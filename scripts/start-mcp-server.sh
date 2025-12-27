#!/bin/bash

# MCP Server Startup Script

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

log_info() { echo -e "\033[0;34m[INFO]\033[0m $1"; }
log_success() { echo -e "\033[0;32m[SUCCESS]\033[0m $1"; }
log_error() { echo -e "\033[0;31m[ERROR]\033[0m $1"; }

# Check if required environment variables are set
check_environment() {
    log_info "Checking environment configuration..."
    
    if [ -z "$OPENAI_API_KEY" ] && [ -z "$CLAUDE_API_KEY" ]; then
        log_error "Either OPENAI_API_KEY or CLAUDE_API_KEY must be set"
        exit 1
    fi
    
    # Load .env if exists
    if [ -f "$PROJECT_ROOT/.env" ]; then
        log_info "Loading .env configuration..."
        set -a
        source "$PROJECT_ROOT/.env"
        set +a
    fi
    
    log_success "Environment configuration loaded"
}

# Wait for dependencies
wait_for_dependencies() {
    log_info "Waiting for dependencies..."
    
    # Wait for ChromaDB
    local max_attempts=30
    local attempt=1
    while [ $attempt -le $max_attempts ]; do
        if curl -s -f "http://localhost:19193/api/v1/heartbeat" > /dev/null 2>&1; then
            log_success "ChromaDB is ready!"
            break
        fi
        
        if [ $attempt -eq $max_attempts ]; then
            log_error "ChromaDB failed to become ready"
            exit 1
        fi
        
        log_info "Waiting for ChromaDB... (attempt $attempt/$max_attempts)"
        sleep 5
        ((attempt++))
    done
    
    # Wait for GitLab (optional)
    if curl -s -f "http://localhost:9191/-/health" > /dev/null 2>&1; then
        log_success "GitLab is ready!"
    else
        log_info "GitLab not ready - continuing without GitLab integration"
    fi
}

# Start MCP server
start_server() {
    log_info "Starting MCP Code Review Server..."
    
    cd "$PROJECT_ROOT"
    
    # Build the project
    log_info "Building project..."
    dotnet build src/Mcp.CodeReview/Mcp.CodeReview.csproj --configuration Release
    
    # Run the server
    log_info "Starting server on port ${MCP_PORT:-5002}..."
    dotnet run --project src/Mcp.CodeReview/Mcp.CodeReview.csproj --configuration Release -- --http --port ${MCP_PORT:-5002}
}

main() {
    check_environment
    wait_for_dependencies
    start_server
}

main "$@"
