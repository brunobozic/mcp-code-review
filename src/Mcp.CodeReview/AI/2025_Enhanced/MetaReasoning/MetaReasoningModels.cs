using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.AI.Enhanced2025.MetaReasoning;

/// <summary>
/// Result of meta-reasoning analysis on an agent's reasoning process
/// </summary>
public record MetaReasoningResult
{
    /// <summary>
    /// Unique identifier for this meta-reasoning analysis
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// The reasoning trace that was analyzed
    /// </summary>
    public AgentReasoningTrace AnalyzedTrace { get; init; } = new();
    
    /// <summary>
    /// When the meta-reasoning analysis was performed
    /// </summary>
    public DateTime AnalysisTimestamp { get; init; }
    
    /// <summary>
    /// How long the analysis took
    /// </summary>
    public TimeSpan AnalysisDuration { get; init; }
    
    /// <summary>
    /// Quality analysis of the reasoning process
    /// </summary>
    public ReasoningQualityAnalysis QualityAnalysis { get; init; } = new();
    
    /// <summary>
    /// Identified reasoning patterns
    /// </summary>
    public List<ReasoningPattern> IdentifiedPatterns { get; init; } = new();
    
    /// <summary>
    /// Detected reasoning biases
    /// </summary>
    public List<ReasoningBias> DetectedBiases { get; init; } = new();
    
    /// <summary>
    /// Logical consistency evaluation
    /// </summary>
    public LogicalConsistencyEvaluation ConsistencyEvaluation { get; init; } = new();
    
    /// <summary>
    /// Confidence calibration assessment
    /// </summary>
    public ConfidenceCalibrationAssessment ConfidenceCalibration { get; init; } = new();
    
    /// <summary>
    /// Recommended improvements
    /// </summary>
    public List<ReasoningImprovement> ImprovementRecommendations { get; init; } = new();
    
    /// <summary>
    /// Meta-meta reasoning (reflection on the meta-reasoning process itself)
    /// </summary>
    public MetaMetaReflection MetaMetaReflection { get; init; } = new();
    
    /// <summary>
    /// Overall meta-reasoning score (0-1)
    /// </summary>
    public double OverallMetaScore { get; init; }
    
    /// <summary>
    /// Error message if analysis failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Analysis of reasoning quality across multiple dimensions
/// </summary>
public record ReasoningQualityAnalysis
{
    /// <summary>
    /// How well-structured the reasoning flow is (0-1)
    /// </summary>
    public double LogicalStructureScore { get; init; }
    
    /// <summary>
    /// How effectively evidence is incorporated (0-1)
    /// </summary>
    public double EvidenceIntegrationScore { get; init; }
    
    /// <summary>
    /// How thorough the analysis is (0-1)
    /// </summary>
    public double DepthOfAnalysisScore { get; init; }
    
    /// <summary>
    /// How well assumptions are identified and managed (0-1)
    /// </summary>
    public double AssumptionManagementScore { get; init; }
    
    /// <summary>
    /// How well alternative explanations are considered (0-1)
    /// </summary>
    public double AlternativeConsiderationScore { get; init; }
    
    /// <summary>
    /// Overall quality score (0-1)
    /// </summary>
    public double OverallQualityScore { get; init; }
    
    /// <summary>
    /// Identified strengths in reasoning
    /// </summary>
    public List<string> Strengths { get; init; } = new();
    
    /// <summary>
    /// Identified weaknesses in reasoning
    /// </summary>
    public List<string> Weaknesses { get; init; } = new();
    
    /// <summary>
    /// Observed cognitive patterns
    /// </summary>
    public List<string> CognitivePatterns { get; init; } = new();
    
    /// <summary>
    /// Potential blind spots
    /// </summary>
    public List<string> BlindSpots { get; init; } = new();
    
