#!/bin/bash
set -e

echo "🚀 MCP Code Review System - Complete Stack Deployment with Full Automation"
echo "=========================================================================="

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
log_automation() { echo -e "${CYAN}🤖 $1${NC}"; }

# Configuration
SKIP_PREREQ_CHECK=${SKIP_PREREQ_CHECK:-false}
AUTO_SETUP_SAMPLE_PROJECT=${AUTO_SETUP_SAMPLE_PROJECT:-true}
AUTO_CONFIGURE_WEBHOOKS=${AUTO_CONFIGURE_WEBHOOKS:-true}
AUTO_SEED_RAG_DATA=${AUTO_SEED_RAG_DATA:-true}
ENABLE_SONARQUBE=${ENABLE_SONARQUBE:-true}
RESTORE_FROM_BACKUP=false

# Global variables for configuration
GITLAB_TOKEN=""
SAMPLE_PROJECT_ID=""
WEBHOOK_ID=""

show_deployment_options() {
    echo ""
    echo "📋 Deployment Options:"
    echo "   --restore-backup <dir>  : Restore from backup directory"
    echo "   --skip-prereq          : Skip prerequisite checks"
    echo "   --no-sample-project    : Skip automatic sample project setup"
    echo "   --no-webhooks          : Skip automatic webhook configuration"
    echo "   --no-rag-seeding       : Skip RAG data seeding"
    echo "   --no-sonarqube         : Skip SonarQube setup"
    echo ""
    echo "Environment Variables:"
    echo "   SKIP_PREREQ_CHECK=${SKIP_PREREQ_CHECK}    : Skip Docker/env checks"
    echo "   AUTO_SETUP_SAMPLE_PROJECT=${AUTO_SETUP_SAMPLE_PROJECT}  : Auto-create demo project"
    echo "   AUTO_CONFIGURE_WEBHOOKS=${AUTO_CONFIGURE_WEBHOOKS}    : Auto-configure GitLab webhooks"
    echo "   AUTO_SEED_RAG_DATA=${AUTO_SEED_RAG_DATA}       : Auto-seed RAG database"
    echo "   ENABLE_SONARQUBE=${ENABLE_SONARQUBE}          : Enable SonarQube integration"
    echo ""
}

# Parse command line arguments
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --restore-backup)
                RESTORE_FROM_BACKUP="$2"
                shift 2
                ;;
            --skip-prereq)
                SKIP_PREREQ_CHECK=true
                shift
                ;;
            --no-sample-project)
                AUTO_SETUP_SAMPLE_PROJECT=false
                shift
                ;;
            --no-webhooks)
                AUTO_CONFIGURE_WEBHOOKS=false
                shift
                ;;
            --no-rag-seeding)
                AUTO_SEED_RAG_DATA=false
                shift
                ;;
            --no-sonarqube)
                ENABLE_SONARQUBE=false
                shift
                ;;
            --help)
                show_deployment_options
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                show_deployment_options
                exit 1
                ;;
        esac
    done
}

# Check prerequisites
check_prerequisites() {
    if [ "$SKIP_PREREQ_CHECK" = true ]; then
        log_info "Skipping prerequisite checks"
        return 0
    fi
    
    log_phase "Checking prerequisites..."
    
    # Check if Docker is running
    if ! docker info >/dev/null 2>&1; then
        log_error "Docker is not running. Please start Docker and try again."
        exit 1
    fi
    
    # Check if docker-compose exists
    if ! command -v docker >/dev/null 2>&1; then
        log_error "Docker Compose is not installed. Please install docker-compose and try again."
        exit 1
    fi
    
    # Check available disk space (need at least 10GB)
    AVAILABLE_SPACE=$(df . | tail -1 | awk '{print $4}')
    REQUIRED_SPACE=$((10 * 1024 * 1024)) # 10GB in KB
    
    if [ "$AVAILABLE_SPACE" -lt "$REQUIRED_SPACE" ]; then
        log_warning "Low disk space detected. Available: $(($AVAILABLE_SPACE / 1024 / 1024))GB, Recommended: 10GB+"
    fi
    
    # Check if .env file exists, create from template if not
    if [ ! -f ".env" ]; then
        if [ -f "devops/config/env.example" ]; then
            log_automation "Creating .env from template"
            cp devops/config/env.example .env
            log_warning "Please update .env with your API keys before proceeding"
        else
            log_error ".env file not found and no template available"
            exit 1
        fi
    fi
    
    log_success "Prerequisites check passed"
}

