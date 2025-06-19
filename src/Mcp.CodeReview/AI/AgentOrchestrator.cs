using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;
using Mcp.CodeReview.Utilities;
using System.Collections.Concurrent;

namespace Mcp.CodeReview.AI;

/// <summary>
/// Agent orchestrator for managing parallel execution of specialized AI agents
/// </summary>
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly IClaudeService _claudeService;
    private readonly ILogger<AgentOrchestrator> _logger;
    private readonly SemaphoreSlim _concurrencyLimiter;
    
    // Agent factory registry
    private readonly ConcurrentDictionary<AgentType, Func<AgentConfiguration, ISpecializedAgent>> _agentFactories;
    
    // Configuration
    private const int MaxConcurrentAgents = 5;

    public AgentOrchestrator(IClaudeService claudeService, ILogger<AgentOrchestrator> logger)
    {
        _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _concurrencyLimiter = new SemaphoreSlim(MaxConcurrentAgents, MaxConcurrentAgents);
        _agentFactories = new ConcurrentDictionary<AgentType, Func<AgentConfiguration, ISpecializedAgent>>();
        
        RegisterAgentFactories();
    }

    /// <inheritdoc />
    public async Task<Abstractions.AgentExecutionResult[]> ExecuteAgentsParallelAsync(
        IEnumerable<AgentExecutionTask> agentTasks, 
        CancellationToken cancellationToken = default)
    {
        var tasks = agentTasks.ToArray();
        ErrorHandling.ValidateRequired(tasks, nameof(agentTasks));

        try
        {
            return await ExecuteAgentsInternalAsync(tasks, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute agents in parallel");
            return Array.Empty<Abstractions.AgentExecutionResult>();
        }
    }

    /// <inheritdoc />
    public ISpecializedAgent CreateAgent(AgentType agentType, AgentConfiguration configuration)
    {
        ErrorHandling.ValidateRequired(configuration, nameof(configuration));

        if (_agentFactories.TryGetValue(agentType, out var factory))
        {
            return factory(configuration);
        }

        _logger.LogWarning("No factory registered for agent type {AgentType}, using default agent", agentType);
        return new DefaultSpecializedAgent(agentType, _claudeService, _logger);
    }

    // Private implementation methods
    private async Task<Abstractions.AgentExecutionResult[]> ExecuteAgentsInternalAsync(
        AgentExecutionTask[] tasks, 
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12];
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["AgentCount"] = tasks.Length
        });

        _logger.LogInformation("Executing {AgentCount} agents in parallel with correlation ID {CorrelationId}", 
            tasks.Length, correlationId);

        // Sort tasks by priority
        var sortedTasks = tasks.OrderBy(t => t.Priority).ToArray();
        
        // Create execution tasks
        var executionTasks = sortedTasks.Select(task => ExecuteSingleAgentAsync(task, correlationId, cancellationToken));
        
        // Execute all agents in parallel
        var results = await Task.WhenAll(executionTasks);
        
        _logger.LogInformation("Completed parallel execution of {AgentCount} agents", tasks.Length);
        
        return results;
    }

    private async Task<Abstractions.AgentExecutionResult> ExecuteSingleAgentAsync(
        AgentExecutionTask task, 
        string correlationId, 
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        // Acquire semaphore to limit concurrency
        await _concurrencyLimiter.WaitAsync(cancellationToken);
        
        try
        {
            using var agentScope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["AgentType"] = task.AgentType,
                ["CorrelationId"] = correlationId,
                ["Priority"] = task.Priority
            });

            _logger.LogDebug("Starting execution of {AgentType} agent", task.AgentType);

            // Create agent with enhanced configuration based on agent type
            var configuration = CreateEnhancedConfiguration(task.AgentType);
            
            var agent = CreateAgent(task.AgentType, configuration);
            
            // Execute agent analysis
            var result = await agent.AnalyzeAsync(task.Content, task.Context, cancellationToken);
            
            var executionTime = DateTime.UtcNow - startTime;
            
            _logger.LogDebug("Completed execution of {AgentType} agent in {ElapsedMs}ms", 
                task.AgentType, executionTime.TotalMilliseconds);

            return new Abstractions.AgentExecutionResult
            {
                AgentType = task.AgentType,
                Result = result,
                ExecutionTime = executionTime,
                Success = true
            };
        }
        catch (Exception ex)
        {
            var executionTime = DateTime.UtcNow - startTime;
            
            _logger.LogError(ex, "Failed to execute {AgentType} agent after {ElapsedMs}ms", 
                task.AgentType, executionTime.TotalMilliseconds);

            return new Abstractions.AgentExecutionResult
            {
                AgentType = task.AgentType,
                Result = CreateFailureResult(task.AgentType),
                ExecutionTime = executionTime,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            _concurrencyLimiter.Release();
        }
    }

    private void RegisterAgentFactories()
    {
        // Register factories for each agent type
        _agentFactories[AgentType.SecurityExpert] = config => new SecurityExpertAgent(_claudeService, _logger, config);
        _agentFactories[AgentType.PerformanceAnalyst] = config => new PerformanceAnalystAgent(_claudeService, _logger, config);
        _agentFactories[AgentType.CodeQualityReviewer] = config => new CodeQualityReviewerAgent(_claudeService, _logger, config);
        _agentFactories[AgentType.ArchitectureExpert] = config => new ArchitectureExpertAgent(_claudeService, _logger, config);
        _agentFactories[AgentType.TestingSpecialist] = config => new TestingSpecialistAgent(_claudeService, _logger, config);
        _agentFactories[AgentType.DomainExpert] = config => new DomainExpertAgent(_claudeService, _logger, config);
        _agentFactories[AgentType.FeatureSlicingExpert] = config => new FeatureSlicingExpertAgent(_claudeService, _logger, config);
        _agentFactories[AgentType.DeveloperMentor] = config => new DeveloperMentorAgent(_claudeService, _logger, config);
        _agentFactories[AgentType.AICodeDetective] = config => new AICodeDetectiveAgent(_claudeService, _logger, config);
        
        _logger.LogDebug("Registered {FactoryCount} agent factories", _agentFactories.Count);
    }

    private static AgentConfiguration CreateEnhancedConfiguration(AgentType agentType)
    {
        // Get enhanced persona configuration if available
        var personas = EnhancedAgentPersonas.CreateEnhancedPersonas();
        var agentKey = agentType.ToString();
        
        if (personas.TryGetValue(agentKey, out var persona))
        {
            return new AgentConfiguration
            {
                Temperature = persona.Temperature,
                MaxTokens = persona.MaxTokens
            };
        }
        
        // Fallback to default configuration
        return new AgentConfiguration
        {
            Temperature = 0.3,
            MaxTokens = 2000
        };
    }

    private static AgentResult CreateFailureResult(AgentType agentType)
    {
        return new AgentResult
        {
            AgentType = agentType,
            AgentName = agentType.ToString(),
            Analysis = "Analysis failed due to system error",
            ConfidenceScore = 0.0,
            Findings = new List<Finding>
            {
                new Finding
                {
                    Type = "SystemError",
                    Description = "Agent execution failed",
                    Severity = "ERROR"
                }
            },
            Recommendations = new List<Recommendation>
            {
                new Recommendation
                {
                    Title = "Retry Analysis",
                    Description = "Please retry the analysis when the system is available",
                    Priority = "LOW"
                }
            }
        };
    }
}

