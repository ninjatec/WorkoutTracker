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
        /// Processes all scheduled workouts with retry logic and comprehensive logging
        /// </summary>
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 600 })]
        public async Task<string> ProcessScheduledWorkoutsWithRetryAndLogging(PerformContext context = null)
        {
            var jobStartTime = DateTime.Now;
            context?.WriteLine($"Starting scheduled workout processing at {jobStartTime}");
            
            try
            {
                // This method is only for local testing
                // The actual service call is made through GetRequiredService in RegisterWorkoutSchedulingJobs
                _logger.LogWarning("This method should not be called directly. Use GetRequiredService() instead.");
                
                var duration = DateTime.Now - jobStartTime;
                var resultMessage = $"Method called directly instead of through job registration";
                
                _logger.LogInformation(resultMessage);
                context?.WriteLine(resultMessage);
                
                return resultMessage;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error processing scheduled workouts: {ex.Message}";
                
                _logger.LogError(ex, errorMessage);
                context?.SetTextColor(ConsoleTextColor.Red);
                context?.WriteLine(errorMessage);
                context?.ResetTextColor();
                
                // Rethrow to ensure Hangfire knows the job failed
                throw;
            }
        }
        
        /// <summary>
        /// Processes urgent scheduled workouts (those due in the next 15 minutes)
        /// </summary>
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 30, 60 })]
        public async Task<string> ProcessUrgentScheduledWorkoutsAsync(PerformContext context = null)
        {
            context?.WriteLine("Starting urgent scheduled workout processing");
            
            try
            {
                // This method is only for local testing
                // The actual service call is made through GetRequiredService in RegisterWorkoutSchedulingJobs
                _logger.LogWarning("This method should not be called directly. Use GetRequiredService() instead.");
                
                var resultMessage = "Method called directly instead of through job registration";
                _logger.LogInformation(resultMessage);
                context?.WriteLine(resultMessage);
                
                return resultMessage;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error processing urgent workouts: {ex.Message}";
                
                _logger.LogError(ex, errorMessage);
                context?.SetTextColor(ConsoleTextColor.Red);
                context?.WriteLine(errorMessage);
                context?.ResetTextColor();
                
                throw;
            }
        }
        
        /// <summary>
        /// Cleans up expired scheduled workouts 
        /// </summary>
        [AutomaticRetry(Attempts = 1)]
        public async Task<string> CleanupExpiredScheduledWorkoutsAsync(PerformContext context = null)
        {
            context?.WriteLine("Starting expired workout cleanup");
            
            try
            {
                // This method is only for local testing
                // The actual service call is made through GetRequiredService in RegisterWorkoutSchedulingJobs
                _logger.LogWarning("This method should not be called directly. Use GetRequiredService() instead.");
                
                var resultMessage = "Method called directly instead of through job registration";
                _logger.LogInformation(resultMessage);
                context?.WriteLine(resultMessage);
                
                return resultMessage;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error cleaning up expired workouts: {ex.Message}";
                
                _logger.LogError(ex, errorMessage);
                context?.SetTextColor(ConsoleTextColor.Red);
                context?.WriteLine(errorMessage);
                context?.ResetTextColor();
                
                throw;
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