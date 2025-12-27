#!/bin/bash

# Automated Test Data Setup

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

log_info() { echo -e "\033[0;34m[INFO]\033[0m $1"; }
log_success() { echo -e "\033[0;32m[SUCCESS]\033[0m $1"; }

setup_test_project() {
    log_info "Setting up test ecommerce project..."
    
    local test_dir="$PROJECT_ROOT/ecommerce-test"
    
    # Clone if exists, otherwise create
    if [ -d "$test_dir" ]; then
        log_info "Test project directory exists, updating..."
        cd "$test_dir"
        git pull origin main || true
    else
        log_info "Cloning test project from GitLab..."
        cd "$PROJECT_ROOT"
        git clone http://localhost:9191/root/ecommerce-api-demo.git ecommerce-test || {
            log_info "Clone failed, test project will need to be created manually"
            return 0
        }
    fi
    
    log_success "Test project setup completed"
}

create_vulnerability_branch() {
    log_info "Creating vulnerability test branch..."
    
    local test_dir="$PROJECT_ROOT/ecommerce-test"
    if [ -d "$test_dir" ]; then
        cd "$test_dir"
        git checkout -b feature/comprehensive-security-issues 2>/dev/null || git checkout feature/comprehensive-security-issues
        log_success "Vulnerability branch ready"
    fi
}

main() {
    setup_test_project
    create_vulnerability_branch
    log_success "Test data automation completed!"
}

main "$@"