/// <summary>
/// Base class for specialized AI agents
/// </summary>
public abstract class BaseSpecializedAgent : ISpecializedAgent
{
    protected readonly IClaudeService ClaudeService;
    protected readonly ILogger Logger;
    protected readonly AgentConfiguration Configuration;

    public abstract AgentType Type { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }

    protected BaseSpecializedAgent(IClaudeService claudeService, ILogger logger, AgentConfiguration configuration)
    {
        ClaudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public virtual async Task<AgentResult> AnalyzeAsync(
        string content, 
        Dictionary<string, object> context, 
        CancellationToken cancellationToken = default)
    {
        // Use enhanced prompt builder for sophisticated prompting
        var prompt = EnhancedPromptBuilder.BuildEnhancedAgentPrompt(Type, content, context);
        
        var analysis = await ClaudeService.GenerateReviewWithParametersAsync(
            prompt, 
            Configuration.Temperature, 
            Configuration.MaxTokens, 
            cancellationToken);

        return ParseAnalysisResult(analysis);
    }

    public virtual bool CanHandle(string contentType)
    {
        // Base implementation - can handle most content types
        return !string.IsNullOrEmpty(contentType);
    }

    protected virtual AgentResult ParseAnalysisResult(string analysis)
    {
        // Basic parsing - subclasses can override for specialized parsing
        return new AgentResult
        {
            AgentType = Type,
            AgentName = Name,
            Analysis = analysis,
            ConfidenceScore = CalculateConfidenceScore(analysis),
            Findings = ExtractFindings(analysis),
            Recommendations = ExtractRecommendations(analysis)
        };
    }

    protected virtual double CalculateConfidenceScore(string analysis)
    {
        // Simple heuristic - could be improved with ML models
        var indicators = new[] { "recommend", "suggest", "issue", "problem", "improve" };
        var matches = indicators.Count(indicator => 
            analysis.Contains(indicator, StringComparison.OrdinalIgnoreCase));
        
        return Math.Min(1.0, 0.3 + (matches * 0.15));
    }

    protected virtual List<Finding> ExtractFindings(string analysis)
    {
        // Simple extraction - could be enhanced with NLP
        var findings = new List<Finding>();
        var lines = analysis.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            if (line.Contains("issue", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("problem", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new Finding
                {
                    Type = "CodeIssue",
                    Description = line.Trim(),
                    Severity = DetermineSeverity(line)
                });
            }
        }
        
        return findings;
    }

    protected virtual List<Recommendation> ExtractRecommendations(string analysis)
    {
        // Simple extraction - could be enhanced with NLP
        var recommendations = new List<Recommendation>();
        var lines = analysis.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            if (line.Contains("recommend", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("suggest", StringComparison.OrdinalIgnoreCase))
            {
                recommendations.Add(new Recommendation
                {
                    Title = ExtractTitle(line),
                    Description = line.Trim(),
                    Priority = DeterminePriority(line),
                    Category = Type.ToString()
                });
            }
        }
        
        return recommendations;
    }

    private static string DetermineSeverity(string text) => text.ToLower() switch
    {
        var s when s.Contains("critical") || s.Contains("severe") => "CRITICAL",
        var s when s.Contains("major") || s.Contains("important") => "HIGH",
        var s when s.Contains("minor") || s.Contains("small") => "LOW",
        _ => "MEDIUM"
    };

    private static string DeterminePriority(string text) => text.ToLower() switch
    {
        var s when s.Contains("urgent") || s.Contains("immediately") => "CRITICAL",
        var s when s.Contains("important") || s.Contains("should") => "HIGH",
        var s when s.Contains("consider") || s.Contains("might") => "MEDIUM",
        _ => "LOW"
    };

    private static string ExtractTitle(string line)
    {
        // Extract first 50 characters as title
        var title = line.Trim();
        return title.Length > 50 ? title.Substring(0, 47) + "..." : title;
    }
}

/// <summary>
/// Default implementation for agents without specific implementations
/// </summary>
public class DefaultSpecializedAgent : BaseSpecializedAgent
{
    public override AgentType Type { get; }
    public override string Name => Type.ToString();
    public override string Description => $"Default implementation for {Type}";

