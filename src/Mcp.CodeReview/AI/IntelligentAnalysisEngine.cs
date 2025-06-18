using Mcp.CodeReview.Services;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mcp.CodeReview.AI;

public class IntelligentAnalysisEngine
{
    private readonly ClaudeService _claudeService;
    private readonly ILogger<IntelligentAnalysisEngine> _logger;
    private readonly Dictionary<string, AnalysisTemplate> _analysisTemplates;

    public IntelligentAnalysisEngine(ClaudeService claudeService, ILogger<IntelligentAnalysisEngine> logger)
    {
        _claudeService = claudeService;
        _logger = logger;
        _analysisTemplates = InitializeAnalysisTemplates();
    }

    /// <summary>
    /// AI-powered security vulnerability detection with specific CVE pattern matching
    /// </summary>
    public async Task<SecurityAnalysisResult> AnalyzeSecurityVulnerabilities(string code, string language, List<string> dependencies)
    {
        var prompt = BuildSecurityAnalysisPrompt(code, language, dependencies);
        
        var analysis = await _claudeService.GenerateReview(prompt);
        
        return new SecurityAnalysisResult
        {
            VulnerabilityScore = ExtractVulnerabilityScore(analysis),
            SecurityIssues = ExtractSecurityIssues(analysis),
            Recommendations = ExtractSecurityRecommendations(analysis),
            CveReferences = ExtractCveReferences(analysis),
            ComplianceViolations = ExtractComplianceViolations(analysis)
        };
    }

    /// <summary>
    /// Intelligent PR risk assessment using multiple AI factors
    /// </summary>
    public async Task<PRRiskAssessment> AssessPullRequestRisk(PRAnalysisContext context)
    {
        var riskFactors = new List<RiskFactor>();

        // Analyze code complexity
        var complexityRisk = await AnalyzeComplexityRisk(context);
        riskFactors.Add(complexityRisk);

        // Analyze change impact
        var impactRisk = await AnalyzeChangeImpact(context);
        riskFactors.Add(impactRisk);

        // Analyze security implications
        var securityRisk = await AnalyzeSecurityRisk(context);
        riskFactors.Add(securityRisk);

        // Analyze testing coverage
        var testingRisk = await AnalyzeTestingRisk(context);
        riskFactors.Add(testingRisk);

        // AI-powered overall risk calculation
        var overallRisk = await CalculateOverallRisk(riskFactors, context);

        return new PRRiskAssessment
        {
            OverallRiskLevel = overallRisk.Level,
            RiskScore = overallRisk.Score,
            RiskFactors = riskFactors,
            RecommendedActions = overallRisk.RecommendedActions,
            ReviewerSuggestions = await SuggestOptimalReviewers(context),
            DeploymentStrategy = await SuggestDeploymentStrategy(overallRisk.Level)
        };
    }

    /// <summary>
    /// AI-powered test case generation suggestions
    /// </summary>
    public async Task<TestSuggestions> GenerateTestSuggestions(string code, string language, string[] existingTests)
    {
        var prompt = $@"
Analyze this {language} code and suggest comprehensive test cases:

CODE:
```{language}
{code}
```

EXISTING TESTS:
{string.Join("\n", existingTests.Select((t, i) => $"{i + 1}. {t}"))}

Generate suggestions for:
1. Unit tests for edge cases
2. Integration test scenarios
3. Performance test cases
4. Security test scenarios
5. Error handling tests

Focus on test cases that are MISSING from the existing tests.
Provide specific test methods with assertions.
Include both positive and negative test cases.
Consider boundary conditions and error states.
";

        var suggestions = await _claudeService.GenerateReview(prompt);
        
        return new TestSuggestions
        {
            UnitTests = ExtractTestCases(suggestions, "Unit"),
            IntegrationTests = ExtractTestCases(suggestions, "Integration"),
            SecurityTests = ExtractTestCases(suggestions, "Security"),
            PerformanceTests = ExtractTestCases(suggestions, "Performance"),
            TestCoverageGaps = ExtractCoverageGaps(suggestions),
            MockingStrategies = ExtractMockingStrategies(suggestions)
        };
    }

