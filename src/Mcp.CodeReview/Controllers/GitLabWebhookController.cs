using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Mcp.CodeReview.GitLab;
using Mcp.CodeReview.Metrics;

namespace Mcp.CodeReview.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GitLabWebhookController : ControllerBase
    {
        private readonly GitLabIntegrationService _gitLabService;
        private readonly ILogger<GitLabWebhookController> _logger;
        private readonly MetricsRegistry _metrics;

        public GitLabWebhookController(
            GitLabIntegrationService gitLabService,
            ILogger<GitLabWebhookController> logger,
            MetricsRegistry metrics)
        {
            _gitLabService = gitLabService;
            _logger = logger;
            _metrics = metrics;
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

                // Process the webhook
                var result = await _gitLabService.ProcessWebhookAsync(payload);
                
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