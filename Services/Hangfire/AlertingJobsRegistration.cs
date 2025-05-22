using System;
using System.Linq;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Services.Hangfire
{
    /// <summary>
    /// Handles the registration of recurring jobs for the alerting system
    /// Supports the new role-based Hangfire server configuration
    /// </summary>
    public class AlertingJobsRegistration
    {
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly ILogger<AlertingJobsRegistration> _logger;
        private readonly AlertingJobsService _alertingJobsService;
        private readonly HangfireServerConfiguration _serverConfig;

        /// <summary>
        /// Creates a new instance of the AlertingJobsRegistration class
        /// </summary>
        public AlertingJobsRegistration(
            IRecurringJobManager recurringJobManager,
            AlertingJobsService alertingJobsService,
            HangfireServerConfiguration serverConfig,
            ILogger<AlertingJobsRegistration> logger)
        {
            _recurringJobManager = recurringJobManager ?? throw new ArgumentNullException(nameof(recurringJobManager));
            _alertingJobsService = alertingJobsService ?? throw new ArgumentNullException(nameof(alertingJobsService));
            _serverConfig = serverConfig ?? throw new ArgumentNullException(nameof(serverConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Registers all alerting system jobs in Hangfire
        /// </summary>
        public void RegisterAlertingJobs()
        {
            _logger.LogInformation("Registering alerting jobs with server role: {ProcessingRole}", 
                _serverConfig.IsProcessingEnabled ? "Worker" : "Web");

            // Always register jobs even if processing is disabled in this instance
            // Other instances might still process them

            // Register daily system health check job
            _recurringJobManager.AddOrUpdate(
                "daily-system-health-check",
                () => _alertingJobsService.RunDailySystemHealthCheck(),
                Cron.Daily(2), // Run at 2 AM daily
                TimeZoneInfo.Local,
                "default"); // Use default queue

            // Register hourly metrics collection job
            _recurringJobManager.AddOrUpdate(
                "hourly-metrics-collection",
                () => _alertingJobsService.CollectHourlyMetrics(),
                Cron.Hourly(),
                TimeZoneInfo.Local, 
                "default");

            // Register weekly report generation job
            _recurringJobManager.AddOrUpdate(
                "weekly-report-generation",
                () => _alertingJobsService.GenerateWeeklyReport(),
                Cron.Weekly(DayOfWeek.Monday, 7), // Monday mornings at 7 AM
                TimeZoneInfo.Local,
                "default");

            // Register a critical-priority job that checks for severe system issues
            _recurringJobManager.AddOrUpdate(
                "critical-system-monitoring",
                () => _alertingJobsService.MonitorCriticalSystemIssues(),
                "*/15 * * * *", // Every 15 minutes
                TimeZoneInfo.Local,
                "critical"); // Critical queue for higher priority

            _logger.LogInformation("Successfully registered alerting jobs");
        }
    }
}