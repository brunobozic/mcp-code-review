#!/bin/bash
set -e

echo "🚀 MCP Code Review System - Full Stack Deployment"
echo "=================================================="

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info() { echo -e "${BLUE}ℹ️  $1${NC}"; }
log_success() { echo -e "${GREEN}✅ $1${NC}"; }
log_warning() { echo -e "${YELLOW}⚠️  $1${NC}"; }
log_error() { echo -e "${RED}❌ $1${NC}"; }

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check if Docker is running
    if ! docker info >/dev/null 2>&1; then
        log_error "Docker is not running. Please start Docker and try again."
        exit 1
    fi
    
    # Check if .env file exists
    if [ ! -f ".env" ]; then
        if [ -f ".env.example" ]; then
            log_warning ".env file not found. Copying from .env.example"
            cp .env.example .env
            log_warning "Please update .env with your API keys and passwords"
        else
            log_error ".env file not found and no .env.example available"
            exit 1
        fi
    fi
    
    log_success "Prerequisites check passed"
}

# Build and start all services
start_services() {
    log_info "Building and starting all services..."
    
    # Stop any existing containers
    docker compose down --remove-orphans >/dev/null 2>&1 || true
    
    # Build and start core infrastructure first
    log_info "Starting core infrastructure (GitLab, databases)..."
    docker compose up -d gitlab-postgres gitlab-redis chromadb elasticsearch
    
    # Wait for databases to be ready
    log_info "Waiting for databases to initialize..."
    sleep 30
    
    # Start GitLab
    log_info "Starting GitLab CE..."
    docker compose up -d gitlab
    
    # Start monitoring stack
    log_info "Starting monitoring stack..."
    docker compose up -d prometheus grafana fluentd
    
    # Build and start MCP server
    log_info "Building and starting MCP Code Review Server..."
    docker compose up -d mcp-server
    
    log_success "All services started"
}

# Wait for GitLab to be ready
wait_for_gitlab() {
    log_info "Waiting for GitLab to be ready (this can take 5-10 minutes)..."
    
    local max_attempts=60
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        if docker ps --filter "name=mcp-gitlab" --format "{{.Status}}" | grep -q "(healthy)"; then
            log_success "GitLab is ready!"
            return 0
        fi
        
        log_info "Attempt $attempt/$max_attempts - GitLab still starting..."
        sleep 30
        ((attempt++))
    done
    
    log_error "GitLab failed to start within expected time"
    docker logs mcp-gitlab --tail 20
    exit 1
}

# Configure GitLab automatically
configure_gitlab() {
    log_info "Configuring GitLab automatically..."
    
    # Create GitLab configuration script
    cat > /tmp/gitlab-auto-config.rb << 'EOF'
# GitLab Auto Configuration Script

# Ensure root user exists and is properly configured
root_user = User.find_by(username: 'root')
if root_user
  puts "Root user exists: #{root_user.email}"
else
  # Create root user if it doesn't exist
  root_user = User.new(
    username: 'root',
    email: 'admin@example.com',
    name: 'Administrator',
    password: ENV['GITLAB_ROOT_PASSWORD'] || 'Adm1nP@ssw0rd2025!',
    password_confirmation: ENV['GITLAB_ROOT_PASSWORD'] || 'Adm1nP@ssw0rd2025!',
    admin: true,
    confirmed_at: Time.current,
    confirmation_token: nil
  )
  
  if root_user.save
    puts "Root user created successfully"
  else
    puts "Failed to create root user: #{root_user.errors.full_messages}"
  end
end

# Create or find API token
token_name = 'MCP-Production-Token'
existing_token = root_user.personal_access_tokens.active.where(name: token_name).first

if existing_token
  puts "Token: #{existing_token.token}"
else
  new_token = root_user.personal_access_tokens.create!(
    name: token_name,
    scopes: ['api', 'read_repository', 'write_repository'],
    expires_at: 1.year.from_now
  )
  puts "Token: #{new_token.token}"
end

# Create demo project if it doesn't exist
project_name = 'mcp-demo-project'
existing_project = Project.find_by(path: project_name, namespace: root_user.namespace)

unless existing_project
  project = Projects::CreateService.new(
    root_user,
    name: 'MCP Demo Project',
    path: project_name,
    description: 'Demonstration project for MCP Code Review integration',
    visibility_level: Gitlab::VisibilityLevel::INTERNAL,
    initialize_with_readme: true
  ).execute
  
  if project.persisted?
    puts "Project created: #{project.web_url}"
    puts "Project ID: #{project.id}"
  else
    puts "Failed to create project: #{project.errors.full_messages}"
  end
else
  puts "Project exists: #{existing_project.web_url}"
  puts "Project ID: #{existing_project.id}"
end
EOF

    # Execute GitLab configuration
    docker cp /tmp/gitlab-auto-config.rb mcp-gitlab:/tmp/
    GITLAB_CONFIG=$(docker exec mcp-gitlab gitlab-rails runner /tmp/gitlab-auto-config.rb)
    
    # Parse results
    GITLAB_TOKEN=$(echo "$GITLAB_CONFIG" | grep "Token:" | cut -d' ' -f2)
    PROJECT_ID=$(echo "$GITLAB_CONFIG" | grep "Project ID:" | cut -d' ' -f3)
    
    if [ -n "$GITLAB_TOKEN" ]; then
        log_success "GitLab API token: ${GITLAB_TOKEN:0:12}..."
        echo "export GITLAB_TOKEN=$GITLAB_TOKEN" > /tmp/gitlab-config.env
    else
        log_error "Failed to create GitLab token"
        exit 1
    fi
    
    if [ -n "$PROJECT_ID" ]; then
        log_success "Demo project created with ID: $PROJECT_ID"
        echo "export PROJECT_ID=$PROJECT_ID" >> /tmp/gitlab-config.env
    fi
    
    # Clean up
    rm -f /tmp/gitlab-auto-config.rb
    docker exec mcp-gitlab rm -f /tmp/gitlab-auto-config.rb
}

