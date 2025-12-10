# 🚀 How to Start MCP Code Review System

**Complete setup guide for Windows and Linux systems**

## 📋 **Prerequisites Checklist**

### **System Requirements**
- ✅ **RAM**: 8GB+ recommended (minimum 4GB)
- ✅ **Disk Space**: 10GB+ free space for containers
- ✅ **CPU**: 2+ cores recommended
- ✅ **Network**: Internet connection for AI APIs and container images

### **Software Requirements**

#### **🐧 Linux (Ubuntu/Debian/CentOS/RHEL)**
```bash
# Docker Engine & Docker Compose
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER

# Docker Compose (if not included)
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Git (usually pre-installed)
sudo apt update && sudo apt install git -y  # Ubuntu/Debian
sudo yum install git -y                     # CentOS/RHEL
```

#### **🪟 Windows 10/11**
1. **Install Docker Desktop for Windows**
   - Download from: https://docs.docker.com/desktop/install/windows-install/
   - Run installer and restart computer
   - Enable WSL2 backend (recommended)

2. **Install Git for Windows**
   - Download from: https://git-scm.com/download/win
   - Use default installation options

3. **PowerShell 7+ (Optional but recommended)**
   - Download from: https://github.com/PowerShell/PowerShell/releases

#### **🍎 macOS**
```bash
# Docker Desktop for Mac
# Download from: https://docs.docker.com/desktop/install/mac-install/

# Git (via Homebrew)
brew install git

# Or via Xcode Command Line Tools
xcode-select --install
```

---

## 🎯 **Quick Start (5-Minute Setup)**

### **1️⃣ Clone Repository**

#### **Linux/macOS Terminal:**
```bash
# Clone the repository
git clone https://github.com/brunobozic/mcp-code-review.git
cd mcp-code-review

# Make scripts executable
chmod +x start-mcp.sh
chmod +x scripts/automation/*.sh
```

#### **Windows PowerShell/Command Prompt:**
```cmd
# Clone the repository
git clone https://github.com/brunobozic/mcp-code-review.git
cd mcp-code-review

# Windows scripts are already executable (.bat files)
```

### **2️⃣ Configure Environment**

#### **Linux/macOS:**
```bash
# Copy environment template
cp .env.example .env

# Edit configuration file
nano .env          # or vim, code, gedit
```

#### **Windows:**
```cmd
# Copy environment template
copy .env.example .env

# Edit configuration file
notepad .env       # or code .env (VS Code)
```

### **3️⃣ Add Your API Keys**

Edit the `.env` file with your API credentials:

```bash
# =============================================================================
# AI API Keys (REQUIRED - System won't work without these)
# =============================================================================
CLAUDE_API_KEY=sk-ant-api03-your-claude-api-key-here
OPENAI_API_KEY=sk-your-openai-api-key-here

# =============================================================================
# GitLab Configuration (Auto-configured, but customizable)
# =============================================================================
GITLAB_TOKEN=your_gitlab_token_optional
GITLAB_HOST=http://localhost:8080
GITLAB_ROOT_PASSWORD=Adm1nP@ssw0rd2025!

# =============================================================================
# Optional Integrations (Leave blank if not using)
# =============================================================================
GITHUB_TOKEN=ghp_your_github_token_optional
SONARQUBE_TOKEN=squ_your_sonarqube_token_optional
```

**🔑 How to get API Keys:**
- **Claude API Key**: Visit https://console.anthropic.com/dashboard
- **OpenAI API Key**: Visit https://platform.openai.com/api-keys
- **GitHub Token**: Visit https://github.com/settings/tokens (optional)

### **4️⃣ Start the System**

#### **🐧 Linux/macOS:**
```bash
# Method 1: Complete system test (recommended - validates everything)
./bin/test-complete-system

# Method 2: Quick infrastructure test
./bin/test-infrastructure

# Method 3: System status check
./bin/quick-status
```

#### **🪟 Windows:**
```cmd
REM Method 1: Complete system test (recommended)
bash bin/test-complete-system

REM Method 2: Quick infrastructure test  
bash bin/test-infrastructure

REM Method 3: System status check
bash bin/quick-status

REM Note: Windows requires WSL2 or Git Bash for shell scripts
```

