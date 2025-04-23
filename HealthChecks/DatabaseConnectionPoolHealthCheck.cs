using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerweb.Data;

namespace WorkoutTrackerWeb.HealthChecks
{
    /// <summary>
    /// Health check that monitors database connection pool status and circuit breaker state.
    /// </summary>
    public class DatabaseConnectionPoolHealthCheck : IHealthCheck
    {
        private readonly DatabaseResilienceService _databaseResilienceService;
        private readonly ILogger<DatabaseConnectionPoolHealthCheck> _logger;
        private readonly WorkoutTrackerWebContext _dbContext;

        public DatabaseConnectionPoolHealthCheck(
            DatabaseResilienceService databaseResilienceService,
            ILogger<DatabaseConnectionPoolHealthCheck> logger,
            WorkoutTrackerWebContext dbContext)
        {
            _databaseResilienceService = databaseResilienceService ?? throw new ArgumentNullException(nameof(databaseResilienceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if circuit breaker is open
                var (isCircuitOpen, lastStateChange) = _databaseResilienceService.GetCircuitBreakerState();
                
                var data = new Dictionary<string, object>
                {
                    { "CircuitBreaker", isCircuitOpen ? "Open" : "Closed" },
                    { "LastStateChange", lastStateChange?.ToString() ?? "N/A" }
                };
                
                if (isCircuitOpen)
                {
                    _logger.LogWarning("Database circuit breaker is currently open. Last state change: {LastStateChange}", 
                        lastStateChange?.ToString() ?? "Unknown");
                    
                    return HealthCheckResult.Degraded(
                        "Database circuit breaker is open. Connection pooling is temporarily suspended.",
                        data: data);
                }

                // Get connection pool statistics through DbContext
                await _databaseResilienceService.ExecuteWithResilienceAsync(async (ct) => {
                    await _dbContext.Database.ExecuteSqlRawAsync(
                        "SELECT 1", // Simple query to test connection
                        ct);
                }, "HealthCheck.ConnectionTest", cancellationToken);
                
                // Add SQL Server connection pool statistics if possible
                try
                {
                    // Get connection information from the connection string
                    var connectionString = _dbContext.Database.GetConnectionString();
                    if (connectionString != null)
                    {
                        var poolingData = ExtractPoolingInfoFromConnectionString(connectionString);
                        foreach (var item in poolingData)
                        {
                            data.Add(item.Key, item.Value);
                        }
                    }
                    else
                    {
                        data.Add("ConnectionPoolInfo", "Connection string not available");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Unable to retrieve connection pool statistics");
                    data.Add("ConnectionPoolInfo", "Unavailable due to error: " + ex.Message);
                }

                return HealthCheckResult.Healthy("Database connection pool is healthy", data: data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing database connection pool health check");
                
                return HealthCheckResult.Unhealthy(
                    "Database connection pool health check failed",
                    ex,
                    new Dictionary<string, object>
                    {
                        { "ExceptionType", ex.GetType().Name },
                        { "ExceptionMessage", ex.Message }
                    });
            }
        }

        private Dictionary<string, string> ExtractPoolingInfoFromConnectionString(string connectionString)
        {
            var poolingData = new Dictionary<string, string>();
            var connectionOptions = connectionString.Split(';')
                .Select(s => s.Trim())
                .Where(s => s.StartsWith("Pooling=", StringComparison.OrdinalIgnoreCase) || 
                            s.StartsWith("Max Pool Size=", StringComparison.OrdinalIgnoreCase) ||
                            s.StartsWith("Min Pool Size=", StringComparison.OrdinalIgnoreCase))
                .ToList();

            bool poolingEnabled = !connectionOptions.Any(o => o.Equals("Pooling=false", StringComparison.OrdinalIgnoreCase));
            poolingData.Add("PoolingEnabled", poolingEnabled.ToString());

            if (poolingEnabled)
            {
                foreach (var option in connectionOptions)
                {
                    var parts = option.Split('=');
                    if (parts.Length == 2)
                    {
                        poolingData.Add(parts[0], parts[1]);
                    }
                }
            }

            return poolingData;
        }
    }
}