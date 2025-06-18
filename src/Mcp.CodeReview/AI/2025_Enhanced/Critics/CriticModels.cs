using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.AI.Enhanced2025.Critics;

/// <summary>
/// Complete debate session between multiple agents
/// </summary>
public record DebateSession
{
    /// <summary>
    /// Unique identifier for the debate session
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Original request that sparked the debate
    /// </summary>
    public CodeReviewRequest OriginalRequest { get; init; } = new();
    
    /// <summary>
    /// Agents participating in the debate
    /// </summary>
    public List<DebateParticipant> Participants { get; init; } = new();
    
    /// <summary>
    /// All debate rounds conducted
    /// </summary>
    public List<DebateRound> Rounds { get; init; } = new();
    
    /// <summary>
    /// When the debate started
    /// </summary>
    public DateTime StartTime { get; init; }
    
    /// <summary>
    /// When the debate ended
    /// </summary>
    public DateTime? EndTime { get; init; }
    
    /// <summary>
    /// Total duration of debate
    /// </summary>
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
    
    /// <summary>
    /// Debate configuration used
    /// </summary>
    public CriticSystemConfig Config { get; init; } = new();
    
    /// <summary>
    /// Moderator notes or observations
    /// </summary>
    public List<string> ModeratorNotes { get; init; } = new();
}

/// <summary>
/// Individual agent participating in debate
/// </summary>
public record DebateParticipant
{
    /// <summary>
    /// Type of agent
    /// </summary>
    public AgentType AgentType { get; init; }
    
    /// <summary>
    /// Agent name/identifier
    /// </summary>
    public string AgentName { get; init; } = string.Empty;
    
    /// <summary>
    /// Initial analysis position
    /// </summary>
    public AgentResult InitialPosition { get; init; } = new();
    
    /// <summary>
    /// Current position after debate rounds
    /// </summary>
    public AgentResult? CurrentPosition { get; init; }
    
    /// <summary>
    /// History of confidence levels throughout debate
    /// </summary>
    public List<double> ConfidenceHistory { get; init; } = new();
    
    /// <summary>
    /// Arguments made by this participant
    /// </summary>
    public List<Argument> ArgumentsMade { get; init; } = new();
    
    /// <summary>
    /// Challenges received by this participant
    /// </summary>
    public List<Challenge> ChallengesReceived { get; init; } = new();
    
    /// <summary>
    /// Responses given to challenges
    /// </summary>
    public List<ChallengeResponse> ResponsesGiven { get; init; } = new();
    
    /// <summary>
    /// How much this participant's position changed during debate
    /// </summary>
    public double PositionChangeScore { get; init; }
    
    /// <summary>
    /// Participant's satisfaction with debate outcome
    /// </summary>
    public double SatisfactionScore { get; init; }
}

/// <summary>
/// Individual round of debate
/// </summary>
public record DebateRound
{
    /// <summary>
    /// Round number (1, 2, 3, etc.)
    /// </summary>
    public int RoundNumber { get; init; }
    
    /// <summary>
    /// Type of round
    /// </summary>
    public DebateRoundType RoundType { get; init; }
    
    /// <summary>
    /// When the round started
    /// </summary>
    public DateTime StartTime { get; init; }
    
    /// <summary>
    /// When the round ended
    /// </summary>
    public DateTime? EndTime { get; init; }
    
    /// <summary>
    /// Challenges made in this round
    /// </summary>
    public List<Challenge> Challenges { get; init; } = new();
    
    /// <summary>
    /// Responses given in this round
    /// </summary>
    public List<ChallengeResponse> Responses { get; init; } = new();
    
    /// <summary>
    /// Arguments presented in this round
    /// </summary>
    public List<Argument> Arguments { get; init; } = new();
    
    /// <summary>
    /// Synthesis produced in this round (if applicable)
    /// </summary>
    public DebateSynthesis? Synthesis { get; init; }
    