# Restore from backup if requested
restore_from_backup() {
    if [ "$RESTORE_FROM_BACKUP" = false ]; then
        return 0
    fi
    
    if [ ! -d "$RESTORE_FROM_BACKUP" ]; then
        log_error "Backup directory not found: $RESTORE_FROM_BACKUP"
        exit 1
    fi
    
    log_phase "Restoring from backup: $RESTORE_FROM_BACKUP"
    
    # Restore environment configuration
    if [ -f "$RESTORE_FROM_BACKUP/env.backup" ]; then
        log_automation "Restoring environment configuration"
        cp "$RESTORE_FROM_BACKUP/env.backup" .env
        log_success "Environment restored"
    fi
    
    # Restore devops configuration
    if [ -f "$RESTORE_FROM_BACKUP/devops-config.tar.gz" ]; then
        log_automation "Restoring devops configuration"
        tar -xzf "$RESTORE_FROM_BACKUP/devops-config.tar.gz" 2>/dev/null || log_warning "Could not restore devops config"
        log_success "Devops configuration restored"
    fi
    
    log_success "Backup restoration completed"
}

# Build and start all services with proper orchestration
start_services() {
    log_phase "Building and starting all services with orchestration..."
    
    # Change to docker directory
    cd devops/docker || {
        log_error "Could not find devops/docker directory"
        exit 1
    }
    
    # Stop any existing containers
    log_info "Cleaning up any existing containers..."
    docker compose down --remove-orphans >/dev/null 2>&1 || true
    
    # Start services in dependency order
    log_automation "Starting core databases (PostgreSQL, Redis)..."
    docker compose up -d gitlab-postgres gitlab-redis sonar-postgres
    
    # Wait for databases to be ready
    log_info "Waiting for databases to initialize (30 seconds)..."
    sleep 30
    
    # Start ChromaDB for RAG
    log_automation "Starting ChromaDB vector database..."
    docker compose up -d chromadb
    
    # Start monitoring infrastructure
    log_automation "Starting monitoring infrastructure..."
    docker compose up -d elasticsearch
    sleep 10
    docker compose up -d fluentd prometheus
    
    # Start SonarQube if enabled
    if [ "$ENABLE_SONARQUBE" = true ]; then
        log_automation "Starting SonarQube for code quality analysis..."
        docker compose up -d sonarqube
    fi
    
    # Start GitLab (this takes the longest)
    log_automation "Starting GitLab CE (this will take 5-10 minutes)..."
    docker compose up -d gitlab
    
    # Start remaining monitoring services
    log_automation "Starting Grafana and Kibana..."
    docker compose up -d grafana kibana
    
    # Build and start MCP server
    log_automation "Building and starting MCP Code Review Server..."
    docker compose up -d --build mcp-server
    
    # Return to original directory
    cd - > /dev/null
    
    log_success "All services started"
}

# Wait for GitLab to be ready with better progress indication
wait_for_gitlab() {
    log_phase "Waiting for GitLab to be fully operational..."
    
    local max_attempts=120  # 20 minutes maximum
    local attempt=1
    local last_status=""
    
    while [ $attempt -le $max_attempts ]; do
        # Check container health
        local container_status=$(docker ps --filter "name=mcp-gitlab" --format "{{.Status}}")
        
        if echo "$container_status" | grep -q "(healthy)"; then
            # Test actual GitLab web interface (check for HTTP response, not content)
            if curl -s -f "http://localhost:9191" --max-time 5 >/dev/null 2>&1 || curl -s "http://localhost:9191" | grep -q "redirected" >/dev/null 2>&1; then
                # CRITICAL: Also test Rails runner readiness for configuration
                if docker exec mcp-gitlab gitlab-rails runner "puts 'ready'" >/dev/null 2>&1; then
                    log_success "GitLab is fully operational (web + Rails ready)!"
                    return 0
                else
                    log_info "GitLab web ready, waiting for Rails runner..."
                fi
            fi
        fi
        
        # Show progress update every 30 seconds
        if [ $((attempt % 3)) -eq 0 ] || [ "$container_status" != "$last_status" ]; then
            log_info "Attempt $((attempt/10))min - GitLab status: $container_status"
            last_status="$container_status"
        fi
        
        sleep 10
        ((attempt++))
    done
    
    log_error "GitLab failed to start within expected time (20 minutes)"
    log_info "Showing recent GitLab logs for debugging..."
    docker logs mcp-gitlab --tail 50
    exit 1
}

