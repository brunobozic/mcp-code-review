using Prometheus;

namespace Mcp.CodeReview.Metrics;

public class MetricsRegistry
{
    public readonly Counter ReviewsTotal = Prometheus.Metrics
        .CreateCounter("mcp_reviews_total", "Total number of code reviews performed");

    public readonly Counter FilesReviewedTotal = Prometheus.Metrics
        .CreateCounter("mcp_files_reviewed_total", "Total number of files reviewed");

    public readonly Counter ErrorsTotal = Prometheus.Metrics
        .CreateCounter("mcp_errors_total", "Total number of errors", new[] { "type" });

    public readonly Histogram ReviewDurationSeconds = Prometheus.Metrics
        .CreateHistogram("mcp_review_duration_seconds", "Duration of code reviews in seconds",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(0.1, 0.5, 20)
            });

    public void RecordReview(int fileCount, double durationSeconds)
    {
        ReviewsTotal.Inc();
        FilesReviewedTotal.Inc(fileCount);
        ReviewDurationSeconds.Observe(durationSeconds);
    }

    public void RecordError(string errorType)
    {
        ErrorsTotal.WithLabels(errorType).Inc();
    }

    public void IncrementCounter(string counterName, params (string, string)[] labels)
    {
        // For now, map to existing counters or create simple increment
        switch (counterName)
        {
            case "gitlab_webhooks_received_total":
                ReviewsTotal.Inc(); // Use existing counter as fallback
                break;
            default:
                // Create a generic counter if needed
                break;
        }
    }
}