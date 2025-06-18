using Mcp.CodeReview.Services;
using Mcp.CodeReview.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mcp.CodeReview.HealthChecks;

public class ExternalApiHealthCheck : IHealthCheck
{
    private readonly GitHubService _githubService;
    private readonly GitLabService _gitlabService;
    private readonly IClaudeService _claudeService;
    private readonly ILogger<ExternalApiHealthCheck> _logger;

    public ExternalApiHealthCheck(
        GitHubService githubService,
        GitLabService gitlabService,
        IClaudeService claudeService,
        ILogger<ExternalApiHealthCheck> logger)
    {
        _githubService = githubService;
        _gitlabService = gitlabService;
        _claudeService = claudeService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, bool>();
        var errors = new List<string>();

        try
        {
            results["github"] = await _githubService.TestConnection();
        }
        catch (Exception ex)
        {
            results["github"] = false;
            errors.Add($"GitHub: {ex.Message}");
        }

        try
        {
            results["gitlab"] = await _gitlabService.TestConnection();
        }
        catch (Exception ex)
        {
            results["gitlab"] = false;
            errors.Add($"GitLab: {ex.Message}");
        }

        try
        {
            results["claude"] = await _claudeService.IsHealthyAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            results["claude"] = false;
            errors.Add($"Claude: {ex.Message}");
        }

        var healthyServices = results.Values.Count(x => x);
        var totalServices = results.Count;

        var resultData = results.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);
        
        if (healthyServices == totalServices)
        {
            return HealthCheckResult.Healthy($"All {totalServices} external services are healthy", resultData);
        }
        else if (healthyServices > 0)
        {
            return HealthCheckResult.Degraded(
                $"{healthyServices}/{totalServices} external services are healthy. Errors: {string.Join(", ", errors)}", 
                data: resultData);
        }
        else
        {
            return HealthCheckResult.Unhealthy(
                $"No external services are healthy. Errors: {string.Join(", ", errors)}", 
                data: resultData);
        }
    }
}