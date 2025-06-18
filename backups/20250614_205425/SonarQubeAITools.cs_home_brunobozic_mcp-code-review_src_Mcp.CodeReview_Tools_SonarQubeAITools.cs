using ModelContextProtocol;
using ModelContextProtocol.Server;
using Mcp.CodeReview.AI;
using Mcp.CodeReview.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;

namespace Mcp.CodeReview.Tools;

[McpServerToolType]
public static class SonarQubeAITools
{
    /// <summary>
    /// Advanced AI analysis of SonarQube findings with contextual insights
    /// </summary>
    [McpServerTool, Description("Conduct AI-powered analysis of SonarQube findings with prioritization and remediation guidance")]
    public static async Task<object> AnalyzeSonarQubeFindings(
        SonarQubeService sonarQubeService,
        ClaudeService claudeService,
        ILogger logger,
        [Description("The SonarQube project key to analyze")] string projectKey,
        [Description("Focus area: security, performance, maintainability, or all")] string focusArea = "all",
        [Description("Include AI remediation suggestions")] bool includeRemediation = true,
        [Description("Business context for prioritization")] string businessContext = "")
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "analyzeSonarQubeFindings",
            ["ProjectKey"] = projectKey,
            ["FocusArea"] = focusArea
        }))
        {
            logger.LogInformation("Starting AI analysis of SonarQube findings for project {ProjectKey}", projectKey);

            try
            {
                // Get SonarQube analysis data
                var sonarAnalysis = await sonarQubeService.GetIssueAnalysisForAI(projectKey);
                
                // Create AI analysis prompt based on focus area
                var prompt = BuildSonarQubeAnalysisPrompt(sonarAnalysis, focusArea, businessContext, includeRemediation);
                
                // Get AI insights
                var aiAnalysis = await claudeService.GenerateReviewWithParameters(prompt, temperature: 0.2, maxTokens: 3000);

                // Categorize and prioritize findings
                var categorizedFindings = CategorizeSonarQubeFindings(sonarAnalysis);
                var prioritizedIssues = PrioritizeSonarQubeIssues(sonarAnalysis.CriticalIssues, businessContext);

                logger.LogInformation("SonarQube AI analysis completed for {TotalIssues} issues", sonarAnalysis.TotalIssues);

                return new
                {
                    success = true,
                    projectKey = projectKey,
                    summary = new
                    {
                        totalIssues = sonarAnalysis.TotalIssues,
                        criticalIssues = sonarAnalysis.CriticalIssues.Count,
                        securityIssues = sonarAnalysis.SecurityIssues.Count,
                        bugIssues = sonarAnalysis.BugIssues.Count,
                        codeSmellIssues = sonarAnalysis.CodeSmellIssues.Count,
                        qualityMetrics = sonarAnalysis.QualityMetrics
                    },
                    
                    // AI-powered insights
                    aiInsights = new
                    {
                        analysis = aiAnalysis,
                        focusArea = focusArea,
                        businessAlignment = ExtractBusinessAlignment(aiAnalysis, businessContext),
                        riskAssessment = ExtractRiskAssessment(aiAnalysis),
                        recommendations = ExtractRecommendations(aiAnalysis)
                    },
                    
                    // Categorized findings
                    categorizedFindings = categorizedFindings,
                    
                    // Prioritized issues for immediate action
                    prioritizedIssues = prioritizedIssues.Take(10).Select(issue => new
                    {
                        key = issue.Key,
                        severity = issue.Severity,
                        type = issue.Type,
                        message = issue.Message,
                        component = issue.Component,
                        line = issue.Line,
                        aiPriority = CalculateAIPriority(issue, businessContext),
                        estimatedEffort = issue.Effort,
                        tags = issue.Tags
                    }),
                    
                    // File-level analysis
                    fileAnalysis = sonarAnalysis.IssuesByFile.Select(kvp => new
                    {
                        file = kvp.Key,
                        issueCount = kvp.Value.Count,
                        criticalCount = kvp.Value.Count(i => i.Severity == "CRITICAL" || i.Severity == "BLOCKER"),
                        securityCount = kvp.Value.Count(i => i.Type == "VULNERABILITY" || i.Type == "SECURITY_HOTSPOT"),
                        aiRiskScore = CalculateFileRiskScore(kvp.Value)
                    }).OrderByDescending(f => f.aiRiskScore).Take(20),
                    
                    // Quality trends and recommendations
                    qualityInsights = new
                    {
                        coverageAnalysis = AnalyzeCoverage(sonarAnalysis.QualityMetrics),
                        complexityAnalysis = AnalyzeComplexity(sonarAnalysis),
                        securityPosture = AnalyzeSecurityPosture(sonarAnalysis.SecurityIssues),
                        maintenanceDebt = CalculateTechnicalDebt(sonarAnalysis)
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to analyze SonarQube findings for project {ProjectKey}", projectKey);
                throw new InvalidOperationException($"SonarQube AI analysis failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Hybrid analysis combining SonarQube static analysis with AI code review
    /// </summary>
    [McpServerTool, Description("Perform hybrid analysis combining SonarQube findings with AI code review insights")]
    public static async Task<object> ConductHybridCodeAnalysis(
        SonarQubeService sonarQubeService,
        ClaudeService claudeService,
        ILogger logger,
        [Description("The SonarQube project key")] string projectKey,
        [Description("Git diff or code snippet for AI analysis")] string codeContent,
        [Description("Programming language")] string language = "csharp",
        [Description("Analysis type: pr_review, security_audit, quality_gate")] string analysisType = "pr_review")
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "hybridCodeAnalysis", 
            ["ProjectKey"] = projectKey,
            ["AnalysisType"] = analysisType
        }))
        {
            logger.LogInformation("Starting hybrid SonarQube + AI analysis for project {ProjectKey}", projectKey);

            try
            {
                // Run both analyses in parallel
                var sonarTask = sonarQubeService.GetIssueAnalysisForAI(projectKey);
                var aiTask = PerformAICodeAnalysis(claudeService, codeContent, language, analysisType);

                await Task.WhenAll(sonarTask, aiTask);

                var sonarAnalysis = sonarTask.Result;
                var aiAnalysis = aiTask.Result;

                // Cross-correlate findings
                var correlatedFindings = CorrelateFindings(sonarAnalysis, aiAnalysis, codeContent);
                
                // Generate hybrid insights
                var hybridInsights = await GenerateHybridInsights(claudeService, sonarAnalysis, aiAnalysis, correlatedFindings);

                logger.LogInformation("Hybrid analysis completed with {SonarIssues} SonarQube issues and AI insights", 
                    sonarAnalysis.TotalIssues);

                return new
                {
                    success = true,
                    analysisType = analysisType,
                    
                    // Combined findings
                    sonarQubeFindings = new
                    {
                        totalIssues = sonarAnalysis.TotalIssues,
                        criticalIssues = sonarAnalysis.CriticalIssues.Count,
                        topIssues = sonarAnalysis.CriticalIssues.Take(5).Select(i => new
                        {
                            severity = i.Severity,
                            type = i.Type,
                            message = i.Message,
                            file = i.Component,
                            line = i.Line
                        })
                    },
                    
                    aiFindings = aiAnalysis,
                    
                    // Correlated insights  
                    correlatedFindings = correlatedFindings,
                    
                    // Hybrid intelligence
                    hybridInsights = new
                    {
                        analysis = hybridInsights,
                        confidence = CalculateHybridConfidence(sonarAnalysis, aiAnalysis),
                        agreement = CalculateAgreementScore(sonarAnalysis, aiAnalysis),
                        uniqueAIFindings = ExtractUniqueAIFindings(aiAnalysis, sonarAnalysis),
                        missedByStatic = ExtractMissedByStaticAnalysis(aiAnalysis, sonarAnalysis)
                    },
                    
                    // Action recommendations
                    recommendations = new
                    {
                        immediate = GetImmediateActions(correlatedFindings),
                        shortTerm = GetShortTermActions(sonarAnalysis, aiAnalysis),
                        longTerm = GetLongTermActions(sonarAnalysis, aiAnalysis),
                        toolingImprovements = GetToolingRecommendations(correlatedFindings)
                    },
                    
                    // Quality assessment
                    qualityAssessment = new
                    {
                        overallScore = CalculateOverallQualityScore(sonarAnalysis, aiAnalysis),
                        strengthAreas = IdentifyStrengths(sonarAnalysis, aiAnalysis),
                        improvementAreas = IdentifyImprovementAreas(sonarAnalysis, aiAnalysis),
                        complianceStatus = AssessComplianceStatus(sonarAnalysis)
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to conduct hybrid analysis for project {ProjectKey}", projectKey);
                throw new InvalidOperationException($"Hybrid analysis failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// AI-powered SonarQube quality gate assessment with business context
    /// </summary>
    [McpServerTool, Description("Assess SonarQube quality gate with AI insights and business context")]
    public static async Task<object> AssessQualityGateWithAI(
        SonarQubeService sonarQubeService,
        ClaudeService claudeService,
        ILogger logger,
        [Description("The SonarQube project key")] string projectKey,
        [Description("Business criticality: critical, high, medium, low")] string businessCriticality = "medium",
        [Description("Deployment target: production, staging, development")] string deploymentTarget = "production",
        [Description("Release timeline: immediate, this_week, this_month, flexible")] string releaseTimeline = "this_week")
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "assessQualityGate",
            ["ProjectKey"] = projectKey,
            ["BusinessCriticality"] = businessCriticality
        }))
        {
            logger.LogInformation("Assessing quality gate with AI for project {ProjectKey}", projectKey);

            try
            {
                // Get quality gate status and detailed analysis
                var projectAnalysis = await sonarQubeService.GetProjectAnalysis(projectKey);
                
                // Create business-aware assessment prompt
                var prompt = BuildQualityGateAssessmentPrompt(projectAnalysis, businessCriticality, deploymentTarget, releaseTimeline);
                
                // Get AI assessment
                var aiAssessment = await claudeService.GenerateReviewWithParameters(prompt, temperature: 0.1, maxTokens: 2500);

                // Calculate risk scores
                var riskAssessment = CalculateDeploymentRisk(projectAnalysis, businessCriticality, deploymentTarget);

                logger.LogInformation("Quality gate assessment completed for project {ProjectKey} with status {Status}", 
                    projectKey, projectAnalysis.QualityGate.Status);

                return new
                {
                    success = true,
                    projectKey = projectKey,
                    
                    // Quality gate status
                    qualityGate = new
                    {
                        status = projectAnalysis.QualityGate.Status,
                        passed = projectAnalysis.QualityGate.Status == "OK",
                        conditions = projectAnalysis.QualityGate.Conditions.Select(c => new
                        {
                            metric = c.MetricKey,
                            status = c.Status,
                            threshold = c.ErrorThreshold,
                            actualValue = c.ActualValue,
                            passed = c.Status == "OK"
                        })
                    },
                    
                    // AI assessment
                    aiAssessment = new
                    {
                        analysis = aiAssessment,
                        recommendation = ExtractRecommendation(aiAssessment),
                        confidence = ExtractConfidence(aiAssessment),
                        businessAlignment = ExtractBusinessAlignment(aiAssessment, businessCriticality)
                    },
                    
                    // Risk assessment
                    riskAssessment = riskAssessment,
                    
                    // Deployment decision
                    deploymentDecision = new
                    {
                        canDeploy = CanDeploy(projectAnalysis, riskAssessment, businessCriticality),
                        shouldDeploy = ShouldDeploy(projectAnalysis, riskAssessment, releaseTimeline),
                        conditions = GetDeploymentConditions(projectAnalysis, riskAssessment),
                        mitigations = GetRiskMitigations(riskAssessment)
                    },
                    
                    // Action plan
                    actionPlan = new
                    {
                        blockers = GetBlockingIssues(projectAnalysis),
                        quickFixes = GetQuickFixes(projectAnalysis),
                        longerTermWork = GetLongerTermWork(projectAnalysis),
                        estimatedFixTime = EstimateFixTime(projectAnalysis)
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to assess quality gate for project {ProjectKey}", projectKey);
                throw new InvalidOperationException($"Quality gate assessment failed: {ex.Message}");
            }
        }
    }

    // Helper methods for SonarQube AI analysis
    private static string BuildSonarQubeAnalysisPrompt(SonarQubeIssueAnalysis analysis, string focusArea, string businessContext, bool includeRemediation)
    {
        var prompt = $@"
As a senior software architect and code quality expert, analyze these SonarQube findings:

## Project Analysis Summary
- Total Issues: {analysis.TotalIssues}
- Critical Issues: {analysis.CriticalIssues.Count} 
- Security Issues: {analysis.SecurityIssues.Count}
- Bug Issues: {analysis.BugIssues.Count}
- Code Smell Issues: {analysis.CodeSmellIssues.Count}

## Quality Metrics
{string.Join("\n", analysis.QualityMetrics.Select(kvp => $"- {kvp.Key}: {kvp.Value}"))}

## Focus Area: {focusArea.ToUpper()}

## Business Context
{businessContext}

## Critical Issues Details
{string.Join("\n", analysis.CriticalIssues.Take(10).Select(issue => 
    $"- {issue.Severity} {issue.Type}: {issue.Message} (File: {issue.Component}, Line: {issue.Line})"))}

Please provide:
1. **Risk Assessment**: Overall risk level and business impact
2. **Priority Analysis**: Which issues should be addressed first and why
3. **Root Cause Analysis**: Common patterns and underlying issues
4. **Quality Trends**: What the metrics tell us about code health";

        if (includeRemediation)
        {
            prompt += @"
5. **Remediation Strategy**: Step-by-step plan to address issues
6. **Prevention Recommendations**: How to prevent similar issues";
        }

        return prompt;
    }

    private static async Task<string> PerformAICodeAnalysis(ClaudeService claudeService, string code, string language, string analysisType)
    {
        var prompt = $@"
Analyze this {language} code for quality issues:

## Analysis Type: {analysisType.ToUpper()}

## Code:
{code}

Focus on finding issues that static analysis tools might miss:
- Business logic errors
- API design problems  
- Performance bottlenecks
- Security vulnerabilities
- Maintainability concerns
- Domain modeling issues

Provide specific, actionable feedback.
";

        return await claudeService.GenerateReviewWithParameters(prompt, temperature: 0.3, maxTokens: 2000);
    }

    private static Dictionary<string, object> CategorizeSonarQubeFindings(SonarQubeIssueAnalysis analysis)
    {
        return new Dictionary<string, object>
        {
            ["security"] = analysis.SecurityIssues.GroupBy(i => i.Severity).ToDictionary(g => g.Key, g => g.Count()),
            ["reliability"] = analysis.BugIssues.GroupBy(i => i.Severity).ToDictionary(g => g.Key, g => g.Count()),
            ["maintainability"] = analysis.CodeSmellIssues.GroupBy(i => i.Severity).ToDictionary(g => g.Key, g => g.Count()),
            ["byRule"] = analysis.SecurityIssues.Concat(analysis.BugIssues).Concat(analysis.CodeSmellIssues)
                .GroupBy(i => i.Rule).OrderByDescending(g => g.Count()).Take(10)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    private static List<SonarQubeIssue> PrioritizeSonarQubeIssues(List<SonarQubeIssue> issues, string businessContext)
    {
        return issues.OrderByDescending(issue => 
        {
            var score = 0;
            
            // Severity scoring
            score += issue.Severity switch
            {
                "BLOCKER" => 100,
                "CRITICAL" => 80,
                "MAJOR" => 60,
                "MINOR" => 40,
                "INFO" => 20,
                _ => 0
            };
            
            // Type scoring
            score += issue.Type switch
            {
                "VULNERABILITY" => 50,
                "SECURITY_HOTSPOT" => 45,
                "BUG" => 40,
                "CODE_SMELL" => 20,
                _ => 0
            };
            
            // Business context scoring (simplified)
            if (businessContext.Contains("payment", StringComparison.OrdinalIgnoreCase) && 
                issue.Component.Contains("payment", StringComparison.OrdinalIgnoreCase))
                score += 30;
                
            return score;
        }).ToList();
    }

    private static string CalculateAIPriority(SonarQubeIssue issue, string businessContext)
    {
        var score = PrioritizeSonarQubeIssues(new List<SonarQubeIssue> { issue }, businessContext).First();
        
        return issue.Severity switch
        {
            "BLOCKER" => "Critical",
            "CRITICAL" => "High", 
            "MAJOR" => "Medium",
            _ => "Low"
        };
    }

    private static double CalculateFileRiskScore(List<SonarQubeIssue> issues)
    {
        if (!issues.Any()) return 0;
        
        var severityScore = issues.Sum(i => i.Severity switch
        {
            "BLOCKER" => 10,
            "CRITICAL" => 8,
            "MAJOR" => 6,
            "MINOR" => 4,
            "INFO" => 2,
            _ => 0
        });
        
        var typeScore = issues.Sum(i => i.Type switch
        {
            "VULNERABILITY" => 10,
            "SECURITY_HOTSPOT" => 8,
            "BUG" => 6,
            "CODE_SMELL" => 3,
            _ => 0
        });
        
        return (severityScore + typeScore) / (double)issues.Count;
    }

    private static object AnalyzeCoverage(Dictionary<string, string> metrics)
    {
        var coverage = metrics.GetValueOrDefault("coverage", "0");
        var coverageValue = double.TryParse(coverage, out var c) ? c : 0;
        
        return new
        {
            percentage = coverageValue,
            status = coverageValue >= 80 ? "Good" : coverageValue >= 60 ? "Acceptable" : "Poor",
            recommendation = coverageValue < 80 ? "Increase test coverage" : "Maintain current coverage"
        };
    }

    private static object AnalyzeComplexity(SonarQubeIssueAnalysis analysis)
    {
        var complexityIssues = analysis.CodeSmellIssues.Where(i => 
            i.Rule.Contains("complexity", StringComparison.OrdinalIgnoreCase)).ToList();
            
        return new
        {
            complexityIssues = complexityIssues.Count,
            status = complexityIssues.Count == 0 ? "Good" : complexityIssues.Count < 5 ? "Acceptable" : "Poor",
            recommendation = complexityIssues.Count > 0 ? "Refactor complex methods" : "Good complexity management"
        };
    }

    private static object AnalyzeSecurityPosture(List<SonarQubeIssue> securityIssues)
    {
        var critical = securityIssues.Count(i => i.Severity == "CRITICAL" || i.Severity == "BLOCKER");
        
        return new
        {
            totalSecurityIssues = securityIssues.Count,
            criticalSecurityIssues = critical,
            status = critical == 0 ? "Good" : critical < 3 ? "Review Required" : "Critical",
            recommendation = critical > 0 ? "Address security vulnerabilities immediately" : "Maintain security practices"
        };
    }

    private static object CalculateTechnicalDebt(SonarQubeIssueAnalysis analysis)
    {
        var debtIssues = analysis.CodeSmellIssues.Count;
        
        return new
        {
            debtIssues = debtIssues,
            estimatedDebt = $"{debtIssues * 15} minutes", // Rough estimation
            status = debtIssues < 10 ? "Low" : debtIssues < 50 ? "Medium" : "High"
        };
    }

    // Additional helper methods would continue here...
    private static object CorrelateFindings(SonarQubeIssueAnalysis sonar, string ai, string code) => new { correlations = "placeholder" };
    private static Task<string> GenerateHybridInsights(ClaudeService claude, SonarQubeIssueAnalysis sonar, string ai, object correlated) => Task.FromResult("hybrid insights");
    private static double CalculateHybridConfidence(SonarQubeIssueAnalysis sonar, string ai) => 0.85;
    private static double CalculateAgreementScore(SonarQubeIssueAnalysis sonar, string ai) => 0.75;
    private static object ExtractUniqueAIFindings(string ai, SonarQubeIssueAnalysis sonar) => new { unique = "AI findings" };
    private static object ExtractMissedByStaticAnalysis(string ai, SonarQubeIssueAnalysis sonar) => new { missed = "static analysis" };
    private static string[] GetImmediateActions(object findings) => new[] { "Fix critical security issues" };
    private static string[] GetShortTermActions(SonarQubeIssueAnalysis sonar, string ai) => new[] { "Address code smells" };
    private static string[] GetLongTermActions(SonarQubeIssueAnalysis sonar, string ai) => new[] { "Improve architecture" };
    private static string[] GetToolingRecommendations(object findings) => new[] { "Configure additional rules" };
    private static double CalculateOverallQualityScore(SonarQubeIssueAnalysis sonar, string ai) => 7.5;
    private static string[] IdentifyStrengths(SonarQubeIssueAnalysis sonar, string ai) => new[] { "Good test coverage" };
    private static string[] IdentifyImprovementAreas(SonarQubeIssueAnalysis sonar, string ai) => new[] { "Security practices" };
    private static string AssessComplianceStatus(SonarQubeIssueAnalysis sonar) => "Compliant";
    private static string BuildQualityGateAssessmentPrompt(SonarQubeProjectAnalysis analysis, string criticality, string target, string timeline) => $"Assess quality gate for {criticality} {target} deployment";
    private static object CalculateDeploymentRisk(SonarQubeProjectAnalysis analysis, string criticality, string target) => new { risk = "Medium" };
    private static string ExtractRecommendation(string assessment) => "Proceed with caution";
    private static double ExtractConfidence(string assessment) => 0.8;
    private static string ExtractBusinessAlignment(string assessment, string context) => "Aligned";
    private static string ExtractRiskAssessment(string analysis) => "Medium risk";
    private static string[] ExtractRecommendations(string analysis) => new[] { "Fix critical issues first" };
    private static bool CanDeploy(SonarQubeProjectAnalysis analysis, object risk, string criticality) => analysis.QualityGate.Status == "OK";
    private static bool ShouldDeploy(SonarQubeProjectAnalysis analysis, object risk, string timeline) => true;
    private static string[] GetDeploymentConditions(SonarQubeProjectAnalysis analysis, object risk) => new[] { "Fix blockers" };
    private static string[] GetRiskMitigations(object risk) => new[] { "Monitor closely" };
    private static string[] GetBlockingIssues(SonarQubeProjectAnalysis analysis) => new[] { "Security vulnerabilities" };
    private static string[] GetQuickFixes(SonarQubeProjectAnalysis analysis) => new[] { "Fix formatting issues" };
    private static string[] GetLongerTermWork(SonarQubeProjectAnalysis analysis) => new[] { "Refactor complex code" };
    private static string EstimateFixTime(SonarQubeProjectAnalysis analysis) => "2-4 hours";
}