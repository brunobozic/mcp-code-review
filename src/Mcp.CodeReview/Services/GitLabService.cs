using NGitLab;
using NGitLab.Models;
using System.Text.RegularExpressions;

namespace Mcp.CodeReview.Services;

public class GitLabService
{
    private readonly IGitLabClient _client;
    private readonly ILogger<GitLabService> _logger;

    public GitLabService(ILogger<GitLabService> logger)
    {
        _logger = logger;
        var token = Environment.GetEnvironmentVariable("GITLAB_TOKEN");
        var host = Environment.GetEnvironmentVariable("GITLAB_HOST") ?? "https://gitlab.com";
        
        _client = new GitLabClient(host, token ?? "");
    }

    public async Task<object> CommentOnPR(string repoUrl, int prNumber, string body, string? path = null, int? line = null)
    {
        var projectId = ParseProjectId(repoUrl);
        
        try
        {
            // Temporarily disabled due to NGitLab API changes
            _logger.LogWarning("GitLab commenting temporarily disabled due to API compatibility issues");
            return new { success = false, error = "GitLab API compatibility issue" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to comment on GitLab MR {ProjectId}!{MR}", projectId, prNumber);
            throw new InvalidOperationException($"GitLab API error: {ex.Message}");
        }
    }

    public async Task<bool> TestConnection()
    {
        try
        {
            var user = _client.Users.Current;
            _logger.LogInformation("GitLab connection test successful for user: {User}", user.Username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GitLab connection test failed: {Message}", ex.Message);
            return false;
        }
    }

    private static string ParseProjectId(string repoUrl)
    {
        var match = Regex.Match(repoUrl, @"gitlab\.com[:/]([^/]+/[^/]+?)(?:\.git)?/?$");
        if (!match.Success)
            throw new ArgumentException($"Invalid GitLab repository URL: {repoUrl}");
        
        return Uri.EscapeDataString(match.Groups[1].Value);
    }
}