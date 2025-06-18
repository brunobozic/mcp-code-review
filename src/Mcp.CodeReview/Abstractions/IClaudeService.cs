using Mcp.CodeReview.AI;

namespace Mcp.CodeReview.Abstractions;

/// <summary>
/// Interface for Claude AI service operations
/// </summary>
public interface IClaudeService
{
    /// <summary>
    /// Generate a code review using Claude AI
    /// </summary>
    /// <param name="prompt">The prompt to send to Claude</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated review content</returns>
    Task<string> GenerateReviewAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate a review with specific parameters
    /// </summary>
    /// <param name="prompt">The prompt to send to Claude</param>
    /// <param name="temperature">Temperature parameter for generation (0.0-1.0)</param>
    /// <param name="maxTokens">Maximum tokens to generate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated review content</returns>
    Task<string> GenerateReviewWithParametersAsync(
        string prompt, 
        double temperature = 0.3, 
        int maxTokens = 2000, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate an enhanced analysis using structured prompts
    /// </summary>
    /// <param name="request">The analysis request with structured parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Enhanced analysis result</returns>
    Task<EnhancedAnalysisResult> GenerateEnhancedAnalysisAsync(
        AnalysisRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if the service is available and healthy
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if service is healthy</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for enhanced analysis
/// </summary>
public class AnalysisRequest
{
    public string Content { get; set; } = string.Empty;
    public string AnalysisType { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string Context { get; set; } = string.Empty;
}

/// <summary>
/// Result model for enhanced analysis
/// </summary>
public class EnhancedAnalysisResult
{
    public string Analysis { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public List<string> KeyFindings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}