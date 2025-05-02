using System;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services.Scheduling;

namespace WorkoutTrackerWeb.Services.Hangfire
{
    /// <summary>
    /// Service responsible for registering and scheduling workout processing jobs in Hangfire
    /// </summary>
    public class WorkoutSchedulingJobsRegistration
    {
        private readonly ILogger<WorkoutSchedulingJobsRegistration> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly HangfireServerConfiguration _serverConfig;

        public WorkoutSchedulingJobsRegistration(
            ILogger<WorkoutSchedulingJobsRegistration> logger,
            IBackgroundJobClient backgroundJobClient,
            IRecurringJobManager recurringJobManager,
            HangfireServerConfiguration serverConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
            _recurringJobManager = recurringJobManager ?? throw new ArgumentNullException(nameof(recurringJobManager));
            _serverConfig = serverConfig ?? throw new ArgumentNullException(nameof(serverConfig));
        }

        /// <summary>
        /// Registers all workout scheduling related background jobs with Hangfire
        /// </summary>
        public void RegisterWorkoutSchedulingJobs()
        {
            _logger.LogInformation("Registering workout scheduling jobs. Processing enabled: {IsProcessingEnabled}", _serverConfig.IsProcessingEnabled);
            
            try
            {
                // Register a recurring job to process scheduled workouts hourly
                _recurringJobManager.AddOrUpdate(
                    "process-scheduled-workouts-hourly",
                    () => GetRequiredService().ProcessScheduledWorkoutsAsync(),
                    "0 * * * *", // Run every hour at minute 0
                    TimeZoneInfo.Local);
                
                // Add a more frequent job to handle time-sensitive workouts
                _recurringJobManager.AddOrUpdate(
                    "process-urgent-workouts",
                    () => GetRequiredService().ProcessScheduledWorkoutsAsync(),
                    "*/15 * * * *", // Run every 15 minutes
                    TimeZoneInfo.Local);
                
                // Add a daily job to clean up expired scheduled workouts
                _recurringJobManager.AddOrUpdate(
                    "cleanup-expired-workouts",
                    () => GetRequiredService().CleanupExpiredWorkoutsAsync(),
                    Cron.Daily,
                    TimeZoneInfo.Local);
                
                // Add a daily job to process missed workouts
                _recurringJobManager.AddOrUpdate(
                    "process-missed-workouts",
                    () => GetRequiredService().ProcessMissedWorkoutsAsync(),
                    "0 2 * * *", // Run at 2 AM daily
                    TimeZoneInfo.Local);
                
                _logger.LogInformation("Successfully registered all workout processing jobs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering workout scheduling jobs");
                throw; // Rethrow to alert the application that job registration failed
            }
        }
        
        /// <summary>
        /// Helper method for RecurringJob registration to avoid Job.FromExpression issues
        /// </summary>
        private static ScheduledWorkoutProcessorService GetRequiredService()
        {
            // This method doesn't actually return the service; Hangfire will resolve it at runtime
            return null;
        }
    }
}