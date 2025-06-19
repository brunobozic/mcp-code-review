using Mcp.CodeReview.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace Mcp.CodeReview.Tools;

[McpServerToolType]
public static class GitLabTools
{
    /// <summary>
    /// Add a review comment to a pull request
    /// </summary>
    [McpServerTool, Description("Add a review comment to a pull request")]
    public static async Task<object> CommentOnPR(
        GitHubService gitHubService,
        GitLabService gitLabService,
        ILogger logger,
        [Description("The repository URL")] string repoUrl,
        [Description("The pull request number")] int prNumber,
        [Description("The comment body")] string body,
        [Description("Optional file path for inline comments")] string? path = null,
        [Description("Optional line number for inline comments")] int? line = null)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["RepoUrl"] = repoUrl,
            ["PRNumber"] = prNumber,
            ["CommentPath"] = path ?? "general",
            ["CommentLine"] = line?.ToString() ?? "none",
            ["CommentLength"] = body.Length
        }))
        {
            logger.LogInformation("Adding comment to PR {PRNumber} on {RepoUrl}", prNumber, repoUrl);

            try
            {
                if (IsGitHubRepo(repoUrl))
                {
                    var result = await gitHubService.CommentOnPR(repoUrl, prNumber, body, path, line);
                    logger.LogInformation("Successfully added GitHub comment to PR {PRNumber}", prNumber);
                    return result;
                }
                else if (IsGitLabRepo(repoUrl))
                {
                    var result = await gitLabService.CommentOnPR(repoUrl, prNumber, body, path, line);
                    logger.LogInformation("Successfully added GitLab comment to PR {PRNumber}", prNumber);
                    return result;
                }
                else
                {
                    logger.LogError("Unsupported repository platform for URL: {RepoUrl}", repoUrl);
                    throw new InvalidOperationException("Unsupported repository platform");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to add comment to PR {PRNumber} on {RepoUrl}", prNumber, repoUrl);
                throw;
            }
        }
    }

    /// <summary>
    /// Get the diff for a pull request
    /// </summary>
    [McpServerTool, Description("Get the diff for a pull request")]
    public static async Task<object> GetPRDiff(
        [Description("The repository URL")] string repoUrl,
        [Description("The base branch name")] string baseBranch,
        [Description("The head branch name")] string headBranch)
    {
        try
        {
            // Extract repo information
            var repoInfo = ParseRepositoryUrl(repoUrl);
            var workingDir = Path.Combine("/tmp", $"repo_{DateTime.UtcNow.Ticks}");

            try
            {
                // Clone repository
                Directory.CreateDirectory(workingDir);
                await RunGitCommand($"clone {repoUrl} .", workingDir);

                // Fetch branches
                await RunGitCommand($"fetch origin {baseBranch}:{baseBranch}", workingDir);
                await RunGitCommand($"fetch origin {headBranch}:{headBranch}", workingDir);

                // Get diff
                var diffResult = await RunGitCommand($"diff {baseBranch}...{headBranch}", workingDir);

                // Get file list
                var filesResult = await RunGitCommand($"diff --name-only {baseBranch}...{headBranch}", workingDir);
                var files = filesResult.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                // Get commit info
                var logResult = await RunGitCommand($"log {baseBranch}..{headBranch} --oneline", workingDir);
                var commits = logResult.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                return new
                {
                    success = true,
                    repository = new
                    {
                        url = repoUrl,
                        name = repoInfo.Name,
                        owner = repoInfo.Owner
                    },
                    diff = new
                    {
                        baseBranch = baseBranch,
                        headBranch = headBranch,
                        content = diffResult,
                        stats = new
                        {
                            filesChanged = files.Length,
                            commits = commits.Length
                        }
                    },
                    files = files,
                    commits = commits.Take(10).ToArray() // Limit commits shown
                };
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(workingDir))
                {
                    Directory.Delete(workingDir, true);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get PR diff: {ex.Message}");
        }
    }

    /// <summary>
    /// Clone a repository to the local workspace
    /// </summary>
    [McpServerTool, Description("Clone a Git repository to the local workspace")]
    public static async Task<object> CloneRepository(
        [Description("The repository URL to clone")] string repoUrl,
        [Description("Target directory name (optional)")] string? targetDir = null)
    {
        try
        {
            var repoInfo = ParseRepositoryUrl(repoUrl);
            var workingDir = "/data";
            var targetPath = Path.Combine(workingDir, targetDir ?? repoInfo.Name);

            // Validate target directory
            if (!targetPath.StartsWith(workingDir))
            {
                throw new InvalidOperationException("Target directory must be within /data");
            }

            // Remove existing directory if it exists
            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }

            // Create target directory
            Directory.CreateDirectory(targetPath);

            // Clone repository
            var cloneResult = await RunGitCommand($"clone {repoUrl} .", targetPath);

            // Get repository info
            var branchResult = await RunGitCommand("branch -r", targetPath);
            var branches = branchResult.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(b => b.Trim().Replace("origin/", ""))
                .Where(b => b != "HEAD")
                .ToArray();

            var currentBranch = await RunGitCommand("branch --show-current", targetPath);
            var lastCommit = await RunGitCommand("log -1 --oneline", targetPath);

            return new
            {
                success = true,
                repository = new
                {
                    url = repoUrl,
                    name = repoInfo.Name,
                    owner = repoInfo.Owner,
                    localPath = targetPath
                },
                git = new
                {
                    currentBranch = currentBranch.Trim(),
                    branches = branches,
                    lastCommit = lastCommit.Trim()
                },
                cloneOutput = cloneResult
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to clone repository: {ex.Message}");
        }
    }

    /// <summary>
    /// Fetch latest changes from the remote repository
    /// </summary>
    [McpServerTool, Description("Fetch latest changes from the remote repository")]
    public static async Task<object> FetchLatestChanges(
        [Description("Local repository path")] string repoPath,
        [Description("Branch to fetch (optional, defaults to current branch)")] string? branch = null)
    {
        try
        {
            // Validate path is within /data
            var fullPath = Path.GetFullPath(repoPath);
            if (!fullPath.StartsWith("/data"))
            {
                throw new InvalidOperationException("Repository path must be within /data directory");
            }

            if (!Directory.Exists(fullPath))
            {
                throw new InvalidOperationException($"Repository not found at {fullPath}");
            }

            // Check if it's a git repository
            if (!Directory.Exists(Path.Combine(fullPath, ".git")))
            {
                throw new InvalidOperationException($"Not a git repository: {fullPath}");
            }

            // Get current state
            var currentBranch = await RunGitCommand("branch --show-current", fullPath);
            var beforeCommit = await RunGitCommand("rev-parse HEAD", fullPath);

            // Fetch changes
            var fetchBranch = branch ?? currentBranch.Trim();
            var fetchResult = await RunGitCommand($"fetch origin {fetchBranch}", fullPath);

            // Get status after fetch
            var statusResult = await RunGitCommand("status --porcelain", fullPath);
            var logResult = await RunGitCommand($"log --oneline -5", fullPath);

            // Check for updates
            var afterCommit = await RunGitCommand($"rev-parse origin/{fetchBranch}", fullPath);
            var hasUpdates = beforeCommit.Trim() != afterCommit.Trim();

            var result = new
            {
                success = true,
                repository = new
                {
                    path = fullPath,
                    branch = currentBranch.Trim()
                },
                fetch = new
                {
                    branch = fetchBranch,
                    hasUpdates = hasUpdates,
                    beforeCommit = beforeCommit.Trim(),
                    afterCommit = afterCommit.Trim(),
                    output = fetchResult
                },
                status = new
                {
                    workingDirectory = statusResult,
                    recentCommits = logResult.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                }
            };

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to fetch changes: {ex.Message}");
        }
    }

    // Helper methods
    private static bool IsGitHubRepo(string repoUrl) =>
        repoUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase);

    private static bool IsGitLabRepo(string repoUrl) =>
        repoUrl.Contains("gitlab.com", StringComparison.OrdinalIgnoreCase) ||
        repoUrl.Contains("gitlab", StringComparison.OrdinalIgnoreCase);

    private static (string Owner, string Name) ParseRepositoryUrl(string repoUrl)
    {
        try
        {
            // Handle both HTTPS and SSH URLs
            var uri = new Uri(repoUrl.Replace("git@", "https://").Replace(":", "/"));
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            
            if (segments.Length >= 2)
            {
                var owner = segments[segments.Length - 2];
                var name = segments[segments.Length - 1].Replace(".git", "");
                return (owner, name);
            }
        }
        catch
        {
            // Fallback parsing
        }

        // Simple fallback
        var lastSlash = repoUrl.LastIndexOf('/');
        if (lastSlash > 0)
        {
            var name = repoUrl.Substring(lastSlash + 1).Replace(".git", "");
            var secondLastSlash = repoUrl.LastIndexOf('/', lastSlash - 1);
            var owner = secondLastSlash > 0 ? repoUrl.Substring(secondLastSlash + 1, lastSlash - secondLastSlash - 1) : "unknown";
            return (owner, name);
        }

        return ("unknown", "repository");
    }

    private static async Task<string> RunGitCommand(string command, string workingDirectory)
    {
        using var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.Arguments = command;
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException($"Git command failed: {error}");
        }

        return output;
    }
}