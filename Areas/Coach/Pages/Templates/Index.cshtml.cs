using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Extensions;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Templates
{
    [CoachAuthorize]
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;

        public IndexModel(WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public List<WorkoutTemplateViewModel> WorkoutTemplates { get; set; } = new List<WorkoutTemplateViewModel>();
        public List<ClientViewModel> Clients { get; set; } = new List<ClientViewModel>();
        public List<string> Categories { get; set; } = new List<string>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Get the current user
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            // Load the user's templates with exercise counts
            var templates = await _context.WorkoutTemplate
                .Where(t => t.UserId == user.UserId)
                .Select(t => new WorkoutTemplateViewModel
                {
                    WorkoutTemplateId = t.WorkoutTemplateId,
                    Name = t.Name,
                    Description = t.Description,
                    Category = t.Category,
                    Tags = t.Tags,
                    CreatedDate = t.CreatedDate,
                    LastModifiedDate = t.LastModifiedDate,
                    IsPublic = t.IsPublic,
                    ExerciseCount = _context.WorkoutTemplateExercise
                        .Count(e => e.WorkoutTemplateId == t.WorkoutTemplateId)
                })
                .OrderByDescending(t => t.LastModifiedDate)
                .ToListAsync();

            // Also include public templates from other coaches
            var publicTemplates = await _context.WorkoutTemplate
                .Where(t => t.UserId != user.UserId && t.IsPublic)
                .Select(t => new WorkoutTemplateViewModel
                {
                    WorkoutTemplateId = t.WorkoutTemplateId,
                    Name = t.Name,
                    Description = t.Description,
                    Category = t.Category,
                    Tags = t.Tags,
                    CreatedDate = t.CreatedDate,
                    LastModifiedDate = t.LastModifiedDate,
                    IsPublic = t.IsPublic,
                    ExerciseCount = _context.WorkoutTemplateExercise
                        .Count(e => e.WorkoutTemplateId == t.WorkoutTemplateId)
                })
                .OrderByDescending(t => t.LastModifiedDate)
                .ToListAsync();

            // Add to the overall templates list (user's templates first)
            WorkoutTemplates = templates;
            // Public templates will be shown/hidden via client-side filtering

            // Get unique categories for filtering
            Categories = await _context.WorkoutTemplate
                .Where(t => t.UserId == user.UserId || t.IsPublic)
                .Where(t => !string.IsNullOrEmpty(t.Category))
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Get clients for the assignment modal
            var relationships = await _context.CoachClientRelationships
                .Where(r => r.CoachId == user.UserId.ToString() && r.Status == RelationshipStatus.Active)
                .Include(r => r.Client)
                .ToListAsync();

            Clients = relationships
                .Select(r => new ClientViewModel
                {
                    UserId = int.Parse(r.ClientId),
                    Name = r.Client.FullName()
                })
                .OrderBy(c => c.Name)
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAssignAsync(int templateId, int clientId, string name, string notes, 
                                                         DateTime startDate, DateTime? endDate, 
                                                         bool scheduleWorkouts = false, string recurrencePattern = "Once", 
                                                         List<string> daysOfWeek = null, int? dayOfMonth = null,
                                                         string workoutTime = null, bool sendReminder = false, 
                                                         int reminderHoursBefore = 3)
        {
            // Get the current user
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            // Verify the template exists
            var template = await _context.WorkoutTemplate
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId);
                
            if (template == null)
            {
                TempData["ErrorMessage"] = "Template not found.";
                return RedirectToPage();
            }

            // Verify the client exists and is a client of the coach
            var relationship = await _context.CoachClientRelationships
                .FirstOrDefaultAsync(r => r.CoachId == user.UserId.ToString() && r.ClientId == clientId.ToString() && r.Status == RelationshipStatus.Active);
                
            if (relationship == null)
            {
                TempData["ErrorMessage"] = "Client relationship not found or inactive.";
                return RedirectToPage();
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
                    CoachUserId = user.UserId,
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
                        ClientUserId = clientId,
                        CoachUserId = user.UserId,
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
                    if (recurrencePattern == "Weekly" && daysOfWeek != null && daysOfWeek.Any())
                    {
                        // Parse the day of week string to the enum and store as int
                        DayOfWeek day = Enum.Parse<DayOfWeek>(daysOfWeek.First());
                        schedule.RecurrenceDayOfWeek = (int)day;
                    }
                    else if (recurrencePattern == "Monthly" && dayOfMonth.HasValue)
                    {
                        schedule.RecurrenceDayOfMonth = dayOfMonth.Value;
                    }

                    _context.WorkoutSchedules.Add(schedule);
                    await _context.SaveChangesAsync();
                }

                // Commit the transaction
                await transaction.CommitAsync();
                
                TempData["SuccessMessage"] = $"Template '{template.Name}' successfully assigned to client.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                // Roll back the transaction on error
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToPage();
            }
        }

        // View models for the page
        public class WorkoutTemplateViewModel
        {
            public int WorkoutTemplateId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public string Tags { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime LastModifiedDate { get; set; }
            public bool IsPublic { get; set; }
            public int ExerciseCount { get; set; }
        }

        public class ClientViewModel
        {
            public int UserId { get; set; }
            public string Name { get; set; }
        }
    }
}