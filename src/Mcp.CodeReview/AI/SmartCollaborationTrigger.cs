using Mcp.CodeReview.Models;
using Microsoft.Extensions.Logging;

namespace Mcp.CodeReview.AI;

/// <summary>
/// Smart collaboration triggering system that determines optimal collaboration level
/// based on code characteristics, avoiding expensive full collaboration for simple changes
/// </summary>
public class SmartCollaborationTrigger
{
    private readonly ILogger<SmartCollaborationTrigger> _logger;
    private readonly CodeCharacteristicsAnalyzer _analyzer;

    public SmartCollaborationTrigger(ILogger<SmartCollaborationTrigger> logger)
    {
        _logger = logger;
        _analyzer = new CodeCharacteristicsAnalyzer();
    }

    /// <summary>
    /// Determines the optimal collaboration level based on code characteristics and context
    /// </summary>
    public async Task<CollaborationDecision> DetermineOptimalCollaborationAsync(
        CodeReviewRequest request,
        RepositoryContext repositoryContext,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🎯 SMART TRIGGER: Analyzing collaboration requirements for {FileName}", request.FileName);

        try
        {
            // Analyze code characteristics
            var characteristics = await _analyzer.AnalyzeCodeCharacteristics(
                request.Content, 
                request.Language, 
                request.Metadata);

            // Determine collaboration level
            var level = DetermineCollaborationLevel(characteristics, request.Options, repositoryContext);
            
            // Calculate expected metrics
            var metrics = EstimateCollaborationMetrics(level, characteristics);
            
            // Build decision reasoning
            var reasoning = BuildDecisionReasoning(level, characteristics, request.Options);

            var decision = new CollaborationDecision
            {
                Level = level,
                SelectedAgents = SelectAgentsForLevel(level, characteristics, request.RequestedAgents),
                EstimatedMetrics = metrics,
                Reasoning = reasoning,
                Characteristics = characteristics,
                Confidence = CalculateDecisionConfidence(level, characteristics)
            };

            _logger.LogInformation("🎯 COLLABORATION DECISION: {Level} level selected for {FileName} " +
                "(agents: {AgentCount}, estimated tokens: {Tokens}, time: {Time})",
                level, request.FileName, decision.SelectedAgents.Count, 
                metrics.EstimatedTokens, metrics.EstimatedDuration);

            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to determine collaboration level, falling back to Standard");
            return CreateFallbackDecision(request);
        }
    }

    private CollaborationLevel DetermineCollaborationLevel(
        CodeCharacteristics characteristics,
        ReviewOptions options,
        RepositoryContext repositoryContext)
    {
        var score = 0;
        var reasons = new List<string>();

        // Size and complexity factors
        if (characteristics.LinesChanged < 20)
        {
            score -= 2;
            reasons.Add("Small change (<20 lines)");
        }
        else if (characteristics.LinesChanged > 100)
        {
            score += 2;
            reasons.Add($"Large change ({characteristics.LinesChanged} lines)");
        }

        if (characteristics.Complexity < 3)
        {
            score -= 2;
            reasons.Add("Low complexity");
        }
        else if (characteristics.Complexity > 7)
        {
            score += 3;
            reasons.Add($"High complexity ({characteristics.Complexity})");
        }

        // Security and risk factors
        if (characteristics.HasSecurityPatterns)
        {
            score += 3;
            reasons.Add("Security patterns detected");
        }

        if (characteristics.HasDatabaseCalls)
        {
            score += 2;
            reasons.Add("Database interactions");
        }

        if (characteristics.HasAuthenticationLogic)
        {
            score += 3;
            reasons.Add("Authentication logic");
        }

        // Business domain factors
        if (options.BusinessDomain?.ToLower().Contains("financial") == true ||
            options.BusinessDomain?.ToLower().Contains("payment") == true)
        {
            score += 3;
            reasons.Add("Financial/payment domain");
        }

        if (options.BusinessDomain?.ToLower().Contains("healthcare") == true)
        {
            score += 2;
            reasons.Add("Healthcare domain");
        }

        // Architecture and design factors
        if (characteristics.ArchitecturalChanges)
        {
            score += 3;
            reasons.Add("Architectural changes");
        }

        if (characteristics.BusinessLogicHeavy)
        {
            score += 2;
            reasons.Add("Business logic heavy");
        }

        // Team and context factors
        if (options.TeamContext?.ToLower().Contains("junior") == true)
        {
            score += 1;
            reasons.Add("Junior team context");
        }

        // File type factors
        var fileName = Path.GetFileName(options.TeamContext ?? "").ToLower();
        if (fileName.Contains("controller") || fileName.Contains("api"))
        {
            score += 2;
            reasons.Add("Controller/API file");
        }

        if (fileName.Contains("test"))
        {
            score -= 2;
            reasons.Add("Test file");
        }

        if (fileName.Contains("config") || fileName.Contains("setting"))
        {
            score -= 3;
            reasons.Add("Configuration file");
        }

        // Review depth override
        if (options.ReviewDepth == "comprehensive")
        {
            score += 3;
            reasons.Add("Comprehensive review requested");
        }
        else if (options.ReviewDepth == "quick")
        {
            score -= 2;
            reasons.Add("Quick review requested");
        }

        // Determine level based on score
        var level = score switch
        {
            <= -3 => CollaborationLevel.Simple,    // Very low risk/complexity
            <= 0 => CollaborationLevel.Focused,    // Low risk, focused review
            <= 4 => CollaborationLevel.Standard,   // Moderate complexity
            <= 8 => CollaborationLevel.Enhanced,   // High complexity/risk
            _ => CollaborationLevel.Comprehensive   // Very high complexity/risk
        };

        _logger.LogInformation("🎯 COLLABORATION SCORING: {Score} points → {Level} level. Factors: {Factors}",
            score, level, string.Join(", ", reasons));

        return level;
    }

