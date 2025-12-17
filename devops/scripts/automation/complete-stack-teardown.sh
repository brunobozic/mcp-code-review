#!/bin/bash
set -e

echo "🧹 MCP Code Review System - Complete Stack Teardown"
echo "===================================================="

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
NC='\033[0m' # No Color

log_info() { echo -e "${BLUE}ℹ️  $1${NC}"; }
log_success() { echo -e "${GREEN}✅ $1${NC}"; }
log_warning() { echo -e "${YELLOW}⚠️  $1${NC}"; }
log_error() { echo -e "${RED}❌ $1${NC}"; }
log_phase() { echo -e "${PURPLE}🔄 $1${NC}"; }

# Configuration
FORCE_MODE=${1:-false}
BACKUP_DATA=${BACKUP_DATA:-true}
KEEP_VOLUMES=${KEEP_VOLUMES:-false}

show_teardown_options() {
    echo ""
    echo "📋 Teardown Options:"
    echo "   --force         : Skip confirmations and force teardown"
    echo "   --keep-volumes  : Preserve data volumes (GitLab, databases)"
    echo "   --no-backup     : Skip data backup before teardown"
    echo ""
    echo "Environment Variables:"
    echo "   BACKUP_DATA=${BACKUP_DATA}     : Backup data before teardown"
    echo "   KEEP_VOLUMES=${KEEP_VOLUMES}    : Keep Docker volumes"
    echo ""
}

# Parse command line arguments
parse_arguments() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --force)
                FORCE_MODE=true
                shift
                ;;
            --keep-volumes)
                KEEP_VOLUMES=true
                shift
                ;;
            --no-backup)
                BACKUP_DATA=false
                shift
                ;;
            --help)
                show_teardown_options
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                show_teardown_options
                exit 1
                ;;
        esac
    done
}

# Confirm teardown
confirm_teardown() {
    if [ "$FORCE_MODE" = true ]; then
        log_warning "Force mode enabled - skipping confirmation"
        return 0
    fi
    
    echo ""
    echo "⚠️  WARNING: This will completely tear down the MCP Code Review stack:"
    echo ""
    echo "🗑️  Will be destroyed:"
    echo "   • All running containers (GitLab, MCP Server, monitoring)"
    echo "   • All container networks"
    echo "   • All temporary data and logs"
    if [ "$KEEP_VOLUMES" = false ]; then
        echo "   • All Docker volumes (databases, persistent data)"
    fi
    echo ""
    echo "💾 Will be preserved:"
    if [ "$KEEP_VOLUMES" = true ]; then
        echo "   • Docker volumes (can be reused in next deployment)"
    fi
    if [ "$BACKUP_DATA" = true ]; then
        echo "   • Backup files in ./backups/"
    fi
    echo "   • Source code and configuration files"
    echo ""
    
    read -p "Are you sure you want to continue? (yes/NO): " -r
    if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
        echo "Teardown cancelled."
        exit 0
    fi
}

