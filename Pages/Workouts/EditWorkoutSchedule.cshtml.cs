using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Extensions;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;

namespace WorkoutTrackerWeb.Pages.Workouts
{
    [Authorize]
    public class EditWorkoutScheduleModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<EditWorkoutScheduleModel> _logger;

        public EditWorkoutScheduleModel(
            WorkoutTrackerWebContext context,
            ILogger<EditWorkoutScheduleModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Models.Coaching.WorkoutSchedule WorkoutSchedule { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? scheduleId)
        {
            var identityUserId = User.GetUserId();
            if (identityUserId == null)
                return RedirectToPage("/Account/Login");

            // Map Identity GUID to internal int UserId
            var appUser = await _context.User.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
            if (appUser == null)
            {
                _logger.LogError("No AppUser found for IdentityUserId {IdentityUserId}", identityUserId);
                return NotFound();
            }

            if (!scheduleId.HasValue || scheduleId.Value <= 0)
            {
                _logger.LogError("No scheduleId provided for user {UserId}", appUser.UserId);
                return NotFound();
            }

            WorkoutSchedule = await _context.WorkoutSchedules
                .Include(w => w.Template)
                .Include(w => w.TemplateAssignment)
                    .ThenInclude(ta => ta != null ? ta.WorkoutTemplate : null)
                .FirstOrDefaultAsync(w => w.WorkoutScheduleId == scheduleId.Value && w.ClientUserId == appUser.UserId);

            if (WorkoutSchedule == null)
            {
                _logger.LogError("No Schedule found for user {UserId}", appUser.UserId);
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(DateTime scheduleDate, TimeSpan scheduleTime)
        {
            var identityUserId = User.GetUserId();
            if (identityUserId == null)
                return RedirectToPage("/Account/Login");

            // Map Identity GUID to internal int UserId
            var appUser = await _context.User.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
            if (appUser == null)
            {
                _logger.LogError("No AppUser found for IdentityUserId {IdentityUserId}", identityUserId);
                return NotFound();
            }

            var originalSchedule = await _context.WorkoutSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WorkoutScheduleId == WorkoutSchedule.WorkoutScheduleId && w.ClientUserId == appUser.UserId);

            if (originalSchedule == null)
            {
                _logger.LogError("No Schedule found for user {UserId}", appUser.UserId);
                return NotFound();
            }

            // We can't change certain properties like recurrence pattern in the edit form
            // so we need to restore these from the original entity
            WorkoutSchedule.ClientUserId = appUser.UserId;
            WorkoutSchedule.IsRecurring = originalSchedule.IsRecurring;
            WorkoutSchedule.RecurrencePattern = originalSchedule.RecurrencePattern;
            WorkoutSchedule.RecurrenceDayOfWeek = originalSchedule.RecurrenceDayOfWeek;
            WorkoutSchedule.RecurrenceDayOfMonth = originalSchedule.RecurrenceDayOfMonth;
            WorkoutSchedule.TemplateId = originalSchedule.TemplateId;
            WorkoutSchedule.TemplateAssignmentId = originalSchedule.TemplateAssignmentId;
            WorkoutSchedule.CoachUserId = originalSchedule.CoachUserId;

            // Combine the date and time
            WorkoutSchedule.ScheduledDateTime = scheduleDate.Date.Add(scheduleTime);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                _context.Attach(WorkoutSchedule).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                // Log the update
                _logger.LogInformation("User {UserId} updated workout schedule {ScheduleId}", appUser.UserId, WorkoutSchedule.WorkoutScheduleId);

                TempData["SuccessMessage"] = "Workout schedule updated successfully.";
                return RedirectToPage("./ScheduledWorkouts");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating workout schedule {ScheduleId}", WorkoutSchedule.WorkoutScheduleId);
                ErrorMessage = "This workout schedule has been modified by another user. Please refresh and try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workout schedule {ScheduleId}", WorkoutSchedule.WorkoutScheduleId);
                ErrorMessage = "An error occurred while updating the workout schedule. Please try again.";
            }

            return Page();
        }
    }
}