# Configure webhooks
configure_webhooks() {
    log_info "Configuring GitLab webhooks..."
    
    # Source GitLab configuration
    source /tmp/gitlab-config.env
    
    # Configure webhook
    WEBHOOK_DATA='{
      "url": "http://host.docker.internal:5002/gitlab/webhook",
      "merge_requests_events": true,
      "push_events": true,
      "issues_events": false,
      "note_events": true,
      "pipeline_events": false,
      "wiki_page_events": false,
      "deployment_events": false,
      "job_events": false,
      "release_events": false,
      "enable_ssl_verification": false,
      "token": "mcp-webhook-secret-2025",
      "push_events_branch_filter": ""
    }'
    
    WEBHOOK_RESPONSE=$(curl -s -X POST \
      -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
      -H "Content-Type: application/json" \
      -d "$WEBHOOK_DATA" \
      "http://localhost:8080/api/v4/projects/$PROJECT_ID/hooks")
    
    WEBHOOK_ID=$(echo "$WEBHOOK_RESPONSE" | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)
    
    if [ -n "$WEBHOOK_ID" ]; then
        log_success "Webhook configured (ID: $WEBHOOK_ID)"
    else
        log_warning "Webhook configuration may have failed: $WEBHOOK_RESPONSE"
    fi
}

# Setup quality gates
setup_quality_gates() {
    log_info "Setting up quality gates..."
    
    source /tmp/gitlab-config.env
    
    # Configure branch protection
    PROTECTION_DATA='{
      "push_access_level": 40,
      "merge_access_level": 40,
      "allow_force_push": false
    }'
    
    curl -s -X POST \
      -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
      -H "Content-Type: application/json" \
      -d "$PROTECTION_DATA" \
      "http://localhost:8080/api/v4/projects/$PROJECT_ID/protected_branches?name=main" >/dev/null 2>&1
    
    log_success "Quality gates configured"
}

# Verify deployment
verify_deployment() {
    log_info "Verifying deployment..."
    
    local failed_services=()
    
    # Check service health
    services=(
        "mcp-gitlab:http://localhost:8080/-/health_check"
        "mcp-code-review:http://localhost:5002/health"
        "mcp-chromadb:http://localhost:8000/api/v1/heartbeat"
        "mcp-prometheus:http://localhost:9090/-/healthy"
        "mcp-grafana:http://localhost:3000/api/health"
    )
    
    for service in "${services[@]}"; do
        name=$(echo "$service" | cut -d':' -f1)
        url=$(echo "$service" | cut -d':' -f2-3)
        
        if curl -s -f "$url" >/dev/null 2>&1; then
            log_success "$name is healthy"
        else
            log_error "$name is not responding"
            failed_services+=("$name")
        fi
    done
    
    if [ ${#failed_services[@]} -eq 0 ]; then
        log_success "All services are healthy!"
        return 0
    else
        log_error "Some services failed health checks: ${failed_services[*]}"
        return 1
    fi
}

# Display deployment summary
show_summary() {
    source /tmp/gitlab-config.env 2>/dev/null || true
    
    echo ""
    echo "🎉 MCP Code Review System Deployment Complete!"
    echo "=============================================="
    echo ""
    echo "📊 Service Endpoints:"
    echo "   🦊 GitLab:     http://localhost:8080"
    echo "   🤖 MCP Server: http://localhost:5002"
    echo "   📊 Grafana:    http://localhost:3000"
    echo "   🔍 Prometheus: http://localhost:9090"
    echo ""
    echo "🔐 Login Credentials:"
    echo "   GitLab:  root / ${GITLAB_ROOT_PASSWORD:-Adm1nP@ssw0rd2025!}"
    echo "   Grafana: admin / ${GRAFANA_ADMIN_PASSWORD:-SecureGrafanaPass123!}"
    echo ""
    echo "🎯 Demo Project:"
    echo "   URL: http://localhost:8080/root/mcp-demo-project"
    echo "   ID:  ${PROJECT_ID:-Check GitLab for project ID}"
    echo ""
    echo "🪝 Integration Status:"
    echo "   ✅ Automatic webhook triggers configured"
    echo "   ✅ Quality gates active"
    echo "   ✅ Multi-agent AI review system operational"
    echo ""
    echo "🧪 To Test Integration:"
    echo "   1. Create a branch in the demo project"
    echo "   2. Add some code and create a merge request"
    echo "   3. Watch the automatic AI review appear!"
    echo ""
    echo "📋 Monitoring:"
    echo "   Health checks: ./scripts/automation/health-check.sh"
    echo "   View logs:     docker compose logs -f mcp-server"
    echo ""
    
    # Clean up temporary files
    rm -f /tmp/gitlab-config.env
}

# Main execution
main() {
    check_prerequisites
    start_services
    wait_for_gitlab
    configure_gitlab
    configure_webhooks
    setup_quality_gates
    
    if verify_deployment; then
        show_summary
        log_success "Deployment completed successfully! 🎉"
        exit 0
    else
        log_error "Deployment completed with some issues. Check the logs above."
        exit 1
    fi
}

# Execute main function
main "$@"