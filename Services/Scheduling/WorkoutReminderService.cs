using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Email;

namespace WorkoutTrackerWeb.Services.Scheduling
{
    /// <summary>
    /// Service responsible for sending reminder notifications for scheduled workouts
    /// </summary>
    public class WorkoutReminderService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ApplicationDbContext _identityContext;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<WorkoutReminderService> _logger;
        private readonly TimeZoneInfo _timeZone;

        public WorkoutReminderService(
            WorkoutTrackerWebContext context,
            ApplicationDbContext identityContext,
            UserManager<AppUser> userManager,
            IEmailService emailService,
            ILogger<WorkoutReminderService> logger,
            IOptions<ScheduledWorkoutProcessorOptions> options)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _identityContext = identityContext ?? throw new ArgumentNullException(nameof(identityContext));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Use the same timezone configuration as the workout processor
            _timeZone = options.Value.UseLocalTimeZone 
                ? TimeZoneInfo.Local 
                : TimeZoneInfo.Utc;
                
            _logger.LogInformation("WorkoutReminderService initialized with timezone: {TimeZone}", 
                _timeZone.DisplayName);
        }

        /// <summary>
        /// Processes and sends reminders for upcoming scheduled workouts
        /// </summary>
        /// <returns>The number of reminders sent</returns>
        public async Task<int> ProcessWorkoutRemindersAsync()
        {
            int remindersSent = 0;
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
            
            _logger.LogInformation("Processing workout reminders at {Now}", now);

            try
            {
                // Get all active scheduled workouts that have reminders enabled and haven't had a reminder sent yet
                // or the reminder was sent for a different occurrence
                var scheduledWorkouts = await _context.WorkoutSchedules
                    .Where(s => s.IsActive && 
                               s.SendReminder && 
                               s.ScheduledDateTime.HasValue &&
                               (s.LastReminderSent == null || 
                               s.LastReminderSent.Value.AddHours(s.ReminderHoursBefore) < s.ScheduledDateTime.Value))
                    .Include(s => s.Client)
                    .Include(s => s.Coach)
                    .Include(s => s.Template)
                    .Include(s => s.TemplateAssignment)
                    .ThenInclude(a => a.WorkoutTemplate)
                    .OrderBy(s => s.ScheduledDateTime)
                    .ToListAsync();

                foreach (var workout in scheduledWorkouts)
                {
                    // Skip if scheduled time is null (shouldn't happen due to filter, but just in case)
                    if (!workout.ScheduledDateTime.HasValue)
                    {
                        continue;
                    }
                    
                    // Calculate when the reminder should be sent
                    var reminderTime = workout.ScheduledDateTime.Value.AddHours(-workout.ReminderHoursBefore);
                    
                    // Only send reminder if it's due (current time is past the reminder time)
                    if (now >= reminderTime)
                    {
                        var success = await SendWorkoutReminderAsync(workout);
                        if (success)
                        {
                            // Update the LastReminderSent timestamp
                            workout.LastReminderSent = now;
                            _context.Update(workout);
                            remindersSent++;
                        }
                    }
                }
                
                if (remindersSent > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully sent {Count} workout reminders", remindersSent);
                }
                else
                {
                    _logger.LogInformation("No workout reminders were due at this time");
                }
                
                return remindersSent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing workout reminders");
                throw; // Rethrow to let Hangfire know the job failed
            }
        }
        
        /// <summary>
        /// Sends a reminder email for a specific scheduled workout
        /// </summary>
        private async Task<bool> SendWorkoutReminderAsync(WorkoutSchedule workout)
        {
            try
            {
                if (workout.Client == null)
                {
                    _logger.LogWarning("Cannot send workout reminder for workout {Id} - client not found", 
                        workout.WorkoutScheduleId);
                    return false;
                }
                
                // Get the Identity user to access their email
                var identityUser = await _identityContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == workout.Client.IdentityUserId);
                
                if (identityUser == null || string.IsNullOrEmpty(identityUser.Email))
                {
                    _logger.LogWarning("Cannot send workout reminder - no identity user or email found for client {ClientId}", 
                        workout.ClientUserId);
                    return false;
                }
                
                // Get template name
                var templateName = workout.Template?.Name ?? 
                                   workout.TemplateAssignment?.WorkoutTemplate?.Name ?? 
                                   "Custom Workout";
                
                // Format workout time
                var workoutTime = workout.ScheduledDateTime?.ToString("dddd, MMMM d, yyyy 'at' h:mm tt");
                
                // Prepare email subject and message
                string subject = $"Reminder: Your workout '{workout.Name}' is coming up";
                string message = $@"
                    <h1>Workout Reminder</h1>
                    <p>Hello {workout.Client.Name},</p>
                    <p>This is a reminder that you have a workout scheduled for <strong>{workoutTime}</strong>.</p>
                    <h2>Workout Details</h2>
                    <ul>
                        <li><strong>Name:</strong> {workout.Name}</li>
                        <li><strong>Template:</strong> {templateName}</li>
                        <li><strong>Time:</strong> {workoutTime}</li>
                    </ul>
                    ";
                
                // Add description if available
                if (!string.IsNullOrEmpty(workout.Description))
                {
                    message += $@"<p><strong>Description:</strong> {workout.Description}</p>";
                }
                
                // Add coach information if available
                if (workout.Coach != null)
                {
                    message += $@"<p><strong>Coach:</strong> {workout.Coach.Name}</p>";
                }
                
                // Add closing message
                message += $@"
                    <p>Be sure to prepare adequately and stay hydrated!</p>
                    <p>Visit <a href='https://workouttracker.online/Workouts'>your workout dashboard</a> to view more details.</p>
                    <p>Good luck with your workout!</p>
                    ";

                // Send the email
                await _emailService.SendEmailAsync(identityUser.Email, subject, message);
                
                _logger.LogInformation("Sent workout reminder email for workout {WorkoutId} to {Email}",
                    workout.WorkoutScheduleId, identityUser.Email);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send workout reminder for workout {WorkoutId}", 
                    workout.WorkoutScheduleId);
                return false;
            }
        }
    }
}