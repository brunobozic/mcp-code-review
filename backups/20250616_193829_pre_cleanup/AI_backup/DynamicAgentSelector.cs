using Mcp.CodeReview.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Mcp.CodeReview.AI;

/// <summary>
/// Dynamic agent selection based on code characteristics, complexity, and context
/// Implements 2024 best practices for adaptive multi-agent orchestration
/// </summary>
public class DynamicAgentSelector
{
    private readonly ILogger<DynamicAgentSelector> _logger;
    private readonly CodeCharacteristicsAnalyzer _analyzer;

    public DynamicAgentSelector(ILogger<DynamicAgentSelector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _analyzer = new CodeCharacteristicsAnalyzer();
    }

    /// <summary>
    /// Select optimal agents based on code analysis and context
    /// </summary>
    public async Task<AgentSelectionResult> SelectOptimalAgents(
        string code,
        string language,
        Dictionary<string, object> context,
        ReviewOptions options)
    {
        _logger.LogInformation("Analyzing code characteristics for dynamic agent selection");

        try
        {
            // Analyze code characteristics
            var characteristics = await _analyzer.AnalyzeCodeCharacteristics(code, language, context);
            
            // Start with core agents (always included)
            var selectedAgents = new List<AgentSelection>
            {
                new AgentSelection(AgentType.CodeQualityReviewer, AgentPriority.High, "Core code quality analysis"),
                new AgentSelection(AgentType.SecurityExpert, AgentPriority.High, "Essential security review")
            };

            // Add agents based on code characteristics
            AddCharacteristicBasedAgents(selectedAgents, characteristics);
            
            // Add agents based on context and options
            AddContextBasedAgents(selectedAgents, context, options);
            
            // Add agents based on team and business factors
            AddBusinessContextAgents(selectedAgents, context);

            // Optimize agent selection (remove duplicates, balance load)
            var optimizedAgents = OptimizeAgentSelection(selectedAgents, characteristics);

            var result = new AgentSelectionResult
            {
                SelectedAgents = optimizedAgents,
                CodeCharacteristics = characteristics,
                SelectionReasoning = BuildSelectionReasoning(optimizedAgents, characteristics),
                EstimatedAnalysisTime = EstimateAnalysisTime(optimizedAgents, characteristics),
                RecommendedExecutionOrder = DetermineExecutionOrder(optimizedAgents, characteristics)
            };

            _logger.LogInformation("Selected {AgentCount} agents for analysis: {Agents}", 
                optimizedAgents.Count, 
                string.Join(", ", optimizedAgents.Select(a => a.AgentType.ToString())));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select optimal agents");
            
            // Fallback to default agent selection
            return CreateDefaultAgentSelection();
        }
    }

    /// <summary>
    /// Evaluate agent fitness for a specific subtask
    /// </summary>
    public async Task<AgentFitnessScore> EvaluateAgentFitness(
        AgentType agentType,
        string subtask,
        CodeCharacteristics characteristics,
        Dictionary<string, object> context)
    {
        try
        {
            var baseScore = GetBaseAgentScore(agentType, characteristics);
            var contextScore = GetContextualScore(agentType, context);
            var specialization = GetSpecializationMatch(agentType, subtask, characteristics);
            
            var fitnessScore = new AgentFitnessScore
            {
                AgentType = agentType,
                OverallScore = (baseScore + contextScore + specialization) / 3.0,
                BaseCompetency = baseScore,
                ContextualRelevance = contextScore,
                SpecializationMatch = specialization,
                EstimatedCost = EstimateAgentCost(agentType, characteristics),
                EstimatedTime = EstimateAgentTime(agentType, characteristics),
                Reasoning = $"Base: {baseScore:F2}, Context: {contextScore:F2}, Specialization: {specialization:F2}"
            };

            return fitnessScore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate agent fitness for {AgentType}", agentType);
            return new AgentFitnessScore { AgentType = agentType, OverallScore = 0.5 };
        }
    }

    // Private implementation methods

