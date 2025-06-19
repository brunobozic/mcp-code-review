using Mcp.CodeReview.AI;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.HealthChecks;
using Mcp.CodeReview.Infrastructure;
using Mcp.CodeReview.Metrics;
using Mcp.CodeReview.Services;
using Mcp.CodeReview.GitLab;
using Mcp.CodeReview.AI.Enhanced2025.TreeOfThoughts;
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
            builder.Services.AddScoped<IClaudeService, RefactoredClaudeService>();

            // Configure AI services with proper dependency injection
            builder.Services.AddScoped<ArchitectureStandardsAgent>();
            
            // Configure standard multi-agent system
            builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
            builder.Services.AddScoped<IAIReviewService, ConsolidatedAIReviewSystem>();
            
            // Configure Enhanced 2025 Multi-Agent System (Simplified)
            Log.Information("🚀 Configuring Enhanced 2025 Multi-Agent System (Basic Implementation)");
            
            // Core services
            builder.Services.AddHttpClient<ChromaDbService>();
            builder.Services.AddScoped<ChromaDbService>();
            
            // Enhanced 2025 TreeOfThoughts Engine
            builder.Services.AddScoped<TreeOfThoughtsEngine>();
            
            Log.Information("✅ Enhanced 2025 Multi-Agent System configured and ready");

            // Add GitLab integration services
            builder.Services.AddHttpClient<GitLabIntegrationService>(client =>
            {
                var gitLabUrl = builder.Configuration["GITLAB_HOST"] ?? "http://localhost:8080";
                client.BaseAddress = new Uri(gitLabUrl);
                client.Timeout = TimeSpan.FromMinutes(5);
            });
            builder.Services.AddScoped<GitLabIntegrationService>();

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
            builder.Services.AddScoped<IClaudeService, RefactoredClaudeService>();

            // Configure AI services with proper dependency injection
            builder.Services.AddScoped<ArchitectureStandardsAgent>();
            
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