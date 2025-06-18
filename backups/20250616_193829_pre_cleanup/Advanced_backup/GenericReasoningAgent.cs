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
    /// Generic reasoning agent that can adapt to any agent type with advanced reasoning capabilities
    /// Serves as a fallback for specialized agents and provides flexible analysis
    /// </summary>
    public class GenericReasoningAgent : ISpecializedReasoningAgent
    {
        private readonly IClaudeService _claudeService;
        private readonly AdvancedReasoningEngine _reasoningEngine;
        private readonly ILogger<GenericReasoningAgent> _logger;
        private readonly AgentType _agentType;

        public GenericReasoningAgent(
            IClaudeService claudeService,
            AdvancedReasoningEngine reasoningEngine,
            ILogger<GenericReasoningAgent> logger,
            AgentType agentType)
        {
            _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
            _reasoningEngine = reasoningEngine ?? throw new ArgumentNullException(nameof(reasoningEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _agentType = agentType;
        }

        /// <summary>
        /// Analyzes code with advanced reasoning adapted to the specific agent type
        /// </summary>
        public async Task<AgentResult> AnalyzeWithReasoningAsync(
            CodeReviewRequest request,
            EnhancedContext context,
            ReasoningMode mode,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting generic analysis for {AgentType} with reasoning mode: {Mode}", _agentType, mode);

            try
            {
                var analysisPrompt = BuildGenericAnalysisPrompt(request, context, _agentType);
                
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
                            analysisPrompt, reasoningContext, cancellationToken);
                        analysis = cotResult.Conclusion;
                        reasoningChain = cotResult.ReasoningSteps;
                        break;

                    case ReasoningMode.TreeOfThoughts:
                        var totResult = await _reasoningEngine.ConductTreeOfThoughtsAsync(
                            analysisPrompt, reasoningContext, TreeSearchStrategy.BreadthFirst, cancellationToken);
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
                            EnableToolIntegration = false, // Generic agent has limited tool access
                            ExtendedThinkingThreshold = 0.8
                        };
                        var hybridResult = await _reasoningEngine.ConductHybridReasoningAsync(
                            analysisPrompt, reasoningContext, hybridOptions, cancellationToken);
                        analysis = hybridResult.Synthesis.Content;
                        break;

                    default:
                        analysis = await _claudeService.GetCompletionAsync(analysisPrompt, cancellationToken);
                        break;
                }

                var findings = ExtractGenericFindings(analysis, _agentType);
                var recommendations = GenerateGenericRecommendations(findings, context, _agentType);
                var confidenceScore = CalculateGenericConfidence(findings, analysis, context);

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
                _logger.LogError(ex, "Generic analysis failed for agent type {AgentType}", _agentType);
                throw;
            }
        }

        /// <summary>
        /// Builds analysis prompt adapted to the specific agent type
        /// </summary>
        private string BuildGenericAnalysisPrompt(CodeReviewRequest request, EnhancedContext context, AgentType agentType)
        {
            var agentDescription = GetAgentDescription(agentType);
            var analysisFramework = GetAnalysisFramework(agentType);
            var focusAreas = GetFocusAreas(agentType);

            return $@"
You are a {agentDescription} conducting comprehensive code analysis.
Apply specialized knowledge and best practices relevant to your domain of expertise.

**Code Context:**
- File: {request.FileName}
- Language: {request.Language}
- Project Type: {context.ProjectType}
- Business Domain: {request.Options.BusinessDomain}
- Complexity: {context.CodeComplexity}/10

**Code to Analyze:**
{request.Content}

**Your Role:** {agentDescription}

**Analysis Framework:**
{analysisFramework}

**Key Focus Areas:**
{string.Join("\n", focusAreas.Select(area => $"- {area}"))}

**Analysis Requirements:**
- Provide domain-specific insights and recommendations
- Identify issues relevant to your area of expertise
- Consider both immediate and long-term implications
- Provide actionable guidance for developers
- Assess risk levels and prioritize findings

**Output Format:**
FINDING: [Issue Name]
CATEGORY: [{GetCategoryName(agentType)}]
SEVERITY: [Critical/High/Medium/Low]
DESCRIPTION: [Detailed analysis]
IMPACT: [Potential consequences]
RECOMMENDATION: [Specific action items]
---

Focus on issues most relevant to your domain expertise while considering the overall code quality and maintainability.
";
        }

        /// <summary>
        /// Gets agent description based on type
        /// </summary>
        private string GetAgentDescription(AgentType agentType)
        {
            return agentType switch
            {
                AgentType.SecurityExpert => "senior security expert specializing in vulnerability assessment and secure coding practices",
                AgentType.PerformanceAnalyst => "performance engineer focused on optimization, scalability, and system efficiency",
                AgentType.ArchitectureExpert => "software architect evaluating design patterns, system structure, and architectural decisions",
                AgentType.ArchitectureStandardsAgent => "enterprise architecture specialist ensuring compliance with organizational standards",
                AgentType.CodeQualityReviewer => "code quality specialist focused on maintainability, readability, and best practices",
                AgentType.TestingSpecialist => "testing expert evaluating test coverage, quality, and testing strategies",
                _ => "specialized code analysis expert with comprehensive review capabilities"
            };
        }

        /// <summary>
        /// Gets analysis framework based on agent type
        /// </summary>
        private string GetAnalysisFramework(AgentType agentType)
        {
            return agentType switch
            {
                AgentType.SecurityExpert => @"
1. **Vulnerability Assessment**: Identify security vulnerabilities and attack vectors
2. **Input Validation**: Check for proper sanitization and validation
3. **Authentication & Authorization**: Review access control mechanisms
4. **Data Protection**: Evaluate encryption and sensitive data handling
5. **Security Patterns**: Assess implementation of security best practices",

                AgentType.PerformanceAnalyst => @"
1. **Algorithmic Efficiency**: Analyze time and space complexity
2. **Resource Management**: Check memory usage and resource disposal
3. **I/O Operations**: Evaluate async patterns and blocking operations
4. **Scalability**: Assess performance under load and scaling considerations
5. **Optimization Opportunities**: Identify performance improvement areas",

                AgentType.ArchitectureExpert => @"
1. **Design Patterns**: Evaluate pattern usage and appropriateness
2. **SOLID Principles**: Check adherence to design principles
3. **Coupling & Cohesion**: Assess component relationships and organization
4. **Extensibility**: Evaluate flexibility and maintainability
5. **Integration Patterns**: Review component interaction and communication",

                AgentType.ArchitectureStandardsAgent => @"
1. **Organizational Standards**: Check compliance with enterprise guidelines
2. **Coding Conventions**: Verify adherence to style and naming standards
3. **Framework Usage**: Evaluate proper use of approved frameworks
4. **Documentation Standards**: Assess code documentation and comments
5. **Process Compliance**: Review alignment with development processes",

                AgentType.CodeQualityReviewer => @"
1. **Code Clarity**: Evaluate readability and understandability
2. **Maintainability**: Assess ease of modification and extension
3. **Code Smells**: Identify anti-patterns and problematic constructs
4. **Best Practices**: Check adherence to language-specific guidelines
5. **Technical Debt**: Evaluate areas requiring refactoring or improvement",

                AgentType.TestingSpecialist => @"
1. **Test Coverage**: Assess completeness of test scenarios
2. **Test Quality**: Evaluate test effectiveness and reliability
3. **Testability**: Check code design for testing ease
4. **Test Patterns**: Review testing patterns and practices
5. **Quality Assurance**: Assess overall testing strategy and implementation",

                _ => @"
1. **General Quality**: Assess overall code quality and practices
2. **Best Practices**: Check adherence to common coding standards
3. **Maintainability**: Evaluate long-term code sustainability
4. **Risk Assessment**: Identify potential issues and concerns
5. **Improvement Opportunities**: Suggest areas for enhancement"
            };
        }

        /// <summary>
        /// Gets focus areas based on agent type
        /// </summary>
        private List<string> GetFocusAreas(AgentType agentType)
        {
            return agentType switch
            {
                AgentType.SecurityExpert => new List<string>
                {
                    "Input validation and sanitization",
                    "Authentication and authorization",
                    "Data encryption and protection",
                    "Security vulnerabilities and threats",
                    "Secure coding practices"
                },
                AgentType.PerformanceAnalyst => new List<string>
                {
                    "Algorithm efficiency and complexity",
                    "Memory usage and management",
                    "I/O operations and async patterns",
                    "Database queries and data access",
                    "Scalability and load handling"
                },
                AgentType.ArchitectureExpert => new List<string>
                {
                    "Design patterns and principles",
                    "Component structure and organization",
                    "Dependency management",
                    "System architecture and integration",
                    "Extensibility and flexibility"
                },
                AgentType.ArchitectureStandardsAgent => new List<string>
                {
                    "Enterprise architecture compliance",
                    "Coding standards and conventions",
                    "Framework and library usage",
                    "Documentation and comments",
                    "Process and methodology adherence"
                },
                AgentType.CodeQualityReviewer => new List<string>
                {
                    "Code readability and clarity",
                    "Maintainability and modularity",
                    "Error handling and logging",
                    "Code organization and structure",
                    "Refactoring opportunities"
                },
                AgentType.TestingSpecialist => new List<string>
                {
                    "Test coverage and completeness",
                    "Test code quality and reliability",
                    "Testability and mock usage",
                    "Integration and unit testing",
                    "Test automation and CI/CD"
                },
                _ => new List<string>
                {
                    "General code quality assessment",
                    "Best practices compliance",
                    "Maintainability evaluation",
                    "Risk identification",
                    "Improvement recommendations"
                }
            };
        }

        /// <summary>
        /// Gets category name for findings
        /// </summary>
        private string GetCategoryName(AgentType agentType)
        {
            return agentType switch
            {
                AgentType.SecurityExpert => "Security",
                AgentType.PerformanceAnalyst => "Performance",
                AgentType.ArchitectureExpert => "Architecture",
                AgentType.ArchitectureStandardsAgent => "Standards",
                AgentType.CodeQualityReviewer => "Quality",
                AgentType.TestingSpecialist => "Testing",
                _ => "General"
            };
        }

        /// <summary>
        /// Extracts findings from analysis based on agent type
        /// </summary>
        private List<Finding> ExtractGenericFindings(string analysis, AgentType agentType)
        {
            var findings = new List<Finding>();
            var sections = analysis.Split("---", StringSplitOptions.RemoveEmptyEntries);

            foreach (var section in sections)
            {
                var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var finding = new Finding();

                foreach (var line in lines)
                {
                    if (line.StartsWith("FINDING:"))
                        finding.Title = line.Substring(8).Trim();
                    else if (line.StartsWith("CATEGORY:"))
                        finding.Category = line.Substring(9).Trim();
                    else if (line.StartsWith("SEVERITY:"))
                        finding.Severity = line.Substring(9).Trim();
                    else if (line.StartsWith("DESCRIPTION:"))
                        finding.Description = line.Substring(12).Trim();
                    else if (line.StartsWith("IMPACT:"))
                        finding.Impact = line.Substring(7).Trim();
                }

                if (!string.IsNullOrEmpty(finding.Title))
                {
                    finding.Category = finding.Category ?? GetCategoryName(agentType);
                    finding.Type = "Analysis";
                    findings.Add(finding);
                }
            }

            return findings;
        }

        /// <summary>
        /// Generates recommendations based on findings and agent type
        /// </summary>
        private List<Recommendation> GenerateGenericRecommendations(List<Finding> findings, EnhancedContext context, AgentType agentType)
        {
            var recommendations = new List<Recommendation>();

            foreach (var finding in findings)
            {
                var recommendation = new Recommendation
                {
                    Title = $"Address {finding.Title}",
                    Category = GetCategoryName(agentType),
                    Priority = finding.Severity ?? "Medium",
                    Description = $"Recommendation for {finding.Title}: {finding.Impact}",
                    EstimatedEffort = EstimateEffortForFinding(finding)
                };

                recommendations.Add(recommendation);
            }

            return recommendations;
        }

        /// <summary>
        /// Estimates effort for a finding
        /// </summary>
        private string EstimateEffortForFinding(Finding finding)
        {
            return finding.Severity switch
            {
                "Critical" => "High",
                "High" => "Medium",
                "Medium" => "Low",
                "Low" => "Low",
                _ => "Medium"
            };
        }

        /// <summary>
        /// Calculates confidence score for generic analysis
        /// </summary>
        private double CalculateGenericConfidence(List<Finding> findings, string analysis, EnhancedContext context)
        {
            var baseConfidence = 0.75; // Lower than specialized agents
            
            // Increase confidence if findings are identified
            if (findings.Any())
                baseConfidence += 0.1;
            
            // Increase confidence if analysis is comprehensive
            if (analysis.Length > 800)
                baseConfidence += 0.05;
            
            // Adjust based on code complexity
            if (context.CodeComplexity > 7)
                baseConfidence += 0.05;

            return Math.Min(1.0, Math.Max(0.6, baseConfidence));
        }
    }
}