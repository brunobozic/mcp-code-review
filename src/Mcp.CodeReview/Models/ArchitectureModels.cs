using System;
using System.Collections.Generic;

namespace Mcp.CodeReview.Models
{
    /// <summary>
    /// Extended code review request for architecture analysis
    /// </summary>
    public class ArchitectureCodeReviewRequest
    {
        public string ProjectId { get; set; } = string.Empty;
        public string MergeRequestIid { get; set; } = string.Empty;
        public List<FileChange> Changes { get; set; } = new();
        public Dictionary<string, object> Context { get; set; } = new();
    }

    /// <summary>
    /// Represents a file change in a merge request
    /// </summary>
    public class FileChange
    {
        public string FilePath { get; set; } = string.Empty;
        public string? OldContent { get; set; }
        public string? NewContent { get; set; }
        public string ChangeType { get; set; } = string.Empty; // added, modified, deleted
        public int LinesAdded { get; set; }
        public int LinesRemoved { get; set; }
    }

    /// <summary>
    /// Result of architecture analysis
    /// </summary>
    public class ArchitectureAnalysisResult
    {
        public string ProjectId { get; set; } = string.Empty;
        public string MergeRequestIid { get; set; } = string.Empty;
        public DateTimeOffset AnalysisTimestamp { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        
        public ProjectMaturityAssessment ProjectMaturity { get; set; } = new();
        public List<ArchitectureRecommendation> Recommendations { get; set; } = new();
        public List<ArchitectureRequirement> Requirements { get; set; } = new();
        public GradualMigrationPlan? GradualMigrationPlan { get; set; }
        public List<StandardsViolation> StandardsViolations { get; set; } = new();
    }

    /// <summary>
    /// Assessment of project maturity
    /// </summary>
    public class ProjectMaturityAssessment
    {
        public bool HasServicePattern { get; set; }
        public bool HasRepositoryPattern { get; set; }
        public bool HasUnitOfWork { get; set; }
        public bool HasRequestResponsePattern { get; set; }
        public bool HasMinimalApis { get; set; }
        public bool HasStructuredLogging { get; set; }
        public bool HasOpenApiAnnotations { get; set; }
        public bool HasRegionOrganization { get; set; }
        public bool HasXmlDocumentation { get; set; }
        public bool HasHateoas { get; set; }
        public bool HasFeatureSlices { get; set; }
        
        public bool IsLegacyProject { get; set; }
        public bool IsGreenfield { get; set; }
        public bool HasModernPatterns { get; set; }
        public double ArchitectureMaturityScore { get; set; }
    }

    /// <summary>
    /// Standards violation
    /// </summary>
    public class StandardsViolation
    {
        public ViolationType Type { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string File { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
        public int? Line { get; set; }
    }

    /// <summary>
    /// Architecture recommendation
    /// </summary>
    public class ArchitectureRecommendation
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
        public RecommendationType Type { get; set; }
        public string Priority { get; set; } = string.Empty;
    }

    /// <summary>
    /// Architecture requirement
    /// </summary>
    public class ArchitectureRequirement
    {
        public string Pattern { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsBlocking { get; set; }
        public string Justification { get; set; } = string.Empty;
    }

    /// <summary>
    /// Gradual migration plan
    /// </summary>
    public class GradualMigrationPlan
    {
        public List<MigrationStep> ImmediateSteps { get; set; } = new();
        public List<MigrationStep> ShortTermSteps { get; set; } = new();
        public List<MigrationStep> LongTermSteps { get; set; } = new();
    }

    /// <summary>
    /// Migration step
    /// </summary>
    public class MigrationStep
    {
        public string Description { get; set; } = string.Empty;
        public string Effort { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public List<string> Prerequisites { get; set; } = new();
    }

    /// <summary>
    /// Code review result for GitLab integration
    /// </summary>
    public class CodeReviewResult
    {
        public string OverallComment { get; set; } = string.Empty;
        public List<FileComment> FileComments { get; set; } = new();
        public double OverallScore { get; set; }
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        
        // Additional properties expected by GitLabIntegrationService
        public ReviewMetrics? Metrics { get; set; }
        public List<ReviewIssue> Issues { get; set; } = new();
        public List<ReviewRecommendation> Recommendations { get; set; } = new();
        public List<AgentInsight> AgentInsights { get; set; } = new();
        public string ReviewId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Review issue
    /// </summary>
    public class ReviewIssue
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string File { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public int? Line { get; set; } // Alias for LineNumber - nullable for optional line info
        public string Suggestion { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty; // Alias for Suggestion
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// Review recommendation
    /// </summary>
    public class ReviewRecommendation
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Implementation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Agent insight
    /// </summary>
    public class AgentInsight
    {
        public string AgentName { get; set; } = string.Empty;
        public string Insight { get; set; } = string.Empty;
        public string Analysis { get; set; } = string.Empty; // Alias for Insight
        public double Confidence { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
    /// File-specific comment
    /// </summary>
    public class FileComment
    {
        public string FilePath { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
    }

    /// <summary>
    /// Violation types
    /// </summary>
    public enum ViolationType
    {
        Architecture,
        Documentation,
        Logging,
        Organization,
        ErrorHandling,
        ApiDesign
    }

    /// <summary>
    /// Recommendation types
    /// </summary>
    public enum RecommendationType
    {
        Gentle,
        Progressive,
        Strict
    }
}