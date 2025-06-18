using System.Text;
using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.Utilities;

/// <summary>
/// Centralized prompt building utilities for AI interactions
/// </summary>
public static class PromptBuilder
{
    private const int MaxPromptLength = 50000; // Reasonable limit for AI models
    
    /// <summary>
    /// Build a structured code review prompt
    /// </summary>
    public static string BuildCodeReviewPrompt(CodeReviewRequest request)
    {
        var prompt = new StringBuilder();
        
        // Add role and context
        prompt.AppendLine("You are an expert code reviewer with years of experience in software development.");
        prompt.AppendLine("Please provide a thorough, constructive analysis of the following code.");
        prompt.AppendLine();
        
        // Add specific context if provided
        if (!string.IsNullOrEmpty(request.Context))
        {
            prompt.AppendLine($"## Context");
            prompt.AppendLine(request.Context);
            prompt.AppendLine();
        }
        
        // Add business domain context
        if (!string.IsNullOrEmpty(request.Options.BusinessDomain))
        {
            prompt.AppendLine($"## Business Domain: {request.Options.BusinessDomain}");
            prompt.AppendLine();
        }
        
        // Add team context
        if (!string.IsNullOrEmpty(request.Options.TeamContext))
        {
            prompt.AppendLine($"## Team Context: {request.Options.TeamContext}");
            prompt.AppendLine();
        }
        
        // Add analysis instructions based on options
        prompt.AppendLine("## Analysis Focus:");
        AddAnalysisFocus(prompt, request.Options);
        prompt.AppendLine();
        
        // Add the code content
        prompt.AppendLine("## Code to Review:");
        if (!string.IsNullOrEmpty(request.FileName))
        {
            prompt.AppendLine($"**File: {request.FileName}**");
        }
        if (!string.IsNullOrEmpty(request.Language))
        {
            prompt.AppendLine($"**Language: {request.Language}**");
        }
        prompt.AppendLine();
        prompt.AppendLine("```");
        prompt.AppendLine(TruncateContent(request.Content));
        prompt.AppendLine("```");
        prompt.AppendLine();
        
        // Add output format instructions
        AddOutputFormatInstructions(prompt, request.Options.ReviewDepth);
        
        return prompt.ToString();
    }
    
    /// <summary>
    /// Build a specialized agent prompt
    /// </summary>
    public static string BuildAgentPrompt(AgentType agentType, string content, Dictionary<string, object> context)
    {
        var prompt = new StringBuilder();
        
        // Add agent-specific role
        prompt.AppendLine(GetAgentRolePrompt(agentType));
        prompt.AppendLine();
        
        // Add context if provided
        if (context.Any())
        {
            prompt.AppendLine("## Context:");
            foreach (var kvp in context)
            {
                prompt.AppendLine($"- {kvp.Key}: {kvp.Value}");
            }
            prompt.AppendLine();
        }
        
        // Add content
        prompt.AppendLine("## Code to Analyze:");
        prompt.AppendLine("```");
        prompt.AppendLine(TruncateContent(content));
        prompt.AppendLine("```");
        prompt.AppendLine();
        
        // Add agent-specific instructions
        prompt.AppendLine(GetAgentInstructions(agentType));
        
        return prompt.ToString();
    }
    
    /// <summary>
    /// Build a comparative analysis prompt
    /// </summary>
    public static string BuildComparativePrompt(string beforeCode, string afterCode, string analysisType)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine($"You are conducting a {analysisType} analysis comparing two versions of code.");
        prompt.AppendLine("Please analyze the differences and provide insights on the changes.");
        prompt.AppendLine();
        
        prompt.AppendLine("## Before (Original Code):");
        prompt.AppendLine("```");
        prompt.AppendLine(TruncateContent(beforeCode));
        prompt.AppendLine("```");
        prompt.AppendLine();
        
        prompt.AppendLine("## After (Modified Code):");
        prompt.AppendLine("```");
        prompt.AppendLine(TruncateContent(afterCode));
        prompt.AppendLine("```");
        prompt.AppendLine();
        
        prompt.AppendLine("## Analysis Request:");
        prompt.AppendLine("Please provide:");
        prompt.AppendLine("1. Summary of key changes");
        prompt.AppendLine("2. Impact assessment");
        prompt.AppendLine("3. Potential risks or benefits");
        prompt.AppendLine("4. Recommendations for improvement");
        