    public DefaultSpecializedAgent(AgentType type, IClaudeService claudeService, ILogger logger)
        : base(claudeService, logger, new AgentConfiguration())
    {
        Type = type;
    }
}

// Specialized agent implementations with enhanced capabilities
public class SecurityExpertAgent : BaseSpecializedAgent
{
    public override AgentType Type => AgentType.SecurityExpert;
    public override string Name => "Security Expert";
    public override string Description => "Specialized in security vulnerability assessment and secure coding practices";

    public SecurityExpertAgent(IClaudeService claudeService, ILogger logger, AgentConfiguration configuration)
        : base(claudeService, logger, configuration) { }

    protected override AgentResult ParseAnalysisResult(string analysis)
    {
        // Enhanced parsing for security-specific JSON output
        try
        {
            return ParseSecurityAnalysisJson(analysis);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to parse JSON security analysis, falling back to text parsing");
            return base.ParseAnalysisResult(analysis);
        }
    }

    private AgentResult ParseSecurityAnalysisJson(string analysis)
    {
        // Attempt to extract JSON from the analysis
        var jsonStart = analysis.IndexOf('{');
        var jsonEnd = analysis.LastIndexOf('}');
        
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            var jsonContent = analysis.Substring(jsonStart, jsonEnd - jsonStart + 1);
            // Parse structured security analysis (implementation would depend on the JSON schema)
            // For now, return enhanced base parsing
        }
        
