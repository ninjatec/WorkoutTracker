using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Hangfire;
using Hangfire.Server;
using Hangfire.Redis.StackExchange;
using Hangfire.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using WorkoutTrackerWeb.Services.Redis;

namespace WorkoutTrackerWeb.Services.Hangfire
{
    /// <summary>
    /// Centralized configuration for Hangfire server settings
    /// Supports role-based processing configuration for dedicated worker pods
    /// </summary>
    public class HangfireServerConfiguration
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<HangfireServerConfiguration> _logger;

        // Server configuration properties
        public bool IsProcessingEnabled { get; private set; }
        public int WorkerCount { get; private set; }
        public string[] Queues { get; private set; }

        /// <summary>
        /// Creates a new instance of HangfireServerConfiguration
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <param name="logger">Logger instance</param>
        public HangfireServerConfiguration(
            IConfiguration configuration,
            ILogger<HangfireServerConfiguration> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize with default configuration
            InitializeConfiguration();
        }

        /// <summary>
        /// Configures Hangfire global configuration based on settings
        /// </summary>
        /// <param name="config">Hangfire global configuration</param>
        public void ConfigureHangfire(IGlobalConfiguration config)
        {
            var redisConfig = _configuration.GetSection("Redis").Get<RedisConfiguration>();
            
            if (redisConfig?.Enabled == true && !string.IsNullOrEmpty(redisConfig.ConnectionString))
            {
                _logger.LogInformation("Configuring Hangfire with Redis storage");
                
                // Configure Redis as the storage provider
                config.UseRedisStorage(redisConfig.ConnectionString, new RedisStorageOptions
                {
                    Prefix = "hangfire:",
                    SucceededListSize = 1000,
                    DeletedListSize = 1000,
                    InvisibilityTimeout = TimeSpan.FromMinutes(30)
                });
            }
            else
            {
                _logger.LogInformation("Configuring Hangfire with SQL Server storage");
                
                // Use SQL Server as the storage provider
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                config.UseSqlServerStorage(connectionString, new SqlServerStorageOptions
                {
                    SchemaName = "HangFire",
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = false // Keep global locks for distributed environment
                });
            }

            // Configure job activation timeout
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                  .UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings()
                  .UseDefaultActivator();
        }

        /// <summary>
        /// Configures Hangfire server options
        /// </summary>
        /// <param name="options">Server options to configure</param>
        public void ConfigureServerOptions(BackgroundJobServerOptions options)
        {
            options.WorkerCount = WorkerCount;
            options.Queues = Queues;
            options.ServerTimeout = TimeSpan.FromMinutes(5);
            options.ShutdownTimeout = TimeSpan.FromMinutes(2);
            options.ServerName = $"{Environment.MachineName}:{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        private void InitializeConfiguration()
        {
            var section = _configuration.GetSection("Hangfire");
            
            // First check for environment variable overrides directly
            var envProcessingEnabled = Environment.GetEnvironmentVariable("HANGFIRE_PROCESSING_ENABLED");
            var envWorkerCount = Environment.GetEnvironmentVariable("HANGFIRE_WORKER_COUNT");
            
            // Determine if this instance should process jobs - prioritize environment variable
            if (!string.IsNullOrEmpty(envProcessingEnabled))
            {
                IsProcessingEnabled = bool.TryParse(envProcessingEnabled, out var result) && result;
                _logger.LogInformation("Using environment variable HANGFIRE_PROCESSING_ENABLED: {Value}", IsProcessingEnabled);
            }
            else
            {
                IsProcessingEnabled = section.GetValue<bool>("Processing:Enabled", true);
                _logger.LogInformation("Using configuration value Hangfire:Processing:Enabled: {Value}", IsProcessingEnabled);
            }
            
            // Configure worker count - prioritize environment variable
            if (!string.IsNullOrEmpty(envWorkerCount) && int.TryParse(envWorkerCount, out var workerCount))
            {
                WorkerCount = workerCount;
                _logger.LogInformation("Using environment variable HANGFIRE_WORKER_COUNT: {Value}", WorkerCount);
            }
            else
            {
                WorkerCount = section.GetValue<int>("Processing:WorkerCount", Environment.ProcessorCount * 2);
                _logger.LogInformation("Using configuration value Hangfire:Processing:WorkerCount: {Value}", WorkerCount);
            }
            
            // Configure job queues and their priorities
            var configuredQueues = section.GetSection("Processing:Queues").Get<string[]>();
            Queues = configuredQueues ?? new[] { "critical", "default" };

            _logger.LogInformation(
                "Initialized Hangfire configuration: Processing={IsEnabled}, Workers={Workers}, Queues={Queues}",
                IsProcessingEnabled, WorkerCount, string.Join(",", Queues));
        }
    }
}