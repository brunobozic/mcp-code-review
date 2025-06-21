using Mcp.CodeReview.Models;
using Mcp.CodeReview.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Mcp.CodeReview.AI;

/// <summary>
/// Tracks historical accuracy of AI agents for confidence calibration learning
/// Maintains performance metrics and learning patterns for dynamic calibration
/// </summary>
public class HistoricalAccuracyTracker
{
    private readonly ILogger<HistoricalAccuracyTracker> _logger;
    private readonly ConcurrentDictionary<string, AgentAccuracyMetrics> _agentMetrics = new();
    private readonly ConcurrentDictionary<string, List<CalibrationRecord>> _calibrationHistory = new();
    private readonly AccuracyTrackerConfig _config;

    public HistoricalAccuracyTracker(
        ILogger<HistoricalAccuracyTracker> logger,
        AccuracyTrackerConfig? config = null)
    {
        _logger = logger;
        _config = config ?? new AccuracyTrackerConfig();
    }

    /// <summary>
    /// Gets agent accuracy metrics for a specific category
    /// </summary>
    public async Task<AgentAccuracyMetrics> GetAgentAccuracyAsync(
        AgentType agent,
        string category,
        CancellationToken cancellationToken = default)
    {
        var key = GetMetricsKey(agent, category);
        
        if (_agentMetrics.TryGetValue(key, out var metrics))
        {
            _logger.LogDebug("📊 ACCURACY METRICS: {Agent} {Category} - {Accuracy:F2}% ({Predictions} predictions)",
                agent, category, metrics.OverallAccuracy * 100, metrics.TotalPredictions);
            return metrics;
        }

        // Return default metrics for new agent-category combinations
        var defaultMetrics = new AgentAccuracyMetrics
        {
            Agent = agent,
            Category = category,
            OverallAccuracy = _config.DefaultAccuracy,
            TotalPredictions = 0,
            CorrectPredictions = 0,
            LastUpdated = DateTime.UtcNow
        };

        _agentMetrics.TryAdd(key, defaultMetrics);
        
        _logger.LogInformation("🆕 NEW AGENT METRICS: {Agent} {Category} initialized with default accuracy {Accuracy:F2}",
            agent, category, _config.DefaultAccuracy);
            
        return defaultMetrics;
    }

    /// <summary>
    /// Gets severity-specific accuracy for an agent
    /// </summary>
    public async Task<SeverityAccuracyMetrics> GetSeverityAccuracyAsync(
        AgentType agent,
        string severity,
        CancellationToken cancellationToken = default)
    {
        var key = GetSeverityMetricsKey(agent, severity);
        var allMetrics = _agentMetrics.Values.Where(m => m.Agent == agent).ToList();
        
        if (!allMetrics.Any())
        {
            return new SeverityAccuracyMetrics
            {
                Severity = severity,
                Accuracy = _config.DefaultAccuracy,
                TotalPredictions = 0
            };
        }

        // Calculate severity-specific accuracy from historical data
        var severityPredictions = allMetrics
            .SelectMany(m => m.SeverityBreakdown)
            .Where(s => s.Key == severity)
            .ToList();

        if (!severityPredictions.Any())
        {
            return new SeverityAccuracyMetrics
            {
                Severity = severity,
                Accuracy = _config.DefaultAccuracy,
                TotalPredictions = 0
            };
        }

        var totalCorrect = severityPredictions.Sum(s => s.Value.CorrectPredictions);
        var totalPredictions = severityPredictions.Sum(s => s.Value.TotalPredictions);
        
        return new SeverityAccuracyMetrics
        {
            Severity = severity,
            Accuracy = totalPredictions > 0 ? (double)totalCorrect / totalPredictions : _config.DefaultAccuracy,
            TotalPredictions = totalPredictions
        };
    }

