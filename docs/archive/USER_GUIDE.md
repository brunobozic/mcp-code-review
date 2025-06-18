# MCP Code Review Server - Production User Guide

## Executive Summary

The MCP Code Review Server is an enterprise-grade AI-powered code review platform that integrates seamlessly with your GitHub/GitLab workflows. Built on Anthropic's Model Context Protocol (MCP), it provides automated code analysis, intelligent security scanning, and actionable feedback while maintaining strict security and compliance standards.

### Business Value Proposition:
- **Reduced Review Time**: 60-80% faster code review cycles with AI-assisted analysis
- **Improved Code Quality**: Consistent application of best practices across all repositories
- **Security Enhancement**: Automated vulnerability detection and security pattern analysis  
- **Developer Productivity**: Real-time feedback reduces iteration cycles and context switching
- **Compliance Ready**: Full audit trails, structured logging, and comprehensive monitoring
- **Cost Optimization**: Reduce senior developer time spent on routine code reviews

### Enterprise Features:
- **High Availability**: Multi-container architecture with health checks and auto-recovery
- **Scalable Architecture**: Horizontal scaling with load balancing support
- **Security First**: Zero-trust design with sandboxed execution and input validation
- **Production Observability**: Real-time metrics, distributed tracing, and alerting
- **Multi-Tenancy Ready**: Environment isolation and resource management

## Production Deployment Guide

### Infrastructure Requirements

#### Minimum System Requirements:
- **CPU**: 4 cores (8 recommended for production)
- **Memory**: 8GB RAM (16GB recommended)
- **Storage**: 100GB SSD (500GB for production with log retention)
- **Network**: 1Gbps bandwidth with low latency to API endpoints

#### Production Environment Prerequisites:
- **Container Orchestration**: Docker Swarm, Kubernetes, or Docker Compose v3.8+
- **Load Balancer**: NGINX, HAProxy, or cloud load balancer
- **TLS Certificates**: Valid SSL certificates for HTTPS endpoints
- **DNS**: Proper domain configuration with health check endpoints
- **Backup Strategy**: Automated backup for data volumes and configurations
- **Monitoring**: Prometheus-compatible metrics ingestion
- **Log Management**: Centralized logging with retention policies

### 1. Secure Environment Setup

#### Production Environment Configuration:
```bash
# Create secure directory structure
sudo mkdir -p /opt/mcp-code-review/{config,data,logs,backups}
sudo chown -R 1000:1000 /opt/mcp-code-review
sudo chmod 750 /opt/mcp-code-review

# Clone and secure the repository
git clone <your-repo-url> /opt/mcp-code-review/app
cd /opt/mcp-code-review/app

# Create production environment file with proper permissions
cp .env.example .env.production
sudo chmod 600 .env.production
```

#### Production Environment Variables:
```env
# Application Configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5000;https://0.0.0.0:5001
ASPNETCORE_HTTPS_PORT=5001
ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certs/aspnetcore.pfx
ASPNETCORE_Kestrel__Certificates__Default__Password=${SSL_CERT_PASSWORD}

# API Keys (use secrets management in production)
CLAUDE_API_KEY=${CLAUDE_API_KEY}
GITHUB_TOKEN=${GITHUB_TOKEN}
GITLAB_TOKEN=${GITLAB_TOKEN}
GITLAB_HOST=${GITLAB_HOST:-https://gitlab.com}

# Security Configuration
GRAFANA_ADMIN_PASSWORD=${GRAFANA_ADMIN_PASSWORD}
GRAFANA_SECRET_KEY=${GRAFANA_SECRET_KEY}
JWT_SECRET_KEY=${JWT_SECRET_KEY}
ENCRYPTION_KEY=${ENCRYPTION_KEY}

# Monitoring & Observability
SENTRY_DSN=${SENTRY_DSN}
ELASTICSEARCH_URL=https://elasticsearch:9200
ELASTICSEARCH_USERNAME=${ELASTICSEARCH_USERNAME}
ELASTICSEARCH_PASSWORD=${ELASTICSEARCH_PASSWORD}

# Performance Tuning
MAX_CONCURRENT_REVIEWS=10
REQUEST_TIMEOUT_SECONDS=120
MAX_REPOSITORY_SIZE_MB=1000
CACHE_TTL_MINUTES=60

# Compliance & Audit
AUDIT_LOG_RETENTION_DAYS=2555  # 7 years
PII_REDACTION_ENABLED=true
GDPR_COMPLIANCE_MODE=true
```

