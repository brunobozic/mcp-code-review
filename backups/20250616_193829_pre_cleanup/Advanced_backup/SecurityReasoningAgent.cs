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
    /// Specialized security reasoning agent with advanced threat analysis capabilities
    /// Implements 2025 security best practices and threat modeling
    /// </summary>
    public class SecurityReasoningAgent : ISpecializedReasoningAgent
    {
        private readonly IClaudeService _claudeService;
        private readonly AdvancedReasoningEngine _reasoningEngine;
        private readonly ILogger<SecurityReasoningAgent> _logger;

        public SecurityReasoningAgent(
            IClaudeService claudeService,
            AdvancedReasoningEngine reasoningEngine,
            ILogger<SecurityReasoningAgent> logger)
        {
            _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
            _reasoningEngine = reasoningEngine ?? throw new ArgumentNullException(nameof(reasoningEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Analyzes code with advanced security reasoning
        /// </summary>
        public async Task<AgentResult> AnalyzeWithReasoningAsync(
            CodeReviewRequest request,
            EnhancedContext context,
            ReasoningMode mode,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Starting security analysis with reasoning mode: {Mode}", mode);

            try
            {
                var securityPrompt = BuildSecurityAnalysisPrompt(request, context);
                
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
                            securityPrompt, reasoningContext, cancellationToken);
                        analysis = cotResult.Conclusion;
                        reasoningChain = cotResult.ReasoningSteps;
                        break;

                    case ReasoningMode.TreeOfThoughts:
                        var totResult = await _reasoningEngine.ConductTreeOfThoughtsAsync(
                            securityPrompt, reasoningContext, TreeSearchStrategy.BestFirst, cancellationToken);
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
                            ExtendedThinkingThreshold = 0.9
                        };
                        var hybridResult = await _reasoningEngine.ConductHybridReasoningAsync(
                            securityPrompt, reasoningContext, hybridOptions, cancellationToken);
                        analysis = hybridResult.Synthesis.Content;
                        break;

                    default:
                        analysis = await _claudeService.GetCompletionAsync(securityPrompt, cancellationToken);
                        break;
                }

                var findings = ExtractSecurityFindings(analysis);
                var recommendations = GenerateSecurityRecommendations(findings, context);
                var confidenceScore = CalculateSecurityConfidence(findings, analysis);

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
                _logger.LogError(ex, "Security analysis failed");
                throw;
            }
        }

        /// <summary>
        /// Builds comprehensive security analysis prompt
        /// </summary>
        private string BuildSecurityAnalysisPrompt(CodeReviewRequest request, EnhancedContext context)
        {
            return $@"
You are a senior security expert conducting a comprehensive security analysis.
Apply the latest 2025 security frameworks including OWASP Top 10, STRIDE threat modeling, and zero-trust principles.

**Code Context:**
- File: {request.FileName}
- Language: {request.Language}
- Project Type: {context.ProjectType}
- Business Domain: {request.Options.BusinessDomain}
- Complexity: {context.CodeComplexity}/10

**Code to Analyze:**
{request.Content}

**Security Analysis Framework:**

1. **Input Validation & Sanitization**
   - Check for SQL injection vulnerabilities
   - Verify XSS prevention measures
   - Validate command injection risks
   - Assess deserialization vulnerabilities

2. **Authentication & Authorization**
   - Review access control implementation
   - Check for privilege escalation risks
   - Validate session management
   - Assess identity verification mechanisms

3. **Data Protection**
   - Verify encryption at rest and in transit
   - Check for sensitive data exposure
   - Validate data masking/anonymization
   - Assess compliance with privacy regulations

4. **Infrastructure Security**
   - Review configuration security
   - Check for hardcoded secrets
   - Validate secure communication protocols
   - Assess deployment security

5. **Application Logic**
   - Identify business logic flaws
   - Check for race conditions
   - Validate error handling security
   - Assess logging and monitoring

**Analysis Requirements:**
- Identify specific vulnerabilities with CVE references where applicable
- Assess risk severity (Critical/High/Medium/Low)
- Consider attack vectors and exploitation scenarios
- Evaluate impact on confidentiality, integrity, and availability
- Provide actionable remediation guidance

**Output Format:**
VULNERABILITY: [Name]
SEVERITY: [Critical/High/Medium/Low]
DESCRIPTION: [Detailed description]
IMPACT: [Potential impact]
REMEDIATION: [Specific fix recommendations]
---

Focus on the most critical security risks that could lead to system compromise.
";
        }

        /// <summary>
        /// Extracts security findings from analysis
        /// </summary>
        private List<Finding> ExtractSecurityFindings(string analysis)
        {
            var findings = new List<Finding>();
            var sections = analysis.Split("---", StringSplitOptions.RemoveEmptyEntries);

            foreach (var section in sections)
            {
                var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var finding = new Finding();

                foreach (var line in lines)
                {
                    if (line.StartsWith("VULNERABILITY:"))
                        finding.Title = line.Substring(14).Trim();
                    else if (line.StartsWith("SEVERITY:"))
                        finding.Severity = line.Substring(9).Trim();
                    else if (line.StartsWith("DESCRIPTION:"))
                        finding.Description = line.Substring(12).Trim();
                    else if (line.StartsWith("IMPACT:"))
                        finding.Impact = line.Substring(7).Trim();
                }

                if (!string.IsNullOrEmpty(finding.Title))
                {
                    finding.Category = "Security";
                    finding.Type = "Vulnerability";
                    findings.Add(finding);
                }
            }

            // Add additional security checks
            findings.AddRange(PerformAdditionalSecurityChecks(analysis));

            return findings;
        }

        /// <summary>
        /// Performs additional automated security checks
        /// </summary>
        private List<Finding> PerformAdditionalSecurityChecks(string analysis)
        {
            var additionalFindings = new List<Finding>();

            // Check for common security anti-patterns
            var securityPatterns = new Dictionary<string, string>
            {
                ["hardcoded"] = "Hardcoded credentials or secrets detected",
                ["password"] = "Password handling concerns identified",
                ["token"] = "Token security issues found",
                ["encrypt"] = "Encryption implementation review needed",
                ["hash"] = "Hashing algorithm security assessment required",
                ["random"] = "Random number generation security review needed"
            };

            foreach (var pattern in securityPatterns)
            {
                if (analysis.ToLower().Contains(pattern.Key))
                {
                    additionalFindings.Add(new Finding
                    {
                        Title = pattern.Value,
                        Category = "Security",
                        Type = "Pattern",
                        Severity = "Medium",
                        Description = $"Security pattern requiring review: {pattern.Key}"
                    });
                }
            }

            return additionalFindings;
        }

        /// <summary>
        /// Generates security-specific recommendations
        /// </summary>
        private List<Recommendation> GenerateSecurityRecommendations(List<Finding> findings, EnhancedContext context)
        {
            var recommendations = new List<Recommendation>();

            foreach (var finding in findings)
            {
                var recommendation = new Recommendation
                {
                    Title = $"Address {finding.Title}",
                    Category = "Security",
                    Priority = MapSeverityToPriority(finding.Severity),
                    Description = GenerateSecurityRecommendationText(finding),
                    EstimatedEffort = EstimateSecurityFixEffort(finding)
                };

                recommendations.Add(recommendation);
            }

            // Add proactive security recommendations
            recommendations.AddRange(GenerateProactiveSecurityRecommendations(context));

            return recommendations;
        }

        /// <summary>
        /// Generates proactive security recommendations
        /// </summary>
        private List<Recommendation> GenerateProactiveSecurityRecommendations(EnhancedContext context)
        {
            var recommendations = new List<Recommendation>();

            if (context.CodeComplexity > 7)
            {
                recommendations.Add(new Recommendation
                {
                    Title = "Implement Security Code Review Process",
                    Category = "Security",
                    Priority = "High",
                    Description = "High complexity code requires enhanced security review process",
                    EstimatedEffort = "Medium"
                });
            }

            if (context.ProjectType?.ToLower().Contains("web") == true)
            {
                recommendations.Add(new Recommendation
                {
                    Title = "Implement Web Application Security Headers",
                    Category = "Security",
                    Priority = "Medium",
                    Description = "Add security headers (CSP, HSTS, X-Frame-Options) for web application protection",
                    EstimatedEffort = "Low"
                });
            }

            return recommendations;
        }

        /// <summary>
        /// Helper methods
        /// </summary>
        private string MapSeverityToPriority(string severity)
        {
            return severity switch
            {
                "Critical" => "Critical",
                "High" => "High",
                "Medium" => "Medium",
                "Low" => "Low",
                _ => "Medium"
            };
        }

        private string GenerateSecurityRecommendationText(Finding finding)
        {
            return $"Implement secure coding practices to address {finding.Title}. " +
                   $"Impact: {finding.Impact}. Consider applying defense-in-depth principles.";
        }

        private string EstimateSecurityFixEffort(Finding finding)
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

        private double CalculateSecurityConfidence(List<Finding> findings, string analysis)
        {
            var baseConfidence = 0.8;
            
            // Increase confidence if critical issues are identified
            if (findings.Any(f => f.Severity == "Critical"))
                baseConfidence += 0.1;
            
            // Increase confidence if analysis is comprehensive
            if (analysis.Length > 1000)
                baseConfidence += 0.05;
            
            // Decrease confidence if too few findings (might have missed issues)
            if (findings.Count < 2)
                baseConfidence -= 0.1;

            return Math.Min(1.0, Math.Max(0.5, baseConfidence));
        }
    }
}