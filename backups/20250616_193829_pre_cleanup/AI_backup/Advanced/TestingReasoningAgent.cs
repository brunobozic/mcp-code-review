using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;
using System.Text.Json;

namespace Mcp.CodeReview.AI.Advanced;

/// <summary>
/// Specialized reasoning agent for testing analysis and recommendations
/// </summary>
public class TestingReasoningAgent : ISpecializedReasoningAgent
{
    private readonly IClaudeService _claudeService;
    private readonly AdvancedReasoningEngine _reasoningEngine;
    private readonly ILogger<TestingReasoningAgent> _logger;

    public TestingReasoningAgent(
        IClaudeService claudeService,
        AdvancedReasoningEngine reasoningEngine,
        ILogger<TestingReasoningAgent> logger)
    {
        _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
        _reasoningEngine = reasoningEngine ?? throw new ArgumentNullException(nameof(reasoningEngine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Analyzes code for testing coverage with enhanced reasoning
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
    /// Analyzes code for testing coverage and recommendations
    /// </summary>
    public async Task<AgentResult> AnalyzeAsync(string code, string context = "", CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting testing analysis");

            var prompt = BuildTestingAnalysisPrompt(code, context);
            
            // Use reasoning engine for enhanced analysis
            var reasoningResult = await _reasoningEngine.ProcessWithHybridReasoningAsync(
                prompt,
                ReasoningMode.ChainOfThought,
                cancellationToken);

            var result = new AgentResult
            {
                AgentType = AgentType.TestingSpecialist,
                AgentName = "Testing Specialist",
                Analysis = reasoningResult.Analysis,
                ConfidenceScore = reasoningResult.Confidence,
                Findings = ExtractFindings(reasoningResult.Analysis),
                Recommendations = ExtractRecommendations(reasoningResult.Analysis),
                Metadata = new Dictionary<string, object>
                {
                    ["reasoning_steps"] = reasoningResult.ReasoningSteps,
                    ["analysis_timestamp"] = DateTime.UtcNow,
                    ["testing_focus"] = "test_coverage_and_quality"
                }
            };

            _logger.LogInformation("Testing analysis completed with {FindingsCount} findings", 
                result.Findings.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in testing analysis");
            return CreateErrorResult(ex.Message);
        }
    }

    private string BuildTestingAnalysisPrompt(string code, string context)
    {
        return $@"
As a Testing Specialist, analyze this code for testability, test coverage needs, and testing best practices.

Focus on:
1. **Testability**: How easy is this code to test
2. **Test Coverage**: What tests should be written
3. **Test Types**: Unit, integration, end-to-end test needs
4. **Test Scenarios**: Edge cases, happy path, error conditions
5. **Mocking Needs**: Dependencies that should be mocked
6. **Test Structure**: Arrange-Act-Assert pattern adherence
7. **Test Data**: Test data requirements and setup
8. **Assertions**: What should be verified in tests

Code to analyze:
```
{code}
```

Context: {context}

Provide:
1. **Testability Assessment**: How testable is this code
2. **Test Coverage Analysis**: What tests are needed
3. **Test Scenarios**: Specific test cases to implement
4. **Testing Challenges**: Obstacles to effective testing
5. **Refactoring for Testability**: Changes to improve testability
6. **Test Implementation Guide**: Concrete test examples
7. **Testing Strategy**: Overall approach for testing this code

Focus on practical testing guidance that improves code reliability and confidence.
";
    }

    private List<Finding> ExtractFindings(string analysis)
    {
        var findings = new List<Finding>();

        // Extract testing-specific findings
        if (analysis.Contains("hard to test", StringComparison.OrdinalIgnoreCase) || 
            analysis.Contains("testability", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new Finding
            {
                Type = "Testability Issues",
                Title = "Code Testability Concerns",
                Description = "Code structure may make testing difficult",
                Severity = "Medium",
                Impact = "Reduces test coverage and code confidence",
                Tags = new List<string> { "testability", "testing", "structure" }
            });
        }

        if (analysis.Contains("test coverage", StringComparison.OrdinalIgnoreCase) ||
            analysis.Contains("missing tests", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new Finding
            {
                Type = "Test Coverage",
                Title = "Test Coverage Analysis",
                Description = "Analysis of test coverage needs and gaps",
                Severity = "Medium",
                Impact = "Affects code reliability and bug detection",
                Tags = new List<string> { "coverage", "testing", "quality" }
            });
        }

        if (analysis.Contains("edge case", StringComparison.OrdinalIgnoreCase) ||
            analysis.Contains("error condition", StringComparison.OrdinalIgnoreCase))
        {
            findings.Add(new Finding
            {
                Type = "Test Scenarios",
                Title = "Critical Test Scenarios",
                Description = "Important test scenarios that should be covered",
                Severity = "Medium",
                Impact = "Missing tests could allow bugs in production",
                Tags = new List<string> { "scenarios", "edge-cases", "testing" }
            });
        }

        return findings;
    }

    private List<Recommendation> ExtractRecommendations(string analysis)
    {
        var recommendations = new List<Recommendation>();

        recommendations.Add(new Recommendation
        {
            Title = "Improve Test Coverage",
            Category = "Testing",
            Priority = "High",
            Description = "Implement comprehensive test suite covering key scenarios",
            Implementation = "Write unit tests for core functionality and edge cases",
            Benefits = new List<string> 
            { 
                "Increased code confidence",
                "Better bug detection",
                "Safer refactoring",
                "Improved code quality"
            },
            EstimatedEffort = "Medium"
        });

        return recommendations;
    }

    private AgentResult CreateErrorResult(string errorMessage)
    {
        return new AgentResult
        {
            AgentType = AgentType.TestingSpecialist,
            AgentName = "Testing Specialist",
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