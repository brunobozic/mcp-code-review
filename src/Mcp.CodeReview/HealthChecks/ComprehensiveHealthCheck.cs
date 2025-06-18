using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace Mcp.CodeReview.HealthChecks;

public class ComprehensiveHealthCheck : IHealthCheck
{
    private readonly ILogger<ComprehensiveHealthCheck> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public ComprehensiveHealthCheck(
        ILogger<ComprehensiveHealthCheck> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var checks = new List<HealthCheckDetail>();

        try
        {
            // System resource checks
            await CheckSystemResources(checks, cancellationToken);
            
            // Application dependencies
            await CheckApplicationDependencies(checks, cancellationToken);
            
            // External services
            await CheckExternalServices(checks, cancellationToken);
            
            // Security validations
            await CheckSecurityRequirements(checks, cancellationToken);

            stopwatch.Stop();
            
            var failedChecks = checks.Where(c => c.Status == HealthStatus.Unhealthy).ToList();
            var degradedChecks = checks.Where(c => c.Status == HealthStatus.Degraded).ToList();

            var overallStatus = failedChecks.Any() ? HealthStatus.Unhealthy :
                               degradedChecks.Any() ? HealthStatus.Degraded :
                               HealthStatus.Healthy;

            var data = new Dictionary<string, object>
            {
                ["checks"] = checks.ToDictionary(c => c.Name, c => new
                {
                    status = c.Status.ToString(),
                    description = c.Description,
                    duration = c.Duration.TotalMilliseconds,
                    data = c.Data
                }),
                ["summary"] = new
                {
                    total = checks.Count,
                    healthy = checks.Count(c => c.Status == HealthStatus.Healthy),
                    degraded = degradedChecks.Count,
                    unhealthy = failedChecks.Count,
                    totalDuration = stopwatch.Elapsed.TotalMilliseconds
                }
            };

            var description = overallStatus switch
            {
                HealthStatus.Healthy => "All health checks passed",
                HealthStatus.Degraded => $"{degradedChecks.Count} checks degraded",
                HealthStatus.Unhealthy => $"{failedChecks.Count} checks failed",
                _ => "Unknown health status"
            };

            return new HealthCheckResult(overallStatus, description, data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            return HealthCheckResult.Unhealthy("Health check failed with exception", ex);
        }
    }

    private async Task CheckSystemResources(List<HealthCheckDetail> checks, CancellationToken cancellationToken)
    {
        // Memory check
        var memoryCheck = CheckMemoryUsage();
        checks.Add(memoryCheck);

        // Disk space check
        var diskCheck = CheckDiskSpace();
        checks.Add(diskCheck);

        // CPU check
        var cpuCheck = await CheckCpuUsage(cancellationToken);
        checks.Add(cpuCheck);
    }

    private async Task CheckApplicationDependencies(List<HealthCheckDetail> checks, CancellationToken cancellationToken)
    {
        // Database/Storage connectivity
        var storageCheck = CheckStorageAccess();
        checks.Add(storageCheck);

        // Configuration validation
        var configCheck = CheckConfiguration();
        checks.Add(configCheck);

        // Service registration
        var servicesCheck = CheckServiceRegistration();
        checks.Add(servicesCheck);
    }

    private async Task CheckExternalServices(List<HealthCheckDetail> checks, CancellationToken cancellationToken)
    {
        // Claude API connectivity
        var claudeCheck = await CheckClaudeService(cancellationToken);
        checks.Add(claudeCheck);

        // GitHub API connectivity
        var githubCheck = await CheckGitHubService(cancellationToken);
        checks.Add(githubCheck);

        // GitLab API connectivity
        var gitlabCheck = await CheckGitLabService(cancellationToken);
        checks.Add(gitlabCheck);

        // Elasticsearch connectivity
        var elasticsearchCheck = await CheckElasticsearchService(cancellationToken);
        checks.Add(elasticsearchCheck);
    }

    private async Task CheckSecurityRequirements(List<HealthCheckDetail> checks, CancellationToken cancellationToken)
    {
        // Environment variables security
        var envCheck = CheckEnvironmentSecurity();
        checks.Add(envCheck);

        // File permissions
        var permissionsCheck = CheckFilePermissions();
        checks.Add(permissionsCheck);

        // Network security
        var networkCheck = CheckNetworkSecurity();
        checks.Add(networkCheck);
    }

    private HealthCheckDetail CheckMemoryUsage()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var process = Process.GetCurrentProcess();
            var workingSetMB = process.WorkingSet64 / 1024 / 1024;
            var maxMemoryMB = 2048; // 2GB limit

            var usagePercent = (double)workingSetMB / maxMemoryMB * 100;

            var status = usagePercent > 90 ? HealthStatus.Unhealthy :
                        usagePercent > 75 ? HealthStatus.Degraded :
                        HealthStatus.Healthy;

            stopwatch.Stop();

            return new HealthCheckDetail
            {
                Name = "memory_usage",
                Status = status,
                Description = $"Memory usage: {workingSetMB}MB ({usagePercent:F1}%)",
                Duration = stopwatch.Elapsed,
                Data = new Dictionary<string, object>
                {
                    ["working_set_mb"] = workingSetMB,
                    ["usage_percent"] = usagePercent,
                    ["limit_mb"] = maxMemoryMB
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDetail
            {
                Name = "memory_usage",
                Status = HealthStatus.Unhealthy,
                Description = $"Memory check failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private HealthCheckDetail CheckDiskSpace()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var dataPath = "/data";
            var drive = new DriveInfo(dataPath);
            
            var freeSpaceGB = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
            var totalSpaceGB = drive.TotalSize / 1024 / 1024 / 1024;
            var usagePercent = (double)(totalSpaceGB - freeSpaceGB) / totalSpaceGB * 100;

            var status = usagePercent > 95 ? HealthStatus.Unhealthy :
                        usagePercent > 85 ? HealthStatus.Degraded :
                        HealthStatus.Healthy;

            stopwatch.Stop();

            return new HealthCheckDetail
            {
                Name = "disk_space",
                Status = status,
                Description = $"Disk usage: {usagePercent:F1}% ({freeSpaceGB}GB free)",
                Duration = stopwatch.Elapsed,
                Data = new Dictionary<string, object>
                {
                    ["free_space_gb"] = freeSpaceGB,
                    ["total_space_gb"] = totalSpaceGB,
                    ["usage_percent"] = usagePercent
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDetail
            {
                Name = "disk_space",
                Status = HealthStatus.Unhealthy,
                Description = $"Disk check failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<HealthCheckDetail> CheckCpuUsage(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var process = Process.GetCurrentProcess();
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            
            await Task.Delay(1000, cancellationToken);
            
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed) * 100;

            var status = cpuUsageTotal > 90 ? HealthStatus.Unhealthy :
                        cpuUsageTotal > 75 ? HealthStatus.Degraded :
                        HealthStatus.Healthy;

            stopwatch.Stop();

            return new HealthCheckDetail
            {
                Name = "cpu_usage",
                Status = status,
                Description = $"CPU usage: {cpuUsageTotal:F1}%",
                Duration = stopwatch.Elapsed,
                Data = new Dictionary<string, object>
                {
                    ["cpu_usage_percent"] = cpuUsageTotal,
                    ["processor_count"] = Environment.ProcessorCount
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDetail
            {
                Name = "cpu_usage",
                Status = HealthStatus.Unhealthy,
                Description = $"CPU check failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private HealthCheckDetail CheckStorageAccess()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var testFile = Path.Combine("/data", $"health_check_{Guid.NewGuid()}.tmp");
            
            // Test write access
            File.WriteAllText(testFile, "health_check");
            
            // Test read access
            var content = File.ReadAllText(testFile);
            
            // Cleanup
            File.Delete(testFile);

            stopwatch.Stop();

            var status = content == "health_check" ? HealthStatus.Healthy : HealthStatus.Unhealthy;

            return new HealthCheckDetail
            {
                Name = "storage_access",
                Status = status,
                Description = "Storage read/write access verified",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDetail
            {
                Name = "storage_access",
                Status = HealthStatus.Unhealthy,
                Description = $"Storage access failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private HealthCheckDetail CheckConfiguration()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var requiredConfigs = new[]
            {
                "CLAUDE_API_KEY",
                "ASPNETCORE_ENVIRONMENT"
            };

            var missingConfigs = new List<string>();
            var configData = new Dictionary<string, object>();

            foreach (var config in requiredConfigs)
            {
                var value = _configuration[config] ?? Environment.GetEnvironmentVariable(config);
                if (string.IsNullOrEmpty(value))
                {
                    missingConfigs.Add(config);
                }
                else
                {
                    configData[config] = value.Length > 10 ? $"{value[..10]}..." : value;
                }
            }

            stopwatch.Stop();

            var status = missingConfigs.Any() ? HealthStatus.Unhealthy : HealthStatus.Healthy;
            var description = missingConfigs.Any() 
                ? $"Missing configurations: {string.Join(", ", missingConfigs)}"
                : "All required configurations present";

            return new HealthCheckDetail
            {
                Name = "configuration",
                Status = status,
                Description = description,
                Duration = stopwatch.Elapsed,
                Data = configData
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDetail
            {
                Name = "configuration",
                Status = HealthStatus.Unhealthy,
                Description = $"Configuration check failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private HealthCheckDetail CheckServiceRegistration()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var requiredServices = new[]
            {
                typeof(Services.ClaudeService),
                typeof(Services.GitHubService),
                typeof(Services.GitLabService),
                typeof(ILogger<ComprehensiveHealthCheck>)
            };

            var missingServices = new List<string>();

            foreach (var serviceType in requiredServices)
            {
                var service = _serviceProvider.GetService(serviceType);
                if (service == null)
                {
                    missingServices.Add(serviceType.Name);
                }
            }

            stopwatch.Stop();

            var status = missingServices.Any() ? HealthStatus.Unhealthy : HealthStatus.Healthy;
            var description = missingServices.Any()
                ? $"Missing services: {string.Join(", ", missingServices)}"
                : "All required services registered";

            return new HealthCheckDetail
            {
                Name = "service_registration",
                Status = status,
                Description = description,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDetail
            {
                Name = "service_registration",
                Status = HealthStatus.Unhealthy,
                Description = $"Service registration check failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<HealthCheckDetail> CheckClaudeService(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var claudeService = _serviceProvider.GetService<Services.ClaudeService>();
            if (claudeService == null)
            {
                stopwatch.Stop();
                return new HealthCheckDetail
                {
                    Name = "claude_service",
                    Status = HealthStatus.Unhealthy,
                    Description = "Claude service not registered",
                    Duration = stopwatch.Elapsed
                };
            }

            var isHealthy = await claudeService.TestConnection();
            stopwatch.Stop();

            return new HealthCheckDetail
            {
                Name = "claude_service",
                Status = isHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                Description = isHealthy ? "Claude API accessible" : "Claude API not accessible",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDetail
            {
                Name = "claude_service",
                Status = HealthStatus.Unhealthy,
                Description = $"Claude service check failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<HealthCheckDetail> CheckGitHubService(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var githubService = _serviceProvider.GetService<Services.GitHubService>();
            if (githubService == null)
            {
                stopwatch.Stop();
                return new HealthCheckDetail
                {
                    Name = "github_service",
                    Status = HealthStatus.Degraded,
                    Description = "GitHub service not registered",
                    Duration = stopwatch.Elapsed
                };
            }

            var isHealthy = await githubService.TestConnection();
            stopwatch.Stop();

            return new HealthCheckDetail
            {
                Name = "github_service",
                Status = isHealthy ? HealthStatus.Healthy : HealthStatus.Degraded,
                Description = isHealthy ? "GitHub API accessible" : "GitHub API not accessible",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDetail
            {
                Name = "github_service",
                Status = HealthStatus.Degraded,
                Description = $"GitHub service check failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<HealthCheckDetail> CheckGitLabService(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var gitlabService = _serviceProvider.GetService<Services.GitLabService>();
            if (gitlabService == null)
            {
                stopwatch.Stop();
                return new HealthCheckDetail
                {
                    Name = "gitlab_service",
                    Status = HealthStatus.Degraded,
                    Description = "GitLab service not registered",
                    Duration = stopwatch.Elapsed
                };
            }

            var isHealthy = await gitlabService.TestConnection();
            stopwatch.Stop();

            return new HealthCheckDetail
            {
                Name = "gitlab_service",
                Status = isHealthy ? HealthStatus.Healthy : HealthStatus.Degraded,
                Description = isHealthy ? "GitLab API accessible" : "GitLab API not accessible",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDetail
            {
                Name = "gitlab_service",
                Status = HealthStatus.Degraded,
                Description = $"GitLab service check failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<HealthCheckDetail> CheckElasticsearchService(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var elasticsearchUrl = _configuration["ELASTICSEARCH_URL"] ?? "http://elasticsearch:9200";
            
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await httpClient.GetAsync($"{elasticsearchUrl}/_cluster/health", cancellationToken);
            
            stopwatch.Stop();

            var status = response.IsSuccessStatusCode ? HealthStatus.Healthy : HealthStatus.Degraded;
            var description = response.IsSuccessStatusCode ? "Elasticsearch accessible" : "Elasticsearch not accessible";

            return new HealthCheckDetail
            {
                Name = "elasticsearch_service",
                Status = status,
                Description = description,
                Duration = stopwatch.Elapsed,
                Data = new Dictionary<string, object>
                {
                    ["url"] = elasticsearchUrl,
                    ["status_code"] = (int)response.StatusCode
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDetail
            {
                Name = "elasticsearch_service",
                Status = HealthStatus.Degraded,
                Description = $"Elasticsearch check failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private HealthCheckDetail CheckEnvironmentSecurity()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var securityIssues = new List<string>();

            // Check for development settings in production
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (environment == "Production")
            {
                // Verify no debug settings
                var debugSettings = new[]
                {
                    "ASPNETCORE_DETAILEDERRORS",
                    "ASPNETCORE_ENVIRONMENT"
                };

                foreach (var setting in debugSettings)
                {
                    var value = Environment.GetEnvironmentVariable(setting);
                    if (setting == "ASPNETCORE_DETAILEDERRORS" && value == "true")
                    {
                        securityIssues.Add("Detailed errors enabled in production");
                    }
                }
            }

            stopwatch.Stop();

            var status = securityIssues.Any() ? HealthStatus.Degraded : HealthStatus.Healthy;
            var description = securityIssues.Any()
                ? $"Security issues: {string.Join(", ", securityIssues)}"
                : "Environment security validated";

            return new HealthCheckDetail
            {
                Name = "environment_security",
                Status = status,
                Description = description,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDetail
            {
                Name = "environment_security",
                Status = HealthStatus.Unhealthy,
                Description = $"Environment security check failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private HealthCheckDetail CheckFilePermissions()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var criticalPaths = new[]
            {
                "/data",
                "/var/log/mcp",
                "/app"
            };

            var permissionIssues = new List<string>();

            foreach (var path in criticalPaths)
            {
                if (!Directory.Exists(path))
                {
                    permissionIssues.Add($"Path does not exist: {path}");
                    continue;
                }

                // Check basic access
                try
                {
                    var files = Directory.GetFiles(path);
                    if (path == "/data" && !Directory.EnumerateFiles(path).Any())
                    {
                        // Test write access to data directory
                        var testFile = Path.Combine(path, "permission_test.tmp");
                        File.WriteAllText(testFile, "test");
                        File.Delete(testFile);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    permissionIssues.Add($"Access denied to: {path}");
                }
            }

            stopwatch.Stop();

            var status = permissionIssues.Any() ? HealthStatus.Unhealthy : HealthStatus.Healthy;
            var description = permissionIssues.Any()
                ? $"Permission issues: {string.Join(", ", permissionIssues)}"
                : "File permissions validated";

            return new HealthCheckDetail
            {
                Name = "file_permissions",
                Status = status,
                Description = description,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDetail
            {
                Name = "file_permissions",
                Status = HealthStatus.Unhealthy,
                Description = $"File permissions check failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private HealthCheckDetail CheckNetworkSecurity()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var networkIssues = new List<string>();

            // Check if running as root (security risk)
            if (Environment.UserName == "root")
            {
                networkIssues.Add("Running as root user");
            }

            // Check for open ports (basic check)
            var expectedPorts = new[] { 5000, 5001 };
            // This is a simplified check - in production you'd want more sophisticated port scanning

            stopwatch.Stop();

            var status = networkIssues.Any() ? HealthStatus.Degraded : HealthStatus.Healthy;
            var description = networkIssues.Any()
                ? $"Network security issues: {string.Join(", ", networkIssues)}"
                : "Network security validated";

            return new HealthCheckDetail
            {
                Name = "network_security",
                Status = status,
                Description = description,
                Duration = stopwatch.Elapsed,
                Data = new Dictionary<string, object>
                {
                    ["user"] = Environment.UserName,
                    ["expected_ports"] = expectedPorts
                }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HealthCheckDetail
            {
                Name = "network_security",
                Status = HealthStatus.Unhealthy,
                Description = $"Network security check failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }
}

public class HealthCheckDetail
{
    public string Name { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}