using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.AI.Enhanced2025.TreeOfThoughts;

/// <summary>
/// Result of Tree of Thoughts reasoning process
/// </summary>
public record TreeOfThoughtsResult
{
    /// <summary>
    /// Initial thought branches generated
    /// </summary>
    public List<ThoughtBranch> InitialThoughts { get; init; } = new();
    
    /// <summary>
    /// All explored branches (including sub-branches)
    /// </summary>
    public List<ThoughtBranch> ExploredBranches { get; init; } = new();
    
    /// <summary>
    /// Evaluated branches with quality scores
    /// </summary>
    public List<EvaluatedThoughtBranch> EvaluatedBranches { get; init; } = new();
    
    /// <summary>
    /// Final synthesized reasoning from optimal paths
    /// </summary>
    public SynthesizedReasoning FinalSynthesis { get; init; } = new();
    
    /// <summary>
    /// Total execution time for ToT process
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }
    
    /// <summary>
    /// Total number of branches explored
    /// </summary>
    public int TotalBranchesExplored { get; init; }
    
    /// <summary>
    /// Number of optimal paths identified
    /// </summary>
    public int OptimalPathsFound { get; init; }
    
    /// <summary>
    /// Reasoning diversity score (0-1)
    /// </summary>
    public double ReasoningDiversityScore => CalculateReasoningDiversity();
    
    /// <summary>
    /// Overall confidence in the synthesized result
    /// </summary>
    public double OverallConfidence => FinalSynthesis.OverallConfidence;

    private double CalculateReasoningDiversity()
    {
        if (EvaluatedBranches.Count < 2) return 0.0;
        
        // Calculate diversity based on how different the optimal paths are
        var optimalBranches = EvaluatedBranches.Where(b => b.IsOptimal).ToList();
        if (optimalBranches.Count < 2) return 0.5;
        
        // Simple diversity metric based on perspective differences
        var perspectives = optimalBranches.Select(b => b.ThoughtBranch.Perspective).Distinct().Count();
        return Math.Min(1.0, perspectives / (double)optimalBranches.Count);
    }
}

/// <summary>
/// Individual thought branch in the reasoning tree
/// </summary>
public record ThoughtBranch
{
    /// <summary>
    /// Unique identifier for this branch
    /// </summary>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Index within the current depth level
    /// </summary>
    public int BranchIndex { get; init; }
    
    /// <summary>
    /// Reasoning perspective or approach taken
    /// </summary>
    public string Perspective { get; init; } = string.Empty;
    
    /// <summary>
    /// Detailed analysis from this perspective
    /// </summary>
    public string Analysis { get; init; } = string.Empty;
    
    /// <summary>
    /// Questions being explored in this branch
    /// </summary>
    public List<string> InitialQuestions { get; init; } = new();
    
    /// <summary>
    /// Methodology used for analysis
    /// </summary>
    public string Methodology { get; init; } = string.Empty;
    
    /// <summary>
    /// Findings discovered in this branch
    /// </summary>
    public List<Finding> Findings { get; init; } = new();
    
    /// <summary>
    /// Depth in the reasoning tree (0 = root)
    /// </summary>
    public int Depth { get; init; }
    
    /// <summary>
    /// Parent branch ID (null for root branches)
    /// </summary>
    public Guid? ParentBranchId { get; init; }
    
    /// <summary>
    /// Child branch IDs
    /// </summary>
    public List<Guid> ChildBranchIds { get; init; } = new();
    
    /// <summary>
    /// Initial confidence in this reasoning path
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// When this branch was created
    /// </summary>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// Assumptions made in this branch
    /// </summary>
    public List<string> Assumptions { get; init; } = new();
    
    /// <summary>
    /// Evidence supporting this branch's conclusions
    /// </summary>
    public List<string> SupportingEvidence { get; init; } = new();
    
    /// <summary>
    /// Potential weaknesses or limitations
    /// </summary>
    public List<string> Limitations { get; init; } = new();
}

/// <summary>
/// Thought branch with evaluation metrics
/// </summary>
public record EvaluatedThoughtBranch
{
    /// <summary>
    /// The original thought branch
    /// </summary>
    public ThoughtBranch ThoughtBranch { get; init; } = new();
    
    /// <summary>
    /// Quality of evidence supporting conclusions (0-1)
    /// </summary>
    public double EvidenceQuality { get; init; }
    
    /// <summary>
    /// Internal logical consistency (0-1)
    /// </summary>
    public double LogicalConsistency { get; init; }
    
    /// <summary>
    /// Completeness of analysis (0-1)
    /// </summary>
    public double Completeness { get; init; }
    
    /// <summary>
    /// Practical value of insights (0-1)
    /// </summary>
    public double PracticalValue { get; init; }
    
    /// <summary>
    /// Overall quality score (0-1)
    /// </summary>
    public double OverallScore { get; init; }
    
    /// <summary>
    /// Confidence in the branch's conclusions (0-1)
    /// </summary>
    public double Confidence { get; init; }
    
    /// <summary>
    /// Justification for the evaluation scores
    /// </summary>
    public string Justification { get; init; } = string.Empty;
    
    /// <summary>
    /// Identified weaknesses in reasoning
    /// </summary>
    public List<string> IdentifiedWeaknesses { get; init; } = new();
    
    /// <summary>
    /// Whether this branch is considered optimal
    /// </summary>
    public bool IsOptimal { get; init; }
    
