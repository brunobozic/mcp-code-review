using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.AI.Advanced
{
    /// <summary>
    /// Dynamic agent selector that intelligently chooses optimal agents based on context and requirements
    /// Implements 2025 AI orchestration patterns with smart agent selection
    /// </summary>
    public class DynamicAgentSelector
    {
        private readonly IClaudeService _claudeService;
        private readonly ILogger<DynamicAgentSelector> _logger;

        public DynamicAgentSelector(
            IClaudeService claudeService,
            ILogger<DynamicAgentSelector> logger)
        {
            _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Selects optimal agents for the given request and context
        /// </summary>
        public async Task<AgentSelectionResult> SelectOptimalAgentsAsync(
            CodeReviewRequest request,
            EnhancedContext context,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Selecting optimal agents for {FileName}", request.FileName);

            try
            {
                // Phase 1: Analyze code characteristics
                var codeCharacteristics = await AnalyzeCodeCharacteristicsAsync(request, context, cancellationToken);
                
                // Phase 2: Determine agent requirements
                var agentRequirements = DetermineAgentRequirements(codeCharacteristics, context);
                
                // Phase 3: Score and rank agents
                var agentScores = ScoreAgentRelevance(agentRequirements, context);
                
                // Phase 4: Select optimal agent set
                var selectedAgents = SelectAgentSet(agentScores, context);

                return new AgentSelectionResult
                {
                    SelectedAgents = selectedAgents,
                    SelectionReasoning = GenerateSelectionReasoning(selectedAgents, agentRequirements),
                    EstimatedExecutionTime = EstimateExecutionTime(selectedAgents),
                    ConfidenceScore = CalculateSelectionConfidence(selectedAgents, agentRequirements)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent selection failed");
                return CreateFallbackSelection();
            }
        }

        /// <summary>
        /// Analyzes code characteristics to inform agent selection
        /// </summary>
        private async Task<CodeCharacteristics> AnalyzeCodeCharacteristicsAsync(
            CodeReviewRequest request,
            EnhancedContext context,
            CancellationToken cancellationToken)
        {
            var prompt = $@"
Analyze this code to determine what types of specialized review it needs:

**Code:**
{request.Content[..Math.Min(2000, request.Content.Length)]}

**Context:**
- Language: {request.Language}
- Project Type: {context.ProjectType}
- Complexity: {context.CodeComplexity}/10

**Analysis Required:**
Identify the key characteristics that would benefit from specialized review:

1. **Security Concerns**: Does this code handle sensitive data, authentication, or have potential vulnerabilities?
2. **Performance Implications**: Are there performance-critical operations, algorithms, or scalability concerns?
3. **Architectural Significance**: Does this code represent important architectural decisions or patterns?
4. **Standards Compliance**: Are there enterprise standards, conventions, or compliance requirements?
5. **Quality Issues**: Are there maintainability, readability, or code quality concerns?
6. **Testing Requirements**: Does this code need specialized testing analysis or coverage evaluation?

Rate each area as High/Medium/Low/None based on the code's needs.

Format:
SECURITY: [High/Medium/Low/None] - [brief reason]
PERFORMANCE: [High/Medium/Low/None] - [brief reason]
ARCHITECTURE: [High/Medium/Low/None] - [brief reason]
STANDARDS: [High/Medium/Low/None] - [brief reason]
QUALITY: [High/Medium/Low/None] - [brief reason]
TESTING: [High/Medium/Low/None] - [brief reason]
";

            var response = await _claudeService.GetCompletionAsync(prompt, cancellationToken);
            return ParseCodeCharacteristics(response);
        }

        /// <summary>
        /// Parses code characteristics from AI response
        /// </summary>
        private CodeCharacteristics ParseCodeCharacteristics(string response)
        {
            var characteristics = new CodeCharacteristics();
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.StartsWith("SECURITY:"))
                    characteristics.SecurityRelevance = ParseRelevanceLevel(line);
                else if (line.StartsWith("PERFORMANCE:"))
                    characteristics.PerformanceRelevance = ParseRelevanceLevel(line);
                else if (line.StartsWith("ARCHITECTURE:"))
                    characteristics.ArchitectureRelevance = ParseRelevanceLevel(line);
                else if (line.StartsWith("STANDARDS:"))
                    characteristics.StandardsRelevance = ParseRelevanceLevel(line);
                else if (line.StartsWith("QUALITY:"))
                    characteristics.QualityRelevance = ParseRelevanceLevel(line);
                else if (line.StartsWith("TESTING:"))
                    characteristics.TestingRelevance = ParseRelevanceLevel(line);
            }

            return characteristics;
        }

        /// <summary>
        /// Parses relevance level from analysis line
        /// </summary>
        private RelevanceLevel ParseRelevanceLevel(string line)
        {
            if (line.ToLower().Contains("high")) return RelevanceLevel.High;
            if (line.ToLower().Contains("medium")) return RelevanceLevel.Medium;
            if (line.ToLower().Contains("low")) return RelevanceLevel.Low;
            return RelevanceLevel.None;
        }

        /// <summary>
        /// Determines agent requirements based on code characteristics
        /// </summary>
        private AgentRequirements DetermineAgentRequirements(CodeCharacteristics characteristics, EnhancedContext context)
        {
            var requirements = new AgentRequirements();

            // Map characteristics to agent needs
            requirements.AgentNeeds = new Dictionary<AgentType, AgentNeed>
            {
                [AgentType.SecurityExpert] = new AgentNeed
                {
                    Priority = MapRelevanceToPriority(characteristics.SecurityRelevance),
                    Justification = "Security analysis needed for vulnerability assessment"
                },
                [AgentType.PerformanceAnalyst] = new AgentNeed
                {
                    Priority = MapRelevanceToPriority(characteristics.PerformanceRelevance),
                    Justification = "Performance analysis needed for optimization opportunities"
                },
                [AgentType.ArchitectureExpert] = new AgentNeed
                {
                    Priority = MapRelevanceToPriority(characteristics.ArchitectureRelevance),
                    Justification = "Architecture analysis needed for design pattern evaluation"
                },
                [AgentType.ArchitectureStandardsAgent] = new AgentNeed
                {
                    Priority = MapRelevanceToPriority(characteristics.StandardsRelevance),
                    Justification = "Standards compliance verification needed"
                },
                [AgentType.CodeQualityReviewer] = new AgentNeed
                {
                    Priority = MapRelevanceToPriority(characteristics.QualityRelevance),
                    Justification = "Code quality assessment needed for maintainability"
                },
                [AgentType.TestingSpecialist] = new AgentNeed
                {
                    Priority = MapRelevanceToPriority(characteristics.TestingRelevance),
                    Justification = "Testing analysis needed for coverage and quality"
                }
            };

            // Adjust priorities based on context
            AdjustPrioritiesForContext(requirements, context);

            return requirements;
        }

        /// <summary>
        /// Maps relevance level to priority
        /// </summary>
        private AgentPriority MapRelevanceToPriority(RelevanceLevel relevance)
        {
            return relevance switch
            {
                RelevanceLevel.High => AgentPriority.Critical,
                RelevanceLevel.Medium => AgentPriority.High,
                RelevanceLevel.Low => AgentPriority.Medium,
                RelevanceLevel.None => AgentPriority.Low,
                _ => AgentPriority.Low
            };
        }

        /// <summary>
        /// Adjusts agent priorities based on context
        /// </summary>
        private void AdjustPrioritiesForContext(AgentRequirements requirements, EnhancedContext context)
        {
            // High complexity code needs more architectural review
            if (context.CodeComplexity > 8)
            {
                if (requirements.AgentNeeds[AgentType.ArchitectureExpert].Priority < AgentPriority.High)
                    requirements.AgentNeeds[AgentType.ArchitectureExpert].Priority = AgentPriority.High;
            }

            // Web applications need more security focus
            if (context.ProjectType?.ToLower().Contains("web") == true)
            {
                if (requirements.AgentNeeds[AgentType.SecurityExpert].Priority < AgentPriority.High)
                    requirements.AgentNeeds[AgentType.SecurityExpert].Priority = AgentPriority.High;
            }

            // Always include quality reviewer as baseline
            if (requirements.AgentNeeds[AgentType.CodeQualityReviewer].Priority == AgentPriority.Low)
                requirements.AgentNeeds[AgentType.CodeQualityReviewer].Priority = AgentPriority.Medium;
        }

        /// <summary>
        /// Scores agent relevance for selection
        /// </summary>
        private Dictionary<AgentType, double> ScoreAgentRelevance(AgentRequirements requirements, EnhancedContext context)
        {
            var scores = new Dictionary<AgentType, double>();

            foreach (var agentNeed in requirements.AgentNeeds)
            {
                var baseScore = agentNeed.Value.Priority switch
                {
                    AgentPriority.Critical => 1.0,
                    AgentPriority.High => 0.8,
                    AgentPriority.Medium => 0.6,
                    AgentPriority.Low => 0.3,
                    _ => 0.0
                };

                // Apply context modifiers
                var contextModifier = CalculateContextModifier(agentNeed.Key, context);
                scores[agentNeed.Key] = Math.Min(1.0, baseScore + contextModifier);
            }

            return scores;
        }

        /// <summary>
        /// Calculates context modifier for agent scoring
        /// </summary>
        private double CalculateContextModifier(AgentType agentType, EnhancedContext context)
        {
            var modifier = 0.0;

            switch (agentType)
            {
                case AgentType.SecurityExpert:
                    if (context.ProjectType?.ToLower().Contains("api") == true) modifier += 0.1;
                    if (context.ProjectType?.ToLower().Contains("web") == true) modifier += 0.1;
                    break;

                case AgentType.PerformanceAnalyst:
                    if (context.CodeComplexity > 7) modifier += 0.1;
                    if (context.ProjectType?.ToLower().Contains("service") == true) modifier += 0.1;
                    break;

                case AgentType.ArchitectureExpert:
                    if (context.CodeComplexity > 8) modifier += 0.15;
                    if (context.ProjectType?.ToLower().Contains("enterprise") == true) modifier += 0.1;
                    break;
            }

            return modifier;
        }

        /// <summary>
        /// Selects optimal agent set based on scores and constraints
        /// </summary>
        private List<AgentSelection> SelectAgentSet(Dictionary<AgentType, double> agentScores, EnhancedContext context)
        {
            var selectedAgents = new List<AgentSelection>();
            var threshold = 0.5; // Minimum score for selection

            // Always select agents above threshold, prioritize by score
            var candidateAgents = agentScores
                .Where(kvp => kvp.Value >= threshold)
                .OrderByDescending(kvp => kvp.Value)
                .ToList();

            foreach (var candidate in candidateAgents)
            {
                selectedAgents.Add(new AgentSelection
                {
                    AgentType = candidate.Key,
                    Priority = ScoreToPriority(candidate.Value),
                    SelectionScore = candidate.Value,
                    ExpectedContribution = GetExpectedContribution(candidate.Key)
                });
            }

            // Ensure minimum viable agent set
            EnsureMinimumAgentSet(selectedAgents, agentScores);

            // Respect maximum agent constraints (for performance)
            if (selectedAgents.Count > 6)
            {
                selectedAgents = selectedAgents.Take(6).ToList();
            }

            return selectedAgents;
        }

        /// <summary>
        /// Converts score to priority
        /// </summary>
        private AgentPriority ScoreToPriority(double score)
        {
            return score switch
            {
                >= 0.9 => AgentPriority.Critical,
                >= 0.7 => AgentPriority.High,
                >= 0.5 => AgentPriority.Medium,
                _ => AgentPriority.Low
            };
        }

        /// <summary>
        /// Gets expected contribution for agent type
        /// </summary>
        private string GetExpectedContribution(AgentType agentType)
        {
            return agentType switch
            {
                AgentType.SecurityExpert => "Security vulnerability assessment and secure coding recommendations",
                AgentType.PerformanceAnalyst => "Performance optimization opportunities and scalability analysis",
                AgentType.ArchitectureExpert => "Design pattern evaluation and architectural guidance",
                AgentType.ArchitectureStandardsAgent => "Enterprise standards compliance verification",
                AgentType.CodeQualityReviewer => "Code quality assessment and maintainability recommendations",
                AgentType.TestingSpecialist => "Testing strategy evaluation and coverage analysis",
                _ => "Specialized domain analysis and recommendations"
            };
        }

        /// <summary>
        /// Ensures minimum viable agent set
        /// </summary>
        private void EnsureMinimumAgentSet(List<AgentSelection> selectedAgents, Dictionary<AgentType, double> allScores)
        {
            // Always include CodeQualityReviewer if not present
            if (!selectedAgents.Any(a => a.AgentType == AgentType.CodeQualityReviewer))
            {
                selectedAgents.Add(new AgentSelection
                {
                    AgentType = AgentType.CodeQualityReviewer,
                    Priority = AgentPriority.Medium,
                    SelectionScore = 0.6,
                    ExpectedContribution = GetExpectedContribution(AgentType.CodeQualityReviewer)
                });
            }

            // Ensure at least 2 agents are selected
            if (selectedAgents.Count < 2)
            {
                var additionalAgent = allScores
                    .Where(kvp => !selectedAgents.Any(s => s.AgentType == kvp.Key))
                    .OrderByDescending(kvp => kvp.Value)
                    .FirstOrDefault();

                if (additionalAgent.Key != default)
                {
                    selectedAgents.Add(new AgentSelection
                    {
                        AgentType = additionalAgent.Key,
                        Priority = AgentPriority.Medium,
                        SelectionScore = additionalAgent.Value,
                        ExpectedContribution = GetExpectedContribution(additionalAgent.Key)
                    });
                }
            }
        }

        /// <summary>
        /// Helper methods
        /// </summary>
        private string GenerateSelectionReasoning(List<AgentSelection> selectedAgents, AgentRequirements requirements)
        {
            var reasoning = $"Selected {selectedAgents.Count} agents based on code analysis:\n";
            
            foreach (var agent in selectedAgents.OrderByDescending(a => a.SelectionScore))
            {
                reasoning += $"- {agent.AgentType} (Score: {agent.SelectionScore:F2}): {agent.ExpectedContribution}\n";
            }

            return reasoning;
        }

        private TimeSpan EstimateExecutionTime(List<AgentSelection> selectedAgents)
        {
            // Estimate based on parallel execution
            var baseTimePerAgent = TimeSpan.FromSeconds(30);
            var maxParallelAgents = Math.Min(selectedAgents.Count, 4);
            var totalTime = baseTimePerAgent.TotalSeconds * (selectedAgents.Count / (double)maxParallelAgents);
            
            return TimeSpan.FromSeconds(Math.Max(30, totalTime));
        }

        private double CalculateSelectionConfidence(List<AgentSelection> selectedAgents, AgentRequirements requirements)
        {
            if (!selectedAgents.Any()) return 0.0;

            var avgScore = selectedAgents.Average(a => a.SelectionScore);
            var coverageBonus = Math.Min(0.2, selectedAgents.Count * 0.05);
            
            return Math.Min(1.0, avgScore + coverageBonus);
        }

        private AgentSelectionResult CreateFallbackSelection()
        {
            return new AgentSelectionResult
            {
                SelectedAgents = new List<AgentSelection>
                {
                    new AgentSelection
                    {
                        AgentType = AgentType.CodeQualityReviewer,
                        Priority = AgentPriority.Medium,
                        SelectionScore = 0.7,
                        ExpectedContribution = "General code quality assessment"
                    }
                },
                SelectionReasoning = "Fallback selection due to analysis failure",
                EstimatedExecutionTime = TimeSpan.FromSeconds(30),
                ConfidenceScore = 0.5
            };
        }
    }

    #region Supporting Classes

    public class CodeCharacteristics
    {
        public RelevanceLevel SecurityRelevance { get; set; }
        public RelevanceLevel PerformanceRelevance { get; set; }
        public RelevanceLevel ArchitectureRelevance { get; set; }
        public RelevanceLevel StandardsRelevance { get; set; }
        public RelevanceLevel QualityRelevance { get; set; }
        public RelevanceLevel TestingRelevance { get; set; }
    }

    public class AgentRequirements
    {
        public Dictionary<AgentType, AgentNeed> AgentNeeds { get; set; } = new();
    }

    public class AgentNeed
    {
        public AgentPriority Priority { get; set; }
        public string Justification { get; set; } = string.Empty;
    }

    public class AgentSelection
    {
        public AgentType AgentType { get; set; }
        public AgentPriority Priority { get; set; }
        public double SelectionScore { get; set; }
        public string ExpectedContribution { get; set; } = string.Empty;
    }

    public class AgentSelectionResult
    {
        public List<AgentSelection> SelectedAgents { get; set; } = new();
        public string SelectionReasoning { get; set; } = string.Empty;
        public TimeSpan EstimatedExecutionTime { get; set; }
        public double ConfidenceScore { get; set; }
    }

    public enum RelevanceLevel
    {
        None,
        Low,
        Medium,
        High
    }

    public enum AgentPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion
}