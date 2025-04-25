using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Hangfire;
using Hangfire.Server;

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

        /// <summary>
        /// Indicates whether this server instance should process background jobs
        /// Used to disable processing on web app pods while enabling it on worker pods
        /// </summary>
        public bool IsProcessingEnabled { get; private set; }

        /// <summary>
        /// Number of worker threads to use for job processing
        /// For dedicated workers, this should be higher than web pods
        /// </summary>
        public int WorkerCount { get; private set; }

        /// <summary>
        /// Server name used to identify this instance in the Hangfire dashboard
        /// </summary>
        public string ServerName { get; private set; }

        /// <summary>
        /// Queues this server will process, in order of priority
        /// </summary>
        public string[] Queues { get; private set; }

        /// <summary>
        /// How often the server should report its status to the database
        /// </summary>
        public TimeSpan HeartbeatInterval { get; private set; }

        /// <summary>
        /// How often to check for server timeouts
        /// </summary>
        public TimeSpan ServerCheckInterval { get; private set; }

        /// <summary>
        /// How often to check for scheduled jobs
        /// </summary>
        public TimeSpan SchedulePollingInterval { get; private set; }

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
        /// Loads configuration from appsettings.json and environment variables
        /// </summary>
        private void InitializeConfiguration()
        {
            // First check environment variables (for Kubernetes pod configuration)
            var processingEnabledEnv = Environment.GetEnvironmentVariable("HANGFIRE_PROCESSING_ENABLED");
            var workerCountEnv = Environment.GetEnvironmentVariable("HANGFIRE_WORKER_COUNT");
            var serverNameEnv = Environment.GetEnvironmentVariable("POD_NAME") ?? Environment.GetEnvironmentVariable("HOSTNAME");

            // Then check configuration files (with environment variables taking precedence)
            IsProcessingEnabled = string.IsNullOrEmpty(processingEnabledEnv)
                ? _configuration.GetValue<bool>("Hangfire:ProcessingEnabled", true)
                : bool.TryParse(processingEnabledEnv, out var result) && result;

            WorkerCount = string.IsNullOrEmpty(workerCountEnv)
                ? _configuration.GetValue<int>("Hangfire:WorkerCount", Environment.ProcessorCount * 2)
                : int.TryParse(workerCountEnv, out var count) ? count : Environment.ProcessorCount * 2;

            ServerName = serverNameEnv ?? 
                _configuration.GetValue<string>("Hangfire:ServerName", $"server:{Environment.MachineName}");

            Queues = _configuration.GetSection("Hangfire:Queues").Get<string[]>() ?? 
                new[] { "critical", "default" };

            // Parse TimeSpan values from configuration
            if (TimeSpan.TryParse(_configuration["Hangfire:HeartbeatInterval"], out var heartbeatInterval))
                HeartbeatInterval = heartbeatInterval;
            else
                HeartbeatInterval = TimeSpan.FromSeconds(30);

            if (TimeSpan.TryParse(_configuration["Hangfire:ServerCheckInterval"], out var serverCheckInterval))
                ServerCheckInterval = serverCheckInterval;
            else
                ServerCheckInterval = TimeSpan.FromMinutes(1);

            if (TimeSpan.TryParse(_configuration["Hangfire:SchedulePollingInterval"], out var schedulePollingInterval))
                SchedulePollingInterval = schedulePollingInterval;
            else
                SchedulePollingInterval = TimeSpan.FromSeconds(15);

            _logger.LogInformation(
                "Hangfire server configuration initialized. Processing: {IsProcessingEnabled}, Workers: {WorkerCount}, Server: {ServerName}",
                IsProcessingEnabled, WorkerCount, ServerName);
        }

        /// <summary>
        /// Applies the current configuration to Hangfire server options
        /// </summary>
        /// <param name="options">The BackgroundJobServerOptions to configure</param>
        public void ConfigureServerOptions(BackgroundJobServerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            options.ServerName = ServerName;
            options.WorkerCount = WorkerCount;
            options.Queues = Queues;
            options.HeartbeatInterval = HeartbeatInterval;
            options.ServerCheckInterval = ServerCheckInterval;
            options.SchedulePollingInterval = SchedulePollingInterval;

            // Set specific worker count based on role
            if (!IsProcessingEnabled)
            {
                options.WorkerCount = 0; // Disable workers completely if processing is disabled
                _logger.LogInformation("Hangfire server configured as non-processing node. Worker count set to 0.");
            }
            else
            {
                _logger.LogInformation("Hangfire server configured as processing node with {WorkerCount} workers.", options.WorkerCount);
            }
        }
    }
}