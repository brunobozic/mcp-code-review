using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
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

        public GitLabWebhookController(
            GitLabIntegrationService gitLabService,
            ILogger<GitLabWebhookController> logger,
            MetricsRegistry metrics,
            IAIReviewService reviewService)
        {
            _gitLabService = gitLabService;
            _logger = logger;
            _metrics = metrics;
            _reviewService = reviewService;
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

                // Process the webhook with the integration service first
                var result = await _gitLabService.ProcessWebhookAsync(payload);
                
                // For merge request events, trigger contextual AI review
                if (payload.EventType == "merge_request" && payload.MergeRequest != null)
                {
                    _ = Task.Run(async () => await TriggerContextualReviewAsync(payload));
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
                if (payload.Project == null || payload.MergeRequest == null)
                {
                    _logger.LogWarning("Cannot trigger contextual review - missing project or merge request data");
                    return;
                }

                _logger.LogInformation("Triggering contextual review for MR {MrIid} in project {ProjectId}", 
                    payload.MergeRequest.Iid, payload.Project.Id);

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

                _logger.LogInformation("Contextual review completed for MR {MrIid} with {FindingCount} findings", 
                    payload.MergeRequest.Iid, reviewResult.KeyFindings.Count);

                // Post results back to GitLab (optional - would need GitLab API integration)
                await PostReviewResultsToGitLabAsync(payload, reviewResult);

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

        private async Task PostReviewResultsToGitLabAsync(GitLabWebhookPayload payload, MultiAgentReviewResult reviewResult)
        {
            try
            {
                // This would post the review results back to GitLab as a merge request comment
                _logger.LogInformation("Review results ready for MR {MrIid}: {FindingCount} findings, quality score {QualityScore}", 
                    payload.MergeRequest?.Iid, 
                    reviewResult.KeyFindings.Count, 
                    reviewResult.QualityScore);
                
                // Implementation would use GitLab API to post comment with review results
                // await _gitLabService.PostMergeRequestCommentAsync(payload.Project.Id, payload.MergeRequest.Iid, reviewResult);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to post review results to GitLab for MR {MrIid}", 
                    payload.MergeRequest?.Iid);
            }
        }
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