using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Templates
{
    [Area("Coach")]
    [CoachAuthorize]
    public class StartWorkoutModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<StartWorkoutModel> _logger;

        public StartWorkoutModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ILogger<StartWorkoutModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public WorkoutTemplate WorkoutTemplate { get; set; } = default!;
        public List<ClientViewModel> Clients { get; set; } = new List<ClientViewModel>();
        public string DefaultSessionName { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string StatusMessageType { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
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

            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Check if the template belongs to this coach or is public
            if (workoutTemplate.UserId != int.Parse(coachId) && !workoutTemplate.IsPublic)
            {
                return Forbid();
            }

            WorkoutTemplate = workoutTemplate;
            DefaultSessionName = $"{workoutTemplate.Name} - {DateTime.Now:MMM d, yyyy}";

            // Get clients for this coach
            var relationships = await _context.CoachClientRelationships
                .Where(r => r.CoachId == coachId && r.Status == RelationshipStatus.Active)
                .Include(r => r.Client)
                .ToListAsync();

            Clients = relationships
                .Select(r => new ClientViewModel
                {
                    UserId = int.Parse(r.ClientId),
                    Name = r.Client.UserName // Use username as name
                })
                .OrderBy(c => c.Name)
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int templateId, int clientId, string sessionName, 
            DateTime sessionDate, string sessionNotes)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Verify the template exists
            var template = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.TemplateSets)
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
                return RedirectToPage("Details", new { id = templateId });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create new workout session
                var session = new Session
                {
                    UserId = clientId,
                    Name = sessionName,
                    Notes = sessionNotes,
                    StartDateTime = sessionDate,
                    datetime = sessionDate
                };

                _context.Session.Add(session);
                await _context.SaveChangesAsync();

                // Create sets from template
                int exerciseSequence = 1;
                foreach (var templateExercise in template.TemplateExercises.OrderBy(e => e.SequenceNum))
                {
                    int setSequence = 1;
                    foreach (var templateSet in templateExercise.TemplateSets.OrderBy(s => s.SequenceNum))
                    {
                        var sessionSet = new Set
                        {
                            SessionId = session.SessionId,
                            SequenceNum = setSequence++,
                            ExerciseTypeId = templateExercise.ExerciseTypeId,
                            SettypeId = templateSet.SettypeId
                        };
                        
                        // Add reps
                        sessionSet.Reps = new List<Rep>();
                        var rep = new Rep
                        {
                            repnumber = templateSet.DefaultReps,
                            weight = templateSet.DefaultWeight
                        };
                        sessionSet.Reps.Add(rep);

                        _context.Set.Add(sessionSet);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                StatusMessageType = "Success";
                StatusMessage = $"Workout session '{sessionName}' created successfully for client.";
                
                // Redirect to the client details page
                return RedirectToPage("/Clients/Details", new { area = "Coach", id = clientId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                
                _logger.LogError(ex, "Error creating workout from template {TemplateId} for client {ClientId}", 
                    templateId, clientId);
                StatusMessageType = "Error";
                StatusMessage = $"An error occurred: {ex.Message}";
                return RedirectToPage("Details", new { id = templateId });
            }
        }

        public async Task<IActionResult> OnPostScheduleWorkoutAsync(int templateId, int clientId, string name, 
            string description, DateTime startDate, DateTime? endDate, string recurrencePattern = "Once",
            List<string> daysOfWeek = null, int? dayOfMonth = null, string workoutTime = null, 
            bool sendReminder = true, int reminderHoursBefore = 3)
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
                return RedirectToPage("Details", new { id = templateId });
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
                return RedirectToPage("/ScheduledWorkouts", new { area = "Coach" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling workout for template {TemplateId} for client {ClientId}", 
                    templateId, clientId);
                StatusMessageType = "Error";
                StatusMessage = $"An error occurred: {ex.Message}";
                return RedirectToPage("Details", new { id = templateId });
            }
        }

        public class ClientViewModel
        {
            public int UserId { get; set; }
            public string Name { get; set; }
        }
    }
}