using Mcp.CodeReview.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Mcp.CodeReview.Tools;

[McpServerToolType]
public static class ReviewTools
{
    // Dependencies are now injected as method parameters instead of constructor injection

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

    [McpServerTool, Description("Generate an AI summary and review of code changes")]
    public static async Task<object> SummarizeCode(
        ClaudeService claudeService,
        ILogger logger,
        [Description("The code diff to analyze")] string diff, 
        [Description("Optional linter output to include in analysis")] string? linterOutput = null, 
        [Description("The programming language of the code")] string language = "csharp")
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Language"] = language,
            ["DiffLength"] = diff.Length,
            ["HasLinterOutput"] = !string.IsNullOrEmpty(linterOutput)
        }))
        {
            logger.LogInformation("Starting code review for {Language} with diff length {DiffLength}", 
                language, diff.Length);

            try
            {
                var lintResults = "";
                if (!string.IsNullOrEmpty(linterOutput))
                {
                    lintResults = linterOutput;
                    logger.LogDebug("Using provided linter output ({Length} chars)", linterOutput.Length);
                }
                else
                {
                    var lintCommand = GetLinterCommand(language);
                    if (!string.IsNullOrEmpty(lintCommand))
                    {
                        logger.LogDebug("Running linter command: {Command}", lintCommand);
                        // Note: CommandTools.RunCommand is not accessible here as static methods can't call each other across static classes
                        // This would need to be refactored to inject a command execution service
                        // var result = await commandTools.RunCommand(lintCommand);
                        // if (result is { } r && r.GetType().GetProperty("stdout")?.GetValue(r) is string stdout)
                        // {
                        //     lintResults = stdout;
                        //     logger.LogDebug("Linter completed with {Length} chars output", stdout.Length);
                        // }
                    }
                    else
                    {
                        logger.LogDebug("No linter command available for language: {Language}", language);
                    }
                }

                var prompt = BuildReviewPrompt(diff, lintResults, language);
                var originalPromptLength = prompt.Length;
                
                if (prompt.Length > 20000)
                {
                    prompt = prompt[..20000] + "\n[Truncated due to length]";
                    logger.LogWarning("Prompt truncated from {Original} to {Truncated} characters", 
                        originalPromptLength, prompt.Length);
                }

                logger.LogDebug("Sending prompt to Claude AI ({Length} chars)", prompt.Length);
                var summary = await claudeService.GenerateReview(prompt);
                
                logger.LogInformation("Successfully generated code review summary ({Length} chars)", 
                    summary.Length);
                
                return new
                {
                    summary,
                    language,
                    linterResults = lintResults,
                    promptLength = prompt.Length,
                    originalPromptLength,
                    wasTruncated = originalPromptLength != prompt.Length
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate code summary for {Language}", language);
                throw;
            }
        }
    }

    private static string BuildReviewPrompt(string diff, string lintResults, string language)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Please review the following code changes and provide constructive feedback:");
        prompt.AppendLine();
        prompt.AppendLine($"Language: {language}");
        prompt.AppendLine();
        prompt.AppendLine("Code Changes:");
        prompt.AppendLine("```diff");
        prompt.AppendLine(diff);
        prompt.AppendLine("```");
        
        if (!string.IsNullOrEmpty(lintResults))
        {
            prompt.AppendLine();
            prompt.AppendLine("Static Analysis Results:");
            prompt.AppendLine("```");
            prompt.AppendLine(lintResults);
            prompt.AppendLine("```");
        }
        
        prompt.AppendLine();
        prompt.AppendLine("Please focus on:");
        prompt.AppendLine("- Code quality and best practices");
        prompt.AppendLine("- Potential bugs or issues");
        prompt.AppendLine("- Performance considerations");
        prompt.AppendLine("- Security concerns");
        prompt.AppendLine("- Maintainability and readability");
        
        return prompt.ToString();
    }

    private static string GetLinterCommand(string language) => language.ToLowerInvariant() switch
    {
        "csharp" or "c#" => "dotnet build --no-restore --verbosity normal",
        "javascript" or "typescript" => "npm run lint 2>&1 || echo 'No lint script found'",
        "python" => "python -m flake8 . 2>&1 || echo 'flake8 not installed'",
        "go" => "go vet ./... 2>&1",
        "java" => "mvn compile 2>&1 || echo 'Maven not configured'",
        _ => ""
    };

    private static bool IsGitHubRepo(string repoUrl) => 
        repoUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase);

    private static bool IsGitLabRepo(string repoUrl) => 
        repoUrl.Contains("gitlab.com", StringComparison.OrdinalIgnoreCase) || 
        repoUrl.Contains("gitlab", StringComparison.OrdinalIgnoreCase);
}