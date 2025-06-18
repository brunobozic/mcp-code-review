using System.Text;
using System.Text.Json;
using Mcp.CodeReview.AI;
using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.Utilities;

/// <summary>
/// Enhanced prompt builder with sophisticated AI prompting techniques
/// Integrates advanced personas, chain-of-thought reasoning, and context-aware prompting
/// </summary>
public static class EnhancedPromptBuilder
{
    private const int MaxPromptLength = 100000; // Increased for more sophisticated prompts
    
    /// <summary>
    /// Build an enhanced agent prompt with sophisticated techniques
    /// </summary>
    public static string BuildEnhancedAgentPrompt(
        AgentType agentType, 
        string content, 
        Dictionary<string, object> context,
        ReviewOptions? options = null)
    {
        var personas = EnhancedAgentPersonas.CreateEnhancedPersonas();
        var agentKey = agentType.ToString();
        
        if (!personas.ContainsKey(agentKey))
        {
            // Fallback to basic prompt for agents without enhanced personas
            return PromptBuilder.BuildAgentPrompt(agentType, content, context);
        }
        
        var persona = personas[agentKey];
        var prompt = new StringBuilder();
        
        // 1. Rich Agent Persona with Experience and Methodology
        prompt.AppendLine(persona.SystemPrompt);
        prompt.AppendLine();
        
        // 2. Context-Aware Analysis Instructions
        BuildContextAwareInstructions(prompt, context, options);
        
        // 3. Chain-of-Thought Analysis Framework
        prompt.AppendLine("## Your Analysis Process:");
        prompt.AppendLine("Follow this systematic approach:");
        prompt.AppendLine(persona.AnalysisFramework);
        prompt.AppendLine();
        
        // 4. Code Content with Smart Truncation
        prompt.AppendLine("## Code to Analyze:");
        AddCodeContent(prompt, content, context);
        
        // 5. Structured Output Format with JSON Schema
        prompt.AppendLine("## Required Output Format:");
        prompt.AppendLine("You MUST respond with valid JSON matching this exact schema:");
        prompt.AppendLine("```json");
        prompt.AppendLine(persona.OutputSchema);
        prompt.AppendLine("```");
        prompt.AppendLine();
        
        // 6. Few-Shot Examples for Better Performance
        if (!string.IsNullOrEmpty(persona.ExampleAnalysis))
        {
            prompt.AppendLine("## Example Analysis:");
            prompt.AppendLine("Here's an example of the quality and format expected:");
            prompt.AppendLine(persona.ExampleAnalysis);
            prompt.AppendLine();
        }
        
        // 7. Advanced Chain-of-Thought and Self-Reflection Instructions
        prompt.AppendLine("## Advanced Analysis Instructions:");
        prompt.AppendLine("Before providing your final JSON response, think through your analysis step-by-step:");
        prompt.AppendLine();
        prompt.AppendLine("### Step 1: Initial Assessment");
        prompt.AppendLine("- What type of code is this? (business logic, data access, UI, etc.)");
        prompt.AppendLine("- What are the obvious patterns, both good and problematic?");
        prompt.AppendLine("- What would a junior developer miss that you can catch?");
        prompt.AppendLine();
        prompt.AppendLine("### Step 2: Deep Analysis (Chain-of-Thought)");
        prompt.AppendLine("- Walk through the code execution path mentally");
        prompt.AppendLine("- Consider edge cases and failure scenarios");
        prompt.AppendLine("- Think about how this code interacts with other system components");
        prompt.AppendLine("- What would happen under high load, with malicious input, or in error conditions?");
        prompt.AppendLine();
        prompt.AppendLine("### Step 3: Contrastive Thinking");
        prompt.AppendLine("- What would GOOD code look like for this same functionality?");
        prompt.AppendLine("- What are the WORST possible ways to implement this?");
        prompt.AppendLine("- How does this code compare to industry best practices?");
        prompt.AppendLine();
        prompt.AppendLine("### Step 4: Self-Validation");
        prompt.AppendLine("- Are my findings actually important or am I being pedantic?");
        prompt.AppendLine("- Do my recommendations have clear business value?");
        prompt.AppendLine("- What would a senior peer reviewer say about my analysis?");
        prompt.AppendLine("- Am I confident in my assessment? (rate 1-10)");
        prompt.AppendLine();
        prompt.AppendLine("### Final Instructions:");
        prompt.AppendLine("- Be honest about uncertainty - use lower confidence scores when unsure");
        prompt.AppendLine("- Provide specific, actionable recommendations with clear implementation steps");
        prompt.AppendLine("- Consider the broader system context, not just the isolated code snippet");
        prompt.AppendLine("- If the code quality is excellent, say so - don't manufacture issues");
        prompt.AppendLine("- Focus on the most impactful issues first, then mention minor improvements");
        prompt.AppendLine("- Include your reasoning process in the 'thinking' field of your JSON response");
        
        return TruncateSmartly(prompt.ToString());
    }
    
