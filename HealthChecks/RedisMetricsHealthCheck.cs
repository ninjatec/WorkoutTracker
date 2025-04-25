using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace WorkoutTrackerWeb.HealthChecks
{
    /// <summary>
    /// Health check that monitors Redis metrics and reports detailed statistics.
    /// </summary>
    public class RedisMetricsHealthCheck : IHealthCheck
    {
        private readonly ILogger<RedisMetricsHealthCheck> _logger;
        private readonly string _redisConnectionString;

        public RedisMetricsHealthCheck(
            ILogger<RedisMetricsHealthCheck> logger,
            IOptions<RedisOptions> redisOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _redisConnectionString = redisOptions?.Value?.ConnectionString ?? 
                                    throw new ArgumentNullException(nameof(redisOptions));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>
            {
                { "LastChecked", DateTime.UtcNow }
            };

            try
            {
                _logger.LogDebug("Checking Redis connection health and gathering metrics");
                
                // Configure Redis connection
                var options = ConfigurationOptions.Parse(_redisConnectionString);
                options.AbortOnConnectFail = false;
                options.ConnectTimeout = 5000; // Short timeout for health check
                
                using var connection = await ConnectionMultiplexer.ConnectAsync(options);
                
                if (!connection.IsConnected)
                {
                    _logger.LogWarning("Redis connection failed during health check");
                    return HealthCheckResult.Unhealthy("Redis connection failed", data: data);
                }

                // Get server instances (usually one in standalone mode, multiple in cluster)
                var endpoints = connection.GetEndPoints();
                var serverStats = new Dictionary<string, string>();
                
                // Track key statistics
                long totalKeys = 0;
                long totalExpiringKeys = 0;
                long totalHits = 0;
                long totalMisses = 0;
                double usedMemoryMB = 0;
                
                _logger.LogDebug("Connected to {EndpointCount} Redis endpoints", endpoints.Length);
                
                foreach (var endpoint in endpoints)
                {
                    var server = connection.GetServer(endpoint);
                    
                    // Skip if this is a replica/slave in a cluster
                    if (server.IsReplica) continue;
                    
                    try
                    {
                        // Get Redis INFO
                        var info = server.Info();
                        foreach (var group in info)
                        {
                            foreach (var entry in group)
                            {
                                // Add important stats to the response
                                if (entry.Key == "used_memory_human" || 
                                    entry.Key == "total_connections_received" ||
                                    entry.Key == "connected_clients" ||
                                    entry.Key == "uptime_in_seconds" ||
                                    entry.Key == "instantaneous_ops_per_sec")
                                {
                                    serverStats[entry.Key] = entry.Value;
                                }
                                
                                // Extract memory usage
                                if (entry.Key == "used_memory")
                                {
                                    if (long.TryParse(entry.Value, out long usedMemoryBytes))
                                    {
                                        usedMemoryMB += Math.Round(usedMemoryBytes / (1024.0 * 1024.0), 2);
                                    }
                                }
                                
                                // Extract hit/miss statistics
                                if (entry.Key == "keyspace_hits" && long.TryParse(entry.Value, out long hits))
                                {
                                    totalHits += hits;
                                }
                                if (entry.Key == "keyspace_misses" && long.TryParse(entry.Value, out long misses))
                                {
                                    totalMisses += misses;
                                }
                            }
                        }
                        
                        // Parse database statistics for key counts
                        foreach (var group in info)
                        {
                            if (group.Key == "Keyspace")
                            {
                                foreach (var entry in group)
                                {
                                    // Parse keyspace info which is in format: "keys=123,expires=45,avg_ttl=3600"
                                    string dbStats = entry.Value;
                                    
                                    // Extract keys count
                                    int keysStart = dbStats.IndexOf("keys=") + 5;
                                    if (keysStart >= 5)
                                    {
                                        int keysEnd = dbStats.IndexOf(",", keysStart);
                                        if (keysEnd > keysStart)
                                        {
                                            if (int.TryParse(dbStats.Substring(keysStart, keysEnd - keysStart), out int keys))
                                            {
                                                totalKeys += keys;
                                            }
                                        }
                                    }
                                    
                                    // Extract expires count
                                    int expiresStart = dbStats.IndexOf("expires=") + 8;
                                    if (expiresStart >= 8)
                                    {
                                        int expiresEnd = dbStats.IndexOf(",", expiresStart);
                                        if (expiresEnd == -1) expiresEnd = dbStats.Length;
                                        
                                        if (expiresEnd > expiresStart)
                                        {
                                            if (int.TryParse(dbStats.Substring(expiresStart, expiresEnd - expiresStart), out int expires))
                                            {
                                                totalExpiringKeys += expires;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error collecting Redis server metrics from {Endpoint}", endpoint);
                    }
                }
                
                // Calculate hit rate
                double hitRate = 0;
                if (totalHits + totalMisses > 0)
                {
                    hitRate = Math.Round(100.0 * totalHits / (totalHits + totalMisses), 2);
                }
                
                // Add calculated metrics to health data
                data["serverStats"] = serverStats;
                data["usedMemoryMB"] = usedMemoryMB;
                data["keyCount"] = totalKeys;
                data["expiringKeys"] = totalExpiringKeys;
                data["hitRate"] = hitRate;
                data["totalHits"] = totalHits;
                data["totalMisses"] = totalMisses;

                _logger.LogInformation("Redis health check successful: Hit Rate={HitRate}%, Memory={Memory}MB, Keys={Keys}", 
                    hitRate, usedMemoryMB, totalKeys);
                
                return HealthCheckResult.Healthy("Redis connection is healthy", data: data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed: {ErrorMessage}", ex.Message);
                
                data.Add("Error", ex.Message);
                data.Add("ErrorType", ex.GetType().Name);
                
                return HealthCheckResult.Unhealthy("Redis connection failed", ex, data);
            }
        }
    }

    /// <summary>
    /// Options class for Redis connection configuration
    /// </summary>
    public class RedisOptions
    {
        public string ConnectionString { get; set; }
    }
}