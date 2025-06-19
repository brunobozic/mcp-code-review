# Production Deployment Guide

## Overview

This guide covers deploying the MCP Code Review System with GitLab integration in a production environment.

## Quick Start

```bash
# One-command deployment
./scripts/automation/deploy-full-stack.sh

# Verify deployment
./scripts/automation/health-check.sh
```

## Architecture

- **MCP Code Review Server**: .NET 8 HTTP API with multi-agent AI system
- **GitLab CE**: Source code management with webhooks
- **ChromaDB**: Vector database for RAG-enhanced analysis
- **Monitoring Stack**: Prometheus, Grafana, Elasticsearch

## Configuration

### Required Environment Variables

```bash
# API Keys
OPENAI_API_KEY=your_openai_key
CLAUDE_API_KEY=your_claude_key  # Optional

# GitLab Configuration
GITLAB_ROOT_PASSWORD=secure_password
GITLAB_HOST=http://localhost:8080

# Database Passwords
GITLAB_POSTGRES_PASSWORD=secure_db_password
SONAR_POSTGRES_PASSWORD=secure_sonar_password
```

### Quality Gate Thresholds

- **Block MR**: Score < 30 (Critical issues)
- **Manual Review**: Score < 70 (Needs attention)
- **Standard Approval**: Score 70-89 (Good quality)
- **Auto-Approve**: Score ≥ 90 (Excellent quality)

## Integration Flow

```
Developer Code → GitLab MR → Webhook → MCP Server → 
AI Analysis → Review Comments → Quality Gates → Approval/Block
```

## Production Considerations

1. **Security**: Use proper secrets management
2. **Scaling**: Configure resource limits in docker-compose.yml
3. **Monitoring**: Enable Prometheus metrics collection
4. **Backup**: Regular backups of GitLab and ChromaDB data
5. **SSL/TLS**: Enable HTTPS for webhook endpoints

## Troubleshooting

### Common Issues

1. **Webhook failures**: Check network connectivity between GitLab and MCP server
2. **AI analysis errors**: Verify API keys and rate limits
3. **Quality gate failures**: Review threshold configuration

### Health Checks

- MCP Server: `http://localhost:5002/health`
- GitLab: `http://localhost:8080/-/health_check`
- ChromaDB: `http://localhost:8000/api/v1/heartbeat`

## Monitoring

Access monitoring dashboards:
- Grafana: `http://localhost:3000` (admin/SecureGrafanaPass123!)
- Prometheus: `http://localhost:9090`
- Kibana: `http://localhost:5601`