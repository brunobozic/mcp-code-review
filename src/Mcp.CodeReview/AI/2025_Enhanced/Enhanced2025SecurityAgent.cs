using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mcp.CodeReview.AI;

/// <summary>
/// Enhanced 2025 Security Expert Agent with advanced hallucination reduction and validation
/// Implements state-of-the-art multi-agent patterns for production security analysis
/// </summary>
public class Enhanced2025SecurityAgent : IEnhanced2025Agent
{
    private readonly IClaudeService _claudeService;
    private readonly ILogger<Enhanced2025SecurityAgent> _logger;
    
    // 2025 Enhancement: Knowledge base of security patterns
    private readonly Dictionary<string, SecurityPattern> _securityPatterns;
    private readonly HashSet<string> _highConfidenceVulnerabilities;
    private readonly Dictionary<string, double> _severityWeights;

    public AgentType AgentType => AgentType.SecurityExpert;
    public string Specialization => "Advanced Security Analysis with Hallucination Reduction";
    public double MinimumConfidenceThreshold => 0.75;

    public Enhanced2025SecurityAgent(IClaudeService claudeService, ILogger<Enhanced2025SecurityAgent> logger)
    {
        _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize security knowledge base
        _securityPatterns = InitializeSecurityPatterns();
        _highConfidenceVulnerabilities = InitializeHighConfidencePatterns();
        _severityWeights = InitializeSeverityWeights();
    }

    /// <summary>
    /// Analyze code with enhanced 2025 security methodology
    /// </summary>
    public async Task<Enhanced2025AnalysisResult> AnalyzeWithReasoningAsync(
        CodeReviewRequest request, 
        CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("🔒 Starting Enhanced 2025 Security Analysis");

            // Phase 1: Pre-analysis pattern matching
            var preAnalysisResults = PerformPreAnalysisPatternMatching(request.Content);
            
            // Phase 2: AI-enhanced deep analysis with reasoning chains
            var deepAnalysisResults = await PerformDeepSecurityAnalysisAsync(request, preAnalysisResults, cancellationToken);
            
            // Phase 3: Self-validation and confidence assessment
            var validatedResults = await PerformSelfValidationAsync(deepAnalysisResults, cancellationToken);
            
            // Phase 4: Uncertainty analysis and limitation identification
            var uncertaintyFactors = AnalyzeUncertaintyFactors(request, validatedResults);
            
            stopwatch.Stop();
            
            return BuildEnhancedAnalysisResult(validatedResults, uncertaintyFactors, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enhanced security analysis failed");
            stopwatch.Stop();
            
            return new Enhanced2025AnalysisResult
            {
                Findings = new List<Finding>
                {
                    new Finding
                    {
                        Title = "Security Analysis Error",
                        Description = "Unable to complete security analysis due to technical error",
                        Severity = "low"
                    }
                },
                OverallConfidence = 0.0,
                AnalysisDuration = stopwatch.Elapsed,
                Limitations = new List<AnalysisLimitation>
                {
                    new AnalysisLimitation
                    {
                        Type = LimitationType.TechnicalLimitations,
                        Description = "Analysis failed due to technical error",
                        Impact = 1.0
                    }
                }
            };
        }
    }

    /// <summary>
    /// Validate findings from another agent
    /// </summary>
    public async Task<AgentValidationResult> ValidateFindingsAsync(
        List<Finding> originalFindings,
        AgentType originalAgentType,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 Validating {Count} findings from {AgentType}", 
            originalFindings.Count, originalAgentType);

        var validationPrompt = $@"SECURITY VALIDATION CHALLENGE 2025:

You are a security expert validating findings from a {originalAgentType} agent.
Your role is to CRITICALLY ASSESS these security-related claims.

FINDINGS TO VALIDATE:
{JsonSerializer.Serialize(originalFindings, new JsonSerializerOptions { WriteIndented = true })}

VALIDATION CRITERIA:
1. Are the security claims technically accurate?
2. Is the evidence sufficient to support the severity rating?
3. Could this be a false positive?
4. Are there alternative explanations?
5. Is the language too absolute/confident?

RESPOND IN JSON:
{{
  ""overall_validation_score"": 0.85,
  ""finding_validations"": [
    {{
      ""finding_title"": ""SQL Injection Risk"",
      ""technical_accuracy"": 0.9,
      ""evidence_sufficiency"": 0.8,
      ""false_positive_risk"": 0.1,
      ""concerns"": [""Severity might be overstated""],
      ""supporting_factors"": [""Clear vulnerable pattern""],
      ""confidence_adjustment"": -0.05
    }}
  ],
  ""overall_concerns"": [""Some claims lack specific evidence""],
  ""validation_reasoning"": ""Generally accurate but some overconfident claims""
}}";