    /// <summary>
    /// Detailed analysis text
    /// </summary>
    public string DetailedAnalysis { get; init; } = string.Empty;
}

/// <summary>
/// Identified reasoning pattern
/// </summary>
public record ReasoningPattern
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Pattern name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Pattern description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of reasoning pattern
    /// </summary>
    public ReasoningPatternType PatternType { get; init; }
    
    /// <summary>
    /// Frequency/strength of the pattern (0-1)
    /// </summary>
    public double Frequency { get; init; }
    
    /// <summary>
    /// Context where pattern appears
    /// </summary>
    public string Context { get; init; } = string.Empty;
    
    /// <summary>
    /// Effectiveness assessment of the pattern
    /// </summary>
    public double Effectiveness { get; init; }
    
    /// <summary>
    /// Whether pattern is beneficial or problematic
    /// </summary>
    public bool IsBeneficial { get; init; }
    
    /// <summary>
    /// Examples of pattern occurrence
    /// </summary>
    public List<string> Examples { get; init; } = new();
    
    /// <summary>
    /// Improvement suggestions for this pattern
    /// </summary>
    public List<string> ImprovementSuggestions { get; init; } = new();
}

/// <summary>
/// Detected cognitive bias in reasoning
/// </summary>
public record ReasoningBias
{
    /// <summary>
    /// Type of bias detected
    /// </summary>
    public BiasType BiasType { get; init; }
    
    /// <summary>
    /// Bias name
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Description of the bias
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Evidence of bias in the reasoning
    /// </summary>
    public List<string> Evidence { get; init; } = new();
    
    /// <summary>
    /// Severity assessment (0-1)
    /// </summary>
    public double Severity { get; init; }
    
    /// <summary>
    /// Impact on reasoning quality
    /// </summary>
    public string Impact { get; init; } = string.Empty;
    
    /// <summary>
    /// Mitigation strategies
    /// </summary>
    public List<string> MitigationStrategies { get; init; } = new();
    
    /// <summary>
    /// Confidence in bias detection (0-1)
    /// </summary>
    public double DetectionConfidence { get; init; }
}

/// <summary>
/// Evaluation of logical consistency
/// </summary>
public record LogicalConsistencyEvaluation
{
    /// <summary>
    /// Overall consistency score (0-1)
    /// </summary>
    public double OverallConsistencyScore { get; init; }
    
    /// <summary>
    /// Specific inconsistencies found
    /// </summary>
    public List<string> IdentifiedInconsistencies { get; init; } = new();
    
    /// <summary>
    /// Logical fallacies identified
    /// </summary>
    public List<string> LogicalFallacies { get; init; } = new();
    
    /// <summary>
    /// Gaps in logical flow
    /// </summary>
    public List<string> ReasoningGaps { get; init; } = new();
    
    /// <summary>
    /// Recommendations for improving consistency
    /// </summary>
    public List<string> ConsistencyRecommendations { get; init; } = new();
    
    /// <summary>
    /// Detailed evaluation text
    /// </summary>
    public string DetailedEvaluation { get; init; } = string.Empty;
}

/// <summary>
/// Assessment of confidence calibration
/// </summary>
public record ConfidenceCalibrationAssessment
{
    /// <summary>
    /// How well-calibrated the confidence is (0-1, where 1 = perfectly calibrated)
    /// </summary>
    public double CalibrationScore { get; init; }
    
    /// <summary>
    /// How appropriate the confidence level is given evidence
    /// </summary>
    public double AppropriatenessScore { get; init; }
    
    /// <summary>
    /// Indicators of overconfidence
    /// </summary>
    public List<string> OverconfidenceIndicators { get; init; } = new();
    
    /// <summary>
    /// Indicators of underconfidence
    /// </summary>
    public List<string> UnderconfidenceIndicators { get; init; } = new();
    
    /// <summary>
    /// Recommendations for improving calibration
    /// </summary>
    public List<string> CalibrationRecommendations { get; init; } = new();
    
    /// <summary>
    /// Detailed assessment text
    /// </summary>
    public string DetailedAssessment { get; init; } = string.Empty;
}

