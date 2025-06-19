using Mcp.CodeReview.Models;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.RAG;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Mcp.CodeReview.Services;

/// <summary>
/// Service for building comprehensive repository context for contextual code analysis
/// </summary>
public class RepositoryContextService
{
    private readonly GitLabService _gitLabService;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly ILogger<RepositoryContextService> _logger;
    private readonly HttpClient _httpClient;

    public RepositoryContextService(
        GitLabService gitLabService,
        IVectorSearchService vectorSearchService,
        ILogger<RepositoryContextService> logger,
        HttpClient httpClient)
    {
        _gitLabService = gitLabService;
        _vectorSearchService = vectorSearchService;
        _logger = logger;
        _httpClient = httpClient;
        
        // Configure HttpClient for GitLab API
        var gitlabHost = Environment.GetEnvironmentVariable("GITLAB_HOST") ?? "http://localhost:8080";
        var gitlabToken = Environment.GetEnvironmentVariable("GITLAB_TOKEN");
        
        _httpClient.BaseAddress = new Uri(gitlabHost);
        if (!string.IsNullOrEmpty(gitlabToken))
        {
            _httpClient.DefaultRequestHeaders.Add("PRIVATE-TOKEN", gitlabToken);
        }
    }

    /// <summary>
    /// Build comprehensive repository context including structure, dependencies, and historical patterns with RAG enhancement
    /// </summary>
    public async Task<RepositoryContext> BuildRepositoryContextAsync(
        string projectId, 
        List<string>? changedFiles = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building RAG-enhanced repository context for project {ProjectId}", projectId);
        
        var context = new RepositoryContext
        {
            ProjectId = projectId
        };

        try
        {
            // Step 1: Get project information
            await PopulateProjectInfoAsync(context, cancellationToken);
            
            // Step 2: Analyze project structure
            await AnalyzeProjectStructureAsync(context, cancellationToken);
            
            // Step 3: Get dependencies
            await AnalyzeDependenciesAsync(context, cancellationToken);
            
            // Step 4: Get related files if we have changed files
            if (changedFiles?.Any() == true)
            {
                await GetRelatedFilesAsync(context, changedFiles, cancellationToken);
            }
            
            // Step 5: RAG-Enhanced Historical Pattern Analysis
            await GetRAGEnhancedHistoricalPatternsAsync(context, changedFiles, cancellationToken);
            
            // Step 6: RAG-Enhanced Team Patterns and Standards
            await GetRAGEnhancedTeamPatternsAsync(context, cancellationToken);
            
            // Step 7: Semantic Code Analysis with RAG
            await PerformSemanticCodeAnalysisAsync(context, changedFiles, cancellationToken);
            
            // Step 8: Dynamic Knowledge Base Seeding
            await SeedKnowledgeBaseFromContextAsync(context, cancellationToken);
            
            _logger.LogInformation("RAG-enhanced repository context built successfully for project {ProjectId} with {PatternCount} patterns and {StandardCount} standards", 
                projectId, context.HistoricalPatterns.Count, context.ProjectStandards.Count);
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build RAG-enhanced repository context for project {ProjectId}", projectId);
            throw;
        }
    }

