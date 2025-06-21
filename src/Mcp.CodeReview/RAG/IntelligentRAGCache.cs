using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mcp.CodeReview.Models;
using Mcp.CodeReview.AI;

namespace Mcp.CodeReview.RAG;

/// <summary>
/// Intelligent RAG caching system that reduces redundant vector searches
/// through semantic similarity matching and smart cache key generation
/// </summary>
public class IntelligentRAGCache
{
    private readonly IMemoryCache _cache;
    private readonly IVectorSearchService _vectorService;
    private readonly ILogger<IntelligentRAGCache> _logger;
    private readonly RAGCacheConfig _config;

    public IntelligentRAGCache(
        IMemoryCache cache,
        IVectorSearchService vectorService,
        ILogger<IntelligentRAGCache> logger,
        RAGCacheConfig? config = null)
    {
        _cache = cache;
        _vectorService = vectorService;
        _logger = logger;
        _config = config ?? new RAGCacheConfig();
    }

    /// <summary>
    /// Gets RAG context from cache or performs search with intelligent caching
    /// </summary>
    public async Task<RAGContext> GetRAGContextAsync(
        string codeContent,
        CodeCharacteristics characteristics,
        string projectId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateIntelligentCacheKey(codeContent, characteristics, projectId);
        
        // Try exact cache hit first
        if (_cache.TryGetValue(cacheKey, out RAGContext? cachedContext) && cachedContext != null)
        {
            _logger.LogInformation("🎯 RAG CACHE HIT: Using exact cached context for {ProjectId}", projectId);
            cachedContext.CacheMetadata.CacheHitType = CacheHitType.Exact;
            return cachedContext;
        }

        // Try semantic similarity cache hit
        var similarContext = await TrySemanticCacheHitAsync(codeContent, characteristics, projectId);
        if (similarContext != null)
        {
            _logger.LogInformation("🎯 RAG SEMANTIC HIT: Using similar cached context (similarity: {Similarity:F2})", 
                similarContext.CacheMetadata.SimilarityScore);
            return similarContext;
        }

        // Perform full RAG search and cache results
        _logger.LogInformation("🔍 RAG CACHE MISS: Performing full search for {ProjectId}", projectId);
        var context = await PerformFullRAGSearchAsync(codeContent, characteristics, projectId, cancellationToken);
        
        // Cache the results with intelligent expiration
        await CacheRAGContextAsync(cacheKey, context, characteristics);
        
        context.CacheMetadata = new RAGCacheMetadata
        {
            CacheHitType = CacheHitType.Miss,
            SearchTimestamp = DateTime.UtcNow,
            CacheKey = cacheKey
        };

        return context;
    }

    /// <summary>
    /// Performs individual collection searches with caching
    /// </summary>
    public async Task<List<RetrievedContext>> SearchWithCacheAsync(
        string collection,
        string query,
        string? projectId,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateSearchCacheKey(collection, query, projectId, topK);
        
        if (_cache.TryGetValue(cacheKey, out List<RetrievedContext>? cachedResults) && cachedResults != null)
        {
            _logger.LogDebug("🎯 SEARCH CACHE HIT: {Collection} query cached", collection);
            return cachedResults;
        }

        // Perform search based on collection type
        List<RetrievedContext> results = collection switch
        {
            "code_patterns" => await _vectorService.SearchSimilarCodeAsync(query, projectId, topK, cancellationToken),
            "coding_standards" => await _vectorService.SearchCodingStandardsAsync(query, null, topK, cancellationToken),
            "historical_issues" => await _vectorService.SearchHistoricalIssuesAsync(query, projectId, topK, cancellationToken),
            "team_patterns" => await _vectorService.SearchTeamPatternsAsync(query, projectId, topK, cancellationToken),
            _ => throw new ArgumentException($"Unknown collection: {collection}")
        };

        // Cache results with collection-specific expiration
        var expiration = GetCollectionCacheExpiration(collection);
        _cache.Set(cacheKey, results, expiration);
        
        _logger.LogDebug("💾 SEARCH CACHE STORE: Cached {Collection} results ({Count} items)", 
            collection, results.Count);

        return results;
    }

