#!/bin/bash
set -e

echo "🎛️  MCP Code Review System - Stack Orchestrator"
echo "=============================================="

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
log_orchestrator() { echo -e "${CYAN}🎛️  $1${NC}"; }

# Configuration
OPERATION=""
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"

show_usage() {
    echo ""
    echo "📋 Available Operations:"
    echo ""
    echo "🚀 Deployment Operations:"
    echo "   deploy           : Complete stack deployment with full automation"
    echo "   deploy-quick     : Quick deployment without sample project setup"
    echo "   redeploy         : Teardown and redeploy (preserves volumes)"
    echo "   restore <dir>    : Restore from backup directory"
    echo ""
    echo "🧹 Management Operations:"
    echo "   teardown         : Graceful teardown (preserves data)"
    echo "   teardown-full    : Complete teardown (removes all data)"
    echo "   teardown-force   : Force teardown (no confirmations)"
    echo ""
    echo "🔧 Maintenance Operations:"
    echo "   health           : Check system health"
    echo "   status           : Show detailed system status"
    echo "   logs             : Show service logs"
    echo "   backup           : Create backup of critical data"
    echo "   update           : Update services (pull latest images)"
    echo ""
    echo "🧪 Testing Operations:"
    echo "   test-integration : Test GitLab + MCP integration"
    echo "   test-webhooks    : Test webhook functionality"
    echo "   test-ai          : Test AI agent collaboration"
    echo ""
    echo "📋 Information:"
    echo "   info             : Show deployment information"
    echo "   help             : Show this help message"
    echo ""
    echo "🔧 Advanced Options:"
    echo "   --force          : Skip confirmations"
    echo "   --keep-volumes   : Preserve data volumes during teardown"
    echo "   --no-backup      : Skip backup creation"
    echo "   --verbose        : Enable verbose output"
    echo ""
    echo "💡 Examples:"
    echo "   ./stack-orchestrator.sh deploy"
    echo "   ./stack-orchestrator.sh teardown --keep-volumes"
    echo "   ./stack-orchestrator.sh redeploy"
    echo "   ./stack-orchestrator.sh restore ./backups/20231217_142530"
    echo ""
}

# Parse command line arguments
parse_arguments() {
    if [ $# -eq 0 ]; then
        echo "❌ No operation specified"
        show_usage
        exit 1
    fi
    
    OPERATION="$1"
    shift
    
    # Pass remaining arguments to the specific scripts
    REMAINING_ARGS="$@"
}

# Execute deployment operations
execute_deploy() {
    log_orchestrator "Executing deployment operation: $1"
    
    case "$1" in
        "deploy")
            log_phase "Starting complete stack deployment with full automation..."
            "$SCRIPT_DIR/complete-stack-deploy.sh" $REMAINING_ARGS
            ;;
        "deploy-quick")
            log_phase "Starting quick deployment without sample project..."
            "$SCRIPT_DIR/complete-stack-deploy.sh" --no-sample-project --no-rag-seeding $REMAINING_ARGS
            ;;
        "redeploy")
            log_phase "Starting redeploy operation (teardown + deploy)..."
            log_orchestrator "Phase 1: Tearing down existing stack..."
            "$SCRIPT_DIR/complete-stack-teardown.sh" --keep-volumes --force
            
            log_orchestrator "Phase 2: Deploying fresh stack..."
            "$SCRIPT_DIR/complete-stack-deploy.sh" $REMAINING_ARGS
            ;;
        "restore")
            if [ -z "$2" ]; then
                log_error "Backup directory required for restore operation"
                echo "Usage: $0 restore <backup_directory>"
                exit 1
            fi
            
            BACKUP_DIR="$2"
            shift 2  # Remove restore and backup_dir from args
            
            log_phase "Starting restore operation from: $BACKUP_DIR"
            "$SCRIPT_DIR/complete-stack-deploy.sh" --restore-backup "$BACKUP_DIR" $@
            ;;
        *)
            log_error "Unknown deployment operation: $1"
            exit 1
            ;;
    esac
}

# Execute teardown operations  
execute_teardown() {
    log_orchestrator "Executing teardown operation: $1"
    
    case "$1" in
        "teardown")
            log_phase "Starting graceful teardown (preserving data)..."
            "$SCRIPT_DIR/complete-stack-teardown.sh" --keep-volumes $REMAINING_ARGS
            ;;
        "teardown-full")
            log_phase "Starting complete teardown (removing all data)..."
            "$SCRIPT_DIR/complete-stack-teardown.sh" $REMAINING_ARGS
            ;;
        "teardown-force")
            log_phase "Starting force teardown..."
            "$SCRIPT_DIR/complete-stack-teardown.sh" --force $REMAINING_ARGS
            ;;
        *)
            log_error "Unknown teardown operation: $1"
            exit 1
            ;;
    esac
}

