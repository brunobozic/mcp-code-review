using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.AI;

public class CodeQualityAgent : ISpecializedAgent
{
    public AgentType Type => AgentType.CodeQuality;
    public string Name => "Code Quality Analysis Agent";
    public string Description => "Specialized in code quality, maintainability, and best practices";

    private readonly IClaudeService _claudeService;
    private readonly ILogger<CodeQualityAgent> _logger;

    public CodeQualityAgent(IClaudeService claudeService, ILogger<CodeQualityAgent> logger)
    {
        _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResult> AnalyzeAsync(string content, Dictionary<string, object> context, CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = $@"Analyze this code for quality and maintainability issues:

{content}

Focus on:
- Code readability and clarity
- Naming conventions
- Method and class organization
- SOLID principles adherence
- Code duplication
- Error handling patterns
- Documentation quality
- Test coverage considerations

Provide specific findings with improvement suggestions.";

            var response = await _claudeService.GenerateTextAsync(prompt, cancellationToken);

            return new AgentResult
            {
                Type = Type,
                Name = Name,
                Summary = "Code quality analysis completed",
                Findings = ParseFindings(response),
                Confidence = 0.8,
                ExecutionTime = TimeSpan.FromSeconds(2)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Code quality analysis failed");
            return new AgentResult
            {
                Type = Type,
                Name = Name,
                Summary = "Code quality analysis failed",
                Findings = new List<Finding>(),
                Confidence = 0.0
            };
        }
    }

    public bool CanHandle(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "csharp" or "c#" or "cs" => true,
            "javascript" or "js" => true,
            "typescript" or "ts" => true,
            "python" or "py" => true,
            _ => false
        };
    }

    private List<Finding> ParseFindings(string response)
    {
        var findings = new List<Finding>();
        
        if (response.Contains("naming") || response.Contains("convention"))
        {
            findings.Add(new Finding
            {
                Type = "Quality",
                Title = "Naming Convention Issue",
                Description = "Code may not follow standard naming conventions",
                Severity = "Low",
                Category = "Quality"
            });
        }

        if (response.Contains("duplicate") || response.Contains("repetition"))
        {
            findings.Add(new Finding
            {
                Type = "Quality",
                Title = "Code Duplication",
                Description = "Duplicate code detected that could be refactored",
                Severity = "Medium",
                Category = "Quality"
            });
        }

        return findings;
    }
}