        return prompt.ToString();
    }
    
    /// <summary>
    /// Build a batch analysis prompt for multiple files
    /// </summary>
    public static string BuildBatchAnalysisPrompt(List<(string fileName, string content)> files, string analysisType)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine($"You are conducting a {analysisType} analysis across multiple files.");
        prompt.AppendLine("Please analyze each file and provide cross-file insights.");
        prompt.AppendLine();
        
        for (int i = 0; i < files.Count; i++)
        {
            var (fileName, content) = files[i];
            prompt.AppendLine($"## File {i + 1}: {fileName}");
            prompt.AppendLine("```");
            prompt.AppendLine(TruncateContent(content));
            prompt.AppendLine("```");
            prompt.AppendLine();
        }
        
        prompt.AppendLine("## Analysis Request:");
        prompt.AppendLine("Please provide:");
        prompt.AppendLine("1. Individual file analysis");
        prompt.AppendLine("2. Cross-file dependencies and interactions");
        prompt.AppendLine("3. Overall architecture assessment");
        prompt.AppendLine("4. Consistency analysis");
        prompt.AppendLine("5. Recommendations for the codebase as a whole");
        
        return prompt.ToString();
    }
    
    // Private helper methods
    private static void AddAnalysisFocus(StringBuilder prompt, ReviewOptions options)
    {
        var focuses = new List<string>();
        
        if (options.IncludeSecurityAnalysis) focuses.Add("Security vulnerabilities and best practices");
        if (options.IncludePerformanceAnalysis) focuses.Add("Performance optimization opportunities");
        if (options.IncludeQualityAnalysis) focuses.Add("Code quality and maintainability");
        if (options.IncludeTestSuggestions) focuses.Add("Testing coverage and strategies");
        if (options.IncludeRefactoringSuggestions) focuses.Add("Refactoring opportunities");
        
        foreach (var focus in focuses)
        {
            prompt.AppendLine($"- {focus}");
        }
    }
    
    private static void AddOutputFormatInstructions(StringBuilder prompt, string reviewDepth)
    {
        prompt.AppendLine("## Output Format:");
        prompt.AppendLine("Please structure your response with the following sections:");
        prompt.AppendLine("1. **Executive Summary** - Brief overview of findings");
        prompt.AppendLine("2. **Key Issues** - Most important problems found");
        prompt.AppendLine("3. **Security Concerns** - Security-related findings");
        prompt.AppendLine("4. **Performance Issues** - Performance optimization opportunities");
        prompt.AppendLine("5. **Code Quality** - Maintainability and readability improvements");
        prompt.AppendLine("6. **Recommendations** - Prioritized action items");
        
        if (reviewDepth == "comprehensive")
        {
            prompt.AppendLine("7. **Detailed Analysis** - In-depth technical analysis");
            prompt.AppendLine("8. **Best Practices** - Industry standard recommendations");
            prompt.AppendLine("9. **Future Considerations** - Long-term architectural suggestions");
        }
    }
    
    private static string GetAgentRolePrompt(AgentType agentType) => agentType switch
    {
        AgentType.SecurityExpert => "You are a cybersecurity expert specializing in secure coding practices and vulnerability assessment.",
        AgentType.PerformanceAnalyst => "You are a performance optimization expert with deep knowledge of efficient algorithms and system optimization.",
        AgentType.CodeQualityReviewer => "You are a senior code quality specialist focused on maintainability, readability, and best practices.",
        AgentType.ArchitectureExpert => "You are a software architecture expert with extensive experience in system design and architectural patterns.",
        AgentType.TestingSpecialist => "You are a testing expert specializing in test strategy, coverage analysis, and quality assurance.",
        AgentType.DomainExpert => "You are a domain-driven design expert focusing on business logic and domain modeling.",
        AgentType.FeatureSlicingExpert => "You are an expert in feature slicing and vertical slice architecture patterns.",
        AgentType.DeveloperMentor => "You are an experienced mentor focused on developer growth and skill improvement.",
        AgentType.AICodeDetective => "You are an expert in detecting AI-generated code patterns and potential shortcuts.",
        _ => "You are an expert code reviewer with comprehensive software development experience."
    };
    
    private static string GetAgentInstructions(AgentType agentType) => agentType switch
    {
        AgentType.SecurityExpert => "Focus on security vulnerabilities, authentication, authorization, data protection, and secure coding practices.",
        AgentType.PerformanceAnalyst => "Analyze for performance bottlenecks, memory usage, algorithmic efficiency, and optimization opportunities.",
        AgentType.CodeQualityReviewer => "Evaluate code structure, naming conventions, maintainability, and adherence to coding standards.",
        AgentType.ArchitectureExpert => "Assess architectural patterns, design principles, modularity, and system structure.",
        AgentType.TestingSpecialist => "Review testing approach, coverage, test quality, and suggest testing improvements.",
        AgentType.DomainExpert => "Analyze domain model accuracy, business logic clarity, and domain-driven design principles.",
        AgentType.FeatureSlicingExpert => "Evaluate feature organization, vertical slicing, and feature cohesion.",
        AgentType.DeveloperMentor => "Provide educational insights, learning opportunities, and skill development suggestions.",
        AgentType.AICodeDetective => "Detect AI-generated patterns, shortcuts, bypasses, and potential quality issues from AI assistance.",
        _ => "Provide comprehensive analysis focusing on your area of expertise."
    };
    
    private static string TruncateContent(string content)
    {
        if (content.Length <= MaxPromptLength)
            return content;
            
        var truncated = content.Substring(0, MaxPromptLength - 100);
        return truncated + "\n\n... [Content truncated for length] ...";
    }
}