    /// <summary>
    /// Smart code pattern detection and architectural suggestions
    /// </summary>
    public async Task<CodePatternAnalysis> AnalyzeCodePatterns(string codebase, string language)
    {
        var prompt = $@"
Analyze this {language} codebase for architectural patterns, anti-patterns, and improvement opportunities:

CODEBASE:
```{language}
{codebase}
```

Identify:
1. Design patterns used (and misused)
2. SOLID principle violations
3. Performance anti-patterns
4. Maintainability issues
5. Refactoring opportunities
6. Architectural improvements

Provide specific code examples and suggested refactoring.
Focus on actionable improvements with business impact.
";

        var analysis = await _claudeService.GenerateReview(prompt);
        
        return new CodePatternAnalysis
        {
            DetectedPatterns = ExtractDetectedPatterns(analysis),
            AntiPatterns = ExtractAntiPatterns(analysis),
            RefactoringOpportunities = ExtractRefactoringOpportunities(analysis),
            ArchitecturalSuggestions = ExtractArchitecturalSuggestions(analysis),
            PerformanceOptimizations = ExtractPerformanceOptimizations(analysis),
            TechnicalDebtScore = CalculateTechnicalDebtScore(analysis)
        };
    }

    /// <summary>
    /// Context-aware intelligent caching decisions
    /// </summary>
    public async Task<CachingStrategy> OptimizeCachingStrategy(string codeAnalysis, List<string> performanceMetrics)
    {
        var prompt = $@"
Based on this code analysis and performance data, suggest optimal caching strategies:

CODE ANALYSIS:
{codeAnalysis}

PERFORMANCE METRICS:
{string.Join("\n", performanceMetrics)}

Suggest:
1. What should be cached
2. Cache invalidation strategies
3. Cache key patterns
4. TTL recommendations
5. Memory vs Redis trade-offs
6. Cache warming strategies

Focus on measurable performance improvements.
";

        var strategy = await _claudeService.GenerateReview(prompt);
        
        return new CachingStrategy
        {
            CacheTargets = ExtractCacheTargets(strategy),
            InvalidationStrategies = ExtractInvalidationStrategies(strategy),
            TtlRecommendations = ExtractTtlRecommendations(strategy),
            ImplementationPlan = ExtractImplementationPlan(strategy)
        };
    }

    /// <summary>
    /// AI-powered code quality predictions based on historical data
    /// </summary>
    public async Task<QualityPrediction> PredictCodeQuality(string diff, List<HistoricalDataPoint> historicalData)
    {
        var prompt = $@"
Based on this code change and historical quality data, predict the likelihood of:

CODE CHANGE:
{diff}

HISTORICAL DATA:
{JsonSerializer.Serialize(historicalData, new JsonSerializerOptions { WriteIndented = true })}

Predict:
1. Bug introduction probability (0-1 scale)
2. Performance impact likelihood
3. Maintainability score change
4. Security risk introduction
5. Test failure probability
6. Rollback likelihood

Provide reasoning for each prediction.
";

        var prediction = await _claudeService.GenerateReview(prompt);
        
        return new QualityPrediction
        {
            BugProbability = ExtractBugProbability(prediction),
            PerformanceImpact = ExtractPerformanceImpact(prediction),
            MaintainabilityScore = ExtractMaintainabilityScore(prediction),
            SecurityRisk = ExtractSecurityRisk(prediction),
            TestFailureProbability = ExtractTestFailureProbability(prediction),
            Confidence = ExtractPredictionConfidence(prediction),
            Reasoning = ExtractReasoning(prediction)
        };
    }

