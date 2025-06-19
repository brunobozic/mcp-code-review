using System.Text.Json;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Utilities;
using Microsoft.Extensions.Logging;

namespace Mcp.CodeReview.Services;

/// <summary>
/// OpenAI ChatGPT service provider implementation
/// </summary>
public class OpenAIServiceProvider : IAIServiceProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAIServiceProvider> _logger;
    private readonly string? _apiKey;
    private readonly CircuitBreaker _circuitBreaker;
    
    // Configuration constants
    private const string DefaultModel = "gpt-4o-mini"; // Cost-effective model
    private const string ApiBaseUrl = "https://api.openai.com/v1/chat/completions";
    private const int DefaultMaxTokens = 2000;
    private const double DefaultTemperature = 0.3;

    public string ProviderName => "OpenAI";

    public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);

    public OpenAIServiceProvider(HttpClient httpClient, ILogger<OpenAIServiceProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _circuitBreaker = ErrorHandling.CreateCircuitBreaker("OpenAIService", logger);
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("OPENAI_API_KEY not found, OpenAI provider will not be available");
        }
        
        ConfigureHttpClient();
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
            throw new InvalidOperationException("OpenAI service is not available. Check API key configuration.");
        }

        var correlationId = Guid.NewGuid().ToString("N")[..8];
        
        return await ErrorHandling.RetryWithBackoffAsync(
            async () => await _circuitBreaker.ExecuteAsync(async () =>
                await GenerateCompletionInternalAsync(prompt, temperature, maxTokens, correlationId, cancellationToken)),
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
            throw new InvalidOperationException("OpenAI service is not available. Check API key configuration.");
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

    private async Task<string> GenerateCompletionInternalAsync(
        string prompt, 
        double temperature, 
        int maxTokens,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var requestPayload = new
        {
            model = DefaultModel,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You are an expert code reviewer with deep knowledge of software engineering best practices, security, performance, and maintainability. Provide detailed, actionable feedback on code quality."
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            max_tokens = maxTokens,
            temperature = temperature,
            top_p = 1.0,
            frequency_penalty = 0.0,
            presence_penalty = 0.0
        };

        var jsonPayload = JsonSerializer.Serialize(requestPayload);
        var content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

        _logger.LogDebug("Sending OpenAI request for correlation ID {CorrelationId}", correlationId);

        var response = await _httpClient.PostAsync(ApiBaseUrl, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"OpenAI API request failed with status {response.StatusCode}: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

        if (responseData.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var firstChoice = choices[0];
            if (firstChoice.TryGetProperty("message", out var message) && 
                message.TryGetProperty("content", out var messageContent))
            {
                var result = messageContent.GetString() ?? "No content received";
                _logger.LogDebug("Successfully generated OpenAI response for correlation ID {CorrelationId}", correlationId);
                return result;
            }
        }

        throw new InvalidOperationException("Invalid response format from OpenAI API");
    }

    private void ConfigureHttpClient()
    {
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        }
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "MCP-CodeReview/1.0");
        _httpClient.Timeout = TimeSpan.FromMinutes(2);
    }
}