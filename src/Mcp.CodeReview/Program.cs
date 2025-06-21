using Mcp.CodeReview.AI;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.HealthChecks;
using Mcp.CodeReview.Infrastructure;
using Mcp.CodeReview.Metrics;
using Mcp.CodeReview.Services;
using Mcp.CodeReview.GitLab;
using Mcp.CodeReview.RAG;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Serilog;
using System.CommandLine;

var rootCommand = new RootCommand("MCP Code Review Server");

var httpOption = new Option<bool>("--http", "Enable HTTP server");
var portOption = new Option<int>("--port", () => 5000, "HTTP port");
var metricsPortOption = new Option<int>("--metrics-port", () => 5001, "Metrics port");

rootCommand.AddOption(httpOption);
rootCommand.AddOption(portOption);
rootCommand.AddOption(metricsPortOption);

rootCommand.SetHandler(async (bool enableHttp, int port, int metricsPort) =>
{
    try
    {
        if (enableHttp)
        {
            // HTTP mode - use WebApplication
            var builder = WebApplication.CreateBuilder();
            
            // Configure enhanced logging with Serilog
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("logs/mcp-code-review-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
                
            builder.Logging.AddSerilog();

            // Add application services
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<GitHubService>();
            builder.Services.AddSingleton<GitLabService>();

            // Configure AI Service Providers (Universal System) - OpenAI Only for this test
            builder.Services.AddHttpClient<OpenAIServiceProvider>();
            builder.Services.AddScoped<OpenAIServiceProvider>();
            builder.Services.AddScoped<AIServiceManager>();
            
            // Register AI providers collection for AIServiceManager
            builder.Services.AddScoped<IEnumerable<IAIServiceProvider>>(provider => new IAIServiceProvider[]
            {
                provider.GetRequiredService<OpenAIServiceProvider>()
            });
            
            // Register primary AI service interface
            builder.Services.AddScoped<IAIServiceProvider>(provider => 
                provider.GetRequiredService<AIServiceManager>());

            // Keep backward compatibility with existing IClaudeService
            builder.Services.AddScoped<IClaudeService, ClaudeService>();

            // Configure AI services with proper dependency injection
            builder.Services.AddScoped<ArchitectureStandardsAgent>();
            
            // Configure standard multi-agent system
            builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
            
            // Configure enhanced frameworks needed by ConsolidatedAIReviewSystem
            builder.Services.AddScoped<NestedChatFramework>();
            builder.Services.AddScoped<DynamicAgentSelector>();
            builder.Services.AddScoped<EnhancedConversationManager>();
            
            // Configure optimization services (Priority 1 & 3 optimizations)
            builder.Services.AddScoped<SmartCollaborationTrigger>();
            builder.Services.AddMemoryCache(); // Required for IntelligentRAGCache
            builder.Services.AddScoped<IntelligentRAGCache>();
            builder.Services.AddSingleton<RAGCacheConfig>(); // Configuration for cache behavior
            
            // Priority 3: Parallel Agent Execution Optimization
            builder.Services.AddScoped<ParallelAgentExecutor>();
            builder.Services.AddSingleton<ParallelExecutionConfig>(); // Configuration for parallel execution
            
            // Priority 4: Evidence-Based Validation for Critical Findings
            builder.Services.AddScoped<EvidenceBasedValidator>();
            builder.Services.AddSingleton<EvidenceValidationConfig>(); // Configuration for evidence validation
            
            // Priority 5: Dynamic Confidence Calibration System
            builder.Services.AddScoped<HistoricalAccuracyTracker>();
            builder.Services.AddScoped<DynamicConfidenceCalibrator>();
            builder.Services.AddSingleton<AccuracyTrackerConfig>(); // Configuration for accuracy tracking
            builder.Services.AddSingleton<ConfidenceCalibrationConfig>(); // Configuration for confidence calibration
            
            // Configure consolidated AI review system
            builder.Services.AddScoped<IAIReviewService, ConsolidatedAIReviewSystem>();
            
            // Configure RAG services
            Log.Information("🚀 Configuring RAG and Vector Search System");
            
            // Core services
            builder.Services.AddHttpClient<ChromaDbService>();
            builder.Services.AddScoped<ChromaDbService>();
            builder.Services.AddScoped<IVectorSearchService, ChromaDbVectorSearchService>();
            // builder.Services.AddScoped<RAGDataSeeder>(); // Temporarily disabled
            builder.Services.AddScoped<LearningRAGService>();
            
            // Configure OpenAI embedding service for RAG
            builder.Services.AddScoped<IEmbeddingService, OpenAiEmbeddingService>();
            
            // Configure ChromaDB options
            builder.Services.Configure<ChromaDbConfig>(options =>
            {
                options.BaseUrl = builder.Configuration["CHROMADB_URL"] ?? "http://localhost:8000";
                options.AuthToken = builder.Configuration["CHROMADB_AUTH_TOKEN"] ?? "test-token";
            });
            
            Log.Information("✅ Enhanced 2025 Multi-Agent System configured and ready");

            // Add GitLab integration services
            builder.Services.AddHttpClient<GitLabIntegrationService>(client =>
            {
                var gitLabUrl = builder.Configuration["GITLAB_HOST"] ?? "http://localhost:8080";
                client.BaseAddress = new Uri(gitLabUrl);
                client.Timeout = TimeSpan.FromMinutes(5);
            });
            builder.Services.AddScoped<GitLabIntegrationService>();
            
            // Add enhanced GitLab service for intelligent commenting
            builder.Services.AddHttpClient<EnhancedGitLabService>(client =>
            {
                var gitLabUrl = builder.Configuration["GITLAB_HOST"] ?? "http://localhost:8080";
                client.BaseAddress = new Uri(gitLabUrl);
                client.Timeout = TimeSpan.FromMinutes(5);
            });
            builder.Services.AddScoped<EnhancedGitLabService>();

            // Add health checks (basic health check without external API dependencies)
            builder.Services.AddHealthChecks();

            // Add controllers and Swagger
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add Prometheus metrics
            builder.Services.AddSingleton<MetricsRegistry>();

            var app = builder.Build();

            Log.Information("Starting MCP Code Review Server in HTTP mode on port {Port}", port);
            
            // Configure HTTP pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseRouting();
            app.MapControllers();
            app.MapHealthChecks("/health");
            
            // Add a simple root endpoint
            app.MapGet("/", () => new
            {
                service = "MCP Code Review System",
                version = "1.0.0",
                mode = "HTTP",
                status = "healthy"
            });

            app.Urls.Add($"http://0.0.0.0:{port}");
            await app.RunAsync();
        }
        else
        {
            // STDIO mode - use HostBuilder for MCP
            var builder = Host.CreateApplicationBuilder();
            
            // Configure enhanced logging with Serilog
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Trace;
            });
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("logs/mcp-code-review-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
                
            builder.Logging.AddSerilog();

            // Add application services
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<GitHubService>();
            builder.Services.AddSingleton<GitLabService>();

            // Configure Claude service
            builder.Services.AddScoped<IClaudeService, ClaudeService>();

            // Configure AI services with proper dependency injection
            builder.Services.AddScoped<ArchitectureStandardsAgent>();
            
            // Configure optimization services (Priority 1 & 3 optimizations) 
            builder.Services.AddScoped<SmartCollaborationTrigger>();
            builder.Services.AddMemoryCache(); // Required for IntelligentRAGCache
            builder.Services.AddScoped<IntelligentRAGCache>();
            builder.Services.AddSingleton<RAGCacheConfig>(); // Configuration for cache behavior
            
            // Priority 3: Parallel Agent Execution Optimization
            builder.Services.AddScoped<ParallelAgentExecutor>();
            builder.Services.AddSingleton<ParallelExecutionConfig>(); // Configuration for parallel execution
            
            // Priority 4: Evidence-Based Validation for Critical Findings
            builder.Services.AddScoped<EvidenceBasedValidator>();
            builder.Services.AddSingleton<EvidenceValidationConfig>(); // Configuration for evidence validation
            
            // Priority 5: Dynamic Confidence Calibration System
            builder.Services.AddScoped<HistoricalAccuracyTracker>();
            builder.Services.AddScoped<DynamicConfidenceCalibrator>();
            builder.Services.AddSingleton<AccuracyTrackerConfig>(); // Configuration for accuracy tracking
            builder.Services.AddSingleton<ConfidenceCalibrationConfig>(); // Configuration for confidence calibration
            
            // 2025 Enhancement: Choose orchestrator based on environment
            var use2025Enhanced = builder.Configuration.GetValue<bool>("USE_ENHANCED_2025_AGENTS", false); // Disabled for now
            if (use2025Enhanced)
            {
                // Enhanced 2025 agents available but disabled pending interface updates
                Log.Information("🚀 Enhanced 2025 Multi-Agent System available but using standard for stability");
            }
            
            builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
            Log.Information("Using stable multi-agent system");
            
            builder.Services.AddScoped<IAIReviewService, ConsolidatedAIReviewSystem>();

            // Add STDIO server capabilities (standard MCP mode)
            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();

            var app = builder.Build();

            Log.Information("Starting MCP Code Review Server in STDIO mode");
            await app.RunAsync();
        }
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Application terminated unexpectedly");
        throw;
    }
    finally
    {
        Log.CloseAndFlush();
    }
}, httpOption, portOption, metricsPortOption);

return await rootCommand.InvokeAsync(args);