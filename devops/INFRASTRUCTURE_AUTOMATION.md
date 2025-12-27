# MCP Code Review System - Complete Infrastructure Automation

## 🎯 Overview

The MCP Code Review System now has **complete infrastructure automation** capable of disposing and reconstructing the entire stack with full automation including:

- ✅ **Complete Stack Teardown** with data preservation options
- ✅ **Full Stack Deployment** with automated GitLab setup
- ✅ **Automatic Sample Project Creation** with realistic code examples
- ✅ **Webhook Integration Setup** for real-time AI review triggers
- ✅ **RAG Database Seeding** with coding standards and patterns
- ✅ **Backup and Restore** functionality with guided instructions
- ✅ **Master Orchestration** script for all operations

## 🚀 Quick Start

### Complete Stack Deployment
```bash
# Deploy everything with full automation
./devops/scripts/automation/stack-orchestrator.sh deploy

# Quick deployment without extras
./devops/scripts/automation/stack-orchestrator.sh deploy-quick
```

### Complete Stack Teardown
```bash
# Graceful teardown (preserves data volumes)
./devops/scripts/automation/stack-orchestrator.sh teardown

# Complete teardown (removes everything)
./devops/scripts/automation/stack-orchestrator.sh teardown-full

# Force teardown (no confirmations)
./devops/scripts/automation/stack-orchestrator.sh teardown-force
```

### Redeploy (Teardown + Deploy)
```bash
# Redeploy with volume preservation for faster startup
./devops/scripts/automation/stack-orchestrator.sh redeploy
```

## 📋 Available Scripts

### Master Orchestration Script
**Location**: `devops/scripts/automation/stack-orchestrator.sh`
**Purpose**: Single entry point for all operations

```bash
# Deployment operations
./stack-orchestrator.sh deploy           # Full deployment
./stack-orchestrator.sh deploy-quick     # Quick deployment
./stack-orchestrator.sh redeploy         # Teardown + deploy
./stack-orchestrator.sh restore <dir>    # Restore from backup

# Management operations  
./stack-orchestrator.sh teardown         # Graceful teardown
./stack-orchestrator.sh teardown-full    # Complete teardown
./stack-orchestrator.sh teardown-force   # Force teardown

# Maintenance operations
./stack-orchestrator.sh health           # System health check
./stack-orchestrator.sh status           # Detailed system status
./stack-orchestrator.sh logs [service]   # Show service logs
./stack-orchestrator.sh backup           # Manual backup
./stack-orchestrator.sh update           # Update service images

# Testing operations
./stack-orchestrator.sh test-integration # Test GitLab + MCP integration
./stack-orchestrator.sh test-webhooks    # Test webhook functionality
./stack-orchestrator.sh test-ai          # Test AI agent collaboration

# Information
./stack-orchestrator.sh info             # Show deployment info
./stack-orchestrator.sh help             # Show help message
```

### Individual Operation Scripts

#### 1. Complete Stack Deployment
**Script**: `devops/scripts/automation/complete-stack-deploy.sh`
**Features**:
- ✅ Full Docker infrastructure deployment
- ✅ GitLab CE with automated user creation
- ✅ Sample project with realistic security/performance issues
- ✅ Webhook configuration for real-time AI triggers
- ✅ RAG database seeding with coding standards
- ✅ SonarQube integration setup
- ✅ Comprehensive health verification

**Options**:
```bash
--restore-backup <dir>   # Restore from backup directory
--skip-prereq           # Skip prerequisite checks  
--no-sample-project     # Skip automatic sample project setup
--no-webhooks           # Skip automatic webhook configuration
--no-rag-seeding        # Skip RAG data seeding
--no-sonarqube          # Skip SonarQube setup
```

#### 2. Complete Stack Teardown
**Script**: `devops/scripts/automation/complete-stack-teardown.sh`
**Features**:
- ✅ Automated backup creation before teardown
- ✅ Graceful service shutdown
- ✅ Selective data preservation options
- ✅ Complete cleanup verification
- ✅ Comprehensive logging and progress indication

