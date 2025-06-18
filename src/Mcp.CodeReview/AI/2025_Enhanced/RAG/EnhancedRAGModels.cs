using Mcp.CodeReview.Models;
using Mcp.CodeReview.AI.Enhanced2025.Critics;
using Mcp.CodeReview.AI.Enhanced2025.TreeOfThoughts;

namespace Mcp.CodeReview.AI.Enhanced2025.RAG;

/// <summary>
/// Record of agent interaction for learning and pattern recognition
/// </summary>
public record AgentInteractionRecord
{
    /// <summary>
    /// Unique identifier for the interaction
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Type of interaction (debate, challenge, synthesis, etc.)
    /// </summary>
    public AgentInteractionType InteractionType { get; init; }
    
    /// <summary>
    /// Agents involved in the interaction
    /// </summary>
    public List<AgentType> Participants { get; init; } = new();
    
    /// <summary>
    /// Original request that triggered the interaction
    /// </summary>
    public CodeReviewRequest OriginalRequest { get; init; } = new();
    
    /// <summary>
    /// Outcome of the interaction
    /// </summary>
    public string Outcome { get; init; } = string.Empty;
    
    /// <summary>
    /// Quality score of the interaction
    /// </summary>
    public double QualityScore { get; init; }
    
    /// <summary>
    /// Confidence score in the outcome
    /// </summary>
    public double ConfidenceScore { get; init; }
    
    /// <summary>
    /// Key learnings from the interaction
    /// </summary>
    public List<string> KeyLearnings { get; init; } = new();
    
    /// <summary>
    /// When the interaction occurred
    /// </summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>
    /// Additional context information
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
    
    /// <summary>
    /// Patterns identified during interaction
    /// </summary>
    public List<string> IdentifiedPatterns { get; init; } = new();
    
    /// <summary>
    /// Challenges encountered and resolved
    /// </summary>
    public List<string> ChallengesResolved { get; init; } = new();
    
    /// <summary>
    /// Improvements made to analysis
    /// </summary>
    public List<string> AnalysisImprovements { get; init; } = new();
}

/// <summary>
/// Enhanced RAG context with historical patterns and learnings
/// </summary>
public record RAGEnhancedContext
{
    /// <summary>
    /// Unique identifier for this context retrieval
    /// </summary>
    public Guid RequestId { get; init; }
    
    /// <summary>
    /// Original request being analyzed
    /// </summary>
    public CodeReviewRequest OriginalRequest { get; init; } = new();
    
    /// <summary>
    /// Target agent type for context
    /// </summary>
    public AgentType TargetAgentType { get; init; }
    
    /// <summary>
    /// Similar code patterns from history
    /// </summary>
    public List<CodePattern> SimilarCodePatterns { get; init; } = new();
    
    /// <summary>
    /// Successful reasoning chains from past analyses
    /// </summary>
    public List<ReasoningChain> HistoricalReasoningChains { get; init; } = new();
    
    /// <summary>
    /// Common issue patterns for this type of code
    /// </summary>
    public List<IssuePattern> CommonIssuePatterns { get; init; } = new();
    
    /// <summary>
    /// Learnings from past agent interactions
    /// </summary>
    public List<InteractionLearning> AgentInteractionLearnings { get; init; } = new();
    
    /// <summary>
    /// Patterns from challenge-response cycles
    /// </summary>
    public List<ChallengeResponsePattern> ChallengeResponsePatterns { get; init; } = new();
    
    /// <summary>
    /// Contextual insights generated from retrieved data
    /// </summary>
    public List<ContextualInsight> ContextualInsights { get; init; } = new();
    
    /// <summary>
    /// Confidence scores for different context elements
    /// </summary>
    public Dictionary<string, double> ContextConfidenceScores { get; init; } = new();
    
    /// <summary>
    /// When this context was retrieved
    /// </summary>
    public DateTime RetrievalTimestamp { get; init; }
    
    /// <summary>
    /// Relevance scores for retrieved items
    /// </summary>
    public Dictionary<string, double> RelevanceScores { get; init; } = new();
    
