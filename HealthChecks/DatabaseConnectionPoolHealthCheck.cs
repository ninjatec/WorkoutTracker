using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Data;

namespace WorkoutTrackerWeb.HealthChecks
{
    /// <summary>
    /// Health check that monitors database connection pool status and circuit breaker state.
    /// </summary>
    public class DatabaseConnectionPoolHealthCheck : IHealthCheck
    {
        private readonly DatabaseResilienceService _databaseResilienceService;
        private readonly ConnectionStringBuilderService _connectionStringBuilder;
        private readonly ILogger<DatabaseConnectionPoolHealthCheck> _logger;
        private readonly WorkoutTrackerWebContext _dbContext;

        public DatabaseConnectionPoolHealthCheck(
            DatabaseResilienceService databaseResilienceService,
            ConnectionStringBuilderService connectionStringBuilder,
            ILogger<DatabaseConnectionPoolHealthCheck> logger,
            WorkoutTrackerWebContext dbContext)
        {
            _databaseResilienceService = databaseResilienceService ?? throw new ArgumentNullException(nameof(databaseResilienceService));
            _connectionStringBuilder = connectionStringBuilder ?? throw new ArgumentNullException(nameof(connectionStringBuilder));
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

                // Test connection and get pool statistics through DbContext
                await using var connection = _dbContext.Database.GetDbConnection();
                try
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync(cancellationToken);
                    }

                    // Execute a simple query to verify connection
                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT 1";
                    await command.ExecuteScalarAsync(cancellationToken);

                    // Get connection string information
                    var connectionString = _dbContext.Database.GetConnectionString();
                    if (connectionString != null)
                    {
                        var poolingData = ExtractPoolingInfoFromConnectionString(connectionString);
                        foreach (var item in poolingData)
                        {
                            data[item.Key] = item.Value;
                        }

                        // Add additional connection information
                        data["DatabaseName"] = connection.Database;
                        data["Server"] = connection.DataSource;
                        data["CommandTimeout"] = _dbContext.Database.GetCommandTimeout()?.ToString() ?? "30";
                        data["ConnectionState"] = connection.State.ToString();
                        
                        // Add configuration information from service
                        var metrics = _connectionStringBuilder.GetConnectionPoolMetrics();
                        data["ConfiguredMaxPoolSize"] = metrics.MaxPoolSize;
                        data["ConfiguredMinPoolSize"] = metrics.MinPoolSize;
                        data["ReadWriteSeparation"] = metrics.ReadWriteSeparationEnabled;
                        if (metrics.ReadWriteSeparationEnabled)
                        {
                            data["ReadConnectionMaxPoolSize"] = metrics.ReadConnectionMaxPoolSize;
                            data["ReadConnectionMinPoolSize"] = metrics.ReadConnectionMinPoolSize;
                        }
                        
                        // Add actual pool utilization statistics from SQL Server
                        await CollectPoolUtilizationStatisticsAsync(connection, data, cancellationToken);
                    }

                    return HealthCheckResult.Healthy("Database connection pool is healthy", data: data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking database connection pool health");
                    return HealthCheckResult.Unhealthy(
                        "Database connection pool check failed",
                        ex,
                        data);
                }
                finally
                {
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        await connection.CloseAsync();
                    }
                }
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
        
        /// <summary>
        /// Collects SQL Server connection pool utilization statistics
        /// </summary>
        private async Task CollectPoolUtilizationStatisticsAsync(System.Data.Common.DbConnection connection, Dictionary<string, object> data, CancellationToken cancellationToken)
        {
            if (connection is SqlConnection sqlConn)
            {
                try
                {
                    using var cmd = sqlConn.CreateCommand();
                    cmd.CommandText = @"
                    SELECT 
                        COUNT(*) AS total_connections, 
                        SUM(CASE WHEN status = 'sleeping' THEN 1 ELSE 0 END) AS idle_connections,
                        SUM(CASE WHEN status = 'running' THEN 1 ELSE 0 END) AS active_connections,
                        MAX(total_elapsed_time) AS longest_running_ms
                    FROM sys.dm_exec_connections
                    WHERE session_id > 50;
                    
                    SELECT 
                        DB_NAME() AS database_name,
                        COUNT(*) AS total_sessions,
                        SUM(CASE WHEN status = 'running' THEN 1 ELSE 0 END) AS active_sessions,
                        AVG(total_elapsed_time) AS avg_session_time_ms
                    FROM sys.dm_exec_sessions
                    WHERE session_id > 50;";
                    
                    using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        data["TotalConnections"] = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        data["IdleConnections"] = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        data["ActiveConnections"] = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        data["LongestRunningQueryMs"] = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                        
                        // Calculate pool utilization percentage
                        var metrics = _connectionStringBuilder.GetConnectionPoolMetrics();
                        int totalConnections = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        double utilizationPercent = metrics.MaxPoolSize > 0 ? 
                            (totalConnections / (double)metrics.MaxPoolSize) * 100.0 : 0;
                        data["PoolUtilizationPercent"] = Math.Round(utilizationPercent, 2);
                    }
                    
                    if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken))
                    {
                        data["TotalSessions"] = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        data["ActiveSessions"] = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        data["AverageSessionTimeMs"] = reader.IsDBNull(3) ? 0 : Math.Round(reader.GetDouble(3), 2);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to collect detailed pool utilization statistics");
                    data["PoolUtilizationError"] = ex.Message;
                }
            }
        }

        private Dictionary<string, string> ExtractPoolingInfoFromConnectionString(string connectionString)
        {
            var poolingData = new Dictionary<string, string>();
            
            try
            {
                // Use sanitized connection string to avoid exposing credentials
                poolingData["ConnectionString"] = SanitizeConnectionString(connectionString);
                
                var connectionOptions = connectionString.Split(';')
                    .Select(s => s.Trim())
                    .Where(s => s.StartsWith("Pooling=", StringComparison.OrdinalIgnoreCase) || 
                               s.StartsWith("Max Pool Size=", StringComparison.OrdinalIgnoreCase) ||
                               s.StartsWith("Min Pool Size=", StringComparison.OrdinalIgnoreCase) ||
                               s.StartsWith("Connection Lifetime=", StringComparison.OrdinalIgnoreCase) ||
                               s.StartsWith("Connection Reset=", StringComparison.OrdinalIgnoreCase) ||
                               s.StartsWith("ApplicationIntent=", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                bool poolingEnabled = !connectionOptions.Any(o => o.Equals("Pooling=false", StringComparison.OrdinalIgnoreCase));
                poolingData["PoolingEnabled"] = poolingEnabled.ToString();

                if (poolingEnabled)
                {
                    foreach (var option in connectionOptions)
                    {
                        var parts = option.Split('=');
                        if (parts.Length == 2)
                        {
                            poolingData[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                poolingData["Error"] = $"Error parsing connection string: {ex.Message}";
            }

            return poolingData;
        }
        
        private string SanitizeConnectionString(string connectionString)
        {
            try
            {
                // Redact sensitive information
                var builder = new SqlConnectionStringBuilder(connectionString);
                
                // Mask the password if present
                if (!string.IsNullOrEmpty(builder.Password))
                {
                    builder.Password = "********";
                }
                
                return builder.ToString();
            }
            catch
            {
                // If we can't parse the connection string, return a placeholder
                return "[Invalid connection string]";
            }
        }
    }
}