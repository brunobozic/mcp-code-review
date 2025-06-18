# System Tools Reference

## Available Development Tools ✅

### Essential Commands
- **git**: `/usr/bin/git` - Version control
- **curl**: `/usr/bin/curl` - HTTP client for API testing
- **jq**: `/usr/bin/jq` - JSON processor for parsing API responses
- **ss**: `/usr/bin/ss` - Network socket statistics (modern netstat replacement)
- **docker**: `/usr/bin/docker` - Container management
- **docker-compose**: Docker Compose for multi-container orchestration
- **vim/nano**: Text editors
- **wget**: File downloader

### Network Diagnostics
```bash
# Check listening ports (replaces netstat -tuln)
ss -tuln

# Check specific port
ss -tuln | grep :9200

# Check network connections
ss -tulpn

# Test HTTP endpoints
curl -f http://localhost:9200/_cluster/health
```

### Docker Management
```bash
# Container status
docker ps
docker compose ps

# View logs
docker logs <container-name>
docker logs <container-name> -f --tail 50

# Resource usage
docker stats

# Network inspection
docker network ls
docker network inspect mcp-network
```

### System Monitoring
```bash
# Port usage
ss -tuln | grep -E ':(5000|3000|9090|9200|5601|8080|9000)'

# Disk usage
df -h
du -sh /home/brunobozic/mcp-code-review/

# Memory usage
free -h

# Process monitoring
ps aux | grep -E '(docker|fluentd|elasticsearch)'
```

### Health Check Commands
```bash
# Run comprehensive health check
./scripts/health-monitor.sh

# Check specific components
./scripts/health-monitor.sh docker
./scripts/health-monitor.sh services
./scripts/health-monitor.sh network

# Start system
./scripts/start-system.sh
```

## System Status Commands

### Quick Health Checks
```bash
# Check all service endpoints
curl -f http://localhost:3000/api/health    # Grafana
curl -f http://localhost:9090/-/healthy     # Prometheus
curl -f http://localhost:9200/_cluster/health # Elasticsearch

# Check container health
docker compose ps
```

### Log Analysis
```bash
# Real-time log monitoring
docker logs mcp-code-review -f
docker logs mcp-fluentd -f | grep ERROR

# Search logs for errors
docker logs mcp-code-review 2>&1 | grep -i error
docker logs mcp-grafana 2>&1 | jq 'select(.level=="error")'
```

## Missing Tools (if needed)

If additional tools are required, they can be installed with:
```bash
# Note: Requires sudo password for user bbozic
sudo apt update
sudo apt install -y net-tools htop tree unzip
```

### Alternative Commands (No sudo required)
- Use `ss` instead of `netstat`
- Use `docker stats` instead of `htop` for container monitoring
- Use `find` instead of `tree` for directory structure
- Use built-in tools for file management

## Notes
- All essential tools for MCP Code Review system are available
- Scripts have been updated to use `ss` instead of `netstat`
- Docker and Docker Compose are properly configured
- System monitoring scripts work without additional installations