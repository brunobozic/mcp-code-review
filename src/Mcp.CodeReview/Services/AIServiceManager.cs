using Mcp.CodeReview.Abstractions;
using Microsoft.Extensions.Logging;

namespace Mcp.CodeReview.Services;

/// <summary>
/// Manages multiple AI service providers and automatically selects the best available one
/// </summary>
public class AIServiceManager : IAIServiceProvider
{
    private readonly List<IAIServiceProvider> _providers;
    private readonly ILogger<AIServiceManager> _logger;
    private IAIServiceProvider? _primaryProvider;
    private readonly string _preferredProvider;

    public string ProviderName => _primaryProvider?.ProviderName ?? "AIServiceManager";

    public bool IsAvailable => _primaryProvider?.IsAvailable ?? false;

    public AIServiceManager(
        IEnumerable<IAIServiceProvider> providers, 
        ILogger<AIServiceManager> logger,
        IConfiguration configuration)
    {
        _providers = providers?.ToList() ?? throw new ArgumentNullException(nameof(providers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Get preferred provider from configuration
        _preferredProvider = configuration["AI:PreferredProvider"] ?? "Claude";
        
        InitializePrimaryProvider();
    }

    public async Task<string> GenerateReviewAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var provider = await GetAvailableProviderAsync(cancellationToken);
        return await provider.GenerateReviewAsync(prompt, cancellationToken);
    }

    public async Task<string> GenerateReviewWithParametersAsync(
        string prompt, 
        double temperature = 0.3, 
        int maxTokens = 2000, 
        CancellationToken cancellationToken = default)
    {
        var provider = await GetAvailableProviderAsync(cancellationToken);
        return await provider.GenerateReviewWithParametersAsync(prompt, temperature, maxTokens, cancellationToken);
    }

    public async Task<List<string>> GenerateMultipleAnalysesAsync(
        List<string> prompts, 
        CancellationToken cancellationToken = default)
    {
        var provider = await GetAvailableProviderAsync(cancellationToken);
        return await provider.GenerateMultipleAnalysesAsync(prompts, cancellationToken);
    }

    public async Task<AIProviderHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var allHealthStatuses = new List<(string Provider, AIProviderHealthStatus Status)>();

        foreach (var provider in _providers)
        {
            try
            {
                var status = await provider.CheckHealthAsync(cancellationToken);
                allHealthStatuses.Add((provider.ProviderName, status));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check health for provider {Provider}", provider.ProviderName);
                allHealthStatuses.Add((provider.ProviderName, new AIProviderHealthStatus
                {
                    IsHealthy = false,
                    ErrorMessage = ex.Message
                }));
            }
        }

        // Return status of primary provider, or aggregate if no primary
        var primaryStatus = allHealthStatuses.FirstOrDefault(h => h.Provider == (_primaryProvider?.ProviderName ?? ""));
        if (primaryStatus.Status != null)
        {
            primaryStatus.Status.AdditionalMetrics["all_providers"] = allHealthStatuses.ToDictionary(
                h => h.Provider,
                h => new { h.Status.IsHealthy, h.Status.ErrorMessage });
            return primaryStatus.Status;
        }

        // Return aggregate status
        var healthyCount = allHealthStatuses.Count(h => h.Status.IsHealthy);
        return new AIProviderHealthStatus
        {
            IsHealthy = healthyCount > 0,
            ErrorMessage = healthyCount == 0 ? "No providers are healthy" : null,
            AdditionalMetrics = new Dictionary<string, object>
            {
                ["healthy_providers"] = healthyCount,
                ["total_providers"] = allHealthStatuses.Count,
                ["provider_details"] = allHealthStatuses.ToDictionary(
                    h => h.Provider,
                    h => new { h.Status.IsHealthy, h.Status.ErrorMessage })
            }
        };
    }

    /// <summary>
    /// Get information about all available providers
    /// </summary>
    public async Task<Dictionary<string, object>> GetProvidersInfoAsync(CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, object>
        {
            ["preferred_provider"] = _preferredProvider,
            ["primary_provider"] = _primaryProvider?.ProviderName ?? "None",
            ["available_providers"] = new List<object>()
        };

        var providersList = (List<object>)result["available_providers"];

        foreach (var provider in _providers)
        {
            var health = await provider.CheckHealthAsync(cancellationToken);
            providersList.Add(new
            {
                name = provider.ProviderName,
                available = provider.IsAvailable,
                healthy = health.IsHealthy,
                response_time_ms = health.ResponseTime.TotalMilliseconds,
                error = health.ErrorMessage
            });
        }

        return result;
    }

    private void InitializePrimaryProvider()
    {
        // Try to use preferred provider first
        _primaryProvider = _providers.FirstOrDefault(p => 
            p.ProviderName.Equals(_preferredProvider, StringComparison.OrdinalIgnoreCase) && p.IsAvailable);

        // Fallback to any available provider
        _primaryProvider ??= _providers.FirstOrDefault(p => p.IsAvailable);

        if (_primaryProvider != null)
        {
            _logger.LogInformation("Primary AI provider set to: {Provider}", _primaryProvider.ProviderName);
        }
        else
        {
            _logger.LogWarning("No AI providers are available. Check API key configurations.");
        }
    }

    private async Task<IAIServiceProvider> GetAvailableProviderAsync(CancellationToken cancellationToken = default)
    {
        // If primary provider is available, use it
        if (_primaryProvider?.IsAvailable == true)
        {
            return _primaryProvider;
        }

        // Try to find any available provider
        foreach (var provider in _providers)
        {
            if (provider.IsAvailable)
            {
                _logger.LogInformation("Switching to available provider: {Provider}", provider.ProviderName);
                _primaryProvider = provider;
                return provider;
            }
        }

        // Last resort: health check all providers
        foreach (var provider in _providers)
        {
            try
            {
                var health = await provider.CheckHealthAsync(cancellationToken);
                if (health.IsHealthy)
                {
                    _logger.LogInformation("Found healthy provider after health check: {Provider}", provider.ProviderName);
                    _primaryProvider = provider;
                    return provider;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Provider {Provider} failed health check", provider.ProviderName);
            }
        }

        throw new InvalidOperationException("No AI providers are available. Please check your API key configurations for Claude (CLAUDE_API_KEY) or OpenAI (OPENAI_API_KEY).");
    }
}