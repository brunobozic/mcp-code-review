using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;
using Mcp.CodeReview.Utilities;

namespace Mcp.CodeReview.AI;

/// <summary>
/// Enhanced conversation manager for multi-turn AI interactions
/// Provides sophisticated conversation flow management and context preservation
/// </summary>
public class EnhancedConversationManager
{
    private readonly IClaudeService _claudeService;
    private readonly ILogger<EnhancedConversationManager> _logger;
    private readonly List<(string role, string content)> _conversationHistory;

    public EnhancedConversationManager(IClaudeService claudeService, ILogger<EnhancedConversationManager> logger)
    {
        _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _conversationHistory = new List<(string, string)>();
    }

    /// <summary>
    /// Start a new conversation session with an agent
    /// </summary>
    public async Task<AgentResult> StartConversationAsync(
        AgentType agentType,
        string initialInput,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        _conversationHistory.Clear();
        return await ContinueConversationAsync(agentType, initialInput, context, cancellationToken);
    }

    /// <summary>
    /// Continue an existing conversation with context preservation
    /// </summary>
    public async Task<AgentResult> ContinueConversationAsync(
        AgentType agentType,
        string userInput,
        Dictionary<string, object> context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Build conversation continuation prompt using enhanced prompting
            var conversationPrompt = EnhancedPromptBuilder.BuildConversationContinuationPrompt(
                agentType,
                _conversationHistory,
                userInput,
                context
            );

            // Add user input to history
            _conversationHistory.Add(("user", userInput));

            _logger.LogDebug("Continuing conversation with {AgentType}, history length: {HistoryLength}", 
                agentType, _conversationHistory.Count);

            // Get enhanced agent response
            var response = await _claudeService.GenerateReviewAsync(conversationPrompt, cancellationToken);

            // Add agent response to history
            _conversationHistory.Add((agentType.ToString(), response));

            // Maintain conversation history size
            MaintainConversationHistory();

            // Parse and return structured result
            return ParseConversationResponse(agentType, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to continue conversation with {AgentType}", agentType);
            throw;
        }
    }

    /// <summary>
    /// Generate collaborative insights between multiple agents
    /// </summary>
    public async Task<string> GenerateCollaborativeInsightsAsync(
        List<AgentResult> agentResults,
        CodeReviewRequest originalRequest,
        CancellationToken cancellationToken = default)
    {
        if (agentResults.Count < 2)
        {
            return string.Empty;
        }

        try
        {
            // Use enhanced collaborative prompting
            var collaborativePrompt = EnhancedPromptBuilder.BuildCollaborativePrompt(
                AgentType.ArchitectureExpert,
                AgentType.SecurityExpert,
                agentResults,
                "Synthesize insights from multiple expert analyses to identify consensus, conflicts, and unified recommendations",
                new Dictionary<string, object>
                {
                    ["BusinessDomain"] = originalRequest.Options.BusinessDomain,
                    ["TeamContext"] = originalRequest.Options.TeamContext,
                    ["Language"] = originalRequest.Language
                }
            );

            _logger.LogDebug("Generating collaborative insights from {AgentCount} agent results", agentResults.Count);

            return await _claudeService.GenerateReviewAsync(collaborativePrompt, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate collaborative insights");
            return string.Empty;
        }
    }

    /// <summary>
    /// Generate adaptive prompts based on code complexity and team experience
    /// </summary>
    public async Task<AgentResult> GenerateAdaptiveAnalysisAsync(
        AgentType agentType,
        string content,
        Dictionary<string, object> context,
        CodeComplexity complexity,
        TeamExperience teamLevel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use adaptive prompting for personalized analysis
            var adaptivePrompt = EnhancedPromptBuilder.BuildAdaptivePrompt(
                agentType,
                content,
                context,
                complexity,
                teamLevel
            );

            _logger.LogDebug("Generating adaptive analysis for {AgentType} with complexity {Complexity} and team level {TeamLevel}", 
                agentType, complexity, teamLevel);

            var response = await _claudeService.GenerateReviewAsync(adaptivePrompt, cancellationToken);

            return ParseConversationResponse(agentType, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate adaptive analysis for {AgentType}", agentType);
            throw;
        }
    }

    /// <summary>
    /// Clear conversation history to start fresh
    /// </summary>
    public void ClearConversationHistory()
    {
        _conversationHistory.Clear();
        _logger.LogDebug("Conversation history cleared");
    }

    /// <summary>
    /// Get current conversation context for debugging
    /// </summary>
    public IReadOnlyList<(string role, string content)> GetConversationHistory()
    {
        return _conversationHistory.AsReadOnly();
    }

    // Private helper methods
    private void MaintainConversationHistory()
    {
        // Keep conversation history manageable (last 20 exchanges)
        while (_conversationHistory.Count > 20)
        {
            _conversationHistory.RemoveAt(0);
        }
    }

    private static AgentResult ParseConversationResponse(AgentType agentType, string response)
    {
        // Basic parsing for conversation responses
        // Could be enhanced with more sophisticated parsing
        return new AgentResult
        {
            AgentType = agentType,
            AgentName = agentType.ToString(),
            Analysis = response,
            ConfidenceScore = 0.8, // Conversation responses have good confidence
            Findings = ExtractFindingsFromText(response),
            Recommendations = ExtractRecommendationsFromText(response)
        };
    }

    private static List<Finding> ExtractFindingsFromText(string text)
    {
        var findings = new List<Finding>();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.Contains("issue", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("problem", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("concern", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new Finding
                {
                    Type = "ConversationFinding",
                    Description = line.Trim(),
                    Severity = DetermineSeverity(line)
                });
            }
        }

        return findings;
    }

    private static List<Recommendation> ExtractRecommendationsFromText(string text)
    {
        var recommendations = new List<Recommendation>();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (line.Contains("recommend", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("suggest", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("consider", StringComparison.OrdinalIgnoreCase))
            {
                recommendations.Add(new Recommendation
                {
                    Title = ExtractTitle(line),
                    Description = line.Trim(),
                    Priority = DeterminePriority(line),
                    Category = "ConversationRecommendation"
                });
            }
        }

        return recommendations;
    }

    private static string DetermineSeverity(string text) => text.ToLower() switch
    {
        var s when s.Contains("critical") || s.Contains("severe") => "CRITICAL",
        var s when s.Contains("major") || s.Contains("important") => "HIGH",
        var s when s.Contains("minor") || s.Contains("small") => "LOW",
        _ => "MEDIUM"
    };

    private static string DeterminePriority(string text) => text.ToLower() switch
    {
        var s when s.Contains("urgent") || s.Contains("immediately") => "CRITICAL",
        var s when s.Contains("important") || s.Contains("should") => "HIGH",
        var s when s.Contains("consider") || s.Contains("might") => "MEDIUM",
        _ => "LOW"
    };

    private static string ExtractTitle(string line)
    {
        var title = line.Trim();
        return title.Length > 50 ? title.Substring(0, 47) + "..." : title;
    }
}