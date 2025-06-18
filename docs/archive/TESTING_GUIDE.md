# MCP Code Review System - Complete Testing Guide

## 📋 Prerequisites

Before testing, ensure you have:
- Docker and Docker Compose installed
- Port access to: 3000, 5000-5003, 8080, 9000, 9090, 9200, 5601
- At least 8GB RAM available for GitLab
- Internet connection for image downloads

## 🚀 Quick Start Testing (5 minutes)

### Step 1: Check System Status
```bash
# Navigate to project directory
cd /home/brunobozic/mcp-code-review

# Check which services are running
docker compose ps

# Run comprehensive health check
./scripts/health-monitor.sh
```

**Expected Output:**
- ✅ MCP Server: Running
- ✅ Prometheus: Healthy
- ✅ Grafana: Healthy
- ✅ Elasticsearch: Healthy
- ⏳ GitLab: Starting (may take 5-10 minutes)

### Step 2: Test AI Code Review System
```bash
# Run the simulated merge request test
./scripts/test-mcp-review.sh
```

**What This Tests:**
- AI agent activation and analysis
- Security vulnerability detection
- Performance optimization suggestions
- Code quality improvements
- Documentation analysis
- Nested chat framework
- Review report generation

**Expected Results:**
- 11 issues detected and analyzed
- 5 AI agents activated
- Comprehensive review report generated
- 91.6% average confidence score

### Step 3: Access Monitoring Dashboards

**Grafana Dashboard:**
```bash
# Open in browser
http://localhost:3000
# Login: admin / SecureGrafanaPass123!
```

**Prometheus Metrics:**
```bash
# Open in browser
http://localhost:9090
# Check targets and metrics
```

**Elasticsearch Logs:**
```bash
# Open in browser
http://localhost:9200
# Check cluster health
curl http://localhost:9200/_cluster/health
```

## 🔥 Advanced Testing (15-30 minutes)

### Step 4: Wait for GitLab Initialization
```bash
# Monitor GitLab startup (takes 5-10 minutes)
docker logs mcp-gitlab -f

# Or check status periodically
./scripts/health-monitor.sh gitlab

# GitLab is ready when you see:
curl http://localhost:8080/users/sign_in
# Returns HTML login page
```

### Step 5: Create GitLab Test Repository
```bash
# Once GitLab is ready, run the repository setup
./scripts/setup-gitlab-repository.sh

# This will:
# 1. Create a sample C# project
# 2. Add code with deliberate issues
# 3. Create a merge request with improvements
# 4. Configure webhooks for MCP integration
```

**Expected Output:**
- ✅ GitLab project created
- ✅ Sample code added
- ✅ Merge request created
- ✅ Webhook configured

### Step 6: Access GitLab Interface
```bash
# Open GitLab in browser
http://localhost:8080

# Login credentials:
# Username: root
# Password: SecureGitLabPass123!
```

**Navigate to:**
1. Projects → `mcp-sample-csharp-project`
2. Merge Requests → `Improve Calculator class...`
3. Review the code changes and comments

### Step 7: Trigger Manual Code Review
```bash
# If GitLab webhook integration is working, reviews trigger automatically
# For manual testing, use the API:

# Get project details first
curl -H "Authorization: Bearer YOUR_GITLAB_TOKEN" \
     http://localhost:8080/api/v4/projects

# Trigger manual review (replace PROJECT_ID and MR_IID)
curl -X POST http://localhost:5002/api/gitlabwebhook/review-merge-request \
     -H 'Content-Type: application/json' \
     -d '{"project_id": "1", "merge_request_iid": "1"}'
```

## 🧪 Comprehensive System Testing

### Step 8: Test Individual Components

**Test MCP Server Health:**
```bash
# Check if MCP server responds
curl http://localhost:5002/health
# Expected: {"status": "healthy", "service": "MCP Code Review"}

# Check server logs
docker logs mcp-code-review --tail 20
```

**Test AI Agent Performance:**
```bash
# Run specific agent tests
./scripts/test-mcp-review.sh analysis
./scripts/test-mcp-review.sh report
./scripts/test-mcp-review.sh metrics
```

**Test Logging Pipeline:**
```bash
# Check if logs are being captured
./scripts/health-monitor.sh logs

# View Elasticsearch indices
curl http://localhost:9200/_cat/indices

# Search for recent logs
curl "http://localhost:9200/_search?q=*&size=5&sort=@timestamp:desc"
```

### Step 9: Performance Testing

**Load Test with Multiple Reviews:**
```bash
# Create multiple test scenarios
for i in {1..5}; do
    echo "Running test scenario $i"
    ./scripts/test-mcp-review.sh &
done
wait

# Check system performance
docker stats --no-stream
```

**Memory and CPU Usage:**
```bash
# Monitor resource usage
docker stats mcp-code-review mcp-gitlab mcp-elasticsearch

# Check disk usage
df -h
du -sh /home/brunobozic/mcp-code-review/
```

### Step 10: Integration Testing

**Test File Change Detection:**
```bash
# Modify test files to trigger different analysis patterns
cd test-repository

# Test security issues
echo 'public string UnsafeMethod(string input) { return input; }' >> Calculator.cs

# Test performance issues  
echo 'public void SlowMethod() { Thread.Sleep(1000); }' >> Calculator.cs

# Re-run analysis
./scripts/test-mcp-review.sh
```

