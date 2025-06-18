using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.AI.Enhanced2025.Conversations;

/// <summary>
/// Complete conversation session with nested exchanges
/// </summary>
public record Conversation
{
    /// <summary>
    /// Unique identifier for the conversation
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Main topic of conversation
    /// </summary>
    public string Topic { get; init; } = string.Empty;
    
    /// <summary>
    /// Participants in the conversation
    /// </summary>
    public List<ConversationParticipant> Participants { get; init; } = new();
    
    /// <summary>
    /// Conversation tree structure
    /// </summary>
    public ConversationTree ConversationTree { get; init; } = new();
    
    /// <summary>
    /// Conversation phases
    /// </summary>
    public List<ConversationPhase> Phases { get; init; } = new();
    
    /// <summary>
    /// When conversation started
    /// </summary>
    public DateTime StartTime { get; init; }
    
    /// <summary>
    /// When conversation ended
    /// </summary>
    public DateTime? EndTime { get; init; }
    
    /// <summary>
    /// Total conversation duration
    /// </summary>
    public TimeSpan Duration => EndTime?.Subtract(StartTime) ?? TimeSpan.Zero;
    
    /// <summary>
    /// Current status of conversation
    /// </summary>
    public ConversationStatus Status { get; init; }
    
    /// <summary>
    /// Final synthesis of the conversation
    /// </summary>
    public ConversationSynthesis? FinalSynthesis { get; init; }
    
    /// <summary>
    /// Configuration used for conversation
    /// </summary>
    public ConversationConfig Config { get; init; } = new();
    
    /// <summary>
    /// Error message if conversation failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Individual participant in conversation
/// </summary>
public record ConversationParticipant
{
    /// <summary>
    /// Agent type
    /// </summary>
    public AgentType AgentType { get; init; }
    
    /// <summary>
    /// Role in the conversation
    /// </summary>
    public string Role { get; init; } = string.Empty;
    
    /// <summary>
    /// Areas of expertise
    /// </summary>
    public List<string> Expertise { get; init; } = new();
    
    /// <summary>
    /// Initial context for participant
    /// </summary>
    public object? InitialContext { get; init; }
    
    /// <summary>
    /// Contributions made by this participant
    /// </summary>
    public List<ConversationExchange> Contributions { get; init; } = new();
    
    /// <summary>
    /// Final reflection from participant
    /// </summary>
    public string? FinalReflection { get; init; }
    
    /// <summary>
    /// Engagement metrics
    /// </summary>
    public ParticipantEngagement Engagement { get; init; } = new();
}

/// <summary>
/// Tree structure representing conversation flow
/// </summary>
public record ConversationTree
{
    /// <summary>
    /// Main conversation thread
    /// </summary>
    public List<ConversationExchange> MainThread { get; init; } = new();
    
    /// <summary>
    /// Sub-conversations branched from main thread
    /// </summary>
    public List<SubConversation> SubConversations { get; init; } = new();
    
    /// <summary>
    /// Cross-references between exchanges
    /// </summary>
    public List<ExchangeReference> CrossReferences { get; init; } = new();
    
    /// <summary>
    /// Get total number of exchanges across all threads
    /// </summary>
    public int GetTotalExchangeCount()
    {
        return MainThread.Count + SubConversations.Sum(sc => sc.Exchanges.Count);
    }
    
    /// <summary>
    /// Get key exchanges that are most important
    /// </summary>
    public List<ConversationExchange> GetKeyExchanges()
    {
        return MainThread.Where(e => e.ImportanceScore > 0.7).ToList();
    }
    
    /// <summary>
    /// Add an exchange to the main thread
    /// </summary>
    public void AddExchange(ConversationExchange exchange)
    {
        MainThread.Add(exchange);
    }
    
    /// <summary>
    /// Add a sub-conversation
    /// </summary>
    public void AddSubConversation(SubConversation subConversation)
    {
        SubConversations.Add(subConversation);
    }
}

/// <summary>
/// Individual exchange in conversation
/// </summary>
public record ConversationExchange
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Who made this exchange
    /// </summary>
    public AgentType Speaker { get; init; }
    
