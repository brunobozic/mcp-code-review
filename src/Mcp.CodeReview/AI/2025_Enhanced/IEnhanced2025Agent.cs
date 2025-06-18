using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.Abstractions;

/// <summary>
/// Enhanced 2025 AI Agent interface with advanced hallucination reduction and validation capabilities
/// Implements latest multi-agent best practices for production-grade AI systems
/// </summary>
public interface IEnhanced2025Agent
{
    /// <summary>
    /// Agent type identifier
    /// </summary>
    AgentType AgentType { get; }
    
    /// <summary>
    /// Agent specialization description
    /// </summary>
    string Specialization { get; }
    
    /// <summary>
    /// Supported confidence threshold for this agent
    /// </summary>
    double MinimumConfidenceThreshold { get; }

    /// <summary>
    /// Analyze code with enhanced 2025 methodology including reasoning chains and uncertainty handling
    /// </summary>
    /// <param name="request">Code review request with context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced analysis result with confidence scoring and reasoning chain</returns>
    Task<Enhanced2025AnalysisResult> AnalyzeWithReasoningAsync(
        CodeReviewRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate findings from another agent (cross-agent validation)
    /// </summary>
    /// <param name="originalFindings">Findings to validate</param>
    /// <param name="originalAgentType">Type of agent that generated the findings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result with confidence adjustments</returns>
    Task<AgentValidationResult> ValidateFindingsAsync(
        List<Finding> originalFindings,
        AgentType originalAgentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Challenge specific claims to reduce hallucinations
    /// </summary>
    /// <param name="claim">Claim to challenge</param>
    /// <param name="evidence">Supporting evidence</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Challenge result with alternative perspectives</returns>
    Task<ClaimChallengeResult> ChallengeClaims(
        string claim,
        string evidence,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assess confidence in own analysis
    /// </summary>
    /// <param name="analysis">Analysis to assess</param>
    /// <returns>Confidence score and reasoning</returns>
    ConfidenceAssessment AssessConfidence(string analysis);

    /// <summary>
    /// Get uncertainty factors that could affect analysis quality
    /// </summary>
    /// <param name="context">Analysis context</param>
    /// <returns>List of uncertainty factors</returns>
    List<UncertaintyFactor> GetUncertaintyFactors(CodeReviewRequest context);
}

/// <summary>
/// Enhanced 2025 analysis result with comprehensive metadata
/// </summary>
public record Enhanced2025AnalysisResult
{
    /// <summary>
    /// Primary analysis findings
    /// </summary>
    public List<Finding> Findings { get; init; } = new();
    
    /// <summary>
    /// Step-by-step reasoning chain
    /// </summary>
    public List<ReasoningStep> ReasoningChain { get; init; } = new();
    
    /// <summary>
    /// Overall confidence in the analysis (0.0-1.0)
    /// </summary>
    public double OverallConfidence { get; init; }
    
    /// <summary>
    /// Specific confidence for each finding
    /// </summary>
    public Dictionary<string, double> FindingConfidences { get; init; } = new();
    
    /// <summary>
    /// Identified limitations and assumptions
    /// </summary>
    public List<AnalysisLimitation> Limitations { get; init; } = new();
    
    /// <summary>
    /// Alternative interpretations considered
    /// </summary>
    public List<AlternativeInterpretation> AlternativeViews { get; init; } = new();
    
    /// <summary>
    /// Uncertainty factors that affected the analysis
    /// </summary>
    public List<UncertaintyFactor> UncertaintyFactors { get; init; } = new();
    
    /// <summary>
    /// Evidence supporting each finding
    /// </summary>
    public Dictionary<string, List<Evidence>> SupportingEvidence { get; init; } = new();
    
    /// <summary>
    /// Recommendations with implementation guidance
    /// </summary>
    public List<ActionableRecommendation> Recommendations { get; init; } = new();
    
    /// <summary>
    /// Self-criticism and potential weaknesses in the analysis
    /// </summary>
    public List<SelfCriticism> SelfCriticisms { get; init; } = new();
    
    /// <summary>
    /// Timestamp of analysis
    /// </summary>
    public DateTime AnalysisTimestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Analysis duration
    /// </summary>
    public TimeSpan AnalysisDuration { get; init; }
}

/// <summary>
/// Individual step in reasoning chain with confidence
/// </summary>
public record ReasoningStep
{
    /// <summary>
    /// Step description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Evidence or observation supporting this step
    /// </summary>
    public string Evidence { get; init; } = string.Empty;
    
    /// <summary>
    /// Confidence in this reasoning step (0.0-1.0)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Alternative ways this step could be interpreted
    /// </summary>
    public List<string> AlternativeInterpretations { get; init; } = new();
    
    /// <summary>
    /// Step order in the reasoning chain
    /// </summary>
    public int StepOrder { get; init; }
}

/// <summary>
/// Analysis limitation or assumption
/// </summary>
public record AnalysisLimitation
{
    /// <summary>
    /// Type of limitation
    /// </summary>
    public LimitationType Type { get; init; }
    
    /// <summary>
    /// Description of the limitation
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Impact on analysis quality (0.0-1.0)
    /// </summary>
    public double Impact { get; init; }
    
    /// <summary>
    /// Suggested mitigation if any
    /// </summary>
    public string? Mitigation { get; init; }
}

/// <summary>
/// Alternative interpretation of findings
/// </summary>
public record AlternativeInterpretation
{
    /// <summary>
    /// Alternative viewpoint description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Plausibility of this interpretation (0.0-1.0)
    /// </summary>
    public double Plausibility { get; init; }
    
    /// <summary>
    /// Evidence supporting this alternative
    /// </summary>
    public List<string> SupportingEvidence { get; init; } = new();
    
    /// <summary>
    /// Why this interpretation might be more accurate
    /// </summary>
    public string? Reasoning { get; init; }
}

/// <summary>
/// Uncertainty factor affecting analysis
/// </summary>
public record UncertaintyFactor
{
    /// <summary>
    /// Source of uncertainty
    /// </summary>
    public UncertaintySource Source { get; init; }
    
    /// <summary>
    /// Description of the uncertainty
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Impact on analysis reliability (0.0-1.0)
    /// </summary>
    public double Impact { get; init; }
    
    /// <summary>
    /// Suggested approach to handle this uncertainty
    /// </summary>
    public string? HandlingStrategy { get; init; }
}

/// <summary>
/// Evidence supporting a finding
/// </summary>
public record Evidence
{
    /// <summary>
    /// Type of evidence
    /// </summary>
    public EvidenceType Type { get; init; }
    
    /// <summary>
    /// Evidence description or quote
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Source location (line number, file path, etc.)
    /// </summary>
    public string? Location { get; init; }
    
    /// <summary>
    /// Strength of this evidence (0.0-1.0)
    /// </summary>
    public double Strength { get; init; }
    
    /// <summary>
    /// Potential counter-evidence or weaknesses
    /// </summary>
    public List<string> CounterEvidence { get; init; } = new();
}

/// <summary>
/// Actionable recommendation with implementation details
/// </summary>
public record ActionableRecommendation
{
    /// <summary>
    /// Recommendation title
    /// </summary>
    public string Title { get; init; } = string.Empty;
    
    /// <summary>
    /// Detailed description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Priority level
    /// </summary>
    public RecommendationPriority Priority { get; init; }
    
    /// <summary>
    /// Specific implementation steps
    /// </summary>
    public List<ImplementationStep> ImplementationSteps { get; init; } = new();
    
    /// <summary>
    /// Expected impact if implemented
    /// </summary>
    public string ExpectedImpact { get; init; } = string.Empty;
    
    /// <summary>
    /// Estimated effort required
    /// </summary>
    public EffortEstimate Effort { get; init; }
    
    /// <summary>
    /// Potential risks of implementing this recommendation
    /// </summary>
    public List<string> Risks { get; init; } = new();
}

/// <summary>
/// Self-criticism of the analysis
/// </summary>
public record SelfCriticism
{
    /// <summary>
    /// Aspect of analysis being criticized
    /// </summary>
    public string Aspect { get; init; } = string.Empty;
    
    /// <summary>
    /// Description of potential weakness
    /// </summary>
    public string Weakness { get; init; } = string.Empty;
    
    /// <summary>
    /// How this weakness might affect conclusions
    /// </summary>
    public string Impact { get; init; } = string.Empty;
    
    /// <summary>
    /// Suggested improvement or mitigation
    /// </summary>
    public string? Improvement { get; init; }
}

/// <summary>
/// Implementation step for a recommendation
/// </summary>
public record ImplementationStep
{
    /// <summary>
    /// Step order
    /// </summary>
    public int Order { get; init; }
    
    /// <summary>
    /// Step description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Required tools or resources
    /// </summary>
    public List<string> RequiredResources { get; init; } = new();
    
    /// <summary>
    /// Success criteria for this step
    /// </summary>
    public string SuccessCriteria { get; init; } = string.Empty;
}

/// <summary>
/// Confidence assessment result
/// </summary>
public record ConfidenceAssessment
{
    /// <summary>
    /// Overall confidence score (0.0-1.0)
    /// </summary>
    public double Score { get; init; }
    
    /// <summary>
    /// Factors that increase confidence
    /// </summary>
    public List<string> ConfidenceFactors { get; init; } = new();
    
    /// <summary>
    /// Factors that decrease confidence
    /// </summary>
    public List<string> UncertaintyFactors { get; init; } = new();
    
    /// <summary>
    /// Reasoning for the confidence score
    /// </summary>
    public string Reasoning { get; init; } = string.Empty;
}

/// <summary>
/// Result of challenging a claim
/// </summary>
public record ClaimChallengeResult
{
    /// <summary>
    /// Original claim
    /// </summary>
    public string OriginalClaim { get; init; } = string.Empty;
    
    /// <summary>
    /// Validity assessment (0.0-1.0)
    /// </summary>
    public double ValidityScore { get; init; }
    
    /// <summary>
    /// Alternative perspectives
    /// </summary>
    public List<string> AlternativePerspectives { get; init; } = new();
    
    /// <summary>
    /// Counter-evidence or concerns
    /// </summary>
    public List<string> CounterEvidence { get; init; } = new();
    
    /// <summary>
    /// Supporting evidence
    /// </summary>
    public List<string> SupportingEvidence { get; init; } = new();
    
    /// <summary>
    /// Refined claim based on challenge
    /// </summary>
    public string? RefinedClaim { get; init; }
}

// Enums for structured classification

public enum LimitationType
{
    InsufficientContext,
    IncompleteCodeBase,
    MissingDocumentation,
    TimeConstraints,
    TechnicalLimitations,
    DomainKnowledgeGaps
}

public enum UncertaintySource
{
    IncompleteInformation,
    AmbiguousCode,
    MissingContext,
    TechnicalComplexity,
    DomainSpecificKnowledge,
    RapidlyChangingTechnology
}

public enum EvidenceType
{
    DirectCodeReference,
    PatternMatch,
    DocumentationReference,
    BestPracticeViolation,
    PerformanceMetric,
    SecurityVulnerability,
    StaticAnalysisResult
}

public enum RecommendationPriority
{
    Critical,
    High,
    Medium,
    Low,
    Optional
}

public enum EffortEstimate
{
    Minimal,    // < 1 hour
    Low,        // 1-4 hours
    Medium,     // 1-2 days
    High,       // 3-7 days
    Significant // > 1 week
}