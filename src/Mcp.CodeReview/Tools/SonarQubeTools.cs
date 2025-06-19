using ModelContextProtocol;
using ModelContextProtocol.Server;
using Mcp.CodeReview.AI;
using Mcp.CodeReview.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;

namespace Mcp.CodeReview.Tools;

[McpServerToolType]
public static class SonarQubeTools
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
                var aiAnalysis = await claudeService.GenerateReviewWithParametersAsync(prompt, temperature: 0.2, maxTokens: 3000);

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
                var aiAssessment = await claudeService.GenerateReviewWithParametersAsync(prompt, temperature: 0.1, maxTokens: 2500);

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

    /// <summary>
    /// AI-powered SonarQube rule optimization and customization
    /// </summary>
    [McpServerTool, Description("Optimize SonarQube rules with AI recommendations for project-specific quality standards")]
    public static async Task<object> OptimizeSonarQubeRules(
        SonarQubeService sonarQubeService,
        ClaudeService claudeService,
        ILogger logger,
        [Description("The SonarQube project key to analyze")] string projectKey,
        [Description("Code domain: web_api, microservices, desktop, mobile, embedded")] string codeDomain = "web_api",
        [Description("Team experience level: junior, mixed, senior")] string teamExperience = "mixed",
        [Description("Business criticality: low, medium, high, critical")] string businessCriticality = "medium",
        [Description("Include custom rule suggestions")] bool includeCustomRules = true)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "optimizeSonarQubeRules",
            ["ProjectKey"] = projectKey,
            ["CodeDomain"] = codeDomain,
            ["TeamExperience"] = teamExperience
        }))
        {
            logger.LogInformation("Starting SonarQube rule optimization for project {ProjectKey}", projectKey);

            try
            {
                // Get current project analysis and issues
                var projectAnalysis = await sonarQubeService.GetProjectAnalysis(projectKey);
                var issuePatterns = AnalyzeIssuePatterns(projectAnalysis.Issues);
                
                // Create AI analysis prompt for rule optimization
                var optimizationPrompt = BuildRuleOptimizationPrompt(
                    projectAnalysis, issuePatterns, codeDomain, teamExperience, businessCriticality, includeCustomRules);
                
                // Get AI recommendations
                var aiRecommendations = await claudeService.GenerateReviewWithParametersAsync(optimizationPrompt, temperature: 0.2, maxTokens: 4000);

                // Analyze current rule effectiveness
                var ruleEffectiveness = AnalyzeRuleEffectiveness(projectAnalysis.Issues);
                
                // Generate rule recommendations
                var ruleRecommendations = GenerateRuleRecommendations(issuePatterns, codeDomain, teamExperience);

                logger.LogInformation("Rule optimization completed with {RuleCount} recommendations", ruleRecommendations.Count);

                return new
                {
                    success = true,
                    projectKey = projectKey,
                    
                    // Current rule analysis
                    currentRuleAnalysis = new
                    {
                        totalRulesTriggered = projectAnalysis.Issues.Select(i => i.Rule).Distinct().Count(),
                        mostTriggeredRules = projectAnalysis.Issues.GroupBy(i => i.Rule)
                            .OrderByDescending(g => g.Count())
                            .Take(10)
                            .Select(g => new { rule = g.Key, count = g.Count() }),
                        ruleEffectiveness = ruleEffectiveness,
                        noiseLevel = CalculateNoiseLevel(projectAnalysis.Issues)
                    },
                    
                    // AI recommendations
                    aiRecommendations = new
                    {
                        analysis = aiRecommendations,
                        strategicApproach = ExtractStrategicApproach(aiRecommendations),
                        priorityAdjustments = ExtractPriorityAdjustments(aiRecommendations),
                        teamCustomizations = ExtractTeamCustomizations(aiRecommendations),
                        businessAlignment = ExtractBusinessAlignment(aiRecommendations, "")
                    },
                    
                    // Rule optimization recommendations
                    ruleOptimizations = new
                    {
                        rulesToDisable = IdentifyRulesToDisable(issuePatterns, teamExperience),
                        rulesToEmphasize = IdentifyRulesToEmphasize(issuePatterns, businessCriticality),
                        severityAdjustments = RecommendSeverityAdjustments(issuePatterns, codeDomain),
                        qualityProfileSuggestions = GenerateQualityProfileSuggestions(codeDomain, teamExperience)
                    },
                    
                    // Custom rule suggestions
                    customRules = includeCustomRules ? new
                    {
                        projectSpecificRules = GenerateProjectSpecificRules(projectAnalysis, codeDomain),
                        domainSpecificRules = GenerateDomainSpecificRules(codeDomain),
                        teamSpecificRules = GenerateTeamSpecificRules(teamExperience),
                        businessSpecificRules = GenerateBusinessSpecificRules(businessCriticality)
                    } : null,
                    
                    // Implementation guidance
                    implementationGuidance = new
                    {
                        phaseApproach = GeneratePhaseApproach(ruleRecommendations),
                        teamCommunication = GenerateTeamCommunicationPlan(teamExperience, aiRecommendations),
                        rolloutStrategy = GenerateRolloutStrategy(businessCriticality),
                        successMetrics = DefineSuccessMetrics(projectAnalysis)
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to optimize SonarQube rules for project {ProjectKey}", projectKey);
                throw new InvalidOperationException($"SonarQube rule optimization failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Generate AI-powered custom SonarQube rules for specific code patterns
    /// </summary>
    [McpServerTool, Description("Generate custom SonarQube rules using AI analysis of code patterns and business requirements")]
    public static async Task<object> GenerateCustomSonarQubeRules(
        SonarQubeService sonarQubeService,
        ClaudeService claudeService,
        ILogger logger,
        [Description("The SonarQube project key to analyze")] string projectKey,
        [Description("Code patterns to focus on (comma-separated)")] string codePatterns = "security,performance,maintainability",
        [Description("Business domain context")] string businessDomain = "e-commerce",
        [Description("Compliance requirements")] string complianceRequirements = "",
        [Description("Rule complexity level: simple, intermediate, advanced")] string complexityLevel = "intermediate")
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "generateCustomSonarQubeRules",
            ["ProjectKey"] = projectKey,
            ["BusinessDomain"] = businessDomain
        }))
        {
            logger.LogInformation("Generating custom SonarQube rules for project {ProjectKey} in domain {Domain}", 
                projectKey, businessDomain);

            try
            {
                // Analyze project code patterns
                var projectAnalysis = await sonarQubeService.GetProjectAnalysis(projectKey);
                var codePatternAnalysis = AnalyzeCodePatterns(projectAnalysis, codePatterns.Split(','));
                
                // Create AI prompt for custom rule generation
                var ruleGenerationPrompt = BuildCustomRulePrompt(
                    projectAnalysis, codePatternAnalysis, businessDomain, complianceRequirements, complexityLevel);
                
                // Get AI rule suggestions
                var aiRuleSuggestions = await claudeService.GenerateReviewWithParametersAsync(ruleGenerationPrompt, temperature: 0.3, maxTokens: 5000);

                // Generate rule specifications
                var customRules = GenerateCustomRuleSpecs(codePatternAnalysis, businessDomain, complexityLevel);

                logger.LogInformation("Generated {RuleCount} custom rule specifications", customRules.Count);

                return new
                {
                    success = true,
                    projectKey = projectKey,
                    businessDomain = businessDomain,
                    
                    // Pattern analysis
                    patternAnalysis = new
                    {
                        identifiedPatterns = codePatternAnalysis.Patterns.Select(p => new
                        {
                            pattern = p.Name,
                            frequency = p.Frequency,
                            riskLevel = p.RiskLevel,
                            businessImpact = p.BusinessImpact
                        }),
                        antiPatterns = codePatternAnalysis.AntiPatterns,
                        businessRisks = codePatternAnalysis.BusinessRisks
                    },
                    
                    // AI rule suggestions
                    aiRuleSuggestions = new
                    {
                        analysis = aiRuleSuggestions,
                        strategicRules = ExtractStrategicRules(aiRuleSuggestions),
                        technicalRules = ExtractTechnicalRules(aiRuleSuggestions),
                        businessRules = ExtractBusinessRules(aiRuleSuggestions),
                        complianceRules = ExtractComplianceRules(aiRuleSuggestions)
                    },
                    
                    // Custom rule specifications
                    customRules = customRules.Select(rule => new
                    {
                        name = rule.Name,
                        description = rule.Description,
                        severity = rule.Severity,
                        category = rule.Category,
                        pattern = rule.Pattern,
                        justification = rule.Justification,
                        implementation = new
                        {
                            language = rule.Language,
                            ruleKey = rule.RuleKey,
                            implementation_approach = rule.ImplementationApproach,
                            test_cases = rule.TestCases
                        }
                    }),
                    
                    // Implementation guidance
                    implementationGuidance = new
                    {
                        priorityOrder = GetRulePriorityOrder(customRules),
                        rolloutPlan = GenerateRuleRolloutPlan(customRules, complexityLevel),
                        testing_strategy = GenerateRuleTestingStrategy(customRules),
                        maintenance_plan = GenerateRuleMaintenancePlan(customRules)
                    },
                    
                    // Quality impact assessment
                    qualityImpact = new
                    {
                        expectedImprovements = AssessExpectedImprovements(customRules, projectAnalysis),
                        riskMitigation = AssessRiskMitigation(customRules, codePatternAnalysis),
                        teamProductivity = AssessProductivityImpact(customRules, complexityLevel),
                        businessValue = AssessBusinessValue(customRules, businessDomain)
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate custom rules for project {ProjectKey}", projectKey);
                throw new InvalidOperationException($"Custom rule generation failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Analyze and optimize SonarQube quality gates with AI insights
    /// </summary>
    [McpServerTool, Description("Optimize SonarQube quality gates with AI-powered threshold recommendations")]
    public static async Task<object> OptimizeQualityGates(
        SonarQubeService sonarQubeService,
        ClaudeService claudeService,
        ILogger logger,
        [Description("The SonarQube project key to analyze")] string projectKey,
        [Description("Release frequency: daily, weekly, monthly, quarterly")] string releaseFrequency = "weekly",
        [Description("Risk tolerance: low, medium, high")] string riskTolerance = "medium",
        [Description("Team maturity level: developing, mature, expert")] string teamMaturity = "mature")
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "optimizeQualityGates",
            ["ProjectKey"] = projectKey,
            ["ReleaseFrequency"] = releaseFrequency
        }))
        {
            logger.LogInformation("Optimizing quality gates for project {ProjectKey}", projectKey);

            try
            {
                // Get current quality gate status and project analysis
                var projectAnalysis = await sonarQubeService.GetProjectAnalysis(projectKey);
                var qualityGate = projectAnalysis.QualityGate;
                
                // Analyze historical quality gate performance
                var gatePerformance = AnalyzeQualityGatePerformance(projectAnalysis, releaseFrequency);
                
                // Create AI optimization prompt
                var optimizationPrompt = BuildQualityGateOptimizationPrompt(
                    projectAnalysis, gatePerformance, releaseFrequency, riskTolerance, teamMaturity);
                
                // Get AI recommendations
                var aiOptimization = await claudeService.GenerateReviewWithParametersAsync(optimizationPrompt, temperature: 0.2, maxTokens: 3500);

                // Generate optimized thresholds
                var optimizedGates = GenerateOptimizedQualityGates(projectAnalysis, releaseFrequency, riskTolerance, teamMaturity);

                logger.LogInformation("Quality gate optimization completed with {GateCount} configurations", optimizedGates.Count);

                return new
                {
                    success = true,
                    projectKey = projectKey,
                    
                    // Current quality gate analysis
                    currentQualityGate = new
                    {
                        status = qualityGate.Status,
                        conditions = qualityGate.Conditions.Select(c => new
                        {
                            metric = c.MetricKey,
                            threshold = c.ErrorThreshold,
                            actual = c.ActualValue,
                            status = c.Status
                        }),
                        effectiveness = gatePerformance.Effectiveness,
                        blockageRate = gatePerformance.BlockageRate
                    },
                    
                    // AI optimization insights
                    aiOptimization = new
                    {
                        analysis = aiOptimization,
                        strategicApproach = ExtractStrategicApproach(aiOptimization),
                        thresholdRecommendations = ExtractThresholdRecommendations(aiOptimization),
                        riskAssessment = ExtractRiskAssessment(aiOptimization),
                        teamGuidance = ExtractTeamGuidance(aiOptimization)
                    },
                    
                    // Optimized quality gate configurations
                    optimizedGates = optimizedGates.Select(gate => new
                    {
                        name = gate.Name,
                        description = gate.Description,
                        suitableFor = gate.SuitableFor,
                        conditions = gate.Conditions.Select(c => new
                        {
                            metric = c.Metric,
                            threshold = c.Threshold,
                            justification = c.Justification
                        }),
                        expectedImpact = gate.ExpectedImpact
                    }),
                    
                    // Implementation strategy
                    implementationStrategy = new
                    {
                        migrationPlan = GenerateMigrationPlan(qualityGate, optimizedGates, teamMaturity),
                        pilotApproach = GeneratePilotApproach(optimizedGates),
                        rollbackStrategy = GenerateRollbackStrategy(qualityGate),
                        successCriteria = DefineQualityGateSuccessCriteria(releaseFrequency, riskTolerance)
                    },
                    
                    // Performance predictions
                    performancePredictions = new
                    {
                        expectedBlockageReduction = PredictBlockageReduction(gatePerformance, optimizedGates),
                        qualityImprovementProjection = PredictQualityImprovement(projectAnalysis, optimizedGates),
                        teamProductivityImpact = PredictProductivityImpact(optimizedGates, teamMaturity),
                        businessValueProjection = PredictBusinessValue(optimizedGates, releaseFrequency)
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to optimize quality gates for project {ProjectKey}", projectKey);
                throw new InvalidOperationException($"Quality gate optimization failed: {ex.Message}");
            }
        }
    }

    // Helper methods consolidated from both original files
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

        return await claudeService.GenerateReviewWithParametersAsync(prompt, temperature: 0.3, maxTokens: 2000);
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

    // Helper methods with placeholder implementations - these would be properly implemented in a production system
    private static string ExtractBusinessAlignment(string analysis, string context) => "Aligned with business context";
    private static string ExtractRiskAssessment(string analysis) => "Medium risk level identified";
    private static string[] ExtractRecommendations(string analysis) => new[] { "Address critical security issues first", "Implement code review process" };
    private static string CalculateAIPriority(SonarQubeIssue issue, string businessContext) => issue.Severity switch { "BLOCKER" => "Critical", "CRITICAL" => "High", "MAJOR" => "Medium", _ => "Low" };
    private static double CalculateFileRiskScore(List<SonarQubeIssue> issues) => issues.Sum(i => i.Severity == "CRITICAL" ? 10 : 5) / (double)Math.Max(1, issues.Count);
    private static object AnalyzeCoverage(Dictionary<string, string> metrics) => new { percentage = metrics.GetValueOrDefault("coverage", "0"), status = "Needs improvement" };
    private static object AnalyzeComplexity(SonarQubeIssueAnalysis analysis) => new { complexityIssues = analysis.CodeSmellIssues.Count(i => i.Rule.Contains("complexity")), status = "Acceptable" };
    private static object AnalyzeSecurityPosture(List<SonarQubeIssue> issues) => new { criticalSecurityIssues = issues.Count(i => i.Severity == "CRITICAL"), status = "Review required" };
    private static object CalculateTechnicalDebt(SonarQubeIssueAnalysis analysis) => new { debtIssues = analysis.CodeSmellIssues.Count, estimatedDebt = $"{analysis.CodeSmellIssues.Count * 15} minutes" };
    private static object CorrelateFindings(SonarQubeIssueAnalysis sonar, string ai, string code) => new { correlations = "Cross-analysis completed" };
    private static Task<string> GenerateHybridInsights(ClaudeService claude, SonarQubeIssueAnalysis sonar, string ai, object correlated) => Task.FromResult("Hybrid insights generated");
    private static double CalculateHybridConfidence(SonarQubeIssueAnalysis sonar, string ai) => 0.85;
    private static double CalculateAgreementScore(SonarQubeIssueAnalysis sonar, string ai) => 0.75;
    private static object ExtractUniqueAIFindings(string ai, SonarQubeIssueAnalysis sonar) => new { unique = "AI-specific findings identified" };
    private static object ExtractMissedByStaticAnalysis(string ai, SonarQubeIssueAnalysis sonar) => new { missed = "Static analysis gaps identified" };
    private static string[] GetImmediateActions(object findings) => new[] { "Fix critical security vulnerabilities", "Address blocking issues" };
    private static string[] GetShortTermActions(SonarQubeIssueAnalysis sonar, string ai) => new[] { "Implement code review process", "Address major code smells" };
    private static string[] GetLongTermActions(SonarQubeIssueAnalysis sonar, string ai) => new[] { "Architectural improvements", "Technical debt reduction" };
    private static string[] GetToolingRecommendations(object findings) => new[] { "Configure additional SonarQube rules", "Implement pre-commit hooks" };
    private static double CalculateOverallQualityScore(SonarQubeIssueAnalysis sonar, string ai) => Math.Max(1.0, 10.0 - sonar.CriticalIssues.Count * 0.5);
    private static string[] IdentifyStrengths(SonarQubeIssueAnalysis sonar, string ai) => new[] { "Good test coverage", "Consistent coding style" };
    private static string[] IdentifyImprovementAreas(SonarQubeIssueAnalysis sonar, string ai) => new[] { "Security practices", "Error handling" };
    private static string AssessComplianceStatus(SonarQubeIssueAnalysis sonar) => sonar.SecurityIssues.Any(i => i.Severity == "CRITICAL") ? "Non-compliant" : "Compliant";
    private static string BuildQualityGateAssessmentPrompt(SonarQubeProjectAnalysis analysis, string criticality, string target, string timeline) => $"Assess quality gate for {criticality} criticality {target} deployment with {timeline} timeline";
    private static object CalculateDeploymentRisk(SonarQubeProjectAnalysis analysis, string criticality, string target) => new { riskLevel = "Medium", factors = new[] { "Quality gate status", "Critical issues count" } };
    private static string ExtractRecommendation(string assessment) => "Proceed with deployment after addressing critical issues";
    private static double ExtractConfidence(string assessment) => 0.8;
    private static bool CanDeploy(SonarQubeProjectAnalysis analysis, object risk, string criticality) => analysis.QualityGate.Status == "OK" || criticality == "low";
    private static bool ShouldDeploy(SonarQubeProjectAnalysis analysis, object risk, string timeline) => analysis.QualityGate.Status == "OK" || timeline == "flexible";
    private static string[] GetDeploymentConditions(SonarQubeProjectAnalysis analysis, object risk) => new[] { "Fix all blocking issues", "Ensure test coverage > 80%" };
    private static string[] GetRiskMitigations(object risk) => new[] { "Monitor deployment closely", "Prepare rollback plan" };
    private static string[] GetBlockingIssues(SonarQubeProjectAnalysis analysis) => analysis.Issues.Where(i => i.Severity == "BLOCKER").Select(i => i.Message).Take(5).ToArray();
    private static string[] GetQuickFixes(SonarQubeProjectAnalysis analysis) => new[] { "Fix code formatting issues", "Remove unused imports" };
    private static string[] GetLongerTermWork(SonarQubeProjectAnalysis analysis) => new[] { "Refactor complex methods", "Improve test coverage" };
    private static string EstimateFixTime(SonarQubeProjectAnalysis analysis) => $"{Math.Max(1, analysis.Issues.Count(i => i.Severity == "CRITICAL") * 2)} hours";
    
    // Rule optimization helper methods
    private static IssuePatternAnalysis AnalyzeIssuePatterns(List<SonarQubeIssue> issues)
    {
        return new IssuePatternAnalysis
        {
            TopPatterns = issues.GroupBy(i => new { i.Rule, i.Severity })
                .Select(g => new IssuePattern { Rule = g.Key.Rule, Severity = g.Key.Severity, Count = g.Count() })
                .OrderByDescending(p => p.Count).Take(15).ToList(),
            Patterns = issues.GroupBy(i => i.Rule)
                .Select(g => new CodePattern { Name = g.Key, Frequency = g.Count(), RiskLevel = g.Any(i => i.Severity == "CRITICAL") ? "High" : "Medium", BusinessImpact = "TBD" }).ToList(),
            AntiPatterns = issues.Where(i => i.Type == "CODE_SMELL").GroupBy(i => i.Rule).OrderByDescending(g => g.Count()).Take(10).Select(g => g.Key).ToList(),
            BusinessRisks = issues.Where(i => i.Type == "VULNERABILITY" || i.Type == "SECURITY_HOTSPOT").Select(i => i.Message).Distinct().Take(10).ToList()
        };
    }
    
    private static string BuildRuleOptimizationPrompt(SonarQubeProjectAnalysis analysis, IssuePatternAnalysis patterns, string domain, string experience, string criticality, bool customRules)
    {
        return $@"As a SonarQube expert, optimize rules for this {domain} project with {experience} team at {criticality} criticality.
Current issues: {analysis.Issues.Count}
Top patterns: {string.Join(", ", patterns.TopPatterns.Take(5).Select(p => p.Rule))}
Provide rule prioritization, severity adjustments, and team customizations.";
    }
    
    private static object AnalyzeRuleEffectiveness(List<SonarQubeIssue> issues) => new { effectiveness = 0.75, noisyRules = issues.GroupBy(i => i.Rule).Where(g => g.Count() > 10).Select(g => g.Key).Take(5) };
    private static double CalculateNoiseLevel(List<SonarQubeIssue> issues) => issues.Count(i => i.Severity == "INFO" || i.Severity == "MINOR") / (double)Math.Max(1, issues.Count);
    private static List<RuleRecommendation> GenerateRuleRecommendations(IssuePatternAnalysis patterns, string domain, string experience) => new();
    private static string ExtractStrategicApproach(string analysis) => "Gradual improvement with team guidance";
    private static string[] ExtractPriorityAdjustments(string analysis) => new[] { "Prioritize security rules", "Reduce noise from formatting rules" };
    private static string[] ExtractTeamCustomizations(string analysis) => new[] { "Enable mentoring rules for junior developers", "Customize complexity thresholds" };
    private static string[] IdentifyRulesToDisable(IssuePatternAnalysis patterns, string experience) => experience == "senior" ? new[] { "basic-formatting-rules" } : new string[0];
    private static string[] IdentifyRulesToEmphasize(IssuePatternAnalysis patterns, string criticality) => criticality == "critical" ? new[] { "security-rules", "reliability-rules" } : new[] { "maintainability-rules" };
    private static object[] RecommendSeverityAdjustments(IssuePatternAnalysis patterns, string domain) => new[] { new { rule = "security-rule-1", from = "MAJOR", to = "CRITICAL" } };
    private static string[] GenerateQualityProfileSuggestions(string domain, string experience) => new[] { $"{domain}-{experience}-optimized" };
    private static List<CustomRuleSpec> GenerateProjectSpecificRules(SonarQubeProjectAnalysis analysis, string domain) => new();
    private static List<CustomRuleSpec> GenerateDomainSpecificRules(string domain) => new();
    private static List<CustomRuleSpec> GenerateTeamSpecificRules(string experience) => new();
    private static List<CustomRuleSpec> GenerateBusinessSpecificRules(string criticality) => new();
    private static object GeneratePhaseApproach(List<RuleRecommendation> recommendations) => new { phases = 3, duration = "6 months" };
    private static string[] GenerateTeamCommunicationPlan(string experience, string analysis) => new[] { "Explain rule changes in team meeting", "Provide training materials" };
    private static string GenerateRolloutStrategy(string criticality) => $"Gradual rollout over 4 weeks for {criticality} criticality systems";
    private static object[] DefineSuccessMetrics(SonarQubeProjectAnalysis analysis) => new[] { new { metric = "issue_reduction", target = "50%" }, new { metric = "quality_gate_pass_rate", target = "95%" } };
    
    // Custom rule generation methods
    private static CodePatternAnalysis AnalyzeCodePatterns(SonarQubeProjectAnalysis analysis, string[] patterns) => new();
    private static string BuildCustomRulePrompt(SonarQubeProjectAnalysis analysis, CodePatternAnalysis patterns, string domain, string compliance, string complexity) => $"Generate custom {complexity} rules for {domain} domain with {compliance} compliance requirements";
    private static string[] ExtractStrategicRules(string analysis) => new[] { "Strategic rule 1", "Strategic rule 2" };
    private static string[] ExtractTechnicalRules(string analysis) => new[] { "Technical rule 1", "Technical rule 2" };
    private static string[] ExtractBusinessRules(string analysis) => new[] { "Business rule 1", "Business rule 2" };
    private static string[] ExtractComplianceRules(string analysis) => new[] { "Compliance rule 1", "Compliance rule 2" };
    private static List<CustomRuleSpec> GenerateCustomRuleSpecs(CodePatternAnalysis patterns, string domain, string complexity) => new();
    private static string[] GetRulePriorityOrder(List<CustomRuleSpec> rules) => new[] { "Security rules first", "Reliability rules second" };
    private static string GenerateRuleRolloutPlan(List<CustomRuleSpec> rules, string complexity) => $"Phased rollout for {complexity} rules over 8 weeks";
    private static string GenerateRuleTestingStrategy(List<CustomRuleSpec> rules) => "Test rules on development branch first";
    private static string GenerateRuleMaintenancePlan(List<CustomRuleSpec> rules) => "Monthly review and adjustment cycle";
    private static object AssessExpectedImprovements(List<CustomRuleSpec> rules, SonarQubeProjectAnalysis analysis) => new { improvement = "Expected 30% reduction in issues" };
    private static object AssessRiskMitigation(List<CustomRuleSpec> rules, CodePatternAnalysis patterns) => new { mitigation = "Significant risk reduction expected" };
    private static object AssessProductivityImpact(List<CustomRuleSpec> rules, string complexity) => new { impact = complexity == "simple" ? "Minimal" : "Moderate" };
    private static object AssessBusinessValue(List<CustomRuleSpec> rules, string domain) => new { value = "High business value for quality improvement" };
    
    // Quality gate optimization methods
    private static QualityGatePerformance AnalyzeQualityGatePerformance(SonarQubeProjectAnalysis analysis, string frequency) => new() { Effectiveness = 0.8, BlockageRate = 0.15 };
    private static string BuildQualityGateOptimizationPrompt(SonarQubeProjectAnalysis analysis, QualityGatePerformance performance, string frequency, string tolerance, string maturity)
        => $"Optimize quality gates for {frequency} releases with {tolerance} risk tolerance and {maturity} team maturity";
    private static List<OptimizedQualityGate> GenerateOptimizedQualityGates(SonarQubeProjectAnalysis analysis, string frequency, string tolerance, string maturity) => new();
    private static string ExtractThresholdRecommendations(string analysis) => "Recommended threshold adjustments identified";
    private static string ExtractTeamGuidance(string analysis) => "Team-specific guidance provided";
    private static object GenerateMigrationPlan(SonarQubeQualityGate current, List<OptimizedQualityGate> optimized, string maturity) => new { plan = "Gradual migration strategy" };
    private static object GeneratePilotApproach(List<OptimizedQualityGate> gates) => new { approach = "Pilot with non-critical projects first" };
    private static object GenerateRollbackStrategy(SonarQubeQualityGate current) => new { strategy = "Quick rollback to previous configuration" };
    private static object[] DefineQualityGateSuccessCriteria(string frequency, string tolerance) => new[] { new { criteria = "Reduced false positives", target = "80%" } };
    private static double PredictBlockageReduction(QualityGatePerformance performance, List<OptimizedQualityGate> gates) => 0.25;
    private static object PredictQualityImprovement(SonarQubeProjectAnalysis analysis, List<OptimizedQualityGate> gates) => new { improvement = "Predicted 20% quality improvement" };
    private static object PredictProductivityImpact(List<OptimizedQualityGate> gates, string maturity) => new { impact = "Positive productivity impact expected" };
    private static object PredictBusinessValue(List<OptimizedQualityGate> gates, string frequency) => new { value = "High business value from reduced deployment friction" };
}

// Supporting data models consolidated from both files
public class IssuePatternAnalysis
{
    public List<IssuePattern> TopPatterns { get; set; } = new();
    public List<CodePattern> Patterns { get; set; } = new();
    public List<string> AntiPatterns { get; set; } = new();
    public List<string> BusinessRisks { get; set; } = new();
}

public class IssuePattern
{
    public string Rule { get; set; } = "";
    public string Severity { get; set; } = "";
    public int Count { get; set; }
}

public class CodePattern
{
    public string Name { get; set; } = "";
    public int Frequency { get; set; }
    public string RiskLevel { get; set; } = "";
    public string BusinessImpact { get; set; } = "";
}

public class CodePatternAnalysis
{
    public List<CodePattern> Patterns { get; set; } = new();
    public List<string> AntiPatterns { get; set; } = new();
    public List<string> BusinessRisks { get; set; } = new();
}

public class CustomRuleSpec
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Category { get; set; } = "";
    public string Pattern { get; set; } = "";
    public string Justification { get; set; } = "";
    public string Language { get; set; } = "";
    public string RuleKey { get; set; } = "";
    public string ImplementationApproach { get; set; } = "";
    public string[] TestCases { get; set; } = Array.Empty<string>();
}

public class RuleRecommendation
{
    public string Action { get; set; } = "";
    public string Rule { get; set; } = "";
    public string Justification { get; set; } = "";
}

public class QualityGatePerformance
{
    public double Effectiveness { get; set; } = 0.8;
    public double BlockageRate { get; set; } = 0.15;
}

public class OptimizedQualityGate
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string[] SuitableFor { get; set; } = Array.Empty<string>();
    public List<QualityGateCondition> Conditions { get; set; } = new();
    public string ExpectedImpact { get; set; } = "";
}

public class QualityGateCondition
{
    public string Metric { get; set; } = "";
    public string Threshold { get; set; } = "";
    public string Justification { get; set; } = "";
}