using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Pages.Workouts
{
    [Authorize]
    public class ScheduledWorkoutsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<ScheduledWorkoutsModel> _logger;
        private readonly UserManager<AppUser> _userManager;

        public ScheduledWorkoutsModel(WorkoutTrackerWebContext context, 
                                     ILogger<ScheduledWorkoutsModel> logger, 
                                     UserManager<AppUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public List<ScheduleViewModel> UpcomingSchedules { get; set; } = new List<ScheduleViewModel>();
        public List<ScheduleViewModel> CompletedSchedules { get; set; } = new List<ScheduleViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
            {
                return Unauthorized();
            }

            try
            {
                // Get the application User entity that matches the identity user
                var appUser = await _context.User
                    .FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id);
                
                if (appUser == null)
                {
                    TempData["ErrorMessage"] = "User profile not found. Please contact support.";
                    return Page();
                }

                // Get current date for comparison
                var currentDate = DateTime.Now;

                // Get upcoming workouts (scheduled in the future)
                var upcomingSchedules = await _context.WorkoutSchedules
                    .Where(s => s.ClientUserId == appUser.UserId && 
                               (s.ScheduledDateTime > currentDate || 
                               (s.IsRecurring && (!s.EndDate.HasValue || s.EndDate.Value > currentDate.Date))))
                    .Include(s => s.TemplateAssignment)
                    .ThenInclude(a => a.WorkoutTemplate)
                    .Include(s => s.Template)
                    .OrderBy(s => s.ScheduledDateTime)
                    .ToListAsync();

                foreach (var schedule in upcomingSchedules)
                {
                    UpcomingSchedules.Add(CreateScheduleViewModel(schedule));
                }

                // Get completed workouts (in the past, not recurring or past end date)
                var completedSchedules = await _context.WorkoutSchedules
                    .Where(s => s.ClientUserId == appUser.UserId && 
                               s.ScheduledDateTime <= currentDate &&
                               (!s.IsRecurring || (s.EndDate.HasValue && s.EndDate.Value <= currentDate.Date)))
                    .Include(s => s.TemplateAssignment)
                    .ThenInclude(a => a.WorkoutTemplate)
                    .Include(s => s.Template)
                    .OrderByDescending(s => s.ScheduledDateTime)
                    .Take(10) // Limit to last 10 completed
                    .ToListAsync();

                foreach (var schedule in completedSchedules)
                {
                    CompletedSchedules.Add(CreateScheduleViewModel(schedule));
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading scheduled workouts for user {UserId}", identityUser.Id);
                TempData["ErrorMessage"] = "An error occurred loading your scheduled workouts.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostToggleScheduleAsync(int scheduleId, bool isActive)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
            {
                return Unauthorized();
            }

            // Get the application User entity that matches the identity user
            var appUser = await _context.User
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id);
            
            if (appUser == null)
            {
                TempData["ErrorMessage"] = "User profile not found. Please contact support.";
                return RedirectToPage();
            }

            var schedule = await _context.WorkoutSchedules
                .FirstOrDefaultAsync(s => s.WorkoutScheduleId == scheduleId && 
                                         s.ClientUserId == appUser.UserId);

            if (schedule == null)
            {
                TempData["ErrorMessage"] = "Schedule not found.";
                return RedirectToPage();
            }

            schedule.IsActive = isActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Workout schedule {(isActive ? "activated" : "paused")} successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteScheduleAsync(int scheduleId)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
            {
                return Unauthorized();
            }

            // Get the application User entity that matches the identity user
            var appUser = await _context.User
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id);
            
            if (appUser == null)
            {
                TempData["ErrorMessage"] = "User profile not found. Please contact support.";
                return RedirectToPage();
            }

            var schedule = await _context.WorkoutSchedules
                .FirstOrDefaultAsync(s => s.WorkoutScheduleId == scheduleId && 
                                         s.ClientUserId == appUser.UserId);

            if (schedule == null)
            {
                TempData["ErrorMessage"] = "Schedule not found.";
                return RedirectToPage();
            }

            _context.WorkoutSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Workout schedule deleted successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditScheduleAsync(int scheduleId, string name, string description,
                                                      string scheduleDate, string scheduleTime, 
                                                      DateTime? endDate, bool sendReminder = true,
                                                      int reminderHoursBefore = 3)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
            {
                return Unauthorized();
            }

            // Get the application User entity that matches the identity user
            var appUser = await _context.User
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id);
            
            if (appUser == null)
            {
                TempData["ErrorMessage"] = "User profile not found. Please contact support.";
                return RedirectToPage();
            }

            var schedule = await _context.WorkoutSchedules
                .FirstOrDefaultAsync(s => s.WorkoutScheduleId == scheduleId && 
                                         s.ClientUserId == appUser.UserId);

            if (schedule == null)
            {
                TempData["ErrorMessage"] = "Schedule not found.";
                return RedirectToPage();
            }

            try
            {
                // Parse date and time
                var scheduledDate = DateTime.Parse(scheduleDate);
                var scheduledTime = TimeSpan.Parse(scheduleTime);
                var scheduledDateTime = scheduledDate.Date.Add(scheduledTime);

                // Update the schedule
                schedule.Name = name;
                schedule.Description = description;
                schedule.ScheduledDateTime = scheduledDateTime;
                schedule.EndDate = endDate;
                schedule.SendReminder = sendReminder;
                schedule.ReminderHoursBefore = reminderHoursBefore;

                _context.Entry(schedule).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Workout schedule updated successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workout schedule {ScheduleId} for user {UserId}", scheduleId, appUser.UserId);
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostScheduleWorkoutAsync(int templateId, string name, string description, 
                                                         DateTime startDate, string workoutTime, 
                                                         DateTime? endDate, string recurrencePattern = "Once",
                                                         List<string> daysOfWeek = null, int? dayOfMonth = null,
                                                         bool sendReminder = true, int reminderHoursBefore = 3)
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
            {
                return Unauthorized();
            }

            try
            {
                // Get the application User entity that matches the identity user
                var appUser = await _context.User
                    .FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id);
                
                if (appUser == null)
                {
                    TempData["ErrorMessage"] = "User profile not found. Please contact support.";
                    return RedirectToPage();
                }

                // Validate the template exists and is accessible to the user
                var template = await _context.WorkoutTemplate
                    .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId && 
                                             (t.UserId == appUser.UserId || t.IsPublic));

                if (template == null)
                {
                    TempData["ErrorMessage"] = "Template not found or you don't have access to it.";
                    return RedirectToPage();
                }

                // Parse workout time - default to 5 PM if not provided
                TimeSpan timeOfDay = string.IsNullOrEmpty(workoutTime) ? 
                    new TimeSpan(17, 0, 0) : TimeSpan.Parse(workoutTime);
                
                var scheduledDateTime = startDate.Date.Add(timeOfDay);

                // Create the workout schedule
                var workoutSchedule = new Models.Coaching.WorkoutSchedule
                {
                    TemplateId = templateId,
                    ClientUserId = appUser.UserId,
                    CoachUserId = appUser.UserId, // Self-scheduling, user is both client and coach
                    Name = name,
                    Description = description,
                    StartDate = startDate,
                    ScheduledDateTime = scheduledDateTime,
                    EndDate = endDate,
                    IsActive = true,
                    SendReminder = sendReminder,
                    ReminderHoursBefore = reminderHoursBefore
                };

                // Handle recurrence pattern
                if (recurrencePattern != "Once")
                {
                    // Always explicitly set both properties in the correct order to ensure IsRecurring is properly saved
                    workoutSchedule.IsRecurring = true;
                    workoutSchedule.RecurrencePattern = recurrencePattern;
                    
                    // For weekly or bi-weekly recurrence, set the day of week
                    if ((recurrencePattern == "Weekly" || recurrencePattern == "BiWeekly") && daysOfWeek != null && daysOfWeek.Any())
                    {
                        // Store the first day in the RecurrenceDayOfWeek property for backward compatibility
                        DayOfWeek firstDay = Enum.Parse<DayOfWeek>(daysOfWeek.First());
                        workoutSchedule.RecurrenceDayOfWeek = (int)firstDay;
                        
                        // Store all days in the MultipleDaysOfWeek property
                        if (daysOfWeek.Count > 1)
                        {
                            workoutSchedule.MultipleDaysOfWeek = string.Join(",", daysOfWeek.Select(d => (int)Enum.Parse<DayOfWeek>(d)));
                        }
                    }
                    else if (recurrencePattern == "Weekly" || recurrencePattern == "BiWeekly")
                    {
                        // Default to the day of the start date
                        workoutSchedule.RecurrenceDayOfWeek = (int)startDate.DayOfWeek;
                    }
                    // For monthly recurrence, set the day of month
                    else if (recurrencePattern == "Monthly" && dayOfMonth.HasValue)
                    {
                        workoutSchedule.RecurrenceDayOfMonth = dayOfMonth.Value;
                    }
                    else if (recurrencePattern == "Monthly")
                    {
                        // Default to the day of the month from the start date
                        workoutSchedule.RecurrenceDayOfMonth = startDate.Day;
                    }
                }
                else
                {
                    // Explicitly set for non-recurring workouts to ensure DB state is correct
                    workoutSchedule.IsRecurring = false;
                    workoutSchedule.RecurrencePattern = "Once";
                }
                
                // Force consistency before saving to ensure database state is correct
                workoutSchedule.EnsureConsistentRecurringState();

                _context.WorkoutSchedules.Add(workoutSchedule);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Workout scheduled successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling workout for template {TemplateId} by user {UserId}", 
                    templateId, identityUser.Id);
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToPage();
            }
        }

        private ScheduleViewModel CreateScheduleViewModel(Models.Coaching.WorkoutSchedule schedule)
        {
            return new ScheduleViewModel
            {
                WorkoutScheduleId = schedule.WorkoutScheduleId,
                TemplateAssignmentId = schedule.TemplateAssignmentId,
                TemplateName = schedule.Template?.Name ?? schedule.TemplateAssignment?.WorkoutTemplate?.Name,
                Name = schedule.Name,
                Description = schedule.Description,
                StartDate = schedule.StartDate,
                EndDate = schedule.EndDate,
                ScheduledDateTime = schedule.ScheduledDateTime,
                IsRecurring = schedule.IsRecurring,
                RecurrencePattern = schedule.RecurrencePattern,
                RecurrenceDayOfWeek = schedule.RecurrenceDayOfWeek,
                RecurrenceDayOfMonth = schedule.RecurrenceDayOfMonth,
                MultipleDaysOfWeek = schedule.MultipleDaysOfWeek,
                SendReminder = schedule.SendReminder,
                ReminderHoursBefore = schedule.ReminderHoursBefore,
                IsActive = schedule.IsActive,
                LastGeneratedWorkoutDate = schedule.LastGeneratedWorkoutDate,
                LastGeneratedSessionId = schedule.LastGeneratedSessionId,
                TotalWorkoutsGenerated = schedule.TotalWorkoutsGenerated,
                LastGenerationStatus = schedule.LastGenerationStatus
            };
        }

        public class ScheduleViewModel
        {
            public int WorkoutScheduleId { get; set; }
            public int? TemplateAssignmentId { get; set; }
            public string TemplateName { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public DateTime? ScheduledDateTime { get; set; }
            public bool IsRecurring { get; set; }
            public string RecurrencePattern { get; set; }
            public int? RecurrenceDayOfWeek { get; set; }
            public int? RecurrenceDayOfMonth { get; set; }
            public string MultipleDaysOfWeek { get; set; }
            public bool SendReminder { get; set; }
            public int ReminderHoursBefore { get; set; }
            public bool IsActive { get; set; }
            
            // Status tracking properties
            public DateTime? LastGeneratedWorkoutDate { get; set; }
            public int? LastGeneratedSessionId { get; set; }
            public int TotalWorkoutsGenerated { get; set; }
            public string LastGenerationStatus { get; set; }
            
            // Helper method to get all days of week as a list of DayOfWeek enums
            public List<DayOfWeek> GetAllDaysOfWeek()
            {
                var result = new List<DayOfWeek>();
                
                // Add the primary day of week if present
                if (RecurrenceDayOfWeek.HasValue)
                {
                    result.Add((DayOfWeek)RecurrenceDayOfWeek.Value);
                }
                
                // Add additional days if any
                if (!string.IsNullOrEmpty(MultipleDaysOfWeek))
                {
                    // Parse comma-separated values and add any days not already in the list
                    foreach (var dayValue in MultipleDaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (int.TryParse(dayValue, out int dayInt) && !result.Contains((DayOfWeek)dayInt))
                        {
                            result.Add((DayOfWeek)dayInt);
                        }
                    }
                }
                
                return result;
            }
            
            // Helper method to get a CSS class based on generation status
            public string GetStatusClass()
            {
                if (string.IsNullOrEmpty(LastGenerationStatus))
                    return "bg-secondary";
                
                return LastGenerationStatus.StartsWith("Failed") ? "bg-danger" : 
                       LastGenerationStatus == "Success" ? "bg-success" : "bg-secondary";
            }
            
            // Helper method to get a user-friendly status message
            public string GetStatusMessage()
            {
                if (string.IsNullOrEmpty(LastGenerationStatus))
                    return "Pending";
                
                if (LastGenerationStatus == "Success")
                {
                    return $"Generated {TotalWorkoutsGenerated} workout{(TotalWorkoutsGenerated != 1 ? "s" : "")}";
                }
                
                return LastGenerationStatus;
            }
        }
    }
}