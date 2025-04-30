using System;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Services.Hangfire
{
    /// <summary>
    /// Handles the registration of recurring jobs for workout reminders
    /// </summary>
    public class WorkoutReminderJobsRegistration
    {
        private readonly IRecurringJobManager _recurringJobManager;
        private readonly ILogger<WorkoutReminderJobsRegistration> _logger;
        private readonly WorkoutReminderJobsService _reminderJobsService;
        private readonly HangfireServerConfiguration _serverConfig;

        /// <summary>
        /// Creates a new instance of the WorkoutReminderJobsRegistration class
        /// </summary>
        public WorkoutReminderJobsRegistration(
            IRecurringJobManager recurringJobManager,
            WorkoutReminderJobsService reminderJobsService,
            HangfireServerConfiguration serverConfig,
            ILogger<WorkoutReminderJobsRegistration> logger)
        {
            _recurringJobManager = recurringJobManager ?? throw new ArgumentNullException(nameof(recurringJobManager));
            _reminderJobsService = reminderJobsService ?? throw new ArgumentNullException(nameof(reminderJobsService));
            _serverConfig = serverConfig ?? throw new ArgumentNullException(nameof(serverConfig));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Registers all workout reminder jobs in Hangfire
        /// </summary>
        public void RegisterWorkoutReminderJobs()
        {
            _logger.LogInformation("Registering workout reminder jobs with server role: {ProcessingRole}", 
                _serverConfig.IsProcessingEnabled ? "Worker" : "Web");

            // Register the workout reminder processing job to run every 15 minutes
            _recurringJobManager.AddOrUpdate(
                "workout-reminder-processing",
                () => _reminderJobsService.ProcessWorkoutRemindersAsync(),
                "*/15 * * * *", // Every 15 minutes
                TimeZoneInfo.Local,
                "default");

            _logger.LogInformation("Successfully registered workout reminder jobs");
        }
    }
}