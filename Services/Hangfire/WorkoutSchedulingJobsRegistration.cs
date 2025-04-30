using System;
using System.Threading.Tasks;
using Hangfire;
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

        public WorkoutSchedulingJobsRegistration(
            ILogger<WorkoutSchedulingJobsRegistration> logger,
            IBackgroundJobClient backgroundJobClient,
            IRecurringJobManager recurringJobManager)
        {
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
        }

        /// <summary>
        /// Registers all workout scheduling related background jobs with Hangfire
        /// </summary>
        public void RegisterWorkoutSchedulingJobs()
        {
            _logger.LogInformation("Registering workout scheduling jobs");
            
            try
            {
                // Register a recurring job to process scheduled workouts every hour
                _recurringJobManager.AddOrUpdate(
                    "process-scheduled-workouts-hourly",
                    () => GetRequiredService().ProcessScheduledWorkoutsAsync(),
                    Cron.Hourly, // Run every hour
                    TimeZoneInfo.Utc);
                
                _logger.LogInformation("Successfully registered hourly workout processing job");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering workout scheduling jobs");
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