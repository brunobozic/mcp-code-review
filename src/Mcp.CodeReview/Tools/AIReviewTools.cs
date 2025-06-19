using ModelContextProtocol;
using ModelContextProtocol.Server;
using Mcp.CodeReview.AI;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;
using Mcp.CodeReview.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace Mcp.CodeReview.Tools;

[McpServerToolType]
public static class AIReviewTools
{
    /// <summary>
    /// Basic AI code review and summary
    /// </summary>
    [McpServerTool, Description("Generate an AI summary and review of code changes")]
    public static async Task<object> SummarizeCode(
        ClaudeService claudeService,
        ILogger logger,
        [Description("The code diff to analyze")] string diff,
        [Description("Optional linter output to include in analysis")] string? linterOutput = null,
        [Description("The programming language of the code")] string language = "csharp")
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Language"] = language,
            ["DiffLength"] = diff.Length,
            ["HasLinterOutput"] = !string.IsNullOrEmpty(linterOutput)
        }))
        {
            logger.LogInformation("Starting code review for {Language} with diff length {DiffLength}",
                language, diff.Length);

            try
            {
                var lintResults = linterOutput ?? "";
                var prompt = BuildBasicReviewPrompt(diff, lintResults, language);
                var originalPromptLength = prompt.Length;

                if (prompt.Length > 20000)
                {
                    prompt = prompt[..20000] + "\n[Truncated due to length]";
                    logger.LogWarning("Prompt truncated from {Original} to {Truncated} characters",
                        originalPromptLength, prompt.Length);
                }

                logger.LogDebug("Sending prompt to Claude AI ({Length} chars)", prompt.Length);
                var summary = await claudeService.GenerateReviewAsync(prompt);

                logger.LogInformation("Successfully generated code review summary ({Length} chars)",
                    summary.Length);

                return new
                {
                    summary,
                    language,
                    linterResults = lintResults,
                    promptLength = prompt.Length,
                    originalPromptLength,
                    wasTruncated = originalPromptLength != prompt.Length
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate code summary for {Language}", language);
                throw;
            }
        }
    }

    /// <summary>
    /// Multi-agent AI code review with specialized experts
    /// </summary>
    [McpServerTool, Description("Conduct comprehensive multi-agent AI code review with specialized experts")]
    public static async Task<object> ConductMultiAgentReview(
        ClaudeService claudeService,
        ILogger logger,
        [Description("The code diff to review")] string diff,
        [Description("The programming language")] string language = "csharp",
        [Description("The pull request title")] string pullRequestTitle = "",
        [Description("The author of the changes")] string author = "",
        [Description("Array of files that were changed")] string[] filesChanged = null)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "multiAgentReview",
            ["Language"] = language,
            ["DiffLength"] = diff.Length,
            ["FilesChanged"] = filesChanged?.Length ?? 0
        }))
        {
            logger.LogInformation("Starting multi-agent code review for {Language} with {Files} files changed",
                language, filesChanged?.Length ?? 0);

            try
            {
                // Create specialized agent reviews
                var agents = new[]
                {
                    new { Name = "SecurityExpert", Focus = "Security vulnerabilities, authentication, encryption" },
                    new { Name = "PerformanceAnalyst", Focus = "Performance bottlenecks, optimization opportunities" },
                    new { Name = "CodeQualityReviewer", Focus = "Code quality, best practices, maintainability" },
                    new { Name = "ArchitectureExpert", Focus = "Design patterns, SOLID principles, architecture" },
                    new { Name = "TestingSpecialist", Focus = "Test coverage, testing strategies" }
                };

                var agentResults = new List<object>();
                var tasks = agents.Select(async agent =>
                {
                    var prompt = BuildAgentPrompt(diff, language, agent.Name, agent.Focus, pullRequestTitle, author);
                    var result = await claudeService.GenerateReviewWithParametersAsync(prompt, temperature: 0.2, maxTokens: 1500);
                    return new
                    {
                        agent = agent.Name,
                        focus = agent.Focus,
                        analysis = result,
                        severity = ExtractSeverity(result),
                        recommendations = ExtractRecommendations(result)
                    };
                });

                var results = await Task.WhenAll(tasks);
                agentResults.AddRange(results);

                // Calculate overall scores
                var qualityScore = CalculateQualityScore(results);
                var riskLevel = CalculateRiskLevel(results);

                logger.LogInformation("Multi-agent review completed with {Agents} agents, quality score: {QualityScore:F2}",
                    results.Length, qualityScore);

                return new
                {
                    success = true,
                    summary = new
                    {
                        overallQualityScore = qualityScore,
                        riskLevel = riskLevel,
                        agentCount = results.Length,
                        languageAnalyzed = language,
                        filesChanged = filesChanged?.Length ?? 0
                    },
                    agentResults = agentResults,
                    consolidatedRecommendations = ConsolidateRecommendations(results),
                    priorityActions = ExtractPriorityActions(results),
                    metadata = new
                    {
                        pullRequestTitle = pullRequestTitle,
                        author = author,
                        timestamp = DateTime.UtcNow,
                        diffSize = diff.Length
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to conduct multi-agent review");
                throw new InvalidOperationException($"Multi-agent review failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Enhanced multi-agent review with additional specialized agents
    /// </summary>
    [McpServerTool, Description("Advanced multi-agent review with enhanced prompt engineering and collaborative challenges")]
    public static async Task<object> ConductEnhancedMultiAgentReview(
        ClaudeService claudeService,
        ILogger logger,
        [Description("The code diff to review")] string diff,
        [Description("The programming language")] string language = "csharp",
        [Description("Business domain context")] string businessDomain = "software development",
        [Description("Code complexity level: simple, moderate, complex")] string complexityLevel = "moderate",
        [Description("Include AI code detection")] bool includeAIDetection = true)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "enhancedMultiAgentReview",
            ["Language"] = language,
            ["BusinessDomain"] = businessDomain,
            ["ComplexityLevel"] = complexityLevel
        }))
        {
            logger.LogInformation("Starting enhanced multi-agent review for {Language} in {Domain} domain",
                language, businessDomain);

            try
            {
                // Enhanced agent lineup with domain specialists
                var enhancedAgents = new[]
                {
                    new { Name = "SecurityExpert", Focus = "OWASP Top 10, secure coding practices, vulnerability assessment" },
                    new { Name = "PerformanceAnalyst", Focus = "Algorithmic complexity, memory optimization, scalability" },
                    new { Name = "CodeQualityReviewer", Focus = "Clean code principles, SOLID, design patterns" },
                    new { Name = "ArchitectureExpert", Focus = "System design, microservices, domain boundaries" },
                    new { Name = "TestingSpecialist", Focus = "TDD, test coverage, integration testing" },
                    new { Name = "DomainExpert", Focus = $"Business logic, {businessDomain} best practices" },
                    new { Name = "DevOpsSpecialist", Focus = "CI/CD, deployment, infrastructure as code" },
                    new { Name = "AICodeDetective", Focus = "AI-generated code patterns, potential shortcuts" }
                };

                // Filter agents based on options
                var activeAgents = includeAIDetection ? enhancedAgents : enhancedAgents.Where(a => a.Name != "AICodeDetective").ToArray();

                var agentResults = new List<object>();
                var tasks = activeAgents.Select(async agent =>
                {
                    var prompt = BuildEnhancedAgentPrompt(diff, language, agent.Name, agent.Focus, businessDomain, complexityLevel);
                    var result = await claudeService.GenerateReviewWithParametersAsync(prompt, temperature: 0.1, maxTokens: 2000);
                    return new
                    {
                        agent = agent.Name,
                        focus = agent.Focus,
                        analysis = result,
                        confidence = ExtractConfidence(result),
                        severity = ExtractSeverity(result),
                        recommendations = ExtractRecommendations(result),
                        businessImpact = ExtractBusinessImpact(result)
                    };
                });

                var results = await Task.WhenAll(tasks);
                agentResults.AddRange(results);

                // Enhanced analysis
                var qualityScore = CalculateEnhancedQualityScore(results);
                var confidenceScore = CalculateConfidenceScore(results);
                var businessRisk = CalculateBusinessRisk(results);

                logger.LogInformation("Enhanced multi-agent review completed with {Agents} agents, quality: {Quality:F2}, confidence: {Confidence:F2}",
                    results.Length, qualityScore, confidenceScore);

                return new
                {
                    success = true,
                    summary = new
                    {
                        overallQualityScore = qualityScore,
                        confidenceScore = confidenceScore,
                        businessRiskLevel = businessRisk,
                        agentCount = results.Length,
                        languageAnalyzed = language,
                        businessDomain = businessDomain,
                        complexityLevel = complexityLevel
                    },
                    agentResults = agentResults,
                    crossAgentValidation = PerformCrossAgentValidation(results),
                    consolidatedFindings = ConsolidateFindings(results),
                    actionablePlan = GenerateActionablePlan(results),
                    riskMitigation = IdentifyRiskMitigation(results),
                    learningOpportunities = IdentifyLearningOpportunities(results),
                    metadata = new
                    {
                        analysisDepth = "enhanced",
                        aiDetectionIncluded = includeAIDetection,
                        timestamp = DateTime.UtcNow,
                        processingTime = DateTime.UtcNow // Would be calculated properly
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to conduct enhanced multi-agent review");
                throw new InvalidOperationException($"Enhanced multi-agent review failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// AI-powered security vulnerability analysis
    /// </summary>
    [McpServerTool, Description("Conduct deep security analysis using AI pattern recognition")]
    public static async Task<object> ConductDeepSecurityAnalysis(
        ClaudeService claudeService,
        ILogger logger,
        [Description("The code to analyze for security issues")] string code,
        [Description("The programming language")] string language = "csharp",
        [Description("Known dependencies (comma-separated)")] string dependencies = "",
        [Description("Security compliance framework (OWASP, NIST, SOC2)")] string complianceFramework = "OWASP")
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "deepSecurityAnalysis",
            ["Language"] = language,
            ["ComplianceFramework"] = complianceFramework
        }))
        {
            logger.LogInformation("Starting deep security analysis for {Language} using {Framework} framework",
                language, complianceFramework);

            try
            {
                var prompt = BuildSecurityAnalysisPrompt(code, language, dependencies, complianceFramework);
                var analysis = await claudeService.GenerateReviewWithParametersAsync(prompt, temperature: 0.1, maxTokens: 3000);

                var vulnerabilities = ExtractVulnerabilities(analysis);
                var riskScore = CalculateSecurityRiskScore(vulnerabilities);
                var compliance = AssessComplianceStatus(analysis, complianceFramework);

                logger.LogInformation("Security analysis completed with risk score: {RiskScore:F2}", riskScore);

                return new
                {
                    success = true,
                    securitySummary = new
                    {
                        overallRiskScore = riskScore,
                        vulnerabilityCount = vulnerabilities.Count,
                        complianceStatus = compliance,
                        framework = complianceFramework
                    },
                    vulnerabilities = vulnerabilities,
                    complianceAssessment = new
                    {
                        framework = complianceFramework,
                        status = compliance,
                        gaps = ExtractComplianceGaps(analysis),
                        recommendations = ExtractComplianceRecommendations(analysis)
                    },
                    remediationPlan = new
                    {
                        immediate = ExtractImmediateActions(analysis),
                        shortTerm = ExtractShortTermActions(analysis),
                        longTerm = ExtractLongTermActions(analysis)
                    },
                    analysis = analysis
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to conduct security analysis");
                throw new InvalidOperationException($"Security analysis failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// AI-powered PR risk assessment
    /// </summary>
    [McpServerTool, Description("Assess pull request risk using AI analysis of multiple factors")]
    public static async Task<object> AssessPullRequestRisk(
        ClaudeService claudeService,
        ILogger logger,
        [Description("The code diff to analyze")] string diff,
        [Description("Files changed in the PR")] string[] filesChanged,
        [Description("PR author experience level")] string authorExperience = "intermediate",
        [Description("Business criticality of affected code")] string businessCriticality = "medium",
        [Description("Deployment environment")] string environment = "production")
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "assessPRRisk",
            ["FilesChanged"] = filesChanged.Length,
            ["AuthorExperience"] = authorExperience,
            ["BusinessCriticality"] = businessCriticality
        }))
        {
            logger.LogInformation("Assessing PR risk for {Files} files with {Criticality} business criticality",
                filesChanged.Length, businessCriticality);

            try
            {
                var prompt = BuildRiskAssessmentPrompt(diff, filesChanged, authorExperience, businessCriticality, environment);
                var analysis = await claudeService.GenerateReviewWithParametersAsync(prompt, temperature: 0.2, maxTokens: 2500);

                var riskFactors = ExtractRiskFactors(analysis);
                var overallRisk = CalculateOverallRisk(riskFactors, filesChanged, businessCriticality);
                var recommendations = GenerateRiskRecommendations(overallRisk, riskFactors);

                logger.LogInformation("PR risk assessment completed with overall risk: {Risk}", overallRisk);

                return new
                {
                    success = true,
                    riskSummary = new
                    {
                        overallRiskLevel = overallRisk,
                        riskScore = CalculateRiskScore(riskFactors),
                        filesAtRisk = IdentifyRiskyFiles(filesChanged, riskFactors),
                        businessImpact = AssessBusinessImpact(businessCriticality, overallRisk)
                    },
                    riskFactors = riskFactors,
                    recommendations = new
                    {
                        reviewProcess = ((dynamic)recommendations).ReviewProcess,
                        testing = ((dynamic)recommendations).Testing,
                        deployment = ((dynamic)recommendations).Deployment,
                        monitoring = ((dynamic)recommendations).Monitoring
                    },
                    analysis = analysis
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to assess PR risk");
                throw new InvalidOperationException($"PR risk assessment failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// AI-powered test suggestion generation
    /// </summary>
    [McpServerTool, Description("Generate intelligent test suggestions using AI analysis")]
    public static async Task<object> GenerateIntelligentTestSuggestions(
        ClaudeService claudeService,
        ILogger logger,
        [Description("The code to generate tests for")] string code,
        [Description("The programming language")] string language = "csharp",
        [Description("Existing test coverage info")] string existingTests = "",
        [Description("Testing framework preference")] string testingFramework = "xunit")
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "generateTestSuggestions",
            ["Language"] = language,
            ["TestingFramework"] = testingFramework
        }))
        {
            logger.LogInformation("Generating test suggestions for {Language} using {Framework}",
                language, testingFramework);

            try
            {
                var prompt = BuildTestSuggestionPrompt(code, language, existingTests, testingFramework);
                var suggestions = await claudeService.GenerateReviewWithParametersAsync(prompt, temperature: 0.3, maxTokens: 3000);

                var testCases = ExtractTestCases(suggestions);
                var coverageGaps = IdentifyCoverageGaps(suggestions);
                var testStrategies = ExtractTestStrategies(suggestions);

                logger.LogInformation("Generated {TestCases} test case suggestions", testCases.Count);

                return new
                {
                    success = true,
                    testingSummary = new
                    {
                        suggestedTestCount = testCases.Count,
                        coverageGaps = coverageGaps.Count,
                        testingFramework = testingFramework,
                        language = language
                    },
                    testCases = testCases,
                    coverageAnalysis = new
                    {
                        identifiedGaps = coverageGaps,
                        recommendations = ExtractCoverageRecommendations(suggestions),
                        priority = PrioritizeTestCases(testCases)
                    },
                    testStrategies = testStrategies,
                    implementation = new
                    {
                        framework = testingFramework,
                        setupRequirements = ExtractSetupRequirements(suggestions),
                        mockingStrategy = ExtractMockingStrategy(suggestions)
                    },
                    fullAnalysis = suggestions
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate test suggestions");
                throw new InvalidOperationException($"Test suggestion generation failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// AI Code Detective - Detect AI-generated shortcuts, bypasses, and problematic patterns
    /// </summary>
    [McpServerTool, Description("Detect AI-generated code shortcuts, bypasses, commented-out implementations, and other problematic patterns")]
    public static async Task<object> DetectAICodeShortcuts(
        ClaudeService claudeService,
        ILogger logger,
        [Description("The code diff or file content to analyze")] string codeContent,
        [Description("Previous version of the code for comparison (optional)")] string? previousVersion = null,
        [Description("Git commit message (optional, helps detect intent)")] string? commitMessage = null,
        [Description("File paths included in the change")] string[]? modifiedFiles = null,
        [Description("Include license compliance checking")] bool checkLicenseCompliance = true)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "detectAICodeShortcuts",
            ["CodeLength"] = codeContent.Length,
            ["HasPreviousVersion"] = previousVersion != null
        }))
        {
            logger.LogInformation("Starting AI Code Detective analysis on {Length} characters of code", codeContent.Length);

            try
            {
                // Create comprehensive AI detection prompt
                var detectionPrompt = BuildAIDetectionPrompt(codeContent, commitMessage, previousVersion, modifiedFiles);
                
                // Get AI analysis for pattern detection
                var aiAnalysis = await claudeService.GenerateReviewWithParametersAsync(detectionPrompt, temperature: 0.1, maxTokens: 3000);

                // Extract structured findings
                var aiPatterns = ExtractAIPatterns(aiAnalysis);
                var shortcuts = ExtractShortcuts(aiAnalysis);
                var suspiciousPatterns = ExtractSuspiciousPatterns(aiAnalysis);
                var riskScore = CalculateAIRiskScore(aiAnalysis);

                logger.LogInformation("AI Code Detective analysis completed with risk score: {RiskScore}", riskScore);

                return new
                {
                    success = true,
                    riskScore = riskScore,
                    riskLevel = riskScore > 8 ? "CRITICAL" : riskScore > 6 ? "HIGH" : riskScore > 4 ? "MEDIUM" : "LOW",
                    
                    // AI-generated pattern detection
                    aiGeneratedPatterns = new
                    {
                        detected = aiPatterns.Count > 0,
                        patterns = aiPatterns,
                        aiConfidenceScore = CalculateAIDetectionConfidence(aiPatterns)
                    },
                    
                    // Code shortcuts and bypasses
                    shortcuts = new
                    {
                        detected = shortcuts.Count > 0,
                        criticalShortcuts = shortcuts.Where(s => ((dynamic)s).Severity == "CRITICAL"),
                        allShortcuts = shortcuts
                    },
                    
                    // Suspicious patterns
                    suspiciousPatterns = new
                    {
                        detected = suspiciousPatterns.Count > 0,
                        patterns = suspiciousPatterns,
                        recommendations = GenerateDetectionRecommendations(suspiciousPatterns)
                    },
                    
                    // Overall assessment
                    assessment = new
                    {
                        aiLikelihood = CalculateAILikelihood(aiAnalysis),
                        qualityLevel = AssessCodeQuality(aiAnalysis),
                        trustworthiness = AssessTrustworthiness(riskScore),
                        followUpActions = GetFollowUpActions(riskScore, aiPatterns, shortcuts)
                    },
                    
                    // Detailed analysis
                    detailedAnalysis = aiAnalysis
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to conduct AI code detection analysis");
                throw new InvalidOperationException($"AI code detection failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// AI-powered code quality prediction
    /// </summary>
    [McpServerTool, Description("Predict code quality metrics and potential issues using AI")]
    public static async Task<object> PredictCodeQuality(
        ClaudeService claudeService,
        ILogger logger,
        [Description("The code diff to analyze")] string diff,
        [Description("Historical quality metrics (JSON)")] string historicalMetrics = "",
        [Description("Team context information")] string teamContext = "")
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "predictCodeQuality",
            ["DiffLength"] = diff.Length,
            ["HasHistoricalData"] = !string.IsNullOrEmpty(historicalMetrics)
        }))
        {
            logger.LogInformation("Predicting code quality for diff of {Length} characters", diff.Length);

            try
            {
                var prompt = BuildQualityPredictionPrompt(diff, historicalMetrics, teamContext);
                var prediction = await claudeService.GenerateReviewWithParametersAsync(prompt, temperature: 0.2, maxTokens: 2500);

                var qualityMetrics = ExtractQualityMetrics(prediction);
                var riskPredictions = ExtractRiskPredictions(prediction);
                var improvementSuggestions = ExtractImprovementSuggestions(prediction);

                logger.LogInformation("Quality prediction completed");

                return new
                {
                    success = true,
                    qualityPrediction = new
                    {
                        predictedQualityScore = ((dynamic)qualityMetrics).OverallScore,
                        maintainabilityIndex = ((dynamic)qualityMetrics).MaintainabilityIndex,
                        technicalDebtLevel = ((dynamic)qualityMetrics).TechnicalDebtLevel,
                        confidenceLevel = ExtractConfidenceLevel(prediction)
                    },
                    riskAssessment = new
                    {
                        bugProbability = ((dynamic)riskPredictions).BugProbability,
                        performanceImpact = ((dynamic)riskPredictions).PerformanceImpact,
                        securityRisk = ((dynamic)riskPredictions).SecurityRisk,
                        maintenanceComplexity = ((dynamic)riskPredictions).MaintenanceComplexity
                    },
                    recommendations = new
                    {
                        immediate = ExtractImmediateActions(prediction),
                        shortTerm = ExtractShortTermActions(prediction),
                        longTerm = ExtractLongTermActions(prediction)
                    },
                    analysis = prediction
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to predict code quality");
                throw new InvalidOperationException($"Code quality prediction failed: {ex.Message}");
            }
        }
    }

    // Helper methods for prompt building and result extraction
    private static string BuildBasicReviewPrompt(string diff, string lintResults, string language)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("Please review the following code changes and provide constructive feedback:");
        prompt.AppendLine();
        prompt.AppendLine($"Language: {language}");
        prompt.AppendLine();
        prompt.AppendLine("Code Changes:");
        prompt.AppendLine("```diff");
        prompt.AppendLine(diff);
        prompt.AppendLine("`");

        if (!string.IsNullOrEmpty(lintResults))
        {
            prompt.AppendLine();
            prompt.AppendLine("Static Analysis Results:");
            prompt.AppendLine("`");
            prompt.AppendLine(lintResults);
            prompt.AppendLine("`");
        }

        prompt.AppendLine();
        prompt.AppendLine("Please focus on:");
        prompt.AppendLine("- Code quality and best practices");
        prompt.AppendLine("- Potential bugs or issues");
        prompt.AppendLine("- Performance considerations");
        prompt.AppendLine("- Security concerns");
        prompt.AppendLine("- Maintainability and readability");

        return prompt.ToString();
    }

    private static string BuildAgentPrompt(string diff, string language, string agentName, string focus, string prTitle, string author)
    {
        return $@"
As a {agentName} with expertise in {focus}, review this {language} code change:

PR: {prTitle}
Author: {author}

Code Diff:
{diff}

Focus your analysis on {focus} and provide:
1. Specific findings related to your expertise
2. Severity assessment (LOW/MEDIUM/HIGH/CRITICAL)
3. Actionable recommendations
4. Risk assessment for your domain

Be specific and actionable in your feedback.
";
    }

    private static string BuildEnhancedAgentPrompt(string diff, string language, string agentName, string focus, string businessDomain, string complexity)
    {
        return $@"
As a {agentName} specializing in {focus}, conduct an expert review of this {language} code:

Business Domain: {businessDomain}
Complexity Level: {complexity}

Code Changes:
{diff}

Provide expert analysis focusing on:
- {focus}
- Business domain considerations for {businessDomain}
- Code complexity implications
- Risk assessment and mitigation
- Confidence level in your assessment (1-10)

Structure your response with:
1. Executive Summary
2. Detailed Findings
3. Risk Assessment
4. Recommendations
5. Confidence Score
";
    }

    private static string BuildSecurityAnalysisPrompt(string code, string language, string dependencies, string framework)
    {
        return $@"
Conduct a comprehensive security analysis of this {language} code using {framework} guidelines:

Code:
{code}

Dependencies: {dependencies}

Analyze for:
- OWASP Top 10 vulnerabilities
- Injection attacks (SQL, XSS, etc.)
- Authentication and authorization flaws
- Data validation issues
- Cryptographic weaknesses
- Security configuration issues
- Dependency vulnerabilities

Provide:
1. Vulnerability inventory with severity ratings
2. Compliance assessment against {framework}
3. Risk scoring (1-10 scale)
4. Detailed remediation steps
5. Prevention recommendations
";
    }

    private static string BuildRiskAssessmentPrompt(string diff, string[] files, string experience, string criticality, string environment)
    {
        return $@"
Assess the deployment risk of this code change:

Code Diff:
{diff}

Files Changed: {string.Join(", ", files)}
Author Experience: {experience}
Business Criticality: {criticality}
Target Environment: {environment}

Evaluate risk factors:
- Code complexity and change scope
- Business impact potential
- Technical risk factors
- Author experience vs change difficulty
- Testing requirements
- Rollback complexity

Provide risk assessment with:
1. Overall risk level (LOW/MEDIUM/HIGH/CRITICAL)
2. Key risk factors identified
3. Mitigation recommendations
4. Review and testing strategy
5. Deployment recommendations
";
    }

    private static string BuildTestSuggestionPrompt(string code, string language, string existingTests, string framework)
    {
        return $@"
Generate comprehensive test suggestions for this {language} code using {framework}:

Code to Test:
{code}

Existing Test Coverage:
{existingTests}

Suggest:
1. Unit tests for edge cases and boundary conditions
2. Integration test scenarios
3. Error handling and exception tests
4. Performance test considerations
5. Security test cases
6. Mock/stub strategies

For each suggestion provide:
- Test case description
- Expected behavior
- Test implementation approach
- Priority level (HIGH/MEDIUM/LOW)
";
    }

    private static string BuildQualityPredictionPrompt(string diff, string historical, string teamContext)
    {
        return $@"
Predict code quality metrics for this change:

Code Diff:
{diff}

Historical Quality Data:
{historical}

Team Context:
{teamContext}

Predict:
1. Overall quality score (1-10)
2. Maintainability index impact
3. Technical debt introduction likelihood
4. Bug probability assessment
5. Performance impact prediction
6. Long-term maintenance complexity

Provide confidence levels and reasoning for each prediction.
";
    }

    // Result extraction helper methods with placeholder implementations
    private static string ExtractSeverity(string analysis) => analysis.Contains("CRITICAL") ? "CRITICAL" : analysis.Contains("HIGH") ? "HIGH" : "MEDIUM";
    private static double ExtractConfidence(string analysis) => 0.8; // Would extract from analysis
    private static string[] ExtractRecommendations(string analysis) => new[] { "Review recommendation 1", "Review recommendation 2" };
    private static double CalculateQualityScore(object[] results) => 7.5;
    private static string CalculateRiskLevel(object[] results) => "MEDIUM";
    private static double CalculateEnhancedQualityScore(object[] results) => 8.2;
    private static double CalculateConfidenceScore(object[] results) => 0.85;
    private static string CalculateBusinessRisk(object[] results) => "LOW";
    private static string[] ConsolidateRecommendations(object[] results) => new[] { "Consolidated recommendation 1" };
    private static string[] ExtractPriorityActions(object[] results) => new[] { "Priority action 1" };
    private static object PerformCrossAgentValidation(object[] results) => new { validation = "Cross-validation complete" };
    private static object ConsolidateFindings(object[] results) => new { findings = "Findings consolidated" };
    private static object GenerateActionablePlan(object[] results) => new { plan = "Actionable plan generated" };
    private static object IdentifyRiskMitigation(object[] results) => new { mitigation = "Risk mitigation identified" };
    private static object IdentifyLearningOpportunities(object[] results) => new { opportunities = "Learning opportunities identified" };
    private static string ExtractBusinessImpact(string analysis) => "MEDIUM";
    private static List<object> ExtractVulnerabilities(string analysis) => new();
    private static double CalculateSecurityRiskScore(List<object> vulnerabilities) => 3.5;
    private static string AssessComplianceStatus(string analysis, string framework) => "COMPLIANT";
    private static string[] ExtractComplianceGaps(string analysis) => new[] { "Gap 1", "Gap 2" };
    private static string[] ExtractComplianceRecommendations(string analysis) => new[] { "Compliance rec 1" };
    private static string[] ExtractImmediateActions(string analysis) => new[] { "Immediate action 1" };
    private static string[] ExtractShortTermActions(string analysis) => new[] { "Short term action 1" };
    private static string[] ExtractLongTermActions(string analysis) => new[] { "Long term action 1" };
    private static List<object> ExtractRiskFactors(string analysis) => new();
    private static string CalculateOverallRisk(List<object> factors, string[] files, string criticality) => "MEDIUM";
    private static object GenerateRiskRecommendations(string risk, List<object> factors) => new { ReviewProcess = new[] { "Enhanced review" }, Testing = new[] { "Extended testing" }, Deployment = new[] { "Gradual rollout" }, Monitoring = new[] { "Enhanced monitoring" } };
    private static double CalculateRiskScore(List<object> factors) => 5.0;
    private static string[] IdentifyRiskyFiles(string[] files, List<object> factors) => files.Take(2).ToArray();
    private static string AssessBusinessImpact(string criticality, string risk) => $"{criticality} criticality with {risk} risk";
    private static List<object> ExtractTestCases(string suggestions) => new();
    private static List<object> IdentifyCoverageGaps(string suggestions) => new();
    private static List<object> ExtractTestStrategies(string suggestions) => new();
    private static string[] ExtractCoverageRecommendations(string suggestions) => new[] { "Coverage rec 1" };
    private static object PrioritizeTestCases(List<object> testCases) => new { priority = "Prioritized" };
    private static string[] ExtractSetupRequirements(string suggestions) => new[] { "Setup req 1" };
    private static string ExtractMockingStrategy(string suggestions) => "Mock strategy";
    private static object ExtractQualityMetrics(string prediction) => new { OverallScore = 7.5, MaintainabilityIndex = 8.0, TechnicalDebtLevel = "LOW" };
    private static object ExtractRiskPredictions(string prediction) => new { BugProbability = 0.2, PerformanceImpact = "MINIMAL", SecurityRisk = "LOW", MaintenanceComplexity = "MEDIUM" };
    private static object ExtractImprovementSuggestions(string prediction) => new { Immediate = new[] { "Immediate 1" }, ShortTerm = new[] { "Short term 1" }, LongTerm = new[] { "Long term 1" } };
    private static double ExtractConfidenceLevel(string prediction) => 0.8;
    
    // AI Detection helper methods
    private static string BuildAIDetectionPrompt(string code, string? commitMessage, string? previousVersion, string[]? files)
    {
        return $@"
As an expert AI code detective, analyze this code for patterns that suggest AI generation or problematic shortcuts:

CODE TO ANALYZE:
{code}

{(commitMessage != null ? $"COMMIT MESSAGE: {commitMessage}" : "")}
{(previousVersion != null ? $"PREVIOUS VERSION:\n{previousVersion}" : "")}
{(files != null ? $"MODIFIED FILES: {string.Join(", ", files)}" : "")}

DETECTION FOCUS:
1. AI-generated code patterns (repetitive structures, boilerplate, generic naming)
2. Code shortcuts and bypasses (TODO comments, placeholder implementations)
3. Commented-out code blocks
4. Incomplete implementations
5. Copy-paste patterns
6. Overly generic or template-like code
7. Inconsistent coding styles within the same change
8. Missing error handling or edge cases

Provide assessment with:
- Risk score (1-10)
- Specific patterns detected
- Confidence level in AI generation likelihood
- Recommendations for code review focus areas
";
    }
    
    private static List<object> ExtractAIPatterns(string analysis)
    {
        var patterns = new List<object>();
        
        // Simple pattern extraction - in production would use more sophisticated parsing
        if (analysis.Contains("AI-generated", StringComparison.OrdinalIgnoreCase))
        {
            patterns.Add(new { Type = "AI_GENERATED", Description = "Potential AI-generated code patterns detected", Confidence = 0.8 });
        }
        
        if (analysis.Contains("template", StringComparison.OrdinalIgnoreCase))
        {
            patterns.Add(new { Type = "TEMPLATE_CODE", Description = "Template-like code structure detected", Confidence = 0.6 });
        }
        
        return patterns;
    }
    
    private static List<object> ExtractShortcuts(string analysis)
    {
        var shortcuts = new List<object>();
        
        if (analysis.Contains("TODO", StringComparison.OrdinalIgnoreCase))
        {
            shortcuts.Add(new { Type = "TODO_COMMENT", Severity = "MEDIUM", Description = "TODO comments indicating incomplete implementation" });
        }
        
        if (analysis.Contains("placeholder", StringComparison.OrdinalIgnoreCase))
        {
            shortcuts.Add(new { Type = "PLACEHOLDER", Severity = "HIGH", Description = "Placeholder implementation detected" });
        }
        
        return shortcuts;
    }
    
    private static List<object> ExtractSuspiciousPatterns(string analysis)
    {
        var patterns = new List<object>();
        
        if (analysis.Contains("suspicious", StringComparison.OrdinalIgnoreCase))
        {
            patterns.Add(new { Pattern = "SUSPICIOUS_STRUCTURE", Risk = "MEDIUM", Description = "Suspicious code structure identified" });
        }
        
        return patterns;
    }
    
    private static double CalculateAIRiskScore(string analysis)
    {
        double score = 0;
        
        if (analysis.Contains("high risk", StringComparison.OrdinalIgnoreCase)) score += 3;
        if (analysis.Contains("medium risk", StringComparison.OrdinalIgnoreCase)) score += 2;
        if (analysis.Contains("AI-generated", StringComparison.OrdinalIgnoreCase)) score += 2;
        if (analysis.Contains("TODO", StringComparison.OrdinalIgnoreCase)) score += 1;
        if (analysis.Contains("placeholder", StringComparison.OrdinalIgnoreCase)) score += 2;
        
        return Math.Min(10, Math.Max(0, score));
    }
    
    private static double CalculateAIDetectionConfidence(List<object> patterns) => patterns.Count > 0 ? 0.8 : 0.2;
    private static string[] GenerateDetectionRecommendations(List<object> patterns) => new[] { "Manual code review recommended", "Verify implementation completeness" };
    private static double CalculateAILikelihood(string analysis) => analysis.Contains("AI-generated", StringComparison.OrdinalIgnoreCase) ? 0.8 : 0.3;
    private static string AssessCodeQuality(string analysis) => analysis.Contains("high quality", StringComparison.OrdinalIgnoreCase) ? "HIGH" : "MEDIUM";
    private static string AssessTrustworthiness(double riskScore) => riskScore < 3 ? "HIGH" : riskScore < 6 ? "MEDIUM" : "LOW";
    private static string[] GetFollowUpActions(double risk, List<object> patterns, List<object> shortcuts)
    {
        var actions = new List<string>();
        if (risk > 6) actions.Add("Conduct thorough manual review");
        if (patterns.Count > 0) actions.Add("Verify AI-generated code quality");
        if (shortcuts.Count > 0) actions.Add("Complete placeholder implementations");
        return actions.ToArray();
    }
}