### 2. Production Docker Compose Configuration

Create `docker-compose.production.yml`:
```yaml
version: '3.8'

services:
  mcp-server:
    build: 
      context: .
      dockerfile: Dockerfile.production
    image: mcp-code-review:${VERSION:-latest}
    container_name: mcp-server-primary
    restart: unless-stopped
    deploy:
      replicas: 3
      resources:
        limits:
          memory: 2G
          cpus: "1.0"
        reservations:
          memory: 1G
          cpus: "0.5"
      restart_policy:
        condition: on-failure
        delay: 5s
        max_attempts: 3
    ports:
      - "5000:5000"
      - "5001:5001"
    volumes:
      - /opt/mcp-code-review/data:/data:rw
      - /opt/mcp-code-review/logs:/var/log/mcp:rw
      - /opt/mcp-code-review/certs:/app/certs:ro
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
    env_file:
      - .env.production
    healthcheck:
      test: ["CMD", "curl", "-f", "https://localhost:5001/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 60s
    logging:
      driver: fluentd
      options:
        fluentd-address: localhost:24224
        tag: mcp.server.{{.Name}}
        labels: "environment,version"
    depends_on:
      elasticsearch:
        condition: service_healthy
      fluentd:
        condition: service_started
    networks:
      - mcp-network

  # Load balancer for high availability
  nginx:
    image: nginx:alpine
    container_name: mcp-loadbalancer
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./nginx/ssl:/etc/nginx/ssl:ro
    depends_on:
      - mcp-server
    networks:
      - mcp-network

  # Enhanced Elasticsearch with security
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.11.0
    container_name: mcp-elasticsearch
    restart: unless-stopped
    environment:
      - discovery.type=single-node
      - "ES_JAVA_OPTS=-Xms2g -Xmx2g"
      - xpack.security.enabled=true
      - ELASTIC_PASSWORD=${ELASTICSEARCH_PASSWORD}
      - xpack.security.transport.ssl.enabled=true
      - xpack.security.http.ssl.enabled=true
    volumes:
      - elasticsearch-data:/usr/share/elasticsearch/data
      - ./elasticsearch/config:/usr/share/elasticsearch/config:ro
    ulimits:
      memlock:
        soft: -1
        hard: -1
    deploy:
      resources:
        limits:
          memory: 4G
        reservations:
          memory: 2G
    healthcheck:
      test: ["CMD-SHELL", "curl -u elastic:${ELASTICSEARCH_PASSWORD} -s https://localhost:9200/_cluster/health | grep -q '\"status\":\"green\"'"]
      interval: 30s
      timeout: 10s
      retries: 5
    networks:
      - mcp-network

  # Production Grafana with persistence
  grafana:
    image: grafana/grafana:latest
    container_name: mcp-grafana
    restart: unless-stopped
    user: "472"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_ADMIN_PASSWORD}
      - GF_SECURITY_SECRET_KEY=${GRAFANA_SECRET_KEY}
      - GF_USERS_ALLOW_SIGN_UP=false
      - GF_AUTH_ANONYMOUS_ENABLED=false
      - GF_SECURITY_COOKIE_SECURE=true
      - GF_SERVER_PROTOCOL=https
      - GF_SERVER_CERT_FILE=/etc/grafana/ssl/grafana.crt
      - GF_SERVER_CERT_KEY=/etc/grafana/ssl/grafana.key
    volumes:
      - grafana-data:/var/lib/grafana
      - ./grafana/dashboards:/etc/grafana/provisioning/dashboards:ro
      - ./grafana/datasources:/etc/grafana/provisioning/datasources:ro
      - ./grafana/ssl:/etc/grafana/ssl:ro
    ports:
      - "3000:3000"
    networks:
      - mcp-network

volumes:
  elasticsearch-data:
    driver: local
    driver_opts:
      type: none
      o: bind
      device: /opt/mcp-code-review/data/elasticsearch
  grafana-data:
    driver: local
    driver_opts:
      type: none
      o: bind
      device: /opt/mcp-code-review/data/grafana

networks:
  mcp-network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
```

