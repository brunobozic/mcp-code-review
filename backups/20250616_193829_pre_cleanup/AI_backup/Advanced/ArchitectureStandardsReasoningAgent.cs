using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;
using System.Text.Json;

namespace Mcp.CodeReview.AI.Advanced;

/// <summary>
/// Specialized reasoning agent for architecture standards analysis
/// </summary>
public class ArchitectureStandardsReasoningAgent : ISpecializedReasoningAgent
{
    private readonly IClaudeService _claudeService;
    private readonly AdvancedReasoningEngine _reasoningEngine;
    private readonly ILogger<ArchitectureStandardsReasoningAgent> _logger;

    public ArchitectureStandardsReasoningAgent(
        IClaudeService claudeService,
        AdvancedReasoningEngine reasoningEngine,
        ILogger<ArchitectureStandardsReasoningAgent> logger)
    {
        _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
        _reasoningEngine = reasoningEngine ?? throw new ArgumentNullException(nameof(reasoningEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Analyzes code for architecture standards compliance with enhanced reasoning
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
    /// Analyzes code for architecture standards compliance
    /// </summary>
    public async Task<AgentResult> AnalyzeAsync(string code, string context = "", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting architecture standards analysis");

            var prompt = BuildArchitectureStandardsPrompt(code, context);
            
            // Use reasoning engine for enhanced analysis
            var reasoningResult = await _reasoningEngine.ProcessWithHybridReasoningAsync(
                prompt,
                ReasoningMode.ChainOfThought,
                cancellationToken);

            var result = new AgentResult
            {
                AgentType = AgentType.ArchitectureStandardsAgent,
                AgentName = "Architecture Standards Specialist",
                Analysis = reasoningResult.Analysis,
                ConfidenceScore = reasoningResult.Confidence,
                Findings = ExtractFindings(reasoningResult.Analysis),
                Recommendations = ExtractRecommendations(reasoningResult.Analysis),
                Metadata = new Dictionary<string, object>
                {
                    ["reasoning_steps"] = reasoningResult.ReasoningSteps,
                    ["analysis_timestamp"] = DateTime.UtcNow,
                    ["standards_focus"] = "architecture_compliance"
                }
            };

            _logger.LogInformation("Architecture standards analysis completed with {FindingsCount} findings", 
                result.Findings.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in architecture standards analysis");
            return CreateErrorResult(ex.Message);
        }
    }

    private string BuildArchitectureStandardsPrompt(string code, string context)
    {
        return $@"
As an Architecture Standards Specialist, analyze this code for compliance with architectural best practices and standards.

Focus on:
1. **Design Patterns**: Proper implementation of established patterns
2. **SOLID Principles**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion
3. **Separation of Concerns**: Clear boundaries between layers and responsibilities
4. **Dependency Management**: Proper dependency injection and inversion of control
5. **Modularity**: Clear module boundaries and minimal coupling
6. **API Design**: RESTful principles, consistent interfaces
7. **Data Access Patterns**: Repository, Unit of Work, etc.
8. **Error Handling Patterns**: Consistent exception handling strategies

Code to analyze:
```
{code}
```

Context: {context}

Provide:
1. **Architecture Assessment**: Overall architectural quality
2. **Standards Compliance**: Areas where code follows/violates standards
3. **Pattern Usage**: Identification of design patterns used or missing
4. **SOLID Analysis**: Evaluation against SOLID principles
5. **Improvement Recommendations**: Specific architectural improvements
6. **Refactoring Suggestions**: How to better align with standards

Focus on actionable architectural guidance that improves maintainability and follows industry standards.
";
    }

    private List<Finding> ExtractFindings(string analysis)
    {
        var findings = new List<Finding>();

        // Extract architecture-specific findings
        if (analysis.Contains("violation", StringComparison.OrdinalIgnoreCase) || 
            analysis.Contains("violates", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new Finding
            {
                Type = "Architecture Standards Violation",
                Title = "Standards Compliance Issues",
                Description = "Code violates established architecture standards",
                Severity = "High",
                Impact = "May affect maintainability and code quality",
                Tags = new List<string> { "architecture", "standards", "violation" }
            });
        }

        if (analysis.Contains("SOLID", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new Finding
            {
                Type = "SOLID Principles",
                Title = "SOLID Principles Analysis",
                Description = "Code evaluation against SOLID principles",
                Severity = "Medium",
                Impact = "Affects code maintainability and extensibility",
                Tags = new List<string> { "SOLID", "principles", "design" }
            });
        }

        if (analysis.Contains("pattern", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new Finding
            {
                Type = "Design Patterns",
                Title = "Pattern Implementation Review",
                Description = "Analysis of design pattern usage",
                Severity = "Medium",
                Impact = "Impacts code structure and reusability",
                Tags = new List<string> { "patterns", "design", "architecture" }
            });
        }

        return findings;
    }

    private List<Recommendation> ExtractRecommendations(string analysis)
    {
        var recommendations = new List<Recommendation>();

        recommendations.Add(new Recommendation
        {
            Title = "Architecture Standards Compliance",
            Category = "Architecture",
            Priority = "High",
            Description = "Ensure code follows established architectural standards and patterns",
            Implementation = "Review and refactor code to align with architectural guidelines",
            Benefits = new List<string> 
            { 
                "Improved maintainability",
                "Better code organization",
                "Enhanced team productivity",
                "Reduced technical debt"
            },
            EstimatedEffort = "Medium"
        });

        return recommendations;
    }

    private AgentResult CreateErrorResult(string errorMessage)
    {
        return new AgentResult
        {
            AgentType = AgentType.ArchitectureStandardsAgent,
            AgentName = "Architecture Standards Specialist",
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