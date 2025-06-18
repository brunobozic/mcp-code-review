using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Mcp.CodeReview.RAG
{
    /// <summary>
    /// Health checker for RAG system components
    /// </summary>
    public class RagHealthChecker
    {
        private readonly IVectorSearchService _vectorSearchService;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<RagHealthChecker> _logger;

        public RagHealthChecker(
            IVectorSearchService vectorSearchService,
            IEmbeddingService embeddingService,
            ILogger<RagHealthChecker> logger)
        {
            _vectorSearchService = vectorSearchService ?? throw new ArgumentNullException(nameof(vectorSearchService));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Performs comprehensive health check of RAG system
        /// </summary>
        public async Task<RagHealthStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var status = new RagHealthStatus
            {
                CheckTimestamp = DateTime.UtcNow,
                IsHealthy = true
            };

            try
            {
                _logger.LogInformation("Starting RAG health check");

                // Test 1: Embedding Service
                await CheckEmbeddingServiceAsync(status, cancellationToken);

                // Test 2: Vector Search Service  
                await CheckVectorSearchServiceAsync(status, cancellationToken);

                // Test 3: End-to-End RAG Flow
                await CheckEndToEndRagFlowAsync(status, cancellationToken);

                _logger.LogInformation("RAG health check completed. Status: {Status}", 
                    status.IsHealthy ? "Healthy" : "Unhealthy");

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RAG health check failed with exception");
                status.IsHealthy = false;
                status.Issues.Add($"Health check exception: {ex.Message}");
                return status;
            }
        }

        /// <summary>
        /// Tests embedding service functionality
        /// </summary>
        private async Task CheckEmbeddingServiceAsync(RagHealthStatus status, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Testing embedding service");

                var testText = "Test embedding generation for authentication security";
                var embedding = await _embeddingService.GetEmbeddingAsync(testText, cancellationToken);

                if (embedding == null || embedding.Length == 0)
                {
                    status.IsHealthy = false;
                    status.Issues.Add("Embedding service returned null or empty embedding");
                    return;
                }

                if (embedding.Length != _embeddingService.EmbeddingDimension)
                {
                    status.IsHealthy = false;
                    status.Issues.Add($"Embedding dimension mismatch: expected {_embeddingService.EmbeddingDimension}, got {embedding.Length}");
                    return;
                }

                status.ComponentStatuses["EmbeddingService"] = "Healthy";
                _logger.LogDebug("Embedding service test passed");
            }
            catch (Exception ex)
            {
                status.IsHealthy = false;
                status.Issues.Add($"Embedding service test failed: {ex.Message}");
                status.ComponentStatuses["EmbeddingService"] = "Failed";
                _logger.LogError(ex, "Embedding service test failed");
            }
        }

        /// <summary>
        /// Tests vector search service functionality
        /// </summary>
        private async Task CheckVectorSearchServiceAsync(RagHealthStatus status, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Testing vector search service");

                // Test collections initialization
                await _vectorSearchService.InitializeCollectionsAsync(cancellationToken);

                // Test search functionality
                var searchResults = await _vectorSearchService.SearchCodingStandardsAsync(
                    "security authentication", "security", 3, cancellationToken);

                if (searchResults == null)
                {
                    status.IsHealthy = false;
                    status.Issues.Add("Vector search returned null results");
                    return;
                }

                status.ComponentStatuses["VectorSearchService"] = "Healthy";
                status.SearchResults = searchResults.Count;
                _logger.LogDebug("Vector search service test passed. Found {Count} results", searchResults.Count);
            }
            catch (Exception ex)
            {
                status.IsHealthy = false;
                status.Issues.Add($"Vector search service test failed: {ex.Message}");
                status.ComponentStatuses["VectorSearchService"] = "Failed";
                _logger.LogError(ex, "Vector search service test failed");
            }
        }

        /// <summary>
        /// Tests end-to-end RAG flow
        /// </summary>
        private async Task CheckEndToEndRagFlowAsync(RagHealthStatus status, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Testing end-to-end RAG flow");

                // Test storing a document
                var testDocument = new RAGDocument
                {
                    Id = $"health-check-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    Collection = "coding_standards",
                    Content = "Test document for health check: Always validate user inputs for security",
                    Metadata = new Dictionary<string, object>
                    {
                        ["category"] = "security",
                        ["type"] = "health-check",
                        ["timestamp"] = DateTime.UtcNow.ToString("O")
                    }
                };

                await _vectorSearchService.StoreDocumentAsync(testDocument, cancellationToken);

                // Wait a moment for indexing
                await Task.Delay(1000, cancellationToken);

                // Test searching for the stored document
                var searchResults = await _vectorSearchService.SearchCodingStandardsAsync(
                    "validate user inputs security", "security", 5, cancellationToken);

                var foundTestDocument = false;
                foreach (var result in searchResults)
                {
                    if (result.Content.Contains("health check"))
                    {
                        foundTestDocument = true;
                        break;
                    }
                }

                if (!foundTestDocument)
                {
                    status.IsHealthy = false;
                    status.Issues.Add("End-to-end test failed: Could not retrieve stored test document");
                    return;
                }

                status.ComponentStatuses["EndToEndFlow"] = "Healthy";
                _logger.LogDebug("End-to-end RAG flow test passed");
            }
            catch (Exception ex)
            {
                status.IsHealthy = false;
                status.Issues.Add($"End-to-end RAG flow test failed: {ex.Message}");
                status.ComponentStatuses["EndToEndFlow"] = "Failed";
                _logger.LogError(ex, "End-to-end RAG flow test failed");
            }
        }
    }

    /// <summary>
    /// RAG system health status
    /// </summary>
    public class RagHealthStatus
    {
        public DateTime CheckTimestamp { get; set; }
        public bool IsHealthy { get; set; }
        public List<string> Issues { get; set; } = new();
        public Dictionary<string, string> ComponentStatuses { get; set; } = new();
        public int SearchResults { get; set; }

        public string GetSummary()
        {
            if (IsHealthy)
            {
                return $"RAG system is healthy. {SearchResults} search results available.";
            }

            return $"RAG system has issues: {string.Join(", ", Issues)}";
        }
    }
}