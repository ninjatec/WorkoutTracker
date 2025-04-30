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
using WorkoutTrackerWeb.Models.Filters;
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

        [BindProperty(SupportsGet = true)]
        public TemplateFilterModel Filter { get; set; } = new TemplateFilterModel();

        public List<WorkoutTemplateViewModel> WorkoutTemplates { get; set; } = new List<WorkoutTemplateViewModel>();
        public List<ClientViewModel> Clients { get; set; } = new List<ClientViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Get the current user
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            // Load categories for filter dropdown
            await Filter.LoadCategoriesAsync(_context, user.UserId);

            // Create initial query for templates
            var templatesQuery = _context.WorkoutTemplate.AsQueryable();
            
            // Apply standardized filters
            templatesQuery = Filter.ApplyFilters(templatesQuery, user.UserId);

            // Load the templates with exercise counts
            WorkoutTemplates = await templatesQuery
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
                    IsOwner = t.UserId == user.UserId,
                    ExerciseCount = _context.WorkoutTemplateExercise
                        .Count(e => e.WorkoutTemplateId == t.WorkoutTemplateId)
                })
                .OrderByDescending(t => t.LastModifiedDate)
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

        public async Task<IActionResult> OnGetFilterTemplatesAsync()
        {
            // Get the current user
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return new UnauthorizedResult();
            }

            // Create initial query for templates
            var templatesQuery = _context.WorkoutTemplate.AsQueryable();
            
            // Apply standardized filters
            templatesQuery = Filter.ApplyFilters(templatesQuery, user.UserId);

            // Get filtered templates
            var templates = await templatesQuery
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
                    IsOwner = t.UserId == user.UserId,
                    ExerciseCount = _context.WorkoutTemplateExercise
                        .Count(e => e.WorkoutTemplateId == t.WorkoutTemplateId)
                })
                .OrderByDescending(t => t.LastModifiedDate)
                .ToListAsync();

            return new JsonResult(templates);
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

        public async Task<IActionResult> OnPostCloneAsync(int templateId, string name, bool makePublic = false)
        {
            // Get the current user
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "A name is required for the cloned template.";
                return RedirectToPage();
            }

            // Verify the template exists
            var sourceTemplate = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.TemplateSets)
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId && 
                                         (t.UserId == user.UserId || t.IsPublic));
                
            if (sourceTemplate == null)
            {
                TempData["ErrorMessage"] = "Template not found or you don't have access to it.";
                return RedirectToPage();
            }

            // Begin transaction for cloning
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Clone the template
                var clonedTemplate = new WorkoutTemplate
                {
                    Name = name,
                    Description = sourceTemplate.Description,
                    Category = sourceTemplate.Category,
                    Tags = sourceTemplate.Tags,
                    IsPublic = makePublic,
                    UserId = user.UserId,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now
                };

                _context.WorkoutTemplate.Add(clonedTemplate);
                await _context.SaveChangesAsync();

                // Clone exercises
                foreach (var sourceExercise in sourceTemplate.TemplateExercises.OrderBy(e => e.SequenceNum))
                {
                    var clonedExercise = new WorkoutTemplateExercise
                    {
                        WorkoutTemplateId = clonedTemplate.WorkoutTemplateId,
                        ExerciseTypeId = sourceExercise.ExerciseTypeId,
                        SequenceNum = sourceExercise.SequenceNum,
                        Notes = sourceExercise.Notes
                    };

                    _context.WorkoutTemplateExercise.Add(clonedExercise);
                    await _context.SaveChangesAsync();

                    // Clone sets for this exercise
                    foreach (var sourceSet in sourceExercise.TemplateSets.OrderBy(s => s.SequenceNum))
                    {
                        var clonedSet = new WorkoutTemplateSet
                        {
                            WorkoutTemplateExerciseId = clonedExercise.WorkoutTemplateExerciseId,
                            SettypeId = sourceSet.SettypeId,
                            DefaultReps = sourceSet.DefaultReps,
                            DefaultWeight = sourceSet.DefaultWeight,
                            SequenceNum = sourceSet.SequenceNum,
                            Description = sourceSet.Description,
                            Notes = sourceSet.Notes
                        };

                        _context.WorkoutTemplateSet.Add(clonedSet);
                    }
                    await _context.SaveChangesAsync();
                }

                // Commit the transaction
                await transaction.CommitAsync();
                
                TempData["SuccessMessage"] = $"Template '{sourceTemplate.Name}' successfully cloned as '{name}'.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                // Roll back the transaction on error
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"An error occurred while cloning the template: {ex.Message}";
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
            public bool IsOwner { get; set; }
            public int ExerciseCount { get; set; }
        }

        public class ClientViewModel
        {
            public int UserId { get; set; }
            public string Name { get; set; }
        }
    }
}