    /// <summary>
    /// Round summary
    /// </summary>
    public string Summary { get; init; } = string.Empty;
    
    /// <summary>
    /// Key insights from this round
    /// </summary>
    public List<string> KeyInsights { get; init; } = new();
}

/// <summary>
/// Challenge made by one agent to another
/// </summary>
public record Challenge
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Agent making the challenge
    /// </summary>
    public AgentType ChallengerId { get; init; }
    
    /// <summary>
    /// Agent being challenged
    /// </summary>
    public AgentType TargetId { get; init; }
    
    /// <summary>
    /// Full text of the challenge
    /// </summary>
    public string ChallengeText { get; init; } = string.Empty;
    
    /// <summary>
    /// Specific concerns raised
    /// </summary>
    public List<string> KeyConcerns { get; init; } = new();
    
    /// <summary>
    /// Counter-evidence provided
    /// </summary>
    public List<string> CounterEvidence { get; init; } = new();
    
    /// <summary>
    /// Missing considerations identified
    /// </summary>
    public List<string> MissingConsiderations { get; init; } = new();
    
    /// <summary>
    /// Assessment of target's confidence level
    /// </summary>
    public string ConfidenceAssessment { get; init; } = string.Empty;
    
    /// <summary>
    /// Severity of the challenge
    /// </summary>
    public ChallengeSeverity Severity { get; init; }
    
    /// <summary>
    /// Type of challenge
    /// </summary>
    public ChallengeType Type { get; init; }
    
    /// <summary>
    /// When challenge was made
    /// </summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>
    /// Supporting evidence for the challenge
    /// </summary>
    public List<Evidence> SupportingEvidence { get; init; } = new();
}

/// <summary>
/// Response to a challenge
/// </summary>
public record ChallengeResponse
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Agent giving the response
    /// </summary>
    public AgentType ResponderId { get; init; }
    
    /// <summary>
    /// Challenges being responded to
    /// </summary>
    public List<Guid> ChallengeIds { get; init; } = new();
    
    /// <summary>
    /// Full response text
    /// </summary>
    public string ResponseText { get; init; } = string.Empty;
    
    /// <summary>
    /// Revised findings based on challenges
    /// </summary>
    public List<Finding> RevisedFindings { get; init; } = new();
    
    /// <summary>
    /// Adjustments to confidence levels
    /// </summary>
    public Dictionary<string, double> ConfidenceAdjustments { get; init; } = new();
    
    /// <summary>
    /// Points acknowledged as valid
    /// </summary>
    public List<string> AcknowledgedPoints { get; init; } = new();
    
    /// <summary>
    /// Positions defended with additional evidence
    /// </summary>
    public List<string> DefendedPositions { get; init; } = new();
    
    /// <summary>
    /// What was learned from the challenges
    /// </summary>
    public List<string> LearningsFromChallenges { get; init; } = new();
    
    /// <summary>
    /// When response was given
    /// </summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>
    /// Quality of the response
    /// </summary>
    public ResponseQuality Quality { get; init; }
}

/// <summary>
/// Argument made during debate
/// </summary>
public record Argument
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Agent making the argument
    /// </summary>
    public AgentType ArgumentMaker { get; init; }
    
    /// <summary>
    /// Main claim or assertion
    /// </summary>
    public string Claim { get; init; } = string.Empty;
    
    /// <summary>
    /// Evidence supporting the claim
    /// </summary>
    public List<Evidence> SupportingEvidence { get; init; } = new();
    
    /// <summary>
    /// Reasoning chain
    /// </summary>
    public List<string> ReasoningSteps { get; init; } = new();
    
    /// <summary>
    /// Confidence in the argument
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Type of argument
    /// </summary>
    public ArgumentType Type { get; init; }
    
    /// <summary>
    /// Potential counter-arguments considered
    /// </summary>
    public List<string> CounterArgumentsConsidered { get; init; } = new();
    
    /// <summary>
    /// When argument was made
    /// </summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Synthesis of debate outcomes
