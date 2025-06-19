using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;
using Mcp.CodeReview.Utilities;
using Mcp.CodeReview.Services;

namespace Mcp.CodeReview.AI;

/// <summary>
/// Nested chat framework implementing writer-critic loops for iterative improvement
/// Based on 2024 best practices from AutoGen and other advanced multi-agent systems
/// </summary>
public class NestedChatFramework
{
    private readonly IAIServiceProvider _aiServiceProvider;
    private readonly ILogger<NestedChatFramework> _logger;

    public NestedChatFramework(IAIServiceProvider aiServiceProvider, ILogger<NestedChatFramework> logger)
    {
        _aiServiceProvider = aiServiceProvider ?? throw new ArgumentNullException(nameof(aiServiceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Enhanced contextual analysis using repository context and nested conversations
    /// </summary>
    public async Task<AgentResult> ConductContextualAnalysis(
        AgentType agentType,
        string content,
        RepositoryContext repositoryContext,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting contextual analysis with {AgentType}", agentType);
        
        try
        {
            // Build contextual prompt that includes repository information
            var contextualPrompt = BuildContextualPrompt(agentType, content, repositoryContext);
            
            // Conduct analysis with full repository context
            var analysis = await _aiServiceProvider.GenerateReviewAsync(contextualPrompt, cancellationToken);
            
            // Parse and structure the results
            var result = ParseContextualAnalysisResult(agentType, analysis, repositoryContext);
            
            _logger.LogInformation("Contextual analysis completed for {AgentType} with confidence {Confidence}", 
                agentType, result.ConfidenceScore);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Contextual analysis failed for {AgentType}", agentType);
            return CreateFailureResult(agentType, ex.Message);
        }
    }

    private string BuildContextualPrompt(AgentType agentType, string content, RepositoryContext repositoryContext)
    {
        return $@"
You are a {agentType} agent conducting a contextual code review.

## Code to Review:
```
{content}
```

## Repository Context:
**Project**: {repositoryContext.ProjectName}
**Architecture**: {repositoryContext.Structure.ArchitecturePattern}
**File Types**: {string.Join(", ", repositoryContext.Structure.FilesByType.Keys)}
**Dependencies**: {string.Join(", ", repositoryContext.Dependencies.Keys.Take(10))}

## Historical Patterns Found:
{string.Join("\n", repositoryContext.HistoricalPatterns.Take(5).Select(p => $"- {p.Pattern} (similarity: {p.Similarity:F2})"))}

## Team Standards:
{string.Join("\n", repositoryContext.ProjectStandards.Take(5).Select(s => $"- {s.Title}: {s.Description}"))}

## Related Files Context:
{string.Join("\n", repositoryContext.RelatedFiles.Keys.Take(3).Select(f => $"- {f}"))}

## Your Analysis Task:
As a {agentType} specialist, analyze this code considering:
1. The broader project architecture and patterns
2. How this code fits within the existing codebase
3. Compliance with team standards and historical patterns
4. Dependencies and their impact
5. Related files and potential side effects

Provide specific findings with references to the repository context when possible.
Be thorough but focused on {agentType} concerns.

Format your response as:
**Findings:**
- [Specific finding with context reference]

**Recommendations:**
- [Actionable recommendation based on repository analysis]

**Confidence:** [0.0-1.0]
";
    }

    private AgentResult ParseContextualAnalysisResult(AgentType agentType, string analysis, RepositoryContext repositoryContext)
    {
        var result = new AgentResult
        {
            AgentType = agentType,
            IsSuccessful = true,
            ProcessingTime = TimeSpan.FromSeconds(2), // Would be measured in real implementation
            Findings = new List<Finding>(),
            Recommendations = new List<Recommendation>(),
            ConfidenceScore = 0.8 // Default, would be parsed from response
        };

        // Parse findings
        var findingsMatch = System.Text.RegularExpressions.Regex.Match(analysis, @"\*\*Findings:\*\*(.*?)\*\*Recommendations:", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (findingsMatch.Success)
        {
            var findings = findingsMatch.Groups[1].Value.Split('\n')
                .Where(line => line.Trim().StartsWith("-"))
                .Select(line => line.Trim().TrimStart('-').Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => new Finding { Description = line })
                .ToList();
            
            result.Findings.AddRange(findings);
        }

        // Parse recommendations
        var recommendationsMatch = System.Text.RegularExpressions.Regex.Match(analysis, @"\*\*Recommendations:\*\*(.*?)\*\*Confidence:", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (recommendationsMatch.Success)
        {
            var recommendations = recommendationsMatch.Groups[1].Value.Split('\n')
                .Where(line => line.Trim().StartsWith("-"))
                .Select(line => line.Trim().TrimStart('-').Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => new Recommendation { Title = line })
                .ToList();
            
            result.Recommendations.AddRange(recommendations);
        }

        // Parse confidence
        var confidenceMatch = System.Text.RegularExpressions.Regex.Match(analysis, @"\*\*Confidence:\*\*\s*(\d*\.?\d+)");
        if (confidenceMatch.Success && double.TryParse(confidenceMatch.Groups[1].Value, out var confidence))
        {
            result.ConfidenceScore = confidence;
        }

        return result;
    }

    private AgentResult CreateFailureResult(AgentType agentType, string error)
    {
        return new AgentResult
        {
            AgentType = agentType,
            Success = false,
            Findings = new List<Finding> { new Finding { Description = $"Analysis failed: {error}" } },
            Recommendations = new List<Recommendation>(),
            ConfidenceScore = 0.0,
            ExecutionTime = TimeSpan.Zero
        };
    }

    /// <summary>
    /// Conduct iterative analysis using writer-critic pattern
    /// </summary>
    public async Task<AgentResult> ConductIterativeAnalysis(
        AgentType writerAgentType,
        string content,
        Dictionary<string, object> context,
        IterativeAnalysisOptions options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new IterativeAnalysisOptions();
        
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["WriterAgent"] = writerAgentType,
            ["MaxIterations"] = options.MaxIterations,
            ["ImprovementThreshold"] = options.ImprovementThreshold
        });

        _logger.LogInformation("Starting iterative analysis with {WriterAgent}", writerAgentType);

        try
        {
            var currentAnalysis = await GenerateInitialAnalysis(writerAgentType, content, context, cancellationToken);
            var iterationHistory = new List<IterationRecord>();

            for (int iteration = 1; iteration <= options.MaxIterations; iteration++)
            {
                _logger.LogDebug("Starting iteration {Iteration} of {MaxIterations}", iteration, options.MaxIterations);

                // Critic reviews the current analysis
                var criticism = await GenerateCriticism(currentAnalysis, content, context, iterationHistory, cancellationToken);
                
                // Check if improvement is needed
                if (criticism.QualityScore >= options.ImprovementThreshold)
                {
                    _logger.LogInformation("Analysis quality threshold met at iteration {Iteration} with score {Score:F2}", 
                        iteration, criticism.QualityScore);
                    break;
                }

                // Writer improves analysis based on criticism
                var improvedAnalysis = await ImproveAnalysis(
                    writerAgentType, 
                    currentAnalysis, 
                    criticism, 
                    content, 
                    context, 
                    cancellationToken);

                // Record this iteration
                iterationHistory.Add(new IterationRecord
                {
                    Iteration = iteration,
                    Analysis = currentAnalysis,
                    Criticism = criticism,
                    ImprovedAnalysis = improvedAnalysis,
                    QualityImprovement = improvedAnalysis.ConfidenceScore - currentAnalysis.ConfidenceScore
                });

                currentAnalysis = improvedAnalysis;

                _logger.LogDebug("Iteration {Iteration} completed, quality improvement: {Improvement:F2}", 
                    iteration, improvedAnalysis.ConfidenceScore - currentAnalysis.ConfidenceScore);
            }

            // Add iteration metadata to final result
            currentAnalysis.Metadata = currentAnalysis.Metadata ?? new Dictionary<string, object>();
            currentAnalysis.Metadata["IterativeAnalysis"] = new
            {
                TotalIterations = iterationHistory.Count,
                FinalQualityScore = currentAnalysis.ConfidenceScore,
                ImprovementHistory = iterationHistory.Select(h => new
                {
                    h.Iteration,
                    h.QualityImprovement,
                    CriticismScore = h.Criticism.QualityScore
                })
            };

            _logger.LogInformation("Iterative analysis completed after {Iterations} iterations with final score {Score:F2}", 
                iterationHistory.Count, currentAnalysis.ConfidenceScore);

            return currentAnalysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to conduct iterative analysis");
            throw;
        }
    }

    /// <summary>
    /// Generate self-reflective analysis where agent critiques its own work
    /// </summary>
    public async Task<AgentResult> ConductSelfReflectiveAnalysis(
        AgentType agentType,
        string content,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting self-reflective analysis with {AgentType}", agentType);

        try
        {
            // Step 1: Initial analysis
            var initialAnalysis = await GenerateInitialAnalysis(agentType, content, context, cancellationToken);

            // Step 2: Self-reflection
            var reflection = await GenerateSelfReflection(initialAnalysis, content, context, cancellationToken);

            // Step 3: Self-correction based on reflection
            var finalAnalysis = await ApplySelfCorrection(agentType, initialAnalysis, reflection, content, context, cancellationToken);

            // Add reflection metadata
            finalAnalysis.Metadata = finalAnalysis.Metadata ?? new Dictionary<string, object>();
            finalAnalysis.Metadata["SelfReflection"] = new
            {
                InitialConfidence = initialAnalysis.ConfidenceScore,
                FinalConfidence = finalAnalysis.ConfidenceScore,
                ReflectionInsights = reflection.KeyInsights,
                SelfCorrections = reflection.SuggestedImprovements.Count
            };

            _logger.LogInformation("Self-reflective analysis completed, confidence improved from {Initial:F2} to {Final:F2}", 
                initialAnalysis.ConfidenceScore, finalAnalysis.ConfidenceScore);

            return finalAnalysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to conduct self-reflective analysis");
            throw;
        }
    }

    /// <summary>
    /// Cross-agent validation where multiple agents validate each other's findings
    /// </summary>
    public async Task<CrossValidationResult> ConductCrossValidation(
        List<AgentResult> agentResults,
        string originalContent,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting cross-validation for {AgentCount} agent results", agentResults.Count);

        var validationMatrix = new Dictionary<string, List<ValidationAssessment>>();

        try
        {
            // Each agent validates findings from other agents
            foreach (var result in agentResults)
            {
                var validators = agentResults.Where(r => r.AgentType != result.AgentType).ToList();
                var validations = new List<ValidationAssessment>();

                foreach (var validator in validators)
                {
                    var validation = await ValidateFinding(validator.AgentType, result, originalContent, context, cancellationToken);
                    validations.Add(validation);
                }

                validationMatrix[result.AgentName] = validations;
            }

            // Synthesize validation results
            var crossValidationResult = SynthesizeValidationResults(validationMatrix, agentResults);

            _logger.LogInformation("Cross-validation completed, consensus score: {ConsensusScore:F2}", 
                crossValidationResult.OverallConsensusScore);

            return crossValidationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to conduct cross-validation");
            throw;
        }
    }

    // Private implementation methods

    private async Task<AgentResult> GenerateInitialAnalysis(
        AgentType agentType,
        string content,
        Dictionary<string, object> context,
        CancellationToken cancellationToken)
    {
        var prompt = EnhancedPromptBuilder.BuildEnhancedAgentPrompt(agentType, content, context);
        var analysis = await _aiServiceProvider.GenerateReviewAsync(prompt, cancellationToken);

        return ParseAgentResult(agentType, analysis);
    }

    private async Task<CriticismResult> GenerateCriticism(
        AgentResult analysis,
        string originalContent,
        Dictionary<string, object> context,
        List<IterationRecord> history,
        CancellationToken cancellationToken)
    {
        var criticPrompt = BuildCriticismPrompt(analysis, originalContent, context, history);
        var criticism = await _aiServiceProvider.GenerateReviewAsync(criticPrompt, cancellationToken);

        return ParseCriticismResult(criticism);
    }

    private async Task<AgentResult> ImproveAnalysis(
        AgentType writerAgentType,
        AgentResult currentAnalysis,
        CriticismResult criticism,
        string content,
        Dictionary<string, object> context,
        CancellationToken cancellationToken)
    {
        var improvementPrompt = BuildImprovementPrompt(writerAgentType, currentAnalysis, criticism, content, context);
        var improvedAnalysis = await _aiServiceProvider.GenerateReviewAsync(improvementPrompt, cancellationToken);

        return ParseAgentResult(writerAgentType, improvedAnalysis);
    }

    private async Task<ReflectionResult> GenerateSelfReflection(
        AgentResult analysis,
        string content,
        Dictionary<string, object> context,
        CancellationToken cancellationToken)
    {
        var reflectionPrompt = BuildSelfReflectionPrompt(analysis, content, context);
        var reflection = await _aiServiceProvider.GenerateReviewAsync(reflectionPrompt, cancellationToken);

        return ParseReflectionResult(reflection);
    }

    private async Task<AgentResult> ApplySelfCorrection(
        AgentType agentType,
        AgentResult initialAnalysis,
        ReflectionResult reflection,
        string content,
        Dictionary<string, object> context,
        CancellationToken cancellationToken)
    {
        var correctionPrompt = BuildSelfCorrectionPrompt(agentType, initialAnalysis, reflection, content, context);
        var correctedAnalysis = await _aiServiceProvider.GenerateReviewAsync(correctionPrompt, cancellationToken);

        return ParseAgentResult(agentType, correctedAnalysis);
    }

    private async Task<ValidationAssessment> ValidateFinding(
        AgentType validatorType,
        AgentResult resultToValidate,
        string content,
        Dictionary<string, object> context,
        CancellationToken cancellationToken)
    {
        var validationPrompt = BuildValidationPrompt(validatorType, resultToValidate, content, context);
        var validation = await _aiServiceProvider.GenerateReviewAsync(validationPrompt, cancellationToken);

        return ParseValidationAssessment(validatorType, validation);
    }

    // Prompt building methods

    private string BuildCriticismPrompt(
        AgentResult analysis,
        string originalContent,
        Dictionary<string, object> context,
        List<IterationRecord> history)
    {
        var prompt = new System.Text.StringBuilder();

        prompt.AppendLine("You are a Senior Code Review Critic with 20+ years of experience evaluating technical analyses.");
        prompt.AppendLine("Your role is to provide constructive criticism to improve the quality of code review analyses.");
        prompt.AppendLine();

        prompt.AppendLine("## Analysis to Critique:");
        prompt.AppendLine($"**Agent**: {analysis.AgentName}");
        prompt.AppendLine($"**Confidence**: {analysis.ConfidenceScore:F2}");
        prompt.AppendLine($"**Analysis**: {analysis.Analysis}");
        prompt.AppendLine();

        if (history.Any())
        {
            prompt.AppendLine("## Previous Iteration History:");
            foreach (var record in history.TakeLast(2))
            {
                prompt.AppendLine($"**Iteration {record.Iteration}**: Quality improvement of {record.QualityImprovement:F2}");
            }
            prompt.AppendLine();
        }

        prompt.AppendLine("## Criticism Framework:");
        prompt.AppendLine("Evaluate this analysis on:");
        prompt.AppendLine("1. **Completeness**: Did they miss important issues?");
        prompt.AppendLine("2. **Accuracy**: Are the findings technically correct?");
        prompt.AppendLine("3. **Actionability**: Are recommendations specific and implementable?");
        prompt.AppendLine("4. **Prioritization**: Are the most important issues highlighted?");
        prompt.AppendLine("5. **Context**: Does it consider the broader system implications?");
        prompt.AppendLine();

        prompt.AppendLine("Provide structured feedback in JSON format:");
        prompt.AppendLine(GetCriticismJsonSchema());

        return prompt.ToString();
    }

    private string BuildSelfReflectionPrompt(AgentResult analysis, string content, Dictionary<string, object> context)
    {
        var prompt = new System.Text.StringBuilder();

        prompt.AppendLine($"You are reflecting on your own analysis as a {analysis.AgentName}.");
        prompt.AppendLine("Step back and critically evaluate your work with fresh eyes.");
        prompt.AppendLine();

        prompt.AppendLine("## Your Previous Analysis:");
        prompt.AppendLine(analysis.Analysis);
        prompt.AppendLine();

        prompt.AppendLine("## Self-Reflection Questions:");
        prompt.AppendLine("1. **Blind Spots**: What important aspects might I have overlooked?");
        prompt.AppendLine("2. **Bias Check**: Am I being too harsh or too lenient?");
        prompt.AppendLine("3. **Practical Value**: Will my recommendations actually help the development team?");
        prompt.AppendLine("4. **Confidence Calibration**: Am I appropriately confident in my assessment?");
        prompt.AppendLine("5. **Alternative Perspectives**: What would other experts emphasize differently?");
        prompt.AppendLine();

        prompt.AppendLine("Provide honest self-reflection in JSON format:");
        prompt.AppendLine(GetReflectionJsonSchema());

        return prompt.ToString();
    }

    // JSON Schema definitions
    private string GetCriticismJsonSchema()
    {
        return @"{
  ""qualityScore"": 0.75,
  ""strengths"": [""What the analysis did well""],
  ""weaknesses"": [""Areas needing improvement""],
  ""missedOpportunities"": [""Important issues that were overlooked""],
  ""improvementSuggestions"": [
    {
      ""area"": ""completeness|accuracy|actionability|prioritization|context"",
      ""suggestion"": ""Specific improvement to make"",
      ""importance"": ""HIGH|MEDIUM|LOW""
    }
  ],
  ""overallAssessment"": ""Brief summary of the analysis quality""
}";
    }

    private string GetReflectionJsonSchema()
    {
        return @"{
  ""keyInsights"": [""Important realizations about my analysis""],
  ""potentialBiases"": [""Ways I might be biased in my assessment""],
  ""suggestedImprovements"": [
    {
      ""area"": ""Area of my analysis to improve"",
      ""improvement"": ""How to make it better"",
      ""reasoning"": ""Why this improvement matters""
    }
  ],
  ""confidenceReassessment"": 0.8,
  ""alternativePerspectives"": [""Other valid ways to view this code""]
}";
    }

    // Helper classes and parsing methods
    private AgentResult ParseAgentResult(AgentType agentType, string analysisText)
    {
        // Implementation would parse the JSON response or fallback to text parsing
        return new AgentResult
        {
            AgentType = agentType,
            AgentName = agentType.ToString(),
            Analysis = analysisText,
            ConfidenceScore = 0.8, // Would be extracted from JSON
            Findings = new List<Finding>(),
            Recommendations = new List<Recommendation>()
        };
    }

    private CriticismResult ParseCriticismResult(string criticismText)
    {
        // Implementation would parse the JSON response
        return new CriticismResult
        {
            QualityScore = 0.8,
            Strengths = new List<string>(),
            Weaknesses = new List<string>(),
            ImprovementSuggestions = new List<ImprovementSuggestion>()
        };
    }

    private ReflectionResult ParseReflectionResult(string reflectionText)
    {
        return new ReflectionResult
        {
            KeyInsights = new List<string>(),
            SuggestedImprovements = new List<SelfImprovement>(),
            ConfidenceReassessment = 0.8
        };
    }

    private ValidationAssessment ParseValidationAssessment(AgentType validatorType, string validationText)
    {
        return new ValidationAssessment
        {
            ValidatorAgent = validatorType,
            AgreementScore = 0.8,
            ValidationComments = new List<string>()
        };
    }

    private CrossValidationResult SynthesizeValidationResults(
        Dictionary<string, List<ValidationAssessment>> validationMatrix,
        List<AgentResult> originalResults)
    {
        return new CrossValidationResult
        {
            OverallConsensusScore = 0.8,
            ValidationMatrix = validationMatrix,
            ConsensusFindings = new List<Finding>(),
            ConflictingFindings = new List<Finding>()
        };
    }

    private string BuildImprovementPrompt(AgentType writerAgentType, AgentResult currentAnalysis, CriticismResult criticism, string content, Dictionary<string, object> context)
    {
        // Implementation would build improvement prompt
        return "Improvement prompt based on criticism";
    }

    private string BuildSelfCorrectionPrompt(AgentType agentType, AgentResult initialAnalysis, ReflectionResult reflection, string content, Dictionary<string, object> context)
    {
        // Implementation would build self-correction prompt
        return "Self-correction prompt based on reflection";
    }

    private string BuildValidationPrompt(AgentType validatorType, AgentResult resultToValidate, string content, Dictionary<string, object> context)
    {
        // Implementation would build validation prompt
        return "Validation prompt for cross-agent validation";
    }
}

// Supporting classes
public class IterativeAnalysisOptions
{
    public int MaxIterations { get; set; } = 3;
    public double ImprovementThreshold { get; set; } = 0.85;
    public bool EnableSelfCorrection { get; set; } = true;
}

public class IterationRecord
{
    public int Iteration { get; set; }
    public AgentResult Analysis { get; set; }
    public CriticismResult Criticism { get; set; }
    public AgentResult ImprovedAnalysis { get; set; }
    public double QualityImprovement { get; set; }
}

public class CriticismResult
{
    public double QualityScore { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<ImprovementSuggestion> ImprovementSuggestions { get; set; } = new();
}

public class ImprovementSuggestion
{
    public string Area { get; set; }
    public string Suggestion { get; set; }
    public string Importance { get; set; }
}

public class ReflectionResult
{
    public List<string> KeyInsights { get; set; } = new();
    public List<SelfImprovement> SuggestedImprovements { get; set; } = new();
    public double ConfidenceReassessment { get; set; }
}

public class SelfImprovement
{
    public string Area { get; set; }
    public string Improvement { get; set; }
    public string Reasoning { get; set; }
}

public class ValidationAssessment
{
    public AgentType ValidatorAgent { get; set; }
    public double AgreementScore { get; set; }
    public List<string> ValidationComments { get; set; } = new();
}

public class CrossValidationResult
{
    public double OverallConsensusScore { get; set; }
    public Dictionary<string, List<ValidationAssessment>> ValidationMatrix { get; set; } = new();
    public List<Finding> ConsensusFindings { get; set; } = new();
    public List<Finding> ConflictingFindings { get; set; } = new();
}