using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Services
{
    /// <summary>
    /// Service that centralizes database connection string building with consistent pool sizing
    /// </summary>
    public class ConnectionStringBuilderService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConnectionStringBuilderService> _logger;
        
        public ConnectionStringBuilderService(IConfiguration configuration, ILogger<ConnectionStringBuilderService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Builds a connection string with optimized connection pooling settings
        /// </summary>
        /// <param name="connectionName">The connection string name in configuration</param>
        /// <param name="isReadOnly">Whether this connection will be used for read-only operations</param>
        /// <returns>Enhanced connection string with proper pooling settings</returns>
        public string BuildConnectionString(string connectionName, bool isReadOnly = false)
        {
            var connectionString = _configuration.GetConnectionString(connectionName) ?? 
                throw new InvalidOperationException($"Connection string '{connectionName}' not found.");
            
            var poolingConfig = _configuration.GetSection("DatabaseConnectionPooling");
            bool readWriteSeparated = poolingConfig.GetValue<bool>("ReadWriteConnectionSeparation", false);
            
            // Build SQL connection with pooling settings from config
            var sqlConnectionBuilder = new SqlConnectionStringBuilder(connectionString);
            if (poolingConfig.GetValue<bool>("EnableConnectionPooling", true))
            {
                sqlConnectionBuilder.Pooling = true;
                
                // Configure pool size based on read/write role if separation is enabled
                if (readWriteSeparated && isReadOnly)
                {
                    sqlConnectionBuilder.MaxPoolSize = poolingConfig.GetValue<int>("ReadConnectionMaxPoolSize", 40);
                    sqlConnectionBuilder.MinPoolSize = poolingConfig.GetValue<int>("ReadConnectionMinPoolSize", 5);
                    _logger.LogDebug("Using read-specific pool settings: Min={MinPool}, Max={MaxPool}", 
                        sqlConnectionBuilder.MinPoolSize, sqlConnectionBuilder.MaxPoolSize);
                }
                else
                {
                    sqlConnectionBuilder.MaxPoolSize = poolingConfig.GetValue<int>("MaxPoolSize", 60);
                    sqlConnectionBuilder.MinPoolSize = poolingConfig.GetValue<int>("MinPoolSize", 10);
                }
                
                sqlConnectionBuilder.ConnectTimeout = poolingConfig.GetValue<int>("ConnectTimeout", 30);
                sqlConnectionBuilder.LoadBalanceTimeout = poolingConfig.GetValue<int>("LoadBalanceTimeout", 30);
                sqlConnectionBuilder.ConnectRetryCount = poolingConfig.GetValue<int>("RetryCount", 3);
                sqlConnectionBuilder.ConnectRetryInterval = poolingConfig.GetValue<int>("RetryInterval", 10);
                
                if (isReadOnly)
                {
                    sqlConnectionBuilder.ApplicationIntent = ApplicationIntent.ReadOnly;
                }
            }
            else
            {
                sqlConnectionBuilder.Pooling = false;
                _logger.LogWarning("Connection pooling is disabled. This is not recommended for production environments.");
            }

            // Apply additional connection settings
            string enhancedConnectionString = sqlConnectionBuilder.ConnectionString;
            int connectionLifetime = poolingConfig.GetValue<int>("ConnectionLifetime", 600);
            if (connectionLifetime > 0)
            {
                enhancedConnectionString += $";Connection Lifetime={connectionLifetime}";
            }
            
            bool connectionResetEnabled = poolingConfig.GetValue<bool>("ConnectionResetEnabled", true);
            if (connectionResetEnabled && OperatingSystem.IsWindows())
            {
                enhancedConnectionString += ";Connection Reset=true";
            }
            
            _logger.LogTrace("Built connection string with pooling settings: MaxPool={MaxPool}, MinPool={MinPool}, Lifetime={Lifetime}", 
                sqlConnectionBuilder.MaxPoolSize, sqlConnectionBuilder.MinPoolSize, connectionLifetime);
            
            return enhancedConnectionString;
        }
        
        /// <summary>
        /// Gets the pool metrics information for monitoring and diagnostics
        /// </summary>
        public ConnectionPoolMetrics GetConnectionPoolMetrics()
        {
            var poolingConfig = _configuration.GetSection("DatabaseConnectionPooling");
            
            return new ConnectionPoolMetrics
            {
                MaxPoolSize = poolingConfig.GetValue<int>("MaxPoolSize", 60),
                MinPoolSize = poolingConfig.GetValue<int>("MinPoolSize", 10),
                ReadConnectionMaxPoolSize = poolingConfig.GetValue<int>("ReadConnectionMaxPoolSize", 40),
                ReadConnectionMinPoolSize = poolingConfig.GetValue<int>("ReadConnectionMinPoolSize", 5),
                ConnectionLifetime = poolingConfig.GetValue<int>("ConnectionLifetime", 600),
                ReadWriteSeparationEnabled = poolingConfig.GetValue<bool>("ReadWriteConnectionSeparation", false),
                PoolingEnabled = poolingConfig.GetValue<bool>("EnableConnectionPooling", true),
                ConnectTimeout = poolingConfig.GetValue<int>("ConnectTimeout", 30),
                RetryCount = poolingConfig.GetValue<int>("RetryCount", 3),
                RetryInterval = poolingConfig.GetValue<int>("RetryInterval", 10)
            };
        }
    }

    /// <summary>
    /// Contains information about connection pool configuration and metrics
    /// </summary>
    public class ConnectionPoolMetrics
    {
        public int MaxPoolSize { get; set; }
        public int MinPoolSize { get; set; }
        public int ReadConnectionMaxPoolSize { get; set; }
        public int ReadConnectionMinPoolSize { get; set; }
        public int ConnectionLifetime { get; set; }
        public bool ReadWriteSeparationEnabled { get; set; }
        public bool PoolingEnabled { get; set; }
        public int ConnectTimeout { get; set; }
        public int RetryCount { get; set; }
        public int RetryInterval { get; set; }
    }
}