# Execute maintenance operations
execute_maintenance() {
    log_orchestrator "Executing maintenance operation: $1"
    
    case "$1" in
        "health")
            log_phase "Checking system health..."
            "$SCRIPT_DIR/health-check.sh" $REMAINING_ARGS
            ;;
        "status")
            show_system_status
            ;;
        "logs")
            show_service_logs
            ;;
        "backup")
            create_manual_backup
            ;;
        "update")
            update_services
            ;;
        *)
            log_error "Unknown maintenance operation: $1"
            exit 1
            ;;
    esac
}

# Execute testing operations
execute_testing() {
    log_orchestrator "Executing testing operation: $1"
    
    case "$1" in
        "test-integration")
            test_integration
            ;;
        "test-webhooks")
            test_webhooks
            ;;
        "test-ai")
            test_ai_agents
            ;;
        *)
            log_error "Unknown testing operation: $1"
            exit 1
            ;;
    esac
}

# Show detailed system status
show_system_status() {
    log_phase "Gathering system status..."
    
    echo ""
    echo "🐳 Docker Containers:"
    echo "===================="
    docker ps --filter "name=mcp-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>/dev/null || echo "No MCP containers found"
    
    echo ""
    echo "💾 Docker Volumes:"
    echo "=================="
    docker volume ls | grep -E "(mcp|gitlab|chromadb|elasticsearch|prometheus|grafana|sonar)" || echo "No MCP volumes found"
    
    echo ""
    echo "🌐 Docker Networks:"
    echo "==================="
    docker network ls | grep mcp || echo "No MCP networks found"
    
    echo ""
    echo "🏥 Service Health:"
    echo "=================="
    
    # Check key services
    local services=(
        "GitLab:http://localhost:9191/-/health"
        "MCP-Server:http://localhost:5002/health"
        "ChromaDB:http://localhost:19193/api/v1/heartbeat"
        "Grafana:http://localhost:19192/api/health"
        "Prometheus:http://localhost:9090/-/healthy"
    )
    
    for service in "${services[@]}"; do
        local name=$(echo "$service" | cut -d':' -f1)
        local url=$(echo "$service" | cut -d':' -f2-3)
        
        if curl -s -f "$url" --max-time 5 >/dev/null 2>&1; then
            echo "   ✅ $name: Healthy"
        else
            echo "   ❌ $name: Not responding"
        fi
    done
    
    echo ""
    echo "💽 Disk Usage:"
    echo "=============="
    echo "Project directory: $(du -sh . 2>/dev/null || echo "Unknown")"
    echo "Docker system: $(docker system df 2>/dev/null | tail -n +2 || echo "Unknown")"
}

# Show service logs
show_service_logs() {
    local service="${2:-mcp-server}"
    local lines="${3:-50}"
    
    log_phase "Showing logs for $service (last $lines lines)..."
    
    cd "$PROJECT_ROOT/devops/docker" 2>/dev/null || {
        log_error "Could not find docker-compose.yml"
        return 1
    }
    
    if [ "$service" = "all" ]; then
        docker compose logs --tail="$lines" -f
    else
        docker compose logs --tail="$lines" -f "$service" 2>/dev/null || {
            log_error "Service $service not found"
            echo "Available services:"
            docker compose ps --services
            return 1
        }
    fi
    
    cd - > /dev/null
}

# Create manual backup
create_manual_backup() {
    log_phase "Creating manual backup..."
    
    local backup_dir="./backups/manual_$(date +%Y%m%d_%H%M%S)"
    mkdir -p "$backup_dir"
    
    # Use the backup logic from teardown script
    if docker ps --filter "name=mcp-gitlab" --format "{{.Names}}" | grep -q "mcp-gitlab"; then
        log_info "Creating GitLab backup..."
        docker exec mcp-gitlab gitlab-backup create BACKUP=manual_$(date +%Y%m%d_%H%M%S) 2>/dev/null || log_warning "GitLab backup failed"
    fi
    
    # Backup environment
    if [ -f ".env" ]; then
        cp .env "$backup_dir/env.backup"
    fi
    
    log_success "Manual backup created: $backup_dir"
}

# Update services
update_services() {
    log_phase "Updating services..."
    
    cd "$PROJECT_ROOT/devops/docker" 2>/dev/null || {
        log_error "Could not find docker-compose.yml"
        return 1
    }
    
    log_info "Pulling latest images..."
    docker compose pull
    
    log_info "Rebuilding custom images..."
    docker compose build --pull
    
    log_success "Services updated. Use 'redeploy' to apply changes."
    
    cd - > /dev/null
}