        try
        {
            var response = await _claudeService.GenerateReviewAsync(validationPrompt, cancellationToken);
            var validation = JsonSerializer.Deserialize<SecurityValidationResult>(response);
            
            return new AgentValidationResult
            {
                ValidationScore = validation?.OverallValidationScore ?? 0.5,
                ValidationConcerns = validation?.OverallConcerns ?? new List<string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Security validation failed");
            return new AgentValidationResult
            {
                ValidationScore = 0.5,
                ValidationConcerns = new List<string> { "Validation process failed" }
            };
        }
    }

    /// <summary>
    /// Challenge specific security claims
    /// </summary>
    public async Task<ClaimChallengeResult> ChallengeClaims(
        string claim,
        string evidence,
        CancellationToken cancellationToken = default)
    {
        var challengePrompt = $@"SECURITY CLAIM CHALLENGE 2025:

CLAIM TO CHALLENGE: ""{claim}""
PROVIDED EVIDENCE: ""{evidence}""

As a security expert, challenge this claim by:
1. Identifying potential false positives
2. Finding alternative explanations
3. Questioning the severity assessment
4. Looking for missing context

RESPOND IN JSON:
{{
  ""validity_assessment"": 0.75,
  ""technical_concerns"": [""Could be configuration dependent""],
  ""alternative_explanations"": [""Might be mitigated by other controls""],
  ""evidence_gaps"": [""Missing context about input validation""],
  ""refined_claim"": ""Potential SQL injection if input validation is insufficient"",
  ""confidence_factors"": [""Clear vulnerable pattern""],
  ""uncertainty_factors"": [""Unknown security controls""]
}}";

        try
        {
            var response = await _claudeService.GenerateReviewAsync(challengePrompt, cancellationToken);
            var challenge = JsonSerializer.Deserialize<SecurityChallengeResult>(response);
            
            return new ClaimChallengeResult
            {
                OriginalClaim = claim,
                ValidityScore = challenge?.ValidityAssessment ?? 0.5,
                AlternativePerspectives = challenge?.AlternativeExplanations ?? new List<string>(),
                CounterEvidence = challenge?.TechnicalConcerns ?? new List<string>(),
                SupportingEvidence = challenge?.ConfidenceFactors ?? new List<string>(),
                RefinedClaim = challenge?.RefinedClaim
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Claim challenge failed");
            return new ClaimChallengeResult
            {
                OriginalClaim = claim,
                ValidityScore = 0.5
            };
        }
    }

    /// <summary>
    /// Assess confidence in security analysis
    /// </summary>
    public ConfidenceAssessment AssessConfidence(string analysis)
    {
        var confidenceFactors = new List<string>();
        var uncertaintyFactors = new List<string>();
        var baseConfidence = 0.7;

        // Check for specific evidence
        if (analysis.Contains("line ") || analysis.Contains("function ") || analysis.Contains("variable "))
        {
            confidenceFactors.Add("Contains specific code references");
            baseConfidence += 0.1;
        }

        // Check for pattern matching
        var patternMatches = _securityPatterns.Keys.Count(pattern => 
            analysis.ToLower().Contains(pattern.ToLower()));
        if (patternMatches > 0)
        {
            confidenceFactors.Add($"Matches {patternMatches} known security patterns");
            baseConfidence += Math.Min(0.2, patternMatches * 0.05);
        }

        // Check for uncertainty language
        var uncertaintyWords = new[] { "might", "could", "possibly", "appears", "seems", "likely" };
        var uncertaintyCount = uncertaintyWords.Count(word => analysis.ToLower().Contains(word));
        if (uncertaintyCount > 0)
        {
            confidenceFactors.Add("Appropriately expresses uncertainty");
        }
        else
        {
            uncertaintyFactors.Add("Lacks uncertainty language");
            baseConfidence -= 0.1;
        }

        // Check for overconfident language
        var overconfidentWords = new[] { "definitely", "always", "never", "impossible", "guaranteed" };
        var overconfidentCount = overconfidentWords.Count(word => analysis.ToLower().Contains(word));
        if (overconfidentCount > 0)
        {
            uncertaintyFactors.Add($"Contains {overconfidentCount} overconfident terms");
            baseConfidence -= overconfidentCount * 0.1;
        }

        return new ConfidenceAssessment
        {
            Score = Math.Max(0.0, Math.Min(1.0, baseConfidence)),
            ConfidenceFactors = confidenceFactors,
            UncertaintyFactors = uncertaintyFactors,
            Reasoning = $"Based on evidence specificity, pattern matching, and language analysis"
        };
    }

    /// <summary>
    /// Get uncertainty factors for security analysis
    /// </summary>
    public List<UncertaintyFactor> GetUncertaintyFactors(CodeReviewRequest context)
    {
        var factors = new List<UncertaintyFactor>();

        // Check for incomplete context
        if (string.IsNullOrEmpty(context.Context))
        {
            factors.Add(new UncertaintyFactor
            {
                Source = UncertaintySource.MissingContext,
                Description = "No business context provided",
                Impact = 0.3,
                HandlingStrategy = "Request additional context about application purpose and security requirements"
            });
        }

        // Check for code completeness
        if (context.Content.Length < 100)
        {
            factors.Add(new UncertaintyFactor
            {
                Source = UncertaintySource.IncompleteInformation,
                Description = "Very limited code sample",
                Impact = 0.4,
                HandlingStrategy = "Request larger code context or full file contents"
            });
        }

        // Check for framework/technology identification
        if (!ContainsFrameworkIndicators(context.Content))
        {
            factors.Add(new UncertaintyFactor
            {
                Source = UncertaintySource.TechnicalComplexity,
                Description = "Framework or technology not clearly identified",
                Impact = 0.2,
                HandlingStrategy = "Analyze import statements and framework patterns"
            });
        }

        return factors;
    }

    #region Private Helper Methods

    /// <summary>
    /// Perform pre-analysis pattern matching
    /// </summary>
    private PreAnalysisResults PerformPreAnalysisPatternMatching(string code)
    {
        var results = new PreAnalysisResults();
        
        foreach (var pattern in _securityPatterns)
        {
            if (Regex.IsMatch(code, pattern.Value.Pattern, RegexOptions.IgnoreCase))
            {
                results.DetectedPatterns.Add(pattern.Key);
                results.ConfidenceBoosts.Add(pattern.Key, pattern.Value.ConfidenceBoost);
            }
        }

        return results;
    }

    /// <summary>
    /// Perform deep AI security analysis
    /// </summary>
    private async Task<DeepAnalysisResults> PerformDeepSecurityAnalysisAsync(
        CodeReviewRequest request,
        PreAnalysisResults preAnalysis,
        CancellationToken cancellationToken)
    {
        var prompt = BuildEnhanced2025SecurityPrompt(request, preAnalysis);
        var response = await _claudeService.GenerateReviewAsync(prompt, cancellationToken);
        
        // Parse structured response
        return ParseDeepAnalysisResponse(response);
    }

    /// <summary>
    /// Build enhanced security analysis prompt
    /// </summary>
    private string BuildEnhanced2025SecurityPrompt(CodeReviewRequest request, PreAnalysisResults preAnalysis)
    {
        var detectedPatternsText = preAnalysis.DetectedPatterns.Any() 
            ? $"PRE-DETECTED PATTERNS: {string.Join(", ", preAnalysis.DetectedPatterns)}"
            : "NO PRE-PATTERNS DETECTED";

        return $@"ENHANCED 2025 SECURITY ANALYSIS:

You are an expert security analyst using 2025 best practices for hallucination reduction.

{detectedPatternsText}

CODE TO ANALYZE:
{request.Content}

CONTEXT: {request.Context}
LANGUAGE: {request.Language}

ANALYSIS REQUIREMENTS:
1. REASONING CHAIN: Show step-by-step security analysis thinking
2. EVIDENCE-BASED: Only flag vulnerabilities with specific code evidence
3. UNCERTAINTY HANDLING: Use ""likely"", ""appears"", ""suggests"" for uncertain findings
4. CONFIDENCE SCORING: Rate confidence for each finding (0.0-1.0)
5. FALSE POSITIVE CONSIDERATION: Consider why findings might be false positives
6. ALTERNATIVE VIEWS: Consider benign explanations for suspicious patterns

RESPOND IN EXACT JSON FORMAT:
{{
  ""reasoning_chain"": [
    ""Step 1: Scanning for input validation patterns"",
    ""Step 2: Checking database interaction methods"",
    ""Step 3: Analyzing authentication mechanisms""
  ],
  ""security_findings"": [
    {{
      ""title"": ""Potential SQL Injection Vector"",
      ""description"": ""The code appears to construct SQL queries using string concatenation, which could allow injection attacks if input is not properly validated."",
      ""severity"": ""high"",
      ""confidence"": 0.85,
      ""evidence"": ""Line 23: 'SELECT * FROM users WHERE id=' + userId"",
      ""false_positive_factors"": [""Could have input validation elsewhere""],
      ""uncertainty_language"": ""appears to construct"",
      ""alternative_explanation"": ""May be safely used in internal context with validated input""
    }}
  ],
  ""overall_confidence"": 0.78,
  ""analysis_limitations"": [""Limited context about broader security controls""],
  ""recommendations"": [
    {{
      ""title"": ""Implement Parameterized Queries"",
      ""description"": ""Replace string concatenation with parameterized queries or ORM methods"",
      ""priority"": ""high"",
      ""implementation_steps"": [""Replace concatenation"", ""Add parameter binding"", ""Test with various inputs""]
    }}
  ],
  ""self_criticism"": [
    {{
      ""aspect"": ""Context limitations"",
      ""weakness"": ""Cannot see full security context of application"",
      ""impact"": ""May miss existing security controls""
    }}
  ]
}}

CRITICAL: Use uncertainty language. Avoid ""definitely"", ""always"", ""never"". Consider false positives.";
    }

    /// <summary>
    /// Parse deep analysis response
    /// </summary>
    private DeepAnalysisResults ParseDeepAnalysisResponse(string response)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<SecurityAnalysisResponse>(response);
            return new DeepAnalysisResults
            {
                SecurityFindings = parsed?.SecurityFindings ?? new List<SecurityFindingDetail>(),
                ReasoningChain = parsed?.ReasoningChain ?? new List<string>(),
                OverallConfidence = parsed?.OverallConfidence ?? 0.5,
                AnalysisLimitations = parsed?.AnalysisLimitations ?? new List<string>(),
                SelfCriticisms = parsed?.SelfCriticism ?? new List<SelfCriticismDetail>()
            };
        }
        catch (JsonException)
        {
            // Fallback parsing
            return new DeepAnalysisResults
            {
                SecurityFindings = new List<SecurityFindingDetail>(),
                OverallConfidence = 0.3,
                AnalysisLimitations = new List<string> { "Response not in expected JSON format" }
            };
        }
    }

    /// <summary>
    /// Perform self-validation of analysis
    /// </summary>
    private async Task<DeepAnalysisResults> PerformSelfValidationAsync(
        DeepAnalysisResults results,
        CancellationToken cancellationToken)
    {
        // Self-challenge each finding
        foreach (var finding in results.SecurityFindings)
        {
            var challenge = await ChallengeClaims(finding.Title, finding.Evidence, cancellationToken);
            finding.ValidationScore = challenge.ValidityScore;
            finding.ValidationConcerns = challenge.CounterEvidence;
        }

        return results;
    }

    /// <summary>
    /// Analyze uncertainty factors
    /// </summary>
    private List<UncertaintyFactor> AnalyzeUncertaintyFactors(
        CodeReviewRequest request,
        DeepAnalysisResults results)
    {
        var factors = GetUncertaintyFactors(request);
        
        // Add analysis-specific uncertainty factors
        if (results.SecurityFindings.Any(f => f.Confidence < 0.7))
        {
            factors.Add(new UncertaintyFactor
            {
                Source = UncertaintySource.TechnicalComplexity,
                Description = "Some findings have low confidence scores",
                Impact = 0.2
            });
        }

        return factors;
    }

    /// <summary>
    /// Build final enhanced analysis result
    /// </summary>
    private Enhanced2025AnalysisResult BuildEnhancedAnalysisResult(
        DeepAnalysisResults validatedResults,
        List<UncertaintyFactor> uncertaintyFactors,
        TimeSpan duration)
    {
        var findings = validatedResults.SecurityFindings.Select(sf => new Finding
        {
            Title = sf.Title,
            Description = sf.Description,
            Severity = sf.Severity,
            Evidence = sf.Evidence
        }).ToList();

        var reasoningSteps = validatedResults.ReasoningChain.Select((step, index) => new ReasoningStep
        {
            Description = step,
            StepOrder = index + 1,
            Confidence = 0.8 // Default confidence for reasoning steps
        }).ToList();

        return new Enhanced2025AnalysisResult
        {
            Findings = findings,
            ReasoningChain = reasoningSteps,
            OverallConfidence = validatedResults.OverallConfidence,
            UncertaintyFactors = uncertaintyFactors,
            AnalysisDuration = duration,
            SelfCriticisms = validatedResults.SelfCriticisms.Select(sc => new SelfCriticism
            {
                Aspect = sc.Aspect,
                Weakness = sc.Weakness,
                Impact = sc.Impact
            }).ToList()
        };
    }

    /// <summary>
    /// Initialize security patterns knowledge base
    /// </summary>
    private Dictionary<string, SecurityPattern> InitializeSecurityPatterns()
    {
        return new Dictionary<string, SecurityPattern>
        {
            ["sql_injection"] = new SecurityPattern
            {
                Pattern = @"(SELECT|INSERT|UPDATE|DELETE).*\+.*['""]",
                ConfidenceBoost = 0.3,
                Description = "SQL query construction with string concatenation"
            },
            ["xss_risk"] = new SecurityPattern
            {
                Pattern = @"innerHTML|document\.write|eval\(",
                ConfidenceBoost = 0.25,
                Description = "Potential XSS vulnerability patterns"
            },
            ["weak_crypto"] = new SecurityPattern
            {
                Pattern = @"MD5|SHA1|DES|RC4",
                ConfidenceBoost = 0.2,
                Description = "Use of weak cryptographic algorithms"
            }
        };
    }

    /// <summary>
    /// Initialize high confidence vulnerability patterns
    /// </summary>
    private HashSet<string> InitializeHighConfidencePatterns()
    {
        return new HashSet<string>
        {
            "eval(",
            "document.write(",
            "innerHTML =",
            "SELECT * FROM",
            "password = ''"
        };
    }

    /// <summary>
    /// Initialize severity weights
    /// </summary>
    private Dictionary<string, double> InitializeSeverityWeights()
    {
        return new Dictionary<string, double>
        {
            ["critical"] = 1.0,
            ["high"] = 0.8,
            ["medium"] = 0.6,
            ["low"] = 0.4
        };
    }

    /// <summary>
    /// Check if code contains framework indicators
    /// </summary>
    private bool ContainsFrameworkIndicators(string code)
    {
        var frameworkPatterns = new[]
        {
            "import ", "using ", "require(", "from ", "@", "<dependency>", "package.json"
        };

        return frameworkPatterns.Any(pattern => code.Contains(pattern));
    }

    #endregion
}