    private List<AgentType> SelectAgentsForLevel(
        CollaborationLevel level,
        CodeCharacteristics characteristics,
        List<AgentType> requestedAgents)
    {
        var agents = new List<AgentType>();

        // Always include core quality reviewer
        agents.Add(AgentType.CodeQualityReviewer);

        switch (level)
        {
            case CollaborationLevel.Simple:
                // Just quality review for simple changes
                if (characteristics.HasSecurityPatterns)
                    agents.Add(AgentType.SecurityExpert);
                break;

            case CollaborationLevel.Focused:
                // Add specialist based on primary concern
                if (characteristics.HasSecurityPatterns || characteristics.HasAuthenticationLogic)
                    agents.Add(AgentType.SecurityExpert);
                else if (characteristics.PerformanceCritical)
                    agents.Add(AgentType.PerformanceAnalyst);
                else if (characteristics.ArchitecturalChanges)
                    agents.Add(AgentType.ArchitectureExpert);
                break;

            case CollaborationLevel.Standard:
                // Standard multi-agent team
                agents.Add(AgentType.SecurityExpert);
                agents.Add(AgentType.PerformanceAnalyst);
                if (characteristics.ArchitecturalChanges)
                    agents.Add(AgentType.ArchitectureExpert);
                break;

            case CollaborationLevel.Enhanced:
                // Enhanced team with domain expertise
                agents.Add(AgentType.SecurityExpert);
                agents.Add(AgentType.PerformanceAnalyst);
                agents.Add(AgentType.ArchitectureExpert);
                if (characteristics.BusinessLogicHeavy)
                    agents.Add(AgentType.DomainExpert);
                break;

            case CollaborationLevel.Comprehensive:
                // Full team for complex reviews
                agents.AddRange(new[]
                {
                    AgentType.SecurityExpert,
                    AgentType.PerformanceAnalyst,
                    AgentType.ArchitectureExpert,
                    AgentType.DomainExpert,
                    AgentType.TestingSpecialist
                });
                break;
        }

        // Honor specific agent requests when reasonable
        if (requestedAgents?.Any() == true)
        {
            foreach (var requested in requestedAgents)
            {
                if (!agents.Contains(requested) && agents.Count < 6) // Cap at 6 agents
                {
                    agents.Add(requested);
                }
            }
        }

        return agents.Distinct().ToList();
    }

    private CollaborationMetrics EstimateCollaborationMetrics(
        CollaborationLevel level,
        CodeCharacteristics characteristics)
    {
        var baseMetrics = level switch
        {
            CollaborationLevel.Simple => new CollaborationMetrics 
            { 
                EstimatedTokens = 2_500, 
                EstimatedDuration = TimeSpan.FromSeconds(15),
                EstimatedApiCalls = 2,
                CollaborationPhases = 1
            },
            CollaborationLevel.Focused => new CollaborationMetrics 
            { 
                EstimatedTokens = 6_000, 
                EstimatedDuration = TimeSpan.FromSeconds(45),
                EstimatedApiCalls = 4,
                CollaborationPhases = 3
            },
            CollaborationLevel.Standard => new CollaborationMetrics 
            { 
                EstimatedTokens = 12_000, 
                EstimatedDuration = TimeSpan.FromMinutes(1.5),
                EstimatedApiCalls = 8,
                CollaborationPhases = 4
            },
            CollaborationLevel.Enhanced => new CollaborationMetrics 
            { 
                EstimatedTokens = 20_000, 
                EstimatedDuration = TimeSpan.FromMinutes(2.5),
                EstimatedApiCalls = 15,
                CollaborationPhases = 5
            },
            CollaborationLevel.Comprehensive => new CollaborationMetrics 
            { 
                EstimatedTokens = 35_000, 
                EstimatedDuration = TimeSpan.FromMinutes(4),
                EstimatedApiCalls = 25,
                CollaborationPhases = 6
            },
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };

        // Adjust for complexity
        var complexityMultiplier = 1.0 + (characteristics.Complexity / 10.0);
        baseMetrics.EstimatedTokens = (int)(baseMetrics.EstimatedTokens * complexityMultiplier);
        baseMetrics.EstimatedDuration = TimeSpan.FromMilliseconds(
            baseMetrics.EstimatedDuration.TotalMilliseconds * complexityMultiplier);

        return baseMetrics;
    }

