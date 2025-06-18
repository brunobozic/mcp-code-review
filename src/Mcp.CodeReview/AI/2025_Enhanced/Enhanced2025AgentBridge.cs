using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.AI.Enhanced2025;

/// <summary>
/// Bridge adapter that makes Enhanced 2025 agents compatible with existing ISpecializedAgent interface
/// while preserving all enhanced capabilities for 2025 features
/// </summary>
public class Enhanced2025AgentBridge : ISpecializedAgent
{
    private readonly IEnhanced2025Agent _enhanced2025Agent;

    public Enhanced2025AgentBridge(IEnhanced2025Agent enhanced2025Agent)
    {
        _enhanced2025Agent = enhanced2025Agent ?? throw new ArgumentNullException(nameof(enhanced2025Agent));
    }

    public AgentType Type => _enhanced2025Agent.AgentType;
    
    public string Name => $"Enhanced2025_{_enhanced2025Agent.AgentType}";
    
    public string Description => $"2025 Enhanced AI Agent - {_enhanced2025Agent.Specialization}";

    /// <summary>
    /// Adapts Enhanced 2025 analysis to standard AgentResult format
    /// while preserving enhanced metadata in context
    /// </summary>
    public async Task<AgentResult> AnalyzeAsync(string content, Dictionary<string, object> context, CancellationToken cancellationToken = default)
    {
        // Create a basic CodeReviewRequest from the content and context
        var request = new CodeReviewRequest
        {
            Content = content,
            FileName = context.GetValueOrDefault("fileName", "unknown.cs")?.ToString() ?? "unknown.cs",
            Language = context.GetValueOrDefault("language", "csharp")?.ToString() ?? "csharp",
            FilePath = context.GetValueOrDefault("filePath", "/unknown")?.ToString() ?? "/unknown"
        };

        // Perform enhanced analysis
        var enhancedResult = await _enhanced2025Agent.AnalyzeWithReasoningAsync(request, cancellationToken);

        // Convert Enhanced2025AnalysisResult to standard AgentResult
        var agentResult = new AgentResult
        {
            Type = Type,
            Name = Name,
            Findings = enhancedResult.Findings,
            Summary = GenerateSummary(enhancedResult),
            Confidence = enhancedResult.OverallConfidence,
            ReasoningChain = enhancedResult.ReasoningChain.Select(r => r.Description).ToList(),
            ExecutionTime = enhancedResult.AnalysisDuration,
            Metadata = CreateEnhancedMetadata(enhancedResult),
            Recommendations = enhancedResult.Recommendations.Select(r => r.Description).ToList()
        };

        return agentResult;
    }

    public bool CanHandle(string contentType)
    {
        // Enhanced 2025 agents can handle more content types
        return contentType.ToLowerInvariant() switch
        {
            "csharp" or "c#" or "cs" => true,
            "javascript" or "js" or "typescript" or "ts" => true,
            "python" or "py" => true,
            "java" => true,
            "go" => true,
            "rust" or "rs" => true,
            "cpp" or "c++" => true,
            _ => false
        };
    }

    /// <summary>
    /// Access the underlying Enhanced 2025 agent for advanced operations
    /// </summary>
    public IEnhanced2025Agent GetEnhancedAgent() => _enhanced2025Agent;

    private string GenerateSummary(Enhanced2025AnalysisResult enhancedResult)
    {
        var summary = $"Enhanced 2025 Analysis completed with {enhancedResult.OverallConfidence:P1} confidence. ";
        summary += $"Found {enhancedResult.Findings.Count} findings across {enhancedResult.ReasoningChain.Count} reasoning steps. ";
        
        if (enhancedResult.Limitations.Any())
        {
            summary += $"Identified {enhancedResult.Limitations.Count} analysis limitations. ";
        }
        
        if (enhancedResult.AlternativeViews.Any())
        {
            summary += $"Considered {enhancedResult.AlternativeViews.Count} alternative interpretations. ";
        }

        return summary;
    }