    private string GenerateIntelligentCacheKey(
        string codeContent, 
        CodeCharacteristics characteristics, 
        string projectId)
    {
        // Create semantic hash that captures code meaning, not just exact text
        var semanticElements = new
        {
            // Core semantic features
            Language = characteristics.Language,
            FileType = characteristics.FileType,
            Complexity = Math.Round((double)characteristics.Complexity, 1), // Round to reduce key variations
            
            // Important patterns (binary flags to reduce variations)
            HasSecurity = characteristics.HasSecurityPatterns,
            HasDatabase = characteristics.HasDatabaseCalls,
            HasAuth = characteristics.HasAuthenticationLogic,
            IsPerformanceCritical = characteristics.PerformanceCritical,
            IsBusinessLogic = characteristics.BusinessLogicHeavy,
            
            // Size buckets instead of exact counts
            SizeBucket = GetSizeBucket(characteristics.LinesChanged),
            
            // Code structure hash (method signatures, class names, etc.)
            StructureHash = ComputeCodeStructureHash(codeContent),
            
            ProjectId = projectId
        };

        var semanticJson = JsonSerializer.Serialize(semanticElements);
        return $"rag_context:{ComputeHash(semanticJson)}";
    }

    private string GenerateSearchCacheKey(string collection, string query, string? projectId, int topK)
    {
        var elements = new { collection, query_hash = ComputeHash(query), projectId, topK };
        var json = JsonSerializer.Serialize(elements);
        return $"rag_search:{ComputeHash(json)}";
    }

    private async Task<RAGContext?> TrySemanticCacheHitAsync(
        string codeContent,
        CodeCharacteristics characteristics,
        string projectId)
    {
        // Generate variations of the cache key for semantic matching
        var baseKey = GenerateIntelligentCacheKey(codeContent, characteristics, projectId);
        var keyVariations = GenerateCacheKeyVariations(characteristics, projectId);

        foreach (var variation in keyVariations)
        {
            if (_cache.TryGetValue(variation, out RAGContext? context) && context != null)
            {
                // Check semantic similarity
                var similarity = await CalculateSemanticSimilarity(codeContent, context.OriginalCode);
                
                if (similarity >= _config.SemanticSimilarityThreshold)
                {
                    // Update cache metadata
                    context.CacheMetadata = new RAGCacheMetadata
                    {
                        CacheHitType = CacheHitType.Semantic,
                        SimilarityScore = similarity,
                        OriginalCacheKey = variation,
                        SearchTimestamp = DateTime.UtcNow
                    };
                    
                    return context;
                }
            }
        }

        return null;
    }

    private async Task<RAGContext> PerformFullRAGSearchAsync(
        string codeContent,
        CodeCharacteristics characteristics,
        string projectId,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var context = new RAGContext
        {
            OriginalCode = codeContent,
            ProjectId = projectId,
            Characteristics = characteristics
        };

        // Perform parallel searches across collections
        var searchTasks = new[]
        {
            SearchWithCacheAsync("code_patterns", BuildCodePatternsQuery(characteristics), projectId, 8, cancellationToken),
            SearchWithCacheAsync("coding_standards", BuildCodingStandardsQuery(characteristics), null, 6, cancellationToken),
            SearchWithCacheAsync("historical_issues", BuildHistoricalIssuesQuery(characteristics), projectId, 5, cancellationToken),
            SearchWithCacheAsync("team_patterns", BuildTeamPatternsQuery(characteristics), projectId, 4, cancellationToken)
        };

        var results = await Task.WhenAll(searchTasks);
        
        context.SimilarPatterns = results[0];
        context.ApplicableStandards = results[1].Select(r => new CodingStandard 
        { 
            Title = ExtractTitle(r.Content), 
            Description = r.Content,
            Priority = ExtractPriority(r.Metadata)
        }).ToList();
        context.HistoricalIssues = results[2];
        context.TeamPatterns = results[3];

        stopwatch.Stop();
        _logger.LogInformation("🔍 FULL RAG SEARCH: Completed in {Duration}ms, found {Patterns} patterns, {Standards} standards, {Issues} issues, {TeamPatterns} team patterns",
            stopwatch.ElapsedMilliseconds, context.SimilarPatterns.Count, context.ApplicableStandards.Count, 
            context.HistoricalIssues.Count, context.TeamPatterns.Count);

        return context;
    }