    /// <summary>
    /// Recommended focus areas based on historical patterns
    /// </summary>
    public List<string> RecommendedFocusAreas { get; init; } = new();
    
    /// <summary>
    /// Potential pitfalls to avoid based on past experiences
    /// </summary>
    public List<string> PotentialPitfalls { get; init; } = new();
}

/// <summary>
/// Code pattern identified from historical analysis
/// </summary>
public record CodePattern
{
    /// <summary>
    /// Unique identifier for the pattern
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Pattern name or description
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Code snippet or structure representing the pattern
    /// </summary>
    public string CodeSnippet { get; init; } = string.Empty;
    
    /// <summary>
    /// Programming language
    /// </summary>
    public string Language { get; init; } = string.Empty;
    
    /// <summary>
    /// Common issues found with this pattern
    /// </summary>
    public List<Finding> CommonIssues { get; init; } = new();
    
    /// <summary>
    /// Successful analysis approaches for this pattern
    /// </summary>
    public List<string> SuccessfulApproaches { get; init; } = new();
    
    /// <summary>
    /// Frequency of occurrence
    /// </summary>
    public int Frequency { get; init; }
    
    /// <summary>
    /// Confidence in pattern recognition
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// When pattern was first identified
    /// </summary>
    public DateTime FirstIdentified { get; init; }
    
    /// <summary>
    /// When pattern was last seen
    /// </summary>
    public DateTime LastSeen { get; init; }
    
    /// <summary>
    /// Contextual information about usage
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
}

/// <summary>
/// Successful reasoning chain from past analysis
/// </summary>
public record ReasoningChain
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Agent type that used this reasoning
    /// </summary>
    public AgentType AgentType { get; init; }
    
    /// <summary>
    /// Steps in the reasoning process
    /// </summary>
    public List<string> ReasoningSteps { get; init; } = new();
    
    /// <summary>
    /// Context where reasoning was successful
    /// </summary>
    public string Context { get; init; } = string.Empty;
    
    /// <summary>
    /// Findings produced by this reasoning
    /// </summary>
    public List<Finding> ProducedFindings { get; init; } = new();
    
    /// <summary>
    /// Success metrics for this reasoning chain
    /// </summary>
    public ReasoningSuccessMetrics SuccessMetrics { get; init; } = new();
    
    /// <summary>
    /// When this reasoning was used
    /// </summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>
    /// Confidence in the reasoning effectiveness
    /// </summary>
    public double Effectiveness { get; init; }
    
    /// <summary>
    /// Conditions under which this reasoning works best
    /// </summary>
    public List<string> OptimalConditions { get; init; } = new();
}

/// <summary>
/// Pattern of common issues for specific code types
/// </summary>
public record IssuePattern
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Pattern name or description
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of issue
    /// </summary>
    public string IssueType { get; init; } = string.Empty;
    
    /// <summary>
    /// Code characteristics that trigger this issue
    /// </summary>
    public List<string> TriggerCharacteristics { get; init; } = new();
    
    /// <summary>
    /// Common manifestations of the issue
    /// </summary>
    public List<Finding> CommonManifestations { get; init; } = new();
    
    /// <summary>
    /// Detection strategies that work well
    /// </summary>
    public List<string> DetectionStrategies { get; init; } = new();
    
    /// <summary>
    /// Resolution approaches
    /// </summary>
    public List<string> ResolutionApproaches { get; init; } = new();
    
    /// <summary>
    /// Frequency of occurrence
    /// </summary>
    public int Frequency { get; init; }
    
    /// <summary>
    /// Severity distribution
    /// </summary>
    public Dictionary<string, int> SeverityDistribution { get; init; } = new();
    
    /// <summary>
    /// Languages where this pattern is common
    /// </summary>
    public List<string> CommonLanguages { get; init; } = new();
}

