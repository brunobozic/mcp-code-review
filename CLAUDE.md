# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture Overview

**Dual-Mode MCP Server**: .NET 8 application that operates in both STDIO (standard MCP) and HTTP server modes for AI-assisted code review with GitLab/GitHub integration and RAG-enhanced analysis.

**Key Architectural Patterns**:
- **Dependency Injection**: All services registered with proper DI container management in `Program.cs`
- **Universal AI Provider System**: Abstracted AI service layer supporting Claude, OpenAI, and other providers through `IAIServiceProvider`
- **Multi-Agent AI Orchestration**: Specialized AI agents working in parallel via `AgentOrchestrator` and `ConsolidatedAIReviewSystem`
- **RAG-Enhanced Analysis**: ChromaDB vector database with semantic search for contextual code analysis
- **Security-First Design**: All file operations restricted to `/data` directory, command execution filtered for dangerous operations
- **Service Layer Pattern**: GitHub, GitLab, and AI services follow consistent patterns with connection testing and structured error handling
- **MCP Tool Pattern**: Tools use `[McpServerTool]` attribute for automatic registration with consistent `McpToolException` error handling

## Current Implementation Status

### ✅ Working Features
- **Multi-Agent AI System**: 8+ specialized agents (SecurityExpert, PerformanceAnalyst, CodeQualityReviewer, etc.) working via `AgentOrchestrator.cs:164-173`
- **Universal AI Provider System**: `IAIServiceProvider` interface with `AIServiceManager` supporting Claude and OpenAI providers
- **RAG Vector Search**: ChromaDB integration via `ChromaDbVectorSearchService.cs` with semantic search capabilities
- **GitLab Integration**: Complete webhook processing and MR analysis
- **HTTP Server Mode**: REST API endpoints for code review operations
- **Docker Infrastructure**: Full containerized stack with monitoring
- **Professional GitLab Setup**: Working multi-container GitLab instance with PostgreSQL/Redis separation
- **Sample C# Project**: E-commerce API with intentional security/performance issues for testing

### 🚧 Partially Implemented/Excluded Features
- **Enhanced 2025 Multi-Agent System**: Advanced features exist in `/AI/2025_Enhanced/` but are **excluded from compilation** (see `Mcp.CodeReview.csproj:42-48`)
  - Only `TreeOfThoughtsEngine.cs` is included
  - Advanced orchestrator, meta-reasoning, and critic systems are excluded
- **RAG System**: Interface and services exist but data seeding and full integration needs verification

### ❌ Documentation Drift Issues
- Documentation claims "PRODUCTION READY" but Enhanced 2025 features are disabled
- Claims of "everything implemented" are inaccurate - many 2025 features are excluded from build
- RAG system documentation is optimistic about operational status

## Development Commands

### Build and Run MCP Server
```bash
# Build the project
dotnet build

# Run in STDIO mode (default MCP mode)
dotnet run --project src/Mcp.CodeReview/Mcp.CodeReview.csproj

# Run in HTTP mode
dotnet run --project src/Mcp.CodeReview/Mcp.CodeReview.csproj -- --http --port 5000 --metrics-port 5001

# Full stack with monitoring
docker compose up --build
```

### GitLab Setup (REQUIRED for Testing)
```bash
# Start GitLab (professional setup)
cd gitlab-config
./start-gitlab.sh

# Check GitLab health
./check-gitlab.sh

# GitLab Access:
# URL: http://localhost:8080
# Root: root / Adm1nP@ssw0rd2025!
# Developer: developer@example.com / DevP@ssw0rd123!
# Reviewer: reviewer@example.com / RevP@ssw0rd123!
```

### Sample C# Project for Testing
**Location**: `sample-projects/ecommerce-api/`
**Purpose**: Contains intentional security and performance issues for MCP testing
**Key Issues**:
- SQL injection vulnerabilities
- Hardcoded credentials and weak authentication
- Sensitive data exposure
- Performance N+1 query problems
- Missing input validation

### Testing MCP Code Review
```bash
# Health checks
curl http://localhost:5002/health           # MCP Server
curl http://localhost:8000/api/v1/heartbeat # ChromaDB
curl http://localhost:8080/-/health         # GitLab

# Test multi-agent review via API
curl -X POST http://localhost:5002/api/review \
  -H "Content-Type: application/json" \
  -d '{
    "repoUrl": "http://localhost:8080/root/ecommerce-api-demo.git",
    "baseBranch": "main",
    "headBranch": "feature/payment-improvements"
  }'
```

## Service Integration Patterns

