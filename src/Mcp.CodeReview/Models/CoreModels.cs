namespace Mcp.CodeReview.Models;

/// <summary>
/// Enumeration of available AI agent types
/// </summary>
public enum AgentType
{
    SecurityExpert,
    PerformanceAnalyst,
    CodeQualityReviewer,
    ArchitectureExpert,
    ArchitectureStandardsAgent,
    TestingSpecialist,
    DomainExpert,
    FeatureSlicingExpert,
    DeveloperMentor,
    AICodeDetective
}

/// <summary>
/// Code review request model
/// </summary>
public class CodeReviewRequest
{
    public string Content { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public List<AgentType> RequestedAgents { get; set; } = new();
    public ReviewOptions Options { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Review options for customizing analysis
/// </summary>
public class ReviewOptions
{
    public bool IncludeSecurityAnalysis { get; set; } = true;
    public bool IncludePerformanceAnalysis { get; set; } = true;
    public bool IncludeQualityAnalysis { get; set; } = true;
    public bool IncludeTestSuggestions { get; set; } = true;
    public bool IncludeRefactoringSuggestions { get; set; } = true;
    public string ReviewDepth { get; set; } = "standard"; // quick, standard, comprehensive
    public string TeamContext { get; set; } = string.Empty;
    public string BusinessDomain { get; set; } = string.Empty;
}

/// <summary>
/// Multi-agent review result
/// </summary>
public class MultiAgentReviewResult
{
    public string OverallAssessment { get; set; } = string.Empty;
    public double QualityScore { get; set; }
    public List<AgentResult> AgentResults { get; set; } = new();
    public List<string> KeyFindings { get; set; } = new();
    public List<string> PriorityRecommendations { get; set; } = new();
    public ReviewMetrics Metrics { get; set; } = new();
    public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Individual agent analysis result
/// </summary>
public class AgentResult
{
    public AgentType AgentType { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string Analysis { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public List<Finding> Findings { get; set; } = new();
    public List<Recommendation> Recommendations { get; set; } = new();
    public List<string> ReasoningChain { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    // Execution status and timing properties
    public bool Success { get; set; } = true;
    public TimeSpan? ExecutionTime { get; set; }
    
    // Legacy properties for compatibility
    public bool IsSuccessful 
    { 
        get => Success; 
        set => Success = value; 
    }
    
    public TimeSpan ProcessingTime 
    { 
        get => ExecutionTime ?? TimeSpan.Zero; 
        set => ExecutionTime = value; 
    }
}

/// <summary>
/// Code analysis finding
/// </summary>
public class Finding
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int? LineNumber { get; set; }
    public string Evidence { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public double Confidence { get; set; } = 0.8;
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Code improvement recommendation
/// </summary>
public class Recommendation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Implementation { get; set; } = string.Empty;
    public List<string> Benefits { get; set; } = new();
    public string EstimatedEffort { get; set; } = string.Empty;
}

/// <summary>
/// Review performance metrics
/// </summary>
public class ReviewMetrics
{
    public TimeSpan TotalAnalysisTime { get; set; }
    public int LinesAnalyzed { get; set; }
    public int IssuesFound { get; set; }
    public int RecommendationsGenerated { get; set; }
    public Dictionary<string, TimeSpan> AgentExecutionTimes { get; set; } = new();
    public double AnalysisEfficiency { get; set; }
    public Dictionary<string, object> EnhancedMetrics { get; set; } = new();
    
    // Additional properties for GitLab integration
    public double QualityScore { get; set; }
    public double DocumentationScore { get; set; }
    public double PerformanceScore { get; set; }
    public double SecurityScore { get; set; }
}

/// <summary>
/// Focused analysis request for specific concerns
/// </summary>
public class FocusedAnalysisRequest
{
    public string Content { get; set; } = string.Empty;
    public AgentType PrimaryAgent { get; set; }
    public List<string> SpecificConcerns { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
    public string AnalysisDepth { get; set; } = "standard";
}

/// <summary>
/// Focused analysis result
/// </summary>
public class FocusedAnalysisResult
{
    public AgentResult PrimaryResult { get; set; } = new();
    public List<string> SpecificFindings { get; set; } = new();
    public List<string> ActionableItems { get; set; } = new();
    public double FocusScore { get; set; }
    public ReviewMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Validation result for requests
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> ValidatedData { get; set; } = new();
    public double ConsistencyScore { get; set; }
    public double OverallConfidence { get; set; }
    public Dictionary<string, object> QualityMetrics { get; set; } = new();
    public DateTime ValidationTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Generic service result wrapper
/// </summary>
public class ServiceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    
    public static ServiceResult<T> SuccessResult(T data) => new() { Success = true, Data = data };
    public static ServiceResult<T> FailureResult(string error) => new() { Success = false, ErrorMessage = error };
}