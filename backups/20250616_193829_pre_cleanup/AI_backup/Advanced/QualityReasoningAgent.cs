using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;
using System.Text.Json;

namespace Mcp.CodeReview.AI.Advanced;

/// <summary>
/// Specialized reasoning agent for code quality analysis
/// </summary>
public class QualityReasoningAgent : ISpecializedReasoningAgent
{
    private readonly IClaudeService _claudeService;
    private readonly AdvancedReasoningEngine _reasoningEngine;
    private readonly ILogger<QualityReasoningAgent> _logger;

    public QualityReasoningAgent(
        IClaudeService claudeService,
        AdvancedReasoningEngine reasoningEngine,
        ILogger<QualityReasoningAgent> logger)
    {
        _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
        _reasoningEngine = reasoningEngine ?? throw new ArgumentNullException(nameof(reasoningEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Analyzes code for quality metrics with enhanced reasoning
    /// </summary>
    public async Task<AgentResult> AnalyzeWithReasoningAsync(
        CodeReviewRequest request,
        EnhancedContext context,
        ReasoningMode mode,
        CancellationToken cancellationToken = default)
    {
        return await AnalyzeAsync(request.Content, request.Context, cancellationToken);
    }

    /// <summary>
    /// Analyzes code for quality metrics and improvements
    /// </summary>
    public async Task<AgentResult> AnalyzeAsync(string code, string context = "", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting code quality analysis");

            var prompt = BuildQualityAnalysisPrompt(code, context);
            
            // Use reasoning engine for enhanced analysis
            var reasoningResult = await _reasoningEngine.ProcessWithHybridReasoningAsync(
                prompt,
                ReasoningMode.ChainOfThought,
                cancellationToken);

            var result = new AgentResult
            {
                AgentType = AgentType.CodeQualityReviewer,
                AgentName = "Code Quality Specialist",
                Analysis = reasoningResult.Analysis,
                ConfidenceScore = reasoningResult.Confidence,
                Findings = ExtractFindings(reasoningResult.Analysis),
                Recommendations = ExtractRecommendations(reasoningResult.Analysis),
                Metadata = new Dictionary<string, object>
                {
                    ["reasoning_steps"] = reasoningResult.ReasoningSteps,
                    ["analysis_timestamp"] = DateTime.UtcNow,
                    ["quality_focus"] = "code_maintainability"
                }
            };

            _logger.LogInformation("Code quality analysis completed with {FindingsCount} findings", 
                result.Findings.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in code quality analysis");
            return CreateErrorResult(ex.Message);
        }
    }

    private string BuildQualityAnalysisPrompt(string code, string context)
    {
        return $@"
As a Code Quality Specialist, analyze this code for maintainability, readability, and overall quality.

Focus on:
1. **Code Clarity**: Variable naming, function naming, class structure
2. **Maintainability**: Code organization, modularity, simplicity
3. **Readability**: Comments, documentation, code flow
4. **Complexity**: Cyclomatic complexity, nesting levels
5. **Code Smells**: Duplicated code, long methods, large classes
6. **Best Practices**: Language-specific conventions and idioms
7. **Documentation**: Inline comments, method documentation
8. **Error Handling**: Exception handling patterns

Code to analyze:
```
{code}
```

Context: {context}

Provide:
1. **Quality Assessment**: Overall code quality score and rationale
2. **Readability Analysis**: How easy is the code to understand
3. **Maintainability Review**: How easy would this be to modify
4. **Code Smells**: Identification of potential quality issues
5. **Complexity Analysis**: Areas of high complexity
6. **Improvement Suggestions**: Specific refactoring recommendations
7. **Best Practice Alignment**: Adherence to coding standards

Focus on actionable quality improvements that enhance long-term maintainability.
";
    }

    private List<Finding> ExtractFindings(string analysis)
    {
        var findings = new List<Finding>();

        // Extract quality-specific findings
        if (analysis.Contains("complex", StringComparison.OrdinalIgnoreCase) || 
            analysis.Contains("complexity", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new Finding
            {
                Type = "Code Complexity",
                Title = "High Complexity Detected",
                Description = "Code has high complexity that may impact maintainability",
                Severity = "Medium",
                Impact = "Increases maintenance burden and bug risk",
                Tags = new List<string> { "complexity", "maintainability", "quality" }
            });
        }

        if (analysis.Contains("smell", StringComparison.OrdinalIgnoreCase) ||
            analysis.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new Finding
            {
                Type = "Code Smell",
                Title = "Code Quality Issues",
                Description = "Code contains patterns that may indicate quality problems",
                Severity = "Medium",
                Impact = "May affect code maintainability and clarity",
                Tags = new List<string> { "code-smell", "quality", "refactoring" }
            });
        }

        if (analysis.Contains("readable", StringComparison.OrdinalIgnoreCase) ||
            analysis.Contains("clarity", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new Finding
            {
                Type = "Readability",
                Title = "Code Readability Assessment",
                Description = "Analysis of code clarity and readability",
                Severity = "Low",
                Impact = "Affects team productivity and code understanding",
                Tags = new List<string> { "readability", "clarity", "documentation" }
            });
        }

        return findings;
    }

    private List<Recommendation> ExtractRecommendations(string analysis)
    {
        var recommendations = new List<Recommendation>();

        recommendations.Add(new Recommendation
        {
            Title = "Improve Code Quality",
            Category = "Quality",
            Priority = "Medium",
            Description = "Enhance code maintainability and readability through targeted improvements",
            Implementation = "Address identified code smells and complexity issues",
            Benefits = new List<string> 
            { 
                "Improved maintainability",
                "Enhanced readability",
                "Reduced bug risk",
                "Faster development velocity"
            },
            EstimatedEffort = "Medium"
        });

        return recommendations;
    }

    private AgentResult CreateErrorResult(string errorMessage)
    {
        return new AgentResult
        {
            AgentType = AgentType.CodeQualityReviewer,
            AgentName = "Code Quality Specialist",
            Analysis = $"Analysis failed: {errorMessage}",
            ConfidenceScore = 0.0,
            Findings = new List<Finding>(),
            Recommendations = new List<Recommendation>(),
            Metadata = new Dictionary<string, object>
            {
                ["error"] = errorMessage,
                ["analysis_timestamp"] = DateTime.UtcNow
            }
        };
    }
}