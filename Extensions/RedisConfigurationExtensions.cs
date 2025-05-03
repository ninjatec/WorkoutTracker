using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WorkoutTrackerWeb.Services.Redis;

namespace WorkoutTrackerWeb.Extensions;

public static class RedisConfigurationExtensions
{
    internal static IServiceCollection ConfigureRedisServices(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment,
        ILogger<Program> logger)
    {
        var redisConfiguration = configuration.GetSection("Redis").Get<RedisConfiguration>();
        if (redisConfiguration?.Enabled != true)
        {
            logger.LogInformation("Redis is disabled in configuration");
            return services;
        }

        services.Configure<RedisConfiguration>(configuration.GetSection("Redis"));
        
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConfiguration.ToConfigurationOptions().ToString();
            options.InstanceName = "WorkoutTracker_";
        });

        logger.LogInformation("Redis services configured successfully");
        return services;
    }

    public static IServiceCollection AddRedisConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConfig = configuration.GetSection("Redis").Get<RedisConfiguration>();
        
        // If Redis is not configured or disabled, skip Redis registration
        if (redisConfig == null || !redisConfig.Enabled)
        {
            return services;
        }
        
        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RedisConfiguration>>();
            
            // Ensure we have a connection string before proceeding
            if (string.IsNullOrEmpty(redisConfig.ConnectionString))
            {
                logger.LogWarning("Redis connection string is null or empty. Redis features will be unavailable.");
                throw new InvalidOperationException("Redis connection string is not configured");
            }
            
            try
            {
                // Parse the connection string properly
                string connectionString = redisConfig.ConnectionString;
                
                // Extract the host:port part
                string hostPort = connectionString;
                if (connectionString.Contains(","))
                {
                    hostPort = connectionString.Substring(0, connectionString.IndexOf(","));
                }
                
                var options = ConfigurationOptions.Parse(connectionString);
                
                // Ensure we have the right endpoint
                options.EndPoints.Clear();
                options.EndPoints.Add(hostPort);
                
                // Set additional connection options if they're not in the connection string
                if (!connectionString.Contains("abortConnect="))
                    options.AbortOnConnectFail = false;
                
                if (!connectionString.Contains("connectRetry="))
                    options.ConnectRetry = 5;
                
                options.ReconnectRetryPolicy = new ExponentialRetry(5000);
                
                logger.LogInformation("Connecting to Redis at {Endpoints} with options: {Options}", 
                    string.Join(", ", options.EndPoints), options);
                
                return ConnectionMultiplexer.Connect(options);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to Redis");
                throw;
            }
        });
        
        // Also configure Redis options for services
        services.Configure<RedisConfiguration>(configuration.GetSection("Redis"));

        return services;
    }

    public static IServiceCollection ConfigureRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RedisConfiguration>(configuration.GetSection("Redis"));
        return services;
    }

    public static IServiceCollection AddRedisCacheWithFallback(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        var multiplexerOptions = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectTimeout = 3000, // 3 seconds timeout
            SyncTimeout = 3000,
            ConnectRetry = 3,
            ReconnectRetryPolicy = new ExponentialRetry(1000, 10000), // Start at 1s, max 10s
        };

        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            multiplexerOptions.EndPoints.Add(redisConnectionString);
            
            // Add Redis health check with connection pooling
            services.AddSingleton<IConnectionMultiplexer>(sp => ConnectionMultiplexer.Connect(multiplexerOptions));

            // Configure cache with sensible options and resilience handling
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "WorkoutTracker:";
                options.ConfigurationOptions = multiplexerOptions;
            });

            // Add health checks for Redis
            services.AddHealthChecks()
                .AddCheck<HealthChecks.RedisHealthCheck>(
                    "redis-connectivity", 
                    HealthStatus.Degraded, 
                    new[] { "redis", "cache" });

            // Register the resilient cache services
            services.AddSingleton<IResilientCacheService, ResilientCacheService>();
        }
        else
        {
            // Fall back to memory cache if Redis connection string is not provided
            services.AddDistributedMemoryCache(options => 
            {
                options.SizeLimit = 100 * 1024 * 1024; // 100 MB limit
            });
            
            // Use a memory-based fallback service instead of Redis
            services.AddSingleton<IResilientCacheService, FallbackCacheService>();
        }

        return services;
    }
}

public class RedisConfiguration
{
    public string ConnectionString { get; set; }
    public bool Enabled { get; set; }

    public ConfigurationOptions ToConfigurationOptions()
    {
        try
        {
            // Parse the connection string properly
            string connectionString = ConnectionString;
            
            // Extract the host:port part
            string hostPort = connectionString;
            if (connectionString.Contains(","))
            {
                hostPort = connectionString.Substring(0, connectionString.IndexOf(","));
            }
            
            var options = ConfigurationOptions.Parse(connectionString);
            
            // Ensure we have the right endpoint
            options.EndPoints.Clear();
            options.EndPoints.Add(hostPort);
            
            return options;
        }
        catch
        {
            // Fallback to basic configuration
            return new ConfigurationOptions
            {
                EndPoints = { ConnectionString.Split(',')[0] },
                AbortOnConnectFail = false,
                ConnectRetry = 5,
                ReconnectRetryPolicy = new ExponentialRetry(5000)
            };
        }
    }
}