/// </summary>
public record DebateSynthesis
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Full synthesis text
    /// </summary>
    public string SynthesisText { get; init; } = string.Empty;
    
    /// <summary>
    /// Findings with broad consensus
    /// </summary>
    public List<Finding> ConsensusFindings { get; init; } = new();
    
    /// <summary>
    /// Findings still in dispute
    /// </summary>
    public List<Finding> DisputedFindings { get; init; } = new();
    
    /// <summary>
    /// Areas requiring further investigation
    /// </summary>
    public List<string> UncertainAreas { get; init; } = new();
    
    /// <summary>
    /// Quality improvements achieved through debate
    /// </summary>
    public List<string> QualityImprovements { get; init; } = new();
    
    /// <summary>
    /// Key insights that emerged
    /// </summary>
    public List<string> EmergentInsights { get; init; } = new();
    
    /// <summary>
    /// Confidence weights for different findings
    /// </summary>
    public Dictionary<string, double> ConfidenceWeights { get; init; } = new();
    
    /// <summary>
    /// When synthesis was created
    /// </summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>
    /// Quality metrics for the synthesis
    /// </summary>
    public SynthesisQualityScore QualityScore { get; init; } = new();
}

/// <summary>
/// Final evaluation of debate outcome
/// </summary>
public record DebateEvaluation
{
    /// <summary>
    /// Final consensus findings
    /// </summary>
    public List<Finding> ConsensusFindingsAgency { get; init; } = new();
    
    /// <summary>
    /// Findings that remain disputed
    /// </summary>
    public List<Finding> DisputedFindings { get; init; } = new();
    
    /// <summary>
    /// Overall confidence in results
    /// </summary>
    public double OverallConfidence { get; init; }
    
    /// <summary>
    /// Quality score of the debate process
    /// </summary>
    public double QualityScore { get; init; }
    
    /// <summary>
    /// How satisfied each participant was
    /// </summary>
    public Dictionary<AgentType, double> ParticipantSatisfaction { get; init; } = new();
    
    /// <summary>
    /// Key insights that emerged
    /// </summary>
    public List<string> KeyInsights { get; init; } = new();
    
    /// <summary>
    /// Areas identified for future improvement
    /// </summary>
    public List<string> AreasForImprovement { get; init; } = new();
    
    /// <summary>
    /// Effectiveness metrics
    /// </summary>
    public DebateEffectivenessMetrics EffectivenessMetrics { get; init; } = new();
}

/// <summary>
/// Result of entire debate process
/// </summary>
public record DebateResult
{
    /// <summary>
    /// Complete debate session
    /// </summary>
    public DebateSession DebateSession { get; init; } = new();
    
    /// <summary>
    /// Final evaluation
    /// </summary>
    public DebateEvaluation FinalEvaluation { get; init; } = new();
    
    /// <summary>
    /// Refined findings after debate
    /// </summary>
    public List<Finding> RefinedFindings { get; init; } = new();
    
    /// <summary>
    /// How confidence levels changed for each agent
    /// </summary>
    public Dictionary<AgentType, List<double>> ConfidenceChanges { get; init; } = new();
    
    /// <summary>
    /// Measure of quality improvement achieved
    /// </summary>
    public double QualityImprovement { get; init; }
    
    /// <summary>
    /// Total execution time
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }
    
    /// <summary>
    /// Number of debate rounds conducted
    /// </summary>
    public int TotalRounds { get; init; }
    
    /// <summary>
    /// Success indicators
    /// </summary>
    public DebateSuccessIndicators SuccessIndicators { get; init; } = new();
}

/// <summary>
/// Configuration for critic system behavior
/// </summary>
public record CriticSystemConfig
{
    /// <summary>
    /// Maximum number of debate rounds
    /// </summary>
    public int MaxRounds { get; init; } = 3;
    
