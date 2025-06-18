# 🚀 MCP Code Review - Quick Test Guide

## ⚡ 5-Minute Quick Test

```bash
# 1. Check system status
./scripts/health-monitor.sh

# 2. Test AI code review
./scripts/test-mcp-review.sh

# 3. Access dashboards
# Grafana: http://localhost:3000 (admin/SecureGrafanaPass123!)
# Prometheus: http://localhost:9090
```

## 🔥 Full GitLab Integration Test

```bash
# 1. Wait for GitLab (5-10 minutes)
docker logs mcp-gitlab -f

# 2. Create test repository
./scripts/setup-gitlab-repository.sh

# 3. Access GitLab
# URL: http://localhost:8080
# Login: root/SecureGitLabPass123!
```

## ✅ Success Indicators

- **AI Test**: 5 agents activated, 11 issues detected
- **GitLab**: Login page accessible, project created
- **Monitoring**: All dashboards responsive
- **Performance**: < 5 second analysis time

## 🐛 Quick Fixes

```bash
# Restart everything
docker compose restart

# Check logs
docker logs mcp-code-review --tail 20

# Health check
./scripts/health-monitor.sh
```

## 📊 Access Points

| Service | URL | Credentials |
|---------|-----|-------------|
| GitLab | http://localhost:8080 | root/SecureGitLabPass123! |
| Grafana | http://localhost:3000 | admin/SecureGrafanaPass123! |
| Prometheus | http://localhost:9090 | - |
| Elasticsearch | http://localhost:9200 | - |

---
**Full guide:** See `TESTING_GUIDE.md` for comprehensive instructions.