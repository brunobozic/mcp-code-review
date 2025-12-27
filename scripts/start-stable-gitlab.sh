#!/bin/bash

# Stable GitLab Setup Script - Addresses Frequent Crashes
# Fixes: Resource limits, deprecated configs, dependency issues

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

analyze_crash_causes() {
    log_info "Analyzing GitLab crash causes..."
    
    echo "🔍 **ROOT CAUSES OF GITLAB CRASHES:**"
    echo ""
    echo "1️⃣ **DEPRECATED CONFIGURATION:**"
    echo "   ❌ unicorn['worker_processes'] = removed in v14.0"
    echo "   ✅ Fixed to: puma['worker_processes'] = 2"
    echo ""
    echo "2️⃣ **RESOURCE EXHAUSTION:**"
    echo "   ❌ No memory/CPU limits = crashes under load"
    echo "   ✅ Added: memory: 4G, cpus: 2, proper ulimits"
    echo ""
    echo "3️⃣ **HEAVY FEATURES ENABLED:**"
    echo "   ❌ Prometheus, Grafana, Registry, Pages all enabled"
    echo "   ✅ Disabled non-essential features"
    echo ""
    echo "4️⃣ **POOR HEALTH CHECK TIMING:**"
    echo "   ❌ Too frequent checks during startup"
    echo "   ✅ Extended startup period, better intervals"
    echo ""
    echo "5️⃣ **VERSION COMPATIBILITY:**"
    echo "   ❌ Old GitLab CE 16.7.0 with deprecated features"
    echo "   ✅ Upgraded to 17.7.0 with modern config"
}

cleanup_old_gitlab() {
    log_info "Cleaning up old GitLab containers..."
    
    # Stop and remove all GitLab containers
    docker stop gitlab-ce gitlab-postgresql gitlab-redis chromadb 2>/dev/null || true
    docker rm gitlab-ce gitlab-postgresql gitlab-redis chromadb 2>/dev/null || true
    
    # Clean up volumes if requested
    if [ "$1" = "--clean-volumes" ]; then
        log_warning "Removing GitLab data volumes..."
        docker volume rm mcp-code-review_gitlab_config mcp-code-review_gitlab_data mcp-code-review_gitlab_logs 2>/dev/null || true
        docker volume rm mcp-code-review_postgresql_data mcp-code-review_redis_data 2>/dev/null || true
    fi
    
    log_success "Old containers cleaned up"
}

start_stable_gitlab() {
    log_info "Starting GitLab with stable configuration..."
    
    cd "$PROJECT_ROOT"
    
    # Start dependencies first
    log_info "Starting PostgreSQL and Redis..."
    docker compose up -d postgresql redis
    
    # Wait for dependencies
    log_info "Waiting for dependencies to be ready..."
    sleep 20
    
    # Start ChromaDB (optional)
    log_info "Starting ChromaDB..."
    docker compose up -d chromadb || log_warning "ChromaDB failed to start (continuing without it)"
    
    # Start GitLab with better configuration
    log_info "Starting GitLab CE with optimized settings..."
    docker compose up -d gitlab
    
    log_success "GitLab started with stable configuration"
}

monitor_startup() {
    log_info "Monitoring GitLab startup (this takes 3-5 minutes)..."
    
    local max_attempts=20
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        # Check container status
        local container_status=$(docker inspect gitlab-ce --format='{{.State.Status}}' 2>/dev/null || echo "not found")
        
        if [ "$container_status" = "running" ]; then
            log_info "Attempt $attempt/$max_attempts - Container running, checking health..."
            
            # Check GitLab health
            if curl -s -f "http://localhost:9191/-/health" >/dev/null 2>&1; then
                log_success "GitLab is ready and healthy!"
                return 0
            fi
        elif [ "$container_status" = "exited" ]; then
            log_error "GitLab container exited! Checking logs..."
            docker logs gitlab-ce --tail 10
            return 1
        fi
        
        log_info "Attempt $attempt/$max_attempts - Still starting (status: $container_status)..."
        sleep 15
        ((attempt++))
    done
    
    log_error "GitLab failed to start after $((max_attempts * 15)) seconds"
    log_info "Container logs:"
    docker logs gitlab-ce --tail 20
    return 1
}

