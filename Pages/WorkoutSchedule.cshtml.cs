using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;

namespace WorkoutTrackerWeb.Pages
{
    [Authorize]
    public class WorkoutScheduleModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ICoachingService _coachingService;

        public WorkoutScheduleModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ICoachingService coachingService)
        {
            _context = context;
            _userManager = userManager;
            _coachingService = coachingService;
        }

        [BindProperty(SupportsGet = true)]
        public int? TemplateId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string TemplateName { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? AssignmentId { get; set; }

        [BindProperty]
        public ScheduleWorkoutModel ScheduleData { get; set; } = new ScheduleWorkoutModel();

        public List<WorkoutTemplate> AvailableTemplates { get; set; } = new List<WorkoutTemplate>();
        public List<TemplateAssignment> AvailableAssignments { get; set; } = new List<TemplateAssignment>();

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string StatusMessageType { get; set; } = "Success";

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            // Load available templates
            AvailableTemplates = await _context.WorkoutTemplate
                .Where(t => t.UserId == int.Parse(userId) || t.IsPublic)
                .OrderBy(t => t.Name)
                .ToListAsync();

            // Load available template assignments
            AvailableAssignments = await _context.TemplateAssignments
                .Include(a => a.WorkoutTemplate)
                .Where(a => a.ClientUserId == int.Parse(userId) && a.IsActive)
                .OrderBy(a => a.WorkoutTemplate.Name)
                .ToListAsync();

            // Pre-select template or assignment if specified in query parameters
            if (TemplateId.HasValue)
            {
                ScheduleData.TemplateId = TemplateId.Value;
                
                // Set default name if template is found
                var template = AvailableTemplates.FirstOrDefault(t => t.WorkoutTemplateId == TemplateId.Value);
                if (template != null)
                {
                    ScheduleData.ScheduleName = $"{template.Name} - {DateTime.Now:MMM d}";
                }
                else if (!string.IsNullOrEmpty(TemplateName))
                {
                    ScheduleData.ScheduleName = $"{TemplateName} - {DateTime.Now:MMM d}";
                }
            }
            else if (AssignmentId.HasValue)
            {
                ScheduleData.AssignmentId = AssignmentId.Value;
                
                // Set default name if assignment is found
                var assignment = AvailableAssignments.FirstOrDefault(a => a.TemplateAssignmentId == AssignmentId.Value);
                if (assignment != null && assignment.WorkoutTemplate != null)
                {
                    ScheduleData.ScheduleName = $"{assignment.WorkoutTemplate.Name} - {DateTime.Now:MMM d}";
                }
            }

            // Set default date to today
            ScheduleData.ScheduleDate = DateTime.Today;
            ScheduleData.ScheduleTime = DateTime.Now.TimeOfDay;
            ScheduleData.SendReminder = true;
            ScheduleData.ReminderHoursBefore = 24;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                await LoadTemplatesAndAssignments(userId);
                return Page();
            }

            try
            {
                // Determine if we're using a template or an assignment
                WorkoutTemplate template = null;
                TemplateAssignment assignment = null;

                if (ScheduleData.TemplateId.HasValue)
                {
                    template = await _context.WorkoutTemplate
                        .FirstOrDefaultAsync(t => t.WorkoutTemplateId == ScheduleData.TemplateId.Value && 
                                                 (t.UserId == int.Parse(userId) || t.IsPublic));
                    
                    if (template == null)
                    {
                        ModelState.AddModelError("", "Selected template not found or not accessible");
                        await LoadTemplatesAndAssignments(userId);
                        return Page();
                    }
                }
                else if (ScheduleData.AssignmentId.HasValue)
                {
                    assignment = await _context.TemplateAssignments
                        .Include(a => a.WorkoutTemplate)
                        .FirstOrDefaultAsync(a => a.TemplateAssignmentId == ScheduleData.AssignmentId.Value && 
                                                 a.ClientUserId == int.Parse(userId) && a.IsActive);
                    
                    if (assignment == null)
                    {
                        ModelState.AddModelError("", "Selected assignment not found or not accessible");
                        await LoadTemplatesAndAssignments(userId);
                        return Page();
                    }
                    
                    template = assignment.WorkoutTemplate;
                }
                else
                {
                    ModelState.AddModelError("", "Please select a template or assignment");
                    await LoadTemplatesAndAssignments(userId);
                    return Page();
                }

                // Combine date and time
                var scheduleDateTime = ScheduleData.ScheduleDate.Date.Add(ScheduleData.ScheduleTime);

                // Create the workout schedule
                var workoutSchedule = new Models.Coaching.WorkoutSchedule
                {
                    ClientUserId = int.Parse(userId),
                    // Make sure CoachUserId is not assigned null
                    CoachUserId = assignment?.CoachUserId ?? int.Parse(userId), // Default to client if no coach
                    // Use null if template is null, otherwise use its WorkoutTemplateId
                    TemplateId = template == null ? null : (int?)template.WorkoutTemplateId,
                    TemplateAssignmentId = assignment?.TemplateAssignmentId,
                    Name = ScheduleData.ScheduleName,
                    Description = ScheduleData.Description,
                    StartDate = scheduleDateTime,
                    IsRecurring = ScheduleData.RecurrenceType != "none",
                    RecurrencePattern = ScheduleData.RecurrenceType,
                    RecurrenceDayOfWeek = ScheduleData.SelectedDaysOfWeek?.FirstOrDefault() != null ? 
                        (int?)ScheduleData.SelectedDaysOfWeek.FirstOrDefault() : null,
                    RecurrenceDayOfMonth = ScheduleData.RecurrenceDayOfMonth,
                    EndDate = ScheduleData.RecurrenceEndDate,
                    SendReminder = ScheduleData.SendReminder,
                    ReminderHoursBefore = ScheduleData.ReminderHoursBefore,
                    IsActive = true,
                    ScheduledDateTime = scheduleDateTime
                };

                _context.WorkoutSchedules.Add(workoutSchedule);
                await _context.SaveChangesAsync();

                StatusMessage = "Workout scheduled successfully!";
                StatusMessageType = "Success";
                
                return RedirectToPage("/Workouts/Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error scheduling workout: {ex.Message}");
                await LoadTemplatesAndAssignments(userId);
                return Page();
            }
        }

        private async Task LoadTemplatesAndAssignments(string userId)
        {
            // Load available templates
            AvailableTemplates = await _context.WorkoutTemplate
                .Where(t => t.UserId == int.Parse(userId) || t.IsPublic)
                .OrderBy(t => t.Name)
                .ToListAsync();

            // Load available template assignments
            AvailableAssignments = await _context.TemplateAssignments
                .Include(a => a.WorkoutTemplate)
                .Where(a => a.ClientUserId == int.Parse(userId) && a.IsActive)
                .OrderBy(a => a.WorkoutTemplate.Name)
                .ToListAsync();
        }
    }

    public class ScheduleWorkoutModel
    {
        [Display(Name = "Template")]
        public int? TemplateId { get; set; }
        
        [Display(Name = "Assignment")]
        public int? AssignmentId { get; set; }
        
        [Required]
        [Display(Name = "Schedule Name")]
        [StringLength(100, MinimumLength = 3)]
        public string ScheduleName { get; set; }
        
        [Display(Name = "Description")]
        [StringLength(500)]
        public string Description { get; set; }
        
        [Required]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime ScheduleDate { get; set; }
        
        [Required]
        [Display(Name = "Time")]
        [DataType(DataType.Time)]
        public TimeSpan ScheduleTime { get; set; }
        
        [Display(Name = "Recurrence Type")]
        public string RecurrenceType { get; set; } = "none";
        
        [Display(Name = "Days of Week")]
        public List<DayOfWeek> SelectedDaysOfWeek { get; set; }
        
        [Display(Name = "Day of Month")]
        public int? RecurrenceDayOfMonth { get; set; }
        
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? RecurrenceEndDate { get; set; }
        
        [Display(Name = "Send Reminder")]
        public bool SendReminder { get; set; } = true;
        
        [Display(Name = "Reminder Hours Before")]
        [Range(1, 72)]
        public int ReminderHoursBefore { get; set; } = 24;
    }
}