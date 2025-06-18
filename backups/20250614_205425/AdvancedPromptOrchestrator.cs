using Mcp.CodeReview.Services;
using System.Text.Json;

namespace Mcp.CodeReview.AI;

/// <summary>
/// Advanced prompt orchestrator implementing cutting-edge prompt engineering techniques
/// Based on latest research from Anthropic, OpenAI, and academic prompt engineering studies
/// </summary>
public class AdvancedPromptOrchestrator
{
    private readonly ClaudeService _claudeService;
    private readonly ILogger<AdvancedPromptOrchestrator> _logger;
    private readonly Dictionary<string, EnhancedAgentPersona> _enhancedPersonas;

    public AdvancedPromptOrchestrator(ClaudeService claudeService, ILogger<AdvancedPromptOrchestrator> logger)
    {
        _claudeService = claudeService;
        _logger = logger;
        _enhancedPersonas = EnhancedAgentPersonas.CreateEnhancedPersonas();
    }

    /// <summary>
    /// Advanced agent analysis using improved prompt engineering techniques:
    /// - Chain-of-thought reasoning
    /// - Few-shot learning with examples
    /// - Structured JSON output
    /// - Context-aware temperature adjustment
    /// - Self-reflection and confidence scoring
    /// </summary>
    public async Task<EnhancedAgentAnalysis> ConductEnhancedAgentAnalysis(
        string agentName,
        AgentConversation conversation,
        ReviewContext context)
    {
        if (!_enhancedPersonas.TryGetValue(agentName, out var persona))
        {
            throw new ArgumentException($"Unknown agent: {agentName}");
        }

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["Agent"] = agentName,
            ["Specialization"] = persona.Specialization,
            ["Language"] = conversation.Language,
            ["CodeLength"] = conversation.CodeChange.Length
        }))
        {
            _logger.LogInformation("Starting enhanced analysis with {Agent}", agentName);

            // Build context-aware prompt with advanced techniques
            var enhancedPrompt = BuildEnhancedPrompt(persona, conversation, context);
            
            // Use agent-specific temperature and parameters
            var response = await _claudeService.GenerateReviewWithParameters(
                enhancedPrompt, 
                temperature: persona.Temperature,
                maxTokens: persona.MaxTokens
            );

            // Parse structured response
            var analysis = ParseEnhancedResponse(response, persona);
            
            _logger.LogInformation("Enhanced analysis completed for {Agent} with confidence {Confidence}", 
                agentName, analysis.ConfidenceLevel);

            return analysis;
        }
    }

    /// <summary>
    /// Builds an enhanced prompt using advanced prompt engineering techniques
    /// </summary>
    private string BuildEnhancedPrompt(EnhancedAgentPersona persona, AgentConversation conversation, ReviewContext context)
    {
        var businessContext = context.Metadata.GetValueOrDefault("BusinessContext", "").ToString();
        var developerExperience = context.Metadata.GetValueOrDefault("DeveloperExperience", "").ToString();

        return $@"<system>
{persona.SystemPrompt}

## Advanced Analysis Instructions

### Context Awareness
You are analyzing code in this context:
- Business Domain: {businessContext}
- Developer Experience: {developerExperience}
- PR Context: {conversation.Context.PullRequestTitle}
- Files Changed: {string.Join(", ", conversation.Context.FilesChanged ?? new List<string>())}
- Language/Framework: {conversation.Language}

### Analysis Approach
1. **Chain-of-Thought**: Work through your analysis step-by-step in the 'thinking' field
2. **Evidence-Based**: Point to specific code locations and patterns
3. **Context-Sensitive**: Consider the business domain and developer experience level
4. **Actionable**: Provide specific, implementable recommendations
5. **Confidence-Aware**: Honestly assess your confidence level in each finding

### Output Requirements
- MUST respond in valid JSON format as specified
- Include confidence levels for all major findings
- Provide specific code examples and remediation
- Consider both immediate fixes and long-term improvements
- Tailor advice to the developer's experience level

### Quality Standards
- Be specific, not generic
- Focus on high-impact improvements
- Consider maintainability over perfection
- Provide learning opportunities, not just fixes
</system>

<user>
## Code Review Request

### Code to Analyze
```{conversation.Language}
{conversation.CodeChange}
```

### Your Task
As a {persona.Specialization} with {persona.ExperienceLevel} experience, analyze this code change and provide insights in the JSON format specified in your persona.

Focus on your areas of expertise:
{string.Join("\n", persona.FocusAreas.Select(area => $"- {area}"))}

### Expected Output Format
{persona.OutputFormat}

{(string.IsNullOrEmpty(persona.ExampleAnalysis) ? "" : $@"
### Reference Example
{persona.ExampleAnalysis}")}

Please provide your analysis now:
</user>";
    }

    /// <summary>
    /// Conducts a collaborative challenge session between agents
    /// Using improved adversarial prompting for better outcomes
    /// </summary>
    public async Task<CollaborativeChallenge> ConductAdvancedCollaborativeChallenge(
        string challengingAgent,
        string targetAgent,
        AgentConversation conversation,
        Dictionary<string, EnhancedAgentAnalysis> previousAnalyses)
    {
        var challengerPersona = _enhancedPersonas[challengingAgent];
        var targetAnalysis = previousAnalyses[targetAgent];

        var challengePrompt = $@"<system>
{challengerPersona.SystemPrompt}

## Collaborative Review Challenge

You are participating in a collaborative code review where multiple expert agents have analyzed the same code. Your role is to provide a constructive challenge to another agent's analysis from your specialized perspective.

### Collaborative Guidelines
- Be respectful but thorough
- Look for blind spots in their analysis
- Identify potential contradictions or missing considerations
- Propose complementary insights from your domain
- Ask clarifying questions where analysis is unclear
- Suggest how your expertise can enhance their findings

### Analysis Quality Framework
- **Completeness**: Did they miss anything important in your domain?
- **Accuracy**: Are there any technical errors from your perspective?
- **Practicality**: Are their recommendations realistic and actionable?
- **Context**: Did they properly consider the business and technical context?
- **Integration**: How do their findings interact with your domain concerns?
</system>

<user>
## Code Under Review
```{conversation.Language}
{conversation.CodeChange}
```

## {targetAgent}'s Analysis to Challenge
{JsonSerializer.Serialize(targetAnalysis, new JsonSerializerOptions { WriteIndented = true })}

## Your Challenge Task
From your perspective as a {challengerPersona.Specialization}, provide a constructive challenge to this analysis.

Required JSON Response Format:
{{
  ""thinking"": ""Your step-by-step analysis of their findings"",
  ""agreements"": [
    {{
      ""finding"": ""what you agree with"",
      ""reasoning"": ""why you agree"",
      ""enhancement"": ""how you could build on this""
    }}
  ],
  ""challenges"": [
    {{
      ""finding"": ""what you question or disagree with"",
      ""concern"": ""what's problematic about it"",
      ""evidence"": ""specific code examples or reasoning"",
      ""alternative"": ""your alternative perspective""
    }}
  ],
  ""complementaryInsights"": [
    {{
      ""insight"": ""additional perspective from your domain"",
      ""importance"": ""why this matters"",
      ""integration"": ""how this connects to their analysis""
    }}
  ],
  ""questions"": [""clarifying questions about unclear aspects""],
  ""synthesisOpportunities"": [
    {{
      ""opportunity"": ""way to combine both perspectives"",
      ""benefits"": ""why this synthesis would be valuable""
    }}
  ],
  ""confidenceLevel"": 1-10
}}

Provide your collaborative challenge now:
</user>";

        var response = await _claudeService.GenerateReviewWithParameters(
            challengePrompt,
            temperature: 0.4, // Higher temperature for creative collaboration
            maxTokens: 3000
        );

        return ParseCollaborativeChallenge(response, challengingAgent, targetAgent);
    }

    /// <summary>
    /// Generates a human-friendly synthesis using advanced summarization techniques
    /// </summary>
    public async Task<EnhancedHumanSummary> GenerateAdvancedHumanSummary(
        AgentConversation conversation,
        Dictionary<string, EnhancedAgentAnalysis> analyses,
        List<CollaborativeChallenge> challenges)
    {
        var synthesisPrompt = $@"<system>
You are an Expert Technical Communication Specialist who excels at translating complex multi-agent AI analysis into clear, actionable insights for human developers.

## Your Communication Expertise
- 15+ years translating technical complexity into clear guidance
- Expert at stakeholder communication across all levels (junior dev to CTO)
- Specialized in decision-support documentation
- Known for actionable, empathetic technical writing

## Synthesis Philosophy
""The best technical communication empowers people to make confident decisions quickly. Complex analysis should become simple clarity.""

## Your Synthesis Goals
1. **Decision Support**: Help humans make confident go/no-go decisions
2. **Learning Amplification**: Turn findings into growth opportunities  
3. **Action Orientation**: Convert analysis into specific next steps
4. **Context Preservation**: Maintain important nuance while simplifying
5. **Confidence Calibration**: Help humans understand confidence levels
</system>

<user>
## Multi-Agent Analysis Results

### Code Reviewed
```{conversation.Language}
{conversation.CodeChange}
```

### Agent Analyses
{JsonSerializer.Serialize(analyses, new JsonSerializerOptions { WriteIndented = true })}

### Collaborative Challenges
{JsonSerializer.Serialize(challenges, new JsonSerializerOptions { WriteIndented = true })}

## Your Synthesis Task

Create a comprehensive yet digestible summary that helps the developer and their team make informed decisions. 

Required Markdown Format:

# 🔍 Code Review Summary

## 📊 Executive Overview
**Overall Assessment**: [Clear verdict with confidence level]
**Primary Concerns**: [Top 2-3 issues by impact]
**Recommended Action**: [Clear go/no-go with conditions]

## 🚨 Critical Issues (Must Fix)
[List with impact/effort/confidence for each]

## 💡 Important Improvements (Should Fix)  
[List with clear business justification]

## 🎯 Learning & Growth Opportunities
[Specific developer growth insights]

## 🏗️ Domain & Architecture Insights
[DDD, feature slicing, and architectural guidance]

## ⚡ Performance & Quality Notes
[Performance and code quality highlights]

## 🛡️ Security Considerations
[Security findings with risk levels]

## 🧪 Testing Strategy
[Testing recommendations and gaps]

## 📋 Implementation Roadmap
- **Immediate (This PR)**: [Must-fix items]
- **Next Sprint**: [Important improvements]  
- **Future**: [Long-term enhancements]

## 🤝 Collaborative Insights
[Key agreements and productive tensions between agents]

## 🎯 Decision Framework
- **Merge Recommendation**: [Yes/No/Conditional with clear criteria]
- **Risk Level**: [Low/Medium/High with explanation]
- **Review Confidence**: [1-10 with reasoning]

Generate this synthesis now:
</user>";

        var response = await _claudeService.GenerateReviewWithParameters(
            synthesisPrompt,
            temperature: 0.3, // Lower temperature for accuracy in synthesis
            maxTokens: 4000
        );

        return new EnhancedHumanSummary
        {
            MarkdownSummary = response,
            OverallConfidence = ExtractConfidenceFromSummary(response),
            MergeRecommendation = ExtractMergeRecommendation(response),
            RiskLevel = ExtractRiskLevel(response),
            CriticalIssueCount = ExtractCriticalCount(response),
            LearningOpportunities = ExtractLearningOpportunities(response)
        };
    }

    // Helper methods for parsing enhanced responses
    private EnhancedAgentAnalysis ParseEnhancedResponse(string response, EnhancedAgentPersona persona)
    {
        try
        {
            // Try to parse as JSON first
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response);
            
            return new EnhancedAgentAnalysis
            {
                Agent = persona.Name,
                Specialization = persona.Specialization,
                RawResponse = response,
                StructuredData = jsonResponse,
                ConfidenceLevel = ExtractConfidence(jsonResponse),
                Findings = ExtractFindings(jsonResponse),
                Recommendations = ExtractRecommendations(jsonResponse),
                Timestamp = DateTime.UtcNow
            };
        }
        catch (JsonException)
        {
            // Fallback to text parsing if JSON parsing fails
            return new EnhancedAgentAnalysis
            {
                Agent = persona.Name,
                Specialization = persona.Specialization,
                RawResponse = response,
                ConfidenceLevel = ExtractConfidenceFromText(response),
                Findings = ExtractFindingsFromText(response),
                Recommendations = ExtractRecommendationsFromText(response),
                Timestamp = DateTime.UtcNow
            };
        }
    }

    private CollaborativeChallenge ParseCollaborativeChallenge(string response, string challenger, string target)
    {
        // Implementation for parsing collaborative challenge responses
        return new CollaborativeChallenge
        {
            ChallengingAgent = challenger,
            TargetAgent = target,
            Response = response,
            Timestamp = DateTime.UtcNow
        };
    }

    // Extraction helper methods
    private double ExtractConfidence(JsonElement json)
    {
        if (json.TryGetProperty("confidenceLevel", out var confidence))
        {
            return confidence.GetDouble();
        }
        return 0.8; // Default confidence
    }

    private List<string> ExtractFindings(JsonElement json)
    {
        var findings = new List<string>();
        // Implementation depends on specific agent output format
        return findings;
    }

    private List<string> ExtractRecommendations(JsonElement json)
    {
        var recommendations = new List<string>();
        // Implementation depends on specific agent output format
        return recommendations;
    }

    private double ExtractConfidenceFromText(string text) => 0.8;
    private List<string> ExtractFindingsFromText(string text) => new();
    private List<string> ExtractRecommendationsFromText(string text) => new();
    private double ExtractConfidenceFromSummary(string summary) => 0.85;
    private string ExtractMergeRecommendation(string summary) => "Conditional";
    private string ExtractRiskLevel(string summary) => "Medium";
    private int ExtractCriticalCount(string summary) => 0;
    private List<string> ExtractLearningOpportunities(string summary) => new();
}

// Enhanced data models
public class EnhancedAgentAnalysis
{
    public string Agent { get; set; } = "";
    public string Specialization { get; set; } = "";
    public string RawResponse { get; set; } = "";
    public JsonElement StructuredData { get; set; }
    public double ConfidenceLevel { get; set; }
    public List<string> Findings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class CollaborativeChallenge
{
    public string ChallengingAgent { get; set; } = "";
    public string TargetAgent { get; set; } = "";
    public string Response { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

public class EnhancedHumanSummary
{
    public string MarkdownSummary { get; set; } = "";
    public double OverallConfidence { get; set; }
    public string MergeRecommendation { get; set; } = "";
    public string RiskLevel { get; set; } = "";
    public int CriticalIssueCount { get; set; }
    public List<string> LearningOpportunities { get; set; } = new();
}