    /// <summary>
    /// Type of exchange
    /// </summary>
    public ExchangeType ExchangeType { get; init; }
    
    /// <summary>
    /// Content of the exchange
    /// </summary>
    public string Content { get; init; } = string.Empty;
    
    /// <summary>
    /// When exchange was made
    /// </summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>
    /// Context information
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
    
    /// <summary>
    /// References to other exchanges
    /// </summary>
    public List<Guid> References { get; init; } = new();
    
    /// <summary>
    /// Importance score for this exchange (0-1)
    /// </summary>
    public double ImportanceScore { get; init; }
    
    /// <summary>
    /// Sentiment of the exchange
    /// </summary>
    public ExchangeSentiment Sentiment { get; init; }
    
    /// <summary>
    /// Keywords or tags
    /// </summary>
    public List<string> Tags { get; init; } = new();
}

/// <summary>
/// Sub-conversation within main conversation
/// </summary>
public record SubConversation
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Parent conversation ID
    /// </summary>
    public Guid ParentConversationId { get; init; }
    
    /// <summary>
    /// Sub-topic being discussed
    /// </summary>
    public string SubTopic { get; init; } = string.Empty;
    
    /// <summary>
    /// Participants in this sub-conversation
    /// </summary>
    public List<ConversationParticipant> Participants { get; init; } = new();
    
    /// <summary>
    /// Depth level in conversation tree
    /// </summary>
    public int Depth { get; init; }
    
    /// <summary>
    /// Exchanges in this sub-conversation
    /// </summary>
    public List<ConversationExchange> Exchanges { get; init; } = new();
    
    /// <summary>
    /// Context that triggered this sub-conversation
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
    
    /// <summary>
    /// When sub-conversation started
    /// </summary>
    public DateTime StartTime { get; init; }
    
    /// <summary>
    /// When sub-conversation ended
    /// </summary>
    public DateTime? EndTime { get; init; }
    
    /// <summary>
    /// Status of sub-conversation
    /// </summary>
    public ConversationStatus Status { get; init; }
    
    /// <summary>
    /// Insights gained from this sub-conversation
    /// </summary>
    public List<ConversationInsight> Insights { get; init; } = new();
    
    /// <summary>
    /// Error message if sub-conversation failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Phase of conversation (opening, exploration, etc.)
/// </summary>
public record ConversationPhase
{
    /// <summary>
    /// Type of phase
    /// </summary>
    public ConversationPhaseType PhaseType { get; init; }
    
    /// <summary>
    /// When phase started
    /// </summary>
    public DateTime StartTime { get; init; }
    
    /// <summary>
    /// When phase ended
    /// </summary>
    public DateTime? EndTime { get; init; }
    
    /// <summary>
    /// Duration of phase
    /// </summary>
    public TimeSpan? Duration => EndTime?.Subtract(StartTime);
    
    /// <summary>
    /// Objectives for this phase
    /// </summary>
    public List<string> Objectives { get; init; } = new();
    
    /// <summary>
    /// Outcomes achieved in this phase
    /// </summary>
    public List<string> Outcomes { get; init; } = new();
    
    /// <summary>
    /// Success metrics for phase
    /// </summary>
    public PhaseMetrics Metrics { get; init; } = new();
}

/// <summary>
/// Synthesis of conversation outcomes
/// </summary>
public record ConversationSynthesis
{
    /// <summary>
    /// Complete synthesis text
    /// </summary>
    public string SynthesisText { get; init; } = string.Empty;
    
    /// <summary>
    /// Areas of consensus
    /// </summary>
    public List<string> ConsensusAreas { get; init; } = new();
    
    /// <summary>
    /// Areas of disagreement
    /// </summary>
    public List<string> DisagreementAreas { get; init; } = new();
    
    /// <summary>
    /// Emergent insights
    /// </summary>
    public List<string> EmergentInsights { get; init; } = new();
    
    /// <summary>
    /// Knowledge constructed
    /// </summary>
    public List<string> ConstructedKnowledge { get; init; } = new();
    
    /// <summary>
    /// Remaining questions
    /// </summary>
    public List<string> RemainingQuestions { get; init; } = new();
    