    /// <summary>
    /// Build a multi-turn conversation prompt for iterative analysis
    /// </summary>
    public static string BuildConversationContinuationPrompt(
        AgentType agentType,
        List<(string role, string content)> conversationHistory,
        string newUserInput,
        Dictionary<string, object> context)
    {
        var personas = EnhancedAgentPersonas.CreateEnhancedPersonas();
        var agentKey = agentType.ToString();
        
        var prompt = new StringBuilder();
        
        // Agent persona (abbreviated for follow-up)
        if (personas.ContainsKey(agentKey))
        {
            var persona = personas[agentKey];
            prompt.AppendLine($"You are {persona.Name}, {persona.Specialization}.");
            prompt.AppendLine("Continue this analysis conversation with the user.");
            prompt.AppendLine();
        }
        
        // Conversation history
        prompt.AppendLine("## Conversation History:");
        foreach (var (role, content) in conversationHistory.TakeLast(5)) // Keep last 5 exchanges
        {
            prompt.AppendLine($"**{role}**: {content}");
            prompt.AppendLine();
        }
        
        // New user input
        prompt.AppendLine("## User's Latest Question/Request:");
        prompt.AppendLine(newUserInput);
        prompt.AppendLine();
        
        // Instructions for continuation
        prompt.AppendLine("## Instructions:");
        prompt.AppendLine("- Build on your previous analysis");
        prompt.AppendLine("- Address the user's specific question directly");
        prompt.AppendLine("- Provide additional insights if relevant");
        prompt.AppendLine("- Maintain your expert persona and analytical approach");
        prompt.AppendLine("- If you need clarification, ask specific follow-up questions");
        
        return prompt.ToString();
    }
    
    /// <summary>
    /// Build a collaborative analysis prompt for agent-to-agent communication
    /// </summary>
    public static string BuildCollaborativePrompt(
        AgentType currentAgent,
        AgentType targetAgent,
        List<AgentResult> previousResults,
        string collaborationRequest,
        Dictionary<string, object> context)
    {
        var personas = EnhancedAgentPersonas.CreateEnhancedPersonas();
        var currentPersona = personas.GetValueOrDefault(currentAgent.ToString());
        var targetPersona = personas.GetValueOrDefault(targetAgent.ToString());
        
        var prompt = new StringBuilder();
        
        // Collaboration context
        prompt.AppendLine($"You are {currentPersona?.Name ?? currentAgent.ToString()}, collaborating with {targetPersona?.Name ?? targetAgent.ToString()}.");
        prompt.AppendLine("You are working together to provide comprehensive code analysis.");
        prompt.AppendLine();
        
        // Previous analysis results
        prompt.AppendLine("## Previous Analysis Results:");
        foreach (var result in previousResults)
        {
            prompt.AppendLine($"**{result.AgentName} ({result.AgentType})**:");
            prompt.AppendLine($"- Confidence: {result.ConfidenceScore:F2}");
            prompt.AppendLine($"- Key Findings: {result.Findings.Count} issues identified");
            prompt.AppendLine($"- Summary: {TruncateContent(result.Analysis, 200)}");
            prompt.AppendLine();
        }
        
        // Collaboration request
        prompt.AppendLine("## Collaboration Request:");
        prompt.AppendLine(collaborationRequest);
        prompt.AppendLine();
        
        // Instructions
        prompt.AppendLine("## Instructions:");
        prompt.AppendLine("- Build on the previous analysis from other experts");
        prompt.AppendLine("- Identify areas of agreement and disagreement");
        prompt.AppendLine("- Provide your specialized perspective");
        prompt.AppendLine("- Suggest how to resolve any conflicting recommendations");
        prompt.AppendLine("- Focus on creating a unified, comprehensive analysis");
        
        return prompt.ToString();
    }
    
