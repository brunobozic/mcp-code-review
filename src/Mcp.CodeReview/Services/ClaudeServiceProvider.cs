using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Anthropic.SDK.Constants;
using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Utilities;

namespace Mcp.CodeReview.Services;

/// <summary>
/// Claude AI service provider implementation (wraps existing RefactoredClaudeService)
/// </summary>
public class ClaudeServiceProvider : IAIServiceProvider
{
    private readonly AnthropicClient _client;
    private readonly ILogger<ClaudeServiceProvider> _logger;
    private readonly string? _apiKey;
    private readonly CircuitBreaker _circuitBreaker;
    
    // Configuration constants
    private const string DefaultModel = AnthropicModels.Claude3Sonnet;
    private const int DefaultMaxTokens = 2000;
    private const double DefaultTemperature = 0.3;

    public string ProviderName => "Claude";

    public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);

    public ClaudeServiceProvider(ILogger<ClaudeServiceProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");
        _circuitBreaker = ErrorHandling.CreateCircuitBreaker("ClaudeService", logger);
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("CLAUDE_API_KEY not found, using dummy key for testing");
            _apiKey = "dummy-key-for-testing";
        }

        _client = new AnthropicClient(_apiKey);
    }

    public async Task<string> GenerateReviewAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await GenerateReviewWithParametersAsync(prompt, DefaultTemperature, DefaultMaxTokens, cancellationToken);
    }

    public async Task<string> GenerateReviewWithParametersAsync(
        string prompt, 
        double temperature = DefaultTemperature, 
        int maxTokens = DefaultMaxTokens, 
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Claude service is not available. Check API key configuration.");
        }

        var correlationId = Guid.NewGuid().ToString("N")[..8];
        
        return await ErrorHandling.RetryWithBackoffAsync(
            async () => await _circuitBreaker.ExecuteAsync(async () =>
                await GenerateReviewInternalAsync(prompt, temperature, maxTokens, correlationId, cancellationToken)),
            _logger,
            "GenerateReviewWithParameters",
            maxAttempts: 3,
            correlationId: correlationId);
    }

    public async Task<List<string>> GenerateMultipleAnalysesAsync(
        List<string> prompts, 
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Claude service is not available. Check API key configuration.");
        }

        var tasks = prompts.Select(async prompt =>
        {
            try
            {
                return await GenerateReviewAsync(prompt, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate analysis for prompt: {Prompt}", prompt.Substring(0, Math.Min(100, prompt.Length)));
                return $"Analysis failed: {ex.Message}";
            }
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    public async Task<AIProviderHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var status = new AIProviderHealthStatus();

        try
        {
            if (!IsAvailable)
            {
                status.IsHealthy = false;
                status.ErrorMessage = "API key not configured";
                return status;
            }

            // Simple health check with minimal token usage
            var testPrompt = "Health check. Respond with 'OK'.";
            var response = await GenerateReviewWithParametersAsync(testPrompt, 0.0, 10, cancellationToken);
            
            status.IsHealthy = !string.IsNullOrEmpty(response);
            status.ResponseTime = DateTime.UtcNow - startTime;
            status.AdditionalMetrics["provider"] = ProviderName;
            status.AdditionalMetrics["model"] = DefaultModel;
        }
        catch (Exception ex)
        {
            status.IsHealthy = false;
            status.ErrorMessage = ex.Message;
            status.ResponseTime = DateTime.UtcNow - startTime;
        }

        return status;
    }

    private async Task<string> GenerateReviewInternalAsync(
        string prompt, 
        double temperature, 
        int maxTokens,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ErrorHandling.ValidateRequired(prompt, nameof(prompt));
        ValidateParameters(temperature, maxTokens);

        _logger.LogDebug("Generating Claude analysis for correlation ID {CorrelationId}", correlationId);

        var messages = new List<Message>
        {
            new()
            {
                Role = RoleType.User,
                Content = new List<ContentBase>
                {
                    new TextContent
                    {
                        Text = prompt
                    }
                }
            }
        };

        var parameters = new MessageParameters
        {
            Messages = messages,
            Model = DefaultModel,
            MaxTokens = maxTokens,
            Temperature = (decimal)temperature
        };

        var response = await _client.Messages.GetClaudeMessageAsync(parameters, cancellationToken);

        if (response?.Content?.FirstOrDefault() is TextContent textContent)
        {
            _logger.LogDebug("Successfully generated Claude response for correlation ID {CorrelationId}", correlationId);
            return textContent.Text ?? "No response content";
        }

        throw new InvalidOperationException("No valid response received from Claude API");
    }

    private static void ValidateParameters(double temperature, int maxTokens)
    {
        if (temperature < 0 || temperature > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(temperature), "Temperature must be between 0 and 1");
        }

        if (maxTokens < 1 || maxTokens > 4096)
        {
            throw new ArgumentOutOfRangeException(nameof(maxTokens), "MaxTokens must be between 1 and 4096");
        }
    }
}