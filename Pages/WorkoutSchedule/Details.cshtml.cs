using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Extensions;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Models.Coaching;

namespace WorkoutTrackerWeb.Pages.WorkoutSchedule
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<DetailsModel> _logger;
        private readonly UserManager<AppUser> _userManager;

        public DetailsModel(
            WorkoutTrackerWebContext context,
            ILogger<DetailsModel> logger,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public Models.Coaching.WorkoutSchedule WorkoutSchedule { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsCoach { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            try
            {
                // Get the application User entity that matches the identity user
                var appUser = await _context.User
                    .FirstOrDefaultAsync(u => u.IdentityUserId == userId);
                
                if (appUser == null)
                {
                    ErrorMessage = "User profile not found. Please contact support.";
                    return Page();
                }

                // Find the schedule and include related data
                WorkoutSchedule = await _context.WorkoutSchedules
                    .Include(w => w.Template)
                    .Include(w => w.TemplateAssignment)
                        .ThenInclude(ta => ta != null ? ta.WorkoutTemplate : null)
                    .Include(w => w.Client)
                    .Include(w => w.Coach)
                    .FirstOrDefaultAsync(w => w.WorkoutScheduleId == id && 
                                             (w.ClientUserId == appUser.UserId || 
                                              w.CoachUserId == appUser.UserId));

                if (WorkoutSchedule == null)
                {
                    ErrorMessage = "Scheduled workout not found or you don't have permission to view it.";
                    return Page();
                }
                
                _logger.LogInformation("[ScheduleDebug] Retrieved WorkoutSchedule {ScheduleId} for user {UserId}: {@ScheduleDetails}",
                    id, appUser.UserId, new {
                        WorkoutSchedule.WorkoutScheduleId,
                        WorkoutSchedule.Name,
                        WorkoutSchedule.RecurrencePattern,
                        WorkoutSchedule.IsRecurring,
                        WorkoutSchedule.RecurrenceDayOfWeek,
                        WorkoutSchedule.RecurrenceDayOfMonth,
                        WorkoutSchedule.MultipleDaysOfWeek,
                        TemplateAssignmentId = WorkoutSchedule.TemplateAssignmentId,
                        ClientUserId = WorkoutSchedule.ClientUserId,
                        CoachUserId = WorkoutSchedule.CoachUserId,
                        DatabaseEntry = new {
                            RawIsRecurring = _context.Entry(WorkoutSchedule).Property("IsRecurring").CurrentValue,
                            RawRecurrencePattern = _context.Entry(WorkoutSchedule).Property("RecurrencePattern").CurrentValue
                        }
                    });

                // Fix for recurring workouts display: Ensure IsRecurring is set correctly based on recurrence pattern
                if (!string.IsNullOrEmpty(WorkoutSchedule.RecurrencePattern) && WorkoutSchedule.RecurrencePattern != "Once")
                {
                    // If the workout has a recurrence pattern that's not "Once", it should be recurring
                    if (!WorkoutSchedule.IsRecurring)
                    {
                        _logger.LogWarning("Found workout schedule {ScheduleId} with recurrence pattern {Pattern} but IsRecurring=false. Setting IsRecurring to true.",
                            WorkoutSchedule.WorkoutScheduleId, WorkoutSchedule.RecurrencePattern);
                        
                        WorkoutSchedule.IsRecurring = true;
                        _context.Entry(WorkoutSchedule).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                    }
                }

                // Check if the user is a coach for this schedule
                IsCoach = WorkoutSchedule.CoachUserId == appUser.UserId;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading schedule {ScheduleId} for user {UserId}", id, userId);
                ErrorMessage = "An error occurred loading the schedule.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostToggleActiveAsync(int id, bool isActive)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            // Get the application User entity that matches the identity user
            var appUser = await _context.User
                .FirstOrDefaultAsync(u => u.IdentityUserId == userId);
            
            if (appUser == null)
            {
                TempData["ErrorMessage"] = "User profile not found. Please contact support.";
                return RedirectToPage("/Workouts/ScheduledWorkouts");
            }

            var schedule = await _context.WorkoutSchedules
                .FirstOrDefaultAsync(s => s.WorkoutScheduleId == id && 
                                         (s.ClientUserId == appUser.UserId || 
                                          s.CoachUserId == appUser.UserId));

            if (schedule == null)
            {
                TempData["ErrorMessage"] = "Schedule not found.";
                return RedirectToPage("/Workouts/ScheduledWorkouts");
            }

            try
            {
                schedule.IsActive = isActive;
                _context.Entry(schedule).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Workout schedule {(isActive ? "activated" : "paused")} successfully.";
                return RedirectToPage(new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workout schedule {ScheduleId} active status for user {UserId}", id, appUser.UserId);
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToPage(new { id });
            }
        }

        public async Task<IActionResult> OnPostDeleteScheduleAsync(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            // Get the application User entity that matches the identity user
            var appUser = await _context.User
                .FirstOrDefaultAsync(u => u.IdentityUserId == userId);
            
            if (appUser == null)
            {
                TempData["ErrorMessage"] = "User profile not found. Please contact support.";
                return RedirectToPage("/Workouts/ScheduledWorkouts");
            }

            var schedule = await _context.WorkoutSchedules
                .FirstOrDefaultAsync(s => s.WorkoutScheduleId == id && 
                                         (s.ClientUserId == appUser.UserId || 
                                          s.CoachUserId == appUser.UserId));

            if (schedule == null)
            {
                TempData["ErrorMessage"] = "Schedule not found.";
                return RedirectToPage("/Workouts/ScheduledWorkouts");
            }

            try
            {
                _context.WorkoutSchedules.Remove(schedule);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Workout schedule deleted successfully.";
                return RedirectToPage("/Workouts/ScheduledWorkouts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workout schedule {ScheduleId} for user {UserId}", id, appUser.UserId);
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToPage(new { id });
            }
        }
    }
}