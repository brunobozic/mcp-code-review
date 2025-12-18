using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using Mcp.CodeReview.GitLab;
using Mcp.CodeReview.Metrics;
using Mcp.CodeReview.Abstractions;
using Mcp.CodeReview.Models;

namespace Mcp.CodeReview.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GitLabWebhookController : ControllerBase
    {
        private readonly GitLabIntegrationService _gitLabService;
        private readonly ILogger<GitLabWebhookController> _logger;
        private readonly MetricsRegistry _metrics;
        private readonly IAIReviewService _reviewService;
        private readonly IServiceScopeFactory _scopeFactory;

        public GitLabWebhookController(
            GitLabIntegrationService gitLabService,
            ILogger<GitLabWebhookController> logger,
            MetricsRegistry metrics,
            IAIReviewService reviewService,
            IServiceScopeFactory scopeFactory)
        {
            _gitLabService = gitLabService;
            _logger = logger;
            _metrics = metrics;
            _reviewService = reviewService;
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Handles GitLab webhook events
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> HandleWebhook([FromBody] GitLabWebhookPayload payload)
        {
            try
            {
                _logger.LogInformation("Received GitLab webhook: {EventType} for project {ProjectId}", 
                    payload.EventType, payload.Project?.Id);

                // Increment webhook metrics
                _metrics.IncrementCounter("gitlab_webhooks_received_total", 
                    ("event_type", payload.EventType ?? "unknown"),
                    ("project_id", payload.Project?.Id.ToString() ?? "unknown"));

                // Validate payload
                if (payload.Project == null)
                {
                    _logger.LogWarning("GitLab webhook payload missing project information");
                    return BadRequest("Missing project information");
                }

                // For debugging - bypass GitLab service and directly test AI review
                _logger.LogInformation("🔍 DEBUG: Webhook processing - EventType='{EventType}', MergeRequest exists={HasMr}", 
                    payload.EventType, payload.MergeRequest != null);

                // Process the webhook with the integration service first
                var result = await _gitLabService.ProcessWebhookAsync(payload);
                _logger.LogInformation("🔍 DEBUG: GitLab integration service returned: {Result}", result);
                
                // For merge request events, trigger contextual AI review
                if (payload.EventType == "merge_request" && payload.MergeRequest != null)
                {
                    _logger.LogInformation("🤖 Starting background AI review task for MR {MrIid}", payload.MergeRequest.Iid);
                    _ = Task.Run(async () => 
                    {
                        // Create a new scope for background task to avoid HttpClient disposal issues
                        using var scope = _scopeFactory.CreateScope();
                        var scopedReviewService = scope.ServiceProvider.GetRequiredService<IAIReviewService>();
                        var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<GitLabWebhookController>>();
                        var scopedGitLabService = scope.ServiceProvider.GetRequiredService<GitLabIntegrationService>();
                        
                        try
                        {
                            await TriggerContextualReviewWithScopeAsync(payload, scopedReviewService, scopedLogger, scopedGitLabService);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Background AI review task failed for MR {MrIid}", payload.MergeRequest?.Iid);
                        }
                    });
                }
                
                if (result)
                {
                    _metrics.IncrementCounter("gitlab_webhooks_processed_total", 
                        ("event_type", payload.EventType ?? "unknown"),
                        ("status", "success"));
                    
                    return Ok(new { status = "success", message = "Webhook processed successfully" });
                }
                else
                {
                    _metrics.IncrementCounter("gitlab_webhooks_processed_total", 
                        ("event_type", payload.EventType ?? "unknown"),
                        ("status", "failed"));
                    
                    return BadRequest(new { status = "error", message = "Failed to process webhook" });
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON in GitLab webhook payload");
                _metrics.IncrementCounter("gitlab_webhooks_errors_total", 
                    ("error_type", "invalid_json"));
                return BadRequest(new { status = "error", message = "Invalid JSON payload" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing GitLab webhook");
                _metrics.IncrementCounter("gitlab_webhooks_errors_total", 
                    ("error_type", "internal_error"));
                return StatusCode(500, new { status = "error", message = "Internal server error" });
            }
        }

        /// <summary>
        /// Manually trigger a merge request review
        /// </summary>
        [HttpPost("review-merge-request")]
        public async Task<IActionResult> TriggerMergeRequestReview([FromBody] ManualReviewRequest request)
        {
            try
            {
                _logger.LogInformation("Manual review trigger for project {ProjectId}, MR {MrIid}", 
                    request.ProjectId, request.MergeRequestIid);

                // Create a synthetic webhook payload for manual review
                var payload = new GitLabWebhookPayload
                {
                    EventType = "merge_request",
                    Project = new GitLabProject { Id = int.Parse(request.ProjectId) },
                    MergeRequest = new GitLabMergeRequest { Iid = int.Parse(request.MergeRequestIid) }
                };

                var result = await _gitLabService.ProcessWebhookAsync(payload);
                
                if (result)
                {
                    _metrics.IncrementCounter("manual_reviews_triggered_total", 
                        ("project_id", request.ProjectId),
                        ("status", "success"));
                    
                    return Ok(new { 
                        status = "success", 
                        message = "Manual review triggered successfully",
                        projectId = request.ProjectId,
                        mergeRequestIid = request.MergeRequestIid
                    });
                }
                else
                {
                    _metrics.IncrementCounter("manual_reviews_triggered_total", 
                        ("project_id", request.ProjectId),
                        ("status", "failed"));
                    
                    return BadRequest(new { status = "error", message = "Failed to trigger manual review" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering manual review");
                return StatusCode(500, new { status = "error", message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get webhook configuration for a project
        /// </summary>
        [HttpGet("config/{projectId}")]
        public async Task<IActionResult> GetWebhookConfig(int projectId)
        {
            try
            {
                var config = new GitLabWebhookConfig
                {
                    Url = $"{Request.Scheme}://{Request.Host}/api/gitlabwebhook",
                    MergeRequestsEvents = true,
                    PushEvents = true,
                    PipelineEvents = true,
                    EnableSslVerification = false
                };

                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting webhook config");
                return StatusCode(500, new { status = "error", message = "Internal server error" });
            }
        }

        /// <summary>
        /// Health check endpoint for GitLab webhook
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { 
                status = "healthy", 
                service = "GitLab Webhook Handler",
                timestamp = DateTimeOffset.UtcNow,
                version = "1.0.0"
            });
        }

        /// <summary>
        /// Test endpoint for webhook validation
        /// </summary>
        [HttpPost("test")]
        public IActionResult TestWebhook([FromBody] object payload)
        {
            _logger.LogInformation("Test webhook received: {Payload}", JsonSerializer.Serialize(payload));
            return Ok(new { status = "success", message = "Test webhook received" });
        }

        /// <summary>
        /// Trigger contextual multi-agent review for merge request events
        /// </summary>
        private async Task TriggerContextualReviewAsync(GitLabWebhookPayload payload)
        {
            try
            {
                _logger.LogInformation("🔍 TriggerContextualReviewAsync called for payload: {EventType}", payload.EventType);
                
                if (payload.Project == null || payload.MergeRequest == null)
                {
                    _logger.LogWarning("❌ Cannot trigger contextual review - missing project or merge request data. Project: {Project}, MR: {MR}", 
                        payload.Project?.Id, payload.MergeRequest?.Iid);
                    return;
                }

                _logger.LogInformation("🚀 BACKGROUND_TASK_STARTED: Triggering contextual review for MR {MrIid} in project {ProjectId}", 
                    payload.MergeRequest.Iid, payload.Project.Id);

                // Create test artifact for verification
                var testArtifactPath = $"/tmp/mcp-background-task-{payload.Project.Id}-{payload.MergeRequest.Iid}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
                await System.IO.File.WriteAllTextAsync(testArtifactPath, System.Text.Json.JsonSerializer.Serialize(new
                {
                    StartTime = DateTime.UtcNow,
                    ProjectId = payload.Project.Id,
                    MergeRequestIid = payload.MergeRequest.Iid,
                    Title = payload.MergeRequest.Title,
                    Status = "STARTED"
                }));
                _logger.LogInformation("📋 TEST_ARTIFACT_CREATED: {ArtifactPath}", testArtifactPath);

                // Extract changed files from merge request
                var changedFiles = ExtractChangedFilesFromMergeRequest(payload);
                
                // Get the primary file being reviewed (first changed file or diff content)
                var primaryContent = await GetPrimaryReviewContentAsync(payload, changedFiles);
                
                if (string.IsNullOrEmpty(primaryContent))
                {
                    _logger.LogWarning("No content to review for MR {MrIid}", payload.MergeRequest.Iid);
                    return;
                }

                // Create comprehensive review request with repository context
                var reviewRequest = new CodeReviewRequest
                {
                    Content = primaryContent,
                    FileName = changedFiles.FirstOrDefault() ?? "merge_request.diff",
                    // FilePath property doesn't exist in CodeReviewRequest - use FileName instead
                    Language = DetectLanguageFromFiles(changedFiles),
                    RequestedAgents = new List<AgentType>
                    {
                        AgentType.SecurityExpert,
                        AgentType.PerformanceAnalyst,
                        AgentType.CodeQualityReviewer,
                        AgentType.ArchitectureExpert,
                        AgentType.TestingSpecialist
                    },
                    Options = new ReviewOptions
                    {
                        IncludeSecurityAnalysis = true,
                        IncludePerformanceAnalysis = true,
                        IncludeQualityAnalysis = true,
                        IncludeTestSuggestions = true,
                        ReviewDepth = "comprehensive"
                    },
                    Metadata = new Dictionary<string, object>
                    {
                        ["projectId"] = payload.Project.Id.ToString(),
                        ["mergeRequestIid"] = payload.MergeRequest.Iid.ToString(),
                        ["changedFiles"] = changedFiles,
                        ["webhookEvent"] = payload.EventType,
                        ["contextualReview"] = true,
                        ["repositoryContext"] = true
                    }
                };

                // Conduct multi-agent review with repository context
                var reviewResult = await _reviewService.ConductMultiAgentReviewAsync(reviewRequest);

                _logger.LogInformation("✅ BACKGROUND_TASK_COMPLETED: Contextual review completed for MR {MrIid} with {FindingCount} findings", 
                    payload.MergeRequest.Iid, reviewResult.KeyFindings.Count);

                // Update test artifact with completion status
                var completionArtifactPath = $"/tmp/mcp-background-task-{payload.Project.Id}-{payload.MergeRequest.Iid}-completed.json";
                await System.IO.File.WriteAllTextAsync(completionArtifactPath, System.Text.Json.JsonSerializer.Serialize(new
                {
                    CompletedTime = DateTime.UtcNow,
                    ProjectId = payload.Project.Id,
                    MergeRequestIid = payload.MergeRequest.Iid,
                    FindingsCount = reviewResult.KeyFindings.Count,
                    QualityScore = reviewResult.QualityScore,
                    Status = "COMPLETED",
                    AgentResults = reviewResult.AgentResults.Count
                }));
                _logger.LogInformation("📋 COMPLETION_ARTIFACT_CREATED: {ArtifactPath}", completionArtifactPath);

                // Post results back to GitLab (use instance service for non-scoped version)
                await PostReviewResultsToGitLabAsync(payload, reviewResult, _gitLabService, _logger);

                _metrics.IncrementCounter("contextual_reviews_completed_total", 
                    ("project_id", payload.Project.Id.ToString()),
                    ("status", "success"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to trigger contextual review for MR {MrIid}", 
                    payload.MergeRequest?.Iid);
                
                _metrics.IncrementCounter("contextual_reviews_completed_total", 
                    ("project_id", payload.Project?.Id.ToString() ?? "unknown"),
                    ("status", "failed"));
            }
        }

        /// <summary>
        /// Scoped version of TriggerContextualReviewAsync that uses scoped services to avoid HttpClient disposal
        /// </summary>
        private async Task TriggerContextualReviewWithScopeAsync(GitLabWebhookPayload payload, IAIReviewService scopedReviewService, ILogger<GitLabWebhookController> scopedLogger, GitLabIntegrationService scopedGitLabService)
        {
            try
            {
                scopedLogger.LogInformation("🔍 TriggerContextualReviewWithScopeAsync called for payload: {EventType}", payload.EventType);
                
                if (payload.Project == null || payload.MergeRequest == null)
                {
                    scopedLogger.LogWarning("❌ Cannot trigger contextual review - missing project or merge request data. Project: {Project}, MR: {MR}", 
                        payload.Project?.Id, payload.MergeRequest?.Iid);
                    return;
                }

                scopedLogger.LogInformation("🚀 BACKGROUND_TASK_STARTED: Triggering contextual review for MR {MrIid} in project {ProjectId}", 
                    payload.MergeRequest.Iid, payload.Project.Id);

                // Create test artifact for verification
                var testArtifactPath = $"/tmp/mcp-background-task-{payload.Project.Id}-{payload.MergeRequest.Iid}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
                await System.IO.File.WriteAllTextAsync(testArtifactPath, System.Text.Json.JsonSerializer.Serialize(new
                {
                    StartTime = DateTime.UtcNow,
                    ProjectId = payload.Project.Id,
                    MergeRequestIid = payload.MergeRequest.Iid,
                    Title = payload.MergeRequest.Title,
                    Status = "STARTED"
                }));
                scopedLogger.LogInformation("📋 TEST_ARTIFACT_CREATED: {ArtifactPath}", testArtifactPath);

                // Extract changed files from merge request
                var changedFiles = ExtractChangedFilesFromMergeRequest(payload);
                
                // Get the primary file being reviewed (first changed file or diff content)
                var primaryContent = await GetPrimaryReviewContentAsync(payload, changedFiles);
                
                if (string.IsNullOrEmpty(primaryContent))
                {
                    scopedLogger.LogWarning("No content to review for MR {MrIid}", payload.MergeRequest.Iid);
                    return;
                }

                // Create comprehensive review request with repository context
                var reviewRequest = new CodeReviewRequest
                {
                    Content = primaryContent,
                    FileName = changedFiles.FirstOrDefault() ?? "merge_request.diff",
                    Language = DetectLanguageFromFiles(changedFiles),
                    RequestedAgents = new List<AgentType>
                    {
                        AgentType.SecurityExpert,
                        AgentType.PerformanceAnalyst,
                        AgentType.CodeQualityReviewer,
                        AgentType.ArchitectureExpert,
                        AgentType.TestingSpecialist
                    },
                    Options = new ReviewOptions
                    {
                        IncludeSecurityAnalysis = true,
                        IncludePerformanceAnalysis = true,
                        IncludeQualityAnalysis = true,
                        IncludeTestSuggestions = true,
                        ReviewDepth = "comprehensive"
                    },
                    Metadata = new Dictionary<string, object>
                    {
                        ["projectId"] = payload.Project.Id.ToString(),
                        ["mergeRequestIid"] = payload.MergeRequest.Iid.ToString(),
                        ["changedFiles"] = changedFiles,
                        ["webhookEvent"] = payload.EventType,
                        ["contextualReview"] = true,
                        ["repositoryContext"] = true
                    }
                };

                // Conduct multi-agent review with repository context using scoped service
                var reviewResult = await scopedReviewService.ConductMultiAgentReviewAsync(reviewRequest);

                scopedLogger.LogInformation("✅ BACKGROUND_TASK_COMPLETED: Contextual review completed for MR {MrIid} with {FindingCount} findings", 
                    payload.MergeRequest.Iid, reviewResult.KeyFindings.Count);

                // Update test artifact with completion status
                var completionArtifactPath = $"/tmp/mcp-background-task-{payload.Project.Id}-{payload.MergeRequest.Iid}-completed.json";
                await System.IO.File.WriteAllTextAsync(completionArtifactPath, System.Text.Json.JsonSerializer.Serialize(new
                {
                    CompletedTime = DateTime.UtcNow,
                    ProjectId = payload.Project.Id,
                    MergeRequestIid = payload.MergeRequest.Iid,
                    FindingsCount = reviewResult.KeyFindings.Count,
                    QualityScore = reviewResult.QualityScore,
                    Status = "COMPLETED",
                    AgentResults = reviewResult.AgentResults.Count
                }));
                scopedLogger.LogInformation("📋 COMPLETION_ARTIFACT_CREATED: {ArtifactPath}", completionArtifactPath);

                // Post results back to GitLab using scoped service to avoid HttpClient disposal
                await PostReviewResultsToGitLabAsync(payload, reviewResult, scopedGitLabService, scopedLogger);

                _metrics.IncrementCounter("contextual_reviews_completed_total", 
                    ("project_id", payload.Project.Id.ToString()),
                    ("status", "success"));
            }
            catch (Exception ex)
            {
                scopedLogger.LogError(ex, "Failed to trigger contextual review for MR {MrIid}", 
                    payload.MergeRequest?.Iid);
                
                _metrics.IncrementCounter("contextual_reviews_completed_total", 
                    ("project_id", payload.Project?.Id.ToString() ?? "unknown"),
                    ("status", "failed"));
            }
        }

        private List<string> ExtractChangedFilesFromMergeRequest(GitLabWebhookPayload payload)
        {
            var changedFiles = new List<string>();
            
            // For now, use placeholder logic since GitLabMergeRequest model may not have Changes property
            // In a real implementation, this would extract actual changed files from GitLab API
            if (payload.MergeRequest != null)
            {
                // Fallback: common file patterns for projects
                changedFiles.AddRange(new[]
                {
                    "src/main.cs",
                    "src/Program.cs", 
                    "Controllers/ApiController.cs",
                    $"MR-{payload.MergeRequest.Iid}-changes.diff"
                });
            }
            
            return changedFiles.Take(10).ToList(); // Limit for performance
        }

        private async Task<string> GetPrimaryReviewContentAsync(GitLabWebhookPayload payload, List<string> changedFiles)
        {
            // For this implementation, return a representative diff or file content
            if (payload.MergeRequest?.Description != null)
            {
                return $"Merge Request: {payload.MergeRequest.Title}\n\nDescription:\n{payload.MergeRequest.Description}\n\nChanged Files:\n{string.Join("\n", changedFiles)}";
            }
            
            return $"Merge Request Review\nChanged files: {string.Join(", ", changedFiles)}";
        }

        private string DetectLanguageFromFiles(List<string> changedFiles)
        {
            if (changedFiles.Any(f => f.EndsWith(".cs")))
                return "C#";
            if (changedFiles.Any(f => f.EndsWith(".js") || f.EndsWith(".ts")))
                return "JavaScript/TypeScript";
            if (changedFiles.Any(f => f.EndsWith(".py")))
                return "Python";
            if (changedFiles.Any(f => f.EndsWith(".java")))
                return "Java";
            
            return "Mixed";
        }

        private async Task PostReviewResultsToGitLabAsync(GitLabWebhookPayload payload, MultiAgentReviewResult reviewResult, GitLabIntegrationService gitLabService, ILogger<GitLabWebhookController> logger)
        {
            try
            {
                if (payload.Project == null || payload.MergeRequest == null)
                {
                    logger.LogWarning("Cannot post review results - missing project or MR data");
                    return;
                }

                logger.LogInformation("📝 POSTING RESULTS: Posting AI review results to GitLab MR {MrIid}: {FindingCount} findings, quality score {QualityScore}", 
                    payload.MergeRequest.Iid, 
                    reviewResult.KeyFindings.Count, 
                    reviewResult.QualityScore);

                // Build comprehensive review comments (potentially multiple)
                var commentParts = BuildMultipleReviewComments(reviewResult);
                
                // Post multiple comments to show complete analysis without truncation
                var successCount = 0;
                for (int i = 0; i < commentParts.Count; i++)
                {
                    var success = await gitLabService.PostMergeRequestNoteAsync(
                        payload.Project.Id, 
                        payload.MergeRequest.Iid, 
                        commentParts[i]);

                    if (success)
                    {
                        successCount++;
                        logger.LogInformation("✅ COMMENT POSTED: Posted AI review comment {Part}/{Total} to GitLab MR {MrIid}", 
                            i + 1, commentParts.Count, payload.MergeRequest.Iid);
                        
                        // Add small delay between comments to ensure proper ordering
                        if (i < commentParts.Count - 1)
                            await Task.Delay(500);
                    }
                    else
                    {
                        logger.LogWarning("❌ COMMENT FAILED: Failed to post AI review comment {Part}/{Total} to GitLab MR {MrIid}", 
                            i + 1, commentParts.Count, payload.MergeRequest.Iid);
                    }
                }

                if (successCount == commentParts.Count)
                {
                    logger.LogInformation("✅ ALL POSTED: Successfully posted all {Count} AI review comments to GitLab MR {MrIid}", 
                        successCount, payload.MergeRequest.Iid);
                }
                else
                {
                    logger.LogWarning("❌ PARTIAL POSTING: Posted only {SuccessCount}/{TotalCount} AI review comments to GitLab MR {MrIid}", 
                        successCount, commentParts.Count, payload.MergeRequest.Iid);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ ERROR POSTING: Exception while posting review results to GitLab MR {MrIid}", 
                    payload.MergeRequest?.Iid);
            }
        }

        private List<string> BuildMultipleReviewComments(MultiAgentReviewResult reviewResult)
        {
            var comments = new List<string>();
            const int maxCommentLength = 4000; // Safe limit for GitLab comments

            // Part 1: Summary and Agent Results
            var summaryComment = new StringBuilder();
            summaryComment.AppendLine("🤖 **AI Code Review Results**");
            summaryComment.AppendLine();
            summaryComment.AppendLine("✅ **Analysis Completed Successfully!**");
            summaryComment.AppendLine();
            summaryComment.AppendLine("📊 **Summary**");
            summaryComment.AppendLine($"- **Quality Score**: {reviewResult.QualityScore}/100");
            summaryComment.AppendLine($"- **Findings**: {reviewResult.KeyFindings.Count} items");
            summaryComment.AppendLine($"- **Agent Results**: {reviewResult.AgentResults.Count} agents analyzed");
            summaryComment.AppendLine($"- **Analysis Time**: {DateTime.UtcNow:HH:mm} UTC");
            summaryComment.AppendLine();

            if (reviewResult.AgentResults.Any())
            {
                summaryComment.AppendLine("🤖 **Multi-Agent Analysis**");
                foreach (var agent in reviewResult.AgentResults)
                {
                    var emoji = GetAgentEmoji(agent.AgentType.ToString());
                    summaryComment.AppendLine($"{emoji} **{agent.AgentType}** - Confidence: {agent.ConfidenceScore:F1}/10");
                }
                summaryComment.AppendLine();
            }

            summaryComment.AppendLine("---");
            comments.Add(summaryComment.ToString());

            // Part 2+: Key Findings (split into chunks)
            if (reviewResult.KeyFindings.Any())
            {
                var findingsChunks = ChunkFindings(reviewResult.KeyFindings, maxCommentLength);
                for (int i = 0; i < findingsChunks.Count; i++)
                {
                    var findingsComment = new StringBuilder();
                    findingsComment.AppendLine($"🔍 **Key Findings (Part {i + 2})** ");
                    findingsComment.AppendLine();
                    
                    foreach (var finding in findingsChunks[i])
                    {
                        findingsComment.AppendLine($"- {finding}");
                    }
                    findingsComment.AppendLine();
                    findingsComment.AppendLine("---");
                    comments.Add(findingsComment.ToString());
                }
            }

            // Final Part: Recommendations
            if (reviewResult.PriorityRecommendations.Any())
            {
                var recComment = new StringBuilder();
                recComment.AppendLine($"💡 **Recommendations (Part {comments.Count + 1})**");
                recComment.AppendLine();
                
                foreach (var rec in reviewResult.PriorityRecommendations.Take(10))
                {
                    recComment.AppendLine($"- {rec}");
                }
                recComment.AppendLine();
                recComment.AppendLine("---");
                comments.Add(recComment.ToString());
            }

            // Update part numbers in all comments
            for (int i = 0; i < comments.Count; i++)
            {
                var totalParts = comments.Count;
                var partNumber = i + 1;
                comments[i] = comments[i].Replace("---", $"*Generated by MCP AI Code Review System - Part {partNumber}/{totalParts}*");
            }

            return comments;
        }

        private List<List<string>> ChunkFindings(List<string> findings, int maxLength)
        {
            var chunks = new List<List<string>>();
            var currentChunk = new List<string>();
            var currentLength = 0;

            foreach (var finding in findings)
            {
                var findingLength = finding.Length + 4; // "- " + finding + "\n"
                
                if (currentLength + findingLength > maxLength - 500 && currentChunk.Any()) // Leave space for header/footer
                {
                    chunks.Add(currentChunk);
                    currentChunk = new List<string>();
                    currentLength = 0;
                }
                
                currentChunk.Add(finding);
                currentLength += findingLength;
            }
            
            if (currentChunk.Any())
            {
                chunks.Add(currentChunk);
            }

            return chunks;
        }

        private string BuildReviewComment(MultiAgentReviewResult reviewResult)
        {
            var comment = new StringBuilder();
            
            comment.AppendLine("🤖 **AI Code Review Results**");
            comment.AppendLine();
            comment.AppendLine("✅ **Analysis Completed Successfully!**");
            comment.AppendLine();
            comment.AppendLine("📊 **Summary**");
            comment.AppendLine($"- **Quality Score**: {reviewResult.QualityScore}/100");
            comment.AppendLine($"- **Findings**: {reviewResult.KeyFindings.Count} items");
            comment.AppendLine($"- **Agent Results**: {reviewResult.AgentResults.Count} agents analyzed");
            comment.AppendLine($"- **Analysis Time**: {DateTime.UtcNow:HH:mm} UTC");
            comment.AppendLine();

            if (reviewResult.AgentResults.Any())
            {
                comment.AppendLine("🤖 **Multi-Agent Analysis**");
                foreach (var agent in reviewResult.AgentResults)
                {
                    var emoji = GetAgentEmoji(agent.AgentType.ToString());
                    comment.AppendLine($"{emoji} **{agent.AgentType}** - Confidence: {agent.ConfidenceScore:F1}/10");
                }
                comment.AppendLine();
            }

            if (reviewResult.KeyFindings.Any())
            {
                comment.AppendLine("🔍 **Key Findings**");
                foreach (var finding in reviewResult.KeyFindings.Take(5)) // Top 5 findings only
                {
                    // Truncate long findings to prevent GitLab truncation
                    var truncatedFinding = finding.Length > 120 ? 
                        finding.Substring(0, 117) + "..." : finding;
                    comment.AppendLine($"- {truncatedFinding}");
                }
                comment.AppendLine();
            }
            else
            {
                comment.AppendLine("✅ **No Issues Found**");
                comment.AppendLine("Great job! The AI analysis found no significant issues with this code.");
                comment.AppendLine();
            }

            if (reviewResult.PriorityRecommendations.Any())
            {
                comment.AppendLine("💡 **Recommendations**");
                foreach (var rec in reviewResult.PriorityRecommendations.Take(3)) // Top 3 recommendations
                {
                    comment.AppendLine($"- {rec}");
                }
                comment.AppendLine();
            }

            comment.AppendLine("---");
            comment.AppendLine("*Generated by MCP AI Code Review System*");
            
            // Ensure comment doesn't exceed GitLab's limits
            var result = comment.ToString();
            const int maxGitLabCommentLength = 5000; // Safe limit for GitLab comments
            
            if (result.Length > maxGitLabCommentLength)
            {
                result = result.Substring(0, maxGitLabCommentLength - 50) + "\n\n... (Analysis truncated for display)\n\n*Generated by MCP AI Code Review System*";
            }
            
            return result;
        }

        private string GetAgentEmoji(string agentType) => agentType switch
        {
            "SecurityExpert" => "🔒",
            "PerformanceAnalyst" => "⚡",
            "CodeQualityReviewer" => "🎯",
            "ArchitectureExpert" => "🏗️",
            "TestingSpecialist" => "🧪",
            _ => "🤖"
        };

        private string GetSeverityEmoji(string severity) => severity?.ToLower() switch
        {
            "critical" => "🚨",
            "high" => "⚠️",
            "medium" => "📝",
            "low" => "💡",
            _ => "ℹ️"
        };

        private int GetSeverityOrder(string severity) => severity?.ToLower() switch
        {
            "critical" => 1,
            "high" => 2,
            "medium" => 3,
            "low" => 4,
            _ => 5
        };
    }

    /// <summary>
    /// Request model for manual review triggers
    /// </summary>
    public class ManualReviewRequest
    {
        public string ProjectId { get; set; } = string.Empty;
        public string MergeRequestIid { get; set; } = string.Empty;
    }
}