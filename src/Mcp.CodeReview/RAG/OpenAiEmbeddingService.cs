using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mcp.CodeReview.RAG
{
    /// <summary>
    /// OpenAI implementation of embedding service
    /// </summary>
    public class OpenAiEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAiEmbeddingService> _logger;
        private readonly EmbeddingConfig _config;

        public int EmbeddingDimension => _config.Model switch
        {
            "text-embedding-3-small" => 1536,
            "text-embedding-3-large" => 3072,
            "text-embedding-ada-002" => 1536,
            _ => 1536
        };

        public OpenAiEmbeddingService(
            HttpClient httpClient,
            IOptions<EmbeddingConfig> config,
            ILogger<OpenAiEmbeddingService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config.Value ?? throw new ArgumentNullException(nameof(config));

            ConfigureHttpClient();
        }

        /// <summary>
        /// Configures HTTP client for OpenAI API
        /// </summary>
        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_config.BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "MCP-CodeReview-RAG/1.0");
        }

        /// <summary>
        /// Generates embeddings for a single text
        /// </summary>
        public async Task<float[]> GetEmbeddingAsync(
            string text, 
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new float[EmbeddingDimension];

            try
            {
                var preprocessedText = PreprocessText(text);
                _logger.LogDebug("Generating embedding for text of length {Length}", preprocessedText.Length);

                var request = new OpenAiEmbeddingRequest
                {
                    Model = _config.Model,
                    Input = preprocessedText
                };

                var response = await SendEmbeddingRequestAsync(request, cancellationToken);
                
                if (response?.Data?.FirstOrDefault()?.Embedding != null)
                {
                    return response.Data.First().Embedding;
                }

                _logger.LogWarning("No embedding returned from OpenAI API");
                return new float[EmbeddingDimension];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for text");
                return new float[EmbeddingDimension];
            }
        }

        /// <summary>
        /// Generates embeddings for multiple texts in batch
        /// </summary>
        public async Task<List<float[]>> GetEmbeddingsAsync(
            List<string> texts, 
            CancellationToken cancellationToken = default)
        {
            if (!texts?.Any() == true)
                return new List<float[]>();

            try
            {
                var results = new List<float[]>();
                var preprocessedTexts = texts.Select(PreprocessText).ToList();
                
                // Process in batches to respect API limits
                for (int i = 0; i < preprocessedTexts.Count; i += _config.BatchSize)
                {
                    var batch = preprocessedTexts.Skip(i).Take(_config.BatchSize).ToList();
                    _logger.LogDebug("Processing batch {BatchStart}-{BatchEnd} of {Total}", 
                        i + 1, Math.Min(i + _config.BatchSize, preprocessedTexts.Count), preprocessedTexts.Count);

                    var request = new OpenAiEmbeddingRequest
                    {
                        Model = _config.Model,
                        Input = batch.ToArray()
                    };

                    var response = await SendEmbeddingRequestAsync(request, cancellationToken);
                    
                    if (response?.Data != null)
                    {
                        results.AddRange(response.Data.OrderBy(d => d.Index).Select(d => d.Embedding));
                    }

                    // Add small delay between batches to respect rate limits
                    if (i + _config.BatchSize < preprocessedTexts.Count)
                    {
                        await Task.Delay(100, cancellationToken);
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating batch embeddings");
                return texts.Select(_ => new float[EmbeddingDimension]).ToList();
            }
        }

        /// <summary>
        /// Preprocesses text for better embedding quality
        /// </summary>
        public string PreprocessText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // 1. Clean up code-specific noise
            var cleaned = text;

            // Remove excessive whitespace
            cleaned = Regex.Replace(cleaned, @"\s+", " ");

            // Clean up common code patterns that add noise
            cleaned = Regex.Replace(cleaned, @"//.*$", "", RegexOptions.Multiline); // Single-line comments
            cleaned = Regex.Replace(cleaned, @"/\*.*?\*/", "", RegexOptions.Singleline); // Multi-line comments
            cleaned = Regex.Replace(cleaned, @"^\s*using\s+.*?;", "", RegexOptions.Multiline); // Using statements
            cleaned = Regex.Replace(cleaned, @"^\s*import\s+.*?;", "", RegexOptions.Multiline); // Import statements

            // Normalize common code constructs
            cleaned = Regex.Replace(cleaned, @"\b(public|private|protected|internal)\s+", ""); // Access modifiers
            cleaned = Regex.Replace(cleaned, @"\b(static|readonly|const)\s+", ""); // Keywords
            cleaned = Regex.Replace(cleaned, @"\b(async|await)\s+", ""); // Async keywords

            // 2. Truncate if too long (leave some buffer for API limits)
            if (cleaned.Length > _config.MaxTokens * 3) // Rough token estimation: ~4 chars per token
            {
                cleaned = cleaned.Substring(0, _config.MaxTokens * 3);
                // Try to end at a sentence or line break
                var lastSentence = cleaned.LastIndexOfAny(new[] { '.', '\n', ';' });
                if (lastSentence > cleaned.Length / 2) // Only if we don't lose too much
                {
                    cleaned = cleaned.Substring(0, lastSentence + 1);
                }
            }

            // 3. Final cleanup
            cleaned = cleaned.Trim();

            _logger.LogTrace("Preprocessed text from {OriginalLength} to {CleanedLength} characters", 
                text.Length, cleaned.Length);

            return cleaned;
        }

        /// <summary>
        /// Sends embedding request to OpenAI API
        /// </summary>
        private async Task<OpenAiEmbeddingResponse> SendEmbeddingRequestAsync(
            OpenAiEmbeddingRequest request, 
            CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("/embeddings", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("OpenAI embedding request failed: {StatusCode} - {Error}", 
                    response.StatusCode, errorContent);
                
                // Try to parse OpenAI error format
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<OpenAiErrorResponse>(errorContent);
                    throw new InvalidOperationException($"OpenAI API Error: {errorResponse?.Error?.Message ?? errorContent}");
                }
                catch (JsonException)
                {
                    throw new InvalidOperationException($"OpenAI API Error: {response.StatusCode} - {errorContent}");
                }
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            
            var embeddingResponse = JsonSerializer.Deserialize<OpenAiEmbeddingResponse>(responseJson, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });

            _logger.LogDebug("Successfully generated {Count} embeddings, used {Tokens} tokens", 
                embeddingResponse?.Data?.Length ?? 0, embeddingResponse?.Usage?.TotalTokens ?? 0);

            return embeddingResponse ?? new OpenAiEmbeddingResponse();
        }
    }

    #region OpenAI DTOs

    public class OpenAiEmbeddingRequest
    {
        public string Model { get; set; } = string.Empty;
        public object Input { get; set; } = string.Empty; // Can be string or string[]
        public string? User { get; set; }
    }

    public class OpenAiEmbeddingResponse
    {
        public string Object { get; set; } = string.Empty;
        public OpenAiEmbeddingData[] Data { get; set; } = Array.Empty<OpenAiEmbeddingData>();
        public string Model { get; set; } = string.Empty;
        public OpenAiUsage? Usage { get; set; }
    }

    public class OpenAiEmbeddingData
    {
        public string Object { get; set; } = string.Empty;
        public int Index { get; set; }
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }

    public class OpenAiUsage
    {
        public int PromptTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    public class OpenAiErrorResponse
    {
        public OpenAiError? Error { get; set; }
    }

    public class OpenAiError
    {
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Code { get; set; }
    }

    #endregion
}