# Test integration
test_integration() {
    log_phase "Testing GitLab + MCP integration..."
    
    # Check if services are running
    local required_services=("mcp-gitlab" "mcp-code-review" "mcp-chromadb")
    local missing_services=()
    
    for service in "${required_services[@]}"; do
        if ! docker ps --filter "name=$service" --format "{{.Names}}" | grep -q "$service"; then
            missing_services+=("$service")
        fi
    done
    
    if [ ${#missing_services[@]} -gt 0 ]; then
        log_error "Required services not running: ${missing_services[*]}"
        log_info "Start the stack first: $0 deploy"
        return 1
    fi
    
    # Test MCP server health
    log_info "Testing MCP server health..."
    if curl -s -f "http://localhost:5002/health" >/dev/null 2>&1; then
        log_success "MCP server is healthy"
    else
        log_error "MCP server health check failed"
        return 1
    fi
    
    # Test GitLab connectivity
    log_info "Testing GitLab connectivity..."
    if curl -s -f "http://localhost:9191/-/health" >/dev/null 2>&1; then
        log_success "GitLab is accessible"
    else
        log_error "GitLab connectivity test failed"
        return 1
    fi
    
    log_success "Integration test completed successfully"
}

# Test webhooks
test_webhooks() {
    log_phase "Testing webhook functionality..."
    
    # This would involve creating a test commit and checking if webhook fires
    log_info "Webhook testing requires manual verification"
    log_info "1. Create a merge request in GitLab"
    log_info "2. Check MCP server logs for webhook receipt"
    log_info "3. Verify AI analysis appears in the merge request"
    
    log_success "Webhook test instructions provided"
}

# Test AI agents
test_ai_agents() {
    log_phase "Testing AI agent collaboration..."
    
    # Send a test request to the MCP server
    log_info "Sending test code to AI agents..."
    
    local test_response
    test_response=$(curl -s -X POST \
        -H "Content-Type: application/json" \
        -d '{
            "content": "public class Test { public void BadMethod(string sql) { var query = \"SELECT * FROM users WHERE id = \" + sql; } }",
            "fileName": "Test.cs",
            "language": "csharp"
        }' \
        "http://localhost:5002/api/review" 2>&1)
    
    if echo "$test_response" | grep -q '"qualityScore"'; then
        local quality_score
        quality_score=$(echo "$test_response" | grep -o '"qualityScore":[0-9]*' | cut -d':' -f2)
        log_success "AI agents responded successfully (Quality Score: $quality_score)"
    else
        log_error "AI agent test failed"
        echo "Response: $test_response"
        return 1
    fi
}

# Show deployment information
show_deployment_info() {
    echo ""
    echo "📋 MCP Code Review System - Deployment Information"
    echo "=================================================="
    echo ""
    echo "📁 Project Structure:"
    echo "   Root: $PROJECT_ROOT"
    echo "   Scripts: $SCRIPT_DIR"
    echo "   Config: $PROJECT_ROOT/devops/config"
    echo "   Docker: $PROJECT_ROOT/devops/docker"
    echo ""
    echo "🔧 Available Scripts:"
    echo "   📦 Deploy: $SCRIPT_DIR/complete-stack-deploy.sh"
    echo "   🧹 Teardown: $SCRIPT_DIR/complete-stack-teardown.sh"
    echo "   🏥 Health: $SCRIPT_DIR/health-check.sh"
    echo "   🎛️  Orchestrator: $SCRIPT_DIR/stack-orchestrator.sh (this script)"
    echo ""
    echo "🐳 Docker Configuration:"
    echo "   Compose File: $PROJECT_ROOT/devops/docker/docker-compose.yml"
    echo "   Data Directory: $PROJECT_ROOT/devops/data"
    echo "   Logs Directory: $PROJECT_ROOT/devops/data/logs"
    echo ""
    
    show_system_status
}

# Main execution
main() {
    # Change to project root
    cd "$PROJECT_ROOT"
    
    parse_arguments "$@"
    
    case "$OPERATION" in
        # Deployment operations
        "deploy"|"deploy-quick"|"redeploy"|"restore")
            execute_deploy "$OPERATION" $REMAINING_ARGS
            ;;
        
        # Teardown operations
        "teardown"|"teardown-full"|"teardown-force")
            execute_teardown "$OPERATION"
            ;;
        
        # Maintenance operations
        "health"|"status"|"logs"|"backup"|"update")
            execute_maintenance "$OPERATION"
            ;;
        
        # Testing operations
        "test-integration"|"test-webhooks"|"test-ai")
            execute_testing "$OPERATION"
            ;;
        
        # Information operations
        "info")
            show_deployment_info
            ;;
        
        "help"|"--help"|"-h")
            show_usage
            ;;
        
        *)
            log_error "Unknown operation: $OPERATION"
            show_usage
            exit 1
            ;;
    esac
}

# Execute main function
main "$@"