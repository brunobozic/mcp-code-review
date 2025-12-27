#!/bin/bash

# Automated GitLab User and Project Setup

set -e

GITLAB_URL="${GITLAB_HOST:-http://localhost:9191}"
ROOT_TOKEN="${GITLAB_TOKEN:-${GITLAB_TOKEN:-glpat-PLACEHOLDER}}"

log_info() { echo -e "\033[0;34m[INFO]\033[0m $1"; }
log_success() { echo -e "\033[0;32m[SUCCESS]\033[0m $1"; }
log_error() { echo -e "\033[0;31m[ERROR]\033[0m $1"; }

wait_for_gitlab() {
    log_info "Waiting for GitLab to be ready..."
    local max_attempts=60
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if curl -s -f "$GITLAB_URL/-/health" > /dev/null 2>&1; then
            log_success "GitLab is ready!"
            return 0
        fi
        
        log_info "Attempt $attempt/$max_attempts - GitLab not ready yet, waiting 10s..."
        sleep 10
        ((attempt++))
    done
    
    log_error "GitLab failed to become ready after $max_attempts attempts"
    return 1
}

create_users() {
    log_info "Creating GitLab users..."
    
    # Create developer user
    curl -X POST "$GITLAB_URL/api/v4/users" \
        -H "PRIVATE-TOKEN: $ROOT_TOKEN" \
        -H "Content-Type: application/json" \
        -d '{
            "email": "developer@example.com",
            "password": "DevP@ssw0rd123!",
            "username": "developer",
            "name": "Developer User",
            "skip_confirmation": true,
            "admin": false
        }' || log_error "Failed to create developer user"
    
    # Create reviewer user  
    curl -X POST "$GITLAB_URL/api/v4/users" \
        -H "PRIVATE-TOKEN: $ROOT_TOKEN" \
        -H "Content-Type: application/json" \
        -d '{
            "email": "reviewer@example.com", 
            "password": "RevP@ssw0rd123!",
            "username": "reviewer",
            "name": "Code Reviewer",
            "skip_confirmation": true,
            "admin": false
        }' || log_error "Failed to create reviewer user"
    
    log_success "Users created successfully"
}

create_test_project() {
    log_info "Creating test project..."
    
    # Create ecommerce-api-demo project
    local project_response=$(curl -X POST "$GITLAB_URL/api/v4/projects" \
        -H "PRIVATE-TOKEN: $ROOT_TOKEN" \
        -H "Content-Type: application/json" \
        -d '{
            "name": "ecommerce-api-demo",
            "description": "E-commerce API demo project for MCP code review testing",
            "visibility": "internal",
            "issues_enabled": true,
            "merge_requests_enabled": true,
            "wiki_enabled": true,
            "snippets_enabled": true,
            "default_branch": "main"
        }' 2>/dev/null)
    
    echo "$project_response" | jq .
    log_success "Test project created successfully"
}

setup_webhooks() {
    log_info "Setting up GitLab webhooks..."
    
    # Get project ID (assuming project ID 1 for first project)
    curl -X POST "$GITLAB_URL/api/v4/projects/1/hooks" \
        -H "PRIVATE-TOKEN: $ROOT_TOKEN" \
        -H "Content-Type: application/json" \
        -d '{
            "url": "http://host.docker.internal:5002/api/gitlab/webhook",
            "push_events": false,
            "merge_requests_events": true,
            "issues_events": false,
            "wiki_page_events": false,
            "deployment_events": false,
            "job_events": false,
            "pipeline_events": false,
            "enable_ssl_verification": false
        }' || log_error "Failed to setup webhook"
    
    log_success "GitLab webhooks configured"
}

main() {
    wait_for_gitlab
    sleep 30  # Additional wait for GitLab to fully initialize
    create_users
    create_test_project
    setup_webhooks
    
    log_success "GitLab automation completed successfully!"
}

main "$@"
