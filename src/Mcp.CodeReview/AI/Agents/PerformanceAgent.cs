using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.AI;

public class PerformanceAgent : ISpecializedAgent
{
    public AgentType Type => AgentType.Performance;
    public string Name => "Performance Analysis Agent";
    public string Description => "Specialized in performance optimization and efficiency analysis";

    private readonly IClaudeService _claudeService;
    private readonly ILogger<PerformanceAgent> _logger;

    public PerformanceAgent(IClaudeService claudeService, ILogger<PerformanceAgent> logger)
    {
        _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResult> AnalyzeAsync(string content, Dictionary<string, object> context, CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = $@"Analyze this code for performance issues:

{content}

Focus on:
- Algorithm efficiency and Big O complexity
- Memory usage patterns
- Database query optimization
- Loop optimization
- String manipulation efficiency
- Resource management
- Async/await patterns

Provide specific findings with performance impact assessment.";

            var response = await _claudeService.GenerateTextAsync(prompt, cancellationToken);

            return new AgentResult
            {
                Type = Type,
                Name = Name,
                Summary = "Performance analysis completed",
                Findings = ParseFindings(response),
                Confidence = 0.8,
                ExecutionTime = TimeSpan.FromSeconds(2)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Performance analysis failed");
            return new AgentResult
            {
                Type = Type,
                Name = Name,
                Summary = "Performance analysis failed",
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
        
        if (response.Contains("inefficient") || response.Contains("O(n"))
        {
            findings.Add(new Finding
            {
                Type = "Performance",
                Title = "Algorithm Efficiency Issue",
                Description = "Algorithm may have suboptimal time complexity",
                Severity = "Medium",
                Category = "Performance"
            });
        }

        if (response.Contains("memory") || response.Contains("leak"))
        {
            findings.Add(new Finding
            {
                Type = "Performance",
                Title = "Memory Usage Concern",
                Description = "Code may have memory usage issues",
                Severity = "Medium",
                Category = "Performance"
            });
        }

        return findings;
    }
}