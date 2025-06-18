# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Architecture Overview

**Dual-Mode MCP Server**: .NET 8 application that operates in both STDIO (standard MCP) and HTTP server modes for AI-assisted code review with GitHub/GitLab integration.

**Key Architectural Patterns**:
- **Dependency Injection**: All services registered as singletons in `Program.cs`
- **Security-First Design**: All file operations restricted to `/data` directory, command execution filtered for dangerous operations
- **Service Layer Pattern**: GitHub, GitLab, and Claude services follow consistent patterns with connection testing and structured error handling
- **MCP Tool Pattern**: Tools use `[McpServerTool]` attribute for automatic registration with consistent `McpToolException` error handling

## Development Commands

### Build and Run
```bash
# Build the project
dotnet build

# Run in STDIO mode (default MCP mode)
dotnet run --project src/Mcp.CodeReview/Mcp.CodeReview.csproj

# Run in HTTP mode
dotnet run --project src/Mcp.CodeReview/Mcp.CodeReview.csproj -- --http --port 5000 --metrics-port 9090

# Full stack with monitoring
docker-compose up --build
```

### Testing
```bash
dotnet test
```

## Service Integration Patterns

**ClaudeService**: Uses Claude 3 Sonnet (temperature 0.3) for consistent code analysis. Requires `CLAUDE_API_KEY`.

**GitHubService**: Octokit-based with support for both general PR comments and line-specific review comments. Works with anonymous access or `GITHUB_TOKEN`.

**GitLabService**: NGitLab-based supporting both GitLab.com and self-hosted instances via `GITLAB_HOST`. Handles merge request comments with position-based feedback.

## MCP Tool Architecture

**Tool Categories**:
- **FileTools**: Sandboxed file operations (restricted to `/data`)
- **CommandTools**: Filtered shell execution (blocks dangerous commands)
- **GitTools**: Repository management with auto-cloning/fetching
- **ReviewTools**: Traditional code review with Claude integration
- **AdvancedAIReviewTools**: Multi-agent AI review system with specialized agents

## AI Enhancement Features

**Multi-Agent Review System**: AutoGen-inspired collaborative AI agents for comprehensive code analysis:
- **SecurityExpert**: OWASP Top 10, vulnerability assessment, penetration testing expertise
- **ArchitecturalReviewer**: SOLID principles, design patterns, system scalability
- **PerformanceSpecialist**: Algorithmic complexity, memory optimization, database tuning
- **CodeQualityGuardian**: Clean code principles, testing strategies, refactoring
- **TestingAdvocate**: Test coverage, quality assurance, test-driven development
- **FeatureSlicingAdvocate**: Domain-driven design, vertical slice architecture, pragmatic solutions
- **DeveloperMentor**: Craft improvement, learning guidance, growth-oriented feedback

**Professional AI Features** (inspired by CodeRabbit, GitHub Copilot, SonarQube):
- **RAG-Enhanced Reviews**: Historical PR patterns and codebase context
- **Senior Developer Perspectives**: Architectural impact and long-term maintainability
- **ML-Powered Issue Prioritization**: Business impact assessment and resource allocation
- **Contextual Assistance**: 1-click fixes and pattern suggestions
- **Team Learning Integration**: Balanced AI + human insights for knowledge sharing

## Advanced Prompt Engineering (v2.0)

**Next-Generation AI Review System** implements cutting-edge prompt engineering techniques:

### 🧠 **Enhanced Agent Personas**
- **Detailed Expertise Profiles**: Each agent has 12-18 years of realistic experience with specific company backgrounds
- **Philosophical Frameworks**: Agents have consistent approaches and decision-making philosophies
- **Context-Aware Analysis**: Agents consider business domain, developer experience, and technical context
- **Structured JSON Outputs**: All responses follow precise formats for tool integration

### 🔄 **Advanced Collaborative Intelligence**
- **Strategic Challenge Pairs**: Agents challenge each other on key tradeoffs (Security vs Architecture, Performance vs Domain Design)
- **Constructive Disagreement**: System encourages productive tension rather than false consensus  
- **Synthesis Expertise**: Advanced summarization techniques convert complex multi-agent discussions into clear action items
- **Confidence Calibration**: Multi-level confidence scoring with agent agreement analysis

