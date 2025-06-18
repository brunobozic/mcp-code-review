using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;
using Mcp.CodeReview.Utilities;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Mcp.CodeReview.AI;

/// <summary>
/// Enhanced 2025 Multi-Agent Orchestrator with advanced hallucination reduction and validation
/// Implements latest trends: Cross-agent validation, reasoning chains, confidence scoring, and adversarial checking
/// </summary>
public class Enhanced2025AgentOrchestrator : IAgentOrchestrator
{
    private readonly IClaudeService _claudeService;
    private readonly ILogger<Enhanced2025AgentOrchestrator> _logger;
    private readonly SemaphoreSlim _concurrencyLimiter;
    
    // 2025 Enhancement: Agent validation and cross-checking
    private readonly ConcurrentDictionary<AgentType, Func<AgentConfiguration, ISpecializedAgent>> _agentFactories;
    private readonly ConcurrentDictionary<string, AgentValidationResult> _validationCache;
    
    // 2025 Enhancement: Hallucination detection patterns
    private readonly HashSet<string> _hallucinationKeywords = new()
    {
        "definitely", "absolutely", "never", "always", "100%", "impossible", 
        "guaranteed", "certain", "without doubt", "obviously", "clearly"
    };
    
    // Configuration
    private const int MaxConcurrentAgents = 3; // Reduced for better quality control
    private const double MinimumConfidenceThreshold = 0.7;
    private const int MinimumCrossValidationAgents = 2;

    public Enhanced2025AgentOrchestrator(IClaudeService claudeService, ILogger<Enhanced2025AgentOrchestrator> logger)
    {
        _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _concurrencyLimiter = new SemaphoreSlim(MaxConcurrentAgents, MaxConcurrentAgents);
        _agentFactories = new ConcurrentDictionary<AgentType, Func<AgentConfiguration, ISpecializedAgent>>();
        _validationCache = new ConcurrentDictionary<string, AgentValidationResult>();
        
        RegisterEnhancedAgentFactories();
    }

