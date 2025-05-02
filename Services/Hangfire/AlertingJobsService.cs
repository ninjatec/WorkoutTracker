using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services.Alerting;
using Microsoft.Extensions.DependencyInjection;
using Hangfire;

namespace WorkoutTrackerWeb.Services.Hangfire
{
    /// <summary>
    /// Service that implements background job methods for the alerting system
    /// </summary>
    public class AlertingJobsService
    {
        private readonly ILogger<AlertingJobsService> _logger;
        private readonly IAlertingService _alertingService;
        private static IServiceProvider _staticServiceProvider;

        /// <summary>
        /// Creates a new instance of the AlertingJobsService
        /// </summary>
        public AlertingJobsService(
            IAlertingService alertingService,
            ILogger<AlertingJobsService> logger)
        {
            _alertingService = alertingService ?? throw new ArgumentNullException(nameof(alertingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Parameterless constructor for Hangfire to create instances
        /// </summary>
        public AlertingJobsService()
        {
            // Get services from JobActivator
            var serviceProvider = JobActivator.Current.BeginScope(null)?.Resolve(typeof(IServiceProvider)) as IServiceProvider;
            
            if (serviceProvider != null)
            {
                _alertingService = serviceProvider.GetService<IAlertingService>();
                _logger = serviceProvider.GetService<ILogger<AlertingJobsService>>();
            }
            else if (_staticServiceProvider != null)
            {
                // Fallback to static service provider
                _alertingService = _staticServiceProvider.GetService<IAlertingService>();
                _logger = _staticServiceProvider.GetService<ILogger<AlertingJobsService>>();
            }
            
            // Fallback logger if nothing else works
            _logger ??= new LoggerFactory().CreateLogger<AlertingJobsService>();
        }

        /// <summary>
        /// Static method to set the service provider for parameterless constructor dependency resolution
        /// </summary>
        public static void SetServiceProvider(IServiceProvider serviceProvider)
        {
            _staticServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Runs the daily system health check job
        /// </summary>
        public async Task RunDailySystemHealthCheck()
        {
            _logger.LogInformation("Running daily system health check");
            try
            {
                // Use the existing CheckAllThresholdsAsync method instead of the missing CheckSystemHealthAsync
                bool result = await _alertingService.CheckAllThresholdsAsync();
                _logger.LogInformation("Daily system health check completed successfully: {Result}", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during daily system health check");
                throw; // Rethrow to let Hangfire know the job failed
            }
        }

        /// <summary>
        /// Collects hourly system metrics
        /// </summary>
        public async Task CollectHourlyMetrics()
        {
            _logger.LogInformation("Collecting hourly system metrics");
            try
            {
                // Rather than calling a non-existent method, let's evaluate key metrics directly
                await _alertingService.EvaluateMetricAsync("CPU Usage", "System", GetCpuUsage());
                await _alertingService.EvaluateMetricAsync("Memory Usage", "System", GetMemoryUsage());
                await _alertingService.EvaluateMetricAsync("Disk Space", "System", GetDiskSpace());
                await _alertingService.EvaluateMetricAsync("Active Users", "Application", GetActiveUsers());
                
                _logger.LogInformation("Hourly metrics collection completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during hourly metrics collection");
                throw; // Rethrow to let Hangfire know the job failed
            }
        }

        /// <summary>
        /// Generates the weekly system report
        /// </summary>
        public async Task GenerateWeeklyReport()
        {
            _logger.LogInformation("Generating weekly system report");
            try
            {
                // Get alert history for the past week
                var oneWeekAgo = DateTime.UtcNow.AddDays(-7);
                var alertHistory = await _alertingService.GetAlertHistoryAsync(oneWeekAgo, null, 1000);
                
                // Get active alerts
                var activeAlerts = await _alertingService.GetActiveAlertsAsync();
                
                _logger.LogInformation("Weekly report summary: {ActiveAlertCount} active alerts, {HistoryCount} alerts in the past week", 
                    activeAlerts.Count(), alertHistory.Count());
                
                // In a real implementation, we might generate a PDF report or send an email summary here
                
                _logger.LogInformation("Weekly report generation completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during weekly report generation");
                throw; // Rethrow to let Hangfire know the job failed
            }
        }

        /// <summary>
        /// Monitors the system for critical issues
        /// </summary>
        public async Task MonitorCriticalSystemIssues()
        {
            _logger.LogInformation("Monitoring system for critical issues");
            try
            {
                // Check for critical issues by evaluating high-priority metrics
                await _alertingService.EvaluateMetricAsync("Service Availability", "Critical", 100);
                await _alertingService.EvaluateMetricAsync("Database Connections", "Critical", GetDatabaseConnections());
                await _alertingService.EvaluateMetricAsync("Error Rate", "Critical", GetErrorRate());
                
                // Also run a full threshold check
                await _alertingService.CheckAllThresholdsAsync();
                
                _logger.LogInformation("Critical issues monitoring completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during critical issues monitoring");
                throw; // Rethrow to let Hangfire know the job failed
            }
        }
        
        #region Helper Methods
        
        private double GetCpuUsage()
        {
            // This would be replaced with actual CPU monitoring in production
            return new Random().Next(10, 90);
        }
        
        private double GetMemoryUsage()
        {
            // This would be replaced with actual memory monitoring in production
            return new Random().Next(20, 85);
        }
        
        private double GetDiskSpace()
        {
            // This would be replaced with actual disk space monitoring in production
            // Returns MB of free space
            return new Random().Next(500, 10000);
        }
        
        private double GetActiveUsers()
        {
            // This would get the actual active user count from a metrics system
            return new Random().Next(1, 100);
        }
        
        private double GetDatabaseConnections()
        {
            // This would get the actual DB connection count
            return new Random().Next(1, 50);
        }
        
        private double GetErrorRate()
        {
            // This would calculate the actual error rate
            return new Random().Next(0, 5);
        }
        
        #endregion
    }
}