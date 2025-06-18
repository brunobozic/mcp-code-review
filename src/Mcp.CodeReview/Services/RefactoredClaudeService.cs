using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Anthropic.SDK.Constants;
using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Utilities;
using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.Services;

/// <summary>
/// Enhanced Claude AI service implementation with improved error handling, performance, and maintainability
/// </summary>
public class RefactoredClaudeService : IClaudeService
{
    private readonly AnthropicClient _client;
    private readonly ILogger<RefactoredClaudeService> _logger;
    private readonly CircuitBreaker _circuitBreaker;
    
    // Configuration constants
    private const string DefaultModel = AnthropicModels.Claude3Sonnet;
    private const int DefaultMaxTokens = 2000;
    private const double DefaultTemperature = 0.3;
    private const int MaxRetryAttempts = 3;

    public RefactoredClaudeService(ILogger<RefactoredClaudeService> logger)
    {
        ErrorHandling.ValidateRequired(logger, nameof(logger));
        
        _logger = logger;
        _client = CreateAnthropicClient();
        _circuitBreaker = ErrorHandling.CreateCircuitBreaker("ClaudeService", logger);
    }

    /// <inheritdoc />
    public async Task<string> GenerateReviewAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await GenerateReviewWithParametersAsync(prompt, DefaultTemperature, DefaultMaxTokens, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> GenerateReviewWithParametersAsync(
        string prompt, 
        double temperature = DefaultTemperature, 
        int maxTokens = DefaultMaxTokens, 
        CancellationToken cancellationToken = default)
    {
        ErrorHandling.ValidateRequired(prompt, nameof(prompt));
        ValidateParameters(temperature, maxTokens);

        return await ErrorHandling.RetryWithBackoffAsync(
            operation: () => _circuitBreaker.ExecuteAsync(() => GenerateReviewInternalAsync(prompt, temperature, maxTokens, cancellationToken)),
            logger: _logger,
            operationName: "GenerateReviewWithParameters",
            maxAttempts: MaxRetryAttempts
        );
    }

    /// <inheritdoc />
    public async Task<EnhancedAnalysisResult> GenerateEnhancedAnalysisAsync(
        AnalysisRequest request, 
        CancellationToken cancellationToken = default)
    {
        ErrorHandling.ValidateRequired(request, nameof(request));
        ErrorHandling.ValidateRequired(request.Content, nameof(request.Content));

        return await ErrorHandling.ExecuteWithErrorHandlingAsync(
            operation: async () =>
            {
                // Build enhanced prompt based on request
                var prompt = BuildEnhancedPrompt(request);
                
                // Get analysis parameters
                var temperature = GetTemperatureFromParameters(request.Parameters);
                var maxTokens = GetMaxTokensFromParameters(request.Parameters);
                
                // Generate analysis
                var analysis = await GenerateReviewInternalAsync(prompt, temperature, maxTokens, cancellationToken);
                
                // Parse and structure the result
                return ParseEnhancedAnalysisResult(analysis, request);
            },
            logger: _logger,
            operationName: "GenerateEnhancedAnalysis",
            defaultValue: new EnhancedAnalysisResult { Analysis = "Analysis failed", ConfidenceScore = 0.0 }
        );
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        return await ErrorHandling.ExecuteWithErrorHandlingAsync(
            operation: async () =>
            {
                // Simple health check with minimal prompt
                var healthCheckPrompt = "Respond with 'OK' if you can process this request.";
                var response = await GenerateReviewInternalAsync(healthCheckPrompt, 0.1, 10, cancellationToken);
                
                return !string.IsNullOrWhiteSpace(response);
            },
            logger: _logger,
            operationName: "HealthCheck"
        );
    }

    // Private implementation methods
    private async Task<string> GenerateReviewInternalAsync(
        string prompt, 
        double temperature, 
        int maxTokens, 
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12];
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["PromptLength"] = prompt.Length,
            ["Temperature"] = temperature,
            ["MaxTokens"] = maxTokens
        });

        _logger.LogDebug("Generating Claude analysis with correlation ID {CorrelationId}", correlationId);

        var messages = new List<Message>
        {
            new Message
            {
                Role = RoleType.User,
                Content = prompt
            }
        };

        var parameters = new MessageParameters
        {
            Model = DefaultModel,
            Messages = messages,
            MaxTokens = maxTokens,
            Temperature = (decimal)temperature
        };

        try
        {
            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            
            if (response?.Content?.FirstOrDefault()?.Text == null)
            {
                throw new InvalidOperationException("Claude API returned null or empty response");
            }

            var content = response.Content.First().Text;
            
            _logger.LogDebug("Claude analysis completed successfully with {ResponseLength} characters", 
                content.Length);
            
            return content;
        }
        catch (Exception ex) when (!(ex is OperationCanceledException))
        {
            _logger.LogError(ex, "Failed to generate Claude analysis for correlation ID {CorrelationId}", correlationId);
            throw;
        }
    }

    private AnthropicClient CreateAnthropicClient()
    {
        var apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");
        
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("CLAUDE_API_KEY not found, using dummy key for testing");
            apiKey = "dummy-key-for-testing";
        }
        
        return new AnthropicClient(new APIAuthentication(apiKey));
    }

    private static void ValidateParameters(double temperature, int maxTokens)
    {
        if (temperature < 0.0 || temperature > 1.0)
            throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature must be between 0.0 and 1.0");

        if (maxTokens < 1 || maxTokens > 8192)
            throw new ArgumentOutOfRangeException(nameof(maxTokens), "MaxTokens must be between 1 and 8192");
    }

    private string BuildEnhancedPrompt(AnalysisRequest request)
    {
        var promptBuilder = new System.Text.StringBuilder();
        
        promptBuilder.AppendLine($"You are conducting a {request.AnalysisType} analysis.");
        
        if (!string.IsNullOrEmpty(request.Context))
        {
            promptBuilder.AppendLine($"Context: {request.Context}");
        }
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Content to analyze:");
        promptBuilder.AppendLine(request.Content);
        
        return promptBuilder.ToString();
    }

    private static double GetTemperatureFromParameters(Dictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("temperature", out var tempValue) && tempValue is double temp)
            return temp;
        
        return DefaultTemperature;
    }

    private static int GetMaxTokensFromParameters(Dictionary<string, object> parameters)
    {
        if (parameters.TryGetValue("maxTokens", out var tokensValue) && tokensValue is int tokens)
            return tokens;
        
        return DefaultMaxTokens;
    }

    private EnhancedAnalysisResult ParseEnhancedAnalysisResult(string analysis, AnalysisRequest request)
    {
        // Simple parsing - in a real implementation, this could be more sophisticated
        var lines = analysis.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        return new EnhancedAnalysisResult
        {
            Analysis = analysis,
            ConfidenceScore = 0.85, // Would be calculated based on response quality
            KeyFindings = ExtractKeyFindings(lines),
            Recommendations = ExtractRecommendations(lines),
            Metadata = new Dictionary<string, object>
            {
                ["AnalysisType"] = request.AnalysisType,
                ["Timestamp"] = DateTime.UtcNow,
                ["ResponseLength"] = analysis.Length
            }
        };
    }

    private static List<string> ExtractKeyFindings(string[] lines)
    {
        // Simple extraction - could be enhanced with NLP
        return lines
            .Where(line => line.Contains("issue", StringComparison.OrdinalIgnoreCase) ||
                          line.Contains("problem", StringComparison.OrdinalIgnoreCase) ||
                          line.Contains("concern", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();
    }

    private static List<string> ExtractRecommendations(string[] lines)
    {
        // Simple extraction - could be enhanced with NLP
        return lines
            .Where(line => line.Contains("recommend", StringComparison.OrdinalIgnoreCase) ||
                          line.Contains("suggest", StringComparison.OrdinalIgnoreCase) ||
                          line.Contains("should", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .ToList();
    }
}