    /// <summary>
    /// When synthesis was created
    /// </summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>
    /// Quality score of synthesis (0-1)
    /// </summary>
    public double QualityScore { get; init; }
}

/// <summary>
/// Result of complete conversation
/// </summary>
public record ConversationResult
{
    /// <summary>
    /// The complete conversation
    /// </summary>
    public Conversation Conversation { get; init; } = new();
    
    /// <summary>
    /// Final synthesis
    /// </summary>
    public ConversationSynthesis? FinalSynthesis { get; init; }
    
    /// <summary>
    /// Key insights extracted
    /// </summary>
    public List<string> KeyInsights { get; init; } = new();
    
    /// <summary>
    /// Knowledge constructed during conversation
    /// </summary>
    public List<string> KnowledgeConstructed { get; init; } = new();
    
    /// <summary>
    /// Conversation metrics
    /// </summary>
    public ConversationMetrics ConversationMetrics { get; init; } = new();
    
    /// <summary>
    /// Participant satisfaction scores
    /// </summary>
    public Dictionary<AgentType, double> ParticipantSatisfaction { get; init; } = new();
}

/// <summary>
/// Request for sub-conversation
/// </summary>
public record SubConversationRequest
{
    /// <summary>
    /// Sub-topic to discuss
    /// </summary>
    public string SubTopic { get; init; } = string.Empty;
    
    /// <summary>
    /// Participants for sub-conversation
    /// </summary>
    public List<ConversationParticipant> Participants { get; init; } = new();
    
    /// <summary>
    /// Depth level
    /// </summary>
    public int Depth { get; init; }
    
    /// <summary>
    /// Context information
    /// </summary>
    public Dictionary<string, object> Context { get; init; } = new();
}

/// <summary>
/// Insight gained from conversation
/// </summary>
public record ConversationInsight
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
    /// Insight summary
    /// </summary>
    public string Summary { get; init; } = string.Empty;
    
    /// <summary>
    /// Detailed description
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Confidence in insight (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Sources that contributed to insight
    /// </summary>
    public List<AgentType> Contributors { get; init; } = new();
    
    /// <summary>
    /// When insight was discovered
    /// </summary>
    public DateTime Timestamp { get; init; }
    
    /// <summary>
    /// Evidence supporting insight
    /// </summary>
    public List<string> SupportingEvidence { get; init; } = new();
}

/// <summary>
/// Configuration for conversation behavior
/// </summary>
public record ConversationConfig
{
    /// <summary>
    /// Maximum number of exploration rounds
    /// </summary>
    public int MaxExplorationRounds { get; init; } = 3;
    
    /// <summary>
    /// Maximum sub-conversations per round
    /// </summary>
    public int MaxSubConversationsPerRound { get; init; } = 2;
    
    /// <summary>
    /// Complexity threshold for creating sub-conversations
    /// </summary>
    public double SubConversationComplexityThreshold { get; init; } = 0.7;
    
    /// <summary>
    /// Maximum total conversation time
    /// </summary>
    public TimeSpan MaxConversationTime { get; init; } = TimeSpan.FromMinutes(20);
    
    /// <summary>
    /// Convergence threshold for ending exploration
    /// </summary>
    public double ConvergenceThreshold { get; init; } = 0.8;
    
    /// <summary>
    /// Whether to enable parallel exchanges
    /// </summary>
    public bool EnableParallelExchanges { get; init; } = true;
    
    /// <summary>
    /// Minimum participant engagement required
    /// </summary>
    public double MinParticipantEngagement { get; init; } = 0.5;
}

/// <summary>
/// Metrics for conversation performance
/// </summary>
public record ConversationMetrics
{
    /// <summary>
    /// Total number of exchanges
    /// </summary>
    public int TotalExchanges { get; init; }
    
    /// <summary>
    /// Number of sub-conversations created
    /// </summary>
    public int SubConversationCount { get; init; }
    
    /// <summary>
    /// Average exchange length
    /// </summary>
    public double AverageExchangeLength { get; init; }
    
    /// <summary>
    /// Conversation depth achieved
    /// </summary>
    public int MaxDepthAchieved { get; init; }
    
    /// <summary>
    /// Engagement score across participants
    /// </summary>
    public double OverallEngagement { get; init; }
    
