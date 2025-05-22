using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using WorkoutTrackerWeb.Services.Redis;

namespace WorkoutTrackerWeb.HealthChecks
{
    public class RedisMetricsHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisMetricsHealthCheck> _logger;
        private readonly RedisOptions _options;

        public RedisMetricsHealthCheck(
            IConnectionMultiplexer redis,
            ILogger<RedisMetricsHealthCheck> logger,
            IOptions<RedisOptions> options)
        {
            _redis = redis;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (!_options.Enabled)
            {
                return HealthCheckResult.Healthy("Redis is not enabled in this environment");
            }

            try
            {
                if (!_redis.IsConnected)
                {
                    return HealthCheckResult.Unhealthy("Redis is not connected");
                }

                var data = new Dictionary<string, object>();
                bool hasErrors = false;

                foreach (var endpoint in _redis.GetEndPoints())
                {
                    var server = _redis.GetServer(endpoint);
                    if (!server.IsConnected)
                    {
                        data.Add($"endpoint_{endpoint}", "Not connected");
                        hasErrors = true;
                        continue;
                    }

                    // Use async operations where possible
                    var info = await server.InfoAsync();
                    var metrics = new Dictionary<string, string>();

                    foreach (var group in info)
                    {
                        foreach (var item in group)
                        {
                            switch (item.Key)
                            {
                                case "used_memory":
                                case "connected_clients":
                                case "instantaneous_ops_per_sec":
                                    metrics.Add(item.Key, item.Value);
                                    break;
                            }
                        }
                    }

                    data.Add($"endpoint_{endpoint}", metrics);
                }

                return hasErrors 
                    ? HealthCheckResult.Degraded("Some Redis endpoints are not responding", null, data)
                    : HealthCheckResult.Healthy("Redis is healthy", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed");
                return HealthCheckResult.Unhealthy("Redis health check failed", ex);
            }
        }
    }
}