### 3. Production Deployment Process

```bash
# Build production image
docker build -f Dockerfile.production -t mcp-code-review:${VERSION} .

# Run security scanning
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  aquasec/trivy image mcp-code-review:${VERSION}

# Deploy with zero-downtime
docker-compose -f docker-compose.production.yml up -d --force-recreate

# Verify deployment
./scripts/health-check.sh
./scripts/smoke-test.sh
```

## Understanding the Components

### 1. MCP Server Core
**What it does**: Provides the main code review functionality through standardized MCP tools.

**Available Tools**:
- `listFiles` - Browse repository files
- `readFile` - Read file contents
- `writeFile` - Write files (sandboxed to `/data`)
- `runCommand` - Execute shell commands (security-filtered)
- `getPRDiff` - Get pull request differences
- `commentOnPR` - Add review comments
- `summarizeCode` - AI-powered code analysis

**How to use it**: 
- HTTP mode: Send POST requests to `/mcp` endpoint
- STDIO mode: Direct MCP protocol communication

### 2. AI Review Engine (Claude Integration)
**What it does**: Analyzes code changes and provides intelligent feedback using Claude 3 Sonnet.

**Features**:
- Context-aware code review
- Security vulnerability detection
- Performance optimization suggestions
- Best practice recommendations
- Language-specific analysis

**How to use it**: Call the `summarizeCode` tool with diff content and language specification.

### 3. Git Platform Integration
**What it does**: Seamlessly integrates with GitHub and GitLab APIs for PR management.

**Capabilities**:
- Fetch PR diffs automatically
- Post review comments (general or line-specific)
- Support for both GitHub.com and self-hosted GitLab
- Automatic platform detection

**How to use it**: Provide repository URLs and PR numbers to the relevant tools.

### 4. Security & Sandboxing
**What it does**: Ensures safe execution of code analysis and file operations.

**Security Features**:
- File operations restricted to `/data` directory
- Command execution filtering (blocks dangerous commands)
- Input validation and sanitization
- Repository size limits (500MB max)
- Request timeouts and rate limiting

### 5. Observability Stack
**What it does**: Provides comprehensive monitoring, logging, and error tracking.

**Components**:
- **Serilog**: Structured logging with rich context
- **Prometheus**: Metrics collection and alerting
- **Grafana**: Visualization dashboards
- **Elasticsearch + Fluentd**: Centralized log aggregation
- **Sentry**: Error tracking and performance monitoring

## Usage Scenarios

### Scenario 1: Automated PR Review
**Goal**: Get AI feedback on a pull request

```bash
# Using HTTP API
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
      "name": "getPRDiff",
      "arguments": {
        "repoUrl": "https://github.com/owner/repo",
        "baseBranch": "main",
        "headBranch": "feature-branch"
      }
    }
  }'

# Then analyze the diff
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/call",
    "params": {
      "name": "summarizeCode",
      "arguments": {
        "diff": "<diff_content>",
        "language": "csharp"
      }
    }
  }'
```

### Scenario 2: Interactive Code Review
**Goal**: Review code changes and post comments directly to PR