    private async Task CacheRAGContextAsync(
        string cacheKey, 
        RAGContext context, 
        CodeCharacteristics characteristics)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            // Smart expiration based on content type
            AbsoluteExpirationRelativeToNow = GetContextCacheExpiration(characteristics),
            SlidingExpiration = TimeSpan.FromHours(6),
            Size = EstimateContextSize(context),
            Priority = GetCachePriority(characteristics)
        };

        _cache.Set(cacheKey, context, cacheOptions);
        
        _logger.LogInformation("💾 RAG CACHE STORE: Cached context {CacheKey} (expires in {Expiration})", 
            cacheKey[^8..], cacheOptions.AbsoluteExpirationRelativeToNow);
    }

    private List<string> GenerateCacheKeyVariations(CodeCharacteristics characteristics, string projectId)
    {
        var variations = new List<string>();
        
        // Generate variations by relaxing some constraints
        var baseCharacteristics = characteristics;
        
        // Complexity variations (±1)
        if (characteristics.Complexity > 1)
        {
            var varCharacteristics = new CodeCharacteristics
            {
                Language = characteristics.Language,
                FileType = characteristics.FileType,
                Complexity = characteristics.Complexity - 1,
                LinesChanged = characteristics.LinesChanged,
                HasSecurityPatterns = characteristics.HasSecurityPatterns,
                HasDatabaseCalls = characteristics.HasDatabaseCalls,
                HasAuthenticationLogic = characteristics.HasAuthenticationLogic,
                PerformanceCritical = characteristics.PerformanceCritical,
                BusinessLogicHeavy = characteristics.BusinessLogicHeavy,
                ArchitecturalChanges = characteristics.ArchitecturalChanges
            };
            variations.Add(GenerateIntelligentCacheKey("", varCharacteristics, projectId));
        }
        if (characteristics.Complexity < 10)
        {
            var varCharacteristics = new CodeCharacteristics
            {
                Language = characteristics.Language,
                FileType = characteristics.FileType,
                Complexity = characteristics.Complexity + 1,
                LinesChanged = characteristics.LinesChanged,
                HasSecurityPatterns = characteristics.HasSecurityPatterns,
                HasDatabaseCalls = characteristics.HasDatabaseCalls,
                HasAuthenticationLogic = characteristics.HasAuthenticationLogic,
                PerformanceCritical = characteristics.PerformanceCritical,
                BusinessLogicHeavy = characteristics.BusinessLogicHeavy,
                ArchitecturalChanges = characteristics.ArchitecturalChanges
            };
            variations.Add(GenerateIntelligentCacheKey("", varCharacteristics, projectId));
        }

        // Size bucket variations
        var currentBucket = GetSizeBucket(characteristics.LinesChanged);
        foreach (var bucket in new[] { "small", "medium", "large" })
        {
            if (bucket != currentBucket)
            {
                var linesForBucket = bucket switch
                {
                    "small" => 25,
                    "medium" => 75,
                    "large" => 150,
                    _ => characteristics.LinesChanged
                };
                var varCharacteristics = new CodeCharacteristics
                {
                    Language = characteristics.Language,
                    FileType = characteristics.FileType,
                    Complexity = characteristics.Complexity,
                    LinesChanged = linesForBucket,
                    HasSecurityPatterns = characteristics.HasSecurityPatterns,
                    HasDatabaseCalls = characteristics.HasDatabaseCalls,
                    HasAuthenticationLogic = characteristics.HasAuthenticationLogic,
                    PerformanceCritical = characteristics.PerformanceCritical,
                    BusinessLogicHeavy = characteristics.BusinessLogicHeavy,
                    ArchitecturalChanges = characteristics.ArchitecturalChanges
                };
                variations.Add(GenerateIntelligentCacheKey("", varCharacteristics, projectId));
            }
        }

        return variations.Take(5).ToList(); // Limit variations to prevent excessive cache checks
    }

    private async Task<double> CalculateSemanticSimilarity(string code1, string code2)
    {
        // Quick structural similarity check
        var structure1 = ExtractCodeStructure(code1);
        var structure2 = ExtractCodeStructure(code2);
        
        // Simple similarity based on method names, class names, key patterns
        var commonElements = structure1.Intersect(structure2).Count();
        var totalElements = structure1.Union(structure2).Count();
        
        return totalElements > 0 ? (double)commonElements / totalElements : 0.0;
    }

    private string ComputeCodeStructureHash(string code)
    {
        var structure = ExtractCodeStructure(code);
        var structureText = string.Join("|", structure.OrderBy(s => s));
        return ComputeHash(structureText)[..16]; // First 16 chars for cache key
    }

    private HashSet<string> ExtractCodeStructure(string code)
    {
        var structure = new HashSet<string>();
        
        // Extract method signatures, class names, key patterns
        var lines = code.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Class declarations
            if (trimmed.StartsWith("public class") || trimmed.StartsWith("class"))
            {
                structure.Add($"class:{ExtractClassName(trimmed)}");
            }
            
            // Method signatures
            if (trimmed.Contains("(") && trimmed.Contains(")") && 
                (trimmed.Contains("public") || trimmed.Contains("private") || trimmed.Contains("protected")))
            {
                structure.Add($"method:{ExtractMethodSignature(trimmed)}");
            }
            
            // Important keywords
            foreach (var keyword in new[] { "async", "await", "Task", "IActionResult", "HttpPost", "HttpGet", "ConnectionString", "SqlCommand" })
            {
                if (trimmed.Contains(keyword))
                {
                    structure.Add($"keyword:{keyword}");
                }
            }
        }
        
        return structure;
    }

    private string GetSizeBucket(int linesChanged) => linesChanged switch
    {
        < 50 => "small",
        < 100 => "medium",
        _ => "large"
    };

    private TimeSpan GetContextCacheExpiration(CodeCharacteristics characteristics)
    {
        // Security-sensitive code cached for shorter time
        if (characteristics.HasSecurityPatterns || characteristics.HasAuthenticationLogic)
            return TimeSpan.FromHours(12);
            
        // Simple code cached longer
        if (characteristics.Complexity < 4 && !characteristics.BusinessLogicHeavy)
            return TimeSpan.FromDays(3);
            
        // Default expiration
        return TimeSpan.FromDays(1);
    }

    private TimeSpan GetCollectionCacheExpiration(string collection) => collection switch
    {
        "code_patterns" => TimeSpan.FromHours(6),     // Code patterns change frequently
        "coding_standards" => TimeSpan.FromDays(7),   // Standards are stable
        "historical_issues" => TimeSpan.FromDays(3),  // Historical data is stable
        "team_patterns" => TimeSpan.FromDays(1),      // Team patterns evolve
        _ => TimeSpan.FromHours(24)
    };

    private CacheItemPriority GetCachePriority(CodeCharacteristics characteristics)
    {
        if (characteristics.HasSecurityPatterns || characteristics.PerformanceCritical)
            return CacheItemPriority.High;
            
        if (characteristics.Complexity > 7 || characteristics.BusinessLogicHeavy)
            return CacheItemPriority.Normal;
            
        return CacheItemPriority.Low;
    }

    private long EstimateContextSize(RAGContext context)
    {
        return (context.SimilarPatterns.Count * 200) +
               (context.ApplicableStandards.Count * 150) +
               (context.HistoricalIssues.Count * 300) +
               (context.TeamPatterns.Count * 100) +
               context.OriginalCode.Length;
    }

    private string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16]; // First 16 chars
    }

    // Helper methods for query building and content extraction
    private string BuildCodePatternsQuery(CodeCharacteristics characteristics) =>
        $"{characteristics.Language} {characteristics.FileType} " +
        $"{(characteristics.HasSecurityPatterns ? "security" : "")} " +
        $"{(characteristics.HasDatabaseCalls ? "database" : "")} " +
        $"{(characteristics.PerformanceCritical ? "performance" : "")}".Trim();

    private string BuildCodingStandardsQuery(CodeCharacteristics characteristics) =>
        $"{characteristics.Language} coding standards " +
        $"{(characteristics.HasSecurityPatterns ? "security" : "")} " +
        $"{(characteristics.ArchitecturalChanges ? "architecture" : "")}".Trim();

    private string BuildHistoricalIssuesQuery(CodeCharacteristics characteristics) =>
        $"{characteristics.Language} issues problems " +
        $"{(characteristics.HasSecurityPatterns ? "security vulnerability" : "")} " +
        $"{(characteristics.PerformanceCritical ? "performance" : "")}".Trim();

    private string BuildTeamPatternsQuery(CodeCharacteristics characteristics) =>
        $"{characteristics.Language} team patterns preferences " +
        $"{characteristics.FileType} conventions".Trim();

    private string ExtractClassName(string line) =>
        line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .SkipWhile(w => w != "class")
            .Skip(1)
            .FirstOrDefault() ?? "unknown";

    private string ExtractMethodSignature(string line)
    {
        var parenIndex = line.IndexOf('(');
        if (parenIndex > 0)
        {
            var beforeParen = line[..parenIndex].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return beforeParen.LastOrDefault() ?? "unknown";
        }
        return "unknown";
    }

    private string ExtractTitle(string content) =>
        content.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Unknown";

    private int ExtractPriority(Dictionary<string, object> metadata) =>
        metadata.TryGetValue("priority", out var priority) && int.TryParse(priority.ToString(), out var p) ? p : 5;
}

