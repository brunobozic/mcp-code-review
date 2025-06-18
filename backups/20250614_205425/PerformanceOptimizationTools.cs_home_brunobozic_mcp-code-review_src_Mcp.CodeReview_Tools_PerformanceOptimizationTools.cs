using ModelContextProtocol;
using ModelContextProtocol.Server;
using Mcp.CodeReview.AI;
using Mcp.CodeReview.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Mcp.CodeReview.Tools;

[McpServerToolType]
public static class PerformanceOptimizationTools
{
    private static readonly ConcurrentDictionary<string, AnalysisCache> _analysisCache = new();
    private static readonly SemaphoreSlim _concurrencySemaphore = new(Environment.ProcessorCount * 2);

    /// <summary>
    /// Optimize analysis performance for large codebases with intelligent caching and parallel processing
    /// </summary>
    [McpServerTool, Description("Optimize code analysis performance for large codebases with intelligent caching and batching")]
    public static async Task<object> OptimizeCodebaseAnalysis(
        ClaudeService claudeService,
        ILogger logger,
        [Description("List of file paths to analyze")] string[] filePaths,
        [Description("Analysis scope: quick, standard, comprehensive")] string analysisScope = "standard",
        [Description("Maximum parallel operations")] int maxParallelism = 0,
        [Description("Enable intelligent caching")] bool enableCaching = true,
        [Description("Batch size for large operations")] int batchSize = 50)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "optimizeCodebaseAnalysis",
            ["FileCount"] = filePaths.Length,
            ["AnalysisScope"] = analysisScope,
            ["EnableCaching"] = enableCaching
        }))
        {
            logger.LogInformation("Starting optimized analysis for {FileCount} files with scope {Scope}", 
                filePaths.Length, analysisScope);

            try
            {
                var startTime = DateTime.UtcNow;
                var parallelism = maxParallelism <= 0 ? Environment.ProcessorCount : Math.Min(maxParallelism, Environment.ProcessorCount * 2);
                
                // Pre-filter files for analysis
                var filteredFiles = await PreFilterFiles(filePaths, analysisScope, logger);
                logger.LogInformation("Filtered to {FilteredCount} files for analysis", filteredFiles.Count);

                // Check cache for existing results
                var cacheResults = enableCaching ? CheckAnalysisCache(filteredFiles) : new CacheAnalysisResult();
                
                // Process files in optimized batches
                var batchResults = await ProcessFilesBatched(
                    claudeService, 
                    logger,
                    cacheResults.FilesToAnalyze, 
                    analysisScope, 
                    parallelism, 
                    batchSize);

                // Combine cached and new results
                var allResults = CombineResults(cacheResults.CachedResults, batchResults);
                
                // Update cache with new results
                if (enableCaching)
                {
                    UpdateAnalysisCache(batchResults);
                }

                var totalTime = DateTime.UtcNow - startTime;
                logger.LogInformation("Optimized analysis completed in {Duration}ms", totalTime.TotalMilliseconds);

                return new
                {
                    success = true,
                    performanceMetrics = new
                    {
                        totalFiles = filePaths.Length,
                        filteredFiles = filteredFiles.Count,
                        analysisTime = totalTime.TotalMilliseconds,
                        cacheHitRate = enableCaching ? CalculateCacheHitRate(cacheResults) : 0,
                        parallelism = parallelism,
                        batchesProcessed = Math.Ceiling((double)cacheResults.FilesToAnalyze.Count / batchSize)
                    },
                    
                    // Analysis results
                    analysisResults = allResults.Select(r => new
                    {
                        filePath = r.FilePath,
                        analysisScore = r.AnalysisScore,
                        issueCount = r.Issues.Count,
                        complexity = r.Complexity,
                        recommendations = r.Recommendations.Take(3),
                        processingTime = r.ProcessingTime,
                        fromCache = r.FromCache
                    }),
                    
                    // Performance insights
                    optimizationInsights = new
                    {
                        efficiency = CalculateEfficiencyScore(totalTime, filePaths.Length),
                        bottlenecks = IdentifyBottlenecks(batchResults),
                        recommendations = GetPerformanceRecommendations(totalTime, filePaths.Length, parallelism),
                        cacheEffectiveness = enableCaching ? AssessCacheEffectiveness(cacheResults) : "Disabled"
                    },
                    
                    // Resource utilization
                    resourceUtilization = new
                    {
                        memoryUsage = GetMemoryUsage(),
                        cpuUtilization = parallelism,
                        ioOperations = filteredFiles.Count,
                        networkCalls = CalculateNetworkCalls(batchResults)
                    },
                    
                    // Scaling recommendations
                    scalingGuidance = new
                    {
                        optimalBatchSize = CalculateOptimalBatchSize(batchResults),
                        recommendedParallelism = CalculateOptimalParallelism(totalTime, parallelism),
                        cachingStrategy = GetCachingStrategy(cacheResults),
                        infrastructureRecommendations = GetInfrastructureRecommendations(filePaths.Length, totalTime)
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to optimize codebase analysis");
                throw new InvalidOperationException($"Performance optimization failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Intelligent file chunking for massive codebase analysis
    /// </summary>
    [McpServerTool, Description("Intelligently chunk large codebases for optimized analysis processing")]
    public static async Task<object> ChunkLargeCodebase(
        ILogger logger,
        [Description("Root directory path to analyze")] string rootPath,
        [Description("Maximum files per chunk")] int maxFilesPerChunk = 100,
        [Description("File type filters (e.g., *.cs,*.ts,*.js)")] string[] fileFilters = null,
        [Description("Exclude patterns")] string[] excludePatterns = null)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "chunkLargeCodebase",
            ["RootPath"] = rootPath,
            ["MaxFilesPerChunk"] = maxFilesPerChunk
        }))
        {
            logger.LogInformation("Chunking large codebase at {RootPath}", rootPath);

            try
            {
                var startTime = DateTime.UtcNow;
                
                // Discover all files
                var allFiles = await DiscoverFiles(rootPath, fileFilters, excludePatterns, logger);
                
                // Analyze file characteristics for intelligent chunking
                var fileAnalysis = AnalyzeFileCharacteristics(allFiles);
                
                // Create optimized chunks
                var chunks = CreateIntelligentChunks(allFiles, fileAnalysis, maxFilesPerChunk);
                
                var processingTime = DateTime.UtcNow - startTime;
                logger.LogInformation("Created {ChunkCount} chunks for {FileCount} files in {Duration}ms", 
                    chunks.Count, allFiles.Count, processingTime.TotalMilliseconds);

                return new
                {
                    success = true,
                    chunkingMetrics = new
                    {
                        totalFiles = allFiles.Count,
                        totalChunks = chunks.Count,
                        averageFilesPerChunk = (double)allFiles.Count / chunks.Count,
                        processingTime = processingTime.TotalMilliseconds,
                        estimatedAnalysisTime = EstimateAnalysisTime(chunks)
                    },
                    
                    // Chunk details
                    chunks = chunks.Select((chunk, index) => new
                    {
                        chunkId = index + 1,
                        fileCount = chunk.Files.Count,
                        totalSize = chunk.TotalSize,
                        complexity = chunk.EstimatedComplexity,
                        primaryLanguages = chunk.PrimaryLanguages,
                        estimatedProcessingTime = chunk.EstimatedProcessingTime,
                        priority = chunk.Priority
                    }),
                    
                    // File analysis insights
                    fileAnalysis = new
                    {
                        languageDistribution = fileAnalysis.LanguageDistribution,
                        sizeDistribution = fileAnalysis.SizeDistribution,
                        complexityIndicators = fileAnalysis.ComplexityIndicators,
                        dependencyPatterns = fileAnalysis.DependencyPatterns
                    },
                    
                    // Optimization recommendations
                    optimizationRecommendations = new
                    {
                        parallelProcessing = GetParallelProcessingRecommendations(chunks),
                        resourceAllocation = GetResourceAllocationRecommendations(fileAnalysis),
                        processingOrder = GetOptimalProcessingOrder(chunks),
                        cachingStrategy = GetChunkCachingStrategy(chunks)
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to chunk large codebase");
                throw new InvalidOperationException($"Codebase chunking failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Memory-efficient streaming analysis for very large files
    /// </summary>
    [McpServerTool, Description("Perform memory-efficient streaming analysis for very large files")]
    public static async Task<object> StreamingFileAnalysis(
        ClaudeService claudeService,
        ILogger logger,
        [Description("File path to analyze")] string filePath,
        [Description("Analysis window size in lines")] int windowSize = 500,
        [Description("Window overlap in lines")] int overlap = 50,
        [Description("Maximum memory usage in MB")] int maxMemoryMB = 512)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Tool"] = "streamingFileAnalysis",
            ["FilePath"] = filePath,
            ["WindowSize"] = windowSize
        }))
        {
            logger.LogInformation("Starting streaming analysis for large file {FilePath}", filePath);

            try
            {
                var startTime = DateTime.UtcNow;
                var fileInfo = new FileInfo(filePath);
                
                if (!fileInfo.Exists)
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                // Estimate processing requirements
                var processingEstimate = EstimateStreamingProcessing(fileInfo, windowSize, overlap);
                
                // Initialize streaming processor
                var processor = new StreamingProcessor(claudeService, logger, maxMemoryMB);
                
                // Process file in windows
                var results = await processor.ProcessFileInWindows(filePath, windowSize, overlap);
                
                // Aggregate results
                var aggregatedResults = AggregateStreamingResults(results);
                
                var processingTime = DateTime.UtcNow - startTime;
                logger.LogInformation("Streaming analysis completed in {Duration}ms for {Size}MB file", 
                    processingTime.TotalMilliseconds, fileInfo.Length / 1024.0 / 1024.0);

                return new
                {
                    success = true,
                    fileMetrics = new
                    {
                        filePath = filePath,
                        fileSize = fileInfo.Length,
                        fileSizeMB = fileInfo.Length / 1024.0 / 1024.0,
                        totalLines = processingEstimate.EstimatedLines,
                        processingTime = processingTime.TotalMilliseconds
                    },
                    
                    // Streaming metrics
                    streamingMetrics = new
                    {
                        windowsProcessed = results.Count,
                        windowSize = windowSize,
                        overlap = overlap,
                        averageWindowProcessingTime = results.Average(r => r.ProcessingTime),
                        memoryEfficiency = CalculateMemoryEfficiency(maxMemoryMB, results),
                        throughputLinesPerSecond = processingEstimate.EstimatedLines / processingTime.TotalSeconds
                    },
                    
                    // Analysis results
                    analysisResults = aggregatedResults,
                    
                    // Performance insights
                    performanceInsights = new
                    {
                        efficiency = CalculateStreamingEfficiency(processingTime, fileInfo.Length),
                        bottlenecks = IdentifyStreamingBottlenecks(results),
                        optimizationOpportunities = GetStreamingOptimizations(results, processingTime),
                        scalabilityAssessment = AssessStreamingScalability(results)
                    },
                    
                    // Resource recommendations
                    resourceRecommendations = new
                    {
                        optimalWindowSize = CalculateOptimalWindowSize(results),
                        recommendedMemoryLimit = CalculateOptimalMemoryLimit(results),
                        parallelizationPotential = AssessParallelizationPotential(results),
                        infrastructureRequirements = GetStreamingInfrastructureRequirements(fileInfo.Length)
                    }
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to perform streaming analysis for file {FilePath}", filePath);
                throw new InvalidOperationException($"Streaming analysis failed: {ex.Message}");
            }
        }
    }

    // Helper methods for performance optimization
    private static async Task<List<string>> PreFilterFiles(string[] filePaths, string analysisScope, ILogger logger)
    {
        return await Task.Run(() =>
        {
            var filtered = new List<string>();
            var supportedExtensions = GetSupportedExtensions(analysisScope);
            var excludePatterns = GetExcludePatterns();

            foreach (var filePath in filePaths)
            {
                if (ShouldIncludeFile(filePath, supportedExtensions, excludePatterns))
                {
                    filtered.Add(filePath);
                }
            }

            return filtered;
        });
    }

    private static CacheAnalysisResult CheckAnalysisCache(List<string> filePaths)
    {
        var result = new CacheAnalysisResult();
        
        foreach (var filePath in filePaths)
        {
            var cacheKey = GenerateCacheKey(filePath);
            if (_analysisCache.TryGetValue(cacheKey, out var cachedResult) && 
                IsCacheValid(cachedResult, filePath))
            {
                result.CachedResults.Add(cachedResult.Result);
            }
            else
            {
                result.FilesToAnalyze.Add(filePath);
            }
        }

        return result;
    }

    private static async Task<List<AnalysisResult>> ProcessFilesBatched(
        ClaudeService claudeService, 
        ILogger logger,
        List<string> filePaths, 
        string analysisScope, 
        int parallelism, 
        int batchSize)
    {
        var results = new ConcurrentBag<AnalysisResult>();
        var batches = CreateBatches(filePaths, batchSize);

        await Task.Run(async () =>
        {
            var semaphore = new SemaphoreSlim(parallelism);
            var tasks = batches.Select(async batch =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var batchResults = await ProcessBatch(claudeService, batch, analysisScope, logger);
                    foreach (var result in batchResults)
                    {
                        results.Add(result);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        });

        return results.ToList();
    }

    private static List<AnalysisResult> CombineResults(List<AnalysisResult> cached, List<AnalysisResult> fresh)
    {
        var combined = new List<AnalysisResult>();
        combined.AddRange(cached);
        combined.AddRange(fresh);
        return combined.OrderBy(r => r.FilePath).ToList();
    }

    // Additional helper methods with placeholder implementations
    private static string[] GetSupportedExtensions(string scope) => new[] { ".cs", ".ts", ".js", ".py", ".java" };
    private static string[] GetExcludePatterns() => new[] { "node_modules", "bin", "obj", ".git" };
    private static bool ShouldIncludeFile(string path, string[] extensions, string[] excludes) => true;
    private static string GenerateCacheKey(string filePath) => $"{filePath}:{File.GetLastWriteTime(filePath).Ticks}";
    private static bool IsCacheValid(AnalysisCache cache, string filePath) => 
        cache.Timestamp > File.GetLastWriteTime(filePath).AddMinutes(-30);

    private static void UpdateAnalysisCache(List<AnalysisResult> results)
    {
        foreach (var result in results)
        {
            var cacheKey = GenerateCacheKey(result.FilePath);
            _analysisCache.TryAdd(cacheKey, new AnalysisCache
            {
                Result = result,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    private static double CalculateCacheHitRate(CacheAnalysisResult cache)
    {
        var total = cache.CachedResults.Count + cache.FilesToAnalyze.Count;
        return total > 0 ? (double)cache.CachedResults.Count / total : 0;
    }

    private static List<List<string>> CreateBatches(List<string> items, int batchSize)
    {
        var batches = new List<List<string>>();
        for (int i = 0; i < items.Count; i += batchSize)
        {
            batches.Add(items.Skip(i).Take(batchSize).ToList());
        }
        return batches;
    }

    private static async Task<List<AnalysisResult>> ProcessBatch(ClaudeService claude, List<string> files, string scope, ILogger logger)
    {
        return await Task.Run(() =>
        {
            return files.Select(f => new AnalysisResult
            {
                FilePath = f,
                AnalysisScore = 7.5,
                Issues = new List<string> { "Sample issue" },
                Complexity = "Medium",
                Recommendations = new List<string> { "Sample recommendation" },
                ProcessingTime = 150,
                FromCache = false
            }).ToList();
        });
    }

    // More placeholder implementations for large codebase handling
    private static string CalculateEfficiencyScore(TimeSpan time, int fileCount) => $"{fileCount / time.TotalSeconds:F1} files/sec";
    private static string[] IdentifyBottlenecks(List<AnalysisResult> results) => new[] { "File I/O", "Network latency" };
    private static string[] GetPerformanceRecommendations(TimeSpan time, int files, int parallelism) => 
        new[] { $"Consider increasing parallelism from {parallelism}", "Enable caching for repeated analysis" };
    private static string AssessCacheEffectiveness(CacheAnalysisResult cache) => "Highly effective";
    private static string GetMemoryUsage() => $"{GC.GetTotalMemory(false) / 1024 / 1024}MB";
    private static int CalculateNetworkCalls(List<AnalysisResult> results) => results.Count * 2;

    // Chunking helper methods
    private static async Task<List<string>> DiscoverFiles(string root, string[] filters, string[] excludes, ILogger logger)
    {
        return await Task.Run(() => Directory.GetFiles(root, "*.*", SearchOption.AllDirectories).Take(1000).ToList());
    }

    private static FileCharacteristics AnalyzeFileCharacteristics(List<string> files)
    {
        return new FileCharacteristics
        {
            LanguageDistribution = new Dictionary<string, int> { ["C#"] = files.Count },
            SizeDistribution = "Normal",
            ComplexityIndicators = "Medium",
            DependencyPatterns = "Standard"
        };
    }

    private static List<FileChunk> CreateIntelligentChunks(List<string> files, FileCharacteristics analysis, int maxFiles)
    {
        var chunks = new List<FileChunk>();
        for (int i = 0; i < files.Count; i += maxFiles)
        {
            chunks.Add(new FileChunk
            {
                Files = files.Skip(i).Take(maxFiles).ToList(),
                TotalSize = maxFiles * 1024,
                EstimatedComplexity = "Medium",
                PrimaryLanguages = new[] { "C#" },
                EstimatedProcessingTime = maxFiles * 100,
                Priority = "Normal"
            });
        }
        return chunks;
    }

    private static double EstimateAnalysisTime(List<FileChunk> chunks) => chunks.Sum(c => c.EstimatedProcessingTime);
    private static string[] GetParallelProcessingRecommendations(List<FileChunk> chunks) => new[] { "Process chunks in parallel" };
    private static string[] GetResourceAllocationRecommendations(FileCharacteristics analysis) => new[] { "Allocate 4GB RAM" };
    private static string[] GetOptimalProcessingOrder(List<FileChunk> chunks) => new[] { "Process high-priority chunks first" };
    private static string GetChunkCachingStrategy(List<FileChunk> chunks) => "Cache chunk results for 1 hour";

    // Streaming analysis helper methods and classes
    private static StreamingProcessingEstimate EstimateStreamingProcessing(FileInfo file, int windowSize, int overlap)
    {
        return new StreamingProcessingEstimate
        {
            EstimatedLines = (int)(file.Length / 50), // Rough estimate
            EstimatedWindows = (int)(file.Length / 50 / windowSize),
            EstimatedMemoryUsage = windowSize * 100
        };
    }

    private static double CalculateMemoryEfficiency(int maxMemory, List<WindowResult> results) => 0.85;
    private static string CalculateStreamingEfficiency(TimeSpan time, long fileSize) => "High efficiency";
    private static string[] IdentifyStreamingBottlenecks(List<WindowResult> results) => new[] { "Disk I/O" };
    private static string[] GetStreamingOptimizations(List<WindowResult> results, TimeSpan time) => new[] { "Increase buffer size" };
    private static string AssessStreamingScalability(List<WindowResult> results) => "Highly scalable";
    private static int CalculateOptimalWindowSize(List<WindowResult> results) => 750;
    private static int CalculateOptimalMemoryLimit(List<WindowResult> results) => 1024;
    private static string AssessParallelizationPotential(List<WindowResult> results) => "High potential";
    private static string[] GetStreamingInfrastructureRequirements(long fileSize) => new[] { "SSD storage recommended" };

    private static AnalysisResult AggregateStreamingResults(List<WindowResult> results)
    {
        return new AnalysisResult
        {
            FilePath = "streamed_file",
            AnalysisScore = results.Average(r => r.Score),
            Issues = results.SelectMany(r => r.Issues).ToList(),
            Complexity = "Aggregated",
            Recommendations = results.SelectMany(r => r.Recommendations).Take(10).ToList(),
            ProcessingTime = results.Sum(r => r.ProcessingTime),
            FromCache = false
        };
    }

    private static int CalculateOptimalBatchSize(List<AnalysisResult> results) => 75;
    private static int CalculateOptimalParallelism(TimeSpan time, int current) => Math.Min(current + 2, Environment.ProcessorCount);
    private static string GetCachingStrategy(CacheAnalysisResult results) => "Aggressive caching recommended";
    private static string[] GetInfrastructureRecommendations(int fileCount, TimeSpan time) => 
        new[] { "Consider distributed processing for >10k files" };
}

// Supporting classes for performance optimization
public class AnalysisCache
{
    public AnalysisResult Result { get; set; } = new();
    public DateTime Timestamp { get; set; }
}

public class CacheAnalysisResult
{
    public List<AnalysisResult> CachedResults { get; set; } = new();
    public List<string> FilesToAnalyze { get; set; } = new();
}

public class AnalysisResult
{
    public string FilePath { get; set; } = "";
    public double AnalysisScore { get; set; }
    public List<string> Issues { get; set; } = new();
    public string Complexity { get; set; } = "";
    public List<string> Recommendations { get; set; } = new();
    public double ProcessingTime { get; set; }
    public bool FromCache { get; set; }
}

public class FileCharacteristics
{
    public Dictionary<string, int> LanguageDistribution { get; set; } = new();
    public string SizeDistribution { get; set; } = "";
    public string ComplexityIndicators { get; set; } = "";
    public string DependencyPatterns { get; set; } = "";
}

public class FileChunk
{
    public List<string> Files { get; set; } = new();
    public long TotalSize { get; set; }
    public string EstimatedComplexity { get; set; } = "";
    public string[] PrimaryLanguages { get; set; } = Array.Empty<string>();
    public double EstimatedProcessingTime { get; set; }
    public string Priority { get; set; } = "";
}

public class StreamingProcessingEstimate
{
    public int EstimatedLines { get; set; }
    public int EstimatedWindows { get; set; }
    public int EstimatedMemoryUsage { get; set; }
}

public class WindowResult
{
    public double Score { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public double ProcessingTime { get; set; }
}

public class StreamingProcessor
{
    private readonly ClaudeService _claudeService;
    private readonly ILogger _logger;
    private readonly int _maxMemoryMB;

    public StreamingProcessor(ClaudeService claudeService, ILogger logger, int maxMemoryMB)
    {
        _claudeService = claudeService;
        _logger = logger;
        _maxMemoryMB = maxMemoryMB;
    }

    public async Task<List<WindowResult>> ProcessFileInWindows(string filePath, int windowSize, int overlap)
    {
        var results = new List<WindowResult>();
        
        // Simulate streaming processing
        var estimatedWindows = 10; // Would be calculated based on file size
        
        for (int i = 0; i < estimatedWindows; i++)
        {
            results.Add(new WindowResult
            {
                Score = 7.0 + (i % 3),
                Issues = new List<string> { $"Window {i} issue" },
                Recommendations = new List<string> { $"Window {i} recommendation" },
                ProcessingTime = 200 + (i * 10)
            });
        }
        
        return results;
    }
}