### 🎯 **Prompt Engineering Techniques**
- **Chain-of-Thought Reasoning**: Agents show their step-by-step analysis process
- **Few-Shot Learning**: Prompts include examples of high-quality analysis
- **Temperature Optimization**: Different agents use optimal temperature settings (Security: 0.1, Mentoring: 0.7)
- **Self-Reflection**: Agents assess their own confidence and reasoning quality
- **Context Injection**: Business domain and developer experience inform all analysis

### 📊 **Enhanced Output Quality**
- **Structured Decision Support**: Clear go/no-go recommendations with reasoning
- **Learning Amplification**: Technical findings converted to growth opportunities
- **Evidence-Based**: All recommendations tied to specific code locations and patterns
- **Actionable Insights**: Complex analysis distilled into implementable next steps
- **ReviewTools**: High-level orchestration combining AI analysis with platform integration

**Security Boundaries**: All operations are sandboxed to `/data` directory. Command execution blocks dangerous operations like `rm -rf`, `sudo`, etc.

## Environment Configuration

**Required Variables**:
- `CLAUDE_API_KEY`: Anthropic API access
- `GITLAB_TOKEN`: GitLab API access  
- `GITHUB_TOKEN`: GitHub API access (optional for public repos)
- `GITLAB_HOST`: Custom GitLab instance (defaults to gitlab.com)

## Observability Stack

**Metrics**: Prometheus-based tracking of review counts, duration histograms, and error rates via `MetricsRegistry.cs`

**Health Checks**: `ExternalApiHealthCheck.cs` monitors GitHub, GitLab, and Claude API connectivity

**Logging**: Serilog with structured logging to console and `/var/log/mcp` with daily rotation

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
- **nextGenAIReview**: Next-generation review with advanced prompt engineering and collaborative challenges ⭐ **NEW**
- **deepSecurityAnalysis**: AI-powered security vulnerability analysis with threat modeling
- **assessPRRisk**: Intelligent PR risk assessment with deployment recommendations
- **generateTestSuggestions**: AI-powered test case generation with coverage gap analysis
- **predictCodeQuality**: Quality prediction using historical patterns and ML insights

### Tool Usage Examples

**Multi-Agent Review**:
```json
{
  "tool": "multiAgentReview",
  "arguments": {
    "diff": "...",
    "language": "csharp", 
    "pullRequestTitle": "Add authentication middleware",
    "author": "developer",
    "filesChanged": ["middleware/auth.cs", "controllers/api.cs"]
  }
}
```

**Security Analysis**:
```json
{
  "tool": "deepSecurityAnalysis", 
  "arguments": {
    "code": "...",
    "language": "csharp",
    "dependencies": ["Microsoft.AspNetCore", "Newtonsoft.Json"],
    "applicationContext": "web API"
  }
}
```

**Next-Generation AI Review**:
```json
{
  "tool": "nextGenAIReview",
  "arguments": {
    "diff": "...",
    "language": "csharp",
    "pullRequestTitle": "Implement payment processing feature",
    "author": "senior-developer",
    "filesChanged": ["domain/payment.cs", "api/billing.cs"],
    "businessContext": "E-commerce payment processing with fraud detection",
    "developerExperience": "5 years, expert in C#, learning advanced patterns",
    "includeCollaborativeChallenges": true
  }
}
```

**Enhanced Multi-Agent Review with DDD & Mentoring**:
```json
{
  "tool": "enhancedMultiAgentReview",
  "arguments": {
    "diff": "...",
    "language": "csharp",
    "pullRequestTitle": "Implement payment processing feature",
    "author": "junior-developer",
    "filesChanged": ["domain/payment.cs", "api/billing.cs"],
    "businessContext": "E-commerce payment processing with fraud detection",
    "developerExperience": "2 years, strong in C# basics, learning DDD"
  }
}
```

**Risk Assessment**:
```json
{
  "tool": "assessPRRisk",
  "arguments": {
    "diff": "...",
    "author": "junior-dev", 
    "linesChanged": 250,
    "modifiedFiles": ["core/payment.cs", "api/billing.cs"]
  }
}
```

## Production Deployment

**Infrastructure**: 
- Docker containerization with Alpine Linux security hardening
- Health checks, rate limiting, and circuit breaker patterns
- Prometheus metrics and Grafana dashboards
- Elasticsearch logging with Sentry error tracking

**Security**:
- Configuration validation on startup
- Secrets management (user secrets, mounted files, environment variables)
- Input sanitization and path traversal protection
- Command execution filtering and sandboxing