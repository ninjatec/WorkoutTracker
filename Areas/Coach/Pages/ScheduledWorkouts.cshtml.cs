using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Extensions;

namespace WorkoutTrackerWeb.Areas.Coach.Pages
{
    [CoachAuthorize]
    public class ScheduledWorkoutsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<ScheduledWorkoutsModel> _logger;

        public ScheduledWorkoutsModel(WorkoutTrackerWebContext context, ILogger<ScheduledWorkoutsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int? ClientId { get; set; }

        public bool IsClientView => ClientId.HasValue;
        public User Client { get; set; }
        public List<ScheduleViewModel> UpcomingSchedules { get; set; } = new List<ScheduleViewModel>();
        public List<ScheduleViewModel> CompletedSchedules { get; set; } = new List<ScheduleViewModel>();
        public List<WorkoutTemplate> AvailableTemplates { get; set; } = new List<WorkoutTemplate>();
        public List<TemplateAssignment> AvailableAssignments { get; set; } = new List<TemplateAssignment>();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            try
            {
                // Get available templates for this coach
                AvailableTemplates = await _context.WorkoutTemplate
                    .Where(t => t.UserId == user.UserId || t.IsPublic)
                    .OrderByDescending(t => t.LastModifiedDate)
                    .ToListAsync();

                // Get current date for comparison
                var currentDate = DateTime.Now;

                // If this is a client-specific view, load client details
                if (ClientId.HasValue)
                {
                    // Verify the client relationship
                    var relationship = await _context.CoachClientRelationships
                        .FirstOrDefaultAsync(r => r.CoachId == user.UserId.ToString() && 
                                                 r.ClientId == ClientId.Value.ToString() && 
                                                 r.Status == RelationshipStatus.Active);

                    if (relationship == null)
                    {
                        TempData["ErrorMessage"] = "You do not have permission to view this client's workouts.";
                        return RedirectToPage("./Clients/Index");
                    }

                    // Load client details
                    Client = await _context.User.FindAsync(ClientId.Value);
                    if (Client == null)
                    {
                        TempData["ErrorMessage"] = "Client not found.";
                        return RedirectToPage("./Clients/Index");
                    }

                    // Load available assignments for this client
                    AvailableAssignments = await _context.TemplateAssignments
                        .Include(a => a.WorkoutTemplate)
                        .Where(a => a.ClientUserId == ClientId.Value && 
                                   a.CoachUserId == user.UserId && 
                                   a.IsActive)
                        .OrderBy(a => a.WorkoutTemplate.Name)
                        .ToListAsync();

                    // Get upcoming workouts for this client (scheduled in the future)
                    var upcomingSchedules = await _context.WorkoutSchedules
                        .Where(s => s.ClientUserId == ClientId.Value && 
                                   s.CoachUserId == user.UserId &&
                                   (s.ScheduledDateTime > currentDate || 
                                   (s.IsRecurring && (!s.EndDate.HasValue || s.EndDate.Value > currentDate.Date))))
                        .Include(s => s.TemplateAssignment)
                        .ThenInclude(a => a.WorkoutTemplate)
                        .Include(s => s.Template)
                        .Include(s => s.Client)
                        .OrderBy(s => s.ScheduledDateTime)
                        .ToListAsync();

                    foreach (var schedule in upcomingSchedules)
                    {
                        UpcomingSchedules.Add(CreateScheduleViewModel(schedule));
                    }

                    // Get completed workouts (in the past, not recurring or past end date)
                    var completedSchedules = await _context.WorkoutSchedules
                        .Where(s => s.ClientUserId == ClientId.Value && 
                                   s.CoachUserId == user.UserId &&
                                   s.ScheduledDateTime <= currentDate &&
                                   (!s.IsRecurring || (s.EndDate.HasValue && s.EndDate.Value <= currentDate.Date)))
                        .Include(s => s.TemplateAssignment)
                        .ThenInclude(a => a.WorkoutTemplate)
                        .Include(s => s.Template)
                        .Include(s => s.Client)
                        .OrderByDescending(s => s.ScheduledDateTime)
                        .Take(10) // Limit to last 10 completed
                        .ToListAsync();

                    foreach (var schedule in completedSchedules)
                    {
                        CompletedSchedules.Add(CreateScheduleViewModel(schedule));
                    }
                }
                else
                {
                    // Get ALL upcoming workouts for all clients of this coach (scheduled in the future)
                    var upcomingSchedules = await _context.WorkoutSchedules
                        .Where(s => s.CoachUserId == user.UserId &&
                                   (s.ScheduledDateTime > currentDate || 
                                   (s.IsRecurring && (!s.EndDate.HasValue || s.EndDate.Value > currentDate.Date))))
                        .Include(s => s.TemplateAssignment)
                        .ThenInclude(a => a.WorkoutTemplate)
                        .Include(s => s.Template)
                        .Include(s => s.Client)
                        .OrderBy(s => s.ScheduledDateTime)
                        .ToListAsync();

                    foreach (var schedule in upcomingSchedules)
                    {
                        UpcomingSchedules.Add(CreateScheduleViewModel(schedule));
                    }

                    // Get ALL completed workouts (in the past, not recurring or past end date)
                    var completedSchedules = await _context.WorkoutSchedules
                        .Where(s => s.CoachUserId == user.UserId &&
                                   s.ScheduledDateTime <= currentDate &&
                                   (!s.IsRecurring || (s.EndDate.HasValue && s.EndDate.Value <= currentDate.Date)))
                        .Include(s => s.TemplateAssignment)
                        .ThenInclude(a => a.WorkoutTemplate)
                        .Include(s => s.Template)
                        .Include(s => s.Client)
                        .OrderByDescending(s => s.ScheduledDateTime)
                        .Take(10) // Limit to last 10 completed
                        .ToListAsync();

                    foreach (var schedule in completedSchedules)
                    {
                        CompletedSchedules.Add(CreateScheduleViewModel(schedule));
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading scheduled workouts for coach {CoachId}", user.UserId);
                TempData["ErrorMessage"] = "An error occurred loading scheduled workouts.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostToggleScheduleAsync(int scheduleId, bool isActive)
        {
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            var schedule = await _context.WorkoutSchedules
                .FirstOrDefaultAsync(s => s.WorkoutScheduleId == scheduleId && 
                                         s.CoachUserId == user.UserId);

            if (schedule == null)
            {
                TempData["ErrorMessage"] = "Schedule not found.";
                return RedirectToPage(new { ClientId });
            }

            schedule.IsActive = isActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Workout schedule {(isActive ? "activated" : "paused")} successfully.";
            return RedirectToPage(new { ClientId });
        }

        public async Task<IActionResult> OnPostDeleteScheduleAsync(int scheduleId)
        {
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            var schedule = await _context.WorkoutSchedules
                .FirstOrDefaultAsync(s => s.WorkoutScheduleId == scheduleId && 
                                         s.CoachUserId == user.UserId);

            if (schedule == null)
            {
                TempData["ErrorMessage"] = "Schedule not found.";
                return RedirectToPage(new { ClientId });
            }

            _context.WorkoutSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Workout schedule deleted successfully.";
            return RedirectToPage(new { ClientId });
        }

        public async Task<IActionResult> OnPostEditScheduleAsync(int scheduleId, string name, string description,
                                                      string scheduleDate, string scheduleTime, 
                                                      DateTime? endDate, bool sendReminder = true,
                                                      int reminderHoursBefore = 3)
        {
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            var schedule = await _context.WorkoutSchedules
                .FirstOrDefaultAsync(s => s.WorkoutScheduleId == scheduleId && 
                                         s.CoachUserId == user.UserId);

            if (schedule == null)
            {
                TempData["ErrorMessage"] = "Schedule not found.";
                return RedirectToPage(new { ClientId });
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
                return RedirectToPage(new { ClientId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workout schedule {ScheduleId} for coach {UserId}", scheduleId, user.UserId);
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToPage(new { ClientId });
            }
        }

        public async Task<IActionResult> OnPostScheduleWorkoutAsync(int clientId, int? templateId, int? assignmentId,
                                                         string name, string description, 
                                                         DateTime startDate, string workoutTime, 
                                                         string recurrencePattern = "Once",
                                                         List<string> daysOfWeek = null, int? dayOfMonth = null,
                                                         DateTime? endDate = null, bool sendReminder = true,
                                                         int reminderHoursBefore = 3)
        {
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            try
            {
                // Verify the client relationship
                var relationship = await _context.CoachClientRelationships
                    .FirstOrDefaultAsync(r => r.CoachId == user.UserId.ToString() && 
                                             r.ClientId == clientId.ToString() && 
                                             r.Status == RelationshipStatus.Active);

                if (relationship == null)
                {
                    TempData["ErrorMessage"] = "You do not have permission to schedule workouts for this client.";
                    return RedirectToPage(new { ClientId });
                }

                // Validate the template or assignment
                if (templateId.HasValue)
                {
                    var template = await _context.WorkoutTemplate
                        .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId.Value && 
                                                 (t.UserId == user.UserId || t.IsPublic));

                    if (template == null)
                    {
                        TempData["ErrorMessage"] = "Template not found or you don't have access to it.";
                        return RedirectToPage(new { ClientId });
                    }
                }
                else if (assignmentId.HasValue)
                {
                    var assignment = await _context.TemplateAssignments
                        .FirstOrDefaultAsync(a => a.TemplateAssignmentId == assignmentId.Value && 
                                                 a.CoachUserId == user.UserId && 
                                                 a.ClientUserId == clientId);

                    if (assignment == null)
                    {
                        TempData["ErrorMessage"] = "Template assignment not found.";
                        return RedirectToPage(new { ClientId });
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Please select a template or assignment.";
                    return RedirectToPage(new { ClientId });
                }

                // Parse workout time
                TimeSpan timeOfDay = string.IsNullOrEmpty(workoutTime) ? 
                    new TimeSpan(17, 0, 0) : TimeSpan.Parse(workoutTime);
                
                var scheduledDateTime = startDate.Date.Add(timeOfDay);

                // Create the workout schedule
                var workoutSchedule = new WorkoutSchedule
                {
                    TemplateId = templateId,
                    TemplateAssignmentId = assignmentId,
                    ClientUserId = clientId,
                    CoachUserId = user.UserId,
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
                    workoutSchedule.IsRecurring = true;
                    workoutSchedule.RecurrencePattern = recurrencePattern;
                    
                    // For weekly or bi-weekly recurrence, set the day of week
                    if ((recurrencePattern == "Weekly" || recurrencePattern == "BiWeekly") && daysOfWeek != null && daysOfWeek.Any())
                    {
                        // Use the first selected day
                        DayOfWeek day = Enum.Parse<DayOfWeek>(daysOfWeek.First());
                        workoutSchedule.RecurrenceDayOfWeek = (int)day;
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

                _context.WorkoutSchedules.Add(workoutSchedule);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Workout scheduled successfully!";
                return RedirectToPage(new { ClientId = ClientId ?? clientId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling workout for client {ClientId} by coach {CoachId}", 
                    clientId, user.UserId);
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToPage(new { ClientId });
            }
        }

        private ScheduleViewModel CreateScheduleViewModel(WorkoutSchedule schedule)
        {
            return new ScheduleViewModel
            {
                WorkoutScheduleId = schedule.WorkoutScheduleId,
                ClientUserId = schedule.ClientUserId,
                ClientName = schedule.Client?.Name,
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
                SendReminder = schedule.SendReminder,
                ReminderHoursBefore = schedule.ReminderHoursBefore,
                IsActive = schedule.IsActive
            };
        }

        public class ScheduleViewModel
        {
            public int WorkoutScheduleId { get; set; }
            public int ClientUserId { get; set; }
            public string ClientName { get; set; }
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
            public bool SendReminder { get; set; }
            public int ReminderHoursBefore { get; set; }
            public bool IsActive { get; set; }
        }
    }
}