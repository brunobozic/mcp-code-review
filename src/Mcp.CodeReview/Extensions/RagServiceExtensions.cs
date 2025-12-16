using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mcp.CodeReview.RAG;

namespace Mcp.CodeReview.Extensions
{
    /// <summary>
    /// Extension methods for registering RAG services
    /// </summary>
    public static class RagServiceExtensions
    {
        /// <summary>
        /// Adds RAG services to the DI container
        /// </summary>
        public static IServiceCollection AddRagServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure ChromaDB settings
            services.Configure<ChromaDbConfig>(options =>
            {
                options.BaseUrl = configuration.GetValue<string>("ChromaDB:BaseUrl") ?? "http://localhost:19193";
                options.AuthToken = configuration.GetValue<string>("ChromaDB:AuthToken") ?? "";
                options.TimeoutSeconds = configuration.GetValue<int>("ChromaDB:TimeoutSeconds", 30);
            });

            // Configure OpenAI embedding settings
            services.Configure<EmbeddingConfig>(options =>
            {
                options.Provider = "OpenAI";
                options.Model = configuration.GetValue<string>("OpenAI:EmbeddingModel") ?? "text-embedding-3-small";
                options.ApiKey = configuration.GetValue<string>("CLAUDE_API_KEY") ?? 
                                configuration.GetValue<string>("OpenAI:ApiKey") ?? "";
                options.BaseUrl = configuration.GetValue<string>("OpenAI:BaseUrl") ?? "https://api.openai.com/v1";
                options.MaxTokens = configuration.GetValue<int>("OpenAI:MaxTokens", 8191);
                options.BatchSize = configuration.GetValue<int>("OpenAI:BatchSize", 100);
            });

            // Register HTTP client for ChromaDB
            services.AddHttpClient<ChromaDbVectorSearchService>((serviceProvider, client) =>
            {
                var config = serviceProvider.GetRequiredService<IOptions<ChromaDbConfig>>().Value;
                client.BaseAddress = new Uri(config.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
                
                if (!string.IsNullOrEmpty(config.AuthToken))
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.AuthToken}");
                }
            });

            // Register HTTP client for OpenAI
            services.AddHttpClient<OpenAiEmbeddingService>((serviceProvider, client) =>
            {
                var config = serviceProvider.GetRequiredService<IOptions<EmbeddingConfig>>().Value;
                client.BaseAddress = new Uri(config.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
                
                if (!string.IsNullOrEmpty(config.ApiKey))
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");
                }
            });

            // Register RAG services
            services.AddScoped<IEmbeddingService, OpenAiEmbeddingService>();
            services.AddScoped<IVectorSearchService, ChromaDbVectorSearchService>();
            services.AddScoped<RagDataSeeder>();

            return services;
        }

        /// <summary>
        /// Validates RAG configuration
        /// </summary>
        public static void ValidateRagConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var chromaDbUrl = configuration.GetValue<string>("ChromaDB:BaseUrl");
            var apiKey = configuration.GetValue<string>("CLAUDE_API_KEY") ?? 
                        configuration.GetValue<string>("OpenAI:ApiKey");

            if (string.IsNullOrEmpty(chromaDbUrl))
            {
                throw new InvalidOperationException("ChromaDB:BaseUrl configuration is required for RAG functionality");
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Warning: No API key configured for embeddings. RAG functionality will use fallback data only.");
            }
        }
    }
}