#region Supporting Classes

public record SecurityPattern
{
    public string Pattern { get; init; } = string.Empty;
    public double ConfidenceBoost { get; init; }
    public string Description { get; init; } = string.Empty;
}

public record PreAnalysisResults
{
    public List<string> DetectedPatterns { get; init; } = new();
    public Dictionary<string, double> ConfidenceBoosts { get; init; } = new();
}

public record DeepAnalysisResults
{
    public List<SecurityFindingDetail> SecurityFindings { get; init; } = new();
    public List<string> ReasoningChain { get; init; } = new();
    public double OverallConfidence { get; init; }
    public List<string> AnalysisLimitations { get; init; } = new();
    public List<SelfCriticismDetail> SelfCriticisms { get; init; } = new();
}

public record SecurityFindingDetail
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public double Confidence { get; init; }
    public string Evidence { get; init; } = string.Empty;
    public List<string> FalsePositiveFactors { get; init; } = new();
    public string UncertaintyLanguage { get; init; } = string.Empty;
    public string AlternativeExplanation { get; init; } = string.Empty;
    
    // Validation results
    public double ValidationScore { get; set; }
    public List<string> ValidationConcerns { get; set; } = new();
}

public record SelfCriticismDetail
{
    public string Aspect { get; init; } = string.Empty;
    public string Weakness { get; init; } = string.Empty;
    public string Impact { get; init; } = string.Empty;
}

public record SecurityAnalysisResponse
{
    public List<string>? ReasoningChain { get; init; }
    public List<SecurityFindingDetail>? SecurityFindings { get; init; }
    public double OverallConfidence { get; init; }
    public List<string>? AnalysisLimitations { get; init; }
    public List<SelfCriticismDetail>? SelfCriticism { get; init; }
}

public record SecurityValidationResult
{
    public double OverallValidationScore { get; init; }
    public List<string>? OverallConcerns { get; init; }
}

public record SecurityChallengeResult
{
    public double ValidityAssessment { get; init; }
    public List<string>? TechnicalConcerns { get; init; }
    public List<string>? AlternativeExplanations { get; init; }
    public List<string>? EvidenceGaps { get; init; }
    public string? RefinedClaim { get; init; }
    public List<string>? ConfidenceFactors { get; init; }
    public List<string>? UncertaintyFactors { get; init; }
}

#endregion