# Create backup of critical data
backup_critical_data() {
    if [ "$BACKUP_DATA" = false ]; then
        log_info "Skipping data backup (--no-backup specified)"
        return 0
    fi
    
    log_phase "Creating backup of critical data..."
    
    BACKUP_DIR="./backups/$(date +%Y%m%d_%H%M%S)"
    mkdir -p "$BACKUP_DIR"
    
    # Backup GitLab data
    if docker ps --filter "name=mcp-gitlab" --format "{{.Names}}" | grep -q "mcp-gitlab"; then
        log_info "Backing up GitLab configuration and repositories..."
        
        # Backup GitLab secrets and configuration
        docker cp mcp-gitlab:/etc/gitlab/gitlab-secrets.json "$BACKUP_DIR/gitlab-secrets.json" 2>/dev/null || log_warning "Could not backup gitlab-secrets.json"
        docker cp mcp-gitlab:/etc/gitlab/gitlab.rb "$BACKUP_DIR/gitlab.rb" 2>/dev/null || log_warning "Could not backup gitlab.rb"
        
        # Create GitLab backup using built-in backup system
        log_info "Creating GitLab application backup (this may take a few minutes)..."
        docker exec mcp-gitlab gitlab-backup create BACKUP=automated_teardown 2>/dev/null || log_warning "GitLab backup failed"
        
        # Copy the backup file
        BACKUP_FILE=$(docker exec mcp-gitlab find /var/opt/gitlab/backups -name "*automated_teardown*" -type f | head -1)
        if [ -n "$BACKUP_FILE" ]; then
            docker cp mcp-gitlab:"$BACKUP_FILE" "$BACKUP_DIR/" 2>/dev/null || log_warning "Could not copy GitLab backup file"
        fi
    fi
    
    # Backup ChromaDB collections
    if docker ps --filter "name=mcp-chromadb" --format "{{.Names}}" | grep -q "mcp-chromadb"; then
        log_info "Backing up ChromaDB vector database..."
        docker exec mcp-chromadb tar -czf /tmp/chromadb-backup.tar.gz /chroma/chroma 2>/dev/null || log_warning "ChromaDB backup failed"
        docker cp mcp-chromadb:/tmp/chromadb-backup.tar.gz "$BACKUP_DIR/" 2>/dev/null || log_warning "Could not copy ChromaDB backup"
    fi
    
    # Backup environment configuration
    if [ -f ".env" ]; then
        cp .env "$BACKUP_DIR/env.backup" || log_warning "Could not backup .env file"
    fi
    
    # Backup custom configurations
    if [ -d "devops/config" ]; then
        tar -czf "$BACKUP_DIR/devops-config.tar.gz" devops/config/ 2>/dev/null || log_warning "Could not backup devops config"
    fi
    
    # Create restore instructions
    cat > "$BACKUP_DIR/RESTORE_INSTRUCTIONS.md" << 'EOF'
# MCP Code Review Stack Restore Instructions

This backup was created automatically during stack teardown.

## Restore Process

1. **Restore environment configuration:**
   ```bash
   cp env.backup .env
   ```

2. **Restore devops configuration:**
   ```bash
   tar -xzf devops-config.tar.gz -C .
   ```

3. **Deploy the stack normally:**
   ```bash
   ./devops/scripts/automation/deploy-full-stack.sh
   ```

4. **Restore GitLab data (optional):**
   ```bash
   # Copy backup file to GitLab container
   docker cp *.tar.gz mcp-gitlab:/var/opt/gitlab/backups/
   
   # Restore GitLab
   docker exec -it mcp-gitlab gitlab-backup restore BACKUP=<backup_timestamp>
   ```

5. **Restore ChromaDB data (optional):**
   ```bash
   # Copy backup to ChromaDB container
   docker cp chromadb-backup.tar.gz mcp-chromadb:/tmp/
   
   # Extract data
   docker exec mcp-chromadb tar -xzf /tmp/chromadb-backup.tar.gz -C /
   ```

## Configuration Files Included

- `env.backup` - Environment variables and API keys
- `gitlab-secrets.json` - GitLab secret keys and tokens  
- `gitlab.rb` - GitLab configuration
- `devops-config.tar.gz` - DevOps configuration files
- GitLab application backup (if created successfully)
- ChromaDB vector database backup
EOF

    log_success "Backup created in: $BACKUP_DIR"
    echo "   📄 Restore instructions: $BACKUP_DIR/RESTORE_INSTRUCTIONS.md"
}

# Stop all services gracefully
stop_services() {
    log_phase "Stopping all MCP services gracefully..."
    
    # Change to devops/docker directory where docker-compose.yml is located
    cd devops/docker 2>/dev/null || {
        log_error "Could not find devops/docker directory with docker-compose.yml"
        log_info "Trying alternative approaches to stop containers..."
        
        # Fallback: Stop containers by name pattern
        CONTAINERS=$(docker ps --filter "name=mcp-" --format "{{.Names}}" | head -20)
        if [ -n "$CONTAINERS" ]; then
            log_info "Stopping containers: $CONTAINERS"
            echo "$CONTAINERS" | xargs docker stop 2>/dev/null || log_warning "Some containers could not be stopped gracefully"
        fi
        
        return 0
    }
    
    # Stop services in reverse dependency order
    log_info "Stopping application services..."
    docker compose stop mcp-server kibana grafana fluentd 2>/dev/null || log_warning "Some application services failed to stop"
    
    log_info "Stopping GitLab and related services..."
    docker compose stop gitlab gitlab-redis gitlab-postgres 2>/dev/null || log_warning "GitLab services failed to stop"
    
    log_info "Stopping monitoring and databases..."
    docker compose stop prometheus elasticsearch chromadb sonarqube sonar-postgres 2>/dev/null || log_warning "Monitoring services failed to stop"
    
    # Return to original directory
    cd - > /dev/null
    
    log_success "Services stopped gracefully"
}