        return base.ParseAnalysisResult(analysis);
    }
}

public class PerformanceAnalystAgent : BaseSpecializedAgent
{
    public override AgentType Type => AgentType.PerformanceAnalyst;
    public override string Name => "Performance Analyst";
    public override string Description => "Specialized in performance optimization and efficiency analysis";

    public PerformanceAnalystAgent(IClaudeService claudeService, ILogger logger, AgentConfiguration configuration)
        : base(claudeService, logger, configuration) { }

    public override async Task<AgentResult> AnalyzeAsync(
        string content, 
        Dictionary<string, object> context, 
        CancellationToken cancellationToken = default)
    {
        // Add performance-specific context
        var enhancedContext = new Dictionary<string, object>(context)
        {
            ["AnalysisType"] = "Performance",
            ["FocusAreas"] = new[] { "Algorithmic Complexity", "Memory Usage", "I/O Operations", "Concurrency" }
        };
        
        var prompt = EnhancedPromptBuilder.BuildEnhancedAgentPrompt(Type, content, enhancedContext);
        
        var analysis = await ClaudeService.GenerateReviewWithParametersAsync(
            prompt, 
            Configuration.Temperature, 
            Configuration.MaxTokens, 
            cancellationToken);

        return ParseAnalysisResult(analysis);
    }
}

public class CodeQualityReviewerAgent : BaseSpecializedAgent
{
    public override AgentType Type => AgentType.CodeQualityReviewer;
    public override string Name => "Code Quality Reviewer";
    public override string Description => "Specialized in code quality, maintainability, and best practices";

    public CodeQualityReviewerAgent(IClaudeService claudeService, ILogger logger, AgentConfiguration configuration)
        : base(claudeService, logger, configuration) { }
}

public class ArchitectureExpertAgent : BaseSpecializedAgent
{
    public override AgentType Type => AgentType.ArchitectureExpert;
    public override string Name => "Architecture Expert";
    public override string Description => "Specialized in software architecture and design patterns";

    public ArchitectureExpertAgent(IClaudeService claudeService, ILogger logger, AgentConfiguration configuration)
        : base(claudeService, logger, configuration) { }
}

public class TestingSpecialistAgent : BaseSpecializedAgent
{
    public override AgentType Type => AgentType.TestingSpecialist;
    public override string Name => "Testing Specialist";
    public override string Description => "Specialized in testing strategies and quality assurance";

    public TestingSpecialistAgent(IClaudeService claudeService, ILogger logger, AgentConfiguration configuration)
        : base(claudeService, logger, configuration) { }
}

public class DomainExpertAgent : BaseSpecializedAgent
{
    public override AgentType Type => AgentType.DomainExpert;
    public override string Name => "Domain Expert";
    public override string Description => "Specialized in domain-driven design and business logic";

    public DomainExpertAgent(IClaudeService claudeService, ILogger logger, AgentConfiguration configuration)
        : base(claudeService, logger, configuration) { }
}

public class FeatureSlicingExpertAgent : BaseSpecializedAgent
{
    public override AgentType Type => AgentType.FeatureSlicingExpert;
    public override string Name => "Feature Slicing Expert";
    public override string Description => "Specialized in feature slicing and vertical architecture";

    public FeatureSlicingExpertAgent(IClaudeService claudeService, ILogger logger, AgentConfiguration configuration)
        : base(claudeService, logger, configuration) { }

    public override async Task<AgentResult> AnalyzeAsync(
        string content, 
        Dictionary<string, object> context, 
        CancellationToken cancellationToken = default)
    {
        // Add domain-driven design specific context
        var enhancedContext = new Dictionary<string, object>(context)
        {
            ["DomainFocus"] = "Feature Organization",
            ["ArchitecturalPatterns"] = new[] { "Vertical Slicing", "Domain Boundaries", "Feature Cohesion" },
            ["BusinessAlignment"] = context.TryGetValue("BusinessDomain", out var domain) ? domain : "General"
        };
        
        var prompt = EnhancedPromptBuilder.BuildEnhancedAgentPrompt(Type, content, enhancedContext);
        
        var analysis = await ClaudeService.GenerateReviewWithParametersAsync(
            prompt, 
            Configuration.Temperature, 
            Configuration.MaxTokens, 
            cancellationToken);

        return ParseAnalysisResult(analysis);
    }
}