    /// <summary>
    /// Ranking among all evaluated branches
    /// </summary>
    public int Ranking { get; init; }
    
    /// <summary>
    /// Suggestions for improving this reasoning path
    /// </summary>
    public List<string> ImprovementSuggestions { get; init; } = new();
}

/// <summary>
/// Synthesized reasoning from multiple optimal paths
/// </summary>
public record SynthesizedReasoning
{
    /// <summary>
    /// Unified analysis combining insights from all optimal paths
    /// </summary>
    public string UnifiedAnalysis { get; init; } = string.Empty;
    
    /// <summary>
    /// IDs of source paths used in synthesis
    /// </summary>
    public List<Guid> SourcePaths { get; init; } = new();
    
    /// <summary>
    /// Final findings with confidence weighting
    /// </summary>
    public List<Finding> ConfidenceWeightedFindings { get; init; } = new();
    
    /// <summary>
    /// Areas where multiple paths reached consensus
    /// </summary>
    public List<string> ConsensusAreas { get; init; } = new();
    
    /// <summary>
    /// Areas where paths diverged (indicating uncertainty)
    /// </summary>
    public List<string> UncertaintyAreas { get; init; } = new();
    
    /// <summary>
    /// Contradictions resolved during synthesis
    /// </summary>
    public List<ResolvedContradiction> ResolvedContradictions { get; init; } = new();
    
    /// <summary>
    /// Overall confidence in synthesized result
    /// </summary>
    public double OverallConfidence { get; init; }
    
    /// <summary>
    /// Confidence breakdown by finding category
    /// </summary>
    public Dictionary<string, double> CategoryConfidences { get; init; } = new();
    
    /// <summary>
    /// Meta-reasoning about the synthesis process
    /// </summary>
    public string MetaReasoning { get; init; } = string.Empty;
    
    /// <summary>
    /// When synthesis was completed
    /// </summary>
    public DateTime SynthesisTimestamp { get; init; }
    
    /// <summary>
    /// Quality metrics for the synthesis
    /// </summary>
    public SynthesisQualityMetrics QualityMetrics { get; init; } = new();
}

/// <summary>
/// Contradiction resolved during synthesis
/// </summary>
public record ResolvedContradiction
{
    /// <summary>
    /// Description of the contradiction
    /// </summary>
    public string Description { get; init; } = string.Empty;
    
    /// <summary>
    /// Conflicting positions
    /// </summary>
    public List<string> ConflictingPositions { get; init; } = new();
    
    /// <summary>
    /// How the contradiction was resolved
    /// </summary>
    public string Resolution { get; init; } = string.Empty;
    
    /// <summary>
    /// Confidence in the resolution (0-1)
    /// </summary>
    public double ResolutionConfidence { get; init; }
    
    /// <summary>
    /// Evidence supporting the resolution
    /// </summary>
    public List<string> SupportingEvidence { get; init; } = new();
}

/// <summary>
/// Quality metrics for synthesis process
/// </summary>
public record SynthesisQualityMetrics
{
    /// <summary>
    /// How well different paths were integrated (0-1)
    /// </summary>
    public double IntegrationQuality { get; init; }
    
    /// <summary>
    /// Consistency of the final synthesis (0-1)
    /// </summary>
    public double ConsistencyScore { get; init; }
    
    /// <summary>
    /// Completeness of the synthesis (0-1)
    /// </summary>
    public double CompletenessScore { get; init; }
    
    /// <summary>
    /// Novel insights generated beyond individual paths (0-1)
    /// </summary>
    public double NoveltyScore { get; init; }
    
    /// <summary>
    /// Overall synthesis quality (0-1)
    /// </summary>
    public double OverallQuality => (IntegrationQuality + ConsistencyScore + CompletenessScore + NoveltyScore) / 4.0;
    
    /// <summary>
    /// Areas where synthesis could be improved
    /// </summary>
    public List<string> ImprovementAreas { get; init; } = new();
    
    /// <summary>
    /// Strengths of the synthesis
    /// </summary>
    public List<string> Strengths { get; init; } = new();
}

/// <summary>
/// Tree of Thoughts configuration
/// </summary>
public record TreeOfThoughtsConfig
{
    /// <summary>
    /// Maximum number of initial branches to generate
    /// </summary>
    public int MaxInitialBranches { get; init; } = 5;
    
    /// <summary>
    /// Maximum depth to explore each branch
    /// </summary>
    public int MaxDepth { get; init; } = 3;
    
    /// <summary>
    /// Number of sub-branches to explore per parent
    /// </summary>
    public int BranchingFactor { get; init; } = 3;
    
    /// <summary>
    /// Minimum confidence threshold for branch exploration
    /// </summary>
    public double MinConfidenceThreshold { get; init; } = 0.3;
    
    /// <summary>
    /// Maximum execution time for ToT process
    /// </summary>
    public TimeSpan MaxExecutionTime { get; init; } = TimeSpan.FromMinutes(10);
    
    /// <summary>
    /// Whether to prune low-quality branches early
    /// </summary>
    public bool EnableEarlyPruning { get; init; } = true;
    
    /// <summary>
    /// Threshold for early pruning (branches below this score are pruned)
    /// </summary>
    public double EarlyPruningThreshold { get; init; } = 0.4;
    
    /// <summary>
    /// Whether to enable parallel branch exploration
    /// </summary>
    public bool EnableParallelExploration { get; init; } = true;
}