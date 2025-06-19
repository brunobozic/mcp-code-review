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
    /// Build comprehensive repository context including structure, dependencies, and historical patterns
    /// </summary>
    public async Task<RepositoryContext> BuildRepositoryContextAsync(
        string projectId, 
        List<string>? changedFiles = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Building repository context for project {ProjectId}", projectId);
        
        var context = new RepositoryContext
        {
            ProjectId = projectId
        };

        try
        {
            // Get project information
            await PopulateProjectInfoAsync(context, cancellationToken);
            
            // Analyze project structure
            await AnalyzeProjectStructureAsync(context, cancellationToken);
            
            // Get dependencies
            await AnalyzeDependenciesAsync(context, cancellationToken);
            
            // Get related files if we have changed files
            if (changedFiles?.Any() == true)
            {
                await GetRelatedFilesAsync(context, changedFiles, cancellationToken);
            }
            
            // Query historical patterns from RAG
            await GetHistoricalPatternsAsync(context, cancellationToken);
            
            // Get team patterns and coding standards
            await GetTeamPatternsAsync(context, cancellationToken);
            
            _logger.LogInformation("Repository context built successfully for project {ProjectId}", projectId);
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build repository context for project {ProjectId}", projectId);
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

    private async Task GetHistoricalPatternsAsync(RepositoryContext context, CancellationToken cancellationToken)
    {
        try
        {
            // Search for similar patterns in historical data
            var patterns = await _vectorSearchService.SearchSimilarCodeAsync(
                $"project architecture {context.Structure.ArchitecturePattern}", 
                context.ProjectId, 
                topK: 10);

            context.HistoricalPatterns = patterns.Select(p => new HistoricalPattern
            {
                Id = p.Id,
                Pattern = p.Content,
                Category = p.Metadata.GetValueOrDefault("category")?.ToString() ?? "",
                Similarity = p.Similarity,
                Recommendation = p.Metadata.GetValueOrDefault("recommendation")?.ToString() ?? ""
            }).ToList();
            
            _logger.LogInformation("Found {PatternCount} historical patterns for project", patterns.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve historical patterns for project {ProjectId}", context.ProjectId);
        }
    }

    private async Task GetTeamPatternsAsync(RepositoryContext context, CancellationToken cancellationToken)
    {
        try
        {
            // Get team-specific patterns and coding standards
            var teamPatterns = await _vectorSearchService.SearchTeamPatternsAsync(
                context.ProjectId, 
                context.ProjectId, 
                topK: 5);

            var codingStandards = await _vectorSearchService.SearchCodingStandardsAsync(
                "general", 
                topK: 10);

            context.TeamPatterns = new TeamPatterns
            {
                PreferredPatterns = teamPatterns
                    .Where(p => p.Metadata.GetValueOrDefault("type")?.ToString() == "preferred")
                    .Select(p => p.Content)
                    .ToList(),
                AvoidedPatterns = teamPatterns
                    .Where(p => p.Metadata.GetValueOrDefault("type")?.ToString() == "avoided")
                    .Select(p => p.Content)
                    .ToList()
            };

            context.ProjectStandards = codingStandards.Select(cs => new CodingStandard
            {
                Id = cs.Id,
                Title = cs.Metadata.GetValueOrDefault("title")?.ToString() ?? "",
                Description = cs.Content,
                Language = cs.Metadata.GetValueOrDefault("language")?.ToString() ?? "",
                Category = cs.Metadata.GetValueOrDefault("category")?.ToString() ?? "",
                Priority = int.TryParse(cs.Metadata.GetValueOrDefault("priority")?.ToString(), out var p) ? p : 1
            }).ToList();
            
            _logger.LogInformation("Retrieved team patterns and coding standards for project");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not retrieve team patterns for project {ProjectId}", context.ProjectId);
        }
    }
}