**Universal AI Service Provider**: Implemented via `IAIServiceProvider` interface with automatic failover:
- **Primary**: Claude 3 Sonnet (via `ClaudeServiceProvider`)
- **Fallback**: OpenAI (via `OpenAIServiceProvider`) 
- **Manager**: `AIServiceManager` handles provider selection and health checks

**GitHubService**: Octokit-based with support for both general PR comments and line-specific review comments. Works with anonymous access or `GITHUB_TOKEN`.

**GitLabService**: NGitLab-based supporting both GitLab.com and self-hosted instances via `GITLAB_HOST`. Handles merge request comments with position-based feedback.

**RAG System**: ChromaDB-based vector search with collections for:
- Code patterns (`code_patterns`)
- Coding standards (`coding_standards`)  
- Historical issues (`historical_issues`)
- Team patterns (`team_patterns`)

## AI Enhancement Features

### Current Multi-Agent Review System (Working)
**Core Agents** via `AgentOrchestrator.cs:164-173`:
- **SecurityExpert**: OWASP Top 10, vulnerability assessment, security analysis
- **PerformanceAnalyst**: Algorithmic complexity, memory optimization, performance tuning
- **CodeQualityReviewer**: Clean code principles, best practices enforcement
- **ArchitectureExpert**: SOLID principles, design patterns, system scalability
- **TestingSpecialist**: Test coverage, quality assurance, test strategies
- **DomainExpert**: Business logic analysis, domain-driven design
- **FeatureSlicingExpert**: Vertical slice architecture, feature decomposition
- **DeveloperMentor**: Code improvement suggestions, learning guidance
- **AICodeDetective**: AI-generated code detection and validation

### Enhanced 2025 Features (Excluded from Build)
**Available but disabled in `Mcp.CodeReview.csproj:42-48`**:
- Tree of Thoughts reasoning engine
- Cross-agent validation and challenge mechanisms
- Meta-reasoning and self-reflection capabilities
- Enhanced RAG with learning and adaptation
- Hallucination detection and confidence calibration
- Agent critic systems and debate frameworks

### RAG-Enhanced Analysis (Implemented)
- **ChromaDB Integration**: `ChromaDbVectorSearchService.cs` provides semantic search
- **Embedding Service**: Local embeddings via `OpenAiEmbeddingService`
- **Vector Collections**: Organized knowledge base with similarity search
- **Context Retrieval**: Similar code patterns, standards, and historical issues

## MCP Tool Architecture

**Tool Categories**:
- **FileTools**: Sandboxed file operations (restricted to `/data`)
- **CommandTools**: Filtered shell execution (blocks dangerous commands)
- **GitTools**: Repository management with auto-cloning/fetching
- **ReviewTools**: Traditional code review with AI integration
- **AdvancedAIReviewTools**: Multi-agent AI review system with specialized agents

## Available MCP Tools

### Core Tools
- **listFiles**: List files in directory with pattern filtering
- **readFile**: Read file contents with line range support
- **writeFile**: Write content to files (sandboxed to `/data`)
- **executeCommand**: Run shell commands (filtered for security)
- **cloneRepository**: Clone Git repositories with automatic cleanup
- **fetchLatestChanges**: Update existing repository with latest changes
- **reviewCode**: Traditional Claude-powered code review
- **reviewPullRequest**: Full PR analysis with GitHub/GitLab integration
- **analyzeDiff**: Quick diff analysis with suggestions

### Advanced AI Tools
- **multiAgentReview**: Multi-agent collaborative code review with specialized AI experts
- **enhancedMultiAgentReview**: Comprehensive review with DDD, feature slicing, and developer mentoring
- **deepSecurityAnalysis**: AI-powered security vulnerability analysis
- **assessPRRisk**: Intelligent PR risk assessment
- **generateTestSuggestions**: AI-powered test case generation
- **predictCodeQuality**: Quality prediction using patterns