/// <summary>
/// Learning from agent interactions
/// </summary>
public record InteractionLearning
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Type of interaction
    /// </summary>
    public AgentInteractionType InteractionType { get; init; }
    
    /// <summary>
    /// Agents involved
    /// </summary>
    public List<AgentType> InvolvedAgents { get; init; } = new();
    
    /// <summary>
    /// Key insight learned
    /// </summary>
    public string KeyInsight { get; init; } = string.Empty;
    
    /// <summary>
    /// Conditions under which this learning applies
    /// </summary>
    public List<string> ApplicableConditions { get; init; } = new();
    
    /// <summary>
    /// Evidence supporting this learning
    /// </summary>
    public List<string> SupportingEvidence { get; init; } = new();
    
    /// <summary>
    /// Confidence in the learning
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// How many times this learning has been validated
    /// </summary>
    public int ValidationCount { get; init; }
    
    /// <summary>
    /// When this learning was first discovered
    /// </summary>
    public DateTime FirstDiscovered { get; init; }
    
    /// <summary>
    /// When this learning was last validated
    /// </summary>
    public DateTime LastValidated { get; init; }
}

/// <summary>
/// Pattern from challenge-response interactions
/// </summary>
public record ChallengeResponsePattern
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Type of challenge
    /// </summary>
    public ChallengeType ChallengeType { get; init; }
    
    /// <summary>
    /// Agent type that typically makes this challenge
    /// </summary>
    public AgentType ChallengerType { get; init; }
    
    /// <summary>
    /// Agent type that typically receives this challenge
    /// </summary>
    public AgentType TargetType { get; init; }
    
    /// <summary>
    /// Common challenge patterns
    /// </summary>
    public List<string> CommonChallengePatterns { get; init; } = new();
    
    /// <summary>
    /// Effective response strategies
    /// </summary>
    public List<string> EffectiveResponseStrategies { get; init; } = new();
    
    /// <summary>
    /// Outcomes that typically result
    /// </summary>
    public List<string> TypicalOutcomes { get; init; } = new();
    
    /// <summary>
    /// Success rate of this challenge type
    /// </summary>
    public double SuccessRate { get; init; }
    
    /// <summary>
    /// Context where this pattern is most effective
    /// </summary>
    public string OptimalContext { get; init; } = string.Empty;
    
    /// <summary>
    /// Frequency of occurrence
    /// </summary>
    public int Frequency { get; init; }
}

/// <summary>
/// Contextual insight generated from RAG retrieval
/// </summary>
public record ContextualInsight
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Type of insight
    /// </summary>
    public InsightType InsightType { get; init; }
    
    /// <summary>
    /// Description of the insight
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Confidence in the insight
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Sources that contributed to this insight
    /// </summary>
    public List<string> Sources { get; init; } = new();
    
    /// <summary>
    /// Actionable recommendations based on insight
    /// </summary>
    public List<string> Recommendations { get; init; } = new();
    
    /// <summary>
    /// Potential risks or concerns
    /// </summary>
    public List<string> Risks { get; init; } = new();
    
    /// <summary>
    /// When this insight was generated
    /// </summary>
    public DateTime GeneratedTimestamp { get; init; }
}

/// <summary>
/// System learning analysis over time
/// </summary>
public record SystemLearningAnalysis
{
    /// <summary>
    /// Time window analyzed
    /// </summary>
    public TimeSpan AnalysisWindow { get; init; }
    
    /// <summary>
    /// When analysis was performed
    /// </summary>
    public DateTime AnalysisTimestamp { get; init; }
    
    /// <summary>
    /// Debate effectiveness trends
    /// </summary>
    public DebateEffectivenessTrend DebateEffectivenessTrends { get; init; } = new();
    
    /// <summary>
    /// Agent performance trends
    /// </summary>
    public Dictionary<AgentType, AgentPerformanceTrend> AgentPerformanceTrends { get; init; } = new();
    
    /// <summary>
    /// Common failure patterns identified
    /// </summary>
    public List<FailurePattern> CommonFailurePatterns { get; init; } = new();
    
    /// <summary>
    /// Improvement opportunities
    /// </summary>
    public List<ImprovementOpportunity> ImprovementOpportunities { get; init; } = new();
    
    /// <summary>
    /// Overall system health metrics
    /// </summary>
    public SystemHealthMetrics HealthMetrics { get; init; } = new();
    
    /// <summary>
    /// Key insights from the analysis
    /// </summary>
    public List<string> KeyInsights { get; init; } = new();
    