**Options**:
```bash
--force                 # Skip confirmations and force teardown
--keep-volumes          # Preserve data volumes (faster redeploy)
--no-backup             # Skip data backup before teardown
```

#### 3. Health Check Script  
**Script**: `devops/scripts/automation/health-check.sh`
**Features**:
- ✅ Service availability verification
- ✅ API endpoint health checks
- ✅ Integration testing
- ✅ Performance monitoring

## 🏗️ Complete Automation Workflow

### What Happens During Deployment

1. **Prerequisites Check**
   - Docker availability and disk space
   - Environment configuration validation
   - Required files and directories

2. **Infrastructure Startup**
   - PostgreSQL and Redis databases
   - ChromaDB vector database  
   - Elasticsearch and monitoring stack
   - GitLab CE with production configuration

3. **GitLab Configuration**
   - Root user setup with secure password
   - API token generation for MCP integration
   - Test developer/reviewer user creation
   - Branch protection and quality gates

4. **Sample Project Setup**
   - E-commerce API project creation
   - Realistic code files with intentional issues:
     - SQL injection vulnerabilities
     - Performance problems (N+1 queries)
     - Security issues (hardcoded keys, weak crypto)
     - Code quality problems

5. **Webhook Integration**
   - Real-time webhook configuration
   - Event filtering for merge requests and comments
   - Integration testing and verification

6. **RAG Database Seeding**
   - Coding standards and best practices
   - Historical issue patterns
   - Team preferences and guidelines
   - Security and performance knowledge

7. **Health Verification**
   - All service endpoints tested
   - Integration workflow validated
   - Performance baselines established

### What Happens During Teardown

1. **Backup Creation** (if enabled)
   - GitLab application backup (databases, repositories)
   - ChromaDB vector database backup
   - Environment and configuration backup
   - Restore instructions generation

2. **Graceful Service Shutdown**
   - Application services stopped first
   - Database services stopped safely
   - Monitoring services cleaned up

3. **Resource Cleanup**
   - Container removal (all or selective)
   - Volume cleanup (optional, can preserve)
   - Network cleanup
   - Temporary file removal

4. **System Optimization**
   - Unused image cleanup
   - Build cache removal
   - System resource reclamation

5. **Verification**
   - Complete cleanup verification
   - Resource usage reporting
   - Success/failure validation

## 🔧 Environment Configuration

### Required Environment Variables (.env)
```bash
# API Keys (Required for AI features)
OPENAI_API_KEY=your-openai-key-here
CLAUDE_API_KEY=your-claude-key-here  

# GitLab Configuration  
GITLAB_ROOT_PASSWORD=Adm1nP@ssw0rd2025!
GITLAB_POSTGRES_PASSWORD=SecureGitLabDBPass123!

# Monitoring Configuration
GRAFANA_ADMIN_PASSWORD=SecureGrafanaPass123!

# SonarQube Configuration (Optional)
SONAR_POSTGRES_PASSWORD=SecureSonarDBPass123!

# ChromaDB Configuration
CHROMADB_URL=http://localhost:19193
CHROMADB_AUTH_TOKEN=test-token
```

### Service Configuration
- **GitLab**: Production-grade configuration with performance tuning
- **MCP Server**: Multi-agent AI system with RAG integration
- **Monitoring**: Prometheus, Grafana, Elasticsearch, Fluentd
- **Databases**: PostgreSQL, Redis, ChromaDB vector database
- **Quality**: SonarQube integration for code quality analysis

## 🧪 Testing the Complete Automation

### 1. Test Complete Lifecycle
```bash
# Start fresh
./stack-orchestrator.sh teardown-full --force

# Deploy everything
./stack-orchestrator.sh deploy

# Verify health
./stack-orchestrator.sh health

# Test integration
./stack-orchestrator.sh test-integration
```

### 2. Test Backup/Restore Workflow
```bash
# Create manual backup
./stack-orchestrator.sh backup

# Teardown completely  
./stack-orchestrator.sh teardown-full

# Restore from backup
./stack-orchestrator.sh restore ./backups/manual_20231217_142530
```