**Test Different Code Languages:**
```bash
# Create Python test file
cat > test-repository/sample.py << 'EOF'
def unsafe_function(user_input):
    exec(user_input)  # Security vulnerability
    
def slow_function():
    for i in range(1000000):  # Performance issue
        print(i)
EOF

# Analyze Python code (if implemented)
./scripts/test-mcp-review.sh
```

## 🔍 Validation Checklist

### ✅ Core Functionality
- [ ] MCP server starts and responds to health checks
- [ ] All 5 AI agents activate and analyze code
- [ ] Security vulnerabilities are detected
- [ ] Performance issues are identified
- [ ] Quality improvements are suggested
- [ ] Documentation gaps are found
- [ ] Review reports are generated

### ✅ GitLab Integration
- [ ] GitLab starts and login page is accessible
- [ ] Sample repository is created successfully
- [ ] Merge request is created with code changes
- [ ] Webhooks are configured properly
- [ ] MCP responds to GitLab webhook events
- [ ] Comments are posted back to merge requests

### ✅ Monitoring & Observability
- [ ] Prometheus metrics are collected
- [ ] Grafana dashboards are accessible
- [ ] Elasticsearch receives and indexes logs
- [ ] Kibana can search and visualize logs
- [ ] All containers output logs to stdout/stderr

### ✅ Performance & Reliability
- [ ] Review analysis completes in < 5 seconds
- [ ] System handles multiple concurrent reviews
- [ ] Memory usage stays under 2GB total
- [ ] No container restarts or crashes
- [ ] Error handling works properly

## 🐛 Troubleshooting Guide

### Common Issues and Solutions

**GitLab Takes Too Long to Start:**
```bash
# Check GitLab logs for errors
docker logs mcp-gitlab --tail 50

# If permission issues:
docker exec -it mcp-gitlab update-permissions
docker compose restart gitlab

# If memory issues, increase Docker memory to 8GB+
```

**MCP Server Not Responding:**
```bash
# Check server status
docker compose ps mcp-server

# Restart if needed
docker compose restart mcp-server

# Check logs for errors
docker logs mcp-code-review --tail 30
```

**Monitoring Services Down:**
```bash
# Restart monitoring stack
docker compose restart prometheus grafana elasticsearch

# Check network connectivity
docker network ls
docker network inspect mcp-network
```

**Port Conflicts:**
```bash
# Check what's using ports
ss -tuln | grep -E ':(3000|5000|8080|9090|9200)'

# Stop conflicting services or change ports in docker-compose.yml
```

### Performance Optimization

**If System is Slow:**
```bash
# Check resource usage
docker stats

# Reduce GitLab workers if needed
# Edit docker-compose.yml:
# unicorn['worker_processes'] = 2
# sidekiq['max_concurrency'] = 10
```

**If Running Out of Disk Space:**
```bash
# Clean up Docker
docker system prune -a

# Clean up logs
docker compose exec elasticsearch curl -X DELETE "localhost:9200/*-logs-*"
```

## 📊 Expected Test Results

### Successful Test Indicators

**AI Analysis Results:**
- Security Agent: 90-98% confidence
- Performance Agent: 85-95% confidence  
- Quality Agent: 80-90% confidence
- Detection of 8-15 issues per analysis
- Analysis time: 1-5 seconds

**System Performance:**
- Memory usage: < 2GB total
- CPU usage: < 50% during analysis
- Response time: < 3 seconds for health checks
- No error logs in critical services

**Integration Results:**
- GitLab accessible within 10 minutes
- Webhook events trigger MCP analysis
- Comments posted back to merge requests
- Monitoring dashboards show real-time data

### Success Criteria

✅ **Functional Requirements:**
- All AI agents operational
- GitLab integration working
- Monitoring stack active
- Code analysis accurate

✅ **Performance Requirements:**
- < 5 second analysis time
- < 2GB memory usage
- 99% uptime for core services
- Real-time log processing

✅ **Usability Requirements:**
- Easy setup and testing
- Clear documentation
- Comprehensive error handling
- Production-ready configuration

## 🎯 Next Steps After Testing

### Production Deployment
1. Configure real API keys in `.env`
2. Set up SSL/TLS certificates
3. Configure external databases
4. Set up backup procedures
5. Configure monitoring alerts

### Customization
1. Add more AI agents for specific languages
2. Customize review criteria
3. Add custom webhook integrations
4. Configure additional metrics

### Scaling
1. Deploy on Kubernetes
2. Add load balancing
3. Scale AI agent workers
4. Implement caching layers

---

## 🆘 Support

If you encounter issues during testing:

1. **Check logs first:** `docker logs <container-name>`
2. **Run health checks:** `./scripts/health-monitor.sh`
3. **Restart services:** `docker compose restart <service>`
4. **Clean restart:** `docker compose down && docker compose up -d`

**System is working correctly when:**
- All health checks pass ✅
- Test script completes successfully ✅  
- GitLab is accessible and functional ✅
- Monitoring dashboards show data ✅

Happy testing! 🚀