    /// <summary>
    /// Intelligent merge conflict resolution suggestions
    /// </summary>
    public async Task<ConflictResolution> SuggestConflictResolution(string conflictedCode, string branchACode, string branchBCode, string baseCode)
    {
        var prompt = $@"
Analyze this merge conflict and suggest the best resolution strategy:

BASE CODE:
```
{baseCode}
```

BRANCH A CHANGES:
```
{branchACode}
```

BRANCH B CHANGES:
```
{branchBCode}
```

CONFLICT:
```
{conflictedCode}
```

Suggest:
1. Optimal merge strategy
2. Specific resolution steps
3. Potential issues with each approach
4. Code that preserves both intents if possible
5. Test scenarios to validate the merge

Prioritize functionality preservation and code quality.
";

        var resolution = await _claudeService.GenerateReview(prompt);
        
        return new ConflictResolution
        {
            RecommendedStrategy = ExtractMergeStrategy(resolution),
            ResolvedCode = ExtractResolvedCode(resolution),
            ResolutionSteps = ExtractResolutionSteps(resolution),
            RiskAssessment = ExtractMergeRiskAssessment(resolution),
            TestingRecommendations = ExtractTestingRecommendations(resolution)
        };
    }

    private Dictionary<string, AnalysisTemplate> InitializeAnalysisTemplates()
    {
        return new Dictionary<string, AnalysisTemplate>
        {
            ["security"] = new AnalysisTemplate
            {
                Focus = "Security vulnerabilities, injection attacks, authentication, authorization",
                Keywords = new[] { "sql injection", "xss", "csrf", "authentication", "authorization", "encryption" },
                Severity = "High"
            },
            ["performance"] = new AnalysisTemplate
            {
                Focus = "Performance bottlenecks, memory leaks, inefficient algorithms",
                Keywords = new[] { "performance", "memory", "cpu", "optimization", "bottleneck" },
                Severity = "Medium"
            },
            ["maintainability"] = new AnalysisTemplate
            {
                Focus = "Code complexity, readability, documentation, technical debt",
                Keywords = new[] { "complexity", "maintainability", "documentation", "refactor" },
                Severity = "Low"
            }
        };
    }

    private string BuildSecurityAnalysisPrompt(string code, string language, List<string> dependencies)
    {
        var knownVulnerabilities = GetKnownVulnerabilities(dependencies);
        
        return $@"
SECURITY ANALYSIS REQUEST for {language} code:

CODE TO ANALYZE:
```{language}
{code}
```

DEPENDENCIES:
{string.Join("\n", dependencies)}

KNOWN VULNERABILITY PATTERNS:
{string.Join("\n", knownVulnerabilities)}

Please analyze for:
1. OWASP Top 10 vulnerabilities
2. Language-specific security issues
3. Dependency vulnerabilities
4. Authentication/authorization flaws
5. Data validation issues
6. Cryptographic weaknesses
7. Information disclosure risks

Provide:
- Vulnerability severity (1-10 scale)
- Specific code locations
- CVE references if applicable
- Remediation steps
- Compliance impact (SOC2, GDPR, etc.)

Format response as structured analysis with clear severity ratings.
";
    }

    private List<string> GetKnownVulnerabilities(List<string> dependencies)
    {
        // In a real implementation, this would query a vulnerability database
        return new List<string>
        {
            "Outdated package versions with known CVEs",
            "Packages with active security advisories",
            "Transitive dependency vulnerabilities"
        };
    }

