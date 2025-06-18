using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mcp.CodeReview.Services;

public class SonarQubeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SonarQubeService> _logger;
    private readonly string _sonarQubeHost;
    private readonly string _sonarQubeToken;

    public SonarQubeService(HttpClient httpClient, ILogger<SonarQubeService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _sonarQubeHost = configuration["SONARQUBE_HOST"] ?? "http://localhost:9000";
        _sonarQubeToken = configuration["SONARQUBE_TOKEN"] ?? "";

        // Configure HTTP client
        _httpClient.BaseAddress = new Uri(_sonarQubeHost);
        if (!string.IsNullOrEmpty(_sonarQubeToken))
        {
            var authValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_sonarQubeToken}:"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
        }
    }

    /// <summary>
    /// Get SonarQube analysis results for a project
    /// </summary>
    public async Task<SonarQubeProjectAnalysis> GetProjectAnalysis(string projectKey)
    {
        try
        {
            var issues = await GetProjectIssues(projectKey);
            var measures = await GetProjectMeasures(projectKey);
            var qualityGate = await GetQualityGateStatus(projectKey);

            return new SonarQubeProjectAnalysis
            {
                ProjectKey = projectKey,
                Issues = issues,
                Measures = measures,
                QualityGate = qualityGate,
                AnalysisDate = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SonarQube analysis for project {ProjectKey}", projectKey);
            throw new InvalidOperationException($"SonarQube analysis failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Get issues for a specific project
    /// </summary>
    public async Task<List<SonarQubeIssue>> GetProjectIssues(string projectKey, string? severity = null, string? type = null)
    {
        var queryParams = new List<string>
        {
            $"componentKeys={Uri.EscapeDataString(projectKey)}",
            "ps=500" // Page size
        };

        if (!string.IsNullOrEmpty(severity))
            queryParams.Add($"severities={Uri.EscapeDataString(severity)}");

        if (!string.IsNullOrEmpty(type))
            queryParams.Add($"types={Uri.EscapeDataString(type)}");

        var query = string.Join("&", queryParams);
        var response = await _httpClient.GetAsync($"/api/issues/search?{query}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SonarQubeIssuesResponse>(content);

        return result?.Issues ?? new List<SonarQubeIssue>();
    }

    /// <summary>
    /// Get quality metrics for a project
    /// </summary>
    public async Task<List<SonarQubeMeasure>> GetProjectMeasures(string projectKey)
    {
        var metricKeys = new[]
        {
            "alert_status", "bugs", "vulnerabilities", "security_hotspots", "code_smells",
            "coverage", "duplicated_lines_density", "ncloc", "complexity", "cognitive_complexity",
            "security_rating", "reliability_rating", "maintainability_rating", "sqale_rating"
        };

        var query = $"component={Uri.EscapeDataString(projectKey)}&metricKeys={string.Join(",", metricKeys)}";
        var response = await _httpClient.GetAsync($"/api/measures/component?{query}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SonarQubeMeasuresResponse>(content);

        return result?.Component?.Measures ?? new List<SonarQubeMeasure>();
    }

    /// <summary>
    /// Get quality gate status for a project
    /// </summary>
    public async Task<SonarQubeQualityGate> GetQualityGateStatus(string projectKey)
    {
        var response = await _httpClient.GetAsync($"/api/qualitygates/project_status?projectKey={Uri.EscapeDataString(projectKey)}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SonarQubeQualityGateResponse>(content);

        return result?.ProjectStatus ?? new SonarQubeQualityGate();
    }

    /// <summary>
    /// Get issue details with AI-friendly formatting
    /// </summary>
    public async Task<SonarQubeIssueAnalysis> GetIssueAnalysisForAI(string projectKey)
    {
        var issues = await GetProjectIssues(projectKey);
        var measures = await GetProjectMeasures(projectKey);

        // Group issues by severity and type for AI analysis
        var securityIssues = issues.Where(i => i.Type == "VULNERABILITY" || i.Type == "SECURITY_HOTSPOT").ToList();
        var bugIssues = issues.Where(i => i.Type == "BUG").ToList();
        var codeSmellIssues = issues.Where(i => i.Type == "CODE_SMELL").ToList();

        // Extract key metrics
        var coverage = measures.FirstOrDefault(m => m.Metric == "coverage")?.Value ?? "0";
        var bugs = measures.FirstOrDefault(m => m.Metric == "bugs")?.Value ?? "0";
        var vulnerabilities = measures.FirstOrDefault(m => m.Metric == "vulnerabilities")?.Value ?? "0";
        var codeSmells = measures.FirstOrDefault(m => m.Metric == "code_smells")?.Value ?? "0";

        return new SonarQubeIssueAnalysis
        {
            ProjectKey = projectKey,
            TotalIssues = issues.Count,
            SecurityIssues = securityIssues,
            BugIssues = bugIssues,
            CodeSmellIssues = codeSmellIssues,
            QualityMetrics = new Dictionary<string, string>
            {
                ["coverage"] = coverage,
                ["bugs"] = bugs,
                ["vulnerabilities"] = vulnerabilities,
                ["code_smells"] = codeSmells
            },
            IssuesByFile = issues.GroupBy(i => i.Component).ToDictionary(g => g.Key, g => g.ToList()),
            CriticalIssues = issues.Where(i => i.Severity == "CRITICAL" || i.Severity == "BLOCKER").ToList()
        };
    }

    /// <summary>
    /// Test SonarQube connection
    /// </summary>
    public async Task<bool> TestConnection()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/system/status");
            var isConnected = response.IsSuccessStatusCode;
            
            if (isConnected)
                _logger.LogInformation("SonarQube connection test successful");
            else
                _logger.LogWarning("SonarQube connection test failed: {StatusCode}", response.StatusCode);
                
            return isConnected;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SonarQube connection test failed: {Message}", ex.Message);
            return false;
        }
    }
}

// Data models for SonarQube API responses
public class SonarQubeProjectAnalysis
{
    public string ProjectKey { get; set; } = "";
    public List<SonarQubeIssue> Issues { get; set; } = new();
    public List<SonarQubeMeasure> Measures { get; set; } = new();
    public SonarQubeQualityGate QualityGate { get; set; } = new();
    public DateTime AnalysisDate { get; set; }
}

public class SonarQubeIssue
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";
    
    [JsonPropertyName("rule")]
    public string Rule { get; set; } = "";
    
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "";
    
    [JsonPropertyName("component")]
    public string Component { get; set; } = "";
    
    [JsonPropertyName("project")]
    public string Project { get; set; } = "";
    
    [JsonPropertyName("line")]
    public int? Line { get; set; }
    
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = "";
    
    [JsonPropertyName("textRange")]
    public SonarQubeTextRange? TextRange { get; set; }
    
    [JsonPropertyName("flows")]
    public List<SonarQubeFlow> Flows { get; set; } = new();
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
    
    [JsonPropertyName("effort")]
    public string Effort { get; set; } = "";
    
    [JsonPropertyName("debt")]
    public string Debt { get; set; } = "";
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
    
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
    
    [JsonPropertyName("creationDate")]
    public DateTime CreationDate { get; set; }
    
    [JsonPropertyName("updateDate")]
    public DateTime UpdateDate { get; set; }
}

public class SonarQubeTextRange
{
    [JsonPropertyName("startLine")]
    public int StartLine { get; set; }
    
    [JsonPropertyName("endLine")]
    public int EndLine { get; set; }
    
    [JsonPropertyName("startOffset")]
    public int StartOffset { get; set; }
    
    [JsonPropertyName("endOffset")]
    public int EndOffset { get; set; }
}

public class SonarQubeFlow
{
    [JsonPropertyName("locations")]
    public List<SonarQubeLocation> Locations { get; set; } = new();
}

public class SonarQubeLocation
{
    [JsonPropertyName("component")]
    public string Component { get; set; } = "";
    
    [JsonPropertyName("textRange")]
    public SonarQubeTextRange? TextRange { get; set; }
    
    [JsonPropertyName("msg")]
    public string Message { get; set; } = "";
}

public class SonarQubeMeasure
{
    [JsonPropertyName("metric")]
    public string Metric { get; set; } = "";
    
    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
    
    [JsonPropertyName("bestValue")]
    public bool? BestValue { get; set; }
}

public class SonarQubeQualityGate
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
    
    [JsonPropertyName("conditions")]
    public List<SonarQubeCondition> Conditions { get; set; } = new();
}

public class SonarQubeCondition
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";
    
    [JsonPropertyName("metricKey")]
    public string MetricKey { get; set; } = "";
    
    [JsonPropertyName("comparator")]
    public string Comparator { get; set; } = "";
    
    [JsonPropertyName("errorThreshold")]
    public string ErrorThreshold { get; set; } = "";
    
    [JsonPropertyName("actualValue")]
    public string ActualValue { get; set; } = "";
}

// API Response containers
public class SonarQubeIssuesResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }
    
    [JsonPropertyName("p")]
    public int Page { get; set; }
    
    [JsonPropertyName("ps")]
    public int PageSize { get; set; }
    
    [JsonPropertyName("issues")]
    public List<SonarQubeIssue> Issues { get; set; } = new();
}

public class SonarQubeMeasuresResponse
{
    [JsonPropertyName("component")]
    public SonarQubeComponent? Component { get; set; }
}

public class SonarQubeComponent
{
    [JsonPropertyName("key")]
    public string Key { get; set; } = "";
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    
    [JsonPropertyName("measures")]
    public List<SonarQubeMeasure> Measures { get; set; } = new();
}

public class SonarQubeQualityGateResponse
{
    [JsonPropertyName("projectStatus")]
    public SonarQubeQualityGate? ProjectStatus { get; set; }
}

// AI Analysis helper class
public class SonarQubeIssueAnalysis
{
    public string ProjectKey { get; set; } = "";
    public int TotalIssues { get; set; }
    public List<SonarQubeIssue> SecurityIssues { get; set; } = new();
    public List<SonarQubeIssue> BugIssues { get; set; } = new();
    public List<SonarQubeIssue> CodeSmellIssues { get; set; } = new();
    public List<SonarQubeIssue> CriticalIssues { get; set; } = new();
    public Dictionary<string, string> QualityMetrics { get; set; } = new();
    public Dictionary<string, List<SonarQubeIssue>> IssuesByFile { get; set; } = new();
}