    private void AddCharacteristicBasedAgents(List<AgentSelection> agents, CodeCharacteristics characteristics)
    {
        // Performance-critical code
        if (characteristics.PerformanceCritical || characteristics.HasLoops || characteristics.HasRecursion)
        {
            agents.Add(new AgentSelection(AgentType.PerformanceAnalyst, AgentPriority.High, 
                "Performance-critical code patterns detected"));
        }

        // Architecture and design patterns
        if (characteristics.Complexity > 7 || characteristics.HasDesignPatterns || characteristics.HasAbstractions)
        {
            agents.Add(new AgentSelection(AgentType.ArchitectureExpert, AgentPriority.High, 
                "Complex architecture requiring expert review"));
        }

        // Enterprise architecture standards (always activate for C# projects)
        if (characteristics.Language.ToLower().Contains("c#") || characteristics.HasDesignPatterns)
        {
            agents.Add(new AgentSelection(AgentType.ArchitectureStandardsAgent, AgentPriority.Medium, 
                "Enterprise architecture standards and patterns enforcement"));
        }

        // Database and data access
        if (characteristics.HasDatabaseCalls || characteristics.HasDataAccess)
        {
            agents.Add(new AgentSelection(AgentType.SecurityExpert, AgentPriority.High, 
                "Database access requires security review"));
        }

        // Async and concurrency
        if (characteristics.HasAsyncPatterns || characteristics.HasConcurrency)
        {
            agents.Add(new AgentSelection(AgentType.PerformanceAnalyst, AgentPriority.Medium, 
                "Async/concurrency patterns need performance review"));
        }

        // Testing and testability
        if (characteristics.HasTests || characteristics.TestabilityScore < 0.6)
        {
            agents.Add(new AgentSelection(AgentType.TestingSpecialist, AgentPriority.Medium, 
                "Testing improvements or test review needed"));
        }

        // Business logic heavy
        if (characteristics.BusinessLogicHeavy || characteristics.HasDomainConcepts)
        {
            agents.Add(new AgentSelection(AgentType.DomainExpert, AgentPriority.Medium, 
                "Business logic requires domain expertise"));
        }

        // AI-generated code patterns
        if (characteristics.SuspiciousPatterns.Any() || characteristics.HasCommentedCode)
        {
            agents.Add(new AgentSelection(AgentType.AICodeDetective, AgentPriority.High, 
                "Potential AI-generated or suspicious code patterns"));
        }
    }

    private void AddContextBasedAgents(List<AgentSelection> agents, Dictionary<string, object> context, ReviewOptions options)
    {
        // Team experience level
        if (context.TryGetValue("TeamExperience", out var teamExp) && 
            teamExp.ToString()?.ToLower() == "junior")
        {
            agents.Add(new AgentSelection(AgentType.DeveloperMentor, AgentPriority.High, 
                "Junior team needs mentoring and guidance"));
        }

        // Business criticality
        if (context.TryGetValue("BusinessCritical", out var critical) && 
            critical is bool isCritical && isCritical)
        {
            agents.Add(new AgentSelection(AgentType.ArchitectureExpert, AgentPriority.High, 
                "Business-critical code requires architecture review"));
        }

        // Review options
        if (options.IncludePerformanceAnalysis)
        {
            agents.Add(new AgentSelection(AgentType.PerformanceAnalyst, AgentPriority.High, 
                "Performance analysis explicitly requested"));
        }

        if (options.IncludeTestSuggestions)
        {
            agents.Add(new AgentSelection(AgentType.TestingSpecialist, AgentPriority.Medium, 
                "Test suggestions explicitly requested"));
        }

        if (options.IncludeRefactoringSuggestions)
        {
            agents.Add(new AgentSelection(AgentType.CodeQualityReviewer, AgentPriority.High, 
                "Refactoring suggestions explicitly requested"));
        }
    }

    private void AddBusinessContextAgents(List<AgentSelection> agents, Dictionary<string, object> context)
    {
        if (context.TryGetValue("BusinessDomain", out var domain))
        {
            var domainStr = domain.ToString()?.ToLower();
            
            if (domainStr?.Contains("financial") == true || domainStr?.Contains("payment") == true)
            {
                agents.Add(new AgentSelection(AgentType.SecurityExpert, AgentPriority.Critical, 
                    "Financial domain requires enhanced security"));
            }
            
            if (domainStr?.Contains("healthcare") == true || domainStr?.Contains("medical") == true)
            {
                agents.Add(new AgentSelection(AgentType.SecurityExpert, AgentPriority.Critical, 
                    "Healthcare domain requires compliance review"));
            }
            
            if (domainStr?.Contains("ecommerce") == true || domainStr?.Contains("retail") == true)
            {
                agents.Add(new AgentSelection(AgentType.PerformanceAnalyst, AgentPriority.High, 
                    "E-commerce requires performance optimization"));
            }
        }
    }

