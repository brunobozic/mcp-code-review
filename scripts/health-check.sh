#!/bin/sh
set -e

# Health check script for MCP Code Review Server
# Performs comprehensive health validation

HEALTH_URL="${HEALTH_URL:-http://localhost:5000/health}"
METRICS_URL="${METRICS_URL:-http://localhost:5001/metrics}"
TIMEOUT="${HEALTH_TIMEOUT:-10}"

log() {
    echo "[$(date -Iseconds)] HEALTH_CHECK: $*"
}

check_http_endpoint() {
    local url="$1"
    local expected_status="$2"
    local name="$3"
    
    log "Checking $name endpoint: $url"
    
    response=$(curl -s -w "%{http_code}" -o /tmp/health_response --max-time "$TIMEOUT" "$url" || echo "000")
    
    if [ "$response" = "$expected_status" ]; then
        log "$name endpoint healthy (HTTP $response)"
        return 0
    else
        log "ERROR: $name endpoint unhealthy (HTTP $response)"
        return 1
    fi
}

check_dependencies() {
    log "Checking application dependencies"
    
    # Check if application is responding
    if ! check_http_endpoint "$HEALTH_URL" "200" "Health"; then
        return 1
    fi
    
    # Check metrics endpoint
    if ! check_http_endpoint "$METRICS_URL" "200" "Metrics"; then
        log "WARNING: Metrics endpoint unavailable, continuing..."
    fi
    
    # Check data directory permissions
    if [ ! -w "/data" ]; then
        log "ERROR: Data directory not writable"
        return 1
    fi
    
    # Check log directory permissions
    if [ ! -w "/var/log/mcp" ]; then
        log "ERROR: Log directory not writable"
        return 1
    fi
    
    return 0
}

check_resources() {
    log "Checking system resources"
    
    # Check memory usage (warn if > 80%)
    if command -v free >/dev/null 2>&1; then
        memory_usage=$(free | awk 'NR==2{printf "%.0f", $3*100/$2}')
        if [ "$memory_usage" -gt 80 ]; then
            log "WARNING: High memory usage: ${memory_usage}%"
        else
            log "Memory usage: ${memory_usage}%"
        fi
    fi
    
    # Check disk space (fail if > 90%)
    disk_usage=$(df /data | awk 'NR==2{print $5}' | sed 's/%//')
    if [ "$disk_usage" -gt 90 ]; then
        log "ERROR: Critical disk usage: ${disk_usage}%"
        return 1
    elif [ "$disk_usage" -gt 80 ]; then
        log "WARNING: High disk usage: ${disk_usage}%"
    else
        log "Disk usage: ${disk_usage}%"
    fi
    
    return 0
}

main() {
    log "Starting health check"
    
    if ! check_dependencies; then
        log "Health check FAILED - dependencies check failed"
        exit 1
    fi
    
    if ! check_resources; then
        log "Health check FAILED - resources check failed"
        exit 1
    fi
    
    log "Health check PASSED"
    exit 0
}

main "$@"