#### **Available Utility Scripts:**
```bash
./bin/test-complete-system  # Full system validation
./bin/test-infrastructure   # Infrastructure testing
./bin/quick-status          # Check system status
./bin/verify-system         # Verify system health  
./bin/cleanup-docker        # Clean Docker resources
./bin/gitlab-init           # Initialize GitLab setup
./bin/verify-gitlab-access.py # GitLab access verification
```

---

## 🔍 **Verification & Access**

### **5️⃣ Wait for System Startup**

The complete system takes **2-5 minutes** to fully initialize all components:

```bash
# Check startup progress (after running test-complete-system)
./bin/quick-status

# Verify all containers are running
docker ps

# Check specific service logs
docker logs <container_name>
```

### **6️⃣ Access System Components**

Once startup is complete, access these services in your browser:

| Service | URL | Default Credentials | Purpose |
|---------|-----|-------------------|---------|
| **🦊 GitLab** | http://localhost:8080 | root / Adm1nP@ssw0rd2025! | Repository management |
| **📊 Grafana** | http://localhost:3000 | admin / SecureGrafanaPass123! | System monitoring |
| **🤖 MCP Server** | http://localhost:5002 | No auth required | AI review API |
| **🔍 SonarQube** | http://localhost:9000 | admin / admin | Code quality |

### **7️⃣ Verify System Health**

#### **Automated Health Check:**
```bash
# Linux/macOS
./scripts/health/validate-env.sh

# Windows
scripts\health\validate-env.sh
```

#### **Manual Verification:**
```bash
# Test each service individually
curl http://localhost:5002/health     # MCP Server ✅
curl http://localhost:8080/-/health   # GitLab ✅  
curl http://localhost:3000/api/health # Grafana ✅
curl http://localhost:9000/api/system/status # SonarQube ✅
curl http://localhost:9200/_cluster/health   # Elasticsearch ✅
```

**✅ Expected Results:**
- All services return HTTP 200 status
- GitLab login page appears
- Grafana dashboard loads
- MCP server returns "Healthy"

---

## 🧪 **Test the AI Review System**

### **8️⃣ Create Test Project in GitLab**

1. **Login to GitLab**: http://localhost:8080
   - Username: `root`
   - Password: `Adm1nP@ssw0rd2025!`

2. **Create New Project**:
   - Click "New project" → "Create blank project"
   - Project name: `test-ai-review`
   - Visibility: Private
   - Click "Create project"

3. **Add Sample Code**:
   ```bash
   # Clone the test project locally
   git clone http://root:Adm1nP@ssw0rd2025!@localhost:8080/root/test-ai-review.git
   cd test-ai-review
   
   # Create a sample file with intentional issues
   cat > PaymentService.cs << 'EOF'
   public class PaymentService
   {
       public void ProcessPayment(string cardNumber)
       {
           // SECURITY ISSUE: SQL Injection vulnerability
           var query = "SELECT * FROM payments WHERE card = '" + cardNumber + "'";
           
           // PERFORMANCE ISSUE: Synchronous database call
           var result = Database.ExecuteSync(query);
           
           // QUALITY ISSUE: No error handling
           SendConfirmationEmail(result.Email);
       }
   }
   EOF
   
   # Commit and push
   git add PaymentService.cs
   git commit -m "Add payment service with security issues"
   git push origin main
   ```

4. **Create Merge Request**:
   - Create feature branch: `git checkout -b fix/payment-security`
   - Make a small change and commit
   - Push branch and create merge request in GitLab web interface

5. **Watch AI Review**:
   - AI system automatically analyzes the merge request
   - Check merge request comments for AI feedback
   - Review should identify security, performance, and quality issues

---

## 🛠️ **Advanced Configuration**

### **🔧 Customize AI Agents**

Edit agent configuration in `src/Mcp.CodeReview/AI/`:
```bash
# Modify agent behavior
src/Mcp.CodeReview/AI/ConsolidatedAIReviewSystem.cs
src/Mcp.CodeReview/AI/OptimizedAgentCollaborationEngine.cs
```

### **📊 Monitoring Configuration**

