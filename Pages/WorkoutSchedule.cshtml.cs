using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages
{
    [Authorize]
    public class WorkoutScheduleModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<WorkoutScheduleModel> _logger;
        private readonly UserService _userService;

        public WorkoutScheduleModel(
            WorkoutTrackerWebContext context,
            ILogger<WorkoutScheduleModel> logger,
            UserService userService)
        {
            _context = context;
            _logger = logger;
            _userService = userService;
        }

        [BindProperty]
        public WorkoutScheduleViewModel ScheduleData { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? TemplateId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string TemplateName { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? AssignmentId { get; set; }

        public List<WorkoutTemplate> AvailableTemplates { get; set; }
        public List<TemplateAssignment> AvailableAssignments { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized();
            }

            // Load available templates (both user-owned and assigned by coaches)
            AvailableTemplates = await _context.WorkoutTemplate
                .Where(t => t.UserId == userId || t.IsPublic)
                .OrderBy(t => t.Name)
                .ToListAsync();

            // Load available template assignments from coaches
            AvailableAssignments = await _context.TemplateAssignments
                .Where(a => a.ClientUserId == userId && a.IsActive)
                .Include(a => a.WorkoutTemplate)
                .OrderBy(a => a.Name)
                .ToListAsync();

            // Initialize the form model
            ScheduleData = new WorkoutScheduleViewModel
            {
                ScheduleDate = DateTime.Today,
                ScheduleTime = DateTime.Now.TimeOfDay,
                SendReminder = true,
                ReminderHoursBefore = 3,
                RecurrenceType = "none"
            };

            // If template or assignment ID was provided, pre-select it in the form
            if (TemplateId.HasValue)
            {
                var template = await _context.WorkoutTemplate.FindAsync(TemplateId.Value);
                if (template != null)
                {
                    ScheduleData.TemplateId = TemplateId.Value;
                    ScheduleData.ScheduleName = template.Name;
                }
            }
            else if (AssignmentId.HasValue)
            {
                var assignment = await _context.TemplateAssignments.FindAsync(AssignmentId.Value);
                if (assignment != null)
                {
                    ScheduleData.AssignmentId = AssignmentId.Value;
                    ScheduleData.ScheduleName = assignment.Name;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync(); // Reload the form data
                return Page();
            }

            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                // Determine if this is a self-scheduled workout or a coach-assigned workout
                if (ScheduleData.TemplateId.HasValue)
                {
                    // Self-scheduled workout (user is scheduling their own workout from a template)
                    return await CreateSelfScheduledWorkout(userId.Value);
                }
                else if (ScheduleData.AssignmentId.HasValue)
                {
                    // Coach-assigned workout (user is scheduling from a template assigned by a coach)
                    return await CreateAssignmentWorkout(userId.Value);
                }
                else
                {
                    ModelState.AddModelError("", "Please select a template or assignment.");
                    await OnGetAsync(); // Reload the form data
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating workout schedule for user {userId}");
                ModelState.AddModelError("", $"Error creating schedule: {ex.Message}");
                await OnGetAsync(); // Reload the form data
                return Page();
            }
        }

        private async Task<IActionResult> CreateSelfScheduledWorkout(int userId)
        {
            // Validate template exists and belongs to the user
            var template = await _context.WorkoutTemplate
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == ScheduleData.TemplateId && 
                                        (t.UserId == userId || t.IsPublic));

            if (template == null)
            {
                ModelState.AddModelError("", "Template not found or you don't have access to it");
                await OnGetAsync(); // Reload the form data
                return Page();
            }

            // Parse schedule date and time
            var scheduledDateTime = ScheduleData.ScheduleDate.Add(ScheduleData.ScheduleTime);

            // Create the workout schedule
            var workoutSchedule = new WorkoutSchedule
            {
                ClientUserId = userId,
                CoachUserId = userId, // Self-scheduling, so the user is both client and coach
                Name = string.IsNullOrEmpty(ScheduleData.ScheduleName) ? template.Name : ScheduleData.ScheduleName,
                Description = ScheduleData.Description ?? string.Empty,
                StartDate = ScheduleData.ScheduleDate,
                ScheduledDateTime = scheduledDateTime,
                IsActive = true
            };

            // Set template information
            workoutSchedule.TemplateId = template.WorkoutTemplateId;

            // Handle recurrence pattern
            ConfigureRecurrencePattern(workoutSchedule);

            // Add reminder settings
            workoutSchedule.SendReminder = ScheduleData.SendReminder;
            workoutSchedule.ReminderHoursBefore = ScheduleData.ReminderHoursBefore;

            _context.WorkoutSchedules.Add(workoutSchedule);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} created self-scheduled workout {workoutSchedule.WorkoutScheduleId}");

            TempData["SuccessMessage"] = "Workout schedule created successfully!";
            return RedirectToPage("/Workouts/Calendar");
        }

        private async Task<IActionResult> CreateAssignmentWorkout(int userId)
        {
            // Validate template assignment exists and belongs to the user
            var assignment = await _context.TemplateAssignments
                .FirstOrDefaultAsync(a => a.TemplateAssignmentId == ScheduleData.AssignmentId && 
                                       a.ClientUserId == userId && 
                                       a.IsActive);

            if (assignment == null)
            {
                ModelState.AddModelError("", "Template assignment not found or inactive");
                await OnGetAsync(); // Reload the form data
                return Page();
            }

            // Parse schedule date and time
            var scheduledDateTime = ScheduleData.ScheduleDate.Add(ScheduleData.ScheduleTime);

            // Create the workout schedule
            var workoutSchedule = new WorkoutSchedule
            {
                TemplateAssignmentId = ScheduleData.AssignmentId,
                ClientUserId = userId,
                CoachUserId = assignment.CoachUserId,
                Name = string.IsNullOrEmpty(ScheduleData.ScheduleName) ? assignment.Name : ScheduleData.ScheduleName,
                Description = ScheduleData.Description ?? string.Empty,
                StartDate = ScheduleData.ScheduleDate,
                ScheduledDateTime = scheduledDateTime,
                IsActive = true
            };

            // Handle recurrence pattern
            ConfigureRecurrencePattern(workoutSchedule);

            // Add reminder settings
            workoutSchedule.SendReminder = ScheduleData.SendReminder;
            workoutSchedule.ReminderHoursBefore = ScheduleData.ReminderHoursBefore;

            _context.WorkoutSchedules.Add(workoutSchedule);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} created assignment workout schedule {workoutSchedule.WorkoutScheduleId}");

            TempData["SuccessMessage"] = "Workout schedule created successfully!";
            return RedirectToPage("/Workouts/Calendar");
        }

        private void ConfigureRecurrencePattern(WorkoutSchedule workoutSchedule)
        {
            if (!string.IsNullOrEmpty(ScheduleData.RecurrenceType) && ScheduleData.RecurrenceType != "none")
            {
                workoutSchedule.IsRecurring = true;
                workoutSchedule.RecurrencePattern = ScheduleData.RecurrenceType;
                
                // Set recurrence end date if provided
                if (ScheduleData.RecurrenceEndDate.HasValue)
                {
                    workoutSchedule.EndDate = ScheduleData.RecurrenceEndDate;
                }

                // For weekly recurrence, set the day of week
                if (ScheduleData.RecurrenceType == "weekly" || ScheduleData.RecurrenceType == "biweekly")
                {
                    workoutSchedule.RecurrenceDayOfWeek = (int)ScheduleData.ScheduleDate.DayOfWeek;
                    
                    // Handle specific days of week selection if provided
                    if (ScheduleData.SelectedDaysOfWeek != null && ScheduleData.SelectedDaysOfWeek.Any())
                    {
                        workoutSchedule.RecurrenceDayOfWeek = (int)Enum.Parse<DayOfWeek>(ScheduleData.SelectedDaysOfWeek.First());
                    }
                }
                // For monthly recurrence, set the day of month
                else if (ScheduleData.RecurrenceType == "monthly")
                {
                    workoutSchedule.RecurrenceDayOfMonth = ScheduleData.ScheduleDate.Day;
                    
                    // Handle specific day of month if provided
                    if (ScheduleData.RecurrenceDayOfMonth.HasValue)
                    {
                        workoutSchedule.RecurrenceDayOfMonth = ScheduleData.RecurrenceDayOfMonth.Value;
                    }
                }
            }
        }
    }

    public class WorkoutScheduleViewModel
    {
        public int? TemplateId { get; set; }
        public int? AssignmentId { get; set; }
        public string ScheduleName { get; set; }
        public string Description { get; set; }
        public DateTime ScheduleDate { get; set; }
        public TimeSpan ScheduleTime { get; set; }
        public string RecurrenceType { get; set; }
        public DateTime? RecurrenceEndDate { get; set; }
        public List<string> SelectedDaysOfWeek { get; set; }
        public int? RecurrenceDayOfMonth { get; set; }
        public bool SendReminder { get; set; }
        public int ReminderHoursBefore { get; set; }
    }
}