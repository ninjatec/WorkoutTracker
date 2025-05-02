using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            
            var options = new ConfigurationOptions
            {
                EndPoints = { redisConfig.ConnectionString },
                AbortOnConnectFail = false,
                ConnectRetry = 5,
                ReconnectRetryPolicy = new ExponentialRetry(5000)
            };

            try
            {
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
}

public class RedisConfiguration
{
    public string ConnectionString { get; set; }
    public bool Enabled { get; set; }

    public ConfigurationOptions ToConfigurationOptions()
    {
        return new ConfigurationOptions
        {
            EndPoints = { ConnectionString },
            AbortOnConnectFail = false,
            ConnectRetry = 5,
            ReconnectRetryPolicy = new ExponentialRetry(5000)
        };
    }
}