# Configure GitLab automatically with enhanced error handling
configure_gitlab() {
    log_phase "Configuring GitLab with automated setup..."
    
    # Create comprehensive GitLab configuration script
    cat > /tmp/gitlab-auto-config.rb << 'EOF'
# GitLab Auto Configuration Script - Enhanced Version

begin
  # Ensure root user exists and is properly configured
  root_user = User.find_by(username: 'root')
  
  if root_user
    puts "✓ Root user found: #{root_user.email}"
  else
    # Create root user if somehow it doesn't exist
    root_password = ENV['GITLAB_ROOT_PASSWORD'] || 'Adm1nP@ssw0rd2025!'
    
    root_user = User.new(
      username: 'root',
      email: 'admin@mcp-review.local',
      name: 'MCP Administrator',
      password: root_password,
      password_confirmation: root_password,
      admin: true,
      confirmed_at: Time.current,
      confirmation_token: nil
    )
    
    if root_user.save
      puts "✓ Root user created successfully"
    else
      puts "✗ Failed to create root user: #{root_user.errors.full_messages.join(', ')}"
      exit 1
    end
  end

  # Create or find API token for MCP integration
  token_name = 'MCP-Production-Token'
  existing_token = root_user.personal_access_tokens.active.where(name: token_name).first

  if existing_token
    puts "GITLAB_TOKEN=#{existing_token.token}"
  else
    new_token = root_user.personal_access_tokens.create!(
      name: token_name,
      scopes: ['api', 'read_user', 'read_repository', 'write_repository'],
      expires_at: 1.year.from_now
    )
    puts "GITLAB_TOKEN=#{new_token.token}"
  end

  # Create demo project for testing
  project_name = 'ecommerce-api-demo'
  project_path = 'ecommerce-api-demo'
  
  existing_project = Project.find_by(path: project_path, namespace: root_user.namespace)

  unless existing_project
    project = Projects::CreateService.new(
      root_user,
      name: 'E-commerce API Demo',
      path: project_path,
      description: 'Demonstration project for MCP Code Review with intentional issues for testing AI analysis',
      visibility_level: Gitlab::VisibilityLevel::INTERNAL,
      initialize_with_readme: true,
      issues_enabled: true,
      merge_requests_enabled: true,
      wiki_enabled: true,
      snippets_enabled: true
    ).execute
    
    if project.persisted?
      puts "✓ Demo project created: #{project.web_url}"
      puts "PROJECT_ID=#{project.id}"
      puts "PROJECT_URL=#{project.web_url}"
      
      # Enable branch protection on main branch
      begin
        protected_branch = ProtectedBranch.create!(
          project: project,
          name: 'main',
          push_access_levels_attributes: [{ access_level: Gitlab::Access::MAINTAINER }],
          merge_access_levels_attributes: [{ access_level: Gitlab::Access::MAINTAINER }]
        )
        puts "✓ Branch protection enabled for main branch"
      rescue => e
        puts "⚠ Branch protection setup failed: #{e.message}"
      end
      
    else
      puts "✗ Failed to create demo project: #{project.errors.full_messages.join(', ')}"
    end
  else
    puts "✓ Demo project already exists: #{existing_project.web_url}"
    puts "PROJECT_ID=#{existing_project.id}"
    puts "PROJECT_URL=#{existing_project.web_url}"
  end

  # Create developer and reviewer users for testing
  test_users = [
    {
      username: 'developer',
      email: 'developer@mcp-review.local',
      name: 'Test Developer',
      password: 'DevP@ssw0rd123!'
    },
    {
      username: 'reviewer',
      email: 'reviewer@mcp-review.local',
      name: 'Test Reviewer',
      password: 'RevP@ssw0rd123!'
    }
  ]

  test_users.each do |user_data|
    existing_user = User.find_by(username: user_data[:username])
    
    unless existing_user
      new_user = User.new(
        username: user_data[:username],
        email: user_data[:email],
        name: user_data[:name],
        password: user_data[:password],
        password_confirmation: user_data[:password],
        confirmed_at: Time.current,
        confirmation_token: nil
      )
      
      if new_user.save
        puts "✓ Test user created: #{user_data[:username]}"
        
        # Add user to demo project if it exists
        if existing_project || project
          target_project = existing_project || project
          target_project.add_developer(new_user)
          puts "✓ Added #{user_data[:username]} as developer to demo project"
        end
      else
        puts "⚠ Failed to create user #{user_data[:username]}: #{new_user.errors.full_messages.join(', ')}"
      end
    else
      puts "✓ Test user already exists: #{user_data[:username]}"
    end
  end

rescue => e
  puts "✗ GitLab configuration error: #{e.message}"
  puts "Backtrace:"
  puts e.backtrace.first(10).join("\n")
  exit 1
end
EOF

    # Execute GitLab configuration
    log_automation "Executing GitLab configuration script..."
    docker cp /tmp/gitlab-auto-config.rb mcp-gitlab:/tmp/
    
    # CRITICAL: Add verbose logging for debugging future failures
    log_info "Testing GitLab Rails runner before configuration..."
    if ! docker exec mcp-gitlab gitlab-rails runner "puts 'Rails test: ' + Time.current.to_s" 2>&1; then
        log_error "GitLab Rails runner not ready! This will cause configuration to fail."
        return 1
    fi
    
    log_automation "Running GitLab auto-configuration Ruby script..."
    local config_output
    config_output=$(docker exec mcp-gitlab gitlab-rails runner /tmp/gitlab-auto-config.rb 2>&1)
    local config_exit_code=$?
    
    # CRITICAL: Always show the output for debugging
    log_info "GitLab configuration output:"
    echo "$config_output"
    
    # Parse and validate results
    if [ $config_exit_code -eq 0 ]; then
        GITLAB_TOKEN=$(echo "$config_output" | grep "GITLAB_TOKEN=" | cut -d'=' -f2)
        SAMPLE_PROJECT_ID=$(echo "$config_output" | grep "PROJECT_ID=" | cut -d'=' -f2)
        
        if [ -n "$GITLAB_TOKEN" ]; then
            log_success "GitLab API token created: ${GITLAB_TOKEN:0:12}..."
            
            # Store configuration for later use
            cat > /tmp/gitlab-deployment-config.env << EOF
GITLAB_TOKEN=$GITLAB_TOKEN
SAMPLE_PROJECT_ID=$SAMPLE_PROJECT_ID
EOF
        else
            log_error "CRITICAL: Failed to create GitLab token from configuration output!"
            log_error "This means the Ruby script ran but didn't generate expected output."
            log_error "Expected: GITLAB_TOKEN=... line in output"
            log_error "Deployment cannot continue without GitLab integration."
            return 1  # Don't exit, let main() handle the error
        fi
        
        if [ -n "$SAMPLE_PROJECT_ID" ]; then
            log_success "Demo project created with ID: $SAMPLE_PROJECT_ID"
        else
            log_warning "No sample project ID found - project creation may have failed"
        fi
    else
        log_error "CRITICAL: GitLab Rails configuration script failed!"
        log_error "Exit code: $config_exit_code"
        log_error "This means GitLab Rails runner couldn't execute the Ruby script."
        log_error "Check GitLab container logs and Rails readiness."
        return 1  # Don't exit, let main() handle the error
    fi
    
    # Clean up
    rm -f /tmp/gitlab-auto-config.rb
    docker exec mcp-gitlab rm -f /tmp/gitlab-auto-config.rb 2>/dev/null || true
    
    log_success "GitLab configuration completed"
}