# Remove containers
remove_containers() {
    log_phase "Removing all containers..."
    
    # Change to devops/docker directory
    cd devops/docker 2>/dev/null || {
        log_warning "Could not find docker-compose.yml, using manual container removal"
        
        # Manual removal by name pattern
        CONTAINERS=$(docker ps -a --filter "name=mcp-" --format "{{.Names}}" | head -20)
        if [ -n "$CONTAINERS" ]; then
            log_info "Removing containers: $CONTAINERS"
            echo "$CONTAINERS" | xargs docker rm -f 2>/dev/null || log_warning "Some containers could not be removed"
        fi
        
        return 0
    }
    
    # Remove containers using docker-compose
    docker compose down --remove-orphans 2>/dev/null || log_warning "docker-compose down failed"
    
    # Return to original directory
    cd - > /dev/null
    
    # Additional cleanup for any missed containers
    REMAINING_CONTAINERS=$(docker ps -a --filter "name=mcp-" --format "{{.Names}}")
    if [ -n "$REMAINING_CONTAINERS" ]; then
        log_info "Removing remaining containers: $REMAINING_CONTAINERS"
        echo "$REMAINING_CONTAINERS" | xargs docker rm -f 2>/dev/null || log_warning "Some remaining containers could not be removed"
    fi
    
    log_success "Containers removed"
}

# Remove Docker volumes
remove_volumes() {
    if [ "$KEEP_VOLUMES" = true ]; then
        log_info "Keeping Docker volumes (--keep-volumes specified)"
        return 0
    fi
    
    log_phase "Removing Docker volumes..."
    
    # List MCP-related volumes
    VOLUMES=$(docker volume ls --filter "name=mcp" --format "{{.Name}}")
    
    # Also check for volumes from docker-compose
    COMPOSE_VOLUMES=$(docker volume ls | grep -E "(gitlab|chromadb|elasticsearch|prometheus|grafana|sonar)" | awk '{print $2}' | head -20)
    
    # Combine and deduplicate
    ALL_VOLUMES=$(echo -e "$VOLUMES\n$COMPOSE_VOLUMES" | sort | uniq | grep -v "^$")
    
    if [ -n "$ALL_VOLUMES" ]; then
        log_info "Removing volumes: $(echo "$ALL_VOLUMES" | tr '\n' ' ')"
        echo "$ALL_VOLUMES" | xargs docker volume rm 2>/dev/null || log_warning "Some volumes could not be removed"
    else
        log_info "No MCP-related volumes found to remove"
    fi
    
    log_success "Volumes cleanup completed"
}

# Remove networks
remove_networks() {
    log_phase "Removing Docker networks..."
    
    # Remove MCP-specific networks
    NETWORKS=$(docker network ls --filter "name=mcp" --format "{{.Name}}")
    
    if [ -n "$NETWORKS" ]; then
        log_info "Removing networks: $NETWORKS"
        echo "$NETWORKS" | xargs docker network rm 2>/dev/null || log_warning "Some networks could not be removed"
    else
        log_info "No MCP-related networks found"
    fi
    
    log_success "Networks cleanup completed"
}