    /// <summary>
    /// Build an adaptive prompt based on code complexity and team experience
    /// </summary>
    public static string BuildAdaptivePrompt(
        AgentType agentType,
        string content,
        Dictionary<string, object> context,
        CodeComplexity complexity,
        TeamExperience teamLevel)
    {
        var basePrompt = BuildEnhancedAgentPrompt(agentType, content, context);
        var prompt = new StringBuilder(basePrompt);
        
        // Add complexity-specific instructions
        prompt.AppendLine();
        prompt.AppendLine("## Adaptive Analysis Instructions:");
        
        switch (complexity)
        {
            case CodeComplexity.Low:
                prompt.AppendLine("- This appears to be straightforward code - focus on quick wins and best practices");
                prompt.AppendLine("- Don't over-engineer simple solutions");
                prompt.AppendLine("- Emphasize clarity and maintainability");
                break;
                
            case CodeComplexity.Medium:
                prompt.AppendLine("- This code has moderate complexity - balance thoroughness with practicality");
                prompt.AppendLine("- Look for opportunities to reduce complexity");
                prompt.AppendLine("- Consider both immediate and long-term improvements");
                break;
                
            case CodeComplexity.High:
                prompt.AppendLine("- This is complex code requiring careful analysis");
                prompt.AppendLine("- Prioritize critical issues that could cause system failures");
                prompt.AppendLine("- Consider breaking down complex components");
                prompt.AppendLine("- Focus on architecture and design patterns");
                break;
        }
        
        // Add team-level specific guidance
        switch (teamLevel)
        {
            case TeamExperience.Junior:
                prompt.AppendLine("- Provide educational explanations with your recommendations");
                prompt.AppendLine("- Include links to documentation and best practices");
                prompt.AppendLine("- Focus on fundamental concepts and common pitfalls");
                break;
                
            case TeamExperience.Mid:
                prompt.AppendLine("- Balance detailed analysis with practical considerations");
                prompt.AppendLine("- Explain the 'why' behind recommendations");
                prompt.AppendLine("- Suggest incremental improvement strategies");
                break;
                
            case TeamExperience.Senior:
                prompt.AppendLine("- Focus on advanced patterns and architectural considerations");
                prompt.AppendLine("- Discuss trade-offs and alternative approaches");
                prompt.AppendLine("- Consider system-wide implications and scalability");
                break;
        }
        
        return prompt.ToString();
    }
    
    /// <summary>
    /// Build a synthesis prompt for combining multiple agent results
    /// </summary>
    public static string BuildSynthesisPrompt(
        List<AgentResult> agentResults,
        CodeReviewRequest originalRequest)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine("You are a Senior Technical Lead with 20+ years of experience synthesizing expert opinions into actionable insights.");
        prompt.AppendLine("Your role is to combine multiple specialized analyses into a coherent, prioritized action plan.");
        prompt.AppendLine();
        
        // Original context
        prompt.AppendLine("## Original Code Review Request:");
        prompt.AppendLine($"- File: {originalRequest.FileName}");
        prompt.AppendLine($"- Language: {originalRequest.Language}");
        prompt.AppendLine($"- Context: {originalRequest.Context}");
        prompt.AppendLine($"- Business Domain: {originalRequest.Options.BusinessDomain}");
        prompt.AppendLine();
        
        // Expert analyses
        prompt.AppendLine("## Expert Analyses to Synthesize:");
        foreach (var result in agentResults.OrderByDescending(r => r.ConfidenceScore))
        {
            prompt.AppendLine($"### {result.AgentName} (Confidence: {result.ConfidenceScore:F2})");
            prompt.AppendLine($"**Findings ({result.Findings.Count} issues):**");
            foreach (var finding in result.Findings.Take(3))
            {
                prompt.AppendLine($"- {finding.Severity}: {finding.Description}");
            }
            
            prompt.AppendLine($"**Key Recommendations ({result.Recommendations.Count} total):**");
            foreach (var rec in result.Recommendations.Take(3))
            {
                prompt.AppendLine($"- {rec.Priority}: {rec.Title}");
            }
            prompt.AppendLine();
        }
        
        // Synthesis instructions
        prompt.AppendLine("## Synthesis Requirements:");
        prompt.AppendLine("Create a unified analysis that:");
        prompt.AppendLine("1. **Identifies consensus** - Issues multiple experts agree on");
        prompt.AppendLine("2. **Resolves conflicts** - Where experts disagree, provide balanced perspective");
        prompt.AppendLine("3. **Prioritizes actions** - Order recommendations by business impact and effort");
        prompt.AppendLine("4. **Provides executive summary** - High-level overview for stakeholders");
        prompt.AppendLine("5. **Creates implementation roadmap** - Phased approach to improvements");
        prompt.AppendLine();
        
