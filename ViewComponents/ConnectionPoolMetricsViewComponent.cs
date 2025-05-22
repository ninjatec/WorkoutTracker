using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;

namespace WorkoutTrackerWeb.ViewComponents
{
    public class ConnectionPoolMetricsViewModel
    {
        public int MaxPoolSize { get; set; }
        public int MinPoolSize { get; set; }
        public int PooledCount { get; set; }
        public int ActiveConnections { get; set; }
        public int FreeConnections { get; set; }
        public int PendingRequests { get; set; }
        public TimeSpan ConnectionTimeout { get; set; }
        public int ConnectionLifetime { get; set; }
        public bool PoolingEnabled { get; set; }
        public string ConnectionString { get; set; }
        public bool ReadWriteConnectionSeparation { get; set; }
        public int ReadConnectionMaxPoolSize { get; set; }
        public int ReadConnectionMinPoolSize { get; set; }
        public string PoolHealthStatus { get; set; }
    }

    public class ConnectionPoolMetricsViewComponent : ViewComponent
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConnectionPoolMetricsViewComponent> _logger;

        public ConnectionPoolMetricsViewComponent(
            IConfiguration configuration,
            ILogger<ConnectionPoolMetricsViewComponent> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public IViewComponentResult Invoke()
        {
            var viewModel = new ConnectionPoolMetricsViewModel();
            
            try
            {
                // Get database connection pooling configuration
                var poolingConfig = _configuration.GetSection("DatabaseConnectionPooling");
                
                // Get basic pool configuration
                viewModel.MaxPoolSize = poolingConfig.GetValue<int>("MaxPoolSize", 100);
                viewModel.MinPoolSize = poolingConfig.GetValue<int>("MinPoolSize", 5);
                viewModel.ConnectionLifetime = poolingConfig.GetValue<int>("ConnectionLifetime", 600);
                viewModel.ConnectionTimeout = TimeSpan.FromSeconds(poolingConfig.GetValue<int>("ConnectTimeout", 30));
                viewModel.PoolingEnabled = poolingConfig.GetValue<bool>("EnableConnectionPooling", true);
                viewModel.ReadWriteConnectionSeparation = poolingConfig.GetValue<bool>("ReadWriteConnectionSeparation", false);
                viewModel.ReadConnectionMaxPoolSize = poolingConfig.GetValue<int>("ReadConnectionMaxPoolSize", 40);
                viewModel.ReadConnectionMinPoolSize = poolingConfig.GetValue<int>("ReadConnectionMinPoolSize", 5);
                
                // Get the sanitized connection string (no credentials)
                string connString = _configuration.GetConnectionString("DefaultConnection") ?? "";
                var builder = new SqlConnectionStringBuilder(connString);
                builder.Password = "[REDACTED]";
                builder.UserID = "[REDACTED]";
                viewModel.ConnectionString = builder.ToString();
                
                // Get current connection pool statistics using SqlConnection.ClearPool method's type
                // to access the internal connection pool statistics via reflection
                var stats = SqlClientHelper.GetPoolStats();
                
                viewModel.PooledCount = stats.PooledCount;
                viewModel.ActiveConnections = stats.ActiveConnections;
                viewModel.PendingRequests = stats.PendingRequests;
                viewModel.FreeConnections = stats.PooledCount - stats.ActiveConnections;
                
                // Calculate pool health status
                double utilizationRate = viewModel.MaxPoolSize > 0 
                    ? (double)viewModel.ActiveConnections / viewModel.MaxPoolSize * 100 
                    : 0;
                
                if (utilizationRate > 90)
                {
                    viewModel.PoolHealthStatus = "Critical";
                }
                else if (utilizationRate > 75)
                {
                    viewModel.PoolHealthStatus = "Warning";
                }
                else if (viewModel.PendingRequests > 0)
                {
                    viewModel.PoolHealthStatus = "Warning";
                }
                else
                {
                    viewModel.PoolHealthStatus = "Healthy";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve connection pool metrics");
                viewModel.PoolHealthStatus = "Unknown";
            }
            
            return View(viewModel);
        }
    }
    
    /// <summary>
    /// Helper class to retrieve SQL connection pool statistics
    /// </summary>
    internal static class SqlClientHelper
    {
        public class PoolStatistics
        {
            public int PooledCount { get; set; }
            public int ActiveConnections { get; set; }
            public int PendingRequests { get; set; }
        }
        
        public static PoolStatistics GetPoolStats()
        {
            var stats = new PoolStatistics
            {
                PooledCount = 0,
                ActiveConnections = 0,
                PendingRequests = 0
            };
            
            try
            {
                // Get SqlConnection statistics using performance counters if available
                // This approach uses reflection since the SqlClient doesn't expose pool stats directly
                var sqlClientAssembly = typeof(SqlConnection).Assembly;
                var perfCounterType = sqlClientAssembly.GetType("Microsoft.Data.SqlClient.SqlPerformanceCounters");
                
                if (perfCounterType != null)
                {
                    // Try to get the instance using reflection
                    var instanceProperty = perfCounterType.GetProperty("SingletonInstance", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    
                    if (instanceProperty != null)
                    {
                        var instance = instanceProperty.GetValue(null);
                        if (instance != null)
                        {
                            // Get connection pool statistics
                            var pooledCountProp = perfCounterType.GetProperty("HardConnectsPerSecond");
                            var activeProp = perfCounterType.GetProperty("NumberOfActiveConnectionPoolGroups");
                            var pendingProp = perfCounterType.GetProperty("NumberOfInactiveConnectionPoolGroups");
                            
                            if (pooledCountProp != null)
                                stats.PooledCount = Convert.ToInt32(pooledCountProp.GetValue(instance));
                            
                            if (activeProp != null)
                                stats.ActiveConnections = Convert.ToInt32(activeProp.GetValue(instance));
                            
                            if (pendingProp != null)
                                stats.PendingRequests = Convert.ToInt32(pendingProp.GetValue(instance));
                        }
                    }
                }
            }
            catch
            {
                // Fallback: estimate based on a test connection
                using (var conn = new SqlConnection())
                {
                    try
                    {
                        conn.Open();
                        stats.PooledCount = 1;
                        stats.ActiveConnections = 1;
                    }
                    catch
                    {
                        // Ignore
                    }
                }
            }
            
            return stats;
        }
    }
}