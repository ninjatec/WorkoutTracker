using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace WorkoutTrackerWeb.HealthChecks
{
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<RedisHealthCheck> _logger;

        public RedisHealthCheck(
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<RedisHealthCheck> logger)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Basic check to see if we can ping Redis
                if (!_connectionMultiplexer.IsConnected)
                {
                    return HealthCheckResult.Degraded("Redis connection is not established");
                }

                // Get a database and perform a simple ping operation
                var db = _connectionMultiplexer.GetDatabase();
                var pingResult = await db.PingAsync();

                // Add some details to the health check
                var data = new Dictionary<string, object>
                {
                    { "Ping", pingResult.TotalMilliseconds },
                    { "ConnectedEndpoints", _connectionMultiplexer.GetEndPoints().Length },
                    { "Status", _connectionMultiplexer.GetStatus() }
                };

                // Check if ping is slow (over 100ms)
                if (pingResult.TotalMilliseconds > 100)
                {
                    return HealthCheckResult.Degraded("Redis ping response time is high", null, data);
                }

                return HealthCheckResult.Healthy("Redis connection is healthy", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed");
                return HealthCheckResult.Unhealthy("Redis connection failed", ex);
            }
        }
    }
}