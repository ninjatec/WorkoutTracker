using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services.Scheduling;

namespace WorkoutTrackerWeb.Services.Hangfire
{
    /// <summary>
    /// Service that implements background job methods for workout reminders
    /// </summary>
    public class WorkoutReminderJobsService
    {
        private readonly ILogger<WorkoutReminderJobsService> _logger;
        private readonly WorkoutReminderService _reminderService;

        /// <summary>
        /// Creates a new instance of the WorkoutReminderJobsService
        /// </summary>
        public WorkoutReminderJobsService(
            WorkoutReminderService reminderService,
            ILogger<WorkoutReminderJobsService> logger)
        {
            _reminderService = reminderService ?? throw new ArgumentNullException(nameof(reminderService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Processes and sends due workout reminders
        /// </summary>
        public async Task ProcessWorkoutRemindersAsync()
        {
            _logger.LogInformation("Starting workout reminder processing job");
            try
            {
                int remindersSent = await _reminderService.ProcessWorkoutRemindersAsync();
                _logger.LogInformation("Workout reminder processing job completed. Sent {Count} reminders", remindersSent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during workout reminder processing");
                throw; // Rethrow to let Hangfire know the job failed
            }
        }
    }
}