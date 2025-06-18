using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.AI;

public class SecurityAgent : ISpecializedAgent
{
    public AgentType Type => AgentType.Security;
    public string Name => "Security Analysis Agent";
    public string Description => "Specialized in security vulnerability detection and analysis";

    private readonly IClaudeService _claudeService;
    private readonly ILogger<SecurityAgent> _logger;

    public SecurityAgent(IClaudeService claudeService, ILogger<SecurityAgent> logger)
    {
        _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResult> AnalyzeAsync(string content, Dictionary<string, object> context, CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = $@"Analyze this code for security vulnerabilities:

{content}

Focus on:
- SQL injection vulnerabilities
- Cross-site scripting (XSS)
- Authentication/authorization issues
- Input validation problems
- Cryptographic weaknesses
- Data exposure risks

Provide specific findings with severity levels.";

            var response = await _claudeService.GenerateTextAsync(prompt, cancellationToken);

            return new AgentResult
            {
                Type = Type,
                Name = Name,
                Summary = "Security analysis completed",
                Findings = ParseFindings(response),
                Confidence = 0.8,
                ExecutionTime = TimeSpan.FromSeconds(2)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Security analysis failed");
            return new AgentResult
            {
                Type = Type,
                Name = Name,
                Summary = "Security analysis failed",
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
        
        if (response.Contains("SQL injection") || response.Contains("sql injection"))
        {
            findings.Add(new Finding
            {
                Type = "Security",
                Title = "Potential SQL Injection Vulnerability",
                Description = "Code may be vulnerable to SQL injection attacks",
                Severity = "High",
                Category = "Security"
            });
        }

        if (response.Contains("XSS") || response.Contains("cross-site scripting"))
        {
            findings.Add(new Finding
            {
                Type = "Security", 
                Title = "Cross-Site Scripting Risk",
                Description = "Code may be vulnerable to XSS attacks",
                Severity = "Medium",
                Category = "Security"
            });
        }

        return findings;
    }
}