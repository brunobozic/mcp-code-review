using System.ComponentModel.DataAnnotations;

namespace Mcp.CodeReview.Infrastructure;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddConfigurationValidation(this IServiceCollection services, IConfiguration configuration)
    {
        // Validate configuration on startup
        var configValidator = new ConfigurationValidator();
        configValidator.ValidateConfiguration(configuration);

        // Register configuration models
        services.Configure<ApplicationConfig>(configuration.GetSection("Application"));
        services.Configure<SecurityConfig>(configuration.GetSection("Security"));
        services.Configure<ExternalApiConfig>(configuration.GetSection("ExternalApis"));
        services.Configure<MonitoringConfig>(configuration.GetSection("Monitoring"));
        services.Configure<RateLimitConfig>(configuration.GetSection("RateLimit"));

        // Register validator as singleton
        services.AddSingleton<IConfigurationValidator, ConfigurationValidator>();

        return services;
    }

    public static WebApplicationBuilder AddSecretsManagement(this WebApplicationBuilder builder)
    {
        var environment = builder.Environment.EnvironmentName;

        // Add different secret sources based on environment
        switch (environment.ToLowerInvariant())
        {
            case "development":
                // Use user secrets in development
                if (builder.Environment.IsDevelopment())
                {
                    try
                    {
                        builder.Configuration.AddUserSecrets<Program>();
                    }
                    catch (Exception ex)
                    {
                        // Log warning but continue - user secrets are optional in development
                        Console.WriteLine($"Warning: Could not load user secrets: {ex.Message}");
                    }
                }
                break;

            case "staging":
            case "production":
                // In production, you would integrate with:
                // - Azure Key Vault: builder.Configuration.AddAzureKeyVault(...)
                // - AWS Secrets Manager: builder.Configuration.AddSecretsManager(...)
                // - HashiCorp Vault: builder.Configuration.AddVault(...)
                // - Kubernetes Secrets: mounted as files or environment variables
                
                // For now, just use environment variables and mounted secret files
                var secretsPath = Environment.GetEnvironmentVariable("SECRETS_PATH");
                if (!string.IsNullOrEmpty(secretsPath) && Directory.Exists(secretsPath))
                {
                    // Load secrets from mounted files (Kubernetes pattern)
                    LoadSecretsFromDirectory(builder.Configuration, secretsPath);
                }
                break;
        }

        // Always add environment variables last (highest priority)
        builder.Configuration.AddEnvironmentVariables();

        return builder;
    }

    private static void LoadSecretsFromDirectory(IConfigurationBuilder configuration, string secretsPath)
    {
        try
        {
            var secretFiles = Directory.GetFiles(secretsPath);
            var secrets = new Dictionary<string, string>();

            foreach (var file in secretFiles)
            {
                var key = Path.GetFileName(file);
                var value = File.ReadAllText(file).Trim();
                secrets[key] = value;
            }

            configuration.AddInMemoryCollection(secrets);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load secrets from {secretsPath}: {ex.Message}");
        }
    }
}

public interface IConfigurationValidator
{
    void ValidateConfiguration(IConfiguration configuration);
    ValidationResult ValidateSection<T>(T configSection) where T : class;
}

public class ConfigurationValidator : IConfigurationValidator
{
    private readonly ILogger<ConfigurationValidator> _logger;