    // Helper methods for extracting structured data from AI responses
    private double ExtractVulnerabilityScore(string analysis)
    {
        var match = Regex.Match(analysis, @"vulnerability.*?score.*?(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        return match.Success ? double.Parse(match.Groups[1].Value) : 0.0;
    }

    private List<SecurityIssue> ExtractSecurityIssues(string analysis)
    {
        // Parse AI response for structured security issues
        var issues = new List<SecurityIssue>();
        var issuePattern = @"(?:ISSUE|VULNERABILITY):\s*(.+?)(?:\n|$)";
        var matches = Regex.Matches(analysis, issuePattern, RegexOptions.IgnoreCase);
        
        foreach (Match match in matches)
        {
            issues.Add(new SecurityIssue
            {
                Description = match.Groups[1].Value.Trim(),
                Severity = ExtractSeverityFromContext(match.Value),
                Line = ExtractLineNumber(match.Value)
            });
        }
        
        return issues;
    }

    private string ExtractSeverityFromContext(string context)
    {
        if (context.Contains("critical", StringComparison.OrdinalIgnoreCase)) return "Critical";
        if (context.Contains("high", StringComparison.OrdinalIgnoreCase)) return "High";
        if (context.Contains("medium", StringComparison.OrdinalIgnoreCase)) return "Medium";
        return "Low";
    }

    private int? ExtractLineNumber(string context)
    {
        var match = Regex.Match(context, @"line\s*(\d+)", RegexOptions.IgnoreCase);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    // Additional extraction methods would be implemented here...
    private List<string> ExtractSecurityRecommendations(string analysis) => new();
    private List<string> ExtractCveReferences(string analysis) => new();
    private List<string> ExtractComplianceViolations(string analysis) => new();
    private Task<RiskFactor> AnalyzeComplexityRisk(PRAnalysisContext context) => Task.FromResult(new RiskFactor());
    private Task<RiskFactor> AnalyzeChangeImpact(PRAnalysisContext context) => Task.FromResult(new RiskFactor());
    private Task<RiskFactor> AnalyzeSecurityRisk(PRAnalysisContext context) => Task.FromResult(new RiskFactor());
    private Task<RiskFactor> AnalyzeTestingRisk(PRAnalysisContext context) => Task.FromResult(new RiskFactor());
    private Task<(RiskLevel Level, double Score, List<string> RecommendedActions)> CalculateOverallRisk(List<RiskFactor> factors, PRAnalysisContext context) => 
        Task.FromResult((RiskLevel.Medium, 0.5, new List<string>()));
    private Task<List<string>> SuggestOptimalReviewers(PRAnalysisContext context) => Task.FromResult(new List<string>());
    private Task<string> SuggestDeploymentStrategy(RiskLevel level) => Task.FromResult("Standard deployment");
    private List<TestCase> ExtractTestCases(string suggestions, string type) => new();
    private List<string> ExtractCoverageGaps(string suggestions) => new();
    private List<string> ExtractMockingStrategies(string suggestions) => new();
    private List<CodePattern> ExtractDetectedPatterns(string analysis) => new();
    private List<AntiPattern> ExtractAntiPatterns(string analysis) => new();
    private List<RefactoringOpportunity> ExtractRefactoringOpportunities(string analysis) => new();
    private List<string> ExtractArchitecturalSuggestions(string analysis) => new();
    private List<string> ExtractPerformanceOptimizations(string analysis) => new();
    private double CalculateTechnicalDebtScore(string analysis) => 0.0;
    private List<CacheTarget> ExtractCacheTargets(string strategy) => new();
    private List<string> ExtractInvalidationStrategies(string strategy) => new();
    private Dictionary<string, TimeSpan> ExtractTtlRecommendations(string strategy) => new();
    private List<string> ExtractImplementationPlan(string strategy) => new();
    private double ExtractBugProbability(string prediction) => 0.0;
    private string ExtractPerformanceImpact(string prediction) => "";
    private double ExtractMaintainabilityScore(string prediction) => 0.0;
    private double ExtractSecurityRisk(string prediction) => 0.0;
    private double ExtractTestFailureProbability(string prediction) => 0.0;
    private double ExtractPredictionConfidence(string prediction) => 0.0;
    private string ExtractReasoning(string prediction) => "";
    private string ExtractMergeStrategy(string resolution) => "";
    private string ExtractResolvedCode(string resolution) => "";
    private List<string> ExtractResolutionSteps(string resolution) => new();
    private string ExtractMergeRiskAssessment(string resolution) => "";
    private List<string> ExtractTestingRecommendations(string resolution) => new();
}

// Supporting data models
public class SecurityAnalysisResult
{
    public double VulnerabilityScore { get; set; }
    public List<SecurityIssue> SecurityIssues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<string> CveReferences { get; set; } = new();
    public List<string> ComplianceViolations { get; set; } = new();
}

public class SecurityIssue
{
    public string Description { get; set; } = "";
    public string Severity { get; set; } = "";
    public int? Line { get; set; }
    public string Category { get; set; } = "";
}

public class PRRiskAssessment
{
    public RiskLevel OverallRiskLevel { get; set; }
    public double RiskScore { get; set; }
    public List<RiskFactor> RiskFactors { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
    public List<string> ReviewerSuggestions { get; set; } = new();
    public string DeploymentStrategy { get; set; } = "";
}

public class RiskFactor
{
    public string Name { get; set; } = "";
    public RiskLevel Level { get; set; }
    public double Impact { get; set; }
    public string Description { get; set; } = "";
}

public enum RiskLevel { Low, Medium, High, Critical }

public class PRAnalysisContext
{
    public string Diff { get; set; } = "";
    public List<string> ModifiedFiles { get; set; } = new();
    public string Author { get; set; } = "";
    public List<string> Reviewers { get; set; } = new();
    public int LinesChanged { get; set; }
    public string BranchName { get; set; } = "";
}

public class TestSuggestions
{
    public List<TestCase> UnitTests { get; set; } = new();
    public List<TestCase> IntegrationTests { get; set; } = new();
    public List<TestCase> SecurityTests { get; set; } = new();
    public List<TestCase> PerformanceTests { get; set; } = new();
    public List<string> TestCoverageGaps { get; set; } = new();
    public List<string> MockingStrategies { get; set; } = new();
}

public class TestCase
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string TestCode { get; set; } = "";
    public string Priority { get; set; } = "";
}

public class CodePatternAnalysis
{
    public List<CodePattern> DetectedPatterns { get; set; } = new();
    public List<AntiPattern> AntiPatterns { get; set; } = new();
    public List<RefactoringOpportunity> RefactoringOpportunities { get; set; } = new();
    public List<string> ArchitecturalSuggestions { get; set; } = new();
    public List<string> PerformanceOptimizations { get; set; } = new();
    public double TechnicalDebtScore { get; set; }
}

public class CodePattern
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string Location { get; set; } = "";
}

public class AntiPattern
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Impact { get; set; } = "";
    public string Suggestion { get; set; } = "";
}

