using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mcp.CodeReview.RAG;

namespace Mcp.CodeReview.Services
{
    /// <summary>
    /// Background service to initialize RAG system on startup
    /// </summary>
    public class RagInitializationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RagInitializationService> _logger;

        public RagInitializationService(
            IServiceProvider serviceProvider,
            ILogger<RagInitializationService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting RAG system initialization");

            try
            {
                // Wait a bit for other services to start
                await Task.Delay(5000, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var vectorSearchService = scope.ServiceProvider.GetService<IVectorSearchService>();
                // var ragDataSeeder = scope.ServiceProvider.GetService<RagDataSeeder>(); // Temporarily disabled

                if (vectorSearchService == null)
                {
                    _logger.LogWarning("RAG services not registered, skipping initialization");
                    return;
                }

                // Initialize ChromaDB collections
                _logger.LogInformation("Initializing ChromaDB collections");
                await vectorSearchService.InitializeCollectionsAsync(stoppingToken);

                // Seed essential data if seeder is available
                // if (ragDataSeeder != null)
                // {
                //     _logger.LogInformation("Seeding essential RAG data");
                //     await ragDataSeeder.SeedEssentialDataAsync("default", stoppingToken);
                //     _logger.LogInformation("RAG data seeding completed");
                // }

                _logger.LogInformation("RAG system initialization completed successfully");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("RAG initialization cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RAG system");
                // Don't throw - allow the application to continue without RAG
            }
        }
    }
}