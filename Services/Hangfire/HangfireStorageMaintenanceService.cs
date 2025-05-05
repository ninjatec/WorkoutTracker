using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Hangfire.Common;

namespace WorkoutTrackerWeb.Services.Hangfire
{
    /// <summary>
    /// Service that provides storage cleanup and maintenance routines for Hangfire jobs
    /// </summary>
    public class HangfireStorageMaintenanceService
    {
        private readonly ILogger<HangfireStorageMaintenanceService> _logger;
        private readonly string _connectionString;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Creates a new instance of the <see cref="HangfireStorageMaintenanceService"/> class
        /// </summary>
        public HangfireStorageMaintenanceService(
            ILogger<HangfireStorageMaintenanceService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        /// <summary>
        /// Register the maintenance job to run daily
        /// </summary>
        public void RegisterMaintenanceJobs()
        {
            try
            {
                // Register the cleanup expired jobs task to run daily at 2:15 AM
                RecurringJob.AddOrUpdate("storage-cleanup-expired", 
                    () => CleanupExpiredJobsAsync(), 
                    "15 2 * * *");
                
                // Register the cleanup successful jobs task to run daily at 3:00 AM
                RecurringJob.AddOrUpdate("storage-cleanup-successful",
                    () => CleanupSuccessfulJobsAsync(30), 
                    "0 3 * * *");
                
                // Register the optimize tables job to run weekly on Sunday at 4:00 AM
                RecurringJob.AddOrUpdate("storage-optimize-tables",
                    () => OptimizeJobStorageTablesAsync(),
                    "0 4 * * 0");
                
                _logger.LogInformation("Successfully registered Hangfire storage maintenance jobs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register Hangfire storage maintenance jobs");
                throw;
            }
        }

        /// <summary>
        /// Cleans up expired jobs from the database
        /// </summary>
        [JobRetryBackoff(maxRetryAttempts: 3, initialDelaySeconds: 30)]
        public async Task CleanupExpiredJobsAsync()
        {
            try
            {
                _logger.LogInformation("Starting cleanup of expired jobs");

                var deletedCount = 0;
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                
                // Find and delete expired jobs in batches
                var expiredJobs = FindExpiredJobs(monitoringApi);
                foreach (var jobId in expiredJobs)
                {
                    try
                    {
                        BackgroundJob.Delete(jobId);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error deleting expired job {JobId}", jobId);
                    }
                }

                _logger.LogInformation("Completed expired job cleanup. Deleted {Count} expired jobs", deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during expired job cleanup");
                throw;
            }
        }

        /// <summary>
        /// Cleans up successful jobs older than the specified number of days
        /// </summary>
        [JobRetryBackoff(maxRetryAttempts: 3, initialDelaySeconds: 30)]
        public async Task CleanupSuccessfulJobsAsync(int olderThanDays = 30)
        {
            try
            {
                _logger.LogInformation("Starting cleanup of successful jobs older than {Days} days", olderThanDays);
                
                var cutoffDate = DateTime.UtcNow.AddDays(-olderThanDays);
                var deletedCount = 0;
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                
                // Find and delete old successful jobs
                var succeededJobs = monitoringApi.SucceededJobs(0, int.MaxValue);
                var oldJobs = succeededJobs
                    .Where(j => j.Value.SucceededAt < cutoffDate)
                    .Select(j => j.Key)
                    .ToList();
                
                foreach (var jobId in oldJobs)
                {
                    try
                    {
                        BackgroundJob.Delete(jobId);
                        deletedCount++;

                        // Log progress in batches to avoid excessive logging
                        if (deletedCount % 100 == 0)
                        {
                            _logger.LogInformation("Deleted {Count} successful jobs so far", deletedCount);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error deleting successful job {JobId}", jobId);
                    }
                }

                _logger.LogInformation("Completed successful job cleanup. Deleted {Count} successful jobs", deletedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during successful job cleanup");
                throw;
            }
        }

        /// <summary>
        /// Optimizes the Hangfire job storage tables
        /// </summary>
        [JobRetryBackoff(maxRetryAttempts: 2, initialDelaySeconds: 60)]
        public async Task OptimizeJobStorageTablesAsync()
        {
            try
            {
                _logger.LogInformation("Starting Hangfire job storage table optimization");

                await using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Get list of tables in the Hangfire schema
                    var tables = await GetHangfireTablesAsync(connection);

                    foreach (var table in tables)
                    {
                        try
                        {
                            // Run index reorganize/rebuild on each table
                            await OptimizeTableIndexesAsync(connection, table);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error optimizing table {TableName}", table);
                        }
                    }
                }

                _logger.LogInformation("Completed Hangfire job storage table optimization");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during job storage table optimization");
                throw;
            }
        }

        /// <summary>
        /// Find expired jobs that should be cleaned up
        /// </summary>
        private List<string> FindExpiredJobs(IMonitoringApi monitoringApi)
        {
            var expiredJobs = new List<string>();

            // Find scheduled jobs that are past their scheduled time by more than 7 days
            var staleCutoff = DateTime.UtcNow.AddDays(-7);
            var scheduledJobs = monitoringApi.ScheduledJobs(0, int.MaxValue);
            foreach (var job in scheduledJobs)
            {
                if (job.Value.ScheduledAt < staleCutoff)
                {
                    expiredJobs.Add(job.Key);
                }
            }

            // Find processing jobs that have been processing for more than 24 hours (likely stuck)
            var processingCutoff = DateTime.UtcNow.AddHours(-24);
            var processingJobs = monitoringApi.ProcessingJobs(0, int.MaxValue);
            foreach (var job in processingJobs)
            {
                if (job.Value.StartedAt < processingCutoff)
                {
                    expiredJobs.Add(job.Key);
                }
            }

            return expiredJobs;
        }

        /// <summary>
        /// Get all tables in the Hangfire schema
        /// </summary>
        private async Task<List<string>> GetHangfireTablesAsync(SqlConnection connection)
        {
            var tables = new List<string>();
            string schemaName = "HangFire";  // Default schema name

            // Get the schema name from configuration if available
            var storageOptions = _configuration.GetSection("Hangfire:SqlServerStorageOptions");
            if (storageOptions.Exists())
            {
                schemaName = storageOptions["SchemaName"] ?? schemaName;
            }

            var command = new SqlCommand(
                $"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES " +
                $"WHERE TABLE_SCHEMA = @SchemaName AND TABLE_TYPE = 'BASE TABLE'", 
                connection);
            command.Parameters.AddWithValue("@SchemaName", schemaName);

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    tables.Add($"{schemaName}.{reader.GetString(0)}");
                }
            }

            return tables;
        }

        /// <summary>
        /// Optimize the indexes on a specific table
        /// </summary>
        private async Task OptimizeTableIndexesAsync(SqlConnection connection, string tableName)
        {
            _logger.LogInformation("Optimizing indexes on table {TableName}", tableName);

            // Get fragmentation info for all indexes on the table
            var fragmentationCommand = new SqlCommand(
                "SELECT idx.name AS IndexName, " +
                "ips.avg_fragmentation_in_percent AS Fragmentation " +
                "FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID(@TableName), NULL, NULL, 'LIMITED') ips " +
                "JOIN sys.indexes idx ON ips.object_id = idx.object_id AND ips.index_id = idx.index_id " +
                "WHERE idx.name IS NOT NULL", connection);
            fragmentationCommand.Parameters.AddWithValue("@TableName", tableName);

            var indexes = new Dictionary<string, double>();
            using (var reader = await fragmentationCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    indexes.Add(reader.GetString(0), reader.GetDouble(1));
                }
            }

            // Optimize each index based on fragmentation level
            foreach (var index in indexes)
            {
                try
                {
                    string operation = index.Value > 30 ? "REBUILD" : "REORGANIZE";
                    var optimizeCommand = new SqlCommand(
                        $"ALTER INDEX [{index.Key}] ON {tableName} {operation}", connection);
                    
                    await optimizeCommand.ExecuteNonQueryAsync();
                    _logger.LogInformation("Successfully {Operation} index {IndexName} on {TableName} with {Fragmentation}% fragmentation",
                        operation, index.Key, tableName, index.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to optimize index {IndexName} on {TableName}", index.Key, tableName);
                }
            }
        }
    }
}