public class RefactoringOpportunity
{
    public string Description { get; set; } = "";
    public string Benefit { get; set; } = "";
    public string Effort { get; set; } = "";
    public string Priority { get; set; } = "";
}

public class CachingStrategy
{
    public List<CacheTarget> CacheTargets { get; set; } = new();
    public List<string> InvalidationStrategies { get; set; } = new();
    public Dictionary<string, TimeSpan> TtlRecommendations { get; set; } = new();
    public List<string> ImplementationPlan { get; set; } = new();
}

public class CacheTarget
{
    public string Target { get; set; } = "";
    public string Reason { get; set; } = "";
    public TimeSpan Ttl { get; set; }
}

public class QualityPrediction
{
    public double BugProbability { get; set; }
    public string PerformanceImpact { get; set; } = "";
    public double MaintainabilityScore { get; set; }
    public double SecurityRisk { get; set; }
    public double TestFailureProbability { get; set; }
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = "";
}

public class ConflictResolution
{
    public string RecommendedStrategy { get; set; } = "";
    public string ResolvedCode { get; set; } = "";
    public List<string> ResolutionSteps { get; set; } = new();
    public string RiskAssessment { get; set; } = "";
    public List<string> TestingRecommendations { get; set; } = new();
}

public class AnalysisTemplate
{
    public string Focus { get; set; } = "";
    public string[] Keywords { get; set; } = Array.Empty<string>();
    public string Severity { get; set; } = "";
}

public class HistoricalDataPoint
{
    public DateTime Date { get; set; }
    public string Author { get; set; } = "";
    public int LinesChanged { get; set; }
    public int BugsIntroduced { get; set; }
    public double TestCoverage { get; set; }
    public string[] FilesModified { get; set; } = Array.Empty<string>();
}