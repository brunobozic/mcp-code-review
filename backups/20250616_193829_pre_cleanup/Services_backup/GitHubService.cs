using Octokit;
using System.Text.RegularExpressions;

namespace Mcp.CodeReview.Services;

public class GitHubService
{
    private readonly GitHubClient _client;
    private readonly ILogger<GitHubService> _logger;

    public GitHubService(ILogger<GitHubService> logger)
    {
        _logger = logger;
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        
        _client = new GitHubClient(new ProductHeaderValue("mcp-code-review"))
        {
            Credentials = !string.IsNullOrEmpty(token) ? new Credentials(token) : Credentials.Anonymous
        };
    }

    public async Task<object> CommentOnPR(string repoUrl, int prNumber, string body, string? path = null, int? line = null)
    {
        var (owner, repo) = ParseRepoUrl(repoUrl);
        
        try
        {
            if (path != null && line.HasValue)
            {
                var pullRequest = await _client.PullRequest.Get(owner, repo, prNumber);
                var review = new PullRequestReviewCreate
                {
                    Body = body,
                    Event = PullRequestReviewEvent.Comment,
                    Comments = new List<DraftPullRequestReviewComment>
                    {
                        new DraftPullRequestReviewComment(body, path, line.Value)
                    }
                };
                
                var result = await _client.PullRequest.Review.Create(owner, repo, prNumber, review);
                return new { success = true, reviewId = result.Id, url = result.HtmlUrl };
            }
            else
            {
                var comment = await _client.Issue.Comment.Create(owner, repo, prNumber, body);
                return new { success = true, commentId = comment.Id, url = comment.HtmlUrl };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to comment on GitHub PR {Owner}/{Repo}#{PR}", owner, repo, prNumber);
            throw new InvalidOperationException($"GitHub API error: {ex.Message}");
        }
    }

    public async Task<bool> TestConnection()
    {
        try
        {
            var user = await _client.User.Current();
            _logger.LogInformation("GitHub connection test successful for user: {User}", user.Login ?? "anonymous");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GitHub connection test failed: {Message}", ex.Message);
            return false;
        }
    }

    private static (string owner, string repo) ParseRepoUrl(string repoUrl)
    {
        var match = Regex.Match(repoUrl, @"github\.com[:/]([^/]+)/([^/]+?)(?:\.git)?/?$");
        if (!match.Success)
            throw new ArgumentException($"Invalid GitHub repository URL: {repoUrl}");
        
        return (match.Groups[1].Value, match.Groups[2].Value);
    }
}