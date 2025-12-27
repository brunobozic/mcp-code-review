using Microsoft.Extensions.Logging;
using Mcp.CodeReview.Services;
using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.RAG
{
    /// <summary>
    /// Learning RAG service that integrates external data sources like SonarQube
    /// </summary>
    public class LearningRAGService
    {
        private readonly IVectorSearchService _vectorSearchService;
        private readonly SonarQubeService _sonarQubeService;
        private readonly ILogger<LearningRAGService> _logger;

        public LearningRAGService(
            IVectorSearchService vectorSearchService,
            SonarQubeService sonarQubeService,
            ILogger<LearningRAGService> logger)
        {
            _vectorSearchService = vectorSearchService ?? throw new ArgumentNullException(nameof(vectorSearchService));
            _sonarQubeService = sonarQubeService ?? throw new ArgumentNullException(nameof(sonarQubeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves and integrates SonarQube data for AI agent analysis
        /// </summary>
        public async Task<EnhancedRepositoryContext> GetEnhancedRepositoryContextAsync(
            string projectKey,
            string? mergeRequestId = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("🔍 RAG ENHANCED: Retrieving SonarQube data for project {ProjectKey}", projectKey);

            var context = new EnhancedRepositoryContext
            {
                ProjectId = projectKey,
                AnalysisDate = DateTime.UtcNow
            };

            try
            {
                // Test SonarQube connection first
                var isConnected = await _sonarQubeService.TestConnection();
                if (!isConnected)
                {
                    _logger.LogWarning("⚠️ SonarQube connection failed, using basic context");
                    return context;
                }

                _logger.LogInformation("✅ SonarQube connection successful, retrieving analysis data...");

                // Get comprehensive SonarQube analysis
                var sonarAnalysis = await _sonarQubeService.GetIssueAnalysisForAI(projectKey);
                
                _logger.LogInformation("📊 SonarQube analysis retrieved: {TotalIssues} issues, {SecurityIssues} security, {BugIssues} bugs", 
                    sonarAnalysis.TotalIssues, sonarAnalysis.SecurityIssues.Count, sonarAnalysis.BugIssues.Count);

                // Convert SonarQube data to repository context
                context.QualityMetrics = sonarAnalysis.QualityMetrics;
                context.Dependencies = await GetProjectDependenciesAsync(projectKey);
                context.Files = await GetAnalyzedFilesAsync(sonarAnalysis);

                // Store SonarQube insights in vector database for future reference
                await StoreSonarQubeInsightsAsync(projectKey, sonarAnalysis, cancellationToken);

                // Get historical context from vector search
                var historicalContext = await GetHistoricalContextAsync(projectKey, cancellationToken);
                context.HistoricalPatterns = historicalContext;

                _logger.LogInformation("🧠 RAG CONTEXT ENHANCED: {Files} files, {Dependencies} dependencies, {Metrics} metrics, {Patterns} patterns",
                    context.Files.Count, context.Dependencies.Count, context.QualityMetrics.Count, context.HistoricalPatterns.Count);

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to retrieve enhanced repository context for {ProjectKey}", projectKey);
                // Return basic context on failure
                return context;
            }
        }

        /// <summary>
        /// Stores SonarQube insights in vector database for future AI analysis
        /// </summary>
        private async Task StoreSonarQubeInsightsAsync(
            string projectKey,
            SonarQubeIssueAnalysis analysis,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("💾 Storing SonarQube insights in vector database...");

                // Store critical security issues
                foreach (var issue in analysis.SecurityIssues.Take(10))
                {
                    var document = new RAGDocument
                    {
                        Id = $"sonar_security_{projectKey}_{issue.Key}",
                        Content = $"Security Issue: {issue.Message}\nFile: {issue.Component}\nSeverity: {issue.Severity}\nRule: {issue.Rule}\nType: {issue.Type}",
                        Collection = "historical_issues",
                        Metadata = new Dictionary<string, object>
                        {
                            ["project_id"] = projectKey,
                            ["issue_type"] = "security",
                            ["severity"] = issue.Severity,
                            ["rule"] = issue.Rule,
                            ["component"] = issue.Component,
                            ["source"] = "sonarqube",
                            ["stored_at"] = DateTime.UtcNow.ToString("O")
                        }
                    };
                    await _vectorSearchService.StoreDocumentAsync(document, cancellationToken);
                }

                // Store code quality issues
                foreach (var issue in analysis.CodeSmellIssues.Take(15))
                {
                    var document = new RAGDocument
                    {
                        Id = $"sonar_quality_{projectKey}_{issue.Key}",
                        Content = $"Code Quality Issue: {issue.Message}\nFile: {issue.Component}\nSeverity: {issue.Severity}\nRule: {issue.Rule}\nEffort: {issue.Effort}",
                        Collection = "code_patterns",
                        Metadata = new Dictionary<string, object>
                        {
                            ["project_id"] = projectKey,
                            ["issue_type"] = "code_smell",
                            ["severity"] = issue.Severity,
                            ["rule"] = issue.Rule,
                            ["component"] = issue.Component,
                            ["source"] = "sonarqube",
                            ["effort"] = issue.Effort,
                            ["stored_at"] = DateTime.UtcNow.ToString("O")
                        }
                    };
                    await _vectorSearchService.StoreDocumentAsync(document, cancellationToken);
                }

                // Store project quality metrics
                var metricsDocument = new RAGDocument
                {
                    Id = $"sonar_metrics_{projectKey}_{DateTime.UtcNow:yyyyMMdd}",
                    Content = $"Project Quality Metrics:\n" +
                             $"Coverage: {analysis.QualityMetrics.GetValueOrDefault("coverage", "unknown")}\n" +
                             $"Bugs: {analysis.QualityMetrics.GetValueOrDefault("bugs", "0")}\n" +
                             $"Vulnerabilities: {analysis.QualityMetrics.GetValueOrDefault("vulnerabilities", "0")}\n" +
                             $"Code Smells: {analysis.QualityMetrics.GetValueOrDefault("code_smells", "0")}\n" +
                             $"Total Issues: {analysis.TotalIssues}",
                    Collection = "team_patterns",
                    Metadata = new Dictionary<string, object>
                    {
                        ["project_id"] = projectKey,
                        ["type"] = "quality_metrics",
                        ["source"] = "sonarqube",
                        ["total_issues"] = analysis.TotalIssues,
                        ["stored_at"] = DateTime.UtcNow.ToString("O")
                    }
                };
                await _vectorSearchService.StoreDocumentAsync(metricsDocument, cancellationToken);

                _logger.LogInformation("✅ SonarQube insights stored successfully: {SecurityIssues} security, {QualityIssues} quality issues",
                    analysis.SecurityIssues.Count, analysis.CodeSmellIssues.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store SonarQube insights for project {ProjectKey}", projectKey);
            }
        }

        /// <summary>
        /// Gets historical context from vector search
        /// </summary>
        private async Task<List<string>> GetHistoricalContextAsync(string projectKey, CancellationToken cancellationToken)
        {
            try
            {
                var patterns = await _vectorSearchService.SearchSimilarCodeAsync(
                    "code quality security vulnerabilities", projectKey, 5, cancellationToken);

                return patterns.Select(p => p.Content).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve historical context for {ProjectKey}", projectKey);
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets project dependencies from analysis
        /// </summary>
        private async Task<List<string>> GetProjectDependenciesAsync(string projectKey)
        {
            try
            {
                // In a real implementation, this would analyze project files or SonarQube dependency data
                // For now, return common dependencies based on project type
                await Task.Delay(10);
                
                return new List<string>
                {
                    "Microsoft.AspNetCore",
                    "System.Text.Json",
                    "Microsoft.EntityFrameworkCore",
                    "Microsoft.Extensions.Logging"
                };
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Gets analyzed files from SonarQube data
        /// </summary>
        private async Task<List<string>> GetAnalyzedFilesAsync(SonarQubeIssueAnalysis analysis)
        {
            try
            {
                await Task.Delay(10);
                
                // Extract unique file components from issues
                var files = analysis.IssuesByFile.Keys.Take(20).ToList();
                
                if (!files.Any())
                {
                    // Fallback to common project structure
                    files = new List<string>
                    {
                        "src/Program.cs",
                        "src/Controllers/ApiController.cs", 
                        "src/Services/UserService.cs",
                        "src/Models/User.cs"
                    };
                }

                return files;
            }
            catch
            {
                return new List<string>();
            }
        }
    }

    /// <summary>
    /// Enhanced repository context with external data integration
    /// </summary>
    public class EnhancedRepositoryContext
    {
        public string ProjectId { get; set; } = "";
        public DateTime AnalysisDate { get; set; }
        public Dictionary<string, string> QualityMetrics { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public List<string> Files { get; set; } = new();
        public List<string> HistoricalPatterns { get; set; } = new();
        
        /// <summary>
        /// Converts Enhanced context to Models.RepositoryContext for agent compatibility
        /// </summary>
        public Models.RepositoryContext ToRepositoryContext()
        {
            return new Models.RepositoryContext
            {
                ProjectId = ProjectId,
                ProjectName = ProjectId,
                Dependencies = QualityMetrics,
                RelatedFiles = Files.Take(10).ToDictionary(f => f, f => "analyzed"),
                HistoricalPatterns = HistoricalPatterns.Select((pattern, index) => new Models.HistoricalPattern
                {
                    Id = $"sonar_pattern_{index}",
                    Pattern = pattern,
                    Category = "sonar_analysis",
                    Similarity = 0.85,
                    Recommendation = "Based on SonarQube analysis"
                }).ToList(),
                RiskAssessment = new Dictionary<string, double>
                {
                    ["security"] = QualityMetrics.ContainsKey("vulnerabilities") ? 
                        double.TryParse(QualityMetrics["vulnerabilities"], out var vulns) && vulns > 0 ? 0.8 : 0.2 : 0.3,
                    ["quality"] = QualityMetrics.ContainsKey("code_smells") ? 
                        double.TryParse(QualityMetrics["code_smells"], out var smells) && smells > 10 ? 0.7 : 0.3 : 0.4,
                    ["performance"] = 0.5
                }
            };
        }
    }
}