using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.Models;

/// <summary>
/// Result of executing an individual agent in the multi-agent system
/// </summary>
public class AgentExecutionResult
{
    public AgentType AgentType { get; set; }
    public bool Success { get; set; }
    public AgentResult Result { get; set; } = new();
    public List<string> KeyFindings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public double ConfidenceScore { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}