    /// <summary>
    /// Knowledge construction effectiveness
    /// </summary>
    public double KnowledgeConstructionScore { get; init; }
    
    /// <summary>
    /// Overall conversation quality (0-1)
    /// </summary>
    public double OverallQuality { get; init; }
    
    /// <summary>
    /// Convergence achieved
    /// </summary>
    public bool ConvergenceAchieved { get; init; }
}

/// <summary>
/// Participant engagement metrics
/// </summary>
public record ParticipantEngagement
{
    /// <summary>
    /// Number of contributions made
    /// </summary>
    public int ContributionCount { get; init; }
    
    /// <summary>
    /// Average contribution quality (0-1)
    /// </summary>
    public double AverageContributionQuality { get; init; }
    
    /// <summary>
    /// Responsiveness to others (0-1)
    /// </summary>
    public double Responsiveness { get; init; }
    
    /// <summary>
    /// Initiative taken in conversation (0-1)
    /// </summary>
    public double Initiative { get; init; }
    
    /// <summary>
    /// Overall engagement score (0-1)
    /// </summary>
    public double OverallEngagement => (AverageContributionQuality + Responsiveness + Initiative) / 3.0;
}

/// <summary>
/// Phase performance metrics
/// </summary>
public record PhaseMetrics
{
    /// <summary>
    /// Objectives achieved ratio (0-1)
    /// </summary>
    public double ObjectiveAchievementRatio { get; init; }
    
    /// <summary>
    /// Quality of phase execution (0-1)
    /// </summary>
    public double ExecutionQuality { get; init; }
    
    /// <summary>
    /// Efficiency of phase (0-1)
    /// </summary>
    public double Efficiency { get; init; }
    
    /// <summary>
    /// Value added by phase (0-1)
    /// </summary>
    public double ValueAdded { get; init; }
}

// Supporting data structures for knowledge construction

/// <summary>
/// Knowledge construction process
/// </summary>
public record KnowledgeConstruction
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Topic for knowledge construction
    /// </summary>
    public string Topic { get; init; } = string.Empty;
    
    /// <summary>
    /// Participants in construction
    /// </summary>
    public List<ConversationParticipant> Participants { get; init; } = new();
    
    /// <summary>
    /// Construction phases
    /// </summary>
    public List<ConstructionPhase> ConstructionPhases { get; init; } = new();
    
    /// <summary>
    /// Start time
    /// </summary>
    public DateTime StartTime { get; init; }
    
    /// <summary>
    /// End time
    /// </summary>
    public DateTime? EndTime { get; init; }
    
    /// <summary>
    /// Status
    /// </summary>
    public ConversationStatus Status { get; init; }
    
    /// <summary>
    /// Final constructed knowledge
    /// </summary>
    public ConstructedKnowledge? FinalKnowledge { get; init; }
    
    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Phase of knowledge construction
/// </summary>
public record ConstructionPhase
{
    /// <summary>
    /// Phase type
    /// </summary>
    public ConstructionPhaseType PhaseType { get; init; }
    
    /// <summary>
    /// Start time
    /// </summary>
    public DateTime StartTime { get; init; }
    
    /// <summary>
    /// End time
    /// </summary>
    public DateTime? EndTime { get; init; }
    
    /// <summary>
    /// Contributions made in this phase
    /// </summary>
    public List<KnowledgeContribution> Contributions { get; init; } = new();
    
    /// <summary>
    /// Outcomes of the phase
    /// </summary>
    public List<string> Outcomes { get; init; } = new();
}

/// <summary>
/// Individual knowledge contribution
/// </summary>
public record KnowledgeContribution
{
    /// <summary>
    /// Contributing agent
    /// </summary>
    public AgentType Contributor { get; init; }
    
    /// <summary>
    /// Knowledge content
    /// </summary>
    public string Content { get; init; } = string.Empty;
    
    /// <summary>
    /// Confidence in contribution
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Supporting evidence
    /// </summary>
    public List<string> Evidence { get; init; } = new();
    
    /// <summary>
    /// When contributed
    /// </summary>
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Final constructed knowledge
/// </summary>
public record ConstructedKnowledge
{
    /// <summary>
    /// Synthesized knowledge content
    /// </summary>
    public string Content { get; init; } = string.Empty;
    