    private async Task PopulateProjectInfoAsync(RepositoryContext context, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/v4/projects/{context.ProjectId}", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var project = JsonSerializer.Deserialize<JsonElement>(content);
                
                context.ProjectName = project.GetProperty("name").GetString() ?? "";
                context.DefaultBranch = project.GetProperty("default_branch").GetString() ?? "main";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get project info for {ProjectId}", context.ProjectId);
        }
    }

    private async Task AnalyzeProjectStructureAsync(RepositoryContext context, CancellationToken cancellationToken)
    {
        try
        {
            // Get project tree (recursive)
            var response = await _httpClient.GetAsync(
                $"/api/v4/projects/{context.ProjectId}/repository/tree?recursive=true&per_page=1000", 
                cancellationToken);
                
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Could not get project tree for {ProjectId}", context.ProjectId);
                return;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var files = JsonSerializer.Deserialize<JsonElement[]>(content);

            if (files == null) return;

            // Analyze file structure
            foreach (var file in files)
            {
                var path = file.GetProperty("path").GetString() ?? "";
                var type = file.GetProperty("type").GetString() ?? "";
                
                if (type == "blob") // It's a file
                {
                    CategorizeFile(context.Structure, path);
                }
            }

            // Detect architecture pattern
            context.Structure.ArchitecturePattern = DetectArchitecturePattern(context.Structure);
            
            _logger.LogInformation("Analyzed {FileCount} files in project structure", 
                context.Structure.FilesByType.Values.Sum(list => list.Count));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze project structure for {ProjectId}", context.ProjectId);
        }
    }

    private void CategorizeFile(ProjectStructure structure, string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var directory = Path.GetDirectoryName(filePath) ?? "";
        
        // Categorize by file type
        var category = extension switch
        {
            ".cs" => "csharp",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".py" => "python",
            ".java" => "java",
            ".cpp" or ".cc" or ".cxx" => "cpp",
            ".h" or ".hpp" => "headers",
            ".json" => "config",
            ".yml" or ".yaml" => "config",
            ".xml" => "config",
            ".md" => "documentation",
            ".txt" => "documentation",
            ".sql" => "database",
            _ => "other"
        };

        if (!structure.FilesByType.ContainsKey(category))
            structure.FilesByType[category] = new List<string>();
        
        structure.FilesByType[category].Add(filePath);

        // Categorize by directory purpose
        var lowerDir = directory.ToLowerInvariant();
        if (lowerDir.Contains("test") || lowerDir.Contains("spec"))
        {
            structure.TestDirectories.Add(directory);
        }
        else if (lowerDir.Contains("src") || lowerDir.Contains("source"))
        {
            structure.SourceDirectories.Add(directory);
        }
        
        // Special files
        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        if (fileName.Contains("config") || fileName.Contains("setting") || 
            fileName.EndsWith(".json") || fileName.EndsWith(".yml"))
        {
            structure.ConfigurationFiles.Add(filePath);
        }
        
        if (fileName.Contains("readme") || fileName.Contains("doc") || extension == ".md")
        {
            structure.DocumentationFiles.Add(filePath);
        }
    }

    private string DetectArchitecturePattern(ProjectStructure structure)
    {
        var directories = structure.SourceDirectories.Concat(structure.TestDirectories).ToList();
        var allFiles = structure.FilesByType.Values.SelectMany(files => files).ToList();
        
        // Simple pattern detection based on directory structure and files
        if (directories.Any(d => d.Contains("Controllers")) && 
            directories.Any(d => d.Contains("Models")) && 
            directories.Any(d => d.Contains("Views")))
        {
            return "MVC";
        }
        
        if (allFiles.Any(f => f.Contains("Dockerfile")) || 
            allFiles.Any(f => f.Contains("docker-compose")))
        {
            return "Microservices/Containerized";
        }
        
        if (directories.Any(d => d.Contains("Services")) && 
            directories.Any(d => d.Contains("Repositories")))
        {
            return "Service Layer";
        }
        
        if (directories.Any(d => d.Contains("Features")) || 
            directories.Any(d => d.Contains("Slices")))
        {
            return "Vertical Slice";
        }
        
        return "Layered";
    }

    private async Task AnalyzeDependenciesAsync(RepositoryContext context, CancellationToken cancellationToken)
    {
        var dependencyFiles = new[] 
        { 
            "package.json", "package-lock.json", // Node.js
            "requirements.txt", "Pipfile", // Python
            "pom.xml", "build.gradle", // Java
            "Cargo.toml", // Rust
            "go.mod", // Go
            "composer.json" // PHP
        };

        // For .NET projects, look for .csproj, .sln files
        var csharpFiles = context.Structure.FilesByType.GetValueOrDefault("config", new List<string>())
            .Where(f => f.EndsWith(".csproj") || f.EndsWith(".sln"))
            .ToList();

        foreach (var file in csharpFiles)
        {
            await AnalyzeCSharpDependencies(context, file, cancellationToken);
        }

        // Analyze other dependency files
        foreach (var depFile in dependencyFiles)
        {
            await AnalyzeDependencyFile(context, depFile, cancellationToken);
        }
    }

    private async Task AnalyzeCSharpDependencies(RepositoryContext context, string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/v4/projects/{context.ProjectId}/repository/files/{Uri.EscapeDataString(filePath)}/raw?ref={context.DefaultBranch}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                
                // Parse PackageReference elements from .csproj
                var packageRegex = new Regex(@"<PackageReference\s+Include=""([^""]+)""\s+Version=""([^""]+)""", RegexOptions.IgnoreCase);
                var matches = packageRegex.Matches(content);
                
                foreach (Match match in matches)
                {
                    context.Dependencies[match.Groups[1].Value] = match.Groups[2].Value;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not analyze .NET dependencies in {FilePath}", filePath);
        }
    }

    private async Task AnalyzeDependencyFile(RepositoryContext context, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/v4/projects/{context.ProjectId}/repository/files/{fileName}/raw?ref={context.DefaultBranch}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                
                if (fileName == "package.json")
                {
                    var package = JsonSerializer.Deserialize<JsonElement>(content);
                    if (package.TryGetProperty("dependencies", out var deps))
                    {
                        foreach (var dep in deps.EnumerateObject())
                        {
                            context.Dependencies[dep.Name] = dep.Value.GetString() ?? "";
                        }
                    }
                }
                // Add parsing for other dependency file formats as needed
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not analyze dependency file {FileName}", fileName);
        }
    }

    private async Task GetRelatedFilesAsync(RepositoryContext context, List<string> changedFiles, CancellationToken cancellationToken)
    {
        foreach (var changedFile in changedFiles)
        {
            try
            {
                // Get file content to analyze imports/dependencies
                var response = await _httpClient.GetAsync(
                    $"/api/v4/projects/{context.ProjectId}/repository/files/{Uri.EscapeDataString(changedFile)}/raw?ref={context.DefaultBranch}",
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var relatedFiles = ExtractRelatedFiles(content, changedFile);
                    
                    // Fetch related file contents
                    foreach (var relatedFile in relatedFiles)
                    {
                        await TryGetRelatedFileContent(context, relatedFile, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not analyze related files for {ChangedFile}", changedFile);
            }
        }
    }

    private List<string> ExtractRelatedFiles(string content, string filePath)
    {
        var relatedFiles = new List<string>();
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        
        // Extract imports based on file type
        switch (extension)
        {
            case ".cs":
                // C# using statements and relative file references
                var usingRegex = new Regex(@"using\s+([^;]+);");
                var matches = usingRegex.Matches(content);
                foreach (Match match in matches)
                {
                    var ns = match.Groups[1].Value.Trim();
                    // Convert namespace to potential file path
                    var potentialPath = ns.Replace(".", "/") + ".cs";
                    relatedFiles.Add(potentialPath);
                }
                break;
                
            case ".js":
            case ".ts":
                // JavaScript/TypeScript imports
                var importRegex = new Regex(@"import.*from\s+['""]([^'""]+)['""]");
                var jsMatches = importRegex.Matches(content);
                foreach (Match match in jsMatches)
                {
                    relatedFiles.Add(match.Groups[1].Value);
                }
                break;
        }
        
        return relatedFiles.Take(10).ToList(); // Limit to prevent excessive API calls
    }

    private async Task TryGetRelatedFileContent(RepositoryContext context, string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/v4/projects/{context.ProjectId}/repository/files/{Uri.EscapeDataString(filePath)}/raw?ref={context.DefaultBranch}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                context.RelatedFiles[filePath] = content;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not fetch related file {FilePath}", filePath);
        }
    }

    /// <summary>
    /// RAG-Enhanced historical pattern analysis with semantic search and context-aware retrieval
    /// </summary>
    private async Task GetRAGEnhancedHistoricalPatternsAsync(RepositoryContext context, List<string>? changedFiles, CancellationToken cancellationToken)
    {
        if (_vectorSearchService == null)
        {
            _logger.LogWarning("Vector search service not available for RAG-enhanced historical pattern analysis");
            return;
        }

        try
        {
            var historicalPatterns = new List<HistoricalPattern>();
            
            // 1. Architecture-specific pattern search
            var architectureQuery = $"architecture pattern {context.Structure.ArchitecturePattern} best practices common issues";
            var architecturePatterns = await _vectorSearchService.SearchSimilarCodeAsync(architectureQuery, context.ProjectId, topK: 15);
            
            // 2. Technology stack pattern search
            var techStackQuery = $"technology stack {string.Join(" ", context.Dependencies.Keys.Take(5))} patterns anti-patterns";
            var techPatterns = await _vectorSearchService.SearchSimilarCodeAsync(techStackQuery, context.ProjectId, topK: 10);
            
            // 3. File-specific pattern search for changed files
            if (changedFiles?.Any() == true)
            {
                foreach (var file in changedFiles.Take(3)) // Limit to prevent excessive queries
                {
                    var fileExtension = Path.GetExtension(file);
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var fileQuery = $"file type {fileExtension} {fileName} common patterns issues security performance";
                    
                    var filePatterns = await _vectorSearchService.SearchSimilarCodeAsync(fileQuery, context.ProjectId, topK: 8);
                    architecturePatterns.AddRange(filePatterns);
                }
            }
            
            // 4. Code quality and security pattern search
            var qualityQuery = $"code quality security vulnerability {context.Structure.ArchitecturePattern} review findings";
            var qualityPatterns = await _vectorSearchService.SearchSimilarCodeAsync(qualityQuery, context.ProjectId, topK: 12);
            
            // 5. Performance pattern search
            var performanceQuery = $"performance optimization {context.Structure.ArchitecturePattern} bottlenecks scalability";
            var performancePatterns = await _vectorSearchService.SearchSimilarCodeAsync(performanceQuery, context.ProjectId, topK: 10);
            
            // Combine and deduplicate patterns
            var allPatterns = architecturePatterns
                .Concat(techPatterns)
                .Concat(qualityPatterns)
                .Concat(performancePatterns)
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .OrderByDescending(p => p.Similarity)
                .Take(25);
            
            // Convert to HistoricalPattern objects with enhanced metadata
            context.HistoricalPatterns = allPatterns.Select(p => new HistoricalPattern
            {
                Id = p.Id,
                Pattern = p.Content,
                Category = DeterminePatternCategory(p, context),
                Similarity = p.Similarity,
                Recommendation = GenerateContextualRecommendation(p, context),
                Metadata = p.Metadata
            }).ToList();
            
            _logger.LogInformation("RAG-enhanced historical pattern analysis found {PatternCount} relevant patterns across {CategoryCount} categories", 
                context.HistoricalPatterns.Count, 
                context.HistoricalPatterns.Select(p => p.Category).Distinct().Count());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve RAG-enhanced historical patterns for project {ProjectId}", context.ProjectId);
        }
    }

    /// <summary>
    /// RAG-Enhanced team patterns and coding standards with semantic understanding
    /// </summary>
    private async Task GetRAGEnhancedTeamPatternsAsync(RepositoryContext context, CancellationToken cancellationToken)
    {
        if (_vectorSearchService == null)
        {
            _logger.LogWarning("Vector search service not available for RAG-enhanced team pattern analysis");
            return;
        }

        try
        {
            // 1. Project-specific team patterns
            var projectPatterns = await _vectorSearchService.SearchTeamPatternsAsync(
                context.ProjectId, 
                context.ProjectId, 
                topK: 8);

            // 2. Architecture-specific coding standards
            var archStandardsQuery = $"{context.Structure.ArchitecturePattern} coding standards best practices guidelines";
            var archStandards = await _vectorSearchService.SearchCodingStandardsAsync(archStandardsQuery, topK: 15);

            // 3. Technology-specific standards
            var mainTechnologies = context.Dependencies.Keys.Take(3);
            var techStandards = new List<RetrievedContext>();
            foreach (var tech in mainTechnologies)
            {
                var techQuery = $"{tech} coding standards conventions best practices";
                var standards = await _vectorSearchService.SearchCodingStandardsAsync(techQuery, topK: 5);
                techStandards.AddRange(standards);
            }

            // 4. Security and performance standards
            var securityStandards = await _vectorSearchService.SearchCodingStandardsAsync("security standards OWASP vulnerabilities", topK: 8);
            var performanceStandards = await _vectorSearchService.SearchCodingStandardsAsync("performance standards optimization scalability", topK: 6);

            // Combine all standards
            var allStandards = archStandards
                .Concat(techStandards)
                .Concat(securityStandards)
                .Concat(performanceStandards)
                .GroupBy(s => s.Id)
                .Select(g => g.First())
                .OrderByDescending(s => s.Similarity)
                .ToList();

            // Build team patterns
            context.TeamPatterns = new TeamPatterns
            {
                PreferredPatterns = projectPatterns
                    .Where(p => p.Metadata.GetValueOrDefault("type")?.ToString() == "preferred")
                    .Select(p => p.Content)
                    .ToList(),
                AvoidedPatterns = projectPatterns
                    .Where(p => p.Metadata.GetValueOrDefault("type")?.ToString() == "avoided")
                    .Select(p => p.Content)
                    .ToList(),
                EmergingPatterns = IdentifyEmergingPatterns(context, allStandards),
                ContextualGuidelines = GenerateContextualGuidelines(context, allStandards)
            };

            // Build enhanced coding standards
            context.ProjectStandards = allStandards.Select(cs => new CodingStandard
            {
                Id = cs.Id,
                Title = cs.Metadata.GetValueOrDefault("title")?.ToString() ?? ExtractTitle(cs.Content),
                Description = cs.Content,
                Language = DetermineLanguage(cs, context),
                Category = cs.Metadata.GetValueOrDefault("category")?.ToString() ?? CategorizeStandard(cs.Content),
                Priority = CalculatePriority(cs, context),
                Applicability = DetermineApplicability(cs, context),
                Examples = ExtractExamples(cs.Content),
                Rationale = cs.Metadata.GetValueOrDefault("rationale")?.ToString() ?? ""
            }).ToList();
            
            _logger.LogInformation("RAG-enhanced team pattern analysis found {PreferredCount} preferred patterns, {AvoidedCount} anti-patterns, and {StandardsCount} coding standards", 
                context.TeamPatterns.PreferredPatterns.Count,
                context.TeamPatterns.AvoidedPatterns.Count,
                context.ProjectStandards.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve RAG-enhanced team patterns for project {ProjectId}", context.ProjectId);
        }
    }

    /// <summary>
    /// Perform semantic code analysis to understand code patterns and relationships
    /// </summary>
    private async Task PerformSemanticCodeAnalysisAsync(RepositoryContext context, List<string>? changedFiles, CancellationToken cancellationToken)
    {
        if (_vectorSearchService == null) return;

        try
        {
            var semanticInsights = new Dictionary<string, object>();

            // 1. Analyze code complexity and patterns
            if (changedFiles?.Any() == true)
            {
                foreach (var file in changedFiles.Take(3))
                {
                    var fileContent = context.RelatedFiles.GetValueOrDefault(file, "");
                    if (!string.IsNullOrEmpty(fileContent))
                    {
                        // Search for similar code patterns
                        var similarCodeQuery = $"similar code patterns {Path.GetExtension(file)} {context.Structure.ArchitecturePattern}";
                        var similarPatterns = await _vectorSearchService.SearchSimilarCodeAsync(similarCodeQuery, context.ProjectId, topK: 5);
                        
                        semanticInsights[file] = new
                        {
                            SimilarPatterns = similarPatterns.Count,
                            ComplexityIndicators = AnalyzeComplexity(fileContent),
                            ArchitecturalRole = DetermineArchitecturalRole(file, context),
                            RiskFactors = IdentifyRiskFactors(fileContent, similarPatterns)
                        };
                    }
                }
            }

            // 2. Cross-reference analysis
            semanticInsights["cross_references"] = AnalyzeCrossReferences(context);
            
            // 3. Dependency risk analysis
            semanticInsights["dependency_risks"] = await AnalyzeDependencyRisks(context, cancellationToken);

            context.SemanticAnalysis = semanticInsights;
            
            _logger.LogInformation("Semantic code analysis completed for {FileCount} files with {InsightCount} insights", 
                changedFiles?.Count ?? 0, semanticInsights.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not perform semantic code analysis for project {ProjectId}", context.ProjectId);
        }
    }

    /// <summary>
    /// Dynamically seed knowledge base with discovered patterns and insights
    /// </summary>
    private async Task SeedKnowledgeBaseFromContextAsync(RepositoryContext context, CancellationToken cancellationToken)
    {
        if (_vectorSearchService == null) return;

        try
        {
            // 1. Extract new patterns from current analysis
            var newPatterns = ExtractDiscoveredPatterns(context);
            
            // 2. Build comprehensive architecture insights
            var architectureInsights = BuildArchitectureInsights(context);
            
            // 3. Use the new dynamic seeding functionality
            await _vectorSearchService.SeedKnowledgeBaseAsync(
                context.ProjectId,
                newPatterns,
                context.ProjectStandards,
                architectureInsights,
                cancellationToken);
            
            _logger.LogInformation("Knowledge base seeded with {PatternCount} new patterns and {StandardCount} standards from project analysis", 
                newPatterns.Count, context.ProjectStandards.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not seed knowledge base for project {ProjectId}", context.ProjectId);
        }
    }

    // Helper methods for RAG-enhanced analysis

    private string DeterminePatternCategory(RetrievedContext pattern, RepositoryContext context)
    {
        var content = pattern.Content.ToLowerInvariant();
        
        if (content.Contains("security") || content.Contains("vulnerability"))
            return "Security";
        if (content.Contains("performance") || content.Contains("optimization"))
            return "Performance";
        if (content.Contains("architecture") || content.Contains("design"))
            return "Architecture";
        if (content.Contains("quality") || content.Contains("maintainability"))
            return "Code Quality";
        
        return pattern.Metadata.GetValueOrDefault("category")?.ToString() ?? "General";
    }

    private string GenerateContextualRecommendation(RetrievedContext pattern, RepositoryContext context)
    {
        var baseRecommendation = pattern.Metadata.GetValueOrDefault("recommendation")?.ToString() ?? 
                                "Review this pattern in the context of your current architecture";
        
        // Enhance with project-specific context
        if (context.Structure.ArchitecturePattern == "Microservices")
            return $"{baseRecommendation} Consider microservice boundaries and distributed system implications.";
        if (context.Structure.ArchitecturePattern == "MVC")
            return $"{baseRecommendation} Ensure proper separation of concerns across MVC layers.";
        
        return baseRecommendation;
    }

    private List<string> IdentifyEmergingPatterns(RepositoryContext context, List<RetrievedContext> standards)
    {
        // Analyze current codebase trends
        var emergingPatterns = new List<string>();
        
        // Check for modern patterns based on dependencies
        if (context.Dependencies.ContainsKey("Microsoft.AspNetCore"))
            emergingPatterns.Add("ASP.NET Core minimal APIs trend");
        if (context.Dependencies.ContainsKey("Docker"))
            emergingPatterns.Add("Containerization adoption pattern");
        
        return emergingPatterns;
    }

    private Dictionary<string, string> GenerateContextualGuidelines(RepositoryContext context, List<RetrievedContext> standards)
    {
        var guidelines = new Dictionary<string, string>();
        
        guidelines[$"{context.Structure.ArchitecturePattern}_naming"] = $"Follow {context.Structure.ArchitecturePattern} naming conventions";
        guidelines["dependency_management"] = $"Manage {context.Dependencies.Count} dependencies carefully";
        guidelines["security_focus"] = "Prioritize security in all implementations";
        
        return guidelines;
    }

    private string ExtractTitle(string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines.FirstOrDefault()?.Trim() ?? "Untitled Standard";
    }

    private string DetermineLanguage(RetrievedContext standard, RepositoryContext context)
    {
        var content = standard.Content.ToLowerInvariant();
        
        if (content.Contains("c#") || content.Contains("csharp"))
            return "C#";
        if (content.Contains("javascript") || content.Contains("typescript"))
            return "JavaScript/TypeScript";
        if (content.Contains("python"))
            return "Python";
        
        // Infer from project dependencies
        if (context.Dependencies.Keys.Any(d => d.Contains("Microsoft")))
            return "C#";
        
        return "General";
    }

    private string CategorizeStandard(string content)
    {
        var lowerContent = content.ToLowerInvariant();
        
        if (lowerContent.Contains("security") || lowerContent.Contains("authentication"))
            return "Security";
        if (lowerContent.Contains("performance") || lowerContent.Contains("optimization"))
            return "Performance";
        if (lowerContent.Contains("testing") || lowerContent.Contains("unit test"))
            return "Testing";
        if (lowerContent.Contains("naming") || lowerContent.Contains("convention"))
            return "Conventions";
        
        return "General";
    }

    private int CalculatePriority(RetrievedContext standard, RepositoryContext context)
    {
        // Higher priority for standards that match current architecture/dependencies
        var priority = 3; // Default medium priority
        
        if (standard.Content.Contains(context.Structure.ArchitecturePattern))
            priority = 1; // High priority
        
        if (context.Dependencies.Keys.Any(dep => standard.Content.Contains(dep)))
            priority = Math.Min(priority, 2); // High-medium priority
        
        return priority;
    }

    private string DetermineApplicability(RetrievedContext standard, RepositoryContext context)
    {
        var applicableScenarios = new List<string>();
        
        if (standard.Content.Contains(context.Structure.ArchitecturePattern))
            applicableScenarios.Add($"{context.Structure.ArchitecturePattern} projects");
        
        foreach (var dep in context.Dependencies.Keys.Take(3))
        {
            if (standard.Content.Contains(dep))
                applicableScenarios.Add($"Projects using {dep}");
        }
        
        return applicableScenarios.Any() ? string.Join(", ", applicableScenarios) : "General applicability";
    }

    private List<string> ExtractExamples(string content)
    {
        var examples = new List<string>();
        var lines = content.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.Trim().StartsWith("//") || line.Trim().StartsWith("/*") || 
                line.Contains("example") || line.Contains("Example"))
            {
                examples.Add(line.Trim());
            }
        }
        
        return examples.Take(3).ToList();
    }

    private object AnalyzeComplexity(string fileContent)
    {
        var lines = fileContent.Split('\n');
        var complexity = new
        {
            LineCount = lines.Length,
            MethodCount = lines.Count(l => l.Contains("public") || l.Contains("private")),
            ConditionalCount = lines.Count(l => l.Contains("if") || l.Contains("switch")),
            LoopCount = lines.Count(l => l.Contains("for") || l.Contains("while")),
            ComplexityScore = CalculateComplexityScore(lines)
        };
        
        return complexity;
    }

    private double CalculateComplexityScore(string[] lines)
    {
        var score = lines.Length * 0.1; // Base score from line count
        score += lines.Count(l => l.Contains("if")) * 2; // Conditionals add complexity
        score += lines.Count(l => l.Contains("for") || l.Contains("while")) * 3; // Loops add more
        return Math.Min(score, 100); // Cap at 100
    }

    private string DetermineArchitecturalRole(string filePath, RepositoryContext context)
    {
        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        var directory = Path.GetDirectoryName(filePath)?.ToLowerInvariant() ?? "";
        
        if (directory.Contains("controller") || fileName.Contains("controller"))
            return "Controller Layer";
        if (directory.Contains("service") || fileName.Contains("service"))
            return "Service Layer";
        if (directory.Contains("model") || fileName.Contains("model"))
            return "Data Layer";
        if (directory.Contains("view") || fileName.Contains("view"))
            return "Presentation Layer";
        
        return "Infrastructure";
    }

    private List<string> IdentifyRiskFactors(string fileContent, List<RetrievedContext> similarPatterns)
    {
        var risks = new List<string>();
        
        if (fileContent.Contains("password") || fileContent.Contains("secret"))
            risks.Add("Potential hardcoded credentials");
        if (fileContent.Contains("eval(") || fileContent.Contains("innerHTML"))
            risks.Add("Code injection vulnerability");
        if (fileContent.Length > 10000)
            risks.Add("Large file size - potential maintenance issues");
        
        // Add risks based on similar patterns
        foreach (var pattern in similarPatterns.Take(3))
        {
            if (pattern.Metadata.GetValueOrDefault("risk_level")?.ToString() == "high")
                risks.Add($"Similar to high-risk pattern: {pattern.Id}");
        }
        
        return risks;
    }

    private object AnalyzeCrossReferences(RepositoryContext context)
    {
        return new
        {
            TotalFiles = context.RelatedFiles.Count,
            Dependencies = context.Dependencies.Count,
            CrossReferences = context.RelatedFiles.Count * 0.3, // Estimate
            CouplingScore = CalculateCouplingScore(context)
        };
    }

    private double CalculateCouplingScore(RepositoryContext context)
    {
        // Simple coupling calculation based on files and dependencies
        var fileCount = context.Structure.FilesByType.Values.Sum(files => files.Count);
        var depCount = context.Dependencies.Count;
        
        return fileCount > 0 ? (double)depCount / fileCount : 0;
    }

    private async Task<object> AnalyzeDependencyRisks(RepositoryContext context, CancellationToken cancellationToken)
    {
        var risks = new Dictionary<string, string>();
        
        foreach (var dependency in context.Dependencies.Take(5))
        {
            // Simple risk assessment based on dependency name patterns
            if (dependency.Key.Contains("beta") || dependency.Key.Contains("alpha"))
                risks[dependency.Key] = "Pre-release version risk";
            else if (dependency.Value.Contains("0."))
                risks[dependency.Key] = "Early version risk";
            else
                risks[dependency.Key] = "Low risk";
        }
        
        return new { DependencyRisks = risks, TotalDependencies = context.Dependencies.Count };
    }

    private List<CodePattern> ExtractDiscoveredPatterns(RepositoryContext context)
    {
        var patterns = new List<CodePattern>();
        
        // Extract patterns from semantic analysis
        if (context.SemanticAnalysis.Any())
        {
            patterns.Add(new CodePattern
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"{context.Structure.ArchitecturePattern} Implementation Pattern",
                Pattern = $"Architecture: {context.Structure.ArchitecturePattern}",
                Context = context.ProjectName,
                Confidence = 0.8,
                Impact = "Architectural consistency",
                Recommendation = "Continue following established architecture patterns"
            });
        }
        
        return patterns;
    }

    private Dictionary<string, object> BuildArchitectureInsights(RepositoryContext context)
    {
        var insights = new Dictionary<string, object>();
        
        // 1. Architecture pattern insights
        insights["architecture_pattern"] = new
        {
            Pattern = context.Structure.ArchitecturePattern,
            FileDistribution = context.Structure.FilesByType,
            SourceDirectories = context.Structure.SourceDirectories.Count,
            TestDirectories = context.Structure.TestDirectories.Count,
            ConfigurationFiles = context.Structure.ConfigurationFiles.Count
        };
        
        // 2. Dependency insights
        insights["dependency_analysis"] = new
        {
            TotalDependencies = context.Dependencies.Count,
            MainTechnologies = context.Dependencies.Keys.Take(10).ToList(),
            DependencyComplexity = CalculateDependencyComplexity(context.Dependencies),
            SecurityRelevantDeps = context.Dependencies.Keys.Where(d => 
                d.ToLowerInvariant().Contains("security") || 
                d.ToLowerInvariant().Contains("auth")).ToList()
        };
        
        // 3. Code complexity insights
        if (context.SemanticAnalysis.Any())
        {
            insights["code_complexity"] = context.SemanticAnalysis;
        }
        
        // 4. Team patterns insights
        if (context.TeamPatterns != null)
        {
            insights["team_patterns"] = new
            {
                PreferredPatterns = context.TeamPatterns.PreferredPatterns,
                AvoidedPatterns = context.TeamPatterns.AvoidedPatterns,
                EmergingPatterns = context.TeamPatterns.EmergingPatterns,
                Guidelines = context.TeamPatterns.ContextualGuidelines
            };
        }
        
        // 5. Quality metrics
        insights["quality_metrics"] = new
        {
            ArchitectureConsistency = CalculateArchitectureConsistency(context),
            DependencyHealth = CalculateDependencyHealth(context),
            CodebaseMaturity = CalculateCodebaseMaturity(context),
            SecurityPosture = CalculateSecurityPosture(context)
        };
        
        return insights;
    }
    
    private double CalculateDependencyComplexity(Dictionary<string, string> dependencies)
    {
        // Calculate complexity based on number of dependencies and version patterns
        var baseScore = Math.Min(dependencies.Count * 0.5, 50);
        var preReleaseCount = dependencies.Values.Count(v => v.Contains("alpha") || v.Contains("beta"));
        var complexityBonus = preReleaseCount * 5;
        
        return Math.Min(baseScore + complexityBonus, 100);
    }
    
    private double CalculateArchitectureConsistency(RepositoryContext context)
    {
        // Simple consistency calculation based on directory structure alignment with architecture pattern
        var totalFiles = context.Structure.FilesByType.Values.Sum(files => files.Count);
        var sourceFiles = context.Structure.SourceDirectories.Count;
        var testFiles = context.Structure.TestDirectories.Count;
        
        // Higher score for balanced source/test ratio and organized structure
        if (totalFiles == 0) return 0;
        
        var testCoverage = testFiles > 0 ? Math.Min((double)testFiles / sourceFiles * 100, 100) : 0;
        var organizationScore = context.Structure.SourceDirectories.Count > 0 ? 75 : 25;
        
        return (testCoverage + organizationScore) / 2;
    }
    
    private double CalculateDependencyHealth(RepositoryContext context)
    {
        if (!context.Dependencies.Any()) return 100;
        
        var healthyDeps = context.Dependencies.Count(d => 
            !d.Value.Contains("alpha") && 
            !d.Value.Contains("beta") && 
            !d.Value.StartsWith("0."));
        
        return (double)healthyDeps / context.Dependencies.Count * 100;
    }
    
    private double CalculateCodebaseMaturity(RepositoryContext context)
    {
        var maturityScore = 0.0;
        
        // Documentation presence
        if (context.Structure.DocumentationFiles.Any()) maturityScore += 25;
        
        // Configuration management
        if (context.Structure.ConfigurationFiles.Any()) maturityScore += 20;
        
        // Test presence
        if (context.Structure.TestDirectories.Any()) maturityScore += 30;
        
        // Architecture pattern usage
        if (context.Structure.ArchitecturePattern != "Layered") maturityScore += 25; // Bonus for explicit patterns
        
        return maturityScore;
    }
    
    private double CalculateSecurityPosture(RepositoryContext context)
    {
        var securityScore = 50.0; // Base score
        
        // Security-related dependencies
        var securityDeps = context.Dependencies.Keys.Count(d => 
            d.ToLowerInvariant().Contains("security") || 
            d.ToLowerInvariant().Contains("auth") ||
            d.ToLowerInvariant().Contains("encrypt"));
        
        if (securityDeps > 0) securityScore += 25;
        
        // Configuration file security (presence of security configs)
        var securityConfigs = context.Structure.ConfigurationFiles.Count(f => 
            f.ToLowerInvariant().Contains("security") || 
            f.ToLowerInvariant().Contains("auth"));
        
        if (securityConfigs > 0) securityScore += 25;
        
        return Math.Min(securityScore, 100);
    }
}