    /// <summary>
    /// Updates agent accuracy based on validation feedback
    /// </summary>
    public async Task UpdateAgentAccuracyAsync(
        AgentType agent,
        string category,
        bool wasAccurate,
        string severity,
        CancellationToken cancellationToken = default)
    {
        var key = GetMetricsKey(agent, category);
        var metrics = await GetAgentAccuracyAsync(agent, category, cancellationToken);

        // Update overall metrics
        metrics.TotalPredictions++;
        if (wasAccurate)
        {
            metrics.CorrectPredictions++;
        }
        
        metrics.OverallAccuracy = (double)metrics.CorrectPredictions / metrics.TotalPredictions;
        metrics.LastUpdated = DateTime.UtcNow;

        // Update severity breakdown
        if (!metrics.SeverityBreakdown.ContainsKey(severity))
        {
            metrics.SeverityBreakdown[severity] = new SeverityAccuracyMetrics
            {
                Severity = severity,
                Accuracy = 0.0,
                TotalPredictions = 0
            };
        }

        var severityMetrics = metrics.SeverityBreakdown[severity];
        severityMetrics.TotalPredictions++;
        if (wasAccurate)
        {
            severityMetrics.CorrectPredictions++;
        }
        severityMetrics.Accuracy = (double)severityMetrics.CorrectPredictions / severityMetrics.TotalPredictions;

        // Update the metrics in the dictionary
        _agentMetrics.AddOrUpdate(key, metrics, (k, v) => metrics);

        _logger.LogInformation("📈 ACCURACY UPDATED: {Agent} {Category} now {Accuracy:F2}% ({Correct}/{Total}), severity {Severity}: {SeverityAccuracy:F2}%",
            agent, category, metrics.OverallAccuracy * 100, metrics.CorrectPredictions, metrics.TotalPredictions,
            severity, severityMetrics.Accuracy * 100);

        // Apply learning rate decay
        await ApplyLearningDecayAsync(metrics, cancellationToken);
    }

    /// <summary>
    /// Tracks a calibration decision for learning
    /// </summary>
    public async Task TrackCalibrationDecisionAsync(
        CalibrationRecord record,
        CancellationToken cancellationToken = default)
    {
        var agentKey = record.Agent.ToString();
        
        if (!_calibrationHistory.ContainsKey(agentKey))
        {
            _calibrationHistory[agentKey] = new List<CalibrationRecord>();
        }

        _calibrationHistory[agentKey].Add(record);

        // Keep only recent calibration history to prevent memory bloat
        var history = _calibrationHistory[agentKey];
        if (history.Count > _config.MaxCalibrationHistory)
        {
            var toRemove = history.Count - _config.MaxCalibrationHistory;
            history.RemoveRange(0, toRemove);
        }

        _logger.LogDebug("📊 CALIBRATION TRACKED: {Agent} decision recorded (history: {HistoryCount})",
            record.Agent, history.Count);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets calibration trends for an agent
    /// </summary>
    public async Task<List<CalibrationTrend>> GetCalibrationTrendsAsync(
        AgentType agent,
        TimeSpan timeWindow,
        CancellationToken cancellationToken = default)
    {
        var agentKey = agent.ToString();
        
        if (!_calibrationHistory.TryGetValue(agentKey, out var history))
        {
            return new List<CalibrationTrend>();
        }

        var cutoffTime = DateTime.UtcNow - timeWindow;
        var recentRecords = history.Where(r => r.Timestamp >= cutoffTime).ToList();

        if (!recentRecords.Any())
        {
            return new List<CalibrationTrend>();
        }

        // Group by category and analyze trends
        var trends = recentRecords
            .GroupBy(r => r.Category)
            .Select(g => new CalibrationTrend
            {
                Category = g.Key,
                AverageOriginalConfidence = g.Average(r => r.OriginalConfidence),
                AverageCalibratedConfidence = g.Average(r => r.CalibratedConfidence),
                TotalCalibrations = g.Count(),
                TimeWindow = timeWindow,
                LastCalibration = g.Max(r => r.Timestamp)
            })
            .ToList();

        _logger.LogDebug("📈 CALIBRATION TRENDS: {Agent} has {TrendCount} category trends over {Hours}h",
            agent, trends.Count, timeWindow.TotalHours);

        return trends;
    }

    /// <summary>
    /// Gets accuracy improvement rate for an agent
    /// </summary>
    public async Task<double> GetAccuracyImprovementRateAsync(
        AgentType agent,
        string category,
        CancellationToken cancellationToken = default)
    {
        var metrics = await GetAgentAccuracyAsync(agent, category, cancellationToken);
        
        if (metrics.TotalPredictions < _config.MinimumPredictionsForTrend)
        {
            return 0.0; // Not enough data for trend analysis
        }

        // Calculate improvement rate based on recent vs historical performance
        var agentKey = agent.ToString();
        if (!_calibrationHistory.TryGetValue(agentKey, out var history))
        {
            return 0.0;
        }

        var categoryHistory = history.Where(h => h.Category == category).OrderBy(h => h.Timestamp).ToList();
        if (categoryHistory.Count < _config.MinimumPredictionsForTrend)
        {
            return 0.0;
        }

        // Compare first half vs second half accuracy
        var halfPoint = categoryHistory.Count / 2;
        var earlyHistory = categoryHistory.Take(halfPoint);
        var recentHistory = categoryHistory.Skip(halfPoint);

        var earlyAccuracy = metrics.OverallAccuracy; // Simplified - in practice would calculate from early history
        var recentAccuracy = metrics.OverallAccuracy; // Simplified - in practice would calculate from recent history

        var improvementRate = recentAccuracy - earlyAccuracy;
        
        _logger.LogDebug("📊 IMPROVEMENT RATE: {Agent} {Category} improvement rate: {Rate:+0.00;-0.00}",
            agent, category, improvementRate);

        return improvementRate;
    }

    /// <summary>
    /// Exports accuracy metrics for analysis
    /// </summary>
    public async Task<string> ExportMetricsAsync(CancellationToken cancellationToken = default)
    {
        var exportData = new
        {
            ExportTimestamp = DateTime.UtcNow,
            AgentMetrics = _agentMetrics.Values.ToList(),
            CalibrationHistory = _calibrationHistory.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.OrderByDescending(r => r.Timestamp).Take(100).ToList()
            ),
            Configuration = _config
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });

        _logger.LogInformation("📤 METRICS EXPORTED: {AgentCount} agents, {HistoryKeys} history keys",
            _agentMetrics.Count, _calibrationHistory.Count);

        return json;
    }

    private string GetMetricsKey(AgentType agent, string category)
    {
        return $"{agent}_{category}";
    }

    private string GetSeverityMetricsKey(AgentType agent, string severity)
    {
        return $"{agent}_severity_{severity}";
    }

    private async Task ApplyLearningDecayAsync(AgentAccuracyMetrics metrics, CancellationToken cancellationToken)
    {
        // Apply exponential moving average for learning rate decay
        if (metrics.TotalPredictions > _config.LearningDecayThreshold)
        {
            var decayFactor = Math.Exp(-_config.LearningDecayRate * metrics.TotalPredictions);
            // This would adjust learning rates in a more sophisticated implementation
            await Task.CompletedTask;
        }
    }
}

/// <summary>
/// Configuration for accuracy tracking behavior
/// </summary>
public class AccuracyTrackerConfig
{
    public double DefaultAccuracy { get; set; } = 0.75;
    public int MaxCalibrationHistory { get; set; } = 1000;
    public int MinimumPredictionsForTrend { get; set; } = 20;
    public int LearningDecayThreshold { get; set; } = 100;
    public double LearningDecayRate { get; set; } = 0.01;
    public TimeSpan MetricsRetentionPeriod { get; set; } = TimeSpan.FromDays(30);
}

/// <summary>
/// Agent accuracy metrics for a specific category
/// </summary>
public class AgentAccuracyMetrics
{
    public AgentType Agent { get; set; }
    public string Category { get; set; } = string.Empty;
    public double OverallAccuracy { get; set; }
    public int TotalPredictions { get; set; }
    public int CorrectPredictions { get; set; }
    public DateTime LastUpdated { get; set; }
    public Dictionary<string, SeverityAccuracyMetrics> SeverityBreakdown { get; set; } = new();
}

/// <summary>
/// Accuracy metrics for a specific severity level
/// </summary>
public class SeverityAccuracyMetrics
{
    public string Severity { get; set; } = string.Empty;
    public double Accuracy { get; set; }
    public int TotalPredictions { get; set; }
    public int CorrectPredictions { get; set; }
}

/// <summary>
/// Calibration trend analysis for an agent category
/// </summary>
public class CalibrationTrend
{
    public string Category { get; set; } = string.Empty;
    public double AverageOriginalConfidence { get; set; }
    public double AverageCalibratedConfidence { get; set; }
    public int TotalCalibrations { get; set; }
    public TimeSpan TimeWindow { get; set; }
    public DateTime LastCalibration { get; set; }
}