    private List<AgentSelection> OptimizeAgentSelection(List<AgentSelection> agents, CodeCharacteristics characteristics)
    {
        // Remove duplicates (keep highest priority)
        var deduplicated = agents
            .GroupBy(a => a.AgentType)
            .Select(g => g.OrderBy(a => a.Priority).First())
            .ToList();

        // Limit total agents based on code complexity
        var maxAgents = characteristics.Complexity switch
        {
            <= 3 => 4,
            <= 7 => 6,
            _ => 8
        };

        return deduplicated
            .OrderBy(a => a.Priority)
            .Take(maxAgents)
            .ToList();
    }

    private double GetBaseAgentScore(AgentType agentType, CodeCharacteristics characteristics)
    {
        return agentType switch
        {
            AgentType.SecurityExpert when characteristics.HasDatabaseCalls => 0.9,
            AgentType.PerformanceAnalyst when characteristics.PerformanceCritical => 0.9,
            AgentType.ArchitectureExpert when characteristics.Complexity > 7 => 0.9,
            AgentType.TestingSpecialist when characteristics.TestabilityScore < 0.6 => 0.8,
            AgentType.DomainExpert when characteristics.BusinessLogicHeavy => 0.8,
            AgentType.CodeQualityReviewer => 0.8, // Always relevant
            AgentType.SecurityExpert => 0.7, // Always important
            _ => 0.6
        };
    }

    private double GetContextualScore(AgentType agentType, Dictionary<string, object> context)
    {
        var score = 0.5;

        if (context.TryGetValue("TeamExperience", out var exp) && exp.ToString() == "junior")
        {
            if (agentType == AgentType.DeveloperMentor) score += 0.4;
        }

        if (context.TryGetValue("BusinessCritical", out var critical) && critical is bool isCritical && isCritical)
        {
            if (agentType == AgentType.SecurityExpert || agentType == AgentType.ArchitectureExpert) 
                score += 0.3;
        }

        return Math.Min(1.0, score);
    }

    private double GetSpecializationMatch(AgentType agentType, string subtask, CodeCharacteristics characteristics)
    {
        var taskLower = subtask.ToLower();
        
        return agentType switch
        {
            AgentType.SecurityExpert when taskLower.Contains("security") || taskLower.Contains("vulnerability") => 1.0,
            AgentType.PerformanceAnalyst when taskLower.Contains("performance") || taskLower.Contains("optimization") => 1.0,
            AgentType.ArchitectureExpert when taskLower.Contains("architecture") || taskLower.Contains("design") => 1.0,
            AgentType.TestingSpecialist when taskLower.Contains("test") || taskLower.Contains("coverage") => 1.0,
            AgentType.DomainExpert when taskLower.Contains("business") || taskLower.Contains("domain") => 1.0,
            _ => 0.6
        };
    }

    private TimeSpan EstimateAnalysisTime(List<AgentSelection> agents, CodeCharacteristics characteristics)
    {
        var baseTime = characteristics.Complexity * 30; // 30 seconds per complexity point
        var agentOverhead = agents.Count * 15; // 15 seconds per agent
        
        return TimeSpan.FromSeconds(baseTime + agentOverhead);
    }

    private List<AgentType> DetermineExecutionOrder(List<AgentSelection> agents, CodeCharacteristics characteristics)
    {
        // Critical agents first, then by dependency
        return agents
            .OrderBy(a => a.Priority)
            .ThenBy(a => GetExecutionOrder(a.AgentType))
            .Select(a => a.AgentType)
            .ToList();
    }

    private int GetExecutionOrder(AgentType agentType)
    {
        return agentType switch
        {
            AgentType.SecurityExpert => 1,
            AgentType.ArchitectureExpert => 2,
            AgentType.ArchitectureStandardsAgent => 3,
            AgentType.PerformanceAnalyst => 4,
            AgentType.CodeQualityReviewer => 5,
            AgentType.TestingSpecialist => 6,
            AgentType.DomainExpert => 7,
            AgentType.DeveloperMentor => 8,
            AgentType.AICodeDetective => 9,
            _ => 10
        };
    }

