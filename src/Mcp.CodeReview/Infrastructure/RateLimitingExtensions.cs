using AspNetCoreRateLimit;
using Microsoft.Extensions.Caching.Memory;

namespace Mcp.CodeReview.Infrastructure;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddProductionRateLimit(this IServiceCollection services, IConfiguration configuration)
    {
        // Rate limiting configuration
        services.AddOptions();
        services.AddMemoryCache();
        
        // Configure IP rate limiting
        services.Configure<IpRateLimitOptions>(options =>
        {
            options.EnableEndpointRateLimiting = true;
            options.StackBlockedRequests = false;
            options.HttpStatusCode = 429;
            options.RealIpHeader = "X-Real-IP";
            options.ClientIdHeader = "X-ClientId";
            
            // General rate limiting rules
            options.GeneralRules = new List<RateLimitRule>
            {
                new RateLimitRule
                {
                    Endpoint = "GET:/health",
                    Period = "1m",
                    Limit = 60
                },
                new RateLimitRule
                {
                    Endpoint = "GET:/metrics",
                    Period = "1m", 
                    Limit = 30
                },
                new RateLimitRule
                {
                    Endpoint = "POST:/mcp",
                    Period = "1m",
                    Limit = GetMcpRateLimit(configuration)
                },
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1h",
                    Limit = 1000
                },
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1d",
                    Limit = 10000
                }
            };

            // IP whitelist for internal services
            options.IpWhitelist = GetIpWhitelist(configuration);
            
            // Client whitelist for authenticated services
            options.ClientWhitelist = GetClientWhitelist(configuration);
        });

        // Configure client rate limiting (for authenticated requests)
        services.Configure<ClientRateLimitOptions>(options =>
        {
            options.EnableEndpointRateLimiting = true;
            options.StackBlockedRequests = false;
            options.HttpStatusCode = 429;
            options.ClientIdHeader = "X-ClientId";
            
            // Client-specific rules
            options.GeneralRules = new List<RateLimitRule>
            {
                new RateLimitRule
                {
                    Endpoint = "POST:/mcp",
                    Period = "1m",
                    Limit = GetAuthenticatedMcpRateLimit(configuration)
                },
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1h",
                    Limit = 5000
                },
                new RateLimitRule
                {
                    Endpoint = "*",
                    Period = "1d",
                    Limit = 50000
                }
            };
        });

        // Rate limit counter and policy stores
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IClientPolicyStore, MemoryCacheClientPolicyStore>();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        return services;
    }

    public static IServiceCollection AddDistributedRateLimit(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            // Use Redis for distributed rate limiting
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "mcp-rate-limit";
            });
            
            services.AddSingleton<IRateLimitCounterStore, DistributedCacheRateLimitCounterStore>();
        }
        
        return services;
    }

    private static int GetMcpRateLimit(IConfiguration configuration)
    {
        return configuration.GetValue<int>("RateLimit:Mcp:PerMinute", 10);
    }

    private static int GetAuthenticatedMcpRateLimit(IConfiguration configuration)
    {
        return configuration.GetValue<int>("RateLimit:AuthenticatedMcp:PerMinute", 30);
    }

    private static List<string> GetIpWhitelist(IConfiguration configuration)
    {
        var whitelist = new List<string>();
        
        // Add localhost and private networks
        whitelist.AddRange(new[]
        {
            "127.0.0.1",
            "::1",
            "10.0.0.0/8",
            "172.16.0.0/12", 
            "192.168.0.0/16"
        });

        // Add configured IPs
        var configuredIps = configuration.GetSection("RateLimit:IpWhitelist").Get<string[]>();
        if (configuredIps != null)
        {
            whitelist.AddRange(configuredIps);
        }

        return whitelist;
    }

    private static List<string> GetClientWhitelist(IConfiguration configuration)
    {
        var whitelist = new List<string>();
        
        // Add configured client IDs
        var configuredClients = configuration.GetSection("RateLimit:ClientWhitelist").Get<string[]>();
        if (configuredClients != null)
        {
            whitelist.AddRange(configuredClients);
        }

        return whitelist;
    }
}