/// <summary>
/// Configuration for RAG cache behavior
/// </summary>
public class RAGCacheConfig
{
    public double SemanticSimilarityThreshold { get; set; } = 0.8;
    public int MaxCacheKeyVariations { get; set; } = 5;
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromDays(1);
    public long MaxCacheSize { get; set; } = 100_000_000; // 100MB
}

/// <summary>
/// Enhanced RAG context with cache metadata
/// </summary>
public class RAGContext
{
    public string OriginalCode { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public CodeCharacteristics? Characteristics { get; set; }
    public List<RetrievedContext> SimilarPatterns { get; set; } = new();
    public List<CodingStandard> ApplicableStandards { get; set; } = new();
    public List<RetrievedContext> HistoricalIssues { get; set; } = new();
    public List<RetrievedContext> TeamPatterns { get; set; } = new();
    public RAGCacheMetadata CacheMetadata { get; set; } = new();
}

/// <summary>
/// Cache metadata for tracking cache performance
/// </summary>
public class RAGCacheMetadata
{
    public CacheHitType CacheHitType { get; set; }
    public double SimilarityScore { get; set; }
    public string OriginalCacheKey { get; set; } = string.Empty;
    public string CacheKey { get; set; } = string.Empty;
    public DateTime SearchTimestamp { get; set; }
}

/// <summary>
/// Types of cache hits for performance tracking
/// </summary>
public enum CacheHitType
{
    Miss,
    Exact,
    Semantic
}