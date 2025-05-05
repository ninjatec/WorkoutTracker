using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;

namespace WorkoutTrackerWeb.Services
{
    /// <summary>
    /// Service for ensuring safety when dealing with NULL values in SQL databases
    /// </summary>
    public class SqlNullSafetyService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<SqlNullSafetyService> _logger;
        
        // List of tables to exclude from automatic NULL fixing (commonly Hangfire tables)
        private static readonly HashSet<string> _excludedTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Job", "JobParameter", "JobQueue", "State", "Server", "List", "Hash", "Set", "Counter", "AggregatedCounter", "Lock"
        };

        public SqlNullSafetyService(
            WorkoutTrackerWebContext context,
            ILogger<SqlNullSafetyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Fixes NULL string values in the database by replacing them with empty strings
        /// </summary>
        /// <returns>Count of rows updated</returns>
        public async Task<int> FixNullStringValuesAsync()
        {
            int totalRowsAffected = 0;

            try
            {
                _logger.LogInformation("Starting to fix NULL string values in database");

                // Ensure we have a valid connection first
                if (_context?.Database?.GetDbConnection() == null)
                {
                    _logger.LogError("Database connection is not available");
                    return 0;
                }

                // Verify the connection string is set
                var connection = _context.Database.GetDbConnection() as SqlConnection;
                if (connection == null || string.IsNullOrEmpty(connection.ConnectionString))
                {
                    _logger.LogError("SQL Connection string is not initialized");
                    return 0;
                }

                // Table-specific fixes for known problematic columns
                // Updated to use plural table names and check existence first
                totalRowsAffected += await UpdateNullStringsInTableAsync("WorkoutSessions", new[] { "Name", "Description", "Notes" });
                totalRowsAffected += await UpdateNullStringsInTableAsync("ExerciseTypes", new[] { "Name", "Description", "Type", "Muscle", "Equipment", "Difficulty" });
                totalRowsAffected += await UpdateNullStringsInTableAsync("WorkoutExercises", new[] { "Name" });

                // Find all string columns across the database and ensure they don't have NULL values
                totalRowsAffected += await UpdateAllNullStringColumnsAsync();

                _logger.LogInformation("Successfully fixed NULL string values. Total rows updated: {RowCount}", totalRowsAffected);
                return totalRowsAffected;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fixing NULL string values in database");
                throw;
            }
        }

        /// <summary>
        /// Updates NULL string values in a specific table and columns
        /// </summary>
        /// <param name="tableName">The table to update</param>
        /// <param name="columnNames">The columns to update</param>
        /// <returns>Count of rows updated</returns>
        private async Task<int> UpdateNullStringsInTableAsync(string tableName, string[] columnNames)
        {
            int rowsAffected = 0;
            
            // Create a new connection to ensure it's properly initialized
            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                try
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    // First check if the table exists
                    if (!await TableExistsAsync(connection, tableName))
                    {
                        _logger.LogInformation("Table {TableName} does not exist, skipping NULL value updates", tableName);
                        return 0;
                    }

                    foreach (var column in columnNames)
                    {
                        // Check if column exists in the table
                        if (!await ColumnExistsAsync(connection, tableName, column))
                        {
                            _logger.LogInformation("Column {Column} does not exist in table {TableName}, skipping", column, tableName);
                            continue;
                        }

                        string sql = $"UPDATE [{tableName}] SET [{column}] = '' WHERE [{column}] IS NULL";
                        
                        using (var command = new SqlCommand(sql, connection))
                        {
                            try
                            {
                                int affected = await command.ExecuteNonQueryAsync();
                                rowsAffected += affected;
                                
                                if (affected > 0)
                                {
                                    _logger.LogInformation("Updated {Count} NULL values in {Table}.{Column}", 
                                        affected, tableName, column);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error updating NULL values in {Table}.{Column}", tableName, column);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to database or update NULL values");
                }
            }
            
            return rowsAffected;
        }

        /// <summary>
        /// Checks if a table exists in the database
        /// </summary>
        private async Task<bool> TableExistsAsync(SqlConnection connection, string tableName)
        {
            string sql = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_NAME = @tableName";
            
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@tableName", tableName);
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
        }

        /// <summary>
        /// Checks if a column exists in a table
        /// </summary>
        private async Task<bool> ColumnExistsAsync(SqlConnection connection, string tableName, string columnName)
        {
            string sql = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @tableName AND COLUMN_NAME = @columnName";
            
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@tableName", tableName);
                command.Parameters.AddWithValue("@columnName", columnName);
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
        }

        /// <summary>
        /// Updates ALL columns with NULL string values across the database
        /// </summary>
        /// <returns>Count of rows updated</returns>
        private async Task<int> UpdateAllNullStringColumnsAsync()
        {
            int rowsAffected = 0;
            
            // Get all tables and string columns in the database
            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                try
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    // Query to get all string columns in the database that can be NULL
                    string columnQuery = @"
                        SELECT 
                            TABLE_NAME, 
                            COLUMN_NAME
                        FROM 
                            INFORMATION_SCHEMA.COLUMNS
                        WHERE 
                            DATA_TYPE IN ('varchar', 'nvarchar', 'char', 'nchar', 'text', 'ntext')
                            AND IS_NULLABLE = 'YES'";
                    
                    var stringColumns = new List<(string TableName, string ColumnName)>();
                    
                    using (var command = new SqlCommand(columnQuery, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string tableName = reader.GetString(0);
                            string columnName = reader.GetString(1);
                            
                            // Skip excluded tables (like Hangfire tables)
                            if (_excludedTables.Contains(tableName))
                            {
                                _logger.LogDebug("Skipping excluded table {TableName} for NULL string updates", tableName);
                                continue;
                            }
                            
                            stringColumns.Add((tableName, columnName));
                        }
                    }
                    
                    // Update each column
                    foreach (var column in stringColumns)
                    {
                        string updateSql = $"UPDATE [{column.TableName}] SET [{column.ColumnName}] = '' WHERE [{column.ColumnName}] IS NULL";
                        
                        using (var command = new SqlCommand(updateSql, connection))
                        {
                            try
                            {
                                int affected = await command.ExecuteNonQueryAsync();
                                rowsAffected += affected;
                                
                                if (affected > 0)
                                {
                                    _logger.LogInformation("Automatically updated {Count} NULL values in {Table}.{Column}", 
                                        affected, column.TableName, column.ColumnName);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error automatically updating NULL values in {Table}.{Column}", 
                                    column.TableName, column.ColumnName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to database or query string columns");
                }
            }
            
            return rowsAffected;
        }
    }
}