# Clean up temporary files and logs
cleanup_files() {
    log_phase "Cleaning up temporary files and logs..."
    
    # Clean up logs
    if [ -d "logs" ]; then
        log_info "Removing log files..."
        rm -rf logs/* 2>/dev/null || log_warning "Some log files could not be removed"
    fi
    
    # Clean up temporary data
    TEMP_DIRS=(
        "devops/data/logs"
        "tmp"
        "/tmp/gitlab-*"
        "/tmp/mcp-*"
        "playwright-report"
        "test-results"
    )
    
    for dir in "${TEMP_DIRS[@]}"; do
        if [ -d "$dir" ] || [ -f "$dir" ]; then
            log_info "Cleaning up: $dir"
            rm -rf "$dir" 2>/dev/null || log_warning "Could not remove $dir"
        fi
    done
    
    # Clean up test artifacts
    find . -name "cookies.txt" -type f -delete 2>/dev/null || true
    find . -name "*test-cookies*" -type f -delete 2>/dev/null || true
    
    log_success "File cleanup completed"
}

# System cleanup (optional)
system_cleanup() {
    log_phase "Performing Docker system cleanup..."
    
    log_info "Removing unused Docker images..."
    docker image prune -f 2>/dev/null || log_warning "Image cleanup failed"
    
    log_info "Removing unused Docker build cache..."
    docker builder prune -f 2>/dev/null || log_warning "Build cache cleanup failed"
    
    log_info "Removing unused Docker system resources..."
    docker system prune -f 2>/dev/null || log_warning "System cleanup failed"
    
    log_success "Docker system cleanup completed"
}

# Verify complete teardown
verify_teardown() {
    log_phase "Verifying complete teardown..."
    
    local failed_cleanup=()
    
    # Check for remaining containers
    REMAINING_CONTAINERS=$(docker ps -a --filter "name=mcp-" --format "{{.Names}}")
    if [ -n "$REMAINING_CONTAINERS" ]; then
        log_warning "Remaining containers found: $REMAINING_CONTAINERS"
        failed_cleanup+=("containers")
    else
        log_success "No containers remaining"
    fi
    
    # Check for remaining volumes (if they should be removed)
    if [ "$KEEP_VOLUMES" = false ]; then
        REMAINING_VOLUMES=$(docker volume ls --filter "name=mcp" --format "{{.Name}}")
        if [ -n "$REMAINING_VOLUMES" ]; then
            log_warning "Remaining volumes found: $REMAINING_VOLUMES"
            failed_cleanup+=("volumes")
        else
            log_success "No volumes remaining"
        fi
    fi
    
    # Check for remaining networks
    REMAINING_NETWORKS=$(docker network ls --filter "name=mcp" --format "{{.Name}}")
    if [ -n "$REMAINING_NETWORKS" ]; then
        log_warning "Remaining networks found: $REMAINING_NETWORKS"
        failed_cleanup+=("networks")
    else
        log_success "No networks remaining"
    fi
    
    if [ ${#failed_cleanup[@]} -eq 0 ]; then
        log_success "Teardown verification passed!"
        return 0
    else
        log_error "Teardown verification failed for: ${failed_cleanup[*]}"
        return 1
    fi
}

# Display teardown summary
show_summary() {
    echo ""
    echo "🧹 MCP Code Review System Teardown Complete!"
    echo "============================================="
    echo ""
    echo "📋 What was removed:"
    echo "   ✅ All MCP containers (GitLab, databases, monitoring)"
    echo "   ✅ Container networks"
    echo "   ✅ Temporary files and logs"
    
    if [ "$KEEP_VOLUMES" = false ]; then
        echo "   ✅ Docker volumes (databases, persistent data)"
    else
        echo "   ⏸️  Docker volumes (preserved for reuse)"
    fi
    
    if [ "$BACKUP_DATA" = true ] && [ -d "backups" ]; then
        echo ""
        echo "💾 Data backups preserved:"
        echo "   📁 Backup directory: ./backups/"
        echo "   📄 Latest backup: $(ls -t backups/ | head -1)"
        echo "   📋 Restore instructions included in backup"
    fi
    
    echo ""
    echo "🚀 To recreate the stack:"
    echo "   ./devops/scripts/automation/deploy-full-stack.sh"
    echo ""
    
    if [ "$KEEP_VOLUMES" = true ]; then
        echo "💡 Since volumes were preserved, the next deployment will be faster!"
        echo ""
    fi
    
    echo "📊 Docker resource usage after cleanup:"
    echo "   🐳 Containers: $(docker ps -a | wc -l) total"
    echo "   💾 Volumes: $(docker volume ls | wc -l) total"
    echo "   🌐 Networks: $(docker network ls | wc -l) total"
    echo "   💽 Images: $(docker images | wc -l) total"
}

# Main execution
main() {
    parse_arguments "$@"
    confirm_teardown
    backup_critical_data
    stop_services
    remove_containers
    remove_volumes
    remove_networks
    cleanup_files
    system_cleanup
    
    if verify_teardown; then
        show_summary
        log_success "Complete stack teardown successful! 🎉"
        exit 0
    else
        show_summary
        log_error "Teardown completed with some issues. Check the verification results above."
        exit 1
    fi
}

# Execute main function
main "$@"