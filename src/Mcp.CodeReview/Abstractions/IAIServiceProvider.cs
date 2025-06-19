using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.Abstractions;

/// <summary>
/// Universal AI service provider interface that can work with different AI providers (Claude, OpenAI, etc.)
/// </summary>
public interface IAIServiceProvider
{
    /// <summary>
    /// The name of the AI provider (e.g., "Claude", "OpenAI", "Azure")
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// Whether this provider is currently available and configured
    /// </summary>
    bool IsAvailable { get; }
    
    /// <summary>
    /// Generate a code review analysis using the AI provider
    /// </summary>
    Task<string> GenerateReviewAsync(string prompt, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate a code review with specific parameters
    /// </summary>
    Task<string> GenerateReviewWithParametersAsync(
        string prompt, 
        double temperature = 0.3, 
        int maxTokens = 2000, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate multiple analyses in parallel (for multi-agent scenarios)
    /// </summary>
    Task<List<string>> GenerateMultipleAnalysesAsync(
        List<string> prompts, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check the health/availability of the AI provider
    /// </summary>
    Task<AIProviderHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Health status of an AI provider
/// </summary>
public class AIProviderHealthStatus
{
    public bool IsHealthy { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}