public class DeveloperMentorAgent : BaseSpecializedAgent
{
    public override AgentType Type => AgentType.DeveloperMentor;
    public override string Name => "Developer Mentor";
    public override string Description => "Specialized in developer guidance and skill improvement";

    public DeveloperMentorAgent(IClaudeService claudeService, ILogger logger, AgentConfiguration configuration)
        : base(claudeService, logger, configuration) { }

    public override async Task<AgentResult> AnalyzeAsync(
        string content, 
        Dictionary<string, object> context, 
        CancellationToken cancellationToken = default)
    {
        // Add mentoring-specific context for adaptive prompting
        var enhancedContext = new Dictionary<string, object>(context)
        {
            ["MentoringFocus"] = "Developer Growth",
            ["LearningObjectives"] = new[] { "Skill Assessment", "Growth Opportunities", "Learning Path" }
        };
        
        // Determine code complexity and team experience for adaptive prompting
        var complexity = DetermineCodeComplexity(content);
        var teamLevel = context.TryGetValue("TeamExperience", out var exp) ? 
            Enum.Parse<TeamExperience>(exp.ToString() ?? "Mid") : TeamExperience.Mid;
        
        var prompt = EnhancedPromptBuilder.BuildAdaptivePrompt(Type, content, enhancedContext, complexity, teamLevel);
        
        var analysis = await ClaudeService.GenerateReviewWithParametersAsync(
            prompt, 
            0.4, // Higher temperature for more creative mentoring responses
            Configuration.MaxTokens, 
            cancellationToken);

        return ParseAnalysisResult(analysis);
    }

    private static CodeComplexity DetermineCodeComplexity(string content)
    {
        // Simple heuristic to determine code complexity
        var lines = content.Split('\n').Length;
        var complexity = content.Count(c => c == '{') + content.Count(c => c == '(');
        
        return (lines, complexity) switch
        {
            (< 50, < 20) => CodeComplexity.Low,
            (< 200, < 100) => CodeComplexity.Medium,
            _ => CodeComplexity.High
        };
    }
}

public class AICodeDetectiveAgent : BaseSpecializedAgent
{
    public override AgentType Type => AgentType.AICodeDetective;
    public override string Name => "AI Code Detective";
    public override string Description => "Specialized in detecting AI-generated code patterns and shortcuts";

    public AICodeDetectiveAgent(IClaudeService claudeService, ILogger logger, AgentConfiguration configuration)
        : base(claudeService, logger, configuration) { }

    public override async Task<AgentResult> AnalyzeAsync(
        string content, 
        Dictionary<string, object> context, 
        CancellationToken cancellationToken = default)
    {
        // Add AI detection specific context and patterns
        var enhancedContext = new Dictionary<string, object>(context)
        {
            ["DetectionTargets"] = new[] {
                "Commented out code blocks",
                "Simple bypass implementations", 
                "Deleted necessary code",
                "Manual EF migration edits",
                "Commented registrations in Program.cs",
                "Usage of paid NuGet packages"
            },
            ["SuspiciousPatterns"] = new[] {
                "// TODO: Implement properly",
                "// Temporary fix",
                "// AI generated",
                "throw new NotImplementedException"
            }
        };
        
        var prompt = EnhancedPromptBuilder.BuildEnhancedAgentPrompt(Type, content, enhancedContext);
        
        var analysis = await ClaudeService.GenerateReviewWithParametersAsync(
            prompt, 
            0.2, // Lower temperature for more focused detection
            Configuration.MaxTokens, 
            cancellationToken);

        return ParseAnalysisResult(analysis);
    }

    protected override double CalculateConfidenceScore(string analysis)
    {
        // Higher confidence for AI detection patterns
        var suspiciousIndicators = new[] {
            "commented out", "bypass", "todo", "temporary", 
            "not implemented", "placeholder", "ai generated"
        };
        
        var matches = suspiciousIndicators.Count(indicator => 
            analysis.Contains(indicator, StringComparison.OrdinalIgnoreCase));
        
        return Math.Min(1.0, 0.4 + (matches * 0.2));
    }
}