    private Dictionary<string, object> CreateEnhancedMetadata(Enhanced2025AnalysisResult enhancedResult)
    {
        return new Dictionary<string, object>
        {
            ["enhanced2025_version"] = "1.0",
            ["overall_confidence"] = enhancedResult.OverallConfidence,
            ["reasoning_steps_count"] = enhancedResult.ReasoningChain.Count,
            ["limitations_count"] = enhancedResult.Limitations.Count,
            ["alternative_views_count"] = enhancedResult.AlternativeViews.Count,
            ["uncertainty_factors_count"] = enhancedResult.UncertaintyFactors.Count,
            ["self_criticisms_count"] = enhancedResult.SelfCriticisms.Count,
            ["analysis_timestamp"] = enhancedResult.AnalysisTimestamp,
            ["minimum_confidence_threshold"] = _enhanced2025Agent.MinimumConfidenceThreshold,
            ["specialization"] = _enhanced2025Agent.Specialization,
            
            // Detailed enhanced data for consumers that can handle it
            ["enhanced_reasoning_chain"] = enhancedResult.ReasoningChain,
            ["enhanced_limitations"] = enhancedResult.Limitations,
            ["enhanced_alternative_views"] = enhancedResult.AlternativeViews,
            ["enhanced_uncertainty_factors"] = enhancedResult.UncertaintyFactors,
            ["enhanced_evidence"] = enhancedResult.SupportingEvidence,
            ["enhanced_recommendations"] = enhancedResult.Recommendations,
            ["enhanced_self_criticisms"] = enhancedResult.SelfCriticisms,
            ["finding_confidences"] = enhancedResult.FindingConfidences
        };
    }
}

/// <summary>
/// Enhanced 2025 Agent Orchestrator that bridges between standard and enhanced interfaces
/// </summary>
public class Enhanced2025OrchestrationBridge : IAgentOrchestrator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Enhanced2025OrchestrationBridge> _logger;
    private readonly Dictionary<AgentType, IEnhanced2025Agent> _enhanced2025Agents;

    public Enhanced2025OrchestrationBridge(IServiceProvider serviceProvider, ILogger<Enhanced2025OrchestrationBridge> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _enhanced2025Agents = new Dictionary<AgentType, IEnhanced2025Agent>();
    }

    public async Task<AgentExecutionResult[]> ExecuteAgentsParallelAsync(
        IEnumerable<AgentExecutionTask> agentTasks, 
        CancellationToken cancellationToken = default)
    {
        var tasks = agentTasks.Select(async task =>
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var agent = CreateAgent(task.AgentType, new AgentConfiguration());
                var result = await agent.AnalyzeAsync(task.Content, task.Context, cancellationToken);
                
                return new AgentExecutionResult
                {
                    AgentType = task.AgentType,
                    Result = result,
                    ExecutionTime = DateTime.UtcNow - startTime,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing agent {AgentType}", task.AgentType);
                return new AgentExecutionResult
                {
                    AgentType = task.AgentType,
                    Result = new AgentResult { Type = task.AgentType, Name = task.AgentType.ToString() },
                    ExecutionTime = DateTime.UtcNow - startTime,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        });

        return await Task.WhenAll(tasks);
    }

    public ISpecializedAgent CreateAgent(AgentType agentType, AgentConfiguration configuration)
    {
        // Try to get Enhanced 2025 agent first
        if (_enhanced2025Agents.TryGetValue(agentType, out var enhanced2025Agent))
        {
            return new Enhanced2025AgentBridge(enhanced2025Agent);
        }

        // Fall back to creating Enhanced 2025 agents as needed
        enhanced2025Agent = agentType switch
        {
            AgentType.Security => _serviceProvider.GetService<Enhanced2025SecurityAgent>(),
            AgentType.Performance => _serviceProvider.GetService<Enhanced2025PerformanceAgent>(),
            AgentType.CodeQuality => _serviceProvider.GetService<Enhanced2025CodeQualityAgent>(),
            AgentType.Architecture => _serviceProvider.GetService<Enhanced2025ArchitectureAgent>(),
            _ => null
        };

        if (enhanced2025Agent != null)
        {
            _enhanced2025Agents[agentType] = enhanced2025Agent;
            return new Enhanced2025AgentBridge(enhanced2025Agent);
        }

        // If Enhanced 2025 agent not available, fall back to standard agents
        _logger.LogWarning("Enhanced 2025 agent not available for {AgentType}, using standard agent", agentType);
        
        return agentType switch
        {
            AgentType.Security => _serviceProvider.GetRequiredService<SecurityAgent>(),
            AgentType.Performance => _serviceProvider.GetRequiredService<PerformanceAgent>(),
            AgentType.CodeQuality => _serviceProvider.GetRequiredService<CodeQualityAgent>(),
            AgentType.Architecture => _serviceProvider.GetRequiredService<ArchitectureStandardsAgent>(),
            _ => throw new ArgumentException($"Unknown agent type: {agentType}")
        };
    }
}