        prompt.AppendLine("## Required Output Format:");
        prompt.AppendLine("```json");
        prompt.AppendLine(GetSynthesisOutputSchema());
        prompt.AppendLine("```");
        
        return prompt.ToString();
    }
    
    // Private helper methods
    private static void BuildContextAwareInstructions(
        StringBuilder prompt, 
        Dictionary<string, object> context, 
        ReviewOptions? options)
    {
        prompt.AppendLine("## Current Analysis Context:");
        
        // Add business context
        if (context.TryGetValue("BusinessDomain", out var domain))
        {
            prompt.AppendLine($"- **Business Domain**: {domain}");
        }
        
        if (context.TryGetValue("TeamContext", out var team))
        {
            prompt.AppendLine($"- **Team Context**: {team}");
        }
        
        // Add file context
        if (context.TryGetValue("FileName", out var fileName))
        {
            prompt.AppendLine($"- **File**: {fileName}");
        }
        
        if (context.TryGetValue("Language", out var language))
        {
            prompt.AppendLine($"- **Language**: {language}");
        }
        
        // Add review options
        if (options != null)
        {
            prompt.AppendLine($"- **Review Depth**: {options.ReviewDepth}");
            if (options.IncludeSecurityAnalysis) prompt.AppendLine("- **Focus**: Include security analysis");
            if (options.IncludePerformanceAnalysis) prompt.AppendLine("- **Focus**: Include performance analysis");
        }
        
        prompt.AppendLine();
    }
    
    private static void AddCodeContent(StringBuilder prompt, string content, Dictionary<string, object> context)
    {
        // Add language hint for better syntax understanding
        if (context.TryGetValue("Language", out var language))
        {
            prompt.AppendLine($"```{language}");
        }
        else
        {
            prompt.AppendLine("```");
        }
        
        prompt.AppendLine(TruncateSmartly(content));
        prompt.AppendLine("```");
        prompt.AppendLine();
    }
    
    private static string TruncateSmartly(string content)
    {
        if (content.Length <= MaxPromptLength)
            return content;
        
        // Smart truncation: try to preserve complete functions/classes
        var lines = content.Split('\n');
        var truncated = new StringBuilder();
        var currentLength = 0;
        
        foreach (var line in lines)
        {
            if (currentLength + line.Length > MaxPromptLength - 200) // Leave buffer
                break;
                
            truncated.AppendLine(line);
            currentLength += line.Length + 1; // +1 for newline
        }
        
        truncated.AppendLine();
        truncated.AppendLine("... [Content truncated - analysis continues with visible portion] ...");
        
        return truncated.ToString();
    }
    
    private static string TruncateContent(string content, int maxLength)
    {
        if (content.Length <= maxLength)
            return content;
            
        return content.Substring(0, maxLength - 3) + "...";
    }
    
    private static string GetSynthesisOutputSchema()
    {
        return @"{
  ""executiveSummary"": ""Brief overview of overall code quality and key findings"",
  ""overallQualityScore"": 0.85,
  ""consensusFindings"": [
    {
      ""issue"": ""Description of agreed-upon issue"",
      ""severity"": ""HIGH|MEDIUM|LOW"",
      ""agentsAgreeing"": [""SecurityExpert"", ""CodeQualityReviewer""]
    }
  ],
  ""conflictingRecommendations"": [
    {
      ""issue"": ""Area of disagreement"",
      ""perspectives"": [
        {
          ""agent"": ""SecurityExpert"",
          ""recommendation"": ""Agent's recommendation"",
          ""reasoning"": ""Why they recommend this""
        }
      ],
      ""synthesizedRecommendation"": ""Balanced recommendation resolving the conflict""
    }
  ],
  ""prioritizedActions"": [
    {
      ""title"": ""Action item title"",
      ""description"": ""Detailed description"",
      ""priority"": ""CRITICAL|HIGH|MEDIUM|LOW"",
      ""effort"": ""HIGH|MEDIUM|LOW"",
      ""businessImpact"": ""Description of business impact"",
      ""implementationSteps"": [""Step 1"", ""Step 2""]
    }
  ],
  ""implementationRoadmap"": {
    ""immediate"": [""Actions to take immediately""],
    ""shortTerm"": [""Actions for next sprint/iteration""],
    ""longTerm"": [""Architectural improvements for future""]
  },
  ""confidenceScore"": 0.9
}";
    }
}

/// <summary>
/// Enums for adaptive prompting
/// </summary>
public enum CodeComplexity
{
    Low,
    Medium,
    High
}

public enum TeamExperience
{
    Junior,
    Mid,
    Senior
}