Configure Grafana dashboards:
```bash
# Custom dashboards
devops/infrastructure/docker/services/grafana/dashboards/

# Data sources
devops/infrastructure/docker/services/grafana/datasources/
```

### **🗃️ RAG Knowledge Base**

Customize the AI knowledge base:
```bash
# Add coding standards
devops/infrastructure/docker/services/rag-data/coding-standards/

# Add team patterns
devops/infrastructure/docker/services/rag-data/team-patterns/

# Add historical issues
devops/infrastructure/docker/services/rag-data/historical-issues/
```

---

## 🚨 **Troubleshooting Guide**

### **Common Startup Issues**

#### **🐳 Docker Issues**
```bash
# Issue: "Cannot connect to Docker daemon"
# Solution: Start Docker service
sudo systemctl start docker         # Linux
# Restart Docker Desktop             # Windows/macOS

# Issue: "Port already in use"
# Solution: Check for conflicting services
sudo lsof -i :8080                  # Check GitLab port
sudo lsof -i :3000                  # Check Grafana port
sudo lsof -i :5002                  # Check MCP port

# Stop conflicting services
sudo systemctl stop nginx           # Example
```

#### **🔑 API Key Issues**
```bash
# Issue: "Unauthorized" errors in logs
# Solution: Verify API keys in .env file

# Check MCP Server logs
docker compose -f devops/infrastructure/docker/docker-compose.yml logs mcp-server

# Look for API authentication errors
```

#### **💾 Memory Issues**
```bash
# Issue: Containers crash or fail to start
# Solution: Increase Docker memory allocation

# Linux: Check available memory
free -h

# Docker Desktop: Settings → Resources → Memory → 8GB+
```

### **🔧 System Diagnostics**

#### **Container Health Check:**
```bash
# Check all container status
docker ps -a

# Check specific container logs
docker logs mcp-gitlab
docker logs mcp-server
docker logs mcp-grafana

# Restart problematic containers
docker restart mcp-gitlab
```

#### **Network Connectivity:**
```bash
# Test internal container communication
docker exec mcp-server ping mcp-gitlab
docker exec mcp-server curl http://mcp-gitlab:80

# Test external API connectivity
docker exec mcp-server curl https://api.anthropic.com/v1/messages
```

#### **GitLab Specific Issues:**
```bash
# GitLab won't start
docker logs mcp-gitlab
docker logs mcp-gitlab-postgres
docker logs mcp-gitlab-redis

# Reset GitLab data (CAUTION: Destroys all data)
docker volume rm mcp-network_gitlab-data
docker volume rm mcp-network_gitlab-config
```

### **🆘 Getting Help**

1. **Check System Logs**:
   ```bash
   # All services
   ./bin/quick-status
   
   # Specific service
   ./bin/verify-system
   ```

2. **Health Validation**:
   ```bash
   ./scripts/health/validate-env.sh 2>&1 | tee health-check.log
   ```

3. **System Reset** (Nuclear option):
   ```bash
   # Stop all services and clean up
   ./bin/cleanup-docker
   
   # Remove all volumes (DESTROYS ALL DATA)
   docker volume prune
   
   # Restart system
   ./bin/test-complete-system
   ```

---

## ✅ **Success Indicators**

Your system is working correctly when:

- ✅ **All 15 containers running** (`docker ps` shows healthy containers)
- ✅ **GitLab accessible** (can login with root credentials)  
- ✅ **Grafana dashboard loads** (monitoring data visible)
- ✅ **MCP health check passes** (`curl http://localhost:5002/health` returns "Healthy")
- ✅ **AI review triggers** (merge requests get automatic AI comments)
- ✅ **Logs flowing** (Grafana shows container logs via Fluentd)

## 🎉 **You're Ready!**

Congratulations! Your AI-powered code review system is now running. Create merge requests in GitLab to see the multi-agent AI system in action.

**Next Steps:**
- Read the [README.md](README.md) for detailed system information
- Explore the [CLAUDE.md](CLAUDE.md) for Claude Code integration
- Check the [Architecture Documentation](devops/infrastructure/) for customization

**Happy Code Reviewing! 🚀**