show_status() {
    echo ""
    echo "🎯 **GITLAB STABILITY STATUS**"
    echo "=============================="
    echo ""
    
    # Container status
    local gitlab_status=$(docker inspect gitlab-ce --format='{{.State.Status}}' 2>/dev/null || echo "not found")
    local postgres_status=$(docker inspect gitlab-postgresql --format='{{.State.Status}}' 2>/dev/null || echo "not found")
    local redis_status=$(docker inspect gitlab-redis --format='{{.State.Status}}' 2>/dev/null || echo "not found")
    
    echo "📦 **Containers:**"
    echo "   GitLab CE: $gitlab_status"
    echo "   PostgreSQL: $postgres_status"
    echo "   Redis: $redis_status"
    echo ""
    
    # Health checks
    echo "🏥 **Health Checks:**"
    if curl -s -f "http://localhost:9191/-/health" >/dev/null 2>&1; then
        echo "   ✅ GitLab Web: Healthy"
    else
        echo "   ❌ GitLab Web: Unhealthy"
    fi
    echo ""
    
    # Resource usage
    echo "📊 **Resource Usage:**"
    docker stats --no-stream --format "   {{.Container}}: CPU {{.CPUPerc}}, Memory {{.MemUsage}}" | grep -E "(gitlab|postgres|redis)" || echo "   No resource data available"
    echo ""
    
    # Configuration improvements
    echo "⚙️ **Stability Improvements Applied:**"
    echo "   ✅ Updated to GitLab CE 17.7.0"
    echo "   ✅ Fixed deprecated unicorn → puma configuration"
    echo "   ✅ Added memory limits (4G) and CPU limits (2 cores)"
    echo "   ✅ Disabled heavy features (Prometheus, Registry, Pages)"
    echo "   ✅ Optimized health check timings"
    echo "   ✅ Added proper ulimits for file handles"
    echo ""
    
    if [ "$gitlab_status" = "running" ] && curl -s -f "http://localhost:9191/-/health" >/dev/null 2>&1; then
        echo "🎉 **GitLab should be stable now!**"
        echo ""
        echo "🔗 **Access GitLab:**"
        echo "   URL: http://localhost:9191"
        echo "   Root: root / Adm1nP@ssw0rd2025!"
        echo ""
        echo "🛠️ **If crashes still occur:**"
        echo "   ./scripts/start-stable-gitlab.sh --monitor"
        echo "   docker logs gitlab-ce --follow"
    else
        echo "⚠️ **GitLab not ready yet - check logs for issues**"
    fi
}

usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --analyze         Show crash analysis and fixes"
    echo "  --clean-volumes   Remove all GitLab data (fresh start)"
    echo "  --monitor        Start and monitor GitLab startup"
    echo "  --status         Show current GitLab status"
    echo "  --help           Show this help message"
    echo ""
}

main() {
    case "${1:-}" in
        --analyze)
            analyze_crash_causes
            ;;
        --clean-volumes)
            cleanup_old_gitlab --clean-volumes
            start_stable_gitlab
            monitor_startup
            show_status
            ;;
        --monitor)
            cleanup_old_gitlab
            start_stable_gitlab
            monitor_startup
            show_status
            ;;
        --status)
            show_status
            ;;
        --help)
            usage
            ;;
        "")
            log_info "Starting GitLab with stability fixes..."
            analyze_crash_causes
            cleanup_old_gitlab
            start_stable_gitlab
            show_status
            log_info "Use --monitor flag to watch startup process"
            ;;
        *)
            echo "Unknown option: $1"
            usage
            exit 1
            ;;
    esac
}

main "$@"