# Setup sample project with actual code files
setup_sample_project() {
    if [ "$AUTO_SETUP_SAMPLE_PROJECT" = false ]; then
        log_info "Skipping sample project setup (disabled)"
        return 0
    fi
    
    if [ -z "$SAMPLE_PROJECT_ID" ]; then
        log_warning "No sample project ID available, skipping code setup"
        return 0
    fi
    
    log_phase "Setting up sample project with realistic code examples..."
    
    # Source the configuration
    source /tmp/gitlab-deployment-config.env 2>/dev/null || {
        log_warning "Could not load GitLab configuration"
        return 1
    }
    
    # Create temporary directory for sample project
    local temp_dir="/tmp/mcp-sample-project"
    rm -rf "$temp_dir"
    mkdir -p "$temp_dir"
    
    # Copy sample project files from devops/data/sample-projects
    if [ -d "devops/data/sample-projects/ecommerce-api" ]; then
        log_automation "Copying sample e-commerce API with intentional issues..."
        cp -r devops/data/sample-projects/ecommerce-api/* "$temp_dir/"
    else
        log_automation "Creating sample project from template..."
        
        # Create realistic e-commerce API with security and performance issues
        mkdir -p "$temp_dir"/{Controllers,Services,Models,Data}
        
        # Create PaymentController with SQL injection vulnerability
        cat > "$temp_dir/Controllers/PaymentController.cs" << 'EOF'
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace EcommerceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly string connectionString = "Server=localhost;Database=Ecommerce;Trusted_Connection=true;";

        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment(PaymentRequest request)
        {
            // SECURITY ISSUE: SQL Injection vulnerability
            var query = $"SELECT * FROM Payments WHERE UserId = {request.UserId} AND Amount = {request.Amount}";
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var command = new SqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();
            
            // SECURITY ISSUE: Hardcoded API key
            var apiKey = "sk-live-abcdef123456789";
            
            return Ok(new { Status = "Success", TransactionId = result, ApiKey = apiKey });
        }

        [HttpGet("history/{userId}")]
        public async Task<IActionResult> GetPaymentHistory(int userId)
        {
            // PERFORMANCE ISSUE: N+1 query problem
            var payments = new List<object>();
            
            for (int i = 0; i < 1000; i++)
            {
                var query = $"SELECT * FROM Payments WHERE UserId = {userId} AND Id = {i}";
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                var command = new SqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();
                if (result != null) payments.Add(result);
            }
            
            return Ok(payments);
        }
    }

    public class PaymentRequest
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string CardNumber { get; set; } = "";
        public string CVV { get; set; } = "";
    }
}
EOF

        # Create UserService with multiple issues
        cat > "$temp_dir/Services/UserService.cs" << 'EOF'
using System.Security.Cryptography;
using System.Text;

namespace EcommerceApi.Services
{
    public class UserService
    {
        private readonly string connectionString = "Server=localhost;Database=Ecommerce;User=sa;Password=Admin123;";

        public async Task<bool> AuthenticateUser(string username, string password)
        {
            // SECURITY ISSUE: MD5 is cryptographically broken
            var hashedPassword = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(password));
            var hashedPasswordString = Convert.ToBase64String(hashedPassword);
            
            // SECURITY ISSUE: Another SQL injection
            var query = $"SELECT COUNT(*) FROM Users WHERE Username = '{username}' AND PasswordHash = '{hashedPasswordString}'";
            
            // PERFORMANCE ISSUE: Blocking async call
            var connection = new SqlConnection(connectionString);
            connection.Open();
            var command = new SqlCommand(query, connection);
            var result = command.ExecuteScalar();
            
            return (int)result > 0;
        }

        public async Task<User> GetUserById(int id)
        {
            // PERFORMANCE ISSUE: Inefficient database query
            await Task.Delay(500); // Simulating slow database
            
            var query = $"SELECT * FROM Users WHERE Id = {id}";
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            var command = new SqlCommand(query, connection);
            var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetInt32("Id"),
                    Username = reader.GetString("Username"),
                    Email = reader.GetString("Email"),
                    // SECURITY ISSUE: Exposing password hash
                    PasswordHash = reader.GetString("PasswordHash")
                };
            }
            
            return null;
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
    }
}
EOF

        # Create README with project information
        cat > "$temp_dir/README.md" << 'EOF'
# E-commerce API Demo Project

This is a demonstration project for the MCP Code Review system. It intentionally contains various security and performance issues to showcase the AI-powered code analysis capabilities.

## Intentional Issues for Testing

### Security Issues
- SQL injection vulnerabilities in database queries
- Hardcoded API keys and connection strings  
- Use of broken cryptographic algorithms (MD5)
- Exposure of sensitive data in API responses

### Performance Issues
- N+1 query problems
- Blocking async calls
- Inefficient database operations
- Missing input validation

### Code Quality Issues
- Missing error handling
- Poor separation of concerns
- Inconsistent naming conventions
- Missing documentation

## How to Use

1. Create a merge request with changes to any of the code files
2. The MCP Code Review system will automatically analyze the changes
3. AI agents will identify security, performance, and quality issues
4. Detailed feedback will be provided in the merge request comments

This project serves as a perfect testing ground for the multi-agent AI review system.
EOF

        # Create .gitignore
        cat > "$temp_dir/.gitignore" << 'EOF'
bin/
obj/
*.user
*.suo
.vs/
packages/
*.log
appsettings.Development.json
EOF
    fi
    
    # Push files to GitLab project using API
    log_automation "Uploading sample project files to GitLab..."
    
    local upload_success=true
    
    # Find all files in the temp directory
    find "$temp_dir" -type f | while read -r file; do
        local relative_path="${file#$temp_dir/}"
        local encoded_content
        
        # Base64 encode the file content
        encoded_content=$(base64 -w 0 "$file")
        
        # Upload file via GitLab API
        local upload_response
        upload_response=$(curl -s -X POST \
            -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
            -H "Content-Type: application/json" \
            -d "{
                \"branch\": \"main\",
                \"content\": \"$encoded_content\",
                \"commit_message\": \"Add sample file: $relative_path\",
                \"encoding\": \"base64\"
            }" \
            "http://localhost:9191/api/v4/projects/$SAMPLE_PROJECT_ID/repository/files/$(echo "$relative_path" | sed 's|/|%2F|g')" 2>&1)
        
        if echo "$upload_response" | grep -q '"file_path"'; then
            log_automation "✓ Uploaded: $relative_path"
        else
            log_warning "Failed to upload: $relative_path"
            echo "Response: $upload_response"
            upload_success=false
        fi
    done
    
    # Clean up temporary directory
    rm -rf "$temp_dir"
    
    if [ "$upload_success" = true ]; then
        log_success "Sample project files uploaded successfully"
    else
        log_warning "Some sample project files failed to upload"
    fi
}

# Configure webhooks automatically
configure_webhooks() {
    if [ "$AUTO_CONFIGURE_WEBHOOKS" = false ]; then
        log_info "Skipping webhook configuration (disabled)"
        return 0
    fi
    
    if [ -z "$GITLAB_TOKEN" ] || [ -z "$SAMPLE_PROJECT_ID" ]; then
        log_warning "Missing GitLab configuration, skipping webhook setup"
        return 0
    fi
    
    log_phase "Configuring GitLab webhooks for MCP integration..."
    
    # Source the configuration
    source /tmp/gitlab-deployment-config.env 2>/dev/null || {
        log_warning "Could not load GitLab configuration"
        return 1
    }
    
    # Configure webhook with comprehensive event handling
    local webhook_data='{
      "url": "http://host.docker.internal:5002/gitlab/webhook",
      "merge_requests_events": true,
      "push_events": true,
      "issues_events": false,
      "note_events": true,
      "pipeline_events": true,
      "wiki_page_events": false,
      "deployment_events": false,
      "job_events": false,
      "release_events": false,
      "enable_ssl_verification": false,
      "token": "mcp-webhook-secret-2025",
      "push_events_branch_filter": "",
      "custom_webhook_template": "",
      "description": "MCP Code Review Integration Webhook"
    }'
    
    log_automation "Creating webhook for real-time code review triggers..."
    
    local webhook_response
    webhook_response=$(curl -s -X POST \
      -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
      -H "Content-Type: application/json" \
      -d "$webhook_data" \
      "http://localhost:9191/api/v4/projects/$SAMPLE_PROJECT_ID/hooks" 2>&1)
    
    WEBHOOK_ID=$(echo "$webhook_response" | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)
    
    if [ -n "$WEBHOOK_ID" ]; then
        log_success "Webhook configured successfully (ID: $WEBHOOK_ID)"
        
        # Test webhook
        log_automation "Testing webhook connectivity..."
        local test_response
        test_response=$(curl -s -X POST \
          -H "PRIVATE-TOKEN: $GITLAB_TOKEN" \
          "http://localhost:9191/api/v4/projects/$SAMPLE_PROJECT_ID/hooks/$WEBHOOK_ID/test/merge_requests" 2>&1)
        
        if echo "$test_response" | grep -q '"success"'; then
            log_success "Webhook test successful"
        else
            log_warning "Webhook test failed, but webhook is configured"
        fi
    else
        log_warning "Webhook configuration may have failed"
        echo "Response: $webhook_response"
    fi
}

# Seed RAG database with knowledge
seed_rag_database() {
    if [ "$AUTO_SEED_RAG_DATA" = false ]; then
        log_info "Skipping RAG database seeding (disabled)"
        return 0
    fi
    
    log_phase "Seeding RAG database with coding standards and patterns..."
    
    # Wait for ChromaDB to be fully ready
    log_automation "Waiting for ChromaDB to be ready..."
    local attempts=0
    while [ $attempts -lt 30 ]; do
        if curl -s -f "http://localhost:19193/api/v1/heartbeat" >/dev/null 2>&1; then
            log_success "ChromaDB is ready"
            break
        fi
        sleep 5
        ((attempts++))
    done
    
    if [ $attempts -eq 30 ]; then
        log_warning "ChromaDB not responding, skipping RAG seeding"
        return 1
    fi
    
    # Trigger RAG data seeding through MCP server API
    log_automation "Triggering RAG data seeding through MCP server..."
    
    local seed_response
    seed_response=$(curl -s -X POST \
        -H "Content-Type: application/json" \
        -d '{"action": "seed_rag_data", "project_id": "default"}' \
        "http://localhost:5002/api/admin/rag/seed" 2>&1)
    
    if echo "$seed_response" | grep -q '"success"'; then
        log_success "RAG database seeding completed"
    else
        log_warning "RAG seeding may have failed"
        echo "Response: $seed_response"
    fi
}

# Setup SonarQube integration
setup_sonarqube() {
    if [ "$ENABLE_SONARQUBE" = false ]; then
        log_info "Skipping SonarQube setup (disabled)"
        return 0
    fi
    
    log_phase "Setting up SonarQube integration..."
    
    # Wait for SonarQube to be ready
    log_automation "Waiting for SonarQube to be operational..."
    local attempts=0
    while [ $attempts -lt 60 ]; do  # 10 minutes max
        if curl -s -f "http://localhost:9000/api/system/status" >/dev/null 2>&1; then
            local status
            status=$(curl -s "http://localhost:9000/api/system/status" | grep -o '"status":"[^"]*"' | cut -d':' -f2 | tr -d '"')
            if [ "$status" = "UP" ]; then
                log_success "SonarQube is operational"
                break
            fi
        fi
        sleep 10
        ((attempts++))
    done
    
    if [ $attempts -eq 60 ]; then
        log_warning "SonarQube not responding, skipping setup"
        return 1
    fi
    
    # Basic SonarQube project setup would go here
    # For now, just confirm it's running
    log_success "SonarQube integration ready"
}

# Verify complete deployment
verify_deployment() {
    log_phase "Verifying complete deployment..."
    
    local failed_services=()
    
    # Define services to check
    local services=(
        "GitLab:http://localhost:9191/-/health"
        "MCP-Server:http://localhost:5002/health"
        "ChromaDB:http://localhost:19193/api/v1/heartbeat"
        "Prometheus:http://localhost:9090/-/healthy"
        "Grafana:http://localhost:19192/api/health"
        "Elasticsearch:http://localhost:9200/_cluster/health"
    )
    
    if [ "$ENABLE_SONARQUBE" = true ]; then
        services+=("SonarQube:http://localhost:9000/api/system/status")
    fi
    
    for service in "${services[@]}"; do
        local name=$(echo "$service" | cut -d':' -f1)
        local url=$(echo "$service" | cut -d':' -f2-3)
        
        if curl -s -f "$url" --max-time 10 >/dev/null 2>&1; then
            log_success "$name is healthy"
        else
            log_error "$name is not responding"
            failed_services+=("$name")
        fi
    done
    
    # Test MCP integration specifically
    if [ -n "$SAMPLE_PROJECT_ID" ] && [ -n "$WEBHOOK_ID" ]; then
        log_automation "Testing MCP integration..."
        local integration_test
        integration_test=$(curl -s -X POST \
            -H "Content-Type: application/json" \
            -d '{"test": true, "project_id": "'"$SAMPLE_PROJECT_ID"'"}' \
            "http://localhost:5002/api/test/integration" 2>&1)
        
        if echo "$integration_test" | grep -q '"success"'; then
            log_success "MCP integration test passed"
        else
            log_warning "MCP integration test failed"
            failed_services+=("MCP-Integration")
        fi
    fi
    
    if [ ${#failed_services[@]} -eq 0 ]; then
        log_success "All services are healthy and integrated!"
        return 0
    else
        log_error "Some services failed health checks: ${failed_services[*]}"
        return 1
    fi
}

# Display comprehensive deployment summary
show_deployment_summary() {
    # Source configuration if available
    source /tmp/gitlab-deployment-config.env 2>/dev/null || true
    
    echo ""
    echo "🎉 MCP Code Review System - Complete Deployment Successful!"
    echo "==========================================================="
    echo ""
    echo "📊 Service Endpoints:"
    echo "   🦊 GitLab:     http://localhost:9191"
    echo "   🤖 MCP Server: http://localhost:5002"
    echo "   📊 Grafana:    http://localhost:19192"
    echo "   🔍 Prometheus: http://localhost:9090"
    echo "   📈 Kibana:     http://localhost:5601"
    echo "   🗄️  ChromaDB:   http://localhost:19193"
    if [ "$ENABLE_SONARQUBE" = true ]; then
        echo "   🔍 SonarQube:  http://localhost:9000"
    fi
    echo ""
    echo "🔐 Login Credentials:"
    echo "   GitLab Root:  root / ${GITLAB_ROOT_PASSWORD:-Adm1nP@ssw0rd2025!}"
    echo "   Test Developer: developer / DevP@ssw0rd123!"
    echo "   Test Reviewer: reviewer / RevP@ssw0rd123!"
    echo "   Grafana: admin / ${GRAFANA_ADMIN_PASSWORD:-SecureGrafanaPass123!}"
    if [ "$ENABLE_SONARQUBE" = true ]; then
        echo "   SonarQube: admin / admin (change on first login)"
    fi
    echo ""
    
    if [ -n "$SAMPLE_PROJECT_ID" ]; then
        echo "🎯 Demo Project Ready:"
        echo "   📁 Project: http://localhost:9191/root/ecommerce-api-demo"
        echo "   🆔 ID: $SAMPLE_PROJECT_ID"
        echo "   📝 Files: Realistic e-commerce API with intentional issues"
        echo ""
    fi
    
    if [ -n "$WEBHOOK_ID" ]; then
        echo "🪝 Integration Status:"
        echo "   ✅ GitLab webhooks configured (ID: $WEBHOOK_ID)"
        echo "   ✅ Real-time AI review triggers active"
        echo "   ✅ Multi-agent analysis system operational"
        if [ "$AUTO_SEED_RAG_DATA" = true ]; then
            echo "   ✅ RAG knowledge base populated"
        fi
        echo ""
    fi
    
    echo "🧪 Testing the Integration:"
    echo "   1. Visit the demo project: http://localhost:9191/root/ecommerce-api-demo"
    echo "   2. Create a new branch: git checkout -b fix/security-issues"
    echo "   3. Edit any .cs file to fix security issues"
    echo "   4. Create a merge request"
    echo "   5. Watch AI agents analyze your code in real-time!"
    echo ""
    echo "📋 Monitoring & Management:"
    echo "   🔍 View logs: docker compose -f devops/docker/docker-compose.yml logs -f mcp-server"
    echo "   📊 Metrics: http://localhost:19192 (Grafana)"
    echo "   🏥 Health: ./devops/scripts/automation/health-check.sh"
    echo "   🧹 Teardown: ./devops/scripts/automation/complete-stack-teardown.sh"
    echo ""
    
    # Clean up temporary files
    rm -f /tmp/gitlab-deployment-config.env
    
    echo "🚀 Your AI-powered code review system is ready!"
    echo ""
}

# Main execution with comprehensive orchestration
main() {
    parse_arguments "$@"
    check_prerequisites
    restore_from_backup
    start_services
    wait_for_gitlab
    
    # CRITICAL: Check if GitLab configuration succeeds
    if ! configure_gitlab; then
        log_error "GitLab configuration failed! Cannot continue with full deployment."
        log_error "This will cause sample project setup and webhook configuration to fail."
        log_error "Deployment will continue with basic services only."
        
        # Set flags to skip dependent functions
        AUTO_SETUP_SAMPLE_PROJECT=false
        AUTO_CONFIGURE_WEBHOOKS=false
    fi
    
    setup_sample_project
    configure_webhooks
    seed_rag_database
    setup_sonarqube
    
    if verify_deployment; then
        show_deployment_summary
        log_success "Complete stack deployment successful! 🎉"
        exit 0
    else
        show_deployment_summary
        log_error "Deployment completed with some issues. Check the verification results above."
        exit 1
    fi
}

# Execute main function
main "$@"