    public ConfigurationValidator()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<ConfigurationValidator>();
    }

    public void ValidateConfiguration(IConfiguration configuration)
    {
        var errors = new List<string>();

        // Validate required environment variables
        var requiredEnvVars = new[]
        {
            "CLAUDE_API_KEY",
            "ASPNETCORE_ENVIRONMENT"
        };

        foreach (var envVar in requiredEnvVars)
        {
            var value = configuration[envVar] ?? Environment.GetEnvironmentVariable(envVar);
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add($"Required environment variable '{envVar}' is missing or empty");
            }
        }

        // Validate conditional requirements
        var environment = configuration["ASPNETCORE_ENVIRONMENT"];
        if (environment == "Production")
        {
            var productionRequiredVars = new[]
            {
                "GRAFANA_ADMIN_PASSWORD",
                "GRAFANA_SECRET_KEY",
                "SENTRY_DSN"
            };

            foreach (var envVar in productionRequiredVars)
            {
                var value = configuration[envVar] ?? Environment.GetEnvironmentVariable(envVar);
                if (string.IsNullOrWhiteSpace(value))
                {
                    errors.Add($"Production environment variable '{envVar}' is missing or empty");
                }
            }
        }

        // Validate configuration sections
        ValidateApplicationConfig(configuration, errors);
        ValidateSecurityConfig(configuration, errors);
        ValidateExternalApiConfig(configuration, errors);
        ValidateMonitoringConfig(configuration, errors);

        if (errors.Any())
        {
            var errorMessage = $"Configuration validation failed:\n{string.Join("\n", errors)}";
            _logger.LogError("Configuration validation failed: {Errors}", string.Join(", ", errors));
            throw new InvalidOperationException(errorMessage);
        }

        _logger.LogInformation("Configuration validation passed");
    }

    public ValidationResult ValidateSection<T>(T configSection) where T : class
    {
        var context = new ValidationContext(configSection);
        var results = new List<ValidationResult>();
        
        var isValid = Validator.TryValidateObject(configSection, context, results, true);
        
        return isValid ? ValidationResult.Success! : 
               new ValidationResult($"Validation failed: {string.Join(", ", results.Select(r => r.ErrorMessage))}");
    }

    private void ValidateApplicationConfig(IConfiguration configuration, List<string> errors)
    {
        var appConfig = configuration.GetSection("Application").Get<ApplicationConfig>() ?? new ApplicationConfig();
        
        if (appConfig.MaxConcurrentRequests <= 0)
        {
            errors.Add("Application.MaxConcurrentRequests must be greater than 0");
        }

        if (appConfig.RequestTimeoutSeconds <= 0)
        {
            errors.Add("Application.RequestTimeoutSeconds must be greater than 0");
        }

        if (appConfig.MaxRepositorySizeMB <= 0)
        {
            errors.Add("Application.MaxRepositorySizeMB must be greater than 0");
        }
    }

    private void ValidateSecurityConfig(IConfiguration configuration, List<string> errors)
    {
        var securityConfig = configuration.GetSection("Security").Get<SecurityConfig>() ?? new SecurityConfig();
        
        if (securityConfig.EnableRateLimiting && securityConfig.RateLimitPerMinute <= 0)
        {
            errors.Add("Security.RateLimitPerMinute must be greater than 0 when rate limiting is enabled");
        }

        if (securityConfig.JwtExpirationMinutes <= 0)
        {
            errors.Add("Security.JwtExpirationMinutes must be greater than 0");
        }

        // Validate JWT secret in production
        var environment = configuration["ASPNETCORE_ENVIRONMENT"];
        if (environment == "Production" && string.IsNullOrWhiteSpace(securityConfig.JwtSecretKey))
        {
            errors.Add("Security.JwtSecretKey is required in production");
        }
    }

    private void ValidateExternalApiConfig(IConfiguration configuration, List<string> errors)
    {
        var apiConfig = configuration.GetSection("ExternalApis").Get<ExternalApiConfig>() ?? new ExternalApiConfig();
        
        if (apiConfig.ClaudeTimeoutSeconds <= 0)
        {
            errors.Add("ExternalApis.ClaudeTimeoutSeconds must be greater than 0");
        }

        if (apiConfig.GitHubTimeoutSeconds <= 0)
        {
            errors.Add("ExternalApis.GitHubTimeoutSeconds must be greater than 0");
        }

        if (apiConfig.GitLabTimeoutSeconds <= 0)
        {
            errors.Add("ExternalApis.GitLabTimeoutSeconds must be greater than 0");
        }

        // Validate URLs
        if (!string.IsNullOrEmpty(apiConfig.GitLabBaseUrl) && !Uri.IsWellFormedUriString(apiConfig.GitLabBaseUrl, UriKind.Absolute))
        {
            errors.Add("ExternalApis.GitLabBaseUrl must be a valid URL");
        }

        if (!string.IsNullOrEmpty(apiConfig.ElasticsearchUrl) && !Uri.IsWellFormedUriString(apiConfig.ElasticsearchUrl, UriKind.Absolute))
        {
            errors.Add("ExternalApis.ElasticsearchUrl must be a valid URL");
        }
    }

    private void ValidateMonitoringConfig(IConfiguration configuration, List<string> errors)
    {
        var monitoringConfig = configuration.GetSection("Monitoring").Get<MonitoringConfig>() ?? new MonitoringConfig();
        
        if (monitoringConfig.LogRetentionDays <= 0)
        {
            errors.Add("Monitoring.LogRetentionDays must be greater than 0");
        }

        if (monitoringConfig.MetricsRetentionDays <= 0)
        {
            errors.Add("Monitoring.MetricsRetentionDays must be greater than 0");
        }

        if (!string.IsNullOrEmpty(monitoringConfig.SentryDsn) && !Uri.IsWellFormedUriString(monitoringConfig.SentryDsn, UriKind.Absolute))
        {
            errors.Add("Monitoring.SentryDsn must be a valid URL");
        }
    }
}

