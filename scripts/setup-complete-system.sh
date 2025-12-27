#!/bin/bash

# Complete System Setup - Infrastructure as Code
# This script automates the entire MCP Code Review system setup

set -e

echo "🚀 Starting Complete MCP Code Review System Setup"
echo "================================================"

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
GITLAB_PORT=9191
CHROMADB_PORT=19193
MCP_PORT=5002

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed. Please install Docker first."
        exit 1
    fi
    
    # Check Docker Compose
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        log_error "Docker Compose is not installed. Please install Docker Compose first."
        exit 1
    fi
    
    # Check .NET
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET 8 SDK is not installed. Please install .NET 8 first."
        exit 1
    fi
    
    log_success "Prerequisites check passed"
}

# Setup environment configuration
setup_environment() {
    log_info "Setting up environment configuration..."
    
    # Create .env file with all required configurations
    cat > "$PROJECT_ROOT/.env" << EOF
# AI Provider Configuration
CLAUDE_API_KEY=\${CLAUDE_API_KEY:-}
OPENAI_API_KEY=\${OPENAI_API_KEY:-}
AI__PreferredProvider=OpenAI

# GitLab Configuration
GITLAB_HOST=http://localhost:${GITLAB_PORT}
GITLAB_TOKEN=glpat-ypaBiHdgSx9DzMQegQdN
GITLAB_ROOT_PASSWORD=Adm1nP@ssw0rd2025!
GITLAB_EXTERNAL_URL=http://localhost:${GITLAB_PORT}

# ChromaDB Configuration
CHROMADB_URL=http://localhost:${CHROMADB_PORT}
CHROMADB_HOST=0.0.0.0
CHROMADB_PORT=${CHROMADB_PORT}

# MCP Server Configuration
MCP_PORT=${MCP_PORT}
MCP_HOST=0.0.0.0

# Database Configuration
POSTGRES_PASSWORD=gitlab_password
POSTGRES_USER=gitlab
POSTGRES_DB=gitlab_production

# Redis Configuration
REDIS_PASSWORD=redis_password

# Logging Configuration
ASPNETCORE_ENVIRONMENT=Development
SERILOG__MINIMUMLEVEL__DEFAULT=Information
EOF

    log_success "Environment configuration created"
}

# Create comprehensive docker-compose
create_docker_compose() {
    log_info "Creating comprehensive docker-compose configuration..."
    
    cat > "$PROJECT_ROOT/docker-compose.yml" << 'EOF'
version: '3.8'

services:
  # PostgreSQL Database for GitLab
  postgresql:
    image: postgres:15-alpine
    container_name: gitlab-postgresql
    restart: always
    environment:
      POSTGRES_DB: ${POSTGRES_DB:-gitlab_production}
      POSTGRES_USER: ${POSTGRES_USER:-gitlab}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-gitlab_password}
    volumes:
      - postgresql_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-gitlab}"]
      interval: 10s
      timeout: 5s
      retries: 5

  # Redis for GitLab
  redis:
    image: redis:7-alpine
    container_name: gitlab-redis
    restart: always
    command: redis-server --requirepass ${REDIS_PASSWORD:-redis_password}
    volumes:
      - redis_data:/data
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "--raw", "incr", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  # GitLab CE
  gitlab:
    image: gitlab/gitlab-ce:16.7.0-ce.0
    container_name: gitlab-ce
    restart: always
    hostname: localhost
    environment:
      GITLAB_OMNIBUS_CONFIG: |
        # External URL
        external_url '${GITLAB_EXTERNAL_URL:-http://localhost:9191}'
        
        # PostgreSQL connection
        postgresql['enable'] = false
        gitlab_rails['db_adapter'] = 'postgresql'
        gitlab_rails['db_encoding'] = 'unicode'
        gitlab_rails['db_host'] = 'postgresql'
        gitlab_rails['db_port'] = 5432
        gitlab_rails['db_database'] = '${POSTGRES_DB:-gitlab_production}'
        gitlab_rails['db_username'] = '${POSTGRES_USER:-gitlab}'
        gitlab_rails['db_password'] = '${POSTGRES_PASSWORD:-gitlab_password}'
        
        # Redis connection
        redis['enable'] = false
        gitlab_rails['redis_host'] = 'redis'
        gitlab_rails['redis_port'] = 6379
        gitlab_rails['redis_password'] = '${REDIS_PASSWORD:-redis_password}'
        
        # GitLab configuration
        gitlab_rails['gitlab_shell_ssh_port'] = 2222
        gitlab_rails['initial_root_password'] = '${GITLAB_ROOT_PASSWORD:-Adm1nP@ssw0rd2025!}'
        
        # Performance settings
        unicorn['worker_processes'] = 2
        postgresql['shared_preload_libraries'] = nil
        
        # Security settings
        gitlab_rails['gitlab_default_projects_features_issues'] = true
        gitlab_rails['gitlab_default_projects_features_merge_requests'] = true
        gitlab_rails['gitlab_default_projects_features_wiki'] = true
        gitlab_rails['gitlab_default_projects_features_snippets'] = true
        gitlab_rails['webhook_timeout'] = 30
    ports:
      - "${GITLAB_PORT:-9191}:80"
      - "2222:22"
    volumes:
      - gitlab_config:/etc/gitlab
      - gitlab_logs:/var/log/gitlab
      - gitlab_data:/var/opt/gitlab
    depends_on:
      postgresql:
        condition: service_healthy
      redis:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost/-/health || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 10
      start_period: 120s

  # ChromaDB Vector Database
  chromadb:
    image: chromadb/chroma:0.4.22
    container_name: chromadb
    restart: always
    environment:
      CHROMA_HOST: ${CHROMADB_HOST:-0.0.0.0}
      CHROMA_PORT: ${CHROMADB_PORT:-19193}
    ports:
      - "${CHROMADB_PORT:-19193}:8000"
    volumes:
      - chromadb_data:/chroma/chroma
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8000/api/v1/heartbeat || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 5