/// <summary>
/// Recommended improvement to reasoning process
/// </summary>
public record ReasoningImprovement
{
    /// <summary>
    /// Improvement category
    /// </summary>
    public ImprovementCategory Category { get; init; }
    
    /// <summary>
    /// Improvement title
    /// </summary>
    public string Title { get; init; } = string.Empty;
    
    /// <summary>
    /// Detailed description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Priority level
    /// </summary>
    public ImprovementPriority Priority { get; init; }
    
    /// <summary>
    /// Expected impact if implemented
    /// </summary>
    public double ExpectedImpact { get; init; }
    
    /// <summary>
    /// Implementation difficulty
    /// </summary>
    public double ImplementationDifficulty { get; init; }
    
    /// <summary>
    /// Specific action steps
    /// </summary>
    public List<string> ActionSteps { get; init; } = new();
    
    /// <summary>
    /// Success metrics
    /// </summary>
    public List<string> SuccessMetrics { get; init; } = new();
}

/// <summary>
/// Meta-meta reflection on the meta-reasoning process
/// </summary>
public record MetaMetaReflection
{
    /// <summary>
    /// Quality of the meta-reasoning analysis itself
    /// </summary>
    public double MetaAnalysisQuality { get; init; }
    
    /// <summary>
    /// Potential limitations of the meta-reasoning
    /// </summary>
    public List<string> MetaLimitations { get; init; } = new();
    
    /// <summary>
    /// Assumptions made during meta-reasoning
    /// </summary>
    public List<string> MetaAssumptions { get; init; } = new();
    
    /// <summary>
    /// Confidence in the meta-reasoning conclusions
    /// </summary>
    public double MetaConfidence { get; init; }
    
    /// <summary>
    /// Suggestions for improving meta-reasoning
    /// </summary>
    public List<string> MetaImprovements { get; init; } = new();
    
    /// <summary>
    /// Self-critique of the meta-reasoning process
    /// </summary>
    public string SelfCritique { get; init; } = string.Empty;
}

/// <summary>
/// Reflection loop for continuous improvement
/// </summary>
public record ReflectionLoop
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Time window for reflection
    /// </summary>
    public TimeSpan ReflectionWindow { get; init; }
    
    /// <summary>
    /// When reflection started
    /// </summary>
    public DateTime StartTime { get; init; }
    
    /// <summary>
    /// When reflection completed
    /// </summary>
    public DateTime? EndTime { get; init; }
    
    /// <summary>
    /// Duration of reflection loop
    /// </summary>
    public TimeSpan? LoopDuration { get; init; }
    
    /// <summary>
    /// Reasoning history analyzed
    /// </summary>
    public List<AgentReasoningTrace> ReasoningHistory { get; init; } = new();
    
    /// <summary>
    /// Patterns identified across sessions
    /// </summary>
    public List<CrossSessionPattern> CrossSessionPatterns { get; init; } = new();
    
    /// <summary>
    /// Learning opportunities identified
    /// </summary>
    public List<LearningOpportunity> LearningOpportunities { get; init; } = new();
    
    /// <summary>
    /// Strategic improvements recommended
    /// </summary>
    public List<StrategicImprovement> StrategicImprovements { get; init; } = new();
    
    /// <summary>
    /// Adaptive feedback mechanisms
    /// </summary>
    public AdaptiveFeedback AdaptiveFeedback { get; init; } = new();
    
    /// <summary>
    /// Error message if reflection failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Real-time reasoning adjustment
