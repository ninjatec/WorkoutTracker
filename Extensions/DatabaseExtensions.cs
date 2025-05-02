using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using WorkoutTrackerWeb.Data;

namespace WorkoutTrackerWeb.Extensions
{
    public static class DatabaseExtensions
    {
        /// <summary>
        /// Gets database connection statistics and performance metrics
        /// </summary>
        public static async Task<Dictionary<string, string>> GetDatabaseStatisticsAsync(this WorkoutTrackerWebContext context)
        {
            var statistics = new Dictionary<string, string>();
            
            try
            {
                // Get connection pool statistics if possible
                var factory = context.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IRelationalConnection>();
                var connection = factory.DbConnection as SqlConnection;
                
                if (connection != null)
                {
                    var poolStats = await GetConnectionPoolStatsAsync(connection);
                    foreach (var stat in poolStats)
                    {
                        statistics[stat.Key] = stat.Value;
                    }
                }
                else
                {
                    statistics["Connection"] = "Active";
                    statistics["PoolType"] = "Non-SQL Server or custom connection";
                }
                
                // Add database size information
                statistics["DatabaseName"] = context.Database.GetDbConnection().Database;
                statistics["Server"] = context.Database.GetDbConnection().DataSource;
                
                // Add simple performance stats
                statistics["CommandTimeout"] = context.Database.GetCommandTimeout()?.ToString() ?? "Default";
                
                // Add connection state
                statistics["ConnectionState"] = context.Database.GetDbConnection().State.ToString();
            }
            catch (Exception ex)
            {
                statistics["Error"] = $"Failed to get database statistics: {ex.Message}";
                statistics["ConnectionState"] = "Unknown";
            }
            
            return statistics;
        }
        
        private static async Task<Dictionary<string, string>> GetConnectionPoolStatsAsync(SqlConnection connection)
        {
            var stats = new Dictionary<string, string>();
            
            try
            {
                stats["ConnectionString"] = SanitizeConnectionString(connection.ConnectionString);
                stats["ServerVersion"] = connection.ServerVersion ?? "Unknown";
                
                // If connection is closed, we can't get the statistics
                if (connection.State == System.Data.ConnectionState.Closed)
                {
                    await connection.OpenAsync();
                    stats["ConnectionOpened"] = "True (temporary for statistics)";
                }
                
                // Get connection pool statistics using SQL Server dynamic management views
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT 
                            COUNT(*) as connections,
                            SUM(CASE WHEN status = 'sleeping' THEN 1 ELSE 0 END) as idle_connections,
                            SUM(CASE WHEN status = 'running' THEN 1 ELSE 0 END) as active_connections
                        FROM sys.dm_exec_connections
                        WHERE session_id > 50"; // Exclude system sessions
                        
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            stats["TotalConnections"] = reader["connections"].ToString();
                            stats["IdleConnections"] = reader["idle_connections"].ToString();
                            stats["ActiveConnections"] = reader["active_connections"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                stats["Error"] = $"Error getting pool stats: {ex.Message}";
            }
            
            return stats;
        }
        
        private static string SanitizeConnectionString(string connectionString)
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
    }
}