    /// <summary>
    /// Confidence in knowledge (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Contributors to knowledge
    /// </summary>
    public List<AgentType> Contributors { get; init; } = new();
    
    /// <summary>
    /// Supporting evidence
    /// </summary>
    public List<string> Evidence { get; init; } = new();
    
    /// <summary>
    /// Knowledge validation results
    /// </summary>
    public KnowledgeValidation Validation { get; init; } = new();
}

/// <summary>
/// Knowledge validation results
/// </summary>
public record KnowledgeValidation
{
    /// <summary>
    /// Validation score (0-1)
    /// </summary>
    public double ValidationScore { get; init; }
    
    /// <summary>
    /// Validation methods used
    /// </summary>
    public List<string> ValidationMethods { get; init; } = new();
    
    /// <summary>
    /// Issues identified
    /// </summary>
    public List<string> IdentifiedIssues { get; init; } = new();
    
    /// <summary>
    /// Validation confidence
    /// </summary>
    public double ValidationConfidence { get; init; }
}

/// <summary>
/// Result of knowledge construction
/// </summary>
public record KnowledgeConstructionResult
{
    /// <summary>
    /// Knowledge construction process
    /// </summary>
    public KnowledgeConstruction KnowledgeConstruction { get; init; } = new();
    
    /// <summary>
    /// Final constructed knowledge
    /// </summary>
    public ConstructedKnowledge? ConstructedKnowledge { get; init; }
    
    /// <summary>
    /// Construction metrics
    /// </summary>
    public ConstructionMetrics ConstructionMetrics { get; init; } = new();
    
    /// <summary>
    /// Quality assessment
    /// </summary>
    public KnowledgeQualityAssessment QualityAssessment { get; init; } = new();
}

/// <summary>
/// Metrics for knowledge construction
/// </summary>
public record ConstructionMetrics
{
    /// <summary>
    /// Construction efficiency
    /// </summary>
    public double ConstructionEfficiency { get; init; }
    
    /// <summary>
    /// Participant collaboration score
    /// </summary>
    public double CollaborationScore { get; init; }
    
    /// <summary>
    /// Knowledge novelty score
    /// </summary>
    public double NoveltyScore { get; init; }
    
    /// <summary>
    /// Overall construction quality
    /// </summary>
    public double OverallQuality { get; init; }
}

/// <summary>
/// Quality assessment of constructed knowledge
/// </summary>
public record KnowledgeQualityAssessment
{
    /// <summary>
    /// Overall quality score (0-1)
    /// </summary>
    public double OverallQuality { get; init; }
    
    /// <summary>
    /// Completeness score
    /// </summary>
    public double Completeness { get; init; }
    
    /// <summary>
    /// Accuracy score
    /// </summary>
    public double Accuracy { get; init; }
    
    /// <summary>
    /// Coherence score
    /// </summary>
    public double Coherence { get; init; }
    
    /// <summary>
    /// Usefulness score
    /// </summary>
    public double Usefulness { get; init; }
}

// Additional supporting structures

public record DiscussionPoint
{
    public string Topic { get; init; } = string.Empty;
    public double Complexity { get; init; }
    public Dictionary<string, object> Context { get; init; } = new();
}

public record ConvergenceAssessment
{
    public bool HasConverged { get; init; }
    public double ConvergenceScore { get; init; }
    public List<string> ConvergedAspects { get; init; } = new();
    public List<string> DivergedAspects { get; init; } = new();
}

public record ExchangeReference
{
    public Guid SourceExchangeId { get; init; }
    public Guid TargetExchangeId { get; init; }
    public ReferenceType ReferenceType { get; init; }
    public string Description { get; init; } = string.Empty;
}

// Enums

public enum ExchangeSentiment
{
    Positive,
    Neutral,
    Negative,
    Constructive,
    Critical,
    Supportive
}

public enum ConstructionPhaseType
{
    Contribution,
    Integration,
    Validation,
    Consensus,
    Refinement
}

public enum ReferenceType
{
    BuildsOn,
    Challenges,
    Supports,
    Clarifies,
    Contradicts,
    Synthesizes
}