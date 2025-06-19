using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;
using Mcp.CodeReview.Utilities;
using Mcp.CodeReview.Services;
using Mcp.CodeReview.RAG;

namespace Mcp.CodeReview.AI;

/// <summary>
/// Nested chat framework implementing writer-critic loops for iterative improvement
/// Based on 2024 best practices from AutoGen and other advanced multi-agent systems
/// </summary>
public class NestedChatFramework
{
    private readonly IAIServiceProvider _aiServiceProvider;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly ILogger<NestedChatFramework> _logger;

    public NestedChatFramework(
        IAIServiceProvider aiServiceProvider, 
        IVectorSearchService vectorSearchService,
        ILogger<NestedChatFramework> logger)
    {
        _aiServiceProvider = aiServiceProvider ?? throw new ArgumentNullException(nameof(aiServiceProvider));
        _vectorSearchService = vectorSearchService;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// RAG-Enhanced contextual analysis using repository context, historical patterns, and semantic understanding
    /// </summary>
    public async Task<AgentResult> ConductContextualAnalysis(
        AgentType agentType,
        string content,
        RepositoryContext repositoryContext,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting RAG-enhanced contextual analysis with {AgentType}", agentType);
        
        try
        {
            // Step 1: Gather RAG-enhanced context specific to this agent type
            var ragContext = await GatherRAGEnhancedContext(agentType, content, repositoryContext, cancellationToken);
            
            // Step 2: Build comprehensive contextual prompt with RAG insights
            var contextualPrompt = BuildRAGEnhancedPrompt(agentType, content, repositoryContext, ragContext);
            
            // Step 3: Conduct analysis with full repository and historical context
            var analysis = await _aiServiceProvider.GenerateReviewAsync(contextualPrompt, cancellationToken);
            
            // Step 4: Parse and enhance results with RAG insights
            var result = ParseRAGEnhancedAnalysisResult(agentType, analysis, repositoryContext, ragContext);
            
            _logger.LogInformation("RAG-enhanced analysis completed for {AgentType} with confidence {Confidence} using {PatternCount} historical patterns", 
                agentType, result.ConfidenceScore, ragContext.RelevantPatterns.Count);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG-enhanced contextual analysis failed for {AgentType}", agentType);
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

    // RAG-Enhanced Helper Methods
    
    private async Task<RAGContext> GatherRAGEnhancedContext(
        AgentType agentType, 
        string content, 
        RepositoryContext repositoryContext, 
        CancellationToken cancellationToken)
    {
        var ragContext = new RAGContext
        {
            AgentType = agentType,
            RepositoryContext = repositoryContext
        };
        
        try
        {
            // For now, use the repository context as the RAG enhancement
            // In a full implementation, this would query vector database for relevant patterns
            ragContext.RelevantPatterns = repositoryContext.HistoricalPatterns
                .Where(p => IsPatternRelevantForAgent(p, agentType))
                .Take(10)
                .ToList();
                
            ragContext.ApplicableStandards = repositoryContext.ProjectStandards
                .Where(s => IsStandardRelevantForAgent(s, agentType))
                .Take(8)
                .ToList();
                
            ragContext.SimilarContexts = await FindSimilarContextsAsync(content, agentType, repositoryContext, cancellationToken);
            
            ragContext.HistoricalInsights = ExtractHistoricalInsights(ragContext.RelevantPatterns, agentType);
            
            return ragContext;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not gather complete RAG context for {AgentType}, using fallback", agentType);
            return ragContext; // Return partial context
        }
    }
    
    private string BuildRAGEnhancedPrompt(
        AgentType agentType, 
        string content, 
        RepositoryContext repositoryContext, 
        RAGContext ragContext)
    {
        var prompt = $@"
You are a {agentType} agent conducting a comprehensive, RAG-enhanced code review.

## Code to Review:
```
{content}
```

## Repository Context:
**Project**: {repositoryContext.ProjectName}
**Architecture**: {repositoryContext.Structure.ArchitecturePattern}
**Technology Stack**: {string.Join(", ", repositoryContext.Dependencies.Keys.Take(5))}
**File Types**: {string.Join(", ", repositoryContext.Structure.FilesByType.Keys)}

## Historical Patterns (RAG-Enhanced):
{string.Join("\n", ragContext.RelevantPatterns.Take(5).Select(p => $"- **{p.Category}**: {p.Pattern} (confidence: {p.Similarity:F2})"))}

## Applicable Standards:
{string.Join("\n", ragContext.ApplicableStandards.Take(5).Select(s => $"- **{s.Category}**: {s.Title} - {s.Description}"))}

## Similar Context Analysis:
{string.Join("\n", ragContext.SimilarContexts.Take(3).Select(sc => $"- {sc}"))}

## Historical Insights for {agentType}:
{string.Join("\n", ragContext.HistoricalInsights.Take(3).Select(hi => $"- {hi}"))}

## Team Patterns:
**Preferred Patterns**: {string.Join(", ", repositoryContext.TeamPatterns.PreferredPatterns.Take(3))}
**Avoided Patterns**: {string.Join(", ", repositoryContext.TeamPatterns.AvoidedPatterns.Take(3))}

## Semantic Analysis Results:
{FormatSemanticAnalysis(repositoryContext.SemanticAnalysis)}

## Your RAG-Enhanced Analysis Task:
As a {agentType} specialist with access to historical patterns and contextual knowledge:

1. **Contextual Analysis**: Analyze this code within the broader repository context
2. **Pattern Matching**: Compare against historical patterns and similar contexts
3. **Standard Compliance**: Evaluate adherence to applicable coding standards
4. **Risk Assessment**: Consider discovered risk factors and dependency insights
5. **Recommendation Synthesis**: Provide recommendations based on RAG insights

Focus specifically on {agentType} concerns while leveraging the full contextual knowledge.

Format your response as:
**Findings:**
- [Specific finding with RAG context reference]

**Recommendations:**
- [RAG-informed actionable recommendation]

**Confidence:** [0.0-1.0 based on RAG context availability]
**RAG Insights Used:** [Count of patterns/standards referenced]
";
        
        return prompt;
    }
    
    private AgentResult ParseRAGEnhancedAnalysisResult(
        AgentType agentType, 
        string analysis, 
        RepositoryContext repositoryContext, 
        RAGContext ragContext)
    {
        var result = new AgentResult
        {
            AgentType = agentType,
            AgentName = agentType.ToString(),
            Analysis = analysis,
            IsSuccessful = true,
            ProcessingTime = TimeSpan.FromSeconds(3), // RAG analysis takes longer
            Findings = new List<Finding>(),
            Recommendations = new List<Recommendation>(),
            ConfidenceScore = 0.8,
            Metadata = new Dictionary<string, object>
            {
                ["ragPatternsUsed"] = ragContext.RelevantPatterns.Count,
                ["ragStandardsUsed"] = ragContext.ApplicableStandards.Count,
                ["ragSimilarContexts"] = ragContext.SimilarContexts.Count,
                ["repositoryContextAvailable"] = true
            }
        };

        // Enhanced parsing with RAG context
        var findingsMatch = System.Text.RegularExpressions.Regex.Match(analysis, @"\*\*Findings:\*\*(.*?)\*\*Recommendations:", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (findingsMatch.Success)
        {
            var findings = findingsMatch.Groups[1].Value.Split('\n')
                .Where(line => line.Trim().StartsWith("-"))
                .Select(line => line.Trim().TrimStart('-').Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => new Finding 
                { 
                    Description = line,
                    Category = DetermineFindingCategory(line, agentType),
                    Severity = DetermineFindingSeverity(line, ragContext)
                })
                .ToList();
            
            result.Findings.AddRange(findings);
        }

        var recommendationsMatch = System.Text.RegularExpressions.Regex.Match(analysis, @"\*\*Recommendations:\*\*(.*?)\*\*Confidence:", System.Text.RegularExpressions.RegexOptions.Singleline);
        if (recommendationsMatch.Success)
        {
            var recommendations = recommendationsMatch.Groups[1].Value.Split('\n')
                .Where(line => line.Trim().StartsWith("-"))
                .Select(line => line.Trim().TrimStart('-').Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => new Recommendation 
                { 
                    Title = line,
                    Priority = DetermineRecommendationPriority(line, ragContext),
                    Category = agentType.ToString()
                })
                .ToList();
            
            result.Recommendations.AddRange(recommendations);
        }

        // Parse RAG-enhanced confidence
        var confidenceMatch = System.Text.RegularExpressions.Regex.Match(analysis, @"\*\*Confidence:\*\*\s*(\d*\.?\d+)");
        if (confidenceMatch.Success && double.TryParse(confidenceMatch.Groups[1].Value, out var confidence))
        {
            result.ConfidenceScore = confidence;
        }
        
        // Parse RAG insights count
        var ragInsightsMatch = System.Text.RegularExpressions.Regex.Match(analysis, @"\*\*RAG Insights Used:\*\*\s*(\d+)");
        if (ragInsightsMatch.Success && int.TryParse(ragInsightsMatch.Groups[1].Value, out var ragCount))
        {
            result.Metadata["actualRagInsightsUsed"] = ragCount;
        }

        return result;
    }
    
    // RAG Helper Methods
    
    private bool IsPatternRelevantForAgent(HistoricalPattern pattern, AgentType agentType)
    {
        var agentConcerns = agentType switch
        {
            AgentType.SecurityExpert => new[] { "security", "vulnerability", "authentication", "authorization" },
            AgentType.PerformanceAnalyst => new[] { "performance", "optimization", "scalability", "efficiency" },
            AgentType.CodeQualityReviewer => new[] { "quality", "maintainability", "readability", "complexity" },
            AgentType.ArchitectureExpert => new[] { "architecture", "design", "pattern", "structure" },
            AgentType.TestingSpecialist => new[] { "test", "testing", "coverage", "validation" },
            _ => new[] { "general", "best practice", "guideline" }
        };
        
        return agentConcerns.Any(concern => pattern.Pattern.Contains(concern, StringComparison.OrdinalIgnoreCase) ||
                                           pattern.Category.Contains(concern, StringComparison.OrdinalIgnoreCase));
    }
    
    private bool IsStandardRelevantForAgent(CodingStandard standard, AgentType agentType)
    {
        var agentCategories = agentType switch
        {
            AgentType.SecurityExpert => new[] { "Security", "Authentication", "Authorization" },
            AgentType.PerformanceAnalyst => new[] { "Performance", "Optimization" },
            AgentType.CodeQualityReviewer => new[] { "Quality", "Conventions", "General" },
            AgentType.ArchitectureExpert => new[] { "Architecture", "Design", "Patterns" },
            AgentType.TestingSpecialist => new[] { "Testing", "Quality" },
            _ => new[] { "General" }
        };
        
        return agentCategories.Contains(standard.Category) || standard.Priority <= 2;
    }
    
    private async Task<List<string>> FindSimilarContextsAsync(
        string content, 
        AgentType agentType, 
        RepositoryContext repositoryContext, 
        CancellationToken cancellationToken)
    {
        var similarContexts = new List<string>();
        
        // Simulate similar context discovery based on repository context
        if (repositoryContext.RelatedFiles.Any())
        {
            similarContexts.Add($"Similar {agentType} patterns found in {repositoryContext.RelatedFiles.Count} related files");
        }
        
        if (repositoryContext.Dependencies.Any())
        {
            var relevantDeps = repositoryContext.Dependencies.Keys
                .Where(dep => IsRelevantForAgent(dep, agentType))
                .Take(3);
            if (relevantDeps.Any())
            {
                similarContexts.Add($"Similar technology contexts: {string.Join(", ", relevantDeps)}");
            }
        }
        
        if (repositoryContext.Structure.ArchitecturePattern != null)
        {
            similarContexts.Add($"Architecture context: {repositoryContext.Structure.ArchitecturePattern} pattern analysis");
        }
        
        return similarContexts;
    }
    
    private List<string> ExtractHistoricalInsights(List<HistoricalPattern> patterns, AgentType agentType)
    {
        var insights = new List<string>();
        
        var relevantPatterns = patterns.Where(p => IsPatternRelevantForAgent(p, agentType)).ToList();
        
        if (relevantPatterns.Any())
        {
            var avgSimilarity = relevantPatterns.Average(p => p.Similarity);
            insights.Add($"Historical pattern confidence: {avgSimilarity:F2} across {relevantPatterns.Count} similar cases");
            
            var topRecommendation = relevantPatterns
                .OrderByDescending(p => p.Similarity)
                .FirstOrDefault()?.Recommendation;
            if (!string.IsNullOrEmpty(topRecommendation))
            {
                insights.Add($"Top historical recommendation: {topRecommendation}");
            }
            
            var commonCategory = relevantPatterns
                .GroupBy(p => p.Category)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;
            if (!string.IsNullOrEmpty(commonCategory))
            {
                insights.Add($"Most common historical category: {commonCategory}");
            }
        }
        
        return insights;
    }
    
    private string FormatSemanticAnalysis(Dictionary<string, object> semanticAnalysis)
    {
        if (!semanticAnalysis.Any())
            return "No semantic analysis available";
            
        var formatted = new List<string>();
        foreach (var item in semanticAnalysis.Take(3))
        {
            formatted.Add($"- {item.Key}: {item.Value}");
        }
        
        return string.Join("\n", formatted);
    }
    
    private string DetermineFindingCategory(string finding, AgentType agentType)
    {
        return agentType switch
        {
            AgentType.SecurityExpert => "Security",
            AgentType.PerformanceAnalyst => "Performance",
            AgentType.CodeQualityReviewer => "Code Quality",
            AgentType.ArchitectureExpert => "Architecture",
            AgentType.TestingSpecialist => "Testing",
            _ => "General"
        };
    }
    
    private string DetermineFindingSeverity(string finding, RAGContext ragContext)
    {
        var lowerFinding = finding.ToLowerInvariant();
        
        if (lowerFinding.Contains("critical") || lowerFinding.Contains("security") || lowerFinding.Contains("vulnerability"))
            return "High";
        if (lowerFinding.Contains("performance") || lowerFinding.Contains("optimization"))
            return "Medium";
        if (ragContext.RelevantPatterns.Any(p => p.Similarity > 0.8))
            return "Medium";
            
        return "Low";
    }
    
    private string DetermineRecommendationPriority(string recommendation, RAGContext ragContext)
    {
        var lowerRec = recommendation.ToLowerInvariant();
        
        if (lowerRec.Contains("immediately") || lowerRec.Contains("critical") || lowerRec.Contains("security"))
            return "High";
        if (ragContext.ApplicableStandards.Any(s => s.Priority <= 2))
            return "High";
        if (lowerRec.Contains("should") || lowerRec.Contains("recommend"))
            return "Medium";
            
        return "Low";
    }
    
    private bool IsRelevantForAgent(string dependency, AgentType agentType)
    {
        var lowerDep = dependency.ToLowerInvariant();
        
        return agentType switch
        {
            AgentType.SecurityExpert => lowerDep.Contains("security") || lowerDep.Contains("auth") || lowerDep.Contains("crypto"),
            AgentType.PerformanceAnalyst => lowerDep.Contains("cache") || lowerDep.Contains("memory") || lowerDep.Contains("performance"),
            AgentType.TestingSpecialist => lowerDep.Contains("test") || lowerDep.Contains("mock") || lowerDep.Contains("spec"),
            _ => true
        };
    }

    /// <summary>
    /// Semantic search for similar issues and solutions with comprehensive context matching
    /// </summary>
    public async Task<SemanticSearchResult> SearchSimilarIssuesAndSolutionsAsync(
        string issueDescription,
        string codeContext,
        AgentType[] interestedAgents,
        string projectId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting semantic search for similar issues and solutions");

        if (_vectorSearchService == null)
        {
            _logger.LogWarning("Vector search service not available, returning empty results");
            return new SemanticSearchResult();
        }

        try
        {
            var searchResult = new SemanticSearchResult
            {
                Query = issueDescription,
                SearchTimestamp = DateTime.UtcNow
            };

            // 1. Search for similar historical issues
            var historicalIssues = await _vectorSearchService.SearchSimilarIssuesAndSolutionsAsync(
                issueDescription, 
                ExtractErrorMessage(issueDescription),
                codeContext,
                topK: 10,
                cancellationToken);

            searchResult.SimilarIssues = historicalIssues.Select(issue => new SimilarIssue
            {
                Id = issue.Id,
                Description = ExtractIssueDescription(issue.Content),
                Solution = ExtractSolution(issue.Content),
                Similarity = issue.Similarity,
                ProjectId = issue.Metadata.GetValueOrDefault("project_id")?.ToString() ?? "",
                ResolvedAt = ParseResolvedDate(issue.Metadata),
                Tags = ExtractTags(issue.Metadata),
                ComplexityLevel = issue.Metadata.GetValueOrDefault("complexity")?.ToString() ?? "Unknown"
            }).ToList();

            // 2. Search for relevant code patterns for each agent type
            foreach (var agentType in interestedAgents)
            {
                var agentSpecificQuery = BuildAgentSpecificQuery(issueDescription, codeContext, agentType);
                var patterns = await _vectorSearchService.SearchSimilarCodeAsync(
                    agentSpecificQuery,
                    projectId,
                    topK: 8,
                    cancellationToken);

                searchResult.RelevantPatterns[agentType] = patterns.Select(p => new SemanticPattern
                {
                    Id = p.Id,
                    Pattern = p.Content,
                    Similarity = p.Similarity,
                    Category = p.Metadata.GetValueOrDefault("category")?.ToString() ?? agentType.ToString(),
                    AgentRelevance = agentType,
                    Confidence = p.Similarity,
                    ContextualAdvice = GenerateContextualAdvice(p, agentType, issueDescription)
                }).ToList();
            }

            // 3. Search for coding standards violations or compliance patterns
            var standardsQuery = BuildStandardsQuery(issueDescription, codeContext);
            var standards = await _vectorSearchService.SearchCodingStandardsAsync(
                standardsQuery,
                null,
                6,
                cancellationToken);

            searchResult.ApplicableStandards = standards.Select(s => new ApplicableStandard
            {
                Id = s.Id,
                Title = s.Metadata.GetValueOrDefault("title")?.ToString() ?? "Untitled Standard",
                Description = s.Content,
                Similarity = s.Similarity,
                Category = s.Metadata.GetValueOrDefault("category")?.ToString() ?? "General",
                Priority = int.TryParse(s.Metadata.GetValueOrDefault("priority")?.ToString(), out var p) ? p : 3,
                Recommendation = s.Metadata.GetValueOrDefault("recommendation")?.ToString() ?? ""
            }).ToList();

            // 4. Cross-reference analysis to find patterns across different data sources
            searchResult.CrossReferences = await PerformCrossReferenceAnalysis(
                searchResult.SimilarIssues, 
                searchResult.RelevantPatterns, 
                searchResult.ApplicableStandards,
                cancellationToken);

            // 5. Generate confidence and relevance scores
            searchResult.OverallConfidence = CalculateOverallConfidence(searchResult);
            searchResult.SearchQuality = AssessSearchQuality(searchResult);

            _logger.LogInformation("Semantic search completed: {IssueCount} similar issues, {PatternCount} patterns, {StandardCount} standards found",
                searchResult.SimilarIssues.Count,
                searchResult.RelevantPatterns.Values.Sum(patterns => patterns.Count),
                searchResult.ApplicableStandards.Count);

            return searchResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform semantic search for similar issues and solutions");
            return new SemanticSearchResult
            {
                Query = issueDescription,
                SearchTimestamp = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Advanced semantic analysis combining multiple search strategies
    /// </summary>
    public async Task<AdvancedSemanticAnalysis> ConductAdvancedSemanticAnalysisAsync(
        string codeSnippet,
        string context,
        RepositoryContext repositoryContext,
        AgentType[] analysisAgents,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Conducting advanced semantic analysis with {AgentCount} agents", analysisAgents.Length);

        var analysis = new AdvancedSemanticAnalysis
        {
            CodeSnippet = codeSnippet,
            Context = context,
            AnalysisTimestamp = DateTime.UtcNow
        };

        if (_vectorSearchService == null)
        {
            _logger.LogWarning("Vector search service not available for advanced semantic analysis");
            return analysis;
        }

        try
        {
            // 1. Multi-dimensional semantic search
            var semanticDimensions = await PerformMultiDimensionalSearch(
                codeSnippet, context, repositoryContext, cancellationToken);
            analysis.SemanticDimensions = semanticDimensions;

            // 2. Agent-specific deep analysis
            foreach (var agent in analysisAgents)
            {
                var agentAnalysis = await PerformAgentSpecificSemanticAnalysis(
                    agent, codeSnippet, context, repositoryContext, cancellationToken);
                analysis.AgentSpecificInsights[agent] = agentAnalysis;
            }

            // 3. Pattern evolution analysis
            analysis.PatternEvolution = await AnalyzePatternEvolution(
                codeSnippet, repositoryContext, cancellationToken);

            // 4. Risk correlation analysis
            analysis.RiskCorrelations = await AnalyzeRiskCorrelations(
                codeSnippet, context, repositoryContext, cancellationToken);

            // 5. Solution recommendation synthesis
            analysis.SolutionRecommendations = await SynthesizeSolutionRecommendations(
                analysis, repositoryContext, cancellationToken);

            // 6. Confidence calibration
            analysis.OverallConfidence = CalibrateSemanticConfidence(analysis);

            _logger.LogInformation("Advanced semantic analysis completed with {DimensionCount} dimensions and {InsightCount} agent insights",
                analysis.SemanticDimensions.Count,
                analysis.AgentSpecificInsights.Count);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to conduct advanced semantic analysis");
            analysis.ErrorMessage = ex.Message;
            return analysis;
        }
    }

    /// <summary>
    /// Store and learn from resolved issues to improve future searches
    /// </summary>
    public async Task StoreResolvedIssueWithLearningAsync(
        string issueTitle,
        string issueDescription,
        string solution,
        string codeContext,
        AgentType[] involvedAgents,
        Dictionary<string, object> resolutionMetadata,
        string projectId,
        CancellationToken cancellationToken = default)
    {
        if (_vectorSearchService == null)
        {
            _logger.LogWarning("Vector search service not available for storing resolved issue");
            return;
        }

        try
        {
            // Extract learning patterns from the resolution
            var learningPatterns = ExtractLearningPatterns(
                issueDescription, solution, codeContext, involvedAgents, resolutionMetadata);

            // Store the resolved issue
            await _vectorSearchService.StoreResolvedIssueAsync(
                projectId,
                issueTitle,
                issueDescription,
                solution,
                codeContext,
                ExtractTags(resolutionMetadata),
                cancellationToken);

            // Update agent-specific knowledge
            foreach (var agent in involvedAgents)
            {
                await UpdateAgentSpecificKnowledge(
                    agent, issueDescription, solution, learningPatterns, projectId, cancellationToken);
            }

            // Update team patterns based on the resolution approach
            var teamPatterns = ExtractTeamPatterns(solution, resolutionMetadata);
            if (teamPatterns.preferredPatterns.Any() || teamPatterns.avoidedPatterns.Any())
            {
                await _vectorSearchService.UpdateTeamPatternsAsync(
                    projectId,
                    teamPatterns.preferredPatterns,
                    teamPatterns.avoidedPatterns,
                    teamPatterns.reasoning,
                    cancellationToken);
            }

            _logger.LogInformation("Successfully stored resolved issue '{IssueTitle}' with learning patterns for {AgentCount} agents",
                issueTitle, involvedAgents.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store resolved issue with learning: {IssueTitle}", issueTitle);
            throw;
        }
    }

    // Private helper methods for semantic search

    private string ExtractErrorMessage(string issueDescription)
    {
        // Extract error messages from issue descriptions using regex patterns
        var errorPatterns = new[]
        {
            @"Error:\s*(.+?)(?:\n|$)",
            @"Exception:\s*(.+?)(?:\n|$)",
            @"ERROR\s*:\s*(.+?)(?:\n|$)",
            @"Failed:\s*(.+?)(?:\n|$)"
        };

        foreach (var pattern in errorPatterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(issueDescription, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();
        }

        return null;
    }

    private string ExtractIssueDescription(string content)
    {
        // Extract issue description from stored content
        var lines = content.Split('\n');
        var descriptionLine = lines.FirstOrDefault(l => l.StartsWith("Description:"));
        return descriptionLine?.Substring("Description:".Length).Trim() ?? content.Split('\n').FirstOrDefault() ?? "";
    }

    private string ExtractSolution(string content)
    {
        var lines = content.Split('\n');
        var solutionStart = Array.FindIndex(lines, l => l.StartsWith("Solution:"));
        if (solutionStart >= 0 && solutionStart < lines.Length - 1)
        {
            var solutionLines = lines.Skip(solutionStart + 1)
                .TakeWhile(l => !l.StartsWith("Code Context:") && !string.IsNullOrWhiteSpace(l));
            return string.Join(" ", solutionLines).Trim();
        }
        return "";
    }

    private DateTime ParseResolvedDate(Dictionary<string, object> metadata)
    {
        if (metadata.TryGetValue("resolved_at", out var resolvedAt) && 
            DateTime.TryParse(resolvedAt.ToString(), out var parsed))
        {
            return parsed;
        }
        return DateTime.UtcNow;
    }

    private List<string> ExtractTags(Dictionary<string, object> metadata)
    {
        if (metadata.TryGetValue("tags", out var tagsObj))
        {
            var tagsString = tagsObj.ToString();
            return string.IsNullOrEmpty(tagsString) ? new List<string>() : 
                   tagsString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(t => t.Trim())
                           .ToList();
        }
        return new List<string>();
    }

    private string BuildAgentSpecificQuery(string issueDescription, string codeContext, AgentType agentType)
    {
        var agentFocus = agentType switch
        {
            AgentType.SecurityExpert => "security vulnerability authentication authorization encryption",
            AgentType.PerformanceAnalyst => "performance optimization scalability memory cpu efficiency",
            AgentType.CodeQualityReviewer => "code quality maintainability readability complexity refactoring",
            AgentType.ArchitectureExpert => "architecture design pattern structure SOLID principles",
            AgentType.TestingSpecialist => "testing unit test integration test coverage validation",
            _ => "best practices patterns guidelines"
        };

        return $"{issueDescription} {agentFocus} {codeContext}".Trim();
    }

    private string GenerateContextualAdvice(RetrievedContext pattern, AgentType agentType, string issueDescription)
    {
        var baseAdvice = pattern.Metadata.GetValueOrDefault("advice")?.ToString() ?? "Consider this pattern for your implementation";
        var agentSpecificAdvice = agentType switch
        {
            AgentType.SecurityExpert => "Review for security implications and potential vulnerabilities",
            AgentType.PerformanceAnalyst => "Analyze performance impact and optimization opportunities",
            AgentType.CodeQualityReviewer => "Evaluate maintainability and code quality improvements",
            AgentType.ArchitectureExpert => "Consider architectural alignment and design principles",
            AgentType.TestingSpecialist => "Ensure adequate test coverage and validation strategies",
            _ => "Apply general best practices"
        };

        return $"{baseAdvice}. {agentSpecificAdvice}.";
    }

    private string BuildStandardsQuery(string issueDescription, string codeContext)
    {
        return $"coding standards best practices guidelines {issueDescription} {codeContext}".Trim();
    }

    private async Task<List<CrossReference>> PerformCrossReferenceAnalysis(
        List<SimilarIssue> issues,
        Dictionary<AgentType, List<SemanticPattern>> patterns,
        List<ApplicableStandard> standards,
        CancellationToken cancellationToken)
    {
        var crossRefs = new List<CrossReference>();

        // Find issues that relate to specific patterns
        foreach (var issue in issues.Take(5))
        {
            foreach (var agentPatterns in patterns.Take(3))
            {
                var relevantPatterns = agentPatterns.Value.Where(p => 
                    CalculateTextSimilarity(issue.Description, p.Pattern) > 0.3).ToList();

                if (relevantPatterns.Any())
                {
                    crossRefs.Add(new CrossReference
                    {
                        Type = "Issue-Pattern",
                        SourceId = issue.Id,
                        TargetId = relevantPatterns.First().Id,
                        Relationship = $"Issue relates to {agentPatterns.Key} pattern",
                        Confidence = relevantPatterns.First().Similarity,
                        Reasoning = $"Similar context found in {agentPatterns.Key} analysis"
                    });
                }
            }
        }

        // Find standards that relate to common issue patterns
        foreach (var standard in standards.Take(3))
        {
            var relatedIssues = issues.Where(i => 
                CalculateTextSimilarity(i.Description, standard.Description) > 0.25).ToList();

            if (relatedIssues.Any())
            {
                crossRefs.Add(new CrossReference
                {
                    Type = "Standard-Issue",
                    SourceId = standard.Id,
                    TargetId = relatedIssues.First().Id,
                    Relationship = "Standard addresses common issue pattern",
                    Confidence = standard.Similarity,
                    Reasoning = $"Standard {standard.Title} provides guidance for similar issues"
                });
            }
        }

        return crossRefs;
    }

    private double CalculateTextSimilarity(string text1, string text2)
    {
        if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            return 0.0;

        var words1 = text1.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var words2 = text2.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();

        return union > 0 ? (double)intersection / union : 0.0;
    }

    private double CalculateOverallConfidence(SemanticSearchResult result)
    {
        var factors = new List<double>();

        // Issue similarity confidence
        if (result.SimilarIssues.Any())
            factors.Add(result.SimilarIssues.Average(i => i.Similarity));

        // Pattern relevance confidence
        if (result.RelevantPatterns.Any())
            factors.Add(result.RelevantPatterns.Values.SelectMany(p => p).Average(p => p.Similarity));

        // Standards applicability confidence
        if (result.ApplicableStandards.Any())
            factors.Add(result.ApplicableStandards.Average(s => s.Similarity));

        // Cross-reference confidence
        if (result.CrossReferences.Any())
            factors.Add(result.CrossReferences.Average(cr => cr.Confidence));

        return factors.Any() ? factors.Average() : 0.0;
    }

    private SearchQuality AssessSearchQuality(SemanticSearchResult result)
    {
        var totalResults = result.SimilarIssues.Count + 
                          result.RelevantPatterns.Values.Sum(p => p.Count) + 
                          result.ApplicableStandards.Count;

        var highConfidenceResults = result.SimilarIssues.Count(i => i.Similarity > 0.8) +
                                   result.RelevantPatterns.Values.SelectMany(p => p).Count(p => p.Similarity > 0.8) +
                                   result.ApplicableStandards.Count(s => s.Similarity > 0.8);

        if (totalResults == 0)
            return SearchQuality.NoResults;
        if (highConfidenceResults >= 3)
            return SearchQuality.Excellent;
        if (totalResults >= 5)
            return SearchQuality.Good;
        if (totalResults >= 2)
            return SearchQuality.Fair;

        return SearchQuality.Poor;
    }

    // Additional helper methods for advanced semantic analysis (placeholder implementations)
    private async Task<Dictionary<string, SemanticDimension>> PerformMultiDimensionalSearch(
        string codeSnippet, string context, RepositoryContext repositoryContext, CancellationToken cancellationToken)
    {
        // Implementation would perform multi-dimensional semantic analysis
        await Task.Delay(1, cancellationToken); // Placeholder
        return new Dictionary<string, SemanticDimension>();
    }

    private async Task<AgentSpecificInsight> PerformAgentSpecificSemanticAnalysis(
        AgentType agent, string codeSnippet, string context, RepositoryContext repositoryContext, CancellationToken cancellationToken)
    {
        // Implementation would perform agent-specific semantic analysis
        await Task.Delay(1, cancellationToken); // Placeholder
        return new AgentSpecificInsight { Agent = agent };
    }

    private async Task<PatternEvolution> AnalyzePatternEvolution(
        string codeSnippet, RepositoryContext repositoryContext, CancellationToken cancellationToken)
    {
        // Implementation would analyze how patterns have evolved
        await Task.Delay(1, cancellationToken); // Placeholder
        return new PatternEvolution();
    }

    private async Task<List<RiskCorrelation>> AnalyzeRiskCorrelations(
        string codeSnippet, string context, RepositoryContext repositoryContext, CancellationToken cancellationToken)
    {
        // Implementation would analyze risk correlations
        await Task.Delay(1, cancellationToken); // Placeholder
        return new List<RiskCorrelation>();
    }

    private async Task<List<SolutionRecommendation>> SynthesizeSolutionRecommendations(
        AdvancedSemanticAnalysis analysis, RepositoryContext repositoryContext, CancellationToken cancellationToken)
    {
        // Implementation would synthesize solution recommendations
        await Task.Delay(1, cancellationToken); // Placeholder
        return new List<SolutionRecommendation>();
    }

    private double CalibrateSemanticConfidence(AdvancedSemanticAnalysis analysis)
    {
        // Implementation would calibrate confidence based on various factors
        return 0.8;
    }

    private List<LearningPattern> ExtractLearningPatterns(
        string issueDescription, string solution, string codeContext, 
        AgentType[] involvedAgents, Dictionary<string, object> resolutionMetadata)
    {
        // Implementation would extract learning patterns from resolution
        return new List<LearningPattern>();
    }

    private async Task UpdateAgentSpecificKnowledge(
        AgentType agent, string issueDescription, string solution, 
        List<LearningPattern> learningPatterns, string projectId, CancellationToken cancellationToken)
    {
        // Implementation would update agent-specific knowledge
        await Task.Delay(1, cancellationToken); // Placeholder
    }

    private (List<string> preferredPatterns, List<string> avoidedPatterns, Dictionary<string, string> reasoning) 
        ExtractTeamPatterns(string solution, Dictionary<string, object> resolutionMetadata)
    {
        // Implementation would extract team patterns from solution
        return (new List<string>(), new List<string>(), new Dictionary<string, string>());
    }
}

/// <summary>
/// RAG context for enhanced agent analysis
/// </summary>
public class RAGContext
{
    public AgentType AgentType { get; set; }
    public RepositoryContext RepositoryContext { get; set; } = new();
    public List<HistoricalPattern> RelevantPatterns { get; set; } = new();
    public List<CodingStandard> ApplicableStandards { get; set; } = new();
    public List<string> SimilarContexts { get; set; } = new();
    public List<string> HistoricalInsights { get; set; } = new();
    public Dictionary<string, object> EnhancedMetadata { get; set; } = new();
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

// Semantic Search Model Classes

/// <summary>
/// Result of semantic search for similar issues and solutions
/// </summary>
public class SemanticSearchResult
{
    public string Query { get; set; } = string.Empty;
    public DateTime SearchTimestamp { get; set; }
    public List<SimilarIssue> SimilarIssues { get; set; } = new();
    public Dictionary<AgentType, List<SemanticPattern>> RelevantPatterns { get; set; } = new();
    public List<ApplicableStandard> ApplicableStandards { get; set; } = new();
    public List<CrossReference> CrossReferences { get; set; } = new();
    public double OverallConfidence { get; set; }
    public SearchQuality SearchQuality { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Similar issue found in semantic search
/// </summary>
public class SimilarIssue
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public double Similarity { get; set; }
    public string ProjectId { get; set; } = string.Empty;
    public DateTime ResolvedAt { get; set; }
    public List<string> Tags { get; set; } = new();
    public string ComplexityLevel { get; set; } = string.Empty;
}

/// <summary>
/// Semantic pattern relevant to specific agents
/// </summary>
public class SemanticPattern
{
    public string Id { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public double Similarity { get; set; }
    public string Category { get; set; } = string.Empty;
    public AgentType AgentRelevance { get; set; }
    public double Confidence { get; set; }
    public string ContextualAdvice { get; set; } = string.Empty;
}

/// <summary>
/// Applicable coding standard found in search
/// </summary>
public class ApplicableStandard
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Similarity { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// Cross-reference between different semantic entities
/// </summary>
public class CrossReference
{
    public string Type { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// Advanced semantic analysis result
/// </summary>
public class AdvancedSemanticAnalysis
{
    public string CodeSnippet { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public DateTime AnalysisTimestamp { get; set; }
    public Dictionary<string, SemanticDimension> SemanticDimensions { get; set; } = new();
    public Dictionary<AgentType, AgentSpecificInsight> AgentSpecificInsights { get; set; } = new();
    public PatternEvolution PatternEvolution { get; set; } = new();
    public List<RiskCorrelation> RiskCorrelations { get; set; } = new();
    public List<SolutionRecommendation> SolutionRecommendations { get; set; } = new();
    public double OverallConfidence { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Semantic dimension of analysis
/// </summary>
public class SemanticDimension
{
    public string Name { get; set; } = string.Empty;
    public double Score { get; set; }
    public List<string> KeyFeatures { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Agent-specific insight from semantic analysis
/// </summary>
public class AgentSpecificInsight
{
    public AgentType Agent { get; set; }
    public List<string> KeyInsights { get; set; } = new();
    public double RelevanceScore { get; set; }
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object> SpecializedData { get; set; } = new();
}

/// <summary>
/// Pattern evolution analysis
/// </summary>
public class PatternEvolution
{
    public string PatternName { get; set; } = string.Empty;
    public List<EvolutionStage> EvolutionStages { get; set; } = new();
    public string CurrentStage { get; set; } = string.Empty;
    public List<string> FutureTrends { get; set; } = new();
}

/// <summary>
/// Evolution stage in pattern analysis
/// </summary>
public class EvolutionStage
{
    public string StageName { get; set; } = string.Empty;
    public DateTime TimeFrame { get; set; }
    public string Description { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

/// <summary>
/// Risk correlation between code elements
/// </summary>
public class RiskCorrelation
{
    public string RiskType { get; set; } = string.Empty;
    public string CorrelatedElement { get; set; } = string.Empty;
    public double CorrelationStrength { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string MitigationStrategy { get; set; } = string.Empty;
}

/// <summary>
/// Solution recommendation from semantic analysis
/// </summary>
public class SolutionRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public List<string> Steps { get; set; } = new();
    public string Rationale { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
}

/// <summary>
/// Learning pattern extracted from resolution
/// </summary>
public class LearningPattern
{
    public string PatternType { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string Solution { get; set; } = string.Empty;
    public List<AgentType> RelevantAgents { get; set; } = new();
    public double Effectiveness { get; set; }
}

/// <summary>
/// Quality assessment of semantic search
/// </summary>
public enum SearchQuality
{
    NoResults,
    Poor,
    Fair,
    Good,
    Excellent
}