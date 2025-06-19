using System.Collections.Generic;

namespace Mcp.CodeReview.Models;

/// <summary>
/// Models for enabling real agent-to-agent communication and collaboration
/// </summary>
public class AgentConversation
{
    public string ConversationId { get; set; } = Guid.NewGuid().ToString();
    public List<AgentMessage> Messages { get; set; } = new();
    public List<AgentType> Participants { get; set; } = new();
    public ConversationStatus Status { get; set; } = ConversationStatus.Active;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public AgentConsensus? Consensus { get; set; }
}

public class AgentMessage
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString();
    public AgentType FromAgent { get; set; }
    public List<AgentType> ToAgents { get; set; } = new(); // Empty = broadcast to all
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ReplyToMessageId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class AgentChallenge
{
    public string ChallengeId { get; set; } = Guid.NewGuid().ToString();
    public AgentType ChallengingAgent { get; set; }
    public AgentType ChallengedAgent { get; set; }
    public string OriginalFinding { get; set; } = string.Empty;
    public string Challenge { get; set; } = string.Empty;
    public string? Response { get; set; }
    public ChallengeStatus Status { get; set; } = ChallengeStatus.Pending;
    public double ConfidenceScore { get; set; }
}

public class AgentConsensus
{
    public string ConsensusId { get; set; } = Guid.NewGuid().ToString();
    public List<ConsensusItem> AgreedFindings { get; set; } = new();
    public List<ConsensusItem> DisputedFindings { get; set; } = new();
    public double OverallConfidence { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> MinorityOpinions { get; set; } = new();
}

public class ConsensusItem
{
    public string Finding { get; set; } = string.Empty;
    public List<AgentType> AgreeingAgents { get; set; } = new();
    public List<AgentType> DisagreeingAgents { get; set; } = new();
    public double ConfidenceScore { get; set; }
    public string Evidence { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class CollaborativeReviewResult
{
    public AgentConversation Conversation { get; set; } = new();
    public List<AgentChallenge> Challenges { get; set; } = new();
    public AgentConsensus Consensus { get; set; } = new();
    public List<string> ValidatedFindings { get; set; } = new();
    public List<string> CollaborativeRecommendations { get; set; } = new();
    public Dictionary<AgentType, double> AgentConfidenceScores { get; set; } = new();
    public string CollaborationSummary { get; set; } = string.Empty;
}

public enum ConversationStatus
{
    Active,
    ConsensusReached,
    DisputeUnresolved,
    Completed,
    Timeout
}

public enum MessageType
{
    InitialAnalysis,
    Question,
    Challenge,
    Response,
    Agreement,
    Disagreement,
    Evidence,
    Synthesis,
    FinalStatement
}

public enum ChallengeStatus
{
    Pending,
    Responded,
    Validated,
    Refuted,
    Withdrawn
}