volumes:
  postgresql_data:
    driver: local
  redis_data:
    driver: local
  gitlab_config:
    driver: local
  gitlab_logs:
    driver: local
  gitlab_data:
    driver: local
  chromadb_data:
    driver: local

networks:
  default:
    driver: bridge
EOF

    log_success "Docker Compose configuration created"
}

# Create automated GitLab setup script
create_gitlab_automation() {
    log_info "Creating GitLab automation scripts..."
    
    mkdir -p "$PROJECT_ROOT/scripts/gitlab"
    
    cat > "$PROJECT_ROOT/scripts/gitlab/setup-users-and-projects.sh" << 'EOF'
#!/bin/bash

# Automated GitLab User and Project Setup

set -e

GITLAB_URL="${GITLAB_HOST:-http://localhost:9191}"
ROOT_TOKEN="${GITLAB_TOKEN:-glpat-ypaBiHdgSx9DzMQegQdN}"

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
EOF

    chmod +x "$PROJECT_ROOT/scripts/gitlab/setup-users-and-projects.sh"
    log_success "GitLab automation script created"
}

# Create MCP server configuration automation
create_mcp_automation() {
    log_info "Creating MCP server automation..."
    
    # Create appsettings.Production.json with all required configurations
    cat > "$PROJECT_ROOT/src/Mcp.CodeReview/appsettings.Production.json" << 'EOF'
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    },
    "Console": {
      "TimestampFormat": "yyyy-MM-dd HH:mm:ss "
    }
  },
  "AllowedHosts": "*",
  "Serilog": {
    "Using": ["Serilog.Sinks.Console"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext"]
  },
  "AI": {
    "PreferredProvider": "OpenAI",
    "Claude": {
      "BaseUrl": "https://api.anthropic.com",
      "Model": "claude-3-sonnet-20240229"
    },
    "OpenAI": {
      "BaseUrl": "https://api.openai.com/v1",
      "Model": "gpt-4o-mini"
    }
  },
  "GitHub": {
    "BaseUrl": "https://api.github.com"
  },
  "GitLab": {
    "BaseUrl": "http://localhost:9191",
    "ApiVersion": "v4"
  },
  "ChromaDB": {
    "BaseUrl": "http://localhost:19193"
  },
  "RAG": {
    "CollectionPrefix": "mcp_code_review",
    "MaxResults": 10,
    "SimilarityThreshold": 0.7
  }
}
EOF

    # Create service startup script
    cat > "$PROJECT_ROOT/scripts/start-mcp-server.sh" << 'EOF'
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
EOF

    chmod +x "$PROJECT_ROOT/scripts/start-mcp-server.sh"
    log_success "MCP server automation created"
}

# Create test data automation
create_test_automation() {
    log_info "Creating test data automation..."
    
    cat > "$PROJECT_ROOT/scripts/setup-test-data.sh" << 'EOF'
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
EOF

    chmod +x "$PROJECT_ROOT/scripts/setup-test-data.sh"
    log_success "Test data automation created"
}

# Create main orchestration script
create_main_script() {
    log_info "Creating main orchestration script..."
    
    cat > "$PROJECT_ROOT/start-system.sh" << 'EOF'
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
    docker-compose down -v --remove-orphans 2>/dev/null || true
    docker system prune -f
    log_success "System cleaned"
}

start_infrastructure() {
    log_info "Starting infrastructure services..."
    
    # Start all services
    docker-compose up -d postgresql redis chromadb
    
    # Wait for core services
    log_info "Waiting for PostgreSQL..."
    docker-compose exec -T postgresql pg_isready -U gitlab || sleep 10
    
    log_info "Waiting for ChromaDB..."
    sleep 5
    
    # Start GitLab
    if [ "$SKIP_GITLAB" != "true" ]; then
        log_info "Starting GitLab..."
        docker-compose up -d gitlab
        
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
EOF

    chmod +x "$PROJECT_ROOT/start-system.sh"
    log_success "Main orchestration script created"
}

# Main execution
main() {
    check_prerequisites
    setup_environment
    create_docker_compose
    create_gitlab_automation
    create_mcp_automation
    create_test_automation
    create_main_script
    
    log_success "🎉 Complete Infrastructure as Code setup finished!"
    echo ""
    echo "📋 What was created:"
    echo "==================="
    echo "✅ .env - Environment configuration"
    echo "✅ docker-compose.yml - Complete infrastructure stack"
    echo "✅ scripts/gitlab/setup-users-and-projects.sh - GitLab automation"
    echo "✅ scripts/start-mcp-server.sh - MCP server startup"
    echo "✅ scripts/setup-test-data.sh - Test data automation"
    echo "✅ start-system.sh - Main orchestration script"
    echo "✅ appsettings.Production.json - MCP server configuration"
    echo ""
    echo "🚀 Quick Start:"
    echo "==============="
    echo "1. Set your API keys in .env:"
    echo "   export OPENAI_API_KEY=your_key_here"
    echo "   export CLAUDE_API_KEY=your_key_here"
    echo ""
    echo "2. Start the complete system:"
    echo "   ./start-system.sh"
    echo ""
    echo "3. Or start individual components:"
    echo "   ./start-system.sh --no-gitlab  # MCP only"
    echo "   ./start-system.sh --clean      # Fresh start"
    echo ""
    log_success "System is now fully automated and repeatable! 🎉"
}

main "$@"