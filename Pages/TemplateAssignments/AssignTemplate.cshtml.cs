using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;

namespace WorkoutTrackerWeb.Pages.TemplateAssignments
{
    [Authorize]
    public class AssignTemplateModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<AssignTemplateModel> _logger;

        public AssignTemplateModel(
            WorkoutTrackerWebContext context,
            ILogger<AssignTemplateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int TemplateId { get; set; }

        [BindProperty]
        public TemplateAssignmentInputModel Assignment { get; set; } = new();

        [BindProperty]
        public bool ScheduleWorkouts { get; set; }

        public WorkoutTemplate Template { get; set; }
        public int ExerciseCount { get; set; }
        public SelectList ClientList { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string StatusMessageType { get; set; } = "success";

        public async Task<IActionResult> OnGetAsync()
        {
            // Get the current user
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            // Verify the template exists and user has access to it
            Template = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises)
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == TemplateId && 
                                         (t.UserId == user.UserId || t.IsPublic));

            if (Template == null)
            {
                return NotFound();
            }

            ExerciseCount = Template.TemplateExercises.Count;

            // If user is a coach, get clients
            if (User.IsInRole("Coach"))
            {
                var clients = await _context.CoachClientRelationships
                    .Where(r => r.CoachId == user.UserId.ToString() && r.Status == RelationshipStatus.Active)
                    .Join(_context.User,
                        r => r.ClientId,
                        u => u.UserId.ToString(),
                        (r, u) => new { UserId = u.UserId, Name = u.Name })
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                ClientList = new SelectList(clients, "UserId", "Name");
            }

            // Set default assignment name
            Assignment.Name = Template.Name;
            Assignment.StartDate = DateTime.Now;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Reload template data
                await LoadTemplate();
                return Page();
            }

            // Get the current user
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            // Check if user is a coach
            if (!User.IsInRole("Coach"))
            {
                StatusMessage = "Only coaches can assign templates to clients.";
                StatusMessageType = "danger";
                await LoadTemplate();
                return Page();
            }

            // Verify the template exists and user has access to it
            var template = await _context.WorkoutTemplate
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == TemplateId && 
                                         (t.UserId == user.UserId || t.IsPublic));

            if (template == null)
            {
                StatusMessage = "Template not found or you don't have access to it.";
                StatusMessageType = "danger";
                await LoadTemplate();
                return Page();
            }

            // Verify the client exists and is a client of the coach
            var clientId = Assignment.ClientId;
            var relationship = await _context.CoachClientRelationships
                .FirstOrDefaultAsync(r => r.CoachId == user.UserId.ToString() && 
                                         r.ClientId == clientId.ToString() && 
                                         r.Status == RelationshipStatus.Active);

            if (relationship == null)
            {
                StatusMessage = "Client relationship not found or inactive.";
                StatusMessageType = "danger";
                await LoadTemplate();
                return Page();
            }

            // Begin transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create template assignment
                var assignment = new TemplateAssignment
                {
                    WorkoutTemplateId = TemplateId,
                    ClientUserId = clientId,
                    CoachUserId = user.UserId,
                    Name = Assignment.Name,
                    Notes = Assignment.Notes,
                    StartDate = Assignment.StartDate,
                    EndDate = Assignment.EndDate,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.TemplateAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                // Schedule workouts if requested
                if (ScheduleWorkouts)
                {
                    // Parse workout time
                    var workoutTime = Request.Form["WorkoutTime"].ToString() ?? "17:00";
                    TimeSpan workoutTimeOfDay = TimeSpan.Parse(workoutTime);

                    // Create workout schedule
                    var schedule = new WorkoutSchedule
                    {
                        TemplateAssignmentId = assignment.TemplateAssignmentId,
                        ClientUserId = clientId,
                        CoachUserId = user.UserId,
                        Name = Assignment.Name,
                        Description = Assignment.Notes,
                        StartDate = Assignment.StartDate,
                        EndDate = Assignment.EndDate,
                        ScheduledDateTime = Assignment.StartDate.Date.Add(workoutTimeOfDay),
                        IsRecurring = Request.Form["RecurrencePattern"] != "Once",
                        RecurrencePattern = Request.Form["RecurrencePattern"],
                        SendReminder = Request.Form["SendReminder"] == "true",
                        ReminderHoursBefore = int.Parse(Request.Form["ReminderHoursBefore"].ToString() ?? "3"),
                        IsActive = true
                    };

                    // Set recurrence specifics
                    if (schedule.RecurrencePattern == "Weekly" && Request.Form["DaysOfWeek"].Count > 0)
                    {
                        // Parse the day of week string to the enum and then to int
                        DayOfWeek dayOfWeek = Enum.Parse<DayOfWeek>(Request.Form["DaysOfWeek"]);
                        schedule.RecurrenceDayOfWeek = (int)dayOfWeek;
                    }
                    else if (schedule.RecurrencePattern == "Monthly" && Request.Form["DayOfMonth"].Count > 0)
                    {
                        schedule.RecurrenceDayOfMonth = int.Parse(Request.Form["DayOfMonth"]);
                    }

                    _context.WorkoutSchedules.Add(schedule);
                    await _context.SaveChangesAsync();
                }

                // Commit the transaction
                await transaction.CommitAsync();

                StatusMessage = $"Template '{template.Name}' successfully assigned to client.";
                StatusMessageType = "success";
                return RedirectToPage("/Coach/Clients/AssignedWorkouts", new { ClientId = clientId, area = "Coach" });
            }
            catch (Exception ex)
            {
                // Roll back the transaction on error
                await transaction.RollbackAsync();

                _logger.LogError(ex, "Error assigning template {TemplateId} to client {ClientId}", TemplateId, clientId);
                
                StatusMessage = $"An error occurred: {ex.Message}";
                StatusMessageType = "danger";
                
                await LoadTemplate();
                return Page();
            }
        }

        private async Task LoadTemplate()
        {
            // Get the current user
            var user = await _context.GetCurrentUserAsync();
            
            // Load template
            Template = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises)
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == TemplateId && 
                                        (user != null && (t.UserId == user.UserId || t.IsPublic)));

            ExerciseCount = Template?.TemplateExercises.Count ?? 0;

            // If user is a coach, get clients
            if (User.IsInRole("Coach") && user != null)
            {
                var clients = await _context.CoachClientRelationships
                    .Where(r => r.CoachId == user.UserId.ToString() && r.Status == RelationshipStatus.Active)
                    .Join(_context.User,
                        r => r.ClientId,
                        u => u.UserId.ToString(),
                        (r, u) => new { UserId = u.UserId, Name = u.Name })
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                ClientList = new SelectList(clients, "UserId", "Name");
            }
        }

        public class TemplateAssignmentInputModel
        {
            [Required(ErrorMessage = "Client is required")]
            [Display(Name = "Client")]
            public int ClientId { get; set; }

            [Required(ErrorMessage = "Assignment name is required")]
            [StringLength(100, ErrorMessage = "Name cannot be longer than 100 characters")]
            [Display(Name = "Assignment Name")]
            public string Name { get; set; }

            [StringLength(500, ErrorMessage = "Notes cannot be longer than 500 characters")]
            [Display(Name = "Notes")]
            public string Notes { get; set; }

            [Required(ErrorMessage = "Start date is required")]
            [Display(Name = "Start Date")]
            [DataType(DataType.Date)]
            public DateTime StartDate { get; set; } = DateTime.Now;

            [Display(Name = "End Date")]
            [DataType(DataType.Date)]
            public DateTime? EndDate { get; set; }
        }
    }
}