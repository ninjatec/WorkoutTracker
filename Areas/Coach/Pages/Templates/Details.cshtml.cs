using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Extensions;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.ViewModels.Coaching;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Templates
{
    [Area("Coach")]
    [CoachAuthorize]
    [OutputCache(Duration = 60, VaryByQueryKeys = new[] { "id" })]
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ILogger<DetailsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public WorkoutTemplate WorkoutTemplate { get; set; } = default!;
        public List<ClientViewModel> Clients { get; set; } = new List<ClientViewModel>();
        public List<TemplateAssignmentViewModel> RecentAssignments { get; set; } = new List<TemplateAssignmentViewModel>();

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string StatusMessageType { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }
            
            // Add a cache vary header based on the coach ID to ensure each coach gets a unique cache entry
            Response.Headers.Add("Cache-Vary-By-User", coachId);

            var workoutTemplate = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises.OrderBy(e => e.SequenceNum))
                .ThenInclude(e => e.ExerciseType)
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.TemplateSets.OrderBy(s => s.SequenceNum))
                .ThenInclude(s => s.Settype)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == id);

            if (workoutTemplate == null)
            {
                return NotFound();
            }

            // Check if the template belongs to this coach or is public
            if (workoutTemplate.UserId != int.Parse(coachId) && !workoutTemplate.IsPublic)
            {
                return Forbid();
            }

            WorkoutTemplate = workoutTemplate;

            // Get clients for this coach
            var relationships = await _context.CoachClientRelationships
                .Where(r => r.CoachId == coachId && r.Status == RelationshipStatus.Active)
                .Include(r => r.Client)
                .ToListAsync();

            Clients = relationships
                .Select(r => new ClientViewModel
                {
                    UserId = int.Parse(r.ClientId),
                    Name = r.Client.GetDisplayName() // Using GetDisplayName() extension method
                })
                .OrderBy(c => c.Name)
                .ToList();

            // Load recent assignments for this template
            RecentAssignments = await _context.TemplateAssignments
                .Where(a => a.WorkoutTemplateId == id)
                .OrderByDescending(a => a.AssignedDate)
                .Take(5)
                .Select(a => new TemplateAssignmentViewModel
                {
                    TemplateAssignmentId = a.TemplateAssignmentId,
                    ClientName = a.Client.GetDisplayName(), // Using GetDisplayName() extension method
                    AssignedDate = a.AssignedDate,
                    IsActive = a.IsActive
                })
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostScheduleWorkoutAsync(int templateId, int clientId, string name, string description, 
                                                             DateTime startDate, DateTime? endDate,
                                                             string recurrencePattern = "Once",
                                                             List<string> daysOfWeek = null, int? dayOfMonth = null,
                                                             string workoutTime = null, bool sendReminder = true,
                                                             int reminderHoursBefore = 3)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Verify the template exists
            var template = await _context.WorkoutTemplate
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId && 
                                          (t.UserId == int.Parse(coachId) || t.IsPublic));
                
            if (template == null)
            {
                StatusMessageType = "Error";
                StatusMessage = "Template not found.";
                return RedirectToPage(new { id = templateId });
            }

            // Verify the client-coach relationship
            var relationship = await _context.CoachClientRelationships
                .FirstOrDefaultAsync(r => r.CoachId == coachId && 
                                           r.ClientId == clientId.ToString() && 
                                           r.Status == RelationshipStatus.Active);
                
            if (relationship == null)
            {
                StatusMessageType = "Error";
                StatusMessage = "Client relationship not found or inactive.";
                return RedirectToPage(new { id = templateId });
            }

            try
            {
                // Parse workout time
                TimeSpan workoutTimeOfDay = TimeSpan.Parse(workoutTime ?? "17:00");
                var scheduledDateTime = startDate.Date.Add(workoutTimeOfDay);

                // Create the workout schedule
                var workoutSchedule = new WorkoutSchedule
                {
                    TemplateId = templateId,
                    ClientUserId = clientId,
                    CoachUserId = int.Parse(coachId),
                    Name = name,
                    Description = description,
                    StartDate = startDate,
                    EndDate = endDate,
                    ScheduledDateTime = scheduledDateTime,
                    IsActive = true,
                    SendReminder = sendReminder,
                    ReminderHoursBefore = reminderHoursBefore
                };

                // Handle recurrence pattern
                if (recurrencePattern != "Once")
                {
                    workoutSchedule.IsRecurring = true;
                    workoutSchedule.RecurrencePattern = recurrencePattern;
                    
                    // For weekly recurrence, set the day of week
                    if ((recurrencePattern == "Weekly" || recurrencePattern == "BiWeekly") && daysOfWeek != null && daysOfWeek.Any())
                    {
                        // Parse the day of week string to the enum and store as int
                        DayOfWeek day = Enum.Parse<DayOfWeek>(daysOfWeek.First());
                        workoutSchedule.RecurrenceDayOfWeek = (int)day;
                    }
                    else if (recurrencePattern == "Weekly" || recurrencePattern == "BiWeekly")
                    {
                        // Use the day of week from the start date
                        workoutSchedule.RecurrenceDayOfWeek = (int)startDate.DayOfWeek;
                    }
                    // For monthly recurrence, set the day of month
                    else if (recurrencePattern == "Monthly" && dayOfMonth.HasValue)
                    {
                        workoutSchedule.RecurrenceDayOfMonth = dayOfMonth.Value;
                    }
                    else if (recurrencePattern == "Monthly")
                    {
                        workoutSchedule.RecurrenceDayOfMonth = startDate.Day;
                    }
                }

                _context.WorkoutSchedules.Add(workoutSchedule);
                await _context.SaveChangesAsync();

                StatusMessageType = "Success";
                StatusMessage = "Workout scheduled successfully!";
                return RedirectToPage(new { id = templateId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling workout for template {TemplateId} for client {ClientId} by coach {CoachId}", 
                    templateId, clientId, coachId);
                StatusMessageType = "Error";
                StatusMessage = $"An error occurred: {ex.Message}";
                return RedirectToPage(new { id = templateId });
            }
        }

        public async Task<IActionResult> OnPostAssignAsync(int templateId, int clientId, string name, string notes, 
                                                     DateTime startDate, DateTime? endDate,
                                                     bool scheduleWorkouts = false, string recurrencePattern = "Once",
                                                     List<string> daysOfWeek = null, int? dayOfMonth = null,
                                                     string workoutTime = null, bool sendReminder = false,
                                                     int reminderHoursBefore = 3)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Verify the template exists
            var template = await _context.WorkoutTemplate
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId && 
                                         (t.UserId == int.Parse(coachId) || t.IsPublic));
                
            if (template == null)
            {
                StatusMessageType = "Error";
                StatusMessage = "Template not found.";
                return RedirectToPage("Index");
            }

            // Verify the client-coach relationship
            var relationship = await _context.CoachClientRelationships
                .FirstOrDefaultAsync(r => r.CoachId == coachId && 
                                           r.ClientId == clientId.ToString() && 
                                           r.Status == RelationshipStatus.Active);
                
            if (relationship == null)
            {
                StatusMessageType = "Error";
                StatusMessage = "Client relationship not found or inactive.";
                return RedirectToPage(new { id = templateId });
            }

            // Begin transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create template assignment
                var assignment = new TemplateAssignment
                {
                    WorkoutTemplateId = templateId,
                    ClientUserId = clientId,
                    CoachUserId = int.Parse(coachId),
                    Name = name,
                    Notes = notes,
                    StartDate = startDate,
                    EndDate = endDate,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.TemplateAssignments.Add(assignment);
                await _context.SaveChangesAsync();

                // Schedule workouts if requested
                if (scheduleWorkouts)
                {
                    // Parse workout time
                    TimeSpan workoutTimeOfDay = TimeSpan.Parse(workoutTime ?? "17:00");

                    // Create workout schedule
                    var schedule = new WorkoutSchedule
                    {
                        TemplateAssignmentId = assignment.TemplateAssignmentId,
                        TemplateId = templateId,
                        ClientUserId = clientId,
                        CoachUserId = int.Parse(coachId),
                        Name = name,
                        Description = notes,
                        StartDate = startDate,
                        EndDate = endDate,
                        ScheduledDateTime = startDate.Date.Add(workoutTimeOfDay),
                        IsRecurring = recurrencePattern != "Once",
                        RecurrencePattern = recurrencePattern,
                        SendReminder = sendReminder,
                        ReminderHoursBefore = reminderHoursBefore,
                        IsActive = true
                    };

                    // Set recurrence specifics based on pattern
                    if (recurrencePattern == "Weekly" || recurrencePattern == "BiWeekly")
                    {
                        if (daysOfWeek != null && daysOfWeek.Any())
                        {
                            // Parse the day of week string to the enum and store as int
                            DayOfWeek day = Enum.Parse<DayOfWeek>(daysOfWeek.First());
                            schedule.RecurrenceDayOfWeek = (int)day;
                        }
                        else
                        {
                            // Use the day of week from the start date
                            schedule.RecurrenceDayOfWeek = (int)startDate.DayOfWeek;
                        }
                    }
                    else if (recurrencePattern == "Monthly")
                    {
                        schedule.RecurrenceDayOfMonth = dayOfMonth ?? startDate.Day;
                    }

                    _context.WorkoutSchedules.Add(schedule);
                    await _context.SaveChangesAsync();
                }

                // Commit the transaction
                await transaction.CommitAsync();
                
                StatusMessageType = "Success";
                StatusMessage = $"Template '{template.Name}' successfully assigned to client.";
                return RedirectToPage(new { id = templateId });
            }
            catch (Exception ex)
            {
                // Roll back the transaction on error
                await transaction.RollbackAsync();
                
                _logger.LogError(ex, "Error assigning template {TemplateId} to client {ClientId} by coach {CoachId}", 
                    templateId, clientId, coachId);
                StatusMessageType = "Error";
                StatusMessage = $"An error occurred: {ex.Message}";
                return RedirectToPage(new { id = templateId });
            }
        }

        public class ClientViewModel
        {
            public int UserId { get; set; }
            public string Name { get; set; }
        }

        public class TemplateAssignmentViewModel
        {
            public int TemplateAssignmentId { get; set; }
            public string ClientName { get; set; }
            public DateTime AssignedDate { get; set; }
            public bool IsActive { get; set; }
        }
    }
}