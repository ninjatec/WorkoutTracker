using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data;
using Microsoft.Data.SqlClient;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

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
            bool connectionWasOpened = false;
            
            try
            {
                stats["ConnectionString"] = SanitizeConnectionString(connection.ConnectionString);
                stats["ServerVersion"] = connection.ServerVersion ?? "Unknown";
                
                // Track if we need to open the connection
                if (connection.State == System.Data.ConnectionState.Closed)
                {
                    await connection.OpenAsync();
                    connectionWasOpened = true;
                    stats["ConnectionOpened"] = "True (temporary for statistics)";
                }
                
                // Verify connection is open before proceeding
                if (connection.State == System.Data.ConnectionState.Open)
                {
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
                                // Handle potentially null values safely
                                stats["TotalConnections"] = !reader.IsDBNull(reader.GetOrdinal("connections")) ? 
                                    reader["connections"].ToString() : "0";
                                    
                                stats["IdleConnections"] = !reader.IsDBNull(reader.GetOrdinal("idle_connections")) ? 
                                    reader["idle_connections"].ToString() : "0";
                                    
                                stats["ActiveConnections"] = !reader.IsDBNull(reader.GetOrdinal("active_connections")) ? 
                                    reader["active_connections"].ToString() : "0";
                            }
                        }
                    }
                }
                else
                {
                    stats["Error"] = "Connection could not be opened to retrieve pool statistics";
                }
            }
            catch (Exception ex)
            {
                stats["Error"] = $"Error getting pool stats: {ex.Message}";
            }
            finally
            {
                // Close the connection only if we opened it
                if (connectionWasOpened && connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                    stats["ConnectionClosed"] = "True (closed after getting statistics)";
                }
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

        /// <summary>
        /// Safely get a string value from a potentially null database field
        /// </summary>
        /// <param name="reader">The data reader</param>
        /// <param name="columnName">The column name</param>
        /// <returns>The string value or empty string if null</returns>
        public static string SafeGetString(this SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                if (reader.IsDBNull(ordinal))
                    return string.Empty;
                    
                return reader.GetString(ordinal);
            }
            catch
            {
                // If anything goes wrong (column doesn't exist, etc.), return empty string
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Safely get a string value from a potentially null database field by ordinal position
        /// </summary>
        /// <param name="reader">The data reader</param>
        /// <param name="ordinal">The column ordinal position</param>
        /// <returns>The string value or empty string if null</returns>
        public static string SafeGetString(this SqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        /// <summary>
        /// Generic method to safely get any type of value from SqlDataReader
        /// </summary>
        public static T SafeGet<T>(this SqlDataReader reader, int ordinal, T defaultValue = default)
        {
            return reader.IsDBNull(ordinal) ? defaultValue : (T)reader.GetValue(ordinal);
        }

        /// <summary>
        /// Generic method to safely get any type of value from IDataReader
        /// </summary>
        public static T SafeGet<T>(this IDataReader reader, int ordinal, T defaultValue = default)
        {
            return reader.IsDBNull(ordinal) ? defaultValue : (T)reader.GetValue(ordinal);
        }

        /// <summary>
        /// Ensures a LINQ query doesn't include entities with NULL values in critical string properties
        /// </summary>
        public static IQueryable<T> ExcludeNulls<T, TProperty>(this IQueryable<T> query, 
            System.Linq.Expressions.Expression<Func<T, TProperty>> propertySelector) where TProperty : class
        {
            return query.Where(e => propertySelector.Compile()(e) != null);
        }

        /// <summary>
        /// Get the current user with safe string handling to prevent SqlNullValueException
        /// </summary>
        public static async Task<Models.Identity.AppUser> GetCurrentUserAsync(this WorkoutTrackerWebContext context, System.Security.Claims.ClaimsPrincipal user)
        {
            if (user == null || !user.Identity.IsAuthenticated)
                return null;
                
            string userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return null;
                
            // The WorkoutTrackerWebContext contains User records, not Identity users
            // We need to retrieve the application user from ApplicationDbContext
            using (var scope = new Microsoft.Extensions.DependencyInjection.ServiceCollection()
                .AddEntityFrameworkSqlServer()
                .BuildServiceProvider())
            {
                // Get connection info from current context to create a consistent connection
                string connectionString = context.Database.GetDbConnection().ConnectionString;
                
                // Create options for ApplicationDbContext which has the AppUser entities
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseSqlServer(connectionString);
                
                using (var identityContext = new ApplicationDbContext(optionsBuilder.Options))
                {
                    return await identityContext.Users
                        .FirstOrDefaultAsync(u => u.Id == userId);
                }
            }
        }
    }
}