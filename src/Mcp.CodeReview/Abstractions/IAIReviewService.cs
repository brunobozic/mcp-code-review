using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.Abstractions;

/// <summary>
/// Interface for AI-powered code review operations
/// </summary>
public interface IAIReviewService
{
    /// <summary>
    /// Conduct a multi-agent code review
    /// </summary>
    /// <param name="request">Code review request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive review result</returns>
    Task<MultiAgentReviewResult> ConductMultiAgentReviewAsync(
        CodeReviewRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Conduct a focused analysis using specialized agents
    /// </summary>
    /// <param name="request">Focused analysis request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Focused analysis result</returns>
    Task<FocusedAnalysisResult> ConductFocusedAnalysisAsync(
        FocusedAnalysisRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get available AI agent types
    /// </summary>
    /// <returns>List of available agent types</returns>
    IEnumerable<AgentType> GetAvailableAgentTypes();

    /// <summary>
    /// Validate analysis request
    /// </summary>
    /// <param name="request">Request to validate</param>
    /// <returns>Validation result</returns>
    ValidationResult ValidateRequest(CodeReviewRequest request);
}

/// <summary>
/// Interface for agent orchestration
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Execute agents in parallel for improved performance
    /// </summary>
    /// <param name="agentTasks">Agent execution tasks</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Aggregated results</returns>
    Task<AgentExecutionResult[]> ExecuteAgentsParallelAsync(
        IEnumerable<AgentExecutionTask> agentTasks, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create specialized agent for specific analysis type
    /// </summary>
    /// <param name="agentType">Type of agent to create</param>
    /// <param name="configuration">Agent configuration</param>
    /// <returns>Configured agent</returns>
    ISpecializedAgent CreateAgent(AgentType agentType, AgentConfiguration configuration);
}

/// <summary>
/// Interface for specialized AI agents
/// </summary>
public interface ISpecializedAgent
{
    AgentType Type { get; }
    string Name { get; }
    string Description { get; }
    
    Task<AgentResult> AnalyzeAsync(string content, Dictionary<string, object> context, CancellationToken cancellationToken = default);
    bool CanHandle(string contentType);
}

/// <summary>
/// Agent execution task for orchestration
/// </summary>
public class AgentExecutionTask
{
    public AgentType AgentType { get; set; }
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; set; } = new();
    public int Priority { get; set; } = 1;
}

/// <summary>
/// Result from agent execution
/// </summary>
public class AgentExecutionResult
{
    public AgentType AgentType { get; set; }
    public AgentResult Result { get; set; } = new();
    public TimeSpan ExecutionTime { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Configuration for agent creation
/// </summary>
public class AgentConfiguration
{
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? CustomPrompt { get; set; }
    public double Temperature { get; set; } = 0.3;
    public int MaxTokens { get; set; } = 2000;
}