/// </summary>
public record ReasoningAdjustment
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Current reasoning being monitored
    /// </summary>
    public AgentReasoningTrace CurrentReasoning { get; init; } = new();
    
    /// <summary>
    /// When adjustment was made
    /// </summary>
    public DateTime AdjustmentTimestamp { get; init; }
    
    /// <summary>
    /// Real-time quality check results
    /// </summary>
    public RealTimeQualityCheck QualityCheck { get; init; } = new();
    
    /// <summary>
    /// Pattern matching results
    /// </summary>
    public PatternMatchResult PatternMatch { get; init; } = new();
    
    /// <summary>
    /// Dynamic strategy adjustment
    /// </summary>
    public DynamicStrategyAdjustment StrategyAdjustment { get; init; } = new();
    
    /// <summary>
    /// Confidence adjustment
    /// </summary>
    public ConfidenceAdjustment ConfidenceAdjustment { get; init; } = new();
    
    /// <summary>
    /// Whether adjustment is recommended
    /// </summary>
    public bool AdjustmentRecommended { get; init; }
    
    /// <summary>
    /// Urgency of adjustment
    /// </summary>
    public AdjustmentUrgency AdjustmentUrgency { get; init; }
    
    /// <summary>
    /// Error message if adjustment failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Configuration for meta-reasoning system
/// </summary>
public record MetaReasoningConfig
{
    /// <summary>
    /// Minimum quality threshold for reasoning
    /// </summary>
    public double MinQualityThreshold { get; init; } = 0.6;
    
    /// <summary>
    /// Maximum time for meta-reasoning analysis
    /// </summary>
    public TimeSpan MaxAnalysisTime { get; init; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Whether to enable real-time monitoring
    /// </summary>
    public bool EnableRealTimeMonitoring { get; init; } = true;
    
    /// <summary>
    /// Whether to enable reflection loops
    /// </summary>
    public bool EnableReflectionLoops { get; init; } = true;
    
    /// <summary>
    /// Frequency of reflection loops
    /// </summary>
    public TimeSpan ReflectionFrequency { get; init; } = TimeSpan.FromDays(1);
    
    /// <summary>
    /// Number of reasoning sessions to include in reflection
    /// </summary>
    public int MaxSessionsInReflection { get; init; } = 10;
}

// Supporting data structures

public record CrossSessionPattern
{
    public string PatternName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Frequency { get; init; }
    public double Impact { get; init; }
    public List<string> Examples { get; init; } = new();
}

public record LearningOpportunity
{
    public string OpportunityName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double PotentialImpact { get; init; }
    public List<string> RequiredActions { get; init; } = new();
}

public record StrategicImprovement
{
    public string ImprovementName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double ExpectedBenefit { get; init; }
    public List<string> ImplementationSteps { get; init; } = new();
}

public record AdaptiveFeedback
{
    public List<string> FeedbackMechanisms { get; init; } = new();
    public double AdaptationRate { get; init; }
    public List<string> TriggerConditions { get; init; } = new();
}

public record RealTimeQualityCheck
{
    public double OverallScore { get; init; }
    public List<string> QualityIssues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
}

public record PatternMatchResult
{
    public List<string> MatchedPatterns { get; init; } = new();
    public double MatchConfidence { get; init; }
    public List<string> Deviations { get; init; } = new();
}

public record DynamicStrategyAdjustment
{
    public string RecommendedStrategy { get; init; } = string.Empty;
    public string Justification { get; init; } = string.Empty;
    public double ExpectedImprovement { get; init; }
}

public record ConfidenceAdjustment
{
    public double OriginalConfidence { get; init; }
    public double AdjustedConfidence { get; init; }
    public string AdjustmentReason { get; init; } = string.Empty;
}

// Enums

public enum ReasoningPatternType
{
    Sequential,
    Parallel,
    Iterative,
    Hierarchical,
    Circular,
    Random
}

public enum BiasType
{
    ConfirmationBias,
    AnchoringBias,
    AvailabilityHeuristic,
    OverconfidenceBias,
    AttributionBias,
    HindsightBias,
    GroupthinkBias
}

public enum ImprovementCategory
{
    LogicalStructure,
    EvidenceHandling,
    BiasReduction,
    ConfidenceCalibration,
    AlternativeConsideration,
    AssumptionManagement,
    MetaCognition
}

public enum ImprovementPriority
{
    Critical,
    High,
    Medium,
    Low
}