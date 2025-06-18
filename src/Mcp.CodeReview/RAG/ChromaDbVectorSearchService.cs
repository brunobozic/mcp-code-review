using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mcp.CodeReview.RAG
{
    /// <summary>
    /// ChromaDB implementation of vector search service
    /// </summary>
    public class ChromaDbVectorSearchService : IVectorSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<ChromaDbVectorSearchService> _logger;
        private readonly ChromaDbConfig _config;

        // Collection names for different types of RAG data
        private const string CodePatternsCollection = "code_patterns";
        private const string CodingStandardsCollection = "coding_standards";
        private const string HistoricalIssuesCollection = "historical_issues";
        private const string TeamPatternsCollection = "team_patterns";

        public ChromaDbVectorSearchService(
            HttpClient httpClient,
            IEmbeddingService embeddingService,
            IOptions<ChromaDbConfig> config,
            ILogger<ChromaDbVectorSearchService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));

            ConfigureHttpClient();
        }

        /// <summary>
        /// Configures HTTP client for ChromaDB API
        /// </summary>
        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.AuthToken}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MCP-CodeReview-RAG/1.0");
        }

        /// <summary>
        /// Searches for similar code patterns and issues
        /// </summary>
        public async Task<List<RetrievedContext>> SearchSimilarCodeAsync(
            string query, 
            string projectId, 
            int topK = 5, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Searching for similar code patterns for project {ProjectId}", projectId);

            try
            {
                var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query, cancellationToken);
                
                var searchRequest = new ChromaSearchRequest
                {
                    QueryEmbeddings = new[] { queryEmbedding },
                    NResults = topK,
                    Where = new Dictionary<string, object>
                    {
                        ["project_id"] = projectId
                    },
                    Include = new[] { "metadatas", "documents", "distances" }
                };

                var results = await SearchCollectionAsync(CodePatternsCollection, searchRequest, cancellationToken);
                return ConvertToRetrievedContext(results, "CodePatterns");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching similar code for project {ProjectId}", projectId);
                return new List<RetrievedContext>();
            }
        }

        /// <summary>
        /// Searches for relevant coding standards and guidelines
        /// </summary>
        public async Task<List<RetrievedContext>> SearchCodingStandardsAsync(
            string query, 
            string category = null, 
            int topK = 3, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Searching coding standards for category {Category}", category);

            try
            {
                var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query, cancellationToken);
                
                var searchRequest = new ChromaSearchRequest
                {
                    QueryEmbeddings = new[] { queryEmbedding },
                    NResults = topK,
                    Include = new[] { "metadatas", "documents", "distances" }
                };

                if (!string.IsNullOrEmpty(category))
                {
                    searchRequest.Where = new Dictionary<string, object>
                    {
                        ["category"] = category
                    };
                }

                var results = await SearchCollectionAsync(CodingStandardsCollection, searchRequest, cancellationToken);
                return ConvertToRetrievedContext(results, "CodingStandards");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching coding standards for category {Category}", category);
                return new List<RetrievedContext>();
            }
        }

        /// <summary>
        /// Searches for historical issues and their resolutions
        /// </summary>
        public async Task<List<RetrievedContext>> SearchHistoricalIssuesAsync(
            string query, 
            string projectId, 
            int topK = 5, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Searching historical issues for project {ProjectId}", projectId);

            try
            {
                var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query, cancellationToken);
                
                var searchRequest = new ChromaSearchRequest
                {
                    QueryEmbeddings = new[] { queryEmbedding },
                    NResults = topK,
                    Where = new Dictionary<string, object>
                    {
                        ["project_id"] = projectId
                    },
                    Include = new[] { "metadatas", "documents", "distances" }
                };

                var results = await SearchCollectionAsync(HistoricalIssuesCollection, searchRequest, cancellationToken);
                return ConvertToRetrievedContext(results, "HistoricalIssues");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching historical issues for project {ProjectId}", projectId);
                return new List<RetrievedContext>();
            }
        }

        /// <summary>
        /// Searches for team patterns and preferences
        /// </summary>
        public async Task<List<RetrievedContext>> SearchTeamPatternsAsync(
            string query, 
            string projectId, 
            int topK = 3, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Searching team patterns for project {ProjectId}", projectId);

            try
            {
                var queryEmbedding = await _embeddingService.GetEmbeddingAsync(query, cancellationToken);
                
                var searchRequest = new ChromaSearchRequest
                {
                    QueryEmbeddings = new[] { queryEmbedding },
                    NResults = topK,
                    Where = new Dictionary<string, object>
                    {
                        ["project_id"] = projectId
                    },
                    Include = new[] { "metadatas", "documents", "distances" }
                };

                var results = await SearchCollectionAsync(TeamPatternsCollection, searchRequest, cancellationToken);
                return ConvertToRetrievedContext(results, "TeamPatterns");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching team patterns for project {ProjectId}", projectId);
                return new List<RetrievedContext>();
            }
        }

        /// <summary>
        /// Stores a new document in the vector database
        /// </summary>
        public async Task StoreDocumentAsync(
            RAGDocument document, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Storing document {DocumentId} in collection {Collection}", document.Id, document.Collection);

            try
            {
                var embedding = await _embeddingService.GetEmbeddingAsync(document.Content, cancellationToken);
                
                var addRequest = new ChromaAddRequest
                {
                    Ids = new[] { document.Id },
                    Embeddings = new[] { embedding },
                    Documents = new[] { document.Content },
                    Metadatas = new[] { document.Metadata }
                };

                await AddToCollectionAsync(document.Collection, addRequest, cancellationToken);
                _logger.LogInformation("Successfully stored document {DocumentId}", document.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing document {DocumentId}", document.Id);
                throw;
            }
        }

        /// <summary>
        /// Initializes collections if they don't exist
        /// </summary>
        public async Task InitializeCollectionsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Initializing ChromaDB collections");

            var collections = new[]
            {
                CodePatternsCollection,
                CodingStandardsCollection,
                HistoricalIssuesCollection,
                TeamPatternsCollection
            };

            foreach (var collection in collections)
            {
                await EnsureCollectionExistsAsync(collection, cancellationToken);
            }

            _logger.LogInformation("ChromaDB collections initialized successfully");
        }

        /// <summary>
        /// Ensures a collection exists, creates it if not
        /// </summary>
        private async Task EnsureCollectionExistsAsync(string collectionName, CancellationToken cancellationToken)
        {
            try
            {
                // Check if collection exists
                var response = await _httpClient.GetAsync($"/api/v1/collections/{collectionName}", cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("Collection {CollectionName} already exists", collectionName);
                    return;
                }

                // Create collection
                var createRequest = new ChromaCreateCollectionRequest
                {
                    Name = collectionName,
                    Metadata = new Dictionary<string, object>
                    {
                        ["description"] = GetCollectionDescription(collectionName),
                        ["created_at"] = DateTime.UtcNow.ToString("O")
                    }
                };

                var json = JsonSerializer.Serialize(createRequest, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var createResponse = await _httpClient.PostAsync("/api/v1/collections", content, cancellationToken);

                if (createResponse.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Created collection {CollectionName}", collectionName);
                }
                else
                {
                    var errorContent = await createResponse.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Failed to create collection {CollectionName}: {Error}", 
                        collectionName, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring collection {CollectionName} exists", collectionName);
            }
        }

        /// <summary>
        /// Gets description for collection
        /// </summary>
        private string GetCollectionDescription(string collectionName)
        {
            return collectionName switch
            {
                CodePatternsCollection => "Code patterns, examples, and similar code structures",
                CodingStandardsCollection => "Coding standards, guidelines, and best practices",
                HistoricalIssuesCollection => "Historical bug reports, issues, and their resolutions",
                TeamPatternsCollection => "Team preferences, patterns, and development conventions",
                _ => "MCP Code Review RAG collection"
            };
        }

        /// <summary>
        /// Searches a specific collection
        /// </summary>
        private async Task<ChromaSearchResponse> SearchCollectionAsync(
            string collectionName, 
            ChromaSearchRequest request, 
            CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"/api/v1/collections/{collectionName}/query", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("ChromaDB search failed for collection {CollectionName}: {Error}", 
                    collectionName, errorContent);
                return new ChromaSearchResponse();
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ChromaSearchResponse>(responseJson, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            }) ?? new ChromaSearchResponse();
        }

        /// <summary>
        /// Adds documents to a collection
        /// </summary>
        private async Task AddToCollectionAsync(
            string collectionName, 
            ChromaAddRequest request, 
            CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"/api/v1/collections/{collectionName}/add", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to add to ChromaDB collection {collectionName}: {errorContent}");
            }
        }

        /// <summary>
        /// Converts ChromaDB response to RetrievedContext
        /// </summary>
        private List<RetrievedContext> ConvertToRetrievedContext(ChromaSearchResponse response, string source)
        {
            var results = new List<RetrievedContext>();

            if (response.Ids?.FirstOrDefault() == null) return results;

            var ids = response.Ids.First();
            var documents = response.Documents?.FirstOrDefault() ?? new string[0];
            var metadatas = response.Metadatas?.FirstOrDefault() ?? new Dictionary<string, object>[0];
            var distances = response.Distances?.FirstOrDefault() ?? new double[0];

            for (int i = 0; i < ids.Length; i++)
            {
                var similarity = distances.Length > i ? 1.0 - distances[i] : 0.0; // Convert distance to similarity
                
                results.Add(new RetrievedContext
                {
                    Id = ids[i],
                    Content = documents.Length > i ? documents[i] : string.Empty,
                    Similarity = similarity,
                    Metadata = metadatas.Length > i ? metadatas[i] : new Dictionary<string, object>(),
                    Source = source,
                    Timestamp = DateTime.UtcNow
                });
            }

            return results.Where(r => r.Similarity > 0.5).OrderByDescending(r => r.Similarity).ToList();
        }
    }

    #region ChromaDB DTOs

    public class ChromaDbConfig
    {
        public string BaseUrl { get; set; } = "http://localhost:8000";
        public string AuthToken { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
    }

    public class ChromaCreateCollectionRequest
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class ChromaSearchRequest
    {
        public float[][] QueryEmbeddings { get; set; } = Array.Empty<float[]>();
        public int NResults { get; set; } = 10;
        public Dictionary<string, object>? Where { get; set; }
        public string[] Include { get; set; } = Array.Empty<string>();
    }

    public class ChromaAddRequest
    {
        public string[] Ids { get; set; } = Array.Empty<string>();
        public float[][] Embeddings { get; set; } = Array.Empty<float[]>();
        public string[] Documents { get; set; } = Array.Empty<string>();
        public Dictionary<string, object>[] Metadatas { get; set; } = Array.Empty<Dictionary<string, object>>();
    }

    public class ChromaSearchResponse
    {
        public string[][]? Ids { get; set; }
        public string[][]? Documents { get; set; }
        public Dictionary<string, object>[][]? Metadatas { get; set; }
        public double[][]? Distances { get; set; }
    }

    #endregion
}