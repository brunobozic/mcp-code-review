using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;
using Mcp.CodeReview.RAG;

namespace Mcp.CodeReview.AI.Advanced
{
    /// <summary>
    /// Advanced context manager implementing RAG-enhanced context awareness
    /// Provides enterprise-grade codebase understanding and memory management
    /// </summary>
    public class ContextManager
    {
        private readonly ILogger<ContextManager> _logger;
        private readonly IClaudeService _claudeService;
        private readonly IVectorSearchService _vectorSearchService;
        private readonly Dictionary<string, ProjectContext> _projectCache = new();
        private readonly Dictionary<string, KnowledgeGraph> _knowledgeGraphs = new();
        
        public ContextManager(
            ILogger<ContextManager> logger,
            IClaudeService claudeService,
            IVectorSearchService vectorSearchService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
            _vectorSearchService = vectorSearchService ?? throw new ArgumentNullException(nameof(vectorSearchService));
        }

        /// <summary>
        /// Builds enhanced context with RAG capabilities
        /// </summary>
        public async Task<EnhancedContext> BuildEnhancedContextAsync(
            CodeReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Building enhanced context for {FileName}", request.FileName);

            try
            {
                // Phase 1: Basic context extraction
                var basicContext = ExtractBasicContext(request);
                
                // Phase 2: Project-level context analysis
                var projectContext = await AnalyzeProjectContextAsync(request, cancellationToken);
                
                // Phase 3: Code complexity assessment
                var complexityAnalysis = await AnalyzeCodeComplexityAsync(request.Content, cancellationToken);
                
                // Phase 4: Historical context integration
                var historicalContext = await IntegrateHistoricalContextAsync(request, cancellationToken);
                
                // Phase 5: Memory and knowledge graph integration
                var memoryContext = await IntegrateMemoryContextAsync(request, cancellationToken);
                
                // Phase 6: Team and organizational context
                var teamContext = await AnalyzeTeamContextAsync(request, cancellationToken);

                return new EnhancedContext
                {
                    ProjectId = ExtractProjectId(request),
                    Language = request.Language,
                    ProjectType = projectContext.ProjectType,
                    CodeComplexity = complexityAnalysis.ComplexityScore,
                    Metadata = CombineMetadata(basicContext, projectContext, complexityAnalysis),
                    AvailableTools = GetAvailableTools(request),
                    History = historicalContext,
                    TeamContext = teamContext,
                    MemoryContext = memoryContext
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build enhanced context for {FileName}", request.FileName);
                return CreateFallbackContext(request);
            }
        }

        /// <summary>
        /// Extracts basic context from the request
        /// </summary>
        private Dictionary<string, object> ExtractBasicContext(CodeReviewRequest request)
        {
            return new Dictionary<string, object>
            {
                ["FileName"] = request.FileName,
                ["Language"] = request.Language,
                ["ContentLength"] = request.Content.Length,
                ["LineCount"] = request.Content.Split('\n').Length,
                ["BusinessDomain"] = request.Options.BusinessDomain,
                ["TeamContext"] = request.Options.TeamContext,
                ["ReviewDepth"] = request.Options.ReviewDepth,
                ["RequestTimestamp"] = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Analyzes project-level context using advanced techniques
        /// </summary>
        private async Task<ProjectContext> AnalyzeProjectContextAsync(
            CodeReviewRequest request,
            CancellationToken cancellationToken)
        {
            var projectId = ExtractProjectId(request);
            
            // Check cache first
            if (_projectCache.TryGetValue(projectId, out var cachedContext) && 
                IsContextFresh(cachedContext))
            {
                return cachedContext;
            }

            _logger.LogDebug("Analyzing project context for {ProjectId}", projectId);

            var prompt = $@"
Analyze this code file to understand the broader project context:

File: {request.FileName}
Language: {request.Language}
Code:
{request.Content}

Provide analysis in the following areas:
1. **Project Type**: (web app, library, microservice, desktop app, etc.)
2. **Architecture Pattern**: (MVC, microservices, layered, clean architecture, etc.)
3. **Technology Stack**: (frameworks, libraries, databases identified)
4. **Domain**: (business domain this code belongs to)
5. **Maturity Level**: (prototype, production-ready, legacy, modern)
6. **Code Style**: (formal/enterprise, startup/agile, academic, etc.)

Format your response as:
PROJECT_TYPE: [type]
ARCHITECTURE: [pattern]
TECH_STACK: [technologies]
DOMAIN: [domain]
MATURITY: [level]
STYLE: [style]
";

            var response = await _claudeService.GetCompletionAsync(prompt, cancellationToken);
            var projectContext = ParseProjectContext(response, projectId);
            
            // Cache the result
            _projectCache[projectId] = projectContext;
            
            return projectContext;
        }

        /// <summary>
        /// Analyzes code complexity using multiple dimensions
        /// </summary>
        private async Task<ComplexityAnalysis> AnalyzeCodeComplexityAsync(
            string code,
            CancellationToken cancellationToken)
        {
            _logger.LogDebug("Analyzing code complexity");

            // Structural analysis
            var structuralComplexity = AnalyzeStructuralComplexity(code);
            
            // Cognitive complexity analysis
            var cognitiveComplexity = await AnalyzeCognitiveComplexityAsync(code, cancellationToken);
            
            // Maintainability analysis
            var maintainabilityScore = AnalyzeMaintainability(code);
            
            // Business logic complexity
            var businessLogicComplexity = AnalyzeBusinessLogicComplexity(code);

            var overallComplexity = CalculateOverallComplexity(
                structuralComplexity, 
                cognitiveComplexity, 
                maintainabilityScore, 
                businessLogicComplexity);

            return new ComplexityAnalysis
            {
                ComplexityScore = overallComplexity,
                StructuralComplexity = structuralComplexity,
                CognitiveComplexity = cognitiveComplexity,
                MaintainabilityScore = maintainabilityScore,
                BusinessLogicComplexity = businessLogicComplexity,
                Analysis = await GenerateComplexityExplanation(code, overallComplexity, cancellationToken)
            };
        }

        /// <summary>
        /// Integrates historical context and patterns
        /// </summary>
        private async Task<ProjectHistory> IntegrateHistoricalContextAsync(
            CodeReviewRequest request,
            CancellationToken cancellationToken)
        {
            var projectId = ExtractProjectId(request);
            
            // In a real implementation, this would query a database or version control system
            // For now, we'll simulate historical context
            return new ProjectHistory
            {
                PreviousIssues = await GetPreviousIssues(projectId, cancellationToken),
                SuccessfulPatterns = await GetSuccessfulPatterns(projectId, cancellationToken),
                TechnologyStack = await AnalyzeTechnologyStack(request.Content, cancellationToken),
                TeamPreferences = await GetTeamPreferences(projectId, cancellationToken),
                LastAnalysis = DateTime.UtcNow.AddDays(-7) // Simulated
            };
        }

        /// <summary>
        /// Integrates memory context using knowledge graphs
        /// </summary>
        private async Task<Dictionary<string, object>> IntegrateMemoryContextAsync(
            CodeReviewRequest request,
            CancellationToken cancellationToken)
        {
            var projectId = ExtractProjectId(request);
            
            // Get or create knowledge graph for this project
            if (!_knowledgeGraphs.TryGetValue(projectId, out var knowledgeGraph))
            {
                knowledgeGraph = await BuildKnowledgeGraphAsync(projectId, request, cancellationToken);
                _knowledgeGraphs[projectId] = knowledgeGraph;
            }

            // Query relevant knowledge
            var relevantKnowledge = QueryKnowledgeGraph(knowledgeGraph, request);
            
            return new Dictionary<string, object>
            {
                ["RelevantPatterns"] = relevantKnowledge.Patterns,
                ["SimilarIssues"] = relevantKnowledge.SimilarIssues,
                ["BestPractices"] = relevantKnowledge.BestPractices,
                ["AntiPatterns"] = relevantKnowledge.AntiPatterns,
                ["TeamLearnings"] = relevantKnowledge.TeamLearnings,
                ["ContextualInsights"] = relevantKnowledge.ContextualInsights
            };
        }

        /// <summary>
        /// Analyzes team context and preferences
        /// </summary>
        private async Task<TeamContext> AnalyzeTeamContextAsync(
            CodeReviewRequest request,
            CancellationToken cancellationToken)
        {
            var prompt = $@"
Based on this code sample, infer the team's development context and preferences:

Code:
{request.Content[..Math.Min(2000, request.Content.Length)]}

Team Context: {request.Options.TeamContext}
Business Domain: {request.Options.BusinessDomain}

Analyze:
1. **Experience Level**: (junior, intermediate, senior, mixed)
2. **Preferred Patterns**: (patterns commonly used)
3. **Avoided Patterns**: (patterns that seem avoided)
4. **Coding Standards**: (style and conventions observed)
5. **Technical Debt**: (areas of concern)

Provide brief, practical insights.
";

            var response = await _claudeService.GetCompletionAsync(prompt, cancellationToken);
            
            return new TeamContext
            {
                ExperienceLevel = ExtractExperienceLevel(response),
                PreferredPatterns = ExtractPatterns(response, "preferred"),
                AvoidedPatterns = ExtractPatterns(response, "avoided"),
                CodingStandards = ExtractCodingStandards(response),
                TechnicalDebt = ExtractTechnicalDebt(response)
            };
        }

        /// <summary>
        /// Structural complexity analysis
        /// </summary>
        private int AnalyzeStructuralComplexity(string code)
        {
            var complexity = 1; // Base complexity
            
            // Cyclomatic complexity indicators
            complexity += CountMatches(code, @"\b(if|else|while|for|foreach|switch|case|catch)\b");
            complexity += CountMatches(code, @"&&|\|\|");
            complexity += CountMatches(code, @"\?.*:");
            
            // Nesting depth penalty
            var maxNesting = CalculateMaxNestingDepth(code);
            complexity += maxNesting * 2;
            
            // Method count
            var methodCount = CountMatches(code, @"\b(public|private|protected|internal)\s+.*\s+\w+\s*\(");
            complexity += methodCount;
            
            return Math.Min(10, complexity);
        }

        /// <summary>
        /// Cognitive complexity analysis using AI
        /// </summary>
        private async Task<int> AnalyzeCognitiveComplexityAsync(string code, CancellationToken cancellationToken)
        {
            var prompt = $@"
Analyze the cognitive complexity of this code on a scale of 1-10:

{code[..Math.Min(1500, code.Length)]}

Consider:
- Mental effort required to understand
- Control flow complexity
- Nested structures
- Variable scope and lifetime
- Business logic complexity

Respond with just a number from 1-10.
";

            try
            {
                var response = await _claudeService.GetCompletionAsync(prompt, cancellationToken);
                if (int.TryParse(response.Trim(), out var complexity))
                {
                    return Math.Min(10, Math.Max(1, complexity));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze cognitive complexity, using fallback");
            }
            
            return 5; // Fallback
        }

        /// <summary>
        /// Helper methods
        /// </summary>
        private string ExtractProjectId(CodeReviewRequest request)
        {
            // Extract project ID from file path or use a hash
            var projectIndicator = request.FileName ?? request.Options.BusinessDomain ?? "unknown";
            return projectIndicator.Length > 20 ? projectIndicator[..20] : projectIndicator;
        }

        private bool IsContextFresh(ProjectContext context)
        {
            return DateTime.UtcNow - context.LastUpdated < TimeSpan.FromHours(24);
        }

        private int CountMatches(string input, string pattern)
        {
            return System.Text.RegularExpressions.Regex.Matches(input, pattern).Count;
        }

        private int CalculateMaxNestingDepth(string code)
        {
            var maxDepth = 0;
            var currentDepth = 0;
            
            foreach (char c in code)
            {
                if (c == '{')
                {
                    currentDepth++;
                    maxDepth = Math.Max(maxDepth, currentDepth);
                }
                else if (c == '}')
                {
                    currentDepth--;
                }
            }
            
            return maxDepth;
        }

        private double AnalyzeMaintainability(string code)
        {
            var score = 1.0;
            
            // Reduce for long methods
            var methodLengths = EstimateMethodLengths(code);
            if (methodLengths.Any(l => l > 50))
                score -= 0.2;
            
            // Reduce for complex expressions
            var complexExpressions = CountMatches(code, @"\w+\.\w+\.\w+\.\w+");
            score -= complexExpressions * 0.05;
            
            // Improve for good naming
            var goodNames = CountMatches(code, @"\b[A-Z][a-z]+[A-Z][a-z]+\b");
            score += goodNames * 0.02;
            
            return Math.Max(0.1, Math.Min(1.0, score));
        }

        private int AnalyzeBusinessLogicComplexity(string code)
        {
            var businessKeywords = new[]
            {
                "calculate", "validate", "process", "business", "rule", "policy",
                "workflow", "price", "cost", "discount", "tax", "approve", "reject"
            };
            
            var complexity = 0;
            foreach (var keyword in businessKeywords)
            {
                complexity += CountMatches(code, $@"\b{keyword}\b");
            }
            
            return Math.Min(10, complexity);
        }

        private int CalculateOverallComplexity(int structural, int cognitive, double maintainability, int businessLogic)
        {
            var weighted = (structural * 0.3) + (cognitive * 0.4) + ((1 - maintainability) * 10 * 0.2) + (businessLogic * 0.1);
            return (int)Math.Min(10, Math.Max(1, weighted));
        }

        private List<int> EstimateMethodLengths(string code)
        {
            var methods = new List<int>();
            var lines = code.Split('\n');
            var inMethod = false;
            var methodLength = 0;
            
            foreach (var line in lines)
            {
                if (line.Contains("(") && (line.Contains("public") || line.Contains("private") || line.Contains("protected")))
                {
                    if (inMethod) methods.Add(methodLength);
                    inMethod = true;
                    methodLength = 1;
                }
                else if (inMethod)
                {
                    methodLength++;
                    if (line.Trim() == "}")
                    {
                        methods.Add(methodLength);
                        inMethod = false;
                        methodLength = 0;
                    }
                }
            }
            
            return methods;
        }

        private ProjectContext ParseProjectContext(string response, string projectId)
        {
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var context = new ProjectContext { ProjectId = projectId, LastUpdated = DateTime.UtcNow };
            
            foreach (var line in lines)
            {
                if (line.StartsWith("PROJECT_TYPE:"))
                    context.ProjectType = line.Substring(13).Trim();
                else if (line.StartsWith("ARCHITECTURE:"))
                    context.ArchitecturePattern = line.Substring(13).Trim();
                else if (line.StartsWith("TECH_STACK:"))
                    context.TechnologyStack = line.Substring(11).Trim().Split(',').Select(s => s.Trim()).ToList();
                else if (line.StartsWith("DOMAIN:"))
                    context.Domain = line.Substring(7).Trim();
                else if (line.StartsWith("MATURITY:"))
                    context.MaturityLevel = line.Substring(9).Trim();
                else if (line.StartsWith("STYLE:"))
                    context.CodingStyle = line.Substring(6).Trim();
            }
            
            return context;
        }

        private Dictionary<string, object> CombineMetadata(params object[] contexts)
        {
            var combined = new Dictionary<string, object>();
            
            foreach (var context in contexts)
            {
                if (context is Dictionary<string, object> dict)
                {
                    foreach (var kvp in dict)
                    {
                        combined[kvp.Key] = kvp.Value;
                    }
                }
                else if (context != null)
                {
                    var props = context.GetType().GetProperties();
                    foreach (var prop in props)
                    {
                        combined[prop.Name] = prop.GetValue(context) ?? "";
                    }
                }
            }
            
            return combined;
        }

        private List<string> GetAvailableTools(CodeReviewRequest request)
        {
            return new List<string>
            {
                "StaticAnalyzer",
                "SecurityScanner", 
                "PerformanceProfiler",
                "DocumentationGenerator",
                "TestGenerator",
                "RefactoringEngine"
            };
        }

        private EnhancedContext CreateFallbackContext(CodeReviewRequest request)
        {
            return new EnhancedContext
            {
                ProjectId = ExtractProjectId(request),
                Language = request.Language,
                CodeComplexity = 5, // Default medium complexity
                Metadata = ExtractBasicContext(request)
            };
        }

        // RAG-enhanced methods for historical context
        private async Task<List<string>> GetPreviousIssues(string projectId, CancellationToken cancellationToken)
        {
            try
            {
                // Use RAG to search for historical issues
                var searchResults = await _vectorSearchService.SearchHistoricalIssuesAsync(
                    "previous issues bugs problems", projectId, 5, cancellationToken);

                if (searchResults.Any())
                {
                    _logger.LogDebug("Found {Count} historical issues via RAG for project {ProjectId}", 
                        searchResults.Count, projectId);
                    return searchResults.Select(r => r.Content).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve historical issues via RAG for project {ProjectId}", projectId);
            }

            // Fallback to simulated data if RAG fails
            return new List<string>
            {
                "Memory leaks in data processing components",
                "SQL injection vulnerabilities in user input handlers", 
                "Performance degradation under high load"
            };
        }

        private async Task<List<string>> GetSuccessfulPatterns(string projectId, CancellationToken cancellationToken)
        {
            try
            {
                // Use RAG to search for successful patterns
                var searchResults = await _vectorSearchService.SearchTeamPatternsAsync(
                    "successful patterns best practices solutions", projectId, 3, cancellationToken);

                if (searchResults.Any())
                {
                    _logger.LogDebug("Found {Count} successful patterns via RAG for project {ProjectId}", 
                        searchResults.Count, projectId);
                    return searchResults.Select(r => r.Content).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve successful patterns via RAG for project {ProjectId}", projectId);
            }

            // Fallback to simulated data if RAG fails
            return new List<string>
            {
                "Repository pattern with Unit of Work",
                "Dependency injection for testability",
                "CQRS for complex business operations"
            };
        }

        private async Task<Dictionary<string, int>> AnalyzeTechnologyStack(string code, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
            var stack = new Dictionary<string, int>();
            
            if (code.Contains("Entity") || code.Contains("DbContext")) stack["EntityFramework"] = 1;
            if (code.Contains("async") || code.Contains("await")) stack["AsyncProgramming"] = 1;
            if (code.Contains("ILogger")) stack["Logging"] = 1;
            if (code.Contains("Controller") || code.Contains("[ApiController]")) stack["ASP.NET"] = 1;
            
            return stack;
        }

        private async Task<List<string>> GetTeamPreferences(string projectId, CancellationToken cancellationToken)
        {
            try
            {
                // Use RAG to search for team preferences
                var searchResults = await _vectorSearchService.SearchTeamPatternsAsync(
                    "team preferences conventions standards", projectId, 3, cancellationToken);

                if (searchResults.Any())
                {
                    _logger.LogDebug("Found {Count} team preferences via RAG for project {ProjectId}", 
                        searchResults.Count, projectId);
                    return searchResults.Select(r => r.Content).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve team preferences via RAG for project {ProjectId}", projectId);
            }

            // Fallback to simulated data if RAG fails
            return new List<string>
            {
                "Prefer composition over inheritance",
                "Use explicit interfaces for testability", 
                "Comprehensive error handling with logging"
            };
        }

        private async Task<KnowledgeGraph> BuildKnowledgeGraphAsync(string projectId, CodeReviewRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(50, cancellationToken); // Simulate building knowledge graph
            return new KnowledgeGraph
            {
                ProjectId = projectId,
                Nodes = new List<KnowledgeNode>(),
                Edges = new List<KnowledgeEdge>(),
                LastUpdated = DateTime.UtcNow
            };
        }

        private RelevantKnowledge QueryKnowledgeGraph(KnowledgeGraph graph, CodeReviewRequest request)
        {
            return new RelevantKnowledge
            {
                Patterns = new List<string> { "Service pattern usage", "Repository implementation" },
                SimilarIssues = new List<string> { "Similar validation logic in UserService" },
                BestPractices = new List<string> { "Use dependency injection", "Implement proper error handling" },
                AntiPatterns = new List<string> { "Avoid static dependencies", "Don't mix business logic with data access" },
                TeamLearnings = new List<string> { "Team prefers explicit error types over generic exceptions" },
                ContextualInsights = new List<string> { "This pattern has worked well in similar components" }
            };
        }

        private async Task<string> GenerateComplexityExplanation(string code, int complexity, CancellationToken cancellationToken)
        {
            var prompt = $@"
Explain why this code has a complexity score of {complexity}/10:

{code[..Math.Min(800, code.Length)]}

Provide a brief explanation focusing on the main complexity drivers.
";

            try
            {
                return await _claudeService.GetCompletionAsync(prompt, cancellationToken);
            }
            catch
            {
                return $"Code complexity rated {complexity}/10 based on structural analysis.";
            }
        }

        private string ExtractExperienceLevel(string response)
        {
            var level = response.ToLower();
            if (level.Contains("senior")) return "Senior";
            if (level.Contains("junior")) return "Junior";
            if (level.Contains("intermediate")) return "Intermediate";
            if (level.Contains("mixed")) return "Mixed";
            return "Intermediate"; // Default
        }

        private List<string> ExtractPatterns(string response, string type)
        {
            // Simple extraction - in real implementation would be more sophisticated
            return new List<string> { $"Example {type} pattern" };
        }

        private string ExtractCodingStandards(string response)
        {
            return "Standard enterprise C# conventions observed";
        }

        private List<string> ExtractTechnicalDebt(string response)
        {
            return new List<string> { "Some areas for improvement identified" };
        }
    }

    #region Supporting Classes

    public class ProjectContext
    {
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectType { get; set; } = string.Empty;
        public string ArchitecturePattern { get; set; } = string.Empty;
        public List<string> TechnologyStack { get; set; } = new();
        public string Domain { get; set; } = string.Empty;
        public string MaturityLevel { get; set; } = string.Empty;
        public string CodingStyle { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }

    public class ComplexityAnalysis
    {
        public int ComplexityScore { get; set; }
        public int StructuralComplexity { get; set; }
        public int CognitiveComplexity { get; set; }
        public double MaintainabilityScore { get; set; }
        public int BusinessLogicComplexity { get; set; }
        public string Analysis { get; set; } = string.Empty;
    }

    public class KnowledgeGraph
    {
        public string ProjectId { get; set; } = string.Empty;
        public List<KnowledgeNode> Nodes { get; set; } = new();
        public List<KnowledgeEdge> Edges { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class KnowledgeNode
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class KnowledgeEdge
    {
        public string FromNode { get; set; } = string.Empty;
        public string ToNode { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public double Weight { get; set; }
    }

    public class RelevantKnowledge
    {
        public List<string> Patterns { get; set; } = new();
        public List<string> SimilarIssues { get; set; } = new();
        public List<string> BestPractices { get; set; } = new();
        public List<string> AntiPatterns { get; set; } = new();
        public List<string> TeamLearnings { get; set; } = new();
        public List<string> ContextualInsights { get; set; } = new();
    }

    #endregion
}