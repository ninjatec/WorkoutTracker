using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace WorkoutTrackerWeb.Services.Redis
{
    public class RedisMonitoringService : IHostedService, IDisposable
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisMonitoringService> _logger;
        private readonly RedisConfiguration _config;
        private Timer _healthCheckTimer;

        public RedisMonitoringService(
            IConnectionMultiplexer redis,
            ILogger<RedisMonitoringService> logger,
            IOptions<RedisConfiguration> config)
        {
            _redis = redis;
            _logger = logger;
            _config = config.Value;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Redis monitoring service is starting");

            _healthCheckTimer = new Timer(DoHealthCheck, null, TimeSpan.Zero, 
                TimeSpan.FromSeconds(30));

            return Task.CompletedTask;
        }

        private void DoHealthCheck(object state)
        {
            try
            {
                if (!_redis.IsConnected)
                {
                    _logger.LogWarning("Redis is not connected");
                    return;
                }

                foreach (var endpoint in _redis.GetEndPoints())
                {
                    var server = _redis.GetServer(endpoint);
                    if (!server.IsConnected)
                    {
                        _logger.LogWarning("Redis server at {Endpoint} is not connected", endpoint);
                        continue;
                    }

                    var info = server.Info();
                    foreach (var group in info)
                    {
                        foreach (var item in group)
                        {
                            if (item.Key == "used_memory")
                            {
                                RedisMetrics.MemoryUsed.Set(double.Parse(item.Value));
                            }
                            else if (item.Key == "connected_clients")
                            {
                                RedisMetrics.ConnectedClients.Set(double.Parse(item.Value));
                            }
                            else if (item.Key == "instantaneous_ops_per_sec")
                            {
                                RedisMetrics.OperationsPerSecond.Set(double.Parse(item.Value));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing Redis health check");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Redis monitoring service is stopping");

            _healthCheckTimer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _healthCheckTimer?.Dispose();
        }
    }
}