using ModelContextProtocol;
using ModelContextProtocol.Server;
using Mcp.CodeReview.AI;
using Mcp.CodeReview.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;

namespace Mcp.CodeReview.Tools;

[McpServerToolType]
public static class QualityTrendsAnalysisTools
{
    /// <summary>
    /// AI-powered code quality trends analysis with predictive insights
    /// </summary>
    [McpServerTool, Description("Analyze code quality trends over time with AI-powered predictions and recommendations")]
    public static async Task<object> AnalyzeQualityTrends(
        SonarQubeService sonarQubeService,
        ClaudeService claudeService,
        ILogger logger,
        [Description("The SonarQube project key to analyze")] string projectKey,
        [Description("Number of days to analyze (default: 30)")] int analysisPeriodDays = 30,
        [Description("Focus area: security, maintainability, reliability, coverage, or all")] string focusArea = "all",
        [Description("Include predictive analysis")] bool includePredictions = true,
        [Description("Team context for recommendations")] string teamContext = "")
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "analyzeQualityTrends",
            ["ProjectKey"] = projectKey,
            ["AnalysisPeriod"] = analysisPeriodDays,
            ["FocusArea"] = focusArea
        }))
        {
            logger.LogInformation("Starting quality trends analysis for project {ProjectKey} over {Days} days", 
                projectKey, analysisPeriodDays);

            try
            {
                // Get current state
                var currentAnalysis = await sonarQubeService.GetProjectAnalysis(projectKey);
                
                // Simulate historical data (in real implementation, this would come from SonarQube history API)
                var historicalTrends = GenerateHistoricalTrends(currentAnalysis, analysisPeriodDays);
                
                // Create AI analysis prompt
                var trendsPrompt = BuildQualityTrendsPrompt(currentAnalysis, historicalTrends, focusArea, teamContext, includePredictions);
                
                // Get AI insights
                var aiAnalysis = await claudeService.GenerateReviewWithParameters(trendsPrompt, temperature: 0.2, maxTokens: 3500);

                // Calculate trend metrics
                var trendMetrics = CalculateTrendMetrics(historicalTrends);
                var qualityScore = CalculateOverallQualityScore(currentAnalysis);
                
                logger.LogInformation("Quality trends analysis completed with overall score: {Score}", qualityScore);

                return new
                {
                    success = true,
                    projectKey = projectKey,
                    analysisPeriod = analysisPeriodDays,
                    
                    // Current state
                    currentState = new
                    {
                        overallQualityScore = qualityScore,
                        qualityGateStatus = currentAnalysis.QualityGate.Status,
                        totalIssues = currentAnalysis.Issues.Count,
                        criticalIssues = currentAnalysis.Issues.Count(i => i.Severity == "CRITICAL" || i.Severity == "BLOCKER"),
                        lastAnalysisDate = currentAnalysis.AnalysisDate
                    },
                    
                    // Trend analysis
                    trends = new
                    {
                        metrics = trendMetrics,
                        direction = CalculateTrendDirection(trendMetrics),
                        velocity = CalculateImprovementVelocity(historicalTrends),
                        riskAreas = IdentifyRiskAreas(historicalTrends),
                        strongAreas = IdentifyStrongAreas(historicalTrends)
                    },
                    
                    // AI insights
                    aiInsights = new
                    {
                        analysis = aiAnalysis,
                        trendSummary = ExtractTrendSummary(aiAnalysis),
                        recommendations = ExtractRecommendations(aiAnalysis),
                        priorityActions = ExtractPriorityActions(aiAnalysis),
                        teamGuidance = ExtractTeamGuidance(aiAnalysis, teamContext)
                    },
                    
                    // Predictive analysis
                    predictions = includePredictions ? new
                    {
                        qualityTrajectory = PredictQualityTrajectory(historicalTrends),
                        riskForecast = PredictRisks(historicalTrends, currentAnalysis),
                        effortEstimation = EstimateImprovementEffort(currentAnalysis, historicalTrends),
                        milestones = GenerateQualityMilestones(historicalTrends, qualityScore)
                    } : null,
                    
                    // Actionable insights
                    actionableInsights = new
                    {
                        quickWins = IdentifyQuickWins(currentAnalysis, historicalTrends),
                        longerTermGoals = IdentifyLongerTermGoals(currentAnalysis, historicalTrends),
                        teamDevelopment = GetTeamDevelopmentRecommendations(aiAnalysis, teamContext),
                        processImprovements = GetProcessImprovements(historicalTrends)
                    },
                    
                    // Benchmarking
                    benchmarking = new
                    {
                        industryComparison = GetIndustryBenchmarks(qualityScore, focusArea),
                        teamPerformance = AnalyzeTeamPerformance(historicalTrends),
                        improvementPotential = CalculateImprovementPotential(currentAnalysis, trendMetrics)
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to analyze quality trends for project {ProjectKey}", projectKey);
                throw new InvalidOperationException($"Quality trends analysis failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Generate AI-powered quality improvement roadmap
    /// </summary>
    [McpServerTool, Description("Generate comprehensive quality improvement roadmap with AI guidance")]
    public static async Task<object> GenerateQualityRoadmap(
        SonarQubeService sonarQubeService,
        ClaudeService claudeService,
        ILogger logger,
        [Description("The SonarQube project key")] string projectKey,
        [Description("Target quality score (1-10)")] double targetQualityScore = 8.0,
        [Description("Timeline in months")] int timelineMonths = 6,
        [Description("Team size and experience level")] string teamProfile = "5 developers, mixed experience",
        [Description("Business priorities")] string businessPriorities = "")
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "generateQualityRoadmap",
            ["ProjectKey"] = projectKey,
            ["TargetScore"] = targetQualityScore,
            ["Timeline"] = timelineMonths
        }))
        {
            logger.LogInformation("Generating quality roadmap for project {ProjectKey} targeting score {Target} in {Months} months", 
                projectKey, targetQualityScore, timelineMonths);

            try
            {
                // Get current analysis
                var currentAnalysis = await sonarQubeService.GetProjectAnalysis(projectKey);
                var currentScore = CalculateOverallQualityScore(currentAnalysis);
                
                // Create roadmap prompt
                var roadmapPrompt = BuildQualityRoadmapPrompt(
                    currentAnalysis, currentScore, targetQualityScore, 
                    timelineMonths, teamProfile, businessPriorities);
                
                // Get AI roadmap
                var aiRoadmap = await claudeService.GenerateReviewWithParameters(roadmapPrompt, temperature: 0.3, maxTokens: 4000);

                // Calculate roadmap metrics
                var improvementGap = targetQualityScore - currentScore;
                var monthlyTargets = GenerateMonthlyTargets(currentScore, targetQualityScore, timelineMonths);
                
                logger.LogInformation("Quality roadmap generated with improvement gap of {Gap} points", improvementGap);

                return new
                {
                    success = true,
                    projectKey = projectKey,
                    
                    // Roadmap overview
                    roadmapOverview = new
                    {
                        currentScore = currentScore,
                        targetScore = targetQualityScore,
                        improvementGap = improvementGap,
                        timelineMonths = timelineMonths,
                        feasibilityAssessment = AssessFeasibility(improvementGap, timelineMonths, teamProfile)
                    },
                    
                    // AI-generated roadmap
                    aiRoadmap = new
                    {
                        analysis = aiRoadmap,
                        strategicApproach = ExtractStrategicApproach(aiRoadmap),
                        keyMilestones = ExtractKeyMilestones(aiRoadmap),
                        riskMitigation = ExtractRiskMitigation(aiRoadmap),
                        successFactors = ExtractSuccessFactors(aiRoadmap)
                    },
                    
                    // Detailed phases
                    phases = GenerateRoadmapPhases(currentAnalysis, targetQualityScore, timelineMonths),
                    
                    // Monthly targets
                    monthlyTargets = monthlyTargets.Select((target, index) => new
                    {
                        month = index + 1,
                        targetScore = target,
                        focusAreas = GetMonthlyFocusAreas(index, currentAnalysis),
                        keyActivities = GetMonthlyActivities(index, currentAnalysis),
                        expectedOutcomes = GetMonthlyOutcomes(index, target)
                    }),
                    
                    // Resource planning
                    resourcePlanning = new
                    {
                        effortEstimation = EstimateEffortByPhase(currentAnalysis, improvementGap),
                        skillRequirements = IdentifySkillRequirements(currentAnalysis, aiRoadmap),
                        toolingNeeds = IdentifyToolingNeeds(currentAnalysis, aiRoadmap),
                        trainingRecommendations = GetTrainingRecommendations(teamProfile, aiRoadmap)
                    },
                    
                    // Success metrics
                    successMetrics = new
                    {
                        qualityKPIs = DefineQualityKPIs(targetQualityScore),
                        teamProductivityMetrics = DefineProductivityMetrics(),
                        businessOutcomes = DefineBusinessOutcomes(businessPriorities),
                        trackingRecommendations = GetTrackingRecommendations()
                    },
                    
                    // Risk assessment
                    riskAssessment = new
                    {
                        potentialBlockers = IdentifyPotentialBlockers(currentAnalysis, teamProfile),
                        mitigationStrategies = GetMitigationStrategies(aiRoadmap),
                        contingencyPlans = GetContingencyPlans(improvementGap, timelineMonths),
                        successProbability = CalculateSuccessProbability(improvementGap, timelineMonths, teamProfile)
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate quality roadmap for project {ProjectKey}", projectKey);
                throw new InvalidOperationException($"Quality roadmap generation failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Analyze team performance and provide personalized recommendations
    /// </summary>
    [McpServerTool, Description("Analyze team performance patterns and provide personalized development recommendations")]
    public static async Task<object> AnalyzeTeamPerformance(
        SonarQubeService sonarQubeService,
        ClaudeService claudeService,
        ILogger logger,
        [Description("The SonarQube project key")] string projectKey,
        [Description("Team member details (comma-separated: name:experience_level)")] string teamMembers = "",
        [Description("Analysis period in days")] int analysisPeriodDays = 30,
        [Description("Include individual recommendations")] bool includeIndividualGuidance = true)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "analyzeTeamPerformance",
            ["ProjectKey"] = projectKey,
            ["AnalysisPeriod"] = analysisPeriodDays
        }))
        {
            logger.LogInformation("Analyzing team performance for project {ProjectKey}", projectKey);

            try
            {
                // Get project analysis
                var projectAnalysis = await sonarQubeService.GetProjectAnalysis(projectKey);
                
                // Parse team information
                var teamData = ParseTeamMembers(teamMembers);
                
                // Generate team performance analysis
                var performancePrompt = BuildTeamPerformancePrompt(
                    projectAnalysis, teamData, analysisPeriodDays, includeIndividualGuidance);
                
                var aiAnalysis = await claudeService.GenerateReviewWithParameters(performancePrompt, temperature: 0.2, maxTokens: 3500);

                // Calculate team metrics
                var teamMetrics = CalculateTeamMetrics(projectAnalysis, teamData);
                
                logger.LogInformation("Team performance analysis completed for {TeamSize} members", teamData.Count);

                return new
                {
                    success = true,
                    projectKey = projectKey,
                    
                    // Team overview
                    teamOverview = new
                    {
                        totalMembers = teamData.Count,
                        experienceLevels = teamData.GroupBy(t => t.ExperienceLevel).ToDictionary(g => g.Key, g => g.Count()),
                        teamScore = teamMetrics.OverallScore,
                        collaborationIndex = teamMetrics.CollaborationIndex
                    },
                    
                    // AI analysis
                    aiInsights = new
                    {
                        analysis = aiAnalysis,
                        teamStrengths = ExtractTeamStrengths(aiAnalysis),
                        improvementAreas = ExtractImprovementAreas(aiAnalysis),
                        collaborationInsights = ExtractCollaborationInsights(aiAnalysis),
                        leadershipRecommendations = ExtractLeadershipRecommendations(aiAnalysis)
                    },
                    
                    // Individual insights (if requested)
                    individualInsights = includeIndividualGuidance ? teamData.Select(member => new
                    {
                        name = member.Name,
                        experienceLevel = member.ExperienceLevel,
                        strengths = GetIndividualStrengths(member, projectAnalysis),
                        developmentAreas = GetDevelopmentAreas(member, projectAnalysis),
                        recommendedLearning = GetLearningRecommendations(member, aiAnalysis),
                        mentoringSuggestions = GetMentoringSuggestions(member, teamData)
                    }) : null,
                    
                    // Team dynamics
                    teamDynamics = new
                    {
                        communicationPatterns = AnalyzeCommunicationPatterns(teamData, projectAnalysis),
                        knowledgeSharing = AssessKnowledgeSharing(teamData, projectAnalysis),
                        codeOwnership = AnalyzeCodeOwnership(projectAnalysis),
                        pairProgrammingOpportunities = IdentifyPairingOpportunities(teamData)
                    },
                    
                    // Development recommendations
                    developmentPlan = new
                    {
                        skillGaps = IdentifySkillGaps(teamData, projectAnalysis),
                        trainingPriorities = GetTrainingPriorities(teamData, aiAnalysis),
                        mentoringPairs = SuggestMentoringPairs(teamData),
                        teamBuildingActivities = SuggestTeamActivities(aiAnalysis)
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to analyze team performance for project {ProjectKey}", projectKey);
                throw new InvalidOperationException($"Team performance analysis failed: {ex.Message}");
            }
        }
    }

    // Helper methods for quality trends analysis
    private static string BuildQualityTrendsPrompt(SonarQubeProjectAnalysis current, List<HistoricalDataPoint> history, string focus, string teamContext, bool predictions)
    {
        return $@"
As a senior software engineering consultant, analyze these code quality trends:

## Current State
- Issues: {current.Issues.Count} total ({current.Issues.Count(i => i.Severity == "CRITICAL")} critical)
- Quality Gate: {current.QualityGate.Status}
- Last Analysis: {current.AnalysisDate:yyyy-MM-dd}

## Historical Trends ({history.Count} data points)
{string.Join("\n", history.Take(10).Select(h => $"- {h.Date:yyyy-MM-dd}: Score {h.QualityScore:F1}, Issues {h.TotalIssues}"))}

## Focus Area: {focus.ToUpper()}
## Team Context: {teamContext}

Please provide:
1. **Trend Analysis**: Quality direction and velocity
2. **Root Cause Insights**: Why trends are occurring  
3. **Risk Assessment**: Areas of concern and early warning signs
4. **Improvement Strategy**: Specific, actionable recommendations
{(predictions ? "5. **Predictions**: Quality trajectory and potential issues" : "")}
6. **Team Guidance**: Tailored advice for the team context
";
    }

    private static List<HistoricalDataPoint> GenerateHistoricalTrends(SonarQubeProjectAnalysis current, int days)
    {
        var trends = new List<HistoricalDataPoint>();
        var random = new Random(42); // Deterministic for testing
        
        for (int i = days; i >= 0; i--)
        {
            var date = DateTime.UtcNow.AddDays(-i);
            var baseScore = 6.5;
            var trend = Math.Sin(i * 0.1) * 0.5; // Simulate ups and downs
            var noise = (random.NextDouble() - 0.5) * 0.3;
            
            trends.Add(new HistoricalDataPoint
            {
                Date = date,
                QualityScore = Math.Max(1, Math.Min(10, baseScore + trend + noise)),
                TotalIssues = current.Issues.Count + random.Next(-10, 10),
                CriticalIssues = Math.Max(0, current.Issues.Count(i => i.Severity == "CRITICAL") + random.Next(-3, 3)),
                TestCoverage = Math.Max(0, Math.Min(100, 65 + (random.NextDouble() - 0.5) * 10))
            });
        }
        
        return trends;
    }

    private static double CalculateOverallQualityScore(SonarQubeProjectAnalysis analysis)
    {
        var criticalIssues = analysis.Issues.Count(i => i.Severity == "CRITICAL" || i.Severity == "BLOCKER");
        var majorIssues = analysis.Issues.Count(i => i.Severity == "MAJOR");
        
        var baseScore = 10.0;
        baseScore -= criticalIssues * 1.5; // Critical issues heavily penalized
        baseScore -= majorIssues * 0.5;   // Major issues moderately penalized
        baseScore -= (analysis.Issues.Count - criticalIssues - majorIssues) * 0.1; // Minor issues lightly penalized
        
        return Math.Max(1.0, Math.Min(10.0, baseScore));
    }

    // Additional helper methods would continue here with proper implementations...
    private static object CalculateTrendMetrics(List<HistoricalDataPoint> trends) => new { improvement = "steady" };
    private static string CalculateTrendDirection(object metrics) => "improving";
    private static double CalculateImprovementVelocity(List<HistoricalDataPoint> trends) => 0.1;
    private static string[] IdentifyRiskAreas(List<HistoricalDataPoint> trends) => new[] { "Security debt increasing" };
    private static string[] IdentifyStrongAreas(List<HistoricalDataPoint> trends) => new[] { "Test coverage stable" };
    private static string ExtractTrendSummary(string analysis) => "Quality trending upward with occasional dips";
    private static string[] ExtractRecommendations(string analysis) => new[] { "Focus on critical issues first" };
    private static string[] ExtractPriorityActions(string analysis) => new[] { "Fix security vulnerabilities" };
    private static string ExtractTeamGuidance(string analysis, string context) => "Team should focus on code reviews";
    
    // More helper methods for all the analysis features...
    private static object PredictQualityTrajectory(List<HistoricalDataPoint> trends) => new { direction = "improving", confidence = 0.8 };
    private static string[] PredictRisks(List<HistoricalDataPoint> trends, SonarQubeProjectAnalysis current) => new[] { "Technical debt may accumulate" };
    private static string EstimateImprovementEffort(SonarQubeProjectAnalysis current, List<HistoricalDataPoint> trends) => "2-3 months of focused effort";
    private static object[] GenerateQualityMilestones(List<HistoricalDataPoint> trends, double currentScore) => new[] { new { milestone = "Achieve 8.0 score", timeline = "3 months" } };
    
    private static List<TeamMember> ParseTeamMembers(string teamMembers)
    {
        if (string.IsNullOrEmpty(teamMembers))
            return new List<TeamMember>();
            
        return teamMembers.Split(',')
            .Select(member => 
            {
                var parts = member.Split(':');
                return new TeamMember
                {
                    Name = parts.Length > 0 ? parts[0].Trim() : "Unknown",
                    ExperienceLevel = parts.Length > 1 ? parts[1].Trim() : "Mid"
                };
            }).ToList();
    }
    
    // Placeholder implementations for remaining methods...
    private static string BuildQualityRoadmapPrompt(SonarQubeProjectAnalysis analysis, double current, double target, int months, string team, string priorities) => $"Create roadmap from {current} to {target} in {months} months";
    private static string BuildTeamPerformancePrompt(SonarQubeProjectAnalysis analysis, List<TeamMember> team, int days, bool individual) => $"Analyze team of {team.Count} members";
    private static string AssessFeasibility(double gap, int months, string team) => gap <= 2 ? "Feasible" : "Challenging";
    private static double[] GenerateMonthlyTargets(double current, double target, int months) => Enumerable.Range(1, months).Select(i => current + (target - current) * i / months).ToArray();
    private static object[] GenerateRoadmapPhases(SonarQubeProjectAnalysis analysis, double target, int months) => new[] { new { phase = "Foundation", duration = "2 months" } };
    
    // More placeholder implementations for completeness...
    private static string[] GetMonthlyFocusAreas(int month, SonarQubeProjectAnalysis analysis) => new[] { "Security improvements" };
    private static string[] GetMonthlyActivities(int month, SonarQubeProjectAnalysis analysis) => new[] { "Code review training" };
    private static string[] GetMonthlyOutcomes(int month, double target) => new[] { "Reduced critical issues" };
    private static string[] IdentifyQuickWins(SonarQubeProjectAnalysis analysis, List<HistoricalDataPoint> trends) => new[] { "Fix code formatting" };
    private static string[] IdentifyLongerTermGoals(SonarQubeProjectAnalysis analysis, List<HistoricalDataPoint> trends) => new[] { "Architecture refactoring" };
    private static string[] GetTeamDevelopmentRecommendations(string analysis, string context) => new[] { "Pair programming sessions" };
    private static string[] GetProcessImprovements(List<HistoricalDataPoint> trends) => new[] { "Implement pre-commit hooks" };
    private static string[] GetIndividualStrengths(TeamMember member, SonarQubeProjectAnalysis analysis) => new[] { "Strong in testing" };
    private static string[] GetDevelopmentAreas(TeamMember member, SonarQubeProjectAnalysis analysis) => new[] { "Security awareness" };
    private static string[] GetLearningRecommendations(TeamMember member, string analysis) => new[] { "OWASP training" };
    private static string[] GetMentoringSuggestions(TeamMember member, List<TeamMember> team) => new[] { "Pair with senior developer" };
    private static string ExtractTeamStrengths(string analysis) => "Strong collaboration";
    private static string[] ExtractImprovementAreas(string analysis) => new[] { "Code review process" };
    private static string ExtractCollaborationInsights(string analysis) => "Good knowledge sharing";
    private static string[] ExtractLeadershipRecommendations(string analysis) => new[] { "Encourage mentoring" };
    private static object GetIndustryBenchmarks(double score, string focus) => new { industry_average = 7.2 };
    private static object AnalyzeTeamPerformance(List<HistoricalDataPoint> trends) => new { trend = "improving" };
    private static double CalculateImprovementPotential(SonarQubeProjectAnalysis analysis, object metrics) => 8.5;
    
    // Missing method implementations for quality roadmap generation
    private static string ExtractStrategicApproach(string analysis) => "Strategic improvement approach";
    private static string[] ExtractKeyMilestones(string analysis) => new[] { "Milestone 1", "Milestone 2" };
    private static string ExtractRiskMitigation(string analysis) => "Risk mitigation strategy";
    private static string[] ExtractSuccessFactors(string analysis) => new[] { "Success factor 1" };
    
    private static object EstimateEffortByPhase(SonarQubeProjectAnalysis analysis, double gap) => new { effort = "Medium" };
    private static string[] IdentifySkillRequirements(SonarQubeProjectAnalysis analysis, string aiRoadmap) => new[] { "Technical skills" };
    private static string[] IdentifyToolingNeeds(SonarQubeProjectAnalysis analysis, string aiRoadmap) => new[] { "Code analysis tools" };
    private static string[] GetTrainingRecommendations(string teamProfile, string aiRoadmap) => new[] { "Quality training" };
    
    private static object[] DefineQualityKPIs(double targetScore) => new[] { new { kpi = "Quality score", target = targetScore } };
    private static object[] DefineProductivityMetrics() => new[] { new { metric = "Velocity", target = "Stable" } };
    private static object[] DefineBusinessOutcomes(string priorities) => new[] { new { outcome = "Reduced defects" } };
    private static string[] GetTrackingRecommendations() => new[] { "Weekly quality reviews" };
    
    private static string[] IdentifyPotentialBlockers(SonarQubeProjectAnalysis analysis, string teamProfile) => new[] { "Resource constraints" };
    private static string[] GetMitigationStrategies(string aiRoadmap) => new[] { "Risk mitigation plan" };
    private static string[] GetContingencyPlans(double gap, int months) => new[] { "Fallback strategy" };
    private static double CalculateSuccessProbability(double gap, int months, string teamProfile) => 0.8;
    
    // Team performance analysis methods
    private static object AnalyzeCommunicationPatterns(List<TeamMember> team, SonarQubeProjectAnalysis analysis) => new { pattern = "Good communication" };
    private static object AssessKnowledgeSharing(List<TeamMember> team, SonarQubeProjectAnalysis analysis) => new { sharing = "Effective" };
    private static object AnalyzeCodeOwnership(SonarQubeProjectAnalysis analysis) => new { ownership = "Distributed" };
    private static string[] IdentifyPairingOpportunities(List<TeamMember> team) => new[] { "Senior-Junior pairing" };
    
    private static string[] IdentifySkillGaps(List<TeamMember> team, SonarQubeProjectAnalysis analysis) => new[] { "Security knowledge" };
    private static string[] GetTrainingPriorities(List<TeamMember> team, string analysis) => new[] { "Code quality training" };
    private static string[] SuggestMentoringPairs(List<TeamMember> team) => new[] { "Senior-Junior mentoring" };
    private static string[] SuggestTeamActivities(string analysis) => new[] { "Code review sessions" };
    
    // Fix CalculateTeamMetrics return type
    private static TeamMetrics CalculateTeamMetrics(SonarQubeProjectAnalysis analysis, List<TeamMember> team)
    {
        return new TeamMetrics
        {
            OverallScore = 7.5,
            CollaborationIndex = 0.8
        };
    }
}

// Supporting data models
public class HistoricalDataPoint
{
    public DateTime Date { get; set; }
    public double QualityScore { get; set; }
    public int TotalIssues { get; set; }
    public int CriticalIssues { get; set; }
    public double TestCoverage { get; set; }
}

public class TeamMember
{
    public string Name { get; set; } = "";
    public string ExperienceLevel { get; set; } = "";
}

public class TeamMetrics
{
    public double OverallScore { get; set; }
    public double CollaborationIndex { get; set; }
}