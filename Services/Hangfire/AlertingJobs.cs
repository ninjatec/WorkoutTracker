using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services.Alerting;

namespace WorkoutTrackerWeb.Services.Hangfire
{
    public class AlertingJobsRegistration
    {
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly ILogger<AlertingJobsRegistration> _logger;

        public AlertingJobsRegistration(
            IRecurringJobManager recurringJobManager,
            ILogger<AlertingJobsRegistration> logger)
        {
            _recurringJobManager = recurringJobManager;
            _logger = logger;
        }

        public void RegisterAlertingJobs()
        {
            _logger.LogInformation("Registering alerting jobs with Hangfire");
            
            // Check alert thresholds every 5 minutes
            _recurringJobManager.AddOrUpdate<AlertingJobsService>(
                "check-alert-thresholds",
                service => service.CheckAlertThresholdsAsync(),
                "*/5 * * * *"); // Cron expression for every 5 minutes
            
            // Run alert maintenance job daily at 2 AM
            _recurringJobManager.AddOrUpdate<AlertingJobsService>(
                "alert-maintenance",
                service => service.PerformAlertMaintenanceAsync(),
                "0 2 * * *"); // Cron expression for 2 AM daily
                
            _logger.LogInformation("Alert jobs registered successfully");
        }
    }

    public class AlertingJobsService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<AlertingJobsService> _logger;

        public AlertingJobsService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<AlertingJobsService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 600 })]
        public async Task CheckAlertThresholdsAsync()
        {
            try
            {
                _logger.LogInformation("Starting alert thresholds check");
                
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var alertingService = scope.ServiceProvider.GetRequiredService<IAlertingService>();
                    
                    // Check CPU usage
                    double cpuUsage = GetCurrentCpuUsage();
                    await alertingService.EvaluateMetricAsync("CPU Usage", "System", cpuUsage);
                    _logger.LogDebug("Evaluated CPU usage: {Usage}%", cpuUsage);
                    
                    // Check Memory usage
                    double memoryUsage = GetCurrentMemoryUsage();
                    await alertingService.EvaluateMetricAsync("Memory Usage", "System", memoryUsage);
                    _logger.LogDebug("Evaluated Memory usage: {Usage}%", memoryUsage);
                    
                    // Check Disk space
                    double diskSpace = GetCurrentDiskSpace();
                    await alertingService.EvaluateMetricAsync("Disk Space Available", "Storage", diskSpace);
                    _logger.LogDebug("Evaluated Disk space: {Space}MB", diskSpace);
                    
                    // Check Database connections
                    double dbConnections = GetDatabaseConnections();
                    await alertingService.EvaluateMetricAsync("Database Connections", "Database", dbConnections);
                    _logger.LogDebug("Evaluated Database connections: {Count}", dbConnections);
                    
                    // Check Active users
                    double activeUsers = GetActiveUserCount();
                    await alertingService.EvaluateMetricAsync("Active Users", "Application", activeUsers);
                    _logger.LogDebug("Evaluated Active users: {Count}", activeUsers);
                    
                    // Check Request rate
                    double requestRate = GetRequestRate();
                    await alertingService.EvaluateMetricAsync("Request Rate", "Application", requestRate);
                    _logger.LogDebug("Evaluated Request rate: {Rate}/sec", requestRate);
                    
                    // Check Error rate
                    double errorRate = GetErrorRate();
                    await alertingService.EvaluateMetricAsync("Error Rate", "Application", errorRate);
                    _logger.LogDebug("Evaluated Error rate: {Rate}%", errorRate);
                }
                
                _logger.LogInformation("Completed alert thresholds check");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during alert thresholds check");
                throw; // Let Hangfire handle the retry
            }
        }

        [AutomaticRetry(Attempts = 1)]
        public async Task PerformAlertMaintenanceAsync()
        {
            try
            {
                _logger.LogInformation("Starting alert maintenance job");
                
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWeb.Data.WorkoutTrackerWebContext>();
                    
                    // Archive old resolved alerts that are more than 30 days old
                    var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                    var oldResolvedAlerts = await dbContext.Alert
                        .Where(a => a.ResolvedAt.HasValue && a.ResolvedAt < thirtyDaysAgo)
                        .ToListAsync();
                    
                    if (oldResolvedAlerts.Any())
                    {
                        _logger.LogInformation("Archiving {Count} old resolved alerts", oldResolvedAlerts.Count);
                        dbContext.Alert.RemoveRange(oldResolvedAlerts);
                        await dbContext.SaveChangesAsync();
                    }
                    
                    // Clean up old notifications that are more than 60 days old
                    var sixtyDaysAgo = DateTime.UtcNow.AddDays(-60);
                    var oldNotifications = await dbContext.Notification
                        .Where(n => n.CreatedAt < sixtyDaysAgo)
                        .ToListAsync();
                    
                    if (oldNotifications.Any())
                    {
                        _logger.LogInformation("Removing {Count} old notifications", oldNotifications.Count);
                        dbContext.Notification.RemoveRange(oldNotifications);
                        await dbContext.SaveChangesAsync();
                    }
                    
                    // Clean up alert history older than 90 days
                    var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);
                    var oldHistory = await dbContext.AlertHistory
                        .Where(h => h.TriggeredAt < ninetyDaysAgo)
                        .ToListAsync();
                    
                    if (oldHistory.Any())
                    {
                        _logger.LogInformation("Removing {Count} old alert history records", oldHistory.Count);
                        dbContext.AlertHistory.RemoveRange(oldHistory);
                        await dbContext.SaveChangesAsync();
                    }
                }
                
                _logger.LogInformation("Completed alert maintenance job");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during alert maintenance job");
                throw;
            }
        }

        #region Metric Collectors
        // These methods would be replaced with actual implementations that collect real metrics
        // For now, we're using mock implementations with random data

        private double GetCurrentCpuUsage()
        {
            // In a real system, this would get actual CPU usage
            // For demo, return a random value between 5 and 95
            return new Random().Next(5, 95);
        }

        private double GetCurrentMemoryUsage()
        {
            // In a real system, this would get actual memory usage
            // For demo, return a random value between 10 and 90
            return new Random().Next(10, 90);
        }

        private double GetCurrentDiskSpace()
        {
            // In a real system, this would get actual available disk space in MB
            // For demo, return a random value between 500 and 10000
            return new Random().Next(500, 10000);
        }

        private double GetDatabaseConnections()
        {
            // In a real system, this would query the database for connection count
            // For demo, return a random value between 1 and 100
            return new Random().Next(1, 100);
        }

        private double GetActiveUserCount()
        {
            // In a real system, this would query active sessions or users
            // For demo, return a random value between 0 and 50
            return new Random().Next(0, 50);
        }

        private double GetRequestRate()
        {
            // In a real system, this would calculate requests per second
            // For demo, return a random value between 1 and 200
            return new Random().Next(1, 200);
        }

        private double GetErrorRate()
        {
            // In a real system, this would calculate error percentage
            // For demo, return a random value between 0 and 10
            return new Random().Next(0, 10);
        }
        #endregion
    }
}