    private double EstimateAgentCost(AgentType agentType, CodeCharacteristics characteristics)
    {
        var baseCost = agentType switch
        {
            AgentType.SecurityExpert => 1.2,
            AgentType.ArchitectureExpert => 1.1,
            AgentType.PerformanceAnalyst => 1.0,
            _ => 0.8
        };

        return baseCost * (1 + characteristics.Complexity * 0.1);
    }

    private TimeSpan EstimateAgentTime(AgentType agentType, CodeCharacteristics characteristics)
    {
        var baseTime = agentType switch
        {
            AgentType.SecurityExpert => 45,
            AgentType.ArchitectureExpert => 40,
            AgentType.PerformanceAnalyst => 35,
            _ => 30
        };

        return TimeSpan.FromSeconds(baseTime * (1 + characteristics.Complexity * 0.2));
    }

    private string BuildSelectionReasoning(List<AgentSelection> agents, CodeCharacteristics characteristics)
    {
        var reasoning = new System.Text.StringBuilder();
        reasoning.AppendLine($"Selected {agents.Count} agents based on code analysis:");
        reasoning.AppendLine($"- Code complexity: {characteristics.Complexity}/10");
        reasoning.AppendLine($"- Performance critical: {characteristics.PerformanceCritical}");
        reasoning.AppendLine($"- Has database calls: {characteristics.HasDatabaseCalls}");
        reasoning.AppendLine($"- Business logic heavy: {characteristics.BusinessLogicHeavy}");
        reasoning.AppendLine();
        
        foreach (var agent in agents.OrderBy(a => a.Priority))
        {
            reasoning.AppendLine($"- {agent.AgentType} ({agent.Priority}): {agent.Reasoning}");
        }

        return reasoning.ToString();
    }

    private AgentSelectionResult CreateDefaultAgentSelection()
    {
        return new AgentSelectionResult
        {
            SelectedAgents = new List<AgentSelection>
            {
                new AgentSelection(AgentType.CodeQualityReviewer, AgentPriority.High, "Default selection"),
                new AgentSelection(AgentType.SecurityExpert, AgentPriority.High, "Default selection")
            },
            CodeCharacteristics = new CodeCharacteristics(),
            SelectionReasoning = "Fallback to default agent selection due to analysis failure"
        };
    }
}

/// <summary>
/// Analyzes code characteristics to inform agent selection
/// </summary>
public class CodeCharacteristicsAnalyzer
{
    public async Task<CodeCharacteristics> AnalyzeCodeCharacteristics(
        string code, 
        string language, 
        Dictionary<string, object> context)
    {
        var characteristics = new CodeCharacteristics
        {
            Language = language,
            LinesOfCode = code.Split('\n').Length,
            Complexity = CalculateComplexity(code),
            HasDatabaseCalls = DetectDatabaseCalls(code),
            HasAsyncPatterns = DetectAsyncPatterns(code),
            HasConcurrency = DetectConcurrency(code),
            HasLoops = DetectLoops(code),
            HasRecursion = DetectRecursion(code),
            HasTests = DetectTests(code),
            HasDesignPatterns = DetectDesignPatterns(code),
            HasAbstractions = DetectAbstractions(code),
            BusinessLogicHeavy = DetectBusinessLogic(code),
            HasDomainConcepts = DetectDomainConcepts(code),
            PerformanceCritical = DetectPerformanceCritical(code, context),
            HasCommentedCode = DetectCommentedCode(code),
            SuspiciousPatterns = DetectSuspiciousPatterns(code),
            TestabilityScore = CalculateTestabilityScore(code),
            HasDataAccess = DetectDataAccess(code)
        };

        return characteristics;
    }

    // Detection methods
    private int CalculateComplexity(string code)
    {
        var complexity = 1; // Base complexity
        
        // Cyclomatic complexity indicators
        complexity += Regex.Matches(code, @"\b(if|else|while|for|foreach|switch|case|catch)\b").Count;
        complexity += Regex.Matches(code, @"&&|\|\|").Count;
        complexity += Regex.Matches(code, @"\?.*:").Count; // Ternary operators
        
        // Structural complexity
        var nestingLevel = CalculateMaxNestingLevel(code);
        complexity += nestingLevel * 2;
        
        return Math.Min(10, complexity);
    }

