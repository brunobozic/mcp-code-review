using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mcp.CodeReview.RAG
{
    /// <summary>
    /// Interface for text embedding generation
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generates embeddings for a single text
        /// </summary>
        Task<float[]> GetEmbeddingAsync(
            string text, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates embeddings for multiple texts in batch
        /// </summary>
        Task<List<float[]>> GetEmbeddingsAsync(
            List<string> texts, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the dimension size of embeddings
        /// </summary>
        int EmbeddingDimension { get; }

        /// <summary>
        /// Preprocesses text for better embedding quality
        /// </summary>
        string PreprocessText(string text);
    }

    /// <summary>
    /// Configuration for embedding service
    /// </summary>
    public class EmbeddingConfig
    {
        public string Provider { get; set; } = "OpenAI"; // OpenAI, HuggingFace, etc.
        public string Model { get; set; } = "text-embedding-3-small";
        public string ApiKey { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";
        public int MaxTokens { get; set; } = 8191;
        public int BatchSize { get; set; } = 100;
    }
}