    /// <summary>
    /// Recommendations for system improvement
    /// </summary>
    public List<string> SystemRecommendations { get; init; } = new();
}

/// <summary>
/// Configuration for enhanced RAG system
/// </summary>
public record EnhancedRAGConfig
{
    /// <summary>
    /// Maximum number of similar patterns to retrieve
    /// </summary>
    public int MaxSimilarPatterns { get; init; } = 10;
    
    /// <summary>
    /// Maximum number of reasoning chains to retrieve
    /// </summary>
    public int MaxReasoningChains { get; init; } = 5;
    
    /// <summary>
    /// Minimum confidence threshold for retrieval
    /// </summary>
    public double MinConfidenceThreshold { get; init; } = 0.3;
    
    /// <summary>
    /// Time window for recent learnings
    /// </summary>
    public TimeSpan RecentLearningsWindow { get; init; } = TimeSpan.FromDays(30);
    
    /// <summary>
    /// Whether to enable pattern learning
    /// </summary>
    public bool EnablePatternLearning { get; init; } = true;
    
    /// <summary>
    /// Whether to enable interaction learning
    /// </summary>
    public bool EnableInteractionLearning { get; init; } = true;
    
    /// <summary>
    /// Maximum age of data to consider
    /// </summary>
    public TimeSpan MaxDataAge { get; init; } = TimeSpan.FromDays(365);
    
    /// <summary>
    /// Frequency of pattern analysis updates
    /// </summary>
    public TimeSpan PatternAnalysisFrequency { get; init; } = TimeSpan.FromDays(7);
}

// Supporting data structures

public record ReasoningSuccessMetrics
{
    public double AccuracyScore { get; init; }
    public double CompletenessScore { get; init; }
    public double EfficiencyScore { get; init; }
    public double OverallScore => (AccuracyScore + CompletenessScore + EfficiencyScore) / 3.0;
}

public record DebateEffectivenessTrend
{
    public double AverageConsensusRate { get; init; }
    public double AverageQualityImprovement { get; init; }
    public int TotalDebates { get; init; }
    public Dictionary<AgentType, double> AgentContributions { get; init; } = new();
    public List<string> ImprovingAreas { get; init; } = new();
    public List<string> DecliningAreas { get; init; } = new();
}

public record AgentPerformanceTrend
{
    public double AverageConfidence { get; init; }
    public double AverageQualityScore { get; init; }
    public int TotalInteractions { get; init; }
    public List<string> StrengthAreas { get; init; } = new();
    public List<string> ImprovementAreas { get; init; } = new();
    public double TrendDirection { get; init; } // Positive = improving, Negative = declining
}

public record FailurePattern
{
    public string PatternName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int Frequency { get; init; }
    public List<string> CommonCauses { get; init; } = new();
    public List<string> PreventionStrategies { get; init; } = new();
    public double ImpactSeverity { get; init; }
}

public record ImprovementOpportunity
{
    public string OpportunityName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double PotentialImpact { get; init; }
    public double ImplementationDifficulty { get; init; }
    public List<string> RequiredActions { get; init; } = new();
    public TimeSpan EstimatedTimeframe { get; init; }
    public double PriorityScore => PotentialImpact / (ImplementationDifficulty + 0.1);
}

public record SystemHealthMetrics
{
    public double OverallHealthScore { get; init; }
    public double LearningEffectiveness { get; init; }
    public double AdaptabilityScore { get; init; }
    public double ConsistencyScore { get; init; }
    public Dictionary<string, double> ComponentHealthScores { get; init; } = new();
    public List<string> HealthConcerns { get; init; } = new();
    public List<string> HealthStrengths { get; init; } = new();
}

// Enums

public enum AgentInteractionType
{
    Debate,
    Challenge,
    TreeOfThoughts,
    Synthesis,
    Collaboration,
    MetaReasoning,
    PatternRecognition,
    LearningValidation
}

public enum InsightType
{
    PatternRecognition,
    RiskAssessment,
    OpportunityIdentification,
    MethodologyRecommendation,
    QualityImprovement,
    EfficiencyOptimization,
    LearningValidation
}