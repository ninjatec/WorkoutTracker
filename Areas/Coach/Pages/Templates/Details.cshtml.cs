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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.ViewModels;
using WorkoutTrackerWeb.ViewModels.Coaching;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Templates
{
    [Area("Coach")]
    [CoachAuthorize]
    [OutputCache(PolicyName = "StaticContentWithId")]
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
        
        public List<User> Clients { get; set; } = new List<User>();
        public List<TemplateAssignmentViewModel> RecentAssignments { get; set; } = new List<TemplateAssignmentViewModel>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // For JSON/API requests, prevent response caching
            if (Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                Response.Headers["Cache-Control"] = "no-store, max-age=0";
                Response.Headers["Pragma"] = "no-cache";
            }

            var workoutTemplate = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.ExerciseType)
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.TemplateSets)
                .ThenInclude(s => s.Settype)
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == id);

            if (workoutTemplate == null)
            {
                return NotFound();
            }

            // Check if the template belongs to this coach or is public
            if (!workoutTemplate.IsPublic)
            {
                // Get the coach user ID
                var coachId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(coachId))
                {
                    return Forbid();
                }

                // Get the coach user ID as an integer from the database
                var coachUser = await _context.User
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.IdentityUserId == coachId);
                
                if (coachUser == null || workoutTemplate.UserId != coachUser.UserId)
                {
                    return Forbid();
                }
                
                // Load clients for the coach if template belongs to this coach
                if (coachUser != null && workoutTemplate.UserId == coachUser.UserId)
                {
                    // Get clients of this coach for the assignment dropdown
                    var relationships = await _context.CoachClientRelationships
                        .Where(r => r.CoachId == coachId && r.Status == RelationshipStatus.Active)
                        .ToListAsync();
                    
                    // Get the client IDs from the relationships
                    var clientIdentityIds = relationships.Select(r => r.ClientId).ToList();
                    
                    // Fetch the User records for these clients
                    Clients = await _context.User
                        .Where(u => clientIdentityIds.Contains(u.IdentityUserId))
                        .ToListAsync();
                    
                    // If no clients found, use demo data
                    if (!Clients.Any())
                    {
                        _logger.LogWarning("No actual clients found. Providing demo data for coach {coachId}", coachId);
                        
                        // Get a list of all users that aren't the coach
                        var otherUsers = await _context.User
                            .Where(u => u.IdentityUserId != coachId && u.IdentityUserId != null)
                            .Take(5)
                            .ToListAsync();
                            
                        if (otherUsers.Any())
                        {
                            _logger.LogInformation("Adding {count} demo clients for testing", otherUsers.Count);
                            Clients = otherUsers;
                        }
                    }
                    
                    _logger.LogInformation("Found {count} clients for coach {coachId}", Clients.Count, coachId);
                    
                    // Get recent assignments of this template
                    var recentAssignments = await _context.TemplateAssignments
                        .Where(a => a.WorkoutTemplateId == id && a.CoachUserId == coachUser.UserId)
                        .Include(a => a.Client)
                        .OrderByDescending(a => a.AssignedDate)
                        .Take(5)
                        .ToListAsync();
                        
                    RecentAssignments = recentAssignments.Select(a => new TemplateAssignmentViewModel
                    {
                        Id = a.TemplateAssignmentId,
                        TemplateId = a.WorkoutTemplateId,
                        Name = workoutTemplate.Name,
                        ClientRelationshipId = (int)a.ClientRelationshipId, // Explicit cast from int? to int
                        Notes = $"Assigned on {a.AssignedDate.ToShortDateString()}"
                    }).ToList();
                }
            }

            WorkoutTemplate = workoutTemplate;
            return Page();
        }
        
        public async Task<IActionResult> OnPostAssign(
            int templateId, 
            int clientId, 
            string name, 
            string notes, 
            [FromForm(Name = "startDate")] string startDateStr, 
            [FromForm(Name = "endDate")] string endDateStr, 
            bool scheduleWorkouts = false,
            string recurrencePattern = "Once",
            List<string> daysOfWeek = null,
            int? dayOfMonth = null,
            string workoutTime = "17:00",
            bool sendReminder = false,
            int reminderHoursBefore = 3)
        {
            _logger.LogInformation("Assigning template {templateId} to client {clientId}", templateId, clientId);
            
            // Parse dates from form inputs
            if (!DateTime.TryParse(startDateStr, out DateTime startDate))
            {
                _logger.LogWarning("Invalid start date format: {startDateStr}", startDateStr);
                ModelState.AddModelError("startDate", "Invalid start date format");
                return BadRequest(ModelState);
            }
            
            DateTime? endDate = null;
            if (!string.IsNullOrEmpty(endDateStr) && DateTime.TryParse(endDateStr, out DateTime parsedEndDate))
            {
                endDate = parsedEndDate;
            }
            
            // Disable output caching for this action
            Response.Headers["Cache-Control"] = "no-store, max-age=0";
            Response.Headers["Pragma"] = "no-cache";
            
            // Get the identity user ID of the coach
            var coachIdentityId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachIdentityId))
            {
                _logger.LogWarning("Coach identity ID not found");
                return Forbid();
            }
            
            // Get the coach user from the database
            var coachUser = await _context.User
                .FirstOrDefaultAsync(u => u.IdentityUserId == coachIdentityId);
            
            if (coachUser == null)
            {
                _logger.LogWarning("Coach user not found in database for identity ID {coachIdentityId}", coachIdentityId);
                return Forbid();
            }
            
            // Get the template from the database
            var template = await _context.WorkoutTemplate
                .FindAsync(templateId);
                
            if (template == null)
            {
                _logger.LogWarning("Template {templateId} not found", templateId);
                return NotFound("Template not found");
            }
            
            // Check if the template belongs to this coach or is public
            if (!template.IsPublic && template.UserId != coachUser.UserId)
            {
                _logger.LogWarning("Template {templateId} does not belong to coach {coachId}", templateId, coachUser.UserId);
                return Forbid();
            }
            
            // Get the client from the database
            var client = await _context.User
                .FindAsync(clientId);
                
            if (client == null)
            {
                _logger.LogWarning("Client {clientId} not found", clientId);
                return NotFound("Client not found");
            }

            // Find the relationship between the coach and client
            var clientIdentityId = await _context.User
                .Where(u => u.UserId == clientId)
                .Select(u => u.IdentityUserId)
                .FirstOrDefaultAsync();
                
            if (string.IsNullOrEmpty(clientIdentityId))
            {
                _logger.LogWarning("Client identity ID not found for user ID {clientId}", clientId);
                return NotFound("Client identity not found");
            }
            
            var relationship = await _context.CoachClientRelationships
                .FirstOrDefaultAsync(r => r.CoachId == coachIdentityId && r.ClientId == clientIdentityId);
                
            if (relationship == null)
            {
                _logger.LogWarning("Relationship not found between coach {coachId} and client {clientId}", coachIdentityId, clientIdentityId);
                return NotFound("Coach-client relationship not found");
            }
            
            // Create the template assignment
            var assignment = new TemplateAssignment
            {
                WorkoutTemplateId = templateId,
                ClientUserId = clientId,
                CoachUserId = coachUser.UserId,
                ClientRelationshipId = relationship.Id,
                Name = name,
                Notes = notes,
                AssignedDate = DateTime.UtcNow,
                StartDate = startDate,
                EndDate = endDate,
                IsActive = true
            };
            
            _context.TemplateAssignments.Add(assignment);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Template {templateId} assigned to client {clientId} with assignment ID {assignmentId}", 
                templateId, clientId, assignment.TemplateAssignmentId);
            
            // If scheduling workouts is requested, create the workout schedules
            if (scheduleWorkouts)
            {
                _logger.LogInformation("Scheduling workouts for template assignment {assignmentId} with pattern {recurrencePattern}", 
                    assignment.TemplateAssignmentId, recurrencePattern);
                
                try
                {
                    // Parse workout time
                    TimeSpan workoutTimeSpan = TimeSpan.Parse(workoutTime);
                    
                    // Handle different recurrence patterns
                    switch (recurrencePattern)
                    {
                        case "Once":
                            var onceWorkout = new WorkoutSchedule
                            {
                                TemplateAssignmentId = assignment.TemplateAssignmentId,
                                ClientUserId = clientId,
                                CoachUserId = coachUser.UserId,
                                Name = name,
                                Description = notes,
                                StartDate = startDate,
                                ScheduledDateTime = startDate.Add(workoutTimeSpan),
                                IsRecurring = false,
                                RecurrencePattern = "Once",
                                IsActive = true,
                                SendReminder = sendReminder,
                                ReminderHoursBefore = reminderHoursBefore
                            };
                            
                            _context.WorkoutSchedules.Add(onceWorkout);
                            break;
                            
                        case "Weekly":
                        case "BiWeekly":
                            // Validate days of week
                            if (daysOfWeek == null || !daysOfWeek.Any())
                            {
                                _logger.LogWarning("No days of week specified for weekly recurrence pattern");
                                // Default to the day of the start date
                                var defaultDay = startDate.DayOfWeek.ToString();
                                daysOfWeek = new List<string> { defaultDay };
                            }
                            
                            // Create a scheduled workout for each selected day of the week
                            foreach (var day in daysOfWeek)
                            {
                                if (Enum.TryParse<DayOfWeek>(day, out var dayOfWeek))
                                {
                                    // Calculate the first occurrence of this day of week on or after the start date
                                    DateTime firstOccurrence = CalculateNextDayOfWeek(startDate, dayOfWeek);
                                    
                                    var weeklyWorkout = new WorkoutSchedule
                                    {
                                        TemplateAssignmentId = assignment.TemplateAssignmentId,
                                        ClientUserId = clientId,
                                        CoachUserId = coachUser.UserId,
                                        Name = name,
                                        Description = notes,
                                        StartDate = startDate,
                                        EndDate = endDate,
                                        ScheduledDateTime = firstOccurrence.Add(workoutTimeSpan),
                                        IsRecurring = true,
                                        RecurrencePattern = recurrencePattern,
                                        RecurrenceDayOfWeek = (int)dayOfWeek,
                                        IsActive = true,
                                        SendReminder = sendReminder,
                                        ReminderHoursBefore = reminderHoursBefore
                                    };
                                    
                                    _context.WorkoutSchedules.Add(weeklyWorkout);
                                }
                            }
                            break;
                            
                        case "Monthly":
                            // Validate day of month
                            if (!dayOfMonth.HasValue || dayOfMonth.Value < 1 || dayOfMonth.Value > 31)
                            {
                                _logger.LogWarning("Invalid day of month specified for monthly recurrence pattern");
                                // Default to the day of the start date
                                dayOfMonth = startDate.Day;
                            }
                            
                            // Create a scheduled workout for the specified day of the month
                            var monthlyWorkout = new WorkoutSchedule
                            {
                                TemplateAssignmentId = assignment.TemplateAssignmentId,
                                ClientUserId = clientId,
                                CoachUserId = coachUser.UserId,
                                Name = name,
                                Description = notes,
                                StartDate = startDate,
                                EndDate = endDate,
                                ScheduledDateTime = new DateTime(startDate.Year, startDate.Month, dayOfMonth.Value).Add(workoutTimeSpan),
                                IsRecurring = true,
                                RecurrencePattern = "Monthly",
                                RecurrenceDayOfMonth = dayOfMonth.Value,
                                IsActive = true,
                                SendReminder = sendReminder,
                                ReminderHoursBefore = reminderHoursBefore
                            };
                            
                            _context.WorkoutSchedules.Add(monthlyWorkout);
                            break;
                    }
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully scheduled workouts for template assignment {assignmentId}", 
                        assignment.TemplateAssignmentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scheduling workouts for template assignment {assignmentId}", 
                        assignment.TemplateAssignmentId);
                }
            }
            
            return RedirectToPage("/WorkoutSchedules/Client", new { area = "Coach", clientId = clientId });
        }
        
        // Helper method to calculate the next occurrence of a specific day of week on or after a given date
        private DateTime CalculateNextDayOfWeek(DateTime startDate, DayOfWeek targetDayOfWeek)
        {
            int daysToAdd = ((int)targetDayOfWeek - (int)startDate.DayOfWeek + 7) % 7;
            return startDate.AddDays(daysToAdd);
        }
    }
}