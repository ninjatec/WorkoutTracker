using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Collections.Generic;
using WorkoutTrackerWeb.Services.Redis;

namespace WorkoutTrackerWeb.HealthChecks
{
    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ILogger<RedisHealthCheck> _logger;
        private readonly IRedisCircuitBreakerService _circuitBreaker;

        public RedisHealthCheck(
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<RedisHealthCheck> logger,
            IRedisCircuitBreakerService circuitBreaker)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _logger = logger;
            _circuitBreaker = circuitBreaker;
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
                    { "Status", _connectionMultiplexer.GetStatus() },
                    { "CircuitState", _circuitBreaker.CurrentState.ToString() },
                    { "IsCircuitAvailable", _circuitBreaker.IsAvailable }
                };

                // Check if ping is slow (over 100ms)
                if (pingResult.TotalMilliseconds > 100)
                {
                    return HealthCheckResult.Degraded("Redis ping response time is high", null, data);
                }

                // Check circuit breaker status
                if (_circuitBreaker.CurrentState != CircuitState.Closed)
                {
                    return HealthCheckResult.Degraded($"Redis circuit breaker is in {_circuitBreaker.CurrentState} state", null, data);
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