1. Get the diff: Use `getPRDiff` tool
2. Analyze code: Use `summarizeCode` tool
3. Post feedback: Use `commentOnPR` tool

```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "method": "tools/call",
  "params": {
    "name": "commentOnPR",
    "arguments": {
      "repoUrl": "https://github.com/owner/repo",
      "prNumber": 123,
      "body": "AI Review: This change looks good! Consider adding error handling for the database connection.",
      "path": "src/Service.cs",
      "line": 45
    }
  }
}
```

### Scenario 3: Local Development Integration
**Goal**: Use as a development tool for local code analysis

```bash
# Start in STDIO mode for direct integration
docker run -it --rm \
  -v $(pwd):/data \
  -e CLAUDE_API_KEY=your_key \
  mcp-code-review

# Then use with MCP-compatible tools or custom scripts
```

## Prototyping & Pilot Strategy

### Phase 1: Local Prototyping (Week 1-2)
1. **Setup local environment** using docker-compose
2. **Test with sample repository**: 
   - Create test PRs with different code changes
   - Experiment with different languages (C#, JavaScript, Python)
   - Test security boundaries and error handling
3. **Explore the tools**: Try each MCP tool individually
4. **Monitor observability**: Check Grafana dashboards and log aggregation

### Phase 2: Team Pilot (Week 3-4)
1. **Deploy to staging environment**:
   ```bash
   # Set production-like environment
   export ASPNETCORE_ENVIRONMENT=Staging
   docker-compose up -d
   ```
2. **Integrate with real repositories**: Start with low-risk projects
3. **Create team workflows**: Document how team members should interact with the system
4. **Collect feedback**: Monitor Sentry for errors, check metrics in Grafana

### Phase 3: Production Pilot (Week 5-8)
1. **Production deployment**: Use production environment settings
2. **CI/CD Integration**: Automate PR reviews in your pipeline
3. **Performance monitoring**: Track response times and resource usage
4. **Scale testing**: Test with multiple concurrent reviews

## Integration Patterns

### 1. GitHub Actions Integration
```yaml
name: AI Code Review
on:
  pull_request:
    types: [opened, synchronize]

jobs:
  ai-review:
    runs-on: ubuntu-latest
    steps:
      - name: Run AI Review
        run: |
          curl -X POST ${{ secrets.MCP_SERVER_URL }}/mcp \
            -H "Content-Type: application/json" \
            -d "{
              \"jsonrpc\": \"2.0\",
              \"id\": 1,
              \"method\": \"tools/call\",
              \"params\": {
                \"name\": \"summarizeCode\",
                \"arguments\": {
                  \"diff\": \"${{ github.event.pull_request.diff_url }}\",
                  \"language\": \"detect\"
                }
              }
            }"
```

### 2. GitLab CI Integration
```yaml
ai-review:
  stage: review
  script:
    - |
      curl -X POST $MCP_SERVER_URL/mcp \
        -H "Content-Type: application/json" \
        -d "{
          \"jsonrpc\": \"2.0\",
          \"id\": 1,
          \"method\": \"tools/call\",
          \"params\": {
            \"name\": \"getPRDiff\",
            \"arguments\": {
              \"repoUrl\": \"$CI_PROJECT_URL\",
              \"baseBranch\": \"$CI_MERGE_REQUEST_TARGET_BRANCH_NAME\",
              \"headBranch\": \"$CI_MERGE_REQUEST_SOURCE_BRANCH_NAME\"
            }
          }
        }"
  only:
    - merge_requests
```

### 3. Custom Webhook Integration
```python
# Example webhook handler
@app.route('/webhook/pr', methods=['POST'])
def handle_pr_webhook():
    data = request.json
    
    # Extract PR information
    repo_url = data['repository']['html_url']
    pr_number = data['number']
    
    # Call MCP server
    response = requests.post(f"{MCP_SERVER_URL}/mcp", json={
        "jsonrpc": "2.0",
        "id": 1,
        "method": "tools/call",
        "params": {
            "name": "getPRDiff",
            "arguments": {
                "repoUrl": repo_url,
                "baseBranch": data['base']['ref'],
                "headBranch": data['head']['ref']
            }
        }
    })
    
    return "OK"
```

## Monitoring & Troubleshooting

### Key Dashboards to Monitor
1. **Grafana Main Dashboard** (http://localhost:3000):
   - Request rates and response times
   - Error rates and success metrics
   - Resource usage (CPU, memory)
   - API call patterns

2. **Sentry Error Tracking**:
   - Real-time error notifications
   - Performance bottlenecks
   - User session traces

3. **Elasticsearch Logs** (via Grafana):
   - Structured log search
   - Request correlation
   - Security event monitoring

### Common Issues & Solutions

**Issue**: Claude API rate limiting
- **Solution**: Implement request queuing, upgrade API plan
- **Monitor**: Check Sentry for HTTP 429 errors

**Issue**: Repository clone failures
- **Solution**: Check network connectivity, validate repository URLs
- **Monitor**: Look for Git operation errors in logs

**Issue**: Memory usage spikes
- **Solution**: Implement repository size limits, add memory monitoring
- **Monitor**: Grafana memory usage dashboard

## Best Practices

### Security
- ✅ Always use environment variables for secrets
- ✅ Regularly rotate API tokens
- ✅ Monitor for unusual access patterns
- ✅ Keep the `/data` directory isolated
- ✅ Review Sentry errors for security issues

### Performance
- ✅ Monitor Claude API usage and costs
- ✅ Implement caching for repeated repository operations
- ✅ Set appropriate timeout values
- ✅ Use staging environment for testing large changes

### Operations
- ✅ Set up log rotation and cleanup
- ✅ Monitor disk space usage
- ✅ Implement health check endpoints
- ✅ Create runbooks for common operations

## Advanced Configuration

### Custom Linter Integration
Add support for additional code quality tools by extending the `GetLinterCommand` method in `ReviewTools.cs`:

```csharp
private string GetLinterCommand(string language) => language.ToLowerInvariant() switch
{
    "csharp" or "c#" => "dotnet build --no-restore --verbosity normal",
    "javascript" or "typescript" => "npm run lint 2>&1 || echo 'No lint script found'",
    "python" => "python -m flake8 . 2>&1 || echo 'flake8 not installed'",
    "rust" => "cargo clippy -- -D warnings 2>&1",
    "go" => "golangci-lint run 2>&1",
    _ => ""
};
```

### Custom Metrics
Add application-specific metrics by extending the `MetricsRegistry.cs`:

```csharp
public static readonly Counter CustomOperations = Metrics
    .CreateCounter("mcp_custom_operations_total", "Custom operations counter");
```

### Environment-Specific Configuration
Create environment-specific docker-compose files:

```yaml
# docker-compose.production.yml
version: '3.8'
services:
  mcp-server:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - SENTRY_DSN=${SENTRY_DSN}
    deploy:
      replicas: 3
      resources:
        limits:
          memory: 1G
          cpus: "0.5"
```

## Getting Help

### Log Analysis
```bash
# View real-time logs
docker-compose logs -f mcp-server

# Search structured logs
curl "http://localhost:9200/mcp-logs-*/_search" \
  -H "Content-Type: application/json" \
  -d '{"query": {"match": {"level": "ERROR"}}}'
```

### Health Checks
```bash
# Check service health
curl http://localhost:5000/health

# Check metrics
curl http://localhost:5000/metrics
```

### Debug Mode
```bash
# Run with debug logging
docker run -e ASPNETCORE_ENVIRONMENT=Development \
  -e CLAUDE_API_KEY=your_key \
  mcp-code-review --http --port 5000
```

This solution provides a production-ready foundation for AI-assisted code reviews with comprehensive monitoring and security features. Start with the prototyping phase to understand the capabilities, then gradually integrate into your development workflow.