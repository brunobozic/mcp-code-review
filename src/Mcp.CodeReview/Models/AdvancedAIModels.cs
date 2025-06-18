using System;
using System.Collections.Generic;

namespace Mcp.CodeReview.Models
{
    #region Hierarchical Orchestration Models

    /// <summary>
    /// Result from hierarchical multi-agent review
    /// </summary>
    public class HierarchicalReviewResult
    {
        public string CorrelationId { get; set; } = string.Empty;
        public StrategicAnalysis? StrategicAnalysis { get; set; }
        public List<SpecializedAgentResult> SpecializedResults { get; set; } = new();
        public SynthesisResult? SynthesisResult { get; set; }
        public ValidationResult? ValidationResult { get; set; }
        public EnhancedContext? Context { get; set; }
        public ExecutionMetrics? ExecutionMetrics { get; set; }
        public PerformanceGains? PerformanceGains { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Strategic analysis from lead orchestrator
    /// </summary>
    public class StrategicAnalysis
    {
        public string OverallStrategy { get; set; } = string.Empty;
        public List<string> KeyFocusAreas { get; set; } = new();
        public Dictionary<AgentType, string> AgentInstructions { get; set; } = new();
        public double ComplexityAssessment { get; set; }
        public List<string> PotentialRisks { get; set; } = new();
        public TimeSpan EstimatedAnalysisTime { get; set; }
    }

    /// <summary>
    /// Result from specialized agent execution
    /// </summary>
    public class SpecializedAgentResult
    {
        public AgentType AgentType { get; set; }
        public string Analysis { get; set; } = string.Empty;
        public List<Finding> Findings { get; set; } = new();
        public List<Recommendation> Recommendations { get; set; } = new();
        public double ConfidenceScore { get; set; }
        public List<ReasoningStep>? ReasoningChain { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Synthesis of all agent results
    /// </summary>
    public class SynthesisResult
    {
        public string UnifiedAnalysis { get; set; } = string.Empty;
        public List<string> KeyFindings { get; set; } = new();
        public List<string> PriorityRecommendations { get; set; } = new();
        public double OverallQualityScore { get; set; }
        public Dictionary<string, double> AgentAgreement { get; set; } = new();
        public List<string> ConflictingRecommendations { get; set; } = new();
    }

    /// <summary>
    /// Enhanced context with advanced capabilities
    /// </summary>
    public class EnhancedContext
    {
        public string ProjectId { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string? ProjectType { get; set; }
        public int CodeComplexity { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public List<string>? AvailableTools { get; set; }
        public ProjectHistory? History { get; set; }
        public TeamContext? TeamContext { get; set; }
        public Dictionary<string, object> MemoryContext { get; set; } = new();
    }

    /// <summary>
    /// Performance gains from parallel execution
    /// </summary>
    public class PerformanceGains
    {
        public TimeSpan SequentialTime { get; set; }
        public TimeSpan ParallelTime { get; set; }
        public double EfficiencyGain { get; set; }
        public TimeSpan TimeReduction { get; set; }
        public int AgentCount { get; set; }
        public int SuccessfulAgents { get; set; }
    }

    /// <summary>
    /// Execution metrics for monitoring
    /// </summary>
    public class ExecutionMetrics
    {
        public TimeSpan TotalExecutionTime { get; set; }
        public Dictionary<string, TimeSpan> AgentExecutionTimes { get; set; } = new();
        public Dictionary<string, double> ConfidenceScores { get; set; } = new();
        public long MemoryUsage { get; set; }
        public int ThreadCount { get; set; }
    }

    #endregion

    #region Advanced Reasoning Models

    /// <summary>
    /// Context for reasoning operations
    /// </summary>
    public class ReasoningContext
    {
        public string? CodeLanguage { get; set; }
        public string? ProjectType { get; set; }
        public int? ComplexityLevel { get; set; }
        public List<string>? AvailableTools { get; set; }
        public Dictionary<string, object> Memory { get; set; } = new();
        public bool EnableExtendedThinking { get; set; }
        public double ConfidenceThreshold { get; set; } = 0.8;
    }

    /// <summary>
    /// Chain of Thought reasoning result
    /// </summary>
    public class ChainOfThoughtResult
    {
        public string SessionId { get; set; } = string.Empty;
        public string Problem { get; set; } = string.Empty;
        public List<ReasoningStep> ReasoningSteps { get; set; } = new();
        public string Conclusion { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public int TokensUsed { get; set; }
    }

    /// <summary>
    /// Tree of Thoughts reasoning result
    /// </summary>
    public class TreeOfThoughtsResult
    {
        public string SessionId { get; set; } = string.Empty;
        public string Problem { get; set; } = string.Empty;
        public List<ThoughtNode> ExploredNodes { get; set; } = new();
        public List<ThoughtNode> BestPath { get; set; } = new();
        public string Synthesis { get; set; } = string.Empty;
        public TreeSearchStrategy Strategy { get; set; }
        public double ConfidenceScore { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public int NodesExplored { get; set; }
    }

    /// <summary>
    /// Hybrid reasoning result
    /// </summary>
    public class HybridReasoningResult
    {
        public string SessionId { get; set; } = string.Empty;
        public string Problem { get; set; } = string.Empty;
        public List<ReasoningResult> ReasoningResults { get; set; } = new();
        public ReasoningSynthesis Synthesis { get; set; } = new();
        public HybridReasoningOptions Options { get; set; } = new();
        public double ConfidenceScore { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public List<ReasoningMode> ModesUsed { get; set; } = new();
    }

    /// <summary>
    /// Individual reasoning step
    /// </summary>
    public class ReasoningStep
    {
        public int StepNumber { get; set; }
        public string Content { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<string> Dependencies { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Node in thought tree
    /// </summary>
    public class ThoughtNode
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ParentId { get; set; }
        public int Depth { get; set; }
        public double Score { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result from individual reasoning mode
    /// </summary>
    public class ReasoningResult
    {
        public ReasoningMode Mode { get; set; }
        public string Content { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Synthesis of multiple reasoning results
    /// </summary>
    public class ReasoningSynthesis
    {
        public string Content { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public List<ReasoningMode> InputModes { get; set; } = new();
        public double SynthesisQuality { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Options for hybrid reasoning
    /// </summary>
    public class HybridReasoningOptions
    {
        public bool EnableInstantMode { get; set; } = true;
        public bool EnableExtendedThinking { get; set; } = false;
        public bool EnableToolIntegration { get; set; } = true;
        public bool EnableMemoryIntegration { get; set; } = true;
        public double ExtendedThinkingThreshold { get; set; } = 0.85;
        public int MaxReasoningDepth { get; set; } = 5;
        public TimeSpan MaxReasoningTime { get; set; } = TimeSpan.FromMinutes(5);
    }

    #endregion

    #region Advanced Analysis Models

    /// <summary>
    /// Project history context
    /// </summary>
    public class ProjectHistory
    {
        public List<string> PreviousIssues { get; set; } = new();
        public List<string> SuccessfulPatterns { get; set; } = new();
        public Dictionary<string, int> TechnologyStack { get; set; } = new();
        public List<string> TeamPreferences { get; set; } = new();
        public DateTime LastAnalysis { get; set; }
    }

    /// <summary>
    /// Team context information
    /// </summary>
    public class TeamContext
    {
        public string ExperienceLevel { get; set; } = string.Empty;
        public List<string> PreferredPatterns { get; set; } = new();
        public List<string> AvoidedPatterns { get; set; } = new();
        public string CodingStandards { get; set; } = string.Empty;
        public List<string> TechnicalDebt { get; set; } = new();
    }

    /// <summary>
    /// Quality metrics for analysis validation
    /// </summary>
    public class QualityMetrics
    {
        public double Accuracy { get; set; }
        public double Completeness { get; set; }
        public double Relevance { get; set; }
        public double Novelty { get; set; }
        public double ActionabilityScore { get; set; }
        public Dictionary<string, double> DetailedMetrics { get; set; } = new();
    }

    /// <summary>
    /// Tree exploration result
    /// </summary>
    public class TreeExplorationResult
    {
        public List<ThoughtNode> Nodes { get; set; } = new();
        public TreeSearchStrategy Strategy { get; set; }
        public int ExplorationDepth { get; set; }
        public int TotalNodes { get; set; }
        public TimeSpan ExplorationTime { get; set; }
    }

    /// <summary>
    /// Reasoning session tracking
    /// </summary>
    public class ReasoningSession
    {
        public string SessionId { get; }
        public ReasoningMode Mode { get; }
        public DateTime StartTime { get; }
        public DateTime? EndTime { get; private set; }
        public bool IsCompleted { get; private set; }
        public Exception? Error { get; private set; }
        
        public TimeSpan ElapsedTime => (EndTime ?? DateTime.UtcNow) - StartTime;

        public ReasoningSession(string sessionId, ReasoningMode mode)
        {
            SessionId = sessionId;
            Mode = mode;
            StartTime = DateTime.UtcNow;
        }

        public void Complete(object result)
        {
            EndTime = DateTime.UtcNow;
            IsCompleted = true;
        }

        public void Fail(Exception error)
        {
            EndTime = DateTime.UtcNow;
            Error = error;
        }
    }

    #endregion

    #region Enums

    /// <summary>
    /// Reasoning modes for different analysis types
    /// </summary>
    public enum ReasoningMode
    {
        DirectAnalysis,
        ChainOfThought,
        TreeOfThoughts,
        ExtendedThinking,
        ToolIntegrated,
        MemoryEnhanced,
        Hybrid
    }

    /// <summary>
    /// Tree search strategies
    /// </summary>
    public enum TreeSearchStrategy
    {
        BreadthFirst,
        DepthFirst,
        BestFirst,
        BeamSearch
    }

    #endregion

    #region Interfaces

    /// <summary>
    /// Interface for specialized reasoning agents
    /// </summary>
    public interface ISpecializedReasoningAgent
    {
        Task<AgentResult> AnalyzeWithReasoningAsync(
            CodeReviewRequest request,
            EnhancedContext context,
            ReasoningMode mode,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for lead orchestrator agent
    /// </summary>
    public interface ILeadOrchestratorAgent
    {
        Task<StrategicAnalysis> ConductStrategicAnalysisAsync(
            CodeReviewRequest request,
            EnhancedContext context,
            CancellationToken cancellationToken = default);

        Task<SynthesisResult> SynthesizeResultsAsync(
            SpecializedAgentResult[] results,
            StrategicAnalysis strategicAnalysis,
            EnhancedContext context,
            CancellationToken cancellationToken = default);
    }

    #endregion
}