    /// <summary>
    /// Maximum time for entire debate
    /// </summary>
    public TimeSpan MaxDebateTime { get; init; } = TimeSpan.FromMinutes(15);
    
    /// <summary>
    /// Minimum confidence threshold for findings
    /// </summary>
    public double MinConfidenceThreshold { get; init; } = 0.3;
    
    /// <summary>
    /// Whether to enable aggressive challenging
    /// </summary>
    public bool EnableAggressiveChallenging { get; init; } = false;
    
    /// <summary>
    /// Consensus threshold for findings agreement
    /// </summary>
    public double ConsensusThreshold { get; init; } = 0.7;
    
    /// <summary>
    /// Whether to enable parallel challenge processing
    /// </summary>
    public bool EnableParallelProcessing { get; init; } = true;
    
    /// <summary>
    /// Quality improvement threshold to continue debate
    /// </summary>
    public double QualityImprovementThreshold { get; init; } = 0.05;
}

/// <summary>
/// Quality score for synthesis
/// </summary>
public record SynthesisQualityScore
{
    /// <summary>
    /// How well different perspectives were integrated
    /// </summary>
    public double IntegrationScore { get; init; }
    
    /// <summary>
    /// Consistency of final synthesis
    /// </summary>
    public double ConsistencyScore { get; init; }
    
    /// <summary>
    /// Completeness of synthesis
    /// </summary>
    public double CompletenessScore { get; init; }
    
    /// <summary>
    /// Overall synthesis quality
    /// </summary>
    public double OverallScore => (IntegrationScore + ConsistencyScore + CompletenessScore) / 3.0;
}

/// <summary>
/// Metrics for debate effectiveness
/// </summary>
public record DebateEffectivenessMetrics
{
    /// <summary>
    /// Percentage of findings that achieved consensus
    /// </summary>
    public double ConsensusRate { get; init; }
    
    /// <summary>
    /// Average confidence improvement across agents
    /// </summary>
    public double ConfidenceImprovement { get; init; }
    
    /// <summary>
    /// Number of hallucinations identified and corrected
    /// </summary>
    public int HallucinationsCorrected { get; init; }
    
    /// <summary>
    /// Quality of argumentation (0-1 scale)
    /// </summary>
    public double ArgumentationQuality { get; init; }
    
    /// <summary>
    /// Efficiency of debate process
    /// </summary>
    public double ProcessEfficiency { get; init; }
}

/// <summary>
/// Success indicators for debate
/// </summary>
public record DebateSuccessIndicators
{
    /// <summary>
    /// Whether debate reached satisfactory consensus
    /// </summary>
    public bool AchievedConsensus { get; init; }
    
    /// <summary>
    /// Whether quality improved significantly
    /// </summary>
    public bool ImprovedQuality { get; init; }
    
    /// <summary>
    /// Whether participants were satisfied
    /// </summary>
    public bool ParticipantSatisfaction { get; init; }
    
    /// <summary>
    /// Whether debate completed within time limits
    /// </summary>
    public bool CompletedOnTime { get; init; }
    
    /// <summary>
    /// Overall success score
    /// </summary>
    public double OverallSuccessScore { get; init; }
}

// Enums for classification

public enum DebateRoundType
{
    Challenge,
    Response,
    Synthesis,
    Clarification,
    FinalArguments
}

public enum ChallengeSeverity
{
    Minor,
    Moderate,
    Significant,
    Critical
}

public enum ChallengeType
{
    LogicalFlaw,
    MissingEvidence,
    OverConfidence,
    BiasIdentification,
    MethodologicalIssue,
    IncompleteAnalysis,
    AlternativePerspective
}

public enum ResponseQuality
{
    Poor,
    Adequate,
    Good,
    Excellent
}

public enum ArgumentType
{
    EvidenceBased,
    LogicalReasoning,
    ExpertJudgment,
    Comparative,
    Causal,
    Probabilistic
}