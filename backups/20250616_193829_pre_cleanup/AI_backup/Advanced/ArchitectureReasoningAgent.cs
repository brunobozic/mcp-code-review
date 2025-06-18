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
    /// Specialized architecture reasoning agent with advanced design pattern analysis
    /// Implements 2025 architectural best practices and modern design principles
    /// </summary>
    public class ArchitectureReasoningAgent : ISpecializedReasoningAgent
    {
        private readonly IClaudeService _claudeService;
        private readonly AdvancedReasoningEngine _reasoningEngine;
        private readonly ILogger<ArchitectureReasoningAgent> _logger;

        public ArchitectureReasoningAgent(
            IClaudeService claudeService,
            AdvancedReasoningEngine reasoningEngine,
            ILogger<ArchitectureReasoningAgent> logger)
        {
            _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
            _reasoningEngine = reasoningEngine ?? throw new ArgumentNullException(nameof(reasoningEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Analyzes code with advanced architecture reasoning
        /// </summary>
        public async Task<AgentResult> AnalyzeWithReasoningAsync(
            CodeReviewRequest request,
            EnhancedContext context,
            ReasoningMode mode,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting architecture analysis with reasoning mode: {Mode}", mode);

            try
            {
                var architecturePrompt = BuildArchitectureAnalysisPrompt(request, context);
                
                var reasoningContext = new ReasoningContext
                {
                    CodeLanguage = request.Language,
                    ProjectType = context.ProjectType,
                    ComplexityLevel = context.CodeComplexity,
                    EnableExtendedThinking = mode == ReasoningMode.ExtendedThinking
                };

                string analysis;
                List<ReasoningStep> reasoningChain = null;

                switch (mode)
                {
                    case ReasoningMode.ChainOfThought:
                        var cotResult = await _reasoningEngine.ConductChainOfThoughtAsync(
                            architecturePrompt, reasoningContext, cancellationToken);
                        analysis = cotResult.Conclusion;
                        reasoningChain = cotResult.ReasoningSteps;
                        break;

                    case ReasoningMode.TreeOfThoughts:
                        var totResult = await _reasoningEngine.ConductTreeOfThoughtsAsync(
                            architecturePrompt, reasoningContext, TreeSearchStrategy.BestFirst, cancellationToken);
                        analysis = totResult.Synthesis;
                        reasoningChain = totResult.BestPath.Select((node, index) => new ReasoningStep
                        {
                            StepNumber = index + 1,
                            Content = node.Content,
                            Confidence = node.Score
                        }).ToList();
                        break;

                    case ReasoningMode.ExtendedThinking:
                        var hybridOptions = new HybridReasoningOptions
                        {
                            EnableExtendedThinking = true,
                            EnableToolIntegration = true,
                            ExtendedThinkingThreshold = 0.85
                        };
                        var hybridResult = await _reasoningEngine.ConductHybridReasoningAsync(
                            architecturePrompt, reasoningContext, hybridOptions, cancellationToken);
                        analysis = hybridResult.Synthesis.Content;
                        break;

                    default:
                        analysis = await _claudeService.GetCompletionAsync(architecturePrompt, cancellationToken);
                        break;
                }

                var findings = ExtractArchitectureFindings(analysis);
                var recommendations = GenerateArchitectureRecommendations(findings, context);
                var confidenceScore = CalculateArchitectureConfidence(findings, analysis, context);

                return new AgentResult
                {
                    Analysis = analysis,
                    Findings = findings,
                    Recommendations = recommendations,
                    ConfidenceScore = confidenceScore,
                    ReasoningChain = reasoningChain
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Architecture analysis failed");
                throw;
            }
        }

        /// <summary>
        /// Builds comprehensive architecture analysis prompt
        /// </summary>
        private string BuildArchitectureAnalysisPrompt(CodeReviewRequest request, EnhancedContext context)
        {
            return $@"
You are a senior software architect conducting comprehensive architectural analysis.
Apply the latest 2025 architectural patterns including microservices, event-driven architecture, CQRS, and modern design principles.

**Code Context:**
- File: {request.FileName}
- Language: {request.Language}
- Project Type: {context.ProjectType}
- Business Domain: {request.Options.BusinessDomain}
- Complexity: {context.CodeComplexity}/10

**Code to Analyze:**
{request.Content}

**Architecture Analysis Framework:**

1. **Design Patterns & Principles**
   - Evaluate SOLID principles adherence
   - Identify design patterns usage and appropriateness
   - Check for proper abstraction and encapsulation
   - Assess dependency management and inversion

2. **Separation of Concerns**
   - Analyze layer separation and responsibility distribution
   - Check for proper business logic isolation
   - Evaluate data access layer implementation
   - Assess presentation layer concerns

3. **Coupling & Cohesion**
   - Measure coupling between components
   - Evaluate cohesion within modules
   - Check for circular dependencies
   - Assess interface design quality

4. **Scalability & Maintainability**
   - Evaluate horizontal and vertical scaling considerations
   - Check for extensibility and flexibility
   - Assess code organization and structure
   - Evaluate configuration and environment management

5. **Domain Modeling**
   - Check domain object design and relationships
   - Evaluate business rule implementation
   - Assess aggregate and entity boundaries
   - Check for proper domain service usage

6. **Integration Patterns**
   - Evaluate API design and integration patterns
   - Check messaging and communication patterns
   - Assess error handling and resilience patterns
   - Evaluate transaction and consistency management

7. **Modern Architecture Patterns**
   - Event-driven architecture considerations
   - CQRS and Event Sourcing applicability
   - Microservices vs monolith design decisions
   - Cloud-native and distributed system patterns

**Quality Attributes:**
- Reliability and fault tolerance
- Performance and scalability
- Security and compliance
- Maintainability and testability
- Usability and accessibility

**Analysis Requirements:**
- Identify architectural smells and anti-patterns
- Evaluate alignment with business requirements
- Assess long-term maintainability implications
- Consider team structure and Conway's Law
- Provide strategic architectural guidance

**Output Format:**
ARCHITECTURAL_CONCERN: [Architecture Issue Name]
CATEGORY: [Design/Pattern/Structure/Integration]
IMPACT: [High/Medium/Low]
DESCRIPTION: [Detailed architectural analysis]
IMPLICATIONS: [Long-term consequences]
RECOMMENDATIONS: [Specific architectural improvements]
---

Focus on the most critical architectural decisions that will impact system evolution and maintainability.
";
        }

        /// <summary>
        /// Extracts architecture findings from analysis
        /// </summary>
        private List<Finding> ExtractArchitectureFindings(string analysis)
        {
            var findings = new List<Finding>();
            var sections = analysis.Split("---", StringSplitOptions.RemoveEmptyEntries);

            foreach (var section in sections)
            {
                var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var finding = new Finding();

                foreach (var line in lines)
                {
                    if (line.StartsWith("ARCHITECTURAL_CONCERN:"))
                        finding.Title = line.Substring(22).Trim();
                    else if (line.StartsWith("CATEGORY:"))
                        finding.Type = line.Substring(9).Trim();
                    else if (line.StartsWith("IMPACT:"))
                        finding.Severity = line.Substring(7).Trim();
                    else if (line.StartsWith("DESCRIPTION:"))
                        finding.Description = line.Substring(12).Trim();
                    else if (line.StartsWith("IMPLICATIONS:"))
                        finding.Impact = line.Substring(13).Trim();
                }

                if (!string.IsNullOrEmpty(finding.Title))
                {
                    finding.Category = "Architecture";
                    findings.Add(finding);
                }
            }

            // Add additional architecture pattern checks
            findings.AddRange(PerformAdditionalArchitectureChecks(analysis));

            return findings;
        }

        /// <summary>
        /// Performs additional automated architecture checks
        /// </summary>
        private List<Finding> PerformAdditionalArchitectureChecks(string analysis)
        {
            var additionalFindings = new List<Finding>();

            // Check for architectural patterns and anti-patterns
            var architecturalPatterns = new Dictionary<string, (string title, string severity, string type)>
            {
                ["tight coupling"] = ("Tight Coupling Detected", "High", "Anti-Pattern"),
                ["god class"] = ("God Class Anti-Pattern", "High", "Anti-Pattern"),
                ["circular dependency"] = ("Circular Dependency Found", "High", "Structure"),
                ["singleton"] = ("Singleton Pattern Usage", "Medium", "Pattern"),
                ["repository"] = ("Repository Pattern Implementation", "Low", "Pattern"),
                ["factory"] = ("Factory Pattern Usage", "Low", "Pattern"),
                ["observer"] = ("Observer Pattern Implementation", "Low", "Pattern"),
                ["strategy"] = ("Strategy Pattern Usage", "Low", "Pattern")
            };

            foreach (var pattern in architecturalPatterns)
            {
                if (analysis.ToLower().Contains(pattern.Key))
                {
                    additionalFindings.Add(new Finding
                    {
                        Title = pattern.Value.title,
                        Category = "Architecture",
                        Type = pattern.Value.type,
                        Severity = pattern.Value.severity,
                        Description = $"Architectural pattern detected: {pattern.Key}"
                    });
                }
            }

            return additionalFindings;
        }

        /// <summary>
        /// Generates architecture-specific recommendations
        /// </summary>
        private List<Recommendation> GenerateArchitectureRecommendations(List<Finding> findings, EnhancedContext context)
        {
            var recommendations = new List<Recommendation>();

            foreach (var finding in findings)
            {
                var recommendation = new Recommendation
                {
                    Title = $"Address {finding.Title}",
                    Category = "Architecture",
                    Priority = MapImpactToPriority(finding.Severity),
                    Description = GenerateArchitectureRecommendationText(finding),
                    EstimatedEffort = EstimateArchitecturalEffort(finding)
                };

                recommendations.Add(recommendation);
            }

            // Add proactive architecture recommendations
            recommendations.AddRange(GenerateProactiveArchitectureRecommendations(context));

            return recommendations;
        }

        /// <summary>
        /// Generates proactive architecture recommendations
        /// </summary>
        private List<Recommendation> GenerateProactiveArchitectureRecommendations(EnhancedContext context)
        {
            var recommendations = new List<Recommendation>();

            if (context.CodeComplexity > 8)
            {
                recommendations.Add(new Recommendation
                {
                    Title = "Consider Modular Architecture Refactoring",
                    Category = "Architecture",
                    Priority = "High",
                    Description = "High complexity suggests need for better modular design and separation of concerns",
                    EstimatedEffort = "High"
                });
            }

            if (context.ProjectType?.ToLower().Contains("microservice") == true)
            {
                recommendations.Add(new Recommendation
                {
                    Title = "Implement Service Mesh Patterns",
                    Category = "Architecture",
                    Priority = "Medium",
                    Description = "Consider service mesh for improved microservice communication and observability",
                    EstimatedEffort = "Medium"
                });
            }

            if (context.ProjectType?.ToLower().Contains("distributed") == true)
            {
                recommendations.Add(new Recommendation
                {
                    Title = "Implement Circuit Breaker Pattern",
                    Category = "Architecture",
                    Priority = "Medium",
                    Description = "Add circuit breaker for improved resilience in distributed systems",
                    EstimatedEffort = "Low"
                });
            }

            return recommendations;
        }

        /// <summary>
        /// Helper methods
        /// </summary>
        private string MapImpactToPriority(string impact)
        {
            return impact switch
            {
                "High" => "High",
                "Medium" => "Medium",
                "Low" => "Low",
                _ => "Medium"
            };
        }

        private string GenerateArchitectureRecommendationText(Finding finding)
        {
            return $"Address architectural concern: {finding.Title}. " +
                   $"Long-term implications: {finding.Impact}. Consider applying modern architectural patterns and principles.";
        }

        private string EstimateArchitecturalEffort(Finding finding)
        {
            return finding.Type switch
            {
                "Anti-Pattern" => "High",
                "Structure" => "Medium",
                "Pattern" => "Low",
                _ => "Medium"
            };
        }

        private double CalculateArchitectureConfidence(List<Finding> findings, string analysis, EnhancedContext context)
        {
            var baseConfidence = 0.85;
            
            // Increase confidence if architectural patterns are identified
            if (findings.Any(f => f.Type == "Pattern"))
                baseConfidence += 0.05;
            
            // Increase confidence if anti-patterns are identified
            if (findings.Any(f => f.Type == "Anti-Pattern"))
                baseConfidence += 0.05;
            
            // Increase confidence for complex code (more architectural considerations)
            if (context.CodeComplexity > 7)
                baseConfidence += 0.05;

            return Math.Min(1.0, Math.Max(0.7, baseConfidence));
        }
    }
}