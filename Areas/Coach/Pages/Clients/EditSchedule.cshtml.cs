using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Clients
{
    [CoachAuthorize]
    public class EditScheduleModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<EditScheduleModel> _logger;

        public EditScheduleModel(
            WorkoutTrackerWebContext context,
            ILogger<EditScheduleModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public WorkoutSchedule WorkoutSchedule { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ClientId { get; set; }

        public User Client { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int scheduleId)
        {
            if (string.IsNullOrEmpty(ClientId))
            {
                return RedirectToPage("./Index");
            }

            // Get the client and verify the coach has access
            Client = await _context.User.FirstOrDefaultAsync(u => u.IdentityUserId == ClientId);
            if (Client == null)
            {
                return NotFound("Client not found");
            }

            var coachId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (coachId == null || !await _context.CoachClientRelationships.AnyAsync(cc => cc.CoachId == coachId && cc.ClientId == ClientId))
            {
                return Forbid();
            }

            WorkoutSchedule = await _context.WorkoutSchedules
                .Include(w => w.Template)
                .Include(w => w.TemplateAssignment)
                    .ThenInclude(ta => ta != null ? ta.WorkoutTemplate : null)
                .FirstOrDefaultAsync(w => w.WorkoutScheduleId == scheduleId && w.ClientUserId.ToString() == ClientId);

            if (WorkoutSchedule == null)
            {
                return NotFound("Workout schedule not found");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(DateTime scheduleDate, TimeSpan scheduleTime)
        {
            if (string.IsNullOrEmpty(ClientId))
            {
                return RedirectToPage("./Index");
            }

            // Get the client and verify the coach has access
            Client = await _context.User.FirstOrDefaultAsync(u => u.IdentityUserId == ClientId);
            if (Client == null)
            {
                return NotFound("Client not found");
            }

            var coachId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (coachId == null || !await _context.CoachClientRelationships.AnyAsync(cc => cc.CoachId == coachId && cc.ClientId == ClientId))
            {
                return Forbid();
            }

            // Get the original entity from the database
            var originalSchedule = await _context.WorkoutSchedules
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WorkoutScheduleId == WorkoutSchedule.WorkoutScheduleId && w.ClientUserId.ToString() == ClientId);

            if (originalSchedule == null)
            {
                return NotFound("Workout schedule not found");
            }

            // We can't change certain properties like recurrence pattern in the edit form
            // so we need to restore these from the original entity
            WorkoutSchedule.ClientUserId = int.Parse(ClientId);
            WorkoutSchedule.IsRecurring = originalSchedule.IsRecurring;
            WorkoutSchedule.RecurrencePattern = originalSchedule.RecurrencePattern;
            WorkoutSchedule.RecurrenceDayOfWeek = originalSchedule.RecurrenceDayOfWeek;
            WorkoutSchedule.RecurrenceDayOfMonth = originalSchedule.RecurrenceDayOfMonth;
            WorkoutSchedule.TemplateId = originalSchedule.TemplateId;
            WorkoutSchedule.TemplateAssignmentId = originalSchedule.TemplateAssignmentId;
            WorkoutSchedule.CoachUserId = int.Parse(coachId);

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
                _logger.LogInformation("Coach {CoachId} updated workout schedule {ScheduleId} for client {ClientId}", 
                    coachId, WorkoutSchedule.WorkoutScheduleId, ClientId);

                TempData["SuccessMessage"] = "Workout schedule updated successfully.";
                return RedirectToPage("./AssignedWorkouts", new { clientId = ClientId });
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