### 3. Test AI Review Workflow
1. Visit: http://localhost:9191/root/ecommerce-api-demo
2. Create branch: `git checkout -b fix/security-issues`
3. Edit PaymentController.cs to fix SQL injection
4. Create merge request
5. Watch AI agents analyze code automatically!

## 📊 Service Endpoints After Deployment

| Service | URL | Purpose |
|---------|-----|---------|
| **GitLab** | http://localhost:9191 | Repository management, merge requests |
| **MCP Server** | http://localhost:5002 | AI code review API |
| **Grafana** | http://localhost:19192 | Monitoring dashboards |
| **Prometheus** | http://localhost:9090 | Metrics collection |
| **Kibana** | http://localhost:5601 | Log analysis |
| **ChromaDB** | http://localhost:19193 | Vector database API |
| **SonarQube** | http://localhost:9000 | Code quality analysis |

## 🔐 Default Login Credentials

| Service | Username | Password |
|---------|----------|----------|
| **GitLab Root** | root | Adm1nP@ssw0rd2025! |
| **GitLab Developer** | developer | DevP@ssw0rd123! |
| **GitLab Reviewer** | reviewer | RevP@ssw0rd123! |
| **Grafana** | admin | SecureGrafanaPass123! |
| **SonarQube** | admin | admin (change on first login) |

## 💾 Backup and Restore

### Automatic Backups
- Created automatically during teardown (unless `--no-backup`)
- Include GitLab repositories, databases, and configuration
- Stored in `./backups/YYYYMMDD_HHMMSS/`
- Include restore instructions

### Manual Backups
```bash
# Create manual backup anytime
./stack-orchestrator.sh backup
```

### Restore Process
```bash
# Restore from specific backup
./stack-orchestrator.sh restore ./backups/20231217_142530

# Or manually:
./complete-stack-deploy.sh --restore-backup ./backups/20231217_142530
```

## 🚨 Troubleshooting

### Common Issues

**1. Docker Out of Space**
```bash
# Clean up Docker resources
docker system prune -a -f
docker volume prune -f
```

**2. GitLab Startup Issues**  
```bash
# Check GitLab logs
./stack-orchestrator.sh logs gitlab

# Restart GitLab service
docker restart mcp-gitlab
```

**3. Service Health Check Failures**
```bash
# Get detailed status
./stack-orchestrator.sh status

# Test individual services
curl http://localhost:5002/health
curl http://localhost:9191/-/health
```

**4. Webhook Not Working**
```bash
# Test webhook manually
./stack-orchestrator.sh test-webhooks

# Check MCP server logs
./stack-orchestrator.sh logs mcp-server
```

### Performance Optimization

**For Faster Development Cycles:**
```bash
# Keep volumes during teardown
./stack-orchestrator.sh teardown --keep-volumes

# Quick redeploy (reuses data)
./stack-orchestrator.sh redeploy
```

**For Production Deployment:**
```bash
# Complete fresh deployment
./stack-orchestrator.sh teardown-full --force
./stack-orchestrator.sh deploy
```

## 🎯 Success Criteria

After running `./stack-orchestrator.sh deploy`, you should have:

✅ **Fully Operational GitLab** with demo project containing realistic issues  
✅ **AI-Powered Code Review** with multi-agent collaboration system  
✅ **Real-time Webhook Integration** triggering automatic analysis  
✅ **Comprehensive Monitoring** with Grafana dashboards  
✅ **RAG-Enhanced Analysis** using vector database knowledge  
✅ **Quality Gates** enforcing code review standards  
✅ **Complete Documentation** and backup/restore capabilities

## 🚀 Next Steps

The infrastructure automation is now **production-ready** and supports:

- **Zero-downtime deployments** with volume preservation
- **Complete disaster recovery** with backup/restore
- **Automated testing workflows** with integration validation  
- **Monitoring and alerting** with comprehensive observability
- **Scalable architecture** ready for production workloads

Your AI-powered code review system is now fully automated and ready for enterprise deployment! 🎉