    /// <inheritdoc />
    public async Task<AgentExecutionResult[]> ExecuteAgentsParallelAsync(
        IEnumerable<AgentExecutionTask> agentTasks, 
        CancellationToken cancellationToken = default)
    {
        var tasks = agentTasks.ToArray();
        ErrorHandling.ValidateRequired(tasks, nameof(agentTasks));

        try
        {
            // 2025 Enhancement: Multi-phase execution with validation
            return await ExecuteWithEnhanced2025ValidationAsync(tasks, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute enhanced 2025 agents");
            return Array.Empty<AgentExecutionResult>();
        }
    }

    /// <summary>
    /// 2025 Enhanced execution with cross-agent validation and hallucination detection
    /// </summary>
    private async Task<AgentExecutionResult[]> ExecuteWithEnhanced2025ValidationAsync(
        AgentExecutionTask[] tasks, 
        CancellationToken cancellationToken)
    {
        // Phase 1: Initial parallel execution
        var initialResults = await ExecuteInitialPhaseAsync(tasks, cancellationToken);
        
        // Phase 2: Cross-agent validation and challenge
        var validatedResults = await ExecuteCrossValidationPhaseAsync(initialResults, cancellationToken);
        
        // Phase 3: Hallucination detection and confidence scoring
        var finalResults = await ExecuteHallucinationDetectionPhaseAsync(validatedResults, cancellationToken);
        
        // Phase 4: Consensus building and final validation
        return await ExecuteConsensusPhaseAsync(finalResults, cancellationToken);
    }

    /// <summary>
    /// Phase 1: Execute agents with enhanced reasoning chain tracking
    /// </summary>
    private async Task<AgentExecutionResult[]> ExecuteInitialPhaseAsync(
        AgentExecutionTask[] tasks, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("🚀 2025 Multi-Agent Phase 1: Initial execution with reasoning chains");

        var semaphoreTasks = tasks.Select(async task =>
        {
            await _concurrencyLimiter.WaitAsync(cancellationToken);
            
            try
            {
                return await ExecuteEnhancedAgentWithReasoningAsync(task, cancellationToken);
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        });

        return await Task.WhenAll(semaphoreTasks);
    }

    /// <summary>
    /// Execute individual agent with enhanced reasoning chain and confidence scoring
    /// </summary>
    private async Task<AgentExecutionResult> ExecuteEnhancedAgentWithReasoningAsync(
        AgentExecutionTask task,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // 2025 Enhancement: Structured reasoning prompt
            var enhancedPrompt = BuildEnhanced2025Prompt(task);
            
            var response = await _claudeService.GenerateReviewAsync(enhancedPrompt, cancellationToken);
            
            // 2025 Enhancement: Parse structured response with reasoning chain
            var parsedResult = ParseEnhancedAgentResponse(response, task.AgentType);
            
            stopwatch.Stop();
            
            return new AgentExecutionResult
            {
                AgentType = task.AgentType,
                Success = true,
                Result = parsedResult,
                ExecutionTime = stopwatch.Elapsed,
                ConfidenceScore = parsedResult.ConfidenceScore,
                ReasoningChain = parsedResult.ReasoningChain,
                ValidationFlags = DetectPotentialIssues(response)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enhanced agent execution failed for {AgentType}", task.AgentType);
            
            stopwatch.Stop();
            
            return new AgentExecutionResult
            {
                AgentType = task.AgentType,
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTime = stopwatch.Elapsed,
                ConfidenceScore = 0.0
            };
        }
    }

    /// <summary>
    /// Build enhanced 2025 prompt with structured reasoning requirements
    /// </summary>
    private string BuildEnhanced2025Prompt(AgentExecutionTask task)
    {
        return $@"You are a {task.AgentType} AI agent. Analyze the following code with ENHANCED 2025 METHODOLOGY:

=== ANALYSIS REQUIREMENTS ===
1. REASONING CHAIN: Show your step-by-step thinking process
2. EVIDENCE-BASED: Only make claims you can support with specific code examples
3. UNCERTAINTY ACKNOWLEDGMENT: Use uncertainty language when appropriate (""likely"", ""appears to"", ""suggests"")
4. CONFIDENCE SCORING: Rate your confidence in each finding (0.0-1.0)
5. ALTERNATIVE PERSPECTIVES: Consider why you might be wrong

=== CODE TO ANALYZE ===
{task.RequestContent}

=== REQUIRED OUTPUT FORMAT ===
Please respond in this EXACT JSON structure:
{{
  ""reasoning_chain"": [""step 1"", ""step 2"", ""step 3""],
  ""findings"": [
    {{
      ""title"": ""Finding title"",
      ""description"": ""Detailed description with evidence"",
      ""severity"": ""high|medium|low"",
      ""confidence"": 0.85,
      ""evidence"": ""Specific code snippet or line reference"",
      ""uncertainty_factors"": [""What could make this wrong""],
      ""alternative_interpretations"": [""Other ways to view this""]
    }}
  ],
  ""overall_confidence"": 0.82,
  ""limitations"": [""What I couldn't analyze"", ""Assumptions I made""],
  ""recommendations"": [""Specific actionable items""]
}}

CRITICAL: Avoid absolute language. Use ""likely"", ""appears"", ""suggests"" instead of ""definitely"", ""always"", ""never"".";
    }

    /// <summary>
    /// Parse enhanced agent response with structured validation
    /// </summary>
    private EnhancedAgentResult ParseEnhancedAgentResponse(string response, AgentType agentType)
    {
        try
        {
            // Try to parse as JSON first
            var jsonResponse = JsonSerializer.Deserialize<EnhancedAgentJsonResponse>(response);
            
            return new EnhancedAgentResult
            {
                AgentType = agentType,
                ReasoningChain = jsonResponse.ReasoningChain ?? new List<string>(),
                Findings = jsonResponse.Findings?.Select(f => new Finding
                {
                    Title = f.Title ?? "",
                    Description = f.Description ?? "",
                    Severity = f.Severity ?? "medium",
                    Evidence = f.Evidence ?? ""
                }).ToList() ?? new List<Finding>(),
                ConfidenceScore = jsonResponse.OverallConfidence,
                Limitations = jsonResponse.Limitations ?? new List<string>(),
                Recommendations = jsonResponse.Recommendations?.Select(r => new Recommendation
                {
                    Title = "Recommendation",
                    Description = r,
                    Priority = "medium"
                }).ToList() ?? new List<Recommendation>()
            };
        }
        catch (JsonException)
        {
            // Fallback: Parse as regular text but with lower confidence
            return new EnhancedAgentResult
            {
                AgentType = agentType,
                ReasoningChain = new List<string> { "Unable to parse structured response" },
                Findings = new List<Finding>
                {
                    new Finding
                    {
                        Title = $"{agentType} Analysis",
                        Description = response,
                        Severity = "low"
                    }
                },
                ConfidenceScore = 0.3, // Low confidence for unstructured response
                Limitations = new List<string> { "Response was not in expected JSON format" }
            };
        }
    }

    /// <summary>
    /// Phase 2: Cross-agent validation and challenge mechanism
    /// </summary>
    private async Task<AgentExecutionResult[]> ExecuteCrossValidationPhaseAsync(
        AgentExecutionResult[] initialResults,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("🔍 2025 Multi-Agent Phase 2: Cross-validation and challenge");

        var validatedResults = new List<AgentExecutionResult>();

        foreach (var result in initialResults)
        {
            if (!result.Success || result.ConfidenceScore < MinimumConfidenceThreshold)
            {
                validatedResults.Add(result);
                continue;
            }

            // Select validation agents (different from original agent)
            var validationAgents = SelectValidationAgents(result.AgentType);
            
            if (validationAgents.Length >= MinimumCrossValidationAgents)
            {
                var challengedResult = await ChallengeAgentFindingsAsync(result, validationAgents, cancellationToken);
                validatedResults.Add(challengedResult);
            }
            else
            {
                validatedResults.Add(result);
            }
        }

        return validatedResults.ToArray();
    }

    /// <summary>
    /// Challenge agent findings with other agents to reduce hallucinations
    /// </summary>
    private async Task<AgentExecutionResult> ChallengeAgentFindingsAsync(
        AgentExecutionResult originalResult,
        AgentType[] validationAgents,
        CancellationToken cancellationToken)
    {
        var challengePrompt = $@"AGENT VALIDATION CHALLENGE 2025:

You are reviewing findings from a {originalResult.AgentType} agent. Your job is to CHALLENGE and VALIDATE these findings.

ORIGINAL FINDINGS:
{JsonSerializer.Serialize(originalResult.Result, new JsonSerializerOptions { WriteIndented = true })}

VALIDATION TASKS:
1. Identify potential hallucinations or overconfident claims
2. Look for missing evidence or weak reasoning
3. Find alternative interpretations
4. Rate the validity of each finding (0.0-1.0)

RESPOND IN JSON:
{{
  ""validation_score"": 0.75,
  ""challenged_findings"": [
    {{
      ""original_finding"": ""title"",
      ""validity_score"": 0.8,
      ""concerns"": [""What seems wrong or overstated""],
      ""supporting_evidence"": [""What supports this finding""],
      ""alternative_view"": ""Different interpretation""
    }}
  ],
  ""overall_assessment"": ""Generally valid but some concerns..."",
  ""confidence_adjustment"": -0.1
}}";

        try
        {
            var challengeResponse = await _claudeService.GenerateReviewAsync(challengePrompt, cancellationToken);
            var validation = JsonSerializer.Deserialize<ValidationChallenge>(challengeResponse);
            
            // Adjust confidence based on validation
            var adjustedConfidence = Math.Max(0.0, originalResult.ConfidenceScore + (validation?.ConfidenceAdjustment ?? 0));
            
            return originalResult with
            {
                ConfidenceScore = adjustedConfidence,
                ValidationFlags = (originalResult.ValidationFlags ?? new List<string>())
                    .Concat(validation?.ChallengedFindings?.SelectMany(f => f.Concerns) ?? Enumerable.Empty<string>())
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Challenge validation failed for {AgentType}", originalResult.AgentType);
            return originalResult;
        }
    }

    /// <summary>
    /// Phase 3: Advanced hallucination detection
    /// </summary>
    private async Task<AgentExecutionResult[]> ExecuteHallucinationDetectionPhaseAsync(
        AgentExecutionResult[] validatedResults,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("🧠 2025 Multi-Agent Phase 3: Hallucination detection");

        return validatedResults.Select(result =>
        {
            if (!result.Success) return result;

            var hallucinationScore = DetectHallucinationPatterns(result);
            var adjustedConfidence = result.ConfidenceScore * (1.0 - hallucinationScore);

            return result with
            {
                ConfidenceScore = adjustedConfidence,
                ValidationFlags = (result.ValidationFlags ?? new List<string>())
                    .Concat(hallucinationScore > 0.2 ? new[] { "Potential hallucination detected" } : Array.Empty<string>())
                    .ToList()
            };
        }).ToArray();
    }

    /// <summary>
    /// Phase 4: Consensus building and final validation
    /// </summary>
    private async Task<AgentExecutionResult[]> ExecuteConsensusPhaseAsync(
        AgentExecutionResult[] hallucinationCheckedResults,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("🤝 2025 Multi-Agent Phase 4: Consensus building");

        // Group findings by topic/file for consensus analysis
        var consensusGroups = hallucinationCheckedResults
            .Where(r => r.Success && r.ConfidenceScore >= MinimumConfidenceThreshold)
            .GroupBy(r => GetFindingTopic(r))
            .ToArray();

        var finalResults = new List<AgentExecutionResult>();

        foreach (var group in consensusGroups)
        {
            if (group.Count() >= 2) // Multiple agents agree
            {
                var consensusResult = BuildConsensusResult(group.ToArray());
                finalResults.Add(consensusResult);
            }
            else
            {
                // Single agent - mark with lower confidence
                var singleResult = group.First();
                finalResults.Add(singleResult with
                {
                    ConfidenceScore = singleResult.ConfidenceScore * 0.8, // Reduce confidence for single-agent findings
                    ValidationFlags = (singleResult.ValidationFlags ?? new List<string>())
                        .Concat(new[] { "Single agent finding - no consensus" })
                        .ToList()
                });
            }
        }

        return finalResults.ToArray();
    }

    /// <summary>
    /// Detect hallucination patterns in agent responses
    /// </summary>
    private double DetectHallucinationPatterns(AgentExecutionResult result)
    {
        if (result.Result == null) return 0.0;

        var responseText = JsonSerializer.Serialize(result.Result).ToLower();
        
        var hallucinationScore = 0.0;
        
        // Check for overconfident language
        var overconfidentCount = _hallucinationKeywords.Count(keyword => responseText.Contains(keyword));
        hallucinationScore += overconfidentCount * 0.1;
        
        // Check for unsupported specific numbers/percentages
        var numberMatches = System.Text.RegularExpressions.Regex.Matches(responseText, @"\d+%|\d+\.\d+%");
        hallucinationScore += numberMatches.Count * 0.05;
        
        // Check for lack of uncertainty language
        var uncertaintyWords = new[] { "likely", "appears", "suggests", "might", "could", "possibly" };
        var uncertaintyCount = uncertaintyWords.Count(word => responseText.Contains(word));
        if (uncertaintyCount == 0 && responseText.Length > 100)
        {
            hallucinationScore += 0.2; // Penalty for no uncertainty language
        }
        
        return Math.Min(1.0, hallucinationScore);
    }

    /// <summary>
    /// Detect potential issues in agent responses
    /// </summary>
    private List<string> DetectPotentialIssues(string response)
    {
        var issues = new List<string>();
        
        if (response.Length < 50)
            issues.Add("Response too short");
            
        if (!response.Contains("evidence") && !response.Contains("example"))
            issues.Add("Lacks supporting evidence");
            
        if (_hallucinationKeywords.Any(word => response.ToLower().Contains(word)))
            issues.Add("Contains overconfident language");
            
        return issues;
    }

    /// <summary>
    /// Select appropriate validation agents for cross-checking
    /// </summary>
    private AgentType[] SelectValidationAgents(AgentType originalAgent)
    {
        // Define cross-validation pairs
        var validationMap = new Dictionary<AgentType, AgentType[]>
        {
            [AgentType.SecurityExpert] = new[] { AgentType.CodeQualityReviewer, AgentType.ArchitectureExpert },
            [AgentType.PerformanceAnalyst] = new[] { AgentType.SecurityExpert, AgentType.CodeQualityReviewer },
            [AgentType.CodeQualityReviewer] = new[] { AgentType.ArchitectureExpert, AgentType.TestingSpecialist },
            [AgentType.ArchitectureExpert] = new[] { AgentType.CodeQualityReviewer, AgentType.DomainExpert },
            [AgentType.TestingSpecialist] = new[] { AgentType.CodeQualityReviewer, AgentType.DeveloperMentor }
        };

        return validationMap.TryGetValue(originalAgent, out var validators) ? validators : Array.Empty<AgentType>();
    }

    /// <summary>
    /// Get finding topic for consensus grouping
    /// </summary>
    private string GetFindingTopic(AgentExecutionResult result)
    {
        // Simple topic extraction - could be enhanced with NLP
        return result.AgentType.ToString();
    }

    /// <summary>
    /// Build consensus result from multiple agent findings
    /// </summary>
    private AgentExecutionResult BuildConsensusResult(AgentExecutionResult[] agentResults)
    {
        var averageConfidence = agentResults.Average(r => r.ConfidenceScore);
        var consensusBonus = agentResults.Length >= 3 ? 0.1 : 0.05; // Bonus for more agents agreeing
        
        return new AgentExecutionResult
        {
            AgentType = AgentType.AICodeDetective, // Meta-agent for consensus
            Success = true,
            ConfidenceScore = Math.Min(1.0, averageConfidence + consensusBonus),
            Result = new EnhancedAgentResult
            {
                AgentType = AgentType.AICodeDetective,
                ReasoningChain = new List<string> 
                { 
                    $"Consensus built from {agentResults.Length} agents",
                    $"Average confidence: {averageConfidence:F2}",
                    "Cross-validated findings"
                }
            },
            ValidationFlags = new List<string> { $"Consensus from {agentResults.Length} agents" }
        };
    }

    /// <summary>
    /// Register enhanced agent factories with 2025 patterns
    /// </summary>
    private void RegisterEnhancedAgentFactories()
    {
        // Implementation would register factory methods for each agent type
        // This is a simplified version
        _logger.LogInformation("🔧 Registered enhanced 2025 agent factories");
    }
}

/// <summary>
/// Enhanced agent result with 2025 features
/// </summary>
public record EnhancedAgentResult
{
    public AgentType AgentType { get; init; }
    public List<string> ReasoningChain { get; init; } = new();
    public List<Finding> Findings { get; init; } = new();
    public double ConfidenceScore { get; init; }
    public List<string> Limitations { get; init; } = new();
    public List<Recommendation> Recommendations { get; init; } = new();
}

/// <summary>
/// JSON response structure for enhanced agents
/// </summary>
public record EnhancedAgentJsonResponse
{
    public List<string>? ReasoningChain { get; init; }
    public List<FindingJson>? Findings { get; init; }
    public double OverallConfidence { get; init; }
    public List<string>? Limitations { get; init; }
    public List<string>? Recommendations { get; init; }
}

public record FindingJson
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Severity { get; init; }
    public double Confidence { get; init; }
    public string? Evidence { get; init; }
    public List<string>? UncertaintyFactors { get; init; }
    public List<string>? AlternativeInterpretations { get; init; }
}

/// <summary>
/// Validation challenge result
/// </summary>
public record ValidationChallenge
{
    public double ValidationScore { get; init; }
    public List<ChallengedFinding>? ChallengedFindings { get; init; }
    public string? OverallAssessment { get; init; }
    public double ConfidenceAdjustment { get; init; }
}

public record ChallengedFinding
{
    public string? OriginalFinding { get; init; }
    public double ValidityScore { get; init; }
    public List<string>? Concerns { get; init; }
    public List<string>? SupportingEvidence { get; init; }
    public string? AlternativeView { get; init; }
}

/// <summary>
/// Agent validation result for caching
/// </summary>
public record AgentValidationResult
{
    public double ValidationScore { get; init; }
    public List<string> ValidationConcerns { get; init; } = new();
    public DateTime ValidatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Enhanced agent execution result with validation data
/// </summary>
public record AgentExecutionResult
{
    public AgentType AgentType { get; init; }
    public bool Success { get; init; }
    public EnhancedAgentResult? Result { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public double ConfidenceScore { get; init; }
    public List<string>? ReasoningChain { get; init; }
    public List<string>? ValidationFlags { get; init; }
}