    private string BuildDecisionReasoning(
        CollaborationLevel level,
        CodeCharacteristics characteristics,
        ReviewOptions options)
    {
        var reasoning = new List<string>
        {
            $"Selected {level} collaboration level based on:",
            $"• Code complexity: {characteristics.Complexity}/10",
            $"• Lines changed: {characteristics.LinesChanged}",
            $"• Security patterns: {(characteristics.HasSecurityPatterns ? "Yes" : "No")}",
            $"• Business domain: {options.BusinessDomain ?? "General"}",
            $"• Review depth: {options.ReviewDepth}"
        };

        if (characteristics.ArchitecturalChanges)
            reasoning.Add("• Architectural changes detected");
        if (characteristics.BusinessLogicHeavy)
            reasoning.Add("• Business logic heavy code");
        if (characteristics.PerformanceCritical)
            reasoning.Add("• Performance critical operations");

        return string.Join("\n", reasoning);
    }

    private double CalculateDecisionConfidence(
        CollaborationLevel level, 
        CodeCharacteristics characteristics)
    {
        // Base confidence depends on how clear-cut the decision is
        var confidence = 0.8;

        // High confidence for clear simple cases
        if (level == CollaborationLevel.Simple && 
            characteristics.LinesChanged < 20 && 
            characteristics.Complexity < 3)
        {
            confidence = 0.95;
        }

        // High confidence for clear complex cases
        if (level == CollaborationLevel.Comprehensive && 
            (characteristics.Complexity > 8 || characteristics.HasSecurityPatterns))
        {
            confidence = 0.9;
        }

        // Lower confidence for borderline cases
        if (characteristics.Complexity >= 4 && characteristics.Complexity <= 6)
        {
            confidence = 0.7;
        }

        return confidence;
    }

    private CollaborationDecision CreateFallbackDecision(CodeReviewRequest request)
    {
        _logger.LogWarning("🚨 FALLBACK DECISION: Using Standard collaboration level");
        
        return new CollaborationDecision
        {
            Level = CollaborationLevel.Standard,
            SelectedAgents = new List<AgentType> 
            { 
                AgentType.CodeQualityReviewer, 
                AgentType.SecurityExpert, 
                AgentType.PerformanceAnalyst 
            },
            EstimatedMetrics = new CollaborationMetrics
            {
                EstimatedTokens = 12_000,
                EstimatedDuration = TimeSpan.FromMinutes(2),
                EstimatedApiCalls = 8,
                CollaborationPhases = 4
            },
            Reasoning = "Fallback to Standard level due to analysis error",
            Confidence = 0.5,
            IsFallback = true
        };
    }
}

/// <summary>
/// Collaboration levels with increasing complexity and resource usage
/// </summary>
public enum CollaborationLevel
{
    /// <summary>Simple parallel review (2-3K tokens, 15-30s, 1-2 agents)</summary>
    Simple,
    
    /// <summary>Focused collaboration (6K tokens, 45s, 2-3 agents, 3 phases)</summary>
    Focused,
    
    /// <summary>Standard collaboration (12K tokens, 1.5min, 3-4 agents, 4 phases)</summary>
    Standard,
    
    /// <summary>Enhanced collaboration (20K tokens, 2.5min, 4-5 agents, 5 phases)</summary>
    Enhanced,
    
    /// <summary>Comprehensive collaboration (35K tokens, 4min, 5-6 agents, 6 phases)</summary>
    Comprehensive
}

/// <summary>
/// Decision result from smart collaboration triggering
/// </summary>
public class CollaborationDecision
{
    public CollaborationLevel Level { get; set; }
    public List<AgentType> SelectedAgents { get; set; } = new();
    public CollaborationMetrics EstimatedMetrics { get; set; } = new();
    public string Reasoning { get; set; } = string.Empty;
    public CodeCharacteristics? Characteristics { get; set; }
    public double Confidence { get; set; }
    public bool IsFallback { get; set; }
    public DateTime DecisionTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Estimated metrics for collaboration level
/// </summary>
public class CollaborationMetrics
{
    public int EstimatedTokens { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
    public int EstimatedApiCalls { get; set; }
    public int CollaborationPhases { get; set; }
    public double EstimatedCost => EstimatedTokens * 0.00003; // Approximate cost in USD
}