// Configuration models with validation attributes
public class ApplicationConfig
{
    [Range(1, 100)]
    public int MaxConcurrentRequests { get; set; } = 10;

    [Range(1, 3600)]
    public int RequestTimeoutSeconds { get; set; } = 120;

    [Range(1, 10000)]
    public int MaxRepositorySizeMB { get; set; } = 1000;

    [Range(1, 1440)]
    public int CacheExpirationMinutes { get; set; } = 60;

    public string DataDirectory { get; set; } = "/data";
    
    public string LogDirectory { get; set; } = "/var/log/mcp";
}

public class SecurityConfig
{
    public bool EnableRateLimiting { get; set; } = true;
    
    [Range(1, 1000)]
    public int RateLimitPerMinute { get; set; } = 10;
    
    public bool EnableAuthentication { get; set; } = false;
    
    [MinLength(32)]
    public string JwtSecretKey { get; set; } = string.Empty;
    
    [Range(1, 10080)] // Max 1 week
    public int JwtExpirationMinutes { get; set; } = 60;
    
    public bool RequireHttps { get; set; } = true;
    
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}

public class ExternalApiConfig
{
    [Url]
    public string GitLabBaseUrl { get; set; } = "https://gitlab.com";
    
    [Url]
    public string ElasticsearchUrl { get; set; } = "http://elasticsearch:9200";
    
    [Range(1, 300)]
    public int ClaudeTimeoutSeconds { get; set; } = 30;
    
    [Range(1, 60)]
    public int GitHubTimeoutSeconds { get; set; } = 15;
    
    [Range(1, 60)]
    public int GitLabTimeoutSeconds { get; set; } = 15;
    
    [Range(1, 60)]
    public int ElasticsearchTimeoutSeconds { get; set; } = 10;
    
    [Range(1, 10)]
    public int RetryAttempts { get; set; } = 3;
}

public class MonitoringConfig
{
    public bool EnableMetrics { get; set; } = true;
    
    public bool EnableTracing { get; set; } = true;
    
    [Range(1, 2555)] // Max 7 years
    public int LogRetentionDays { get; set; } = 30;
    
    [Range(1, 365)]
    public int MetricsRetentionDays { get; set; } = 90;
    
    [Url]
    public string SentryDsn { get; set; } = string.Empty;
    
    public double SentrySampleRate { get; set; } = 0.1;
    
    public string LogLevel { get; set; } = "Information";
}

public class RateLimitConfig
{
    public int McpPerMinute { get; set; } = 10;
    
    public int AuthenticatedMcpPerMinute { get; set; } = 30;
    
    public int HealthCheckPerMinute { get; set; } = 60;
    
    public int MetricsPerMinute { get; set; } = 30;
    
    public string[] IpWhitelist { get; set; } = Array.Empty<string>();
    
    public string[] ClientWhitelist { get; set; } = Array.Empty<string>();
}