### HTTP API Endpoints (Working)
- **POST /api/review**: Standard multi-agent code review
- **POST /api/review/enhanced-2025**: Enhanced review with metadata simulation
- **GET /api/review/enhanced-2025/info**: System capabilities information
- **GET /**: Service information and health status
- **GET /health**: Health check endpoint

## Environment Configuration

**Required Variables**:
- `CLAUDE_API_KEY`: Anthropic API access (required)
- `OPENAI_API_KEY`: OpenAI API access (optional, for fallback)
- `GITLAB_TOKEN`: GitLab API access (optional for public repos)
- `GITHUB_TOKEN`: GitHub API access (optional for public repos) 
- `GITLAB_HOST`: Custom GitLab instance (defaults to gitlab.com)

**AI Provider Selection**:
- `AI:PreferredProvider`: "Claude" (default) or "OpenAI"

## Current System Limitations

### Build Configuration Issues
1. **Enhanced 2025 Features Excluded**: Many advanced features exist in codebase but are excluded from compilation
2. **Interface Inconsistencies**: Some Enhanced 2025 components reference interfaces that may not be fully compatible
3. **RAG Integration**: While implemented, needs verification of data seeding and full operational status

### Documentation vs Reality
1. **Overstated Capabilities**: Documentation claims full implementation of features that are excluded
2. **Production Readiness**: Claims need verification given excluded components
3. **Performance Claims**: Benchmarks may not reflect actual system with excluded features

## Observability Stack

**Metrics**: Prometheus-based tracking of review counts, duration histograms, and error rates via `MetricsRegistry.cs`

**Health Checks**: Basic health monitoring for service availability

**Logging**: Serilog with structured logging to console and files with daily rotation

## Production Deployment Notes

**Infrastructure**: 
- Docker containerization with multi-service stack
- ChromaDB for RAG vector storage
- GitLab CE for repository management
- Prometheus/Grafana for monitoring

**Current Status**: 
- Core multi-agent system is functional
- Enhanced 2025 features require build configuration updates to enable
- RAG system requires data seeding verification
- Full production deployment should validate all integrated components

**Security**:
- Configuration validation on startup
- Secrets management via environment variables
- Input sanitization and path traversal protection
- Command execution filtering and sandboxing

## GitLab Integration Setup

### Professional GitLab Instance (✅ OPERATIONAL)
**Configuration Files**: `gitlab-config/`
- **docker-compose-gitlab-fixed.yml**: Multi-container setup (PostgreSQL + Redis + GitLab)
- **start-gitlab.sh**: Professional startup script with health monitoring
- **check-gitlab.sh**: Real-time status monitoring

### GitLab Access Details
- **URL**: http://localhost:8080
- **Root User**: `root / Adm1nP@ssw0rd2025!`
- **Developer**: `developer@example.com / DevP@ssw0rd123!`
- **Reviewer**: `reviewer@example.com / RevP@ssw0rd123!`
- **SSH**: `ssh://git@localhost:2222`

### GitLab Integration Status
✅ **Container Health**: All services (GitLab, PostgreSQL, Redis) healthy
✅ **Web Interface**: Responding with proper login redirects
✅ **API Access**: Authentication working, token generation tested
✅ **User Accounts**: Root + developer + reviewer accounts created
✅ **Repository Access**: Ready for Git operations and merge requests

## MCP Code Review Testing Workflow

### Prerequisites
1. **GitLab Running**: `./gitlab-config/start-gitlab.sh`
2. **MCP Server Running**: `dotnet run --project src/Mcp.CodeReview/Mcp.CodeReview.csproj -- --http`
3. **Sample Project**: Available in `sample-projects/ecommerce-api/`

### Testing Process
1. **Create GitLab Project**: Upload sample C# project to GitLab
2. **Create Feature Branch**: Make changes to trigger security/performance issues
3. **Create Merge Request**: Submit MR for review
4. **Trigger MCP Review**: Use API or webhook to initiate multi-agent analysis
5. **Verify AI Response**: Check that SecurityExpert, PerformanceAnalyst, etc. detect intentional issues

### Expected AI Agent Detections
- **SecurityExpert**: SQL injection, hardcoded credentials, data exposure
- **PerformanceAnalyst**: N+1 queries, blocking async calls, memory leaks
- **CodeQualityReviewer**: Missing validation, error handling issues
- **ArchitectureExpert**: DI misconfigurations, service lifecycle issues

### Files Structure Reference
```
.
├── CLAUDE.md                           # This file - project guidance
├── GITLAB_SETUP.md                     # Detailed GitLab setup documentation
├── gitlab-config/                      # GitLab professional setup
│   ├── docker-compose-gitlab-fixed.yml # Working GitLab configuration
│   ├── start-gitlab.sh                 # Startup script
│   └── check-gitlab.sh                 # Health monitoring
├── scripts/                            # GitLab automation
│   ├── gitlab-setup.sh                 # User creation automation
│   └── postgres-init.sql               # Database initialization
├── sample-projects/ecommerce-api/      # C# test project with issues
│   ├── Controllers/PaymentController.cs # Intentional security issues
│   ├── Services/                       # Performance problems
│   └── EcommerceApi.csproj             # .NET 8 web API
└── src/Mcp.CodeReview/                 # Main MCP server code
    ├── AI/                             # Multi-agent system
    ├── Tools/                          # MCP tools for integration
    └── Services/                       # GitLab/GitHub/AI services
```