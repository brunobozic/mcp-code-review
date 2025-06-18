using LibGit2Sharp;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Mcp.CodeReview.Tools;

[McpServerToolType]
public static class GitTools
{
    private const string ReposRoot = "/data/repos";
    private const long MaxRepoSize = 500 * 1024 * 1024; // 500MB
    private const int MaxDiffFiles = 100;
    private const int MaxDiffLines = 10000;
    
    private static readonly Regex ValidRepoUrlPattern = new(
        @"^https://github\.com/[\w\-\.]+/[\w\-\.]+(?:\.git)?$|^https://gitlab\.com/[\w\-\.]+/[\w\-\.]+(?:\.git)?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly Regex ValidBranchPattern = new(
        @"^[a-zA-Z0-9/_\-\.]+$",
        RegexOptions.Compiled
    );

    [McpServerTool, Description("Get the diff for a pull request")]
    public static async Task<object> GetPRDiff(
        [Description("The repository URL")] string repoUrl, 
        [Description("The base branch name")] string baseBranch, 
        [Description("The head branch name")] string headBranch)
    {
        // Input validation
        ValidateInputs(repoUrl, baseBranch, headBranch);

        var repoHash = ComputeHash(repoUrl);
        var repoPath = Path.Combine(ReposRoot, repoHash);

        try
        {
            await EnsureRepository(repoUrl, repoPath);

            using var repo = new Repository(repoPath);
            
            var baseCommit = FindCommit(repo, baseBranch);
            var headCommit = FindCommit(repo, headBranch);

            if (baseCommit == null || headCommit == null)
                throw new InvalidOperationException($"Could not find commits for branches {baseBranch} or {headBranch}");

            var diff = repo.Diff.Compare<Patch>(baseCommit.Tree, headCommit.Tree);
            
            if (diff.Count() > MaxDiffFiles)
                throw new InvalidOperationException($"Too many files changed (max {MaxDiffFiles})");

            var totalLines = diff.Sum(p => p.LinesAdded + p.LinesDeleted);
            if (totalLines > MaxDiffLines)
                throw new InvalidOperationException($"Diff too large (max {MaxDiffLines} lines)");

            var files = diff.Select(patch => new
            {
                path = patch.Path,
                status = patch.Status.ToString(),
                additions = patch.LinesAdded,
                deletions = patch.LinesDeleted,
                patch = patch.Patch
            }).ToArray();

            return new
            {
                baseBranch,
                headBranch,
                baseCommit = baseCommit.Sha,
                headCommit = headCommit.Sha,
                files,
                totalFiles = files.Length,
                totalAdditions = files.Sum(f => f.additions),
                totalDeletions = files.Sum(f => f.deletions)
            };
        }
        catch (LibGit2SharpException ex)
        {
            throw new InvalidOperationException($"Git operation failed: {ex.Message}");
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            throw new InvalidOperationException($"Unexpected error: {ex.Message}");
        }
    }

    private static void ValidateInputs(string repoUrl, string baseBranch, string headBranch)
    {
        if (string.IsNullOrWhiteSpace(repoUrl))
            throw new InvalidOperationException("Repository URL cannot be empty");

        if (string.IsNullOrWhiteSpace(baseBranch))
            throw new InvalidOperationException("Base branch cannot be empty");

        if (string.IsNullOrWhiteSpace(headBranch))
            throw new InvalidOperationException("Head branch cannot be empty");

        if (!ValidRepoUrlPattern.IsMatch(repoUrl))
            throw new InvalidOperationException("Invalid repository URL. Only GitHub and GitLab HTTPS URLs are allowed");

        if (!ValidBranchPattern.IsMatch(baseBranch))
            throw new InvalidOperationException("Invalid base branch name");

        if (!ValidBranchPattern.IsMatch(headBranch))
            throw new InvalidOperationException("Invalid head branch name");

        if (baseBranch.Length > 100 || headBranch.Length > 100)
            throw new InvalidOperationException("Branch names too long (max 100 characters)");
    }

    private static Commit? FindCommit(Repository repo, string branchName)
    {
        // Try local branch first
        var branch = repo.Branches[branchName];
        if (branch != null)
            return branch.Tip;

        // Try remote branch
        var remoteBranch = repo.Branches[$"origin/{branchName}"];
        if (remoteBranch != null)
            return remoteBranch.Tip;

        // Try as commit SHA
        if (branchName.Length >= 7 && branchName.All(c => char.IsLetterOrDigit(c)))
        {
            try
            {
                return repo.Lookup<Commit>(branchName);
            }
            catch (LibGit2SharpException)
            {
                // Not a valid commit SHA
            }
        }

        return null;
    }

    private static async Task EnsureRepository(string repoUrl, string repoPath)
    {
        if (Directory.Exists(repoPath))
        {
            await UpdateRepository(repoPath);
        }
        else
        {
            await CloneRepository(repoUrl, repoPath);
        }
    }

    private static async Task UpdateRepository(string repoPath)
    {
        try
        {
            using var repo = new Repository(repoPath);
            var remote = repo.Network.Remotes["origin"];
            if (remote == null)
                throw new InvalidOperationException("No origin remote found in repository");

            var fetchOptions = new FetchOptions
            {
                Prune = true
            };

            Commands.Fetch(repo, remote.Name, Array.Empty<string>(), fetchOptions, null);
        }
        catch (LibGit2SharpException ex)
        {
            throw new InvalidOperationException($"Failed to update repository: {ex.Message}");
        }
    }

    private static async Task CloneRepository(string repoUrl, string repoPath)
    {
        try
        {
            var parentDir = Path.GetDirectoryName(repoPath);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                Directory.CreateDirectory(parentDir);

            var cloneOptions = new CloneOptions
            {
                IsBare = false,
                Checkout = true
            };

            Repository.Clone(repoUrl, repoPath, cloneOptions);

            // Check repository size
            var repoSize = GetDirectorySize(repoPath);
            if (repoSize > MaxRepoSize)
            {
                Directory.Delete(repoPath, true);
                throw new InvalidOperationException($"Repository too large (max {MaxRepoSize / (1024 * 1024)}MB)");
            }
        }
        catch (LibGit2SharpException ex)
        {
            // Clean up on failure
            if (Directory.Exists(repoPath))
            {
                try
                {
                    Directory.Delete(repoPath, true);
                }
                catch { /* Ignore cleanup errors */ }
            }
            throw new InvalidOperationException($"Failed to clone repository: {ex.Message}");
        }
    }

    private static long GetDirectorySize(string path)
    {
        var dirInfo = new DirectoryInfo(path);
        return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
            .Sum(file => file.Length);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}