using System.Collections.Generic;

namespace Mcp.CodeReview.Models;

/// <summary>
/// Comprehensive repository context for contextual code analysis
/// </summary>
public class RepositoryContext
{
    public string ProjectId { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string DefaultBranch { get; set; } = "main";
    
    /// <summary>
    /// Complete project file structure
    /// </summary>
    public ProjectStructure Structure { get; set; } = new();
    
    /// <summary>
    /// Dependencies and their versions
    /// </summary>
    public Dictionary<string, string> Dependencies { get; set; } = new();
    
    /// <summary>
    /// Related files that might be affected by changes
    /// </summary>
    public Dictionary<string, string> RelatedFiles { get; set; } = new();
    
    /// <summary>
    /// Historical patterns from RAG system
    /// </summary>
    public List<HistoricalPattern> HistoricalPatterns { get; set; } = new();
    
    /// <summary>
    /// Coding standards specific to this project/team
    /// </summary>
    public List<CodingStandard> ProjectStandards { get; set; } = new();
    
    /// <summary>
    /// Team preferences and patterns
    /// </summary>
    public TeamPatterns TeamPatterns { get; set; } = new();
}

public class ProjectStructure
{
    public List<string> SourceDirectories { get; set; } = new();
    public List<string> TestDirectories { get; set; } = new();
    public List<string> ConfigurationFiles { get; set; } = new();
    public List<string> DocumentationFiles { get; set; } = new();
    public string ArchitecturePattern { get; set; } = string.Empty;
    public Dictionary<string, List<string>> FilesByType { get; set; } = new();
}

public class HistoricalPattern
{
    public string Id { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Similarity { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

public class CodingStandard
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Priority { get; set; }
}

public class TeamPatterns
{
    public List<string> PreferredPatterns { get; set; } = new();
    public List<string> AvoidedPatterns { get; set; } = new();
    public Dictionary<string, string> NamingConventions { get; set; } = new();
    public List<string> ApprovedLibraries { get; set; } = new();
}