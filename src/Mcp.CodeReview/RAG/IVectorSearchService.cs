using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mcp.CodeReview.RAG
{
    /// <summary>
    /// Interface for vector-based search operations in the RAG system
    /// </summary>
    public interface IVectorSearchService
    {
        /// <summary>
        /// Searches for similar code patterns and issues
        /// </summary>
        Task<List<RetrievedContext>> SearchSimilarCodeAsync(
            string query, 
            string projectId, 
            int topK = 5, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for relevant coding standards and guidelines
        /// </summary>
        Task<List<RetrievedContext>> SearchCodingStandardsAsync(
            string query, 
            string category = null, 
            int topK = 3, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for historical issues and their resolutions
        /// </summary>
        Task<List<RetrievedContext>> SearchHistoricalIssuesAsync(
            string query, 
            string projectId, 
            int topK = 5, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for team patterns and preferences
        /// </summary>
        Task<List<RetrievedContext>> SearchTeamPatternsAsync(
            string query, 
            string projectId, 
            int topK = 3, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores a new document in the vector database
        /// </summary>
        Task StoreDocumentAsync(
            RAGDocument document, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Initializes collections if they don't exist
        /// </summary>
        Task InitializeCollectionsAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Retrieved context from vector search
    /// </summary>
    public class RetrievedContext
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double Similarity { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public string Source { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Document to be stored in RAG system
    /// </summary>
    public class RAGDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Collection { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}