    private bool DetectDatabaseCalls(string code)
    {
        var patterns = new[]
        {
            @"\.Query\(", @"\.Execute\(", @"SELECT\s+", @"INSERT\s+", @"UPDATE\s+", @"DELETE\s+",
            @"DbContext", @"SqlConnection", @"SqlCommand", @"EntityFramework", @"\.Find\(", @"\.Where\("
        };
        
        return patterns.Any(pattern => Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase));
    }

    private bool DetectAsyncPatterns(string code)
    {
        return Regex.IsMatch(code, @"\b(async|await|Task<|Task\.)", RegexOptions.IgnoreCase);
    }

    private bool DetectConcurrency(string code)
    {
        var patterns = new[]
        {
            @"\b(Thread|ThreadPool|Parallel|ConcurrentDictionary|SemaphoreSlim|Mutex|lock\s*\()\b",
            @"\.ConfigureAwait\(", @"Task\.Run\(", @"Task\.Factory"
        };
        
        return patterns.Any(pattern => Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase));
    }

    private bool DetectLoops(string code)
    {
        return Regex.IsMatch(code, @"\b(for|foreach|while|do\s*{)\b", RegexOptions.IgnoreCase);
    }

    private bool DetectRecursion(string code)
    {
        // Look for method calls that might be recursive
        var methodMatches = Regex.Matches(code, @"(\w+)\s*\([^)]*\)\s*{", RegexOptions.IgnoreCase);
        foreach (Match match in methodMatches)
        {
            var methodName = match.Groups[1].Value;
            if (code.Contains($"{methodName}(") && code.IndexOf($"{methodName}(") != match.Index)
            {
                return true;
            }
        }
        return false;
    }

    private bool DetectTests(string code)
    {
        var patterns = new[]
        {
            @"\[Test\]", @"\[Fact\]", @"\[Theory\]", @"Assert\.", @"Should\.", @"\.Verify\(", @"Mock<"
        };
        
        return patterns.Any(pattern => Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase));
    }

    private bool DetectDesignPatterns(string code)
    {
        var patterns = new[]
        {
            @"\bFactory\b", @"\bBuilder\b", @"\bSingleton\b", @"\bObserver\b", @"\bStrategy\b",
            @"\bRepository\b", @"\bService\b", @"\.Create\(", @"\.Build\("
        };
        
        return patterns.Any(pattern => Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase));
    }

    private bool DetectAbstractions(string code)
    {
        return Regex.IsMatch(code, @"\b(abstract|interface|virtual|override)\b", RegexOptions.IgnoreCase);
    }

    private bool DetectBusinessLogic(string code)
    {
        var businessKeywords = new[]
        {
            @"\b(calculate|validate|process|business|rule|policy|workflow)\b",
            @"\b(price|cost|discount|tax|commission|fee)\b",
            @"\b(approve|reject|authorize|verify)\b"
        };
        
        return businessKeywords.Any(pattern => Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase));
    }

    private bool DetectDomainConcepts(string code)
    {
        // Look for domain-specific naming patterns
        var hasComplexTypes = Regex.Matches(code, @"class\s+\w+").Count > 1;
        var hasBusinessMethods = Regex.IsMatch(code, @"\b(Get|Create|Update|Delete|Process)\w+\b");
        
        return hasComplexTypes && hasBusinessMethods;
    }

    private bool DetectPerformanceCritical(string code, Dictionary<string, object> context)
    {
        var hasPerformanceIndicators = Regex.IsMatch(code, 
            @"\b(benchmark|performance|optimization|cache|throttle|rate\s*limit)\b", 
            RegexOptions.IgnoreCase);
            
        var isPerformanceSensitive = context.TryGetValue("PerformanceCritical", out var perf) && 
                                   perf is bool isPerfCritical && isPerfCritical;
        
        return hasPerformanceIndicators || isPerformanceSensitive;
    }

    private bool DetectCommentedCode(string code)
    {
        var commentedCodeLines = Regex.Matches(code, @"^\s*//\s*\w+.*[;{}]\s*$", RegexOptions.Multiline);
        return commentedCodeLines.Count > 2;
    }

    private List<string> DetectSuspiciousPatterns(string code)
    {
        var patterns = new List<string>();
        
        if (Regex.IsMatch(code, @"//\s*(TODO|FIXME|HACK|XXX)", RegexOptions.IgnoreCase))
            patterns.Add("TODO/FIXME comments");
            
        if (Regex.IsMatch(code, @"throw\s+new\s+NotImplementedException", RegexOptions.IgnoreCase))
            patterns.Add("NotImplementedException");
            
        if (Regex.IsMatch(code, @"//\s*AI\s*(generated|created)", RegexOptions.IgnoreCase))
            patterns.Add("AI-generated code comments");
        
        return patterns;
    }

    private double CalculateTestabilityScore(string code)
    {
        var score = 1.0;
        
        // Reduce score for static dependencies
        if (Regex.IsMatch(code, @"DateTime\.Now|File\.|Directory\.|Console\."))
            score -= 0.3;
            
        // Reduce score for tight coupling
        if (Regex.IsMatch(code, @"new\s+\w+\(.*\)"))
            score -= 0.2;
            
        // Increase score for dependency injection patterns
        if (Regex.IsMatch(code, @"I\w+\s+\w+", RegexOptions.IgnoreCase))
            score += 0.2;
        
        return Math.Max(0.0, Math.Min(1.0, score));
    }

    private bool DetectDataAccess(string code)
    {
        var patterns = new[]
        {
            @"\.json", @"\.xml", @"\.csv", @"\.Read\(", @"\.Write\(",
            @"HttpClient", @"WebRequest", @"RestClient", @"\.Send\("
        };
        
        return patterns.Any(pattern => Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase));
    }

    private int CalculateMaxNestingLevel(string code)
    {
        var maxLevel = 0;
        var currentLevel = 0;
        
        foreach (char c in code)
        {
            if (c == '{')
            {
                currentLevel++;
                maxLevel = Math.Max(maxLevel, currentLevel);
            }
            else if (c == '}')
            {
                currentLevel--;
            }
        }
        
        return maxLevel;
    }
}

// Supporting classes
public class AgentSelectionResult
{
    public List<AgentSelection> SelectedAgents { get; set; } = new();
    public CodeCharacteristics CodeCharacteristics { get; set; }
    public string SelectionReasoning { get; set; } = "";
    public TimeSpan EstimatedAnalysisTime { get; set; }
    public List<AgentType> RecommendedExecutionOrder { get; set; } = new();
}

public class AgentSelection
{
    public AgentType AgentType { get; set; }
    public AgentPriority Priority { get; set; }
    public string Reasoning { get; set; }

    public AgentSelection(AgentType agentType, AgentPriority priority, string reasoning)
    {
        AgentType = agentType;
        Priority = priority;
        Reasoning = reasoning;
    }
}

public enum AgentPriority
{
    Critical = 1,
    High = 2,
    Medium = 3,
    Low = 4
}

public class CodeCharacteristics
{
    public string Language { get; set; } = "";
    public int LinesOfCode { get; set; }
    public int Complexity { get; set; }
    public bool HasDatabaseCalls { get; set; }
    public bool HasAsyncPatterns { get; set; }
    public bool HasConcurrency { get; set; }
    public bool HasLoops { get; set; }
    public bool HasRecursion { get; set; }
    public bool HasTests { get; set; }
    public bool HasDesignPatterns { get; set; }
    public bool HasAbstractions { get; set; }
    public bool BusinessLogicHeavy { get; set; }
    public bool HasDomainConcepts { get; set; }
    public bool PerformanceCritical { get; set; }
    public bool HasCommentedCode { get; set; }
    public List<string> SuspiciousPatterns { get; set; } = new();
    public double TestabilityScore { get; set; }
    public bool HasDataAccess { get; set; }
}

public class AgentFitnessScore
{
    public AgentType AgentType { get; set; }
    public double OverallScore { get; set; }
    public double BaseCompetency { get; set; }
    public double ContextualRelevance { get; set; }
    public double SpecializationMatch { get; set; }
    public double EstimatedCost { get; set; }
    public TimeSpan EstimatedTime { get; set; }
    public string Reasoning { get; set; } = "";
}