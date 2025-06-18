using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.AI.Advanced
{
    /// <summary>
    /// Lead orchestrator agent implementing strategic analysis and result synthesis
    /// Acts as the "conductor" in hierarchical multi-agent orchestration
    /// </summary>
    public class LeadOrchestratorAgent : ILeadOrchestratorAgent
    {
        private readonly IClaudeService _claudeService;
        private readonly AdvancedReasoningEngine _reasoningEngine;
        private readonly ILogger<LeadOrchestratorAgent> _logger;

        public LeadOrchestratorAgent(
            IClaudeService claudeService,
            AdvancedReasoningEngine reasoningEngine,
            ILogger<LeadOrchestratorAgent> logger)
        {
            _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
            _reasoningEngine = reasoningEngine ?? throw new ArgumentNullException(nameof(reasoningEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Conducts strategic analysis to guide specialized agents
        /// </summary>
        public async Task<StrategicAnalysis> ConductStrategicAnalysisAsync(
            CodeReviewRequest request,
            EnhancedContext context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Conducting strategic analysis for {FileName}", request.FileName);

            try
            {
                // Phase 1: High-level code analysis
                var overviewAnalysis = await ConductCodeOverviewAsync(request, context, cancellationToken);
                
                // Phase 2: Risk assessment
                var riskAssessment = await ConductRiskAssessmentAsync(request, context, cancellationToken);
                
                // Phase 3: Agent coordination strategy
                var coordinationStrategy = await DevelopCoordinationStrategyAsync(request, context, overviewAnalysis, cancellationToken);
                
                // Phase 4: Quality gates definition
                var qualityGates = DefineQualityGates(context, riskAssessment);

                return new StrategicAnalysis
                {
                    OverallStrategy = overviewAnalysis.Strategy,
                    KeyFocusAreas = overviewAnalysis.FocusAreas,
                    AgentInstructions = coordinationStrategy.Instructions,
                    ComplexityAssessment = context.CodeComplexity,
                    PotentialRisks = riskAssessment.Risks,
                    EstimatedAnalysisTime = coordinationStrategy.EstimatedTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Strategic analysis failed for {FileName}", request.FileName);
                return CreateFallbackStrategy(request, context);
            }
        }

        /// <summary>
        /// Synthesizes results from all specialized agents
        /// </summary>
        public async Task<SynthesisResult> SynthesizeResultsAsync(
            SpecializedAgentResult[] results,
            StrategicAnalysis strategicAnalysis,
            EnhancedContext context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Synthesizing results from {AgentCount} specialized agents", results.Length);

            try
            {
                // Phase 1: Cross-agent validation
                var validation = await ValidateCrossAgentConsistency(results, cancellationToken);
                
                // Phase 2: Priority ranking of findings
                var prioritizedFindings = await PrioritizeFindings(results, strategicAnalysis, cancellationToken);
                
                // Phase 3: Unified analysis generation
                var unifiedAnalysis = await GenerateUnifiedAnalysis(results, strategicAnalysis, context, cancellationToken);
                
                // Phase 4: Conflict resolution
                var conflictResolution = await ResolveConflicts(results, cancellationToken);

                return new SynthesisResult
                {
                    UnifiedAnalysis = unifiedAnalysis,
                    KeyFindings = prioritizedFindings.TopFindings,
                    PriorityRecommendations = prioritizedFindings.TopRecommendations,
                    OverallQualityScore = CalculateOverallQuality(results, validation),
                    AgentAgreement = validation.AgreementScores,
                    ConflictingRecommendations = conflictResolution.Conflicts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Result synthesis failed");
                return CreateFallbackSynthesis(results);
            }
        }

        /// <summary>
        /// Conducts high-level code overview analysis
        /// </summary>
        private async Task<CodeOverviewAnalysis> ConductCodeOverviewAsync(
            CodeReviewRequest request,
            EnhancedContext context,
            CancellationToken cancellationToken)
        {
            var prompt = BuildStrategicAnalysisPrompt(request, context);
            
            // Use Chain of Thought reasoning for strategic thinking
            var reasoningContext = new ReasoningContext
            {
                CodeLanguage = request.Language,
                ProjectType = context.ProjectType,
                ComplexityLevel = context.CodeComplexity
            };

            var cotResult = await _reasoningEngine.ConductChainOfThoughtAsync(
                prompt, reasoningContext, cancellationToken);

            return ParseCodeOverview(cotResult.Conclusion);
        }

        /// <summary>
        /// Conducts comprehensive risk assessment
        /// </summary>
        private async Task<RiskAssessment> ConductRiskAssessmentAsync(
            CodeReviewRequest request,
            EnhancedContext context,
            CancellationToken cancellationToken)
        {
            var prompt = $@"
As a lead technical architect, assess the potential risks in this code:

**Context:**
- Language: {request.Language}
- Project Type: {context.ProjectType}
- Complexity: {context.CodeComplexity}/10
- Business Domain: {request.Options.BusinessDomain}

**Code to Analyze:**
{request.Content}

**Risk Assessment Framework:**
Evaluate risks in these categories:
1. **Security Risks**: Vulnerabilities, attack vectors, data exposure
2. **Performance Risks**: Scalability issues, memory leaks, inefficiencies  
3. **Maintainability Risks**: Technical debt, coupling, complexity
4. **Business Risks**: Logic errors, compliance issues, data integrity
5. **Operational Risks**: Deployment issues, monitoring gaps, failure modes

For each risk identified:
- **Severity**: Critical/High/Medium/Low
- **Probability**: High/Medium/Low  
- **Impact**: Describe business/technical impact
- **Mitigation**: Suggested approach

Focus on the most significant risks that require specialized agent attention.
";

            var response = await _claudeService.GenerateReviewAsync(prompt, cancellationToken);
            return ParseRiskAssessment(response);
        }

        /// <summary>
        /// Develops coordination strategy for specialized agents
        /// </summary>
        private async Task<CoordinationStrategy> DevelopCoordinationStrategyAsync(
            CodeReviewRequest request,
            EnhancedContext context,
            CodeOverviewAnalysis overview,
            CancellationToken cancellationToken)
        {
            var prompt = $@"
As a lead orchestrator, develop a coordination strategy for specialized AI agents:

**Code Overview:**
{overview.Strategy}

**Focus Areas:**
{string.Join("\n", overview.FocusAreas.Select(f => $"- {f}"))}

**Available Agents:**
- SecurityExpert: Security vulnerability analysis
- PerformanceAnalyst: Performance optimization
- ArchitectureExpert: Design patterns and architecture
- ArchitectureStandardsAgent: Enterprise standards enforcement
- CodeQualityReviewer: Code quality and maintainability
- TestingSpecialist: Test coverage and quality

**Instructions:**
For each agent that should be activated, provide:
1. **Specific Focus**: What should this agent concentrate on?
2. **Priority Level**: Critical/High/Medium/Low
3. **Expected Outcome**: What should this agent deliver?
4. **Coordination Notes**: How should this agent's work relate to others?

Format as:
AGENT: [AgentType]
FOCUS: [specific focus area]
PRIORITY: [priority level]
OUTCOME: [expected deliverable]
COORDINATION: [interaction with other agents]
---
";

            var response = await _claudeService.GenerateReviewAsync(prompt, cancellationToken);
            return ParseCoordinationStrategy(response);
        }

        /// <summary>
        /// Validates consistency across agent results
        /// </summary>
        private async Task<CrossAgentValidation> ValidateCrossAgentConsistency(
            SpecializedAgentResult[] results,
            CancellationToken cancellationToken)
        {
            var prompt = $@"
Analyze these specialized agent results for consistency and conflicts:

{string.Join("\n\n", results.Select((r, i) => $"**{r.AgentType} Agent Result:**\n{r.Analysis}"))}

**Validation Tasks:**
1. **Consistency Check**: Do agents agree on major findings?
2. **Conflict Detection**: Are there contradictory recommendations?
3. **Coverage Analysis**: Are there gaps or overlaps in analysis?
4. **Quality Assessment**: Are findings well-supported and actionable?

Provide:
- **Agreement Score** for each pair of agents (0.0-1.0)
- **Major Conflicts** if any
- **Coverage Gaps** if identified
- **Quality Concerns** if present
";

            var response = await _claudeService.GenerateReviewAsync(prompt, cancellationToken);
            return ParseCrossAgentValidation(response, results);
        }

        /// <summary>
        /// Prioritizes findings across all agents
        /// </summary>
        private async Task<PrioritizedFindings> PrioritizeFindings(
            SpecializedAgentResult[] results,
            StrategicAnalysis strategicAnalysis,
            CancellationToken cancellationToken)
        {
            var allFindings = results.SelectMany(r => r.Findings).ToList();
            var allRecommendations = results.SelectMany(r => r.Recommendations).ToList();

            var prompt = $@"
Prioritize these findings and recommendations based on strategic importance:

**Strategic Context:**
{strategicAnalysis.OverallStrategy}

**Key Focus Areas:**
{string.Join("\n", strategicAnalysis.KeyFocusAreas.Select(f => $"- {f}"))}

**All Findings:**
{string.Join("\n", allFindings.Select((f, i) => $"{i + 1}. [{f.Severity}] {f.Description}"))}

**All Recommendations:**
{string.Join("\n", allRecommendations.Select((r, i) => $"{i + 1}. [{r.Priority}] {r.Title}"))}

**Prioritization Criteria:**
1. **Business Impact**: Effect on functionality, security, performance
2. **Strategic Alignment**: Matches key focus areas
3. **Implementation Urgency**: How quickly this should be addressed
4. **Risk Mitigation**: Reduces identified risks

Select the top 5 findings and top 5 recommendations, with justification.
";

            var response = await _claudeService.GenerateReviewAsync(prompt, cancellationToken);
            return ParsePrioritizedFindings(response, allFindings, allRecommendations);
        }

        /// <summary>
        /// Generates unified analysis combining all agent insights
        /// </summary>
        private async Task<string> GenerateUnifiedAnalysis(
            SpecializedAgentResult[] results,
            StrategicAnalysis strategicAnalysis,
            EnhancedContext context,
            CancellationToken cancellationToken)
        {
            var prompt = $@"
Create a unified, comprehensive code review analysis:

**Strategic Framework:**
{strategicAnalysis.OverallStrategy}

**Individual Agent Analyses:**
{string.Join("\n\n", results.Select(r => $"**{r.AgentType} (Confidence: {r.ConfidenceScore:F2})**\n{r.Analysis}"))}

**Context:**
- Project Type: {context.ProjectType}
- Complexity: {context.CodeComplexity}/10
- Team Experience: {context.TeamContext?.ExperienceLevel}

**Synthesis Requirements:**
1. **Executive Summary**: High-level assessment and key insights
2. **Critical Issues**: Most important problems identified
3. **Architectural Assessment**: Design and pattern evaluation  
4. **Quality Evaluation**: Code quality and maintainability
5. **Security & Performance**: Key concerns and recommendations
6. **Next Steps**: Prioritized action items

Create a cohesive narrative that leverages the best insights from each specialist while avoiding redundancy. Focus on actionable guidance for the development team.
";

            return await _claudeService.GenerateReviewAsync(prompt, cancellationToken);
        }

        /// <summary>
        /// Resolves conflicts between agent recommendations
        /// </summary>
        private async Task<ConflictResolution> ResolveConflicts(
            SpecializedAgentResult[] results,
            CancellationToken cancellationToken)
        {
            var recommendations = results.SelectMany(r => r.Recommendations).ToList();
            
            // Identify potential conflicts
            var conflicts = IdentifyRecommendationConflicts(recommendations);
            
            if (!conflicts.Any())
            {
                return new ConflictResolution { Conflicts = new List<string>() };
            }

            var prompt = $@"
Resolve these conflicting recommendations:

{string.Join("\n\n", conflicts.Select((c, i) => $"**Conflict {i + 1}:**\n{c}"))}

For each conflict:
1. **Root Cause**: Why do these recommendations conflict?
2. **Trade-offs**: What are the pros/cons of each approach?
3. **Resolution**: What's the best path forward?
4. **Rationale**: Why is this the optimal choice?

Provide balanced, practical resolutions that consider the overall codebase context.
";

            var response = await _claudeService.GenerateReviewAsync(prompt, cancellationToken);
            return new ConflictResolution 
            { 
                Conflicts = conflicts,
                Resolutions = ParseConflictResolutions(response)
            };
        }

        /// <summary>
        /// Builds strategic analysis prompt
        /// </summary>
        private string BuildStrategicAnalysisPrompt(CodeReviewRequest request, EnhancedContext context)
        {
            var prompt = new StringBuilder();
            
            prompt.AppendLine("You are a lead technical architect conducting strategic code analysis.");
            prompt.AppendLine("Your role is to provide high-level guidance for specialized review agents.");
            prompt.AppendLine();
            prompt.AppendLine("**Code Context:**");
            prompt.AppendLine($"- File: {request.FileName}");
            prompt.AppendLine($"- Language: {request.Language}");
            prompt.AppendLine($"- Project Type: {context.ProjectType}");
            prompt.AppendLine($"- Complexity Level: {context.CodeComplexity}/10");
            prompt.AppendLine($"- Business Domain: {request.Options.BusinessDomain}");
            
            if (context.History?.PreviousIssues.Any() == true)
            {
                prompt.AppendLine();
                prompt.AppendLine("**Historical Context:**");
                foreach (var issue in context.History.PreviousIssues.Take(3))
                {
                    prompt.AppendLine($"- {issue}");
                }
            }
            
            prompt.AppendLine();
            prompt.AppendLine("**Code to Analyze:**");
            prompt.AppendLine(request.Content);
            prompt.AppendLine();
            prompt.AppendLine("**Strategic Analysis Required:**");
            prompt.AppendLine("1. What is the overall review strategy for this code?");
            prompt.AppendLine("2. What are the 3-5 key focus areas for detailed analysis?");
            prompt.AppendLine("3. What are the main technical and business risks?");
            prompt.AppendLine("4. How should specialized agents coordinate their efforts?");
            prompt.AppendLine();
            prompt.AppendLine("Provide strategic guidance that will enable effective specialized analysis.");
            
            return prompt.ToString();
        }

        /// <summary>
        /// Helper methods for parsing and calculation
        /// </summary>
        private CodeOverviewAnalysis ParseCodeOverview(string conclusion)
        {
            return new CodeOverviewAnalysis
            {
                Strategy = conclusion,
                FocusAreas = ExtractFocusAreas(conclusion)
            };
        }

        private List<string> ExtractFocusAreas(string text)
        {
            var areas = new List<string>();
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (line.Contains("focus") || line.Contains("area") || line.Contains("priority"))
                {
                    var cleaned = line.Trim().TrimStart('-', '*', '1', '2', '3', '4', '5', '.').Trim();
                    if (cleaned.Length > 10 && cleaned.Length < 100)
                    {
                        areas.Add(cleaned);
                    }
                }
            }
            
            return areas.Take(5).ToList();
        }

        private RiskAssessment ParseRiskAssessment(string response)
        {
            var risks = new List<string>();
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (line.ToLower().Contains("risk") || line.ToLower().Contains("vulnerability") || 
                    line.ToLower().Contains("critical") || line.ToLower().Contains("high"))
                {
                    var cleaned = line.Trim().TrimStart('-', '*', '1', '2', '3', '4', '5', '.').Trim();
                    if (cleaned.Length > 15)
                    {
                        risks.Add(cleaned);
                    }
                }
            }
            
            return new RiskAssessment { Risks = risks.Take(10).ToList() };
        }

        private CoordinationStrategy ParseCoordinationStrategy(string response)
        {
            var instructions = new Dictionary<AgentType, string>();
            var sections = response.Split("---", StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var section in sections)
            {
                var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                AgentType? agentType = null;
                var instruction = new StringBuilder();
                
                foreach (var line in lines)
                {
                    if (line.StartsWith("AGENT:"))
                    {
                        var agentName = line.Substring(6).Trim();
                        if (Enum.TryParse<AgentType>(agentName, out var type))
                        {
                            agentType = type;
                        }
                    }
                    else if (agentType.HasValue)
                    {
                        instruction.AppendLine(line);
                    }
                }
                
                if (agentType.HasValue && instruction.Length > 0)
                {
                    instructions[agentType.Value] = instruction.ToString().Trim();
                }
            }
            
            return new CoordinationStrategy
            {
                Instructions = instructions,
                EstimatedTime = TimeSpan.FromMinutes(5 * instructions.Count)
            };
        }

        private CrossAgentValidation ParseCrossAgentValidation(string response, SpecializedAgentResult[] results)
        {
            var agreementScores = new Dictionary<string, double>();
            
            // Simple heuristic for agreement scoring
            for (int i = 0; i < results.Length; i++)
            {
                for (int j = i + 1; j < results.Length; j++)
                {
                    var key = $"{results[i].AgentType}-{results[j].AgentType}";
                    agreementScores[key] = CalculateAgreementScore(results[i], results[j]);
                }
            }
            
            return new CrossAgentValidation
            {
                AgreementScores = agreementScores,
                OverallConsistency = agreementScores.Values.Average()
            };
        }

        private double CalculateAgreementScore(SpecializedAgentResult result1, SpecializedAgentResult result2)
        {
            // Simple heuristic based on confidence scores and finding overlap
            var confidenceAlignment = 1.0 - Math.Abs(result1.ConfidenceScore - result2.ConfidenceScore);
            
            // Check for common themes in findings
            var commonWords = GetCommonWords(result1.Analysis, result2.Analysis);
            var contentAlignment = Math.Min(1.0, commonWords.Count * 0.1);
            
            return (confidenceAlignment + contentAlignment) / 2.0;
        }

        private List<string> GetCommonWords(string text1, string text2)
        {
            var words1 = text1.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 4).ToHashSet();
            var words2 = text2.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 4).ToHashSet();
            
            return words1.Intersect(words2).ToList();
        }

        private PrioritizedFindings ParsePrioritizedFindings(string response, List<Finding> allFindings, List<Recommendation> allRecommendations)
        {
            // Extract top findings and recommendations from the response
            // This is a simplified implementation
            return new PrioritizedFindings
            {
                TopFindings = allFindings.OrderByDescending(f => GetSeverityScore(f.Severity)).Take(5).Select(f => f.Description).ToList(),
                TopRecommendations = allRecommendations.OrderByDescending(r => GetPriorityScore(r.Priority)).Take(5).Select(r => r.Title).ToList()
            };
        }

        private int GetSeverityScore(string severity)
        {
            return severity?.ToUpper() switch
            {
                "CRITICAL" => 4,
                "HIGH" => 3,
                "MEDIUM" => 2,
                "LOW" => 1,
                _ => 0
            };
        }

        private int GetPriorityScore(string priority)
        {
            return priority?.ToUpper() switch
            {
                "CRITICAL" => 4,
                "HIGH" => 3,
                "MEDIUM" => 2,
                "LOW" => 1,
                _ => 0
            };
        }

        private List<string> IdentifyRecommendationConflicts(List<Recommendation> recommendations)
        {
            var conflicts = new List<string>();
            
            // Simple conflict detection based on keywords
            var performanceRecs = recommendations.Where(r => r.Category?.ToLower().Contains("performance") == true).ToList();
            var securityRecs = recommendations.Where(r => r.Category?.ToLower().Contains("security") == true).ToList();
            
            // Check for performance vs security conflicts
            if (performanceRecs.Any(p => p.Description.ToLower().Contains("cache")) &&
                securityRecs.Any(s => s.Description.ToLower().Contains("validation")))
            {
                conflicts.Add("Potential conflict between caching for performance and validation for security");
            }
            
            return conflicts;
        }

        private List<string> ParseConflictResolutions(string response)
        {
            var resolutions = new List<string>();
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (line.ToLower().Contains("resolution") || line.ToLower().Contains("recommend"))
                {
                    resolutions.Add(line.Trim());
                }
            }
            
            return resolutions;
        }

        private double CalculateOverallQuality(SpecializedAgentResult[] results, CrossAgentValidation validation)
        {
            var avgConfidence = results.Average(r => r.ConfidenceScore);
            var consistencyBonus = validation.OverallConsistency * 0.2;
            var successPenalty = results.Any(r => !r.Success) ? 0.1 : 0.0;
            
            return Math.Min(1.0, avgConfidence + consistencyBonus - successPenalty);
        }

        private List<string> DefineQualityGates(EnhancedContext context, RiskAssessment riskAssessment)
        {
            var gates = new List<string>
            {
                "All critical security issues must be addressed",
                "Performance impact must be assessed for complex operations"
            };
            
            if (context.CodeComplexity > 7)
            {
                gates.Add("Architecture review required for high complexity code");
            }
            
            if (riskAssessment.Risks.Any(r => r.ToLower().Contains("critical")))
            {
                gates.Add("Critical risks must be mitigated before approval");
            }
            
            return gates;
        }

        private StrategicAnalysis CreateFallbackStrategy(CodeReviewRequest request, EnhancedContext context)
        {
            return new StrategicAnalysis
            {
                OverallStrategy = "Comprehensive review focusing on security, performance, and quality",
                KeyFocusAreas = new List<string> { "Security analysis", "Code quality", "Performance considerations" },
                AgentInstructions = new Dictionary<AgentType, string>
                {
                    [AgentType.SecurityExpert] = "Conduct thorough security analysis",
                    [AgentType.CodeQualityReviewer] = "Review code quality and maintainability"
                },
                ComplexityAssessment = context.CodeComplexity,
                PotentialRisks = new List<string> { "Unknown risks due to analysis failure" },
                EstimatedAnalysisTime = TimeSpan.FromMinutes(10)
            };
        }

        private SynthesisResult CreateFallbackSynthesis(SpecializedAgentResult[] results)
        {
            return new SynthesisResult
            {
                UnifiedAnalysis = "Analysis completed with mixed results. Review individual agent outputs for details.",
                KeyFindings = results.SelectMany(r => r.Findings).Take(5).Select(f => f.Description).ToList(),
                PriorityRecommendations = results.SelectMany(r => r.Recommendations).Take(5).Select(r => r.Title).ToList(),
                OverallQualityScore = results.Any() ? results.Average(r => r.ConfidenceScore) : 0.5,
                AgentAgreement = new Dictionary<string, double>(),
                ConflictingRecommendations = new List<string>()
            };
        }
    }

    #region Supporting Classes

    public class CodeOverviewAnalysis
    {
        public string Strategy { get; set; } = string.Empty;
        public List<string> FocusAreas { get; set; } = new();
    }

    public class RiskAssessment
    {
        public List<string> Risks { get; set; } = new();
    }

    public class CoordinationStrategy
    {
        public Dictionary<AgentType, string> Instructions { get; set; } = new();
        public TimeSpan EstimatedTime { get; set; }
    }

    public class CrossAgentValidation
    {
        public Dictionary<string, double> AgreementScores { get; set; } = new();
        public double OverallConsistency { get; set; }
    }

    public class PrioritizedFindings
    {
        public List<string> TopFindings { get; set; } = new();
        public List<string> TopRecommendations { get; set; } = new();
    }

    public class ConflictResolution
    {
        public List<string> Conflicts { get; set; } = new();
        public List<string> Resolutions { get; set; } = new();
    }

    #endregion
}