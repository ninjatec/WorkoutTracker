using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class DatabaseHealthController : ControllerBase
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<DatabaseHealthController> _logger;
        private readonly DatabaseResilienceService _resilienceService;
        private readonly ConnectionStringBuilderService _connectionStringBuilder;

        public DatabaseHealthController(
            WorkoutTrackerWebContext context,
            ILogger<DatabaseHealthController> logger,
            DatabaseResilienceService resilienceService,
            ConnectionStringBuilderService connectionStringBuilder)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _resilienceService = resilienceService ?? throw new ArgumentNullException(nameof(resilienceService));
            _connectionStringBuilder = connectionStringBuilder ?? throw new ArgumentNullException(nameof(connectionStringBuilder));
        }

        [HttpGet("pool-stats")]
        public async Task<IActionResult> GetPoolStats()
        {
            try
            {
                var stats = new Dictionary<string, object>();
                
                // Get circuit breaker state
                var (isCircuitOpen, lastStateChange) = _resilienceService.GetCircuitBreakerState();
                stats["CircuitBreakerOpen"] = isCircuitOpen;
                stats["CircuitBreakerLastStateChange"] = lastStateChange;
                
                // Get configuration settings
                var metrics = _connectionStringBuilder.GetConnectionPoolMetrics();
                stats["ConfiguredMaxPoolSize"] = metrics.MaxPoolSize;
                stats["ConfiguredMinPoolSize"] = metrics.MinPoolSize;
                stats["ConnectionLifetime"] = metrics.ConnectionLifetime;
                stats["PoolingEnabled"] = metrics.PoolingEnabled;
                stats["ReadWriteSeparationEnabled"] = metrics.ReadWriteSeparationEnabled;
                
                if (metrics.ReadWriteSeparationEnabled)
                {
                    stats["ReadConnectionMaxPoolSize"] = metrics.ReadConnectionMaxPoolSize;
                    stats["ReadConnectionMinPoolSize"] = metrics.ReadConnectionMinPoolSize;
                }

                // Get runtime statistics
                await using var connection = _context.Database.GetDbConnection();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                if (connection is SqlConnection sqlConnection)
                {
                    stats["ConnectionState"] = connection.State.ToString();
                    stats["ConnectionString"] = SanitizeConnectionString(connection.ConnectionString);
                    stats["ServerVersion"] = connection.ServerVersion;
                    stats["Database"] = connection.Database;
                    stats["DataSource"] = connection.DataSource;
                    
                    // Query pool statistics
                    await QueryPoolStatisticsAsync(sqlConnection, stats);
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting database connection pool statistics");
                return StatusCode(500, new { Error = "Error getting database statistics", Message = ex.Message });
            }
        }
        
        [HttpGet("connections")]
        public async Task<IActionResult> GetActiveConnections()
        {
            try
            {
                var activeConnections = await GetActiveConnectionsAsync();
                return Ok(new { 
                    Timestamp = DateTime.UtcNow,
                    ConnectionCount = activeConnections.Count,
                    Connections = activeConnections
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active database connections");
                return StatusCode(500, new { Error = "Error getting active connections", Message = ex.Message });
            }
        }
        
        private async Task<List<Dictionary<string, object>>> GetActiveConnectionsAsync()
        {
            var connections = new List<Dictionary<string, object>>();
            
            await using var connection = _context.Database.GetDbConnection() as SqlConnection;
            if (connection == null)
            {
                return connections;
            }
            
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }
            
            using var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 
                    conn.session_id,
                    conn.client_net_address,
                    conn.auth_scheme,
                    conn.connect_time,
                    sess.login_time,
                    sess.host_name,
                    sess.program_name,
                    sess.login_name,
                    sess.status,
                    sess.last_request_start_time,
                    sess.last_request_end_time,
                    DATEDIFF(SECOND, sess.last_request_start_time, COALESCE(sess.last_request_end_time, GETDATE())) as request_duration_seconds
                FROM sys.dm_exec_connections conn
                JOIN sys.dm_exec_sessions sess ON conn.session_id = sess.session_id
                WHERE sess.session_id > 50
                ORDER BY sess.last_request_start_time DESC";
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var conn = new Dictionary<string, object>
                {
                    ["SessionId"] = reader.GetInt32(0),
                    ["ClientAddress"] = reader.IsDBNull(1) ? null : reader.GetString(1),
                    ["AuthScheme"] = reader.IsDBNull(2) ? null : reader.GetString(2),
                    ["ConnectTime"] = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                    ["LoginTime"] = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4),
                    ["HostName"] = reader.IsDBNull(5) ? null : reader.GetString(5),
                    ["ProgramName"] = reader.IsDBNull(6) ? null : reader.GetString(6),
                    ["LoginName"] = reader.IsDBNull(7) ? null : reader.GetString(7),
                    ["Status"] = reader.IsDBNull(8) ? null : reader.GetString(8),
                    ["LastRequestStart"] = reader.IsDBNull(9) ? DateTime.MinValue : reader.GetDateTime(9),
                    ["LastRequestEnd"] = reader.IsDBNull(10) ? DateTime.MinValue : reader.GetDateTime(10),
                    ["DurationSeconds"] = reader.IsDBNull(11) ? 0 : reader.GetInt32(11)
                };
                connections.Add(conn);
            }
            
            return connections;
        }
        
        private async Task QueryPoolStatisticsAsync(SqlConnection connection, Dictionary<string, object> stats)
        {
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                SELECT 
                    COUNT(*) AS total_connections, 
                    SUM(CASE WHEN status = 'sleeping' THEN 1 ELSE 0 END) AS idle_connections,
                    SUM(CASE WHEN status = 'running' THEN 1 ELSE 0 END) AS active_connections,
                    MAX(total_elapsed_time) AS longest_running_ms
                FROM sys.dm_exec_connections
                WHERE session_id > 50;
                
                SELECT 
                    COUNT(*) AS total_sessions,
                    SUM(CASE WHEN status = 'running' THEN 1 ELSE 0 END) AS active_sessions,
                    AVG(total_elapsed_time) AS avg_session_time_ms
                FROM sys.dm_exec_sessions
                WHERE session_id > 50;";
                
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    stats["TotalConnections"] = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    stats["IdleConnections"] = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                    stats["ActiveConnections"] = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                    stats["LongestRunningQueryMs"] = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                    
                    // Calculate pool utilization percentage
                    var metrics = _connectionStringBuilder.GetConnectionPoolMetrics();
                    int totalConnections = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    double utilizationPercent = metrics.MaxPoolSize > 0 ? 
                        (totalConnections / (double)metrics.MaxPoolSize) * 100.0 : 0;
                    stats["PoolUtilizationPercent"] = Math.Round(utilizationPercent, 2);
                }
                
                if (await reader.NextResultAsync() && await reader.ReadAsync())
                {
                    stats["TotalSessions"] = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    stats["ActiveSessions"] = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                    stats["AverageSessionTimeMs"] = reader.IsDBNull(2) ? 0 : Math.Round(reader.GetDouble(2), 2);
                }
            }
            catch (Exception ex)
            {
                stats["QueryError"] = ex.Message;
                _logger.LogWarning(ex, "Error querying SQL Server for pool statistics");
            }
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