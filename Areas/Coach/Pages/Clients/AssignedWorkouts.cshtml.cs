using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Areas.Coach.Pages.ErrorHandling;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Clients
{
    [CoachAuthorize]
    public class AssignedWorkoutsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<AssignedWorkoutsModel> _logger;

        public AssignedWorkoutsModel(WorkoutTrackerWebContext context, ILogger<AssignedWorkoutsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public int ClientId { get; set; }

        public AppUser Client { get; set; }
        public List<TemplateAssignmentViewModel> TemplateAssignments { get; set; } = new List<TemplateAssignmentViewModel>();
        public List<WorkoutScheduleViewModel> WorkoutSchedules { get; set; } = new List<WorkoutScheduleViewModel>();
        public List<WorkoutFeedbackViewModel> RecentFeedback { get; set; } = new List<WorkoutFeedbackViewModel>();
        public List<WorkoutTemplateViewModel> AvailableTemplates { get; set; } = new List<WorkoutTemplateViewModel>();
        public List<ChartDataPoint> ChartData { get; set; } = new List<ChartDataPoint>();
        public ExerciseProgressViewModel ExerciseProgressData { get; set; } = new ExerciseProgressViewModel();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Get the current user (coach)
                var user = await _context.GetCurrentUserAsync();
                if (user == null)
                {
                    return Unauthorized();
                }

                // Verify the client exists and is a client of the coach
                var relationship = await _context.CoachClientRelationships
                    .Include(r => r.Client)
                    .FirstOrDefaultAsync(r => r.CoachId == user.UserId.ToString() && r.ClientId == ClientId.ToString() && r.Status == RelationshipStatus.Active);

                if (relationship == null)
                {
                    ErrorUtils.HandleValidationError(this, "Client relationship not found or inactive.");
                    return RedirectToPage("./Index");
                }

                // Set the client
                Client = relationship.Client;

                try
                {
                    // Get template assignments for this client
                    TemplateAssignments = await _context.TemplateAssignments
                        .Where(a => a.ClientUserId == ClientId && a.CoachUserId == user.UserId)
                        .Include(a => a.WorkoutTemplate)
                        .OrderByDescending(a => a.IsActive)
                        .ThenByDescending(a => a.CreatedDate)
                        .Select(a => new TemplateAssignmentViewModel
                        {
                            TemplateAssignmentId = a.TemplateAssignmentId,
                            WorkoutTemplateId = a.WorkoutTemplateId,
                            Name = a.Name,
                            TemplateName = a.WorkoutTemplate.Name,
                            Notes = a.Notes,
                            StartDate = a.StartDate,
                            EndDate = a.EndDate,
                            IsActive = a.IsActive,
                            CreatedDate = a.CreatedDate
                        })
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading template assignments for client {ClientId}", ClientId);
                    // Continue with empty template assignments rather than failing the whole page
                }

                try
                {
                    // Get workout schedules for this client
                    WorkoutSchedules = await _context.WorkoutSchedules
                        .Where(s => s.ClientUserId == ClientId && s.CoachUserId == user.UserId)
                        .Include(s => s.TemplateAssignment)
                        .OrderByDescending(s => s.IsActive)
                        .ThenByDescending(s => s.ScheduledDateTime)
                        .Select(s => new WorkoutScheduleViewModel
                        {
                            WorkoutScheduleId = s.WorkoutScheduleId,
                            TemplateAssignmentId = s.TemplateAssignmentId,
                            TemplateAssignmentName = s.TemplateAssignment != null ? s.TemplateAssignment.Name : null,
                            Name = s.Name,
                            Description = s.Description,
                            StartDate = s.StartDate,
                            EndDate = s.EndDate,
                            ScheduledDateTime = s.ScheduledDateTime,
                            IsRecurring = s.IsRecurring,
                            RecurrencePattern = s.RecurrencePattern,
                            RecurrenceDayOfWeekInt = s.RecurrenceDayOfWeek,
                            RecurrenceDayOfMonth = s.RecurrenceDayOfMonth,
                            SendReminder = s.SendReminder,
                            ReminderHoursBefore = s.ReminderHoursBefore,
                            IsActive = s.IsActive
                        })
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading workout schedules for client {ClientId}", ClientId);
                    // Continue with empty workout schedules rather than failing the whole page
                }

                try
                {
                    // Get recent workout feedback from this client
                    RecentFeedback = await _context.WorkoutFeedbacks
                        .Where(f => f.ClientUserId == ClientId)
                        .OrderByDescending(f => f.FeedbackDate)
                        .Take(10)
                        .Select(f => new WorkoutFeedbackViewModel
                        {
                            WorkoutFeedbackId = f.WorkoutFeedbackId,
                            SessionName = f.Session.Name,
                            SessionDate = f.Session.StartDateTime,
                            FeedbackDate = f.FeedbackDate,
                            OverallRating = f.OverallRating,
                            DifficultyRating = f.DifficultyRating,
                            EnergyLevel = f.EnergyLevel,
                            CompletedAllExercises = f.CompletedAllExercises,
                            Comments = f.Comments
                        })
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading workout feedback for client {ClientId}", ClientId);
                    // Continue with empty feedback rather than failing the whole page
                }

                try
                {
                    // Get available templates for assignment
                    AvailableTemplates = await _context.WorkoutTemplate
                        .Where(t => t.UserId == user.UserId || t.IsPublic)
                        .OrderByDescending(t => t.LastModifiedDate)
                        .Select(t => new WorkoutTemplateViewModel
                        {
                            WorkoutTemplateId = t.WorkoutTemplateId,
                            Name = t.Name,
                            Category = t.Category,
                            CreatedDate = t.CreatedDate
                        })
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading available templates for client {ClientId}", ClientId);
                    // Continue with empty templates rather than failing the whole page
                }

                try
                {
                    // Get performance data for charts
                    await LoadPerformanceData(ClientId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading performance data for client {ClientId}", ClientId);
                    // Initialize empty performance data
                    InitializeEmptyPerformanceData();
                }

                return Page();
            }
            catch (Exception ex)
            {
                ErrorUtils.HandleException(_logger, ex, this, 
                    "An error occurred loading the client's workout data. Please try again.",
                    $"loading assigned workouts for client {ClientId}");
                return RedirectToPage("./Index");
            }
        }

        private void InitializeEmptyPerformanceData()
        {
            // Create empty chart data
            ChartData = new List<ChartDataPoint>();
            for (int i = 0; i < 5; i++)
            {
                ChartData.Add(new ChartDataPoint
                {
                    Date = DateTime.Today.AddDays(-i * 5),
                    TotalVolume = 0
                });
            }

            // Initialize exercise progress view model with empty data
            ExerciseProgressData = new ExerciseProgressViewModel
            {
                Dates = new List<DateTime>(),
                Exercises = new List<ExerciseProgressItem>()
            };

            // Add sample dates
            for (int i = 0; i < 10; i++)
            {
                ExerciseProgressData.Dates.Add(DateTime.Today.AddDays(-3 * i));
            }
            ExerciseProgressData.Dates.Reverse();

            // Add a placeholder exercise
            var placeholderExercise = new ExerciseProgressItem
            {
                Name = "No data yet",
                Color = "#4e73df",
                Weights = ExerciseProgressData.Dates.Select(d => (decimal?)0).ToList()
            };
            ExerciseProgressData.Exercises.Add(placeholderExercise);
        }

        private async Task LoadPerformanceData(int clientId)
        {
            try
            {
                // For workout volume chart: Last 30 days by default
                var startDate = DateTime.Today.AddDays(-30);
                var endDate = DateTime.Today;

                // Get workout sessions with volume data
                var workoutSessions = await _context.WorkoutSessions
                    .Where(s => s.UserId == clientId && s.CompletedDate >= startDate && s.CompletedDate <= endDate)
                    .Include(s => s.WorkoutExercises)
                    .ThenInclude(e => e.WorkoutSets)
                    .OrderBy(s => s.CompletedDate)
                    .ToListAsync();

                // Calculate total volume for each session
                ChartData = workoutSessions
                    .Select(s => new ChartDataPoint
                    {
                        Date = s.CompletedDate ?? DateTime.Now,
                        TotalVolume = s.WorkoutExercises
                            .SelectMany(e => e.WorkoutSets)
                            .Sum(set => (set.Weight ?? 0) * (set.Reps ?? 0))
                    })
                    .ToList();

                // If no data, add some placeholder data points
                if (!ChartData.Any())
                {
                    for (int i = 0; i < 5; i++)
                    {
                        ChartData.Add(new ChartDataPoint
                        {
                            Date = DateTime.Today.AddDays(-i * 5),
                            TotalVolume = 0
                        });
                    }
                }

                // For exercise progress chart: Get key exercises and their progress
                var keyExercises = new List<int>();
                try
                {
                    keyExercises = await _context.WorkoutSessions
                        .Where(s => s.UserId == clientId)
                        .SelectMany(s => s.WorkoutExercises)
                        .GroupBy(e => e.ExerciseTypeId)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .Select(g => g.Key)
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting key exercises for client {ClientId}", clientId);
                }

                // Placeholder for demo - in case there's no data yet
                if (!keyExercises.Any())
                {
                    try
                    {
                        keyExercises = await _context.ExerciseType
                            .OrderBy(e => e.Name)
                            .Take(3)
                            .Select(e => e.ExerciseTypeId)
                            .ToListAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting placeholder exercise types for client {ClientId}", clientId);
                    }
                }

                // Initialize exercise progress view model
                ExerciseProgressData = new ExerciseProgressViewModel
                {
                    Dates = new List<DateTime>(),
                    Exercises = new List<ExerciseProgressItem>()
                };

                // Set dates for exercise progress chart (last 10 data points)
                for (int i = 0; i < 10; i++)
                {
                    ExerciseProgressData.Dates.Add(DateTime.Today.AddDays(-3 * i));
                }
                ExerciseProgressData.Dates.Reverse();

                // Get exercise names and generate colors
                var colors = new[] { "#4e73df", "#1cc88a", "#36b9cc", "#f6c23e", "#e74a3b" };
                var colorIndex = 0;

                foreach (var exerciseId in keyExercises)
                {
                    try
                    {
                        var exercise = await _context.ExerciseType.FindAsync(exerciseId);
                        if (exercise == null) continue;

                        // Get max weight for each exercise over time
                        var weightData = await _context.WorkoutSessions
                            .Where(s => s.UserId == clientId && s.CompletedDate >= startDate)
                            .OrderBy(s => s.CompletedDate)
                            .SelectMany(s => s.WorkoutExercises.Where(e => e.ExerciseTypeId == exerciseId))
                            .SelectMany(e => e.WorkoutSets)
                            .GroupBy(s => (s.WorkoutExercise.WorkoutSession.CompletedDate ?? DateTime.Now).Date)
                            .Select(g => new
                            {
                                Date = g.Key,
                                MaxWeight = g.Max(s => s.Weight ?? 0)
                            })
                            .ToListAsync();

                        // Create exercise progress item
                        var progressItem = new ExerciseProgressItem
                        {
                            Name = exercise.Name,
                            Color = colors[colorIndex % colors.Length],
                            Weights = new List<decimal?>()
                        };

                        // Fill in weights data for each date
                        foreach (var date in ExerciseProgressData.Dates)
                        {
                            var dataPoint = weightData.FirstOrDefault(w => w.Date == date.Date);
                            progressItem.Weights.Add(dataPoint?.MaxWeight);
                        }

                        ExerciseProgressData.Exercises.Add(progressItem);
                        colorIndex++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing exercise data for exercise {ExerciseId}, client {ClientId}", exerciseId, clientId);
                    }
                }

                // If no exercise data, add placeholder data
                if (!ExerciseProgressData.Exercises.Any())
                {
                    var placeholderExercise = new ExerciseProgressItem
                    {
                        Name = "No data yet",
                        Color = colors[0],
                        Weights = ExerciseProgressData.Dates.Select(d => (decimal?)0).ToList()
                    };
                    ExerciseProgressData.Exercises.Add(placeholderExercise);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LoadPerformanceData for client {ClientId}", clientId);
                // Initialize empty performance data
                InitializeEmptyPerformanceData();
            }
        }

        public async Task<IActionResult> OnPostAssignAsync(int templateId, string name, string notes,
                                                         DateTime startDate, DateTime? endDate,
                                                         bool scheduleWorkouts = false, string recurrencePattern = "Once",
                                                         List<string> daysOfWeek = null, int? dayOfMonth = null,
                                                         string workoutTime = null, bool sendReminder = false,
                                                         int reminderHoursBefore = 3)
        {
            // Get the current user (coach)
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            // Verify the client exists and is a client of the coach
            var relationship = await _context.CoachClientRelationships
                .FirstOrDefaultAsync(r => r.CoachId == user.UserId.ToString() && r.ClientId == ClientId.ToString() && r.Status == RelationshipStatus.Active);

            if (relationship == null)
            {
                ErrorUtils.HandleValidationError(this, "Client relationship not found or inactive.");
                return RedirectToPage(new { ClientId });
            }

            // Verify the template exists
            var template = await _context.WorkoutTemplate
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId && (t.UserId == user.UserId || t.IsPublic));

            if (template == null)
            {
                ErrorUtils.HandleValidationError(this, "Template not found.");
                return RedirectToPage(new { ClientId });
            }

            // Begin transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create template assignment
                var assignment = new TemplateAssignment
                {
                    WorkoutTemplateId = templateId,
                    ClientUserId = ClientId,
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
                        ClientUserId = ClientId,
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

                    // Set recurrence specifics
                    if (recurrencePattern == "Weekly" && daysOfWeek != null && daysOfWeek.Any())
                    {
                        // Parse the day of week string to the enum and then to int
                        DayOfWeek dayOfWeek = Enum.Parse<DayOfWeek>(daysOfWeek.First());
                        schedule.RecurrenceDayOfWeek = (int)dayOfWeek;
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

                ErrorUtils.SetSuccessMessage(this, $"Template '{template.Name}' successfully assigned to client.");
                return RedirectToPage(new { ClientId });
            }
            catch (Exception ex)
            {
                // Roll back the transaction on error
                await transaction.RollbackAsync();
                ErrorUtils.HandleException(_logger, ex, this, 
                    "An error occurred while assigning the template to the client.",
                    $"assigning template {templateId} to client {ClientId}");
                return RedirectToPage(new { ClientId });
            }
        }

        public async Task<IActionResult> OnPostScheduleAsync(int assignmentId, string name, string description,
                                                           DateTime startDate, DateTime? endDate,
                                                           string recurrencePattern = "Once",
                                                           List<string> daysOfWeek = null, int? dayOfMonth = null,
                                                           string workoutTime = null, bool sendReminder = false,
                                                           int reminderHoursBefore = 3)
        {
            // Get the current user (coach)
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            // Verify assignment exists and belongs to this client/coach
            var assignment = await _context.TemplateAssignments
                .FirstOrDefaultAsync(a => a.TemplateAssignmentId == assignmentId &&
                                         a.ClientUserId == ClientId &&
                                         a.CoachUserId == user.UserId);

            if (assignment == null)
            {
                ErrorUtils.HandleValidationError(this, "Template assignment not found.");
                return RedirectToPage(new { ClientId });
            }

            try
            {
                // Parse workout time
                TimeSpan workoutTimeOfDay = TimeSpan.Parse(workoutTime ?? "17:00");

                // Create workout schedule
                var schedule = new WorkoutSchedule
                {
                    TemplateAssignmentId = assignmentId,
                    ClientUserId = ClientId,
                    CoachUserId = user.UserId,
                    Name = name,
                    Description = description,
                    StartDate = startDate,
                    EndDate = endDate,
                    ScheduledDateTime = startDate.Date.Add(workoutTimeOfDay),
                    IsRecurring = recurrencePattern != "Once",
                    RecurrencePattern = recurrencePattern,
                    SendReminder = sendReminder,
                    ReminderHoursBefore = reminderHoursBefore,
                    IsActive = true
                };

                // Set recurrence specifics
                if (recurrencePattern == "Weekly" && daysOfWeek != null && daysOfWeek.Any())
                {
                    // Parse the day of week string to the enum and then to int
                    DayOfWeek dayOfWeek = Enum.Parse<DayOfWeek>(daysOfWeek.First());
                    schedule.RecurrenceDayOfWeek = (int)dayOfWeek;
                }
                else if (recurrencePattern == "Monthly" && dayOfMonth.HasValue)
                {
                    schedule.RecurrenceDayOfMonth = dayOfMonth.Value;
                }

                _context.WorkoutSchedules.Add(schedule);
                await _context.SaveChangesAsync();

                ErrorUtils.SetSuccessMessage(this, "Workout scheduled successfully.");
                return RedirectToPage(new { ClientId });
            }
            catch (Exception ex)
            {
                ErrorUtils.HandleException(_logger, ex, this, 
                    "An error occurred while scheduling the workout.",
                    $"scheduling workout for assignment {assignmentId}, client {ClientId}");
                return RedirectToPage(new { ClientId });
            }
        }

        public async Task<IActionResult> OnPostToggleActiveAsync(int assignmentId, bool isActive)
        {
            // Get the current user (coach)
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            try
            {
                // Verify assignment exists and belongs to this client/coach
                var assignment = await _context.TemplateAssignments
                    .FirstOrDefaultAsync(a => a.TemplateAssignmentId == assignmentId &&
                                             a.ClientUserId == ClientId &&
                                             a.CoachUserId == user.UserId);

                if (assignment == null)
                {
                    ErrorUtils.HandleValidationError(this, "Template assignment not found.");
                    return RedirectToPage(new { ClientId });
                }

                // Update assignment status
                assignment.IsActive = isActive;
                await _context.SaveChangesAsync();

                ErrorUtils.SetSuccessMessage(this, $"Assignment '{assignment.Name}' {(isActive ? "activated" : "deactivated")} successfully.");
                return RedirectToPage(new { ClientId });
            }
            catch (Exception ex)
            {
                ErrorUtils.HandleException(_logger, ex, this, 
                    $"An error occurred while {(isActive ? "activating" : "deactivating")} the assignment.",
                    $"toggling assignment {assignmentId} active state to {isActive}");
                return RedirectToPage(new { ClientId });
            }
        }

        public async Task<IActionResult> OnPostToggleScheduleAsync(int scheduleId, bool isActive)
        {
            // Get the current user (coach)
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            try
            {
                // Verify schedule exists and belongs to this client/coach
                var schedule = await _context.WorkoutSchedules
                    .FirstOrDefaultAsync(s => s.WorkoutScheduleId == scheduleId &&
                                             s.ClientUserId == ClientId &&
                                             s.CoachUserId == user.UserId);

                if (schedule == null)
                {
                    ErrorUtils.HandleValidationError(this, "Workout schedule not found.");
                    return RedirectToPage(new { ClientId });
                }

                // Update schedule status
                schedule.IsActive = isActive;
                await _context.SaveChangesAsync();

                ErrorUtils.SetSuccessMessage(this, $"Schedule '{schedule.Name}' {(isActive ? "activated" : "deactivated")} successfully.");
                return RedirectToPage(new { ClientId });
            }
            catch (Exception ex)
            {
                ErrorUtils.HandleException(_logger, ex, this, 
                    $"An error occurred while {(isActive ? "activating" : "deactivating")} the schedule.",
                    $"toggling schedule {scheduleId} active state to {isActive}");
                return RedirectToPage(new { ClientId });
            }
        }

        public async Task<IActionResult> OnPostDeleteAssignmentAsync(int assignmentId)
        {
            // Get the current user (coach)
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            // Verify assignment exists and belongs to this client/coach
            var assignment = await _context.TemplateAssignments
                .Include(a => a.WorkoutSchedules)
                .FirstOrDefaultAsync(a => a.TemplateAssignmentId == assignmentId &&
                                         a.ClientUserId == ClientId &&
                                         a.CoachUserId == user.UserId);

            if (assignment == null)
            {
                ErrorUtils.HandleValidationError(this, "Template assignment not found.");
                return RedirectToPage(new { ClientId });
            }

            // Begin transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // First remove related schedules
                _context.WorkoutSchedules.RemoveRange(assignment.WorkoutSchedules);
                
                // Then remove the assignment
                _context.TemplateAssignments.Remove(assignment);
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ErrorUtils.SetSuccessMessage(this, $"Assignment '{assignment.Name}' deleted successfully.");
                return RedirectToPage(new { ClientId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ErrorUtils.HandleException(_logger, ex, this, 
                    "An error occurred while deleting the assignment.",
                    $"deleting assignment {assignmentId}");
                return RedirectToPage(new { ClientId });
            }
        }

        public async Task<IActionResult> OnPostDeleteScheduleAsync(int scheduleId)
        {
            // Get the current user (coach)
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            try
            {
                // Verify schedule exists and belongs to this client/coach
                var schedule = await _context.WorkoutSchedules
                    .FirstOrDefaultAsync(s => s.WorkoutScheduleId == scheduleId &&
                                             s.ClientUserId == ClientId &&
                                             s.CoachUserId == user.UserId);

                if (schedule == null)
                {
                    ErrorUtils.HandleValidationError(this, "Workout schedule not found.");
                    return RedirectToPage(new { ClientId });
                }

                // Remove the schedule
                _context.WorkoutSchedules.Remove(schedule);
                await _context.SaveChangesAsync();

                ErrorUtils.SetSuccessMessage(this, $"Schedule '{schedule.Name}' deleted successfully.");
                return RedirectToPage(new { ClientId });
            }
            catch (Exception ex)
            {
                ErrorUtils.HandleException(_logger, ex, this, 
                    "An error occurred while deleting the schedule.",
                    $"deleting schedule {scheduleId}");
                return RedirectToPage(new { ClientId });
            }
        }

        // Method to convert DayOfWeek? to int?
        private int? DayOfWeekToInt(DayOfWeek? dayOfWeek)
        {
            if (dayOfWeek.HasValue)
            {
                return (int)dayOfWeek.Value;
            }
            return null;
        }

        // View models for the page
        public class TemplateAssignmentViewModel
        {
            public int TemplateAssignmentId { get; set; }
            public int WorkoutTemplateId { get; set; }
            public string Name { get; set; }
            public string TemplateName { get; set; }
            public string Notes { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        public class WorkoutScheduleViewModel
        {
            public int WorkoutScheduleId { get; set; }
            public int? TemplateAssignmentId { get; set; }
            public string TemplateAssignmentName { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public DateTime? ScheduledDateTime { get; set; }
            public bool IsRecurring { get; set; }
            public string RecurrencePattern { get; set; }
            public int? RecurrenceDayOfWeekInt { get; set; }
            public int? RecurrenceDayOfMonth { get; set; }
            public bool SendReminder { get; set; }
            public int ReminderHoursBefore { get; set; }
            public bool IsActive { get; set; }
            
            // Method to get the day of week as an enum for display
            public DayOfWeek? GetDayOfWeekEnum()
            {
                if (RecurrencePattern == "Weekly" && RecurrenceDayOfWeekInt.HasValue)
                {
                    // Convert int to DayOfWeek enum for display
                    return (DayOfWeek)RecurrenceDayOfWeekInt.Value;
                }
                return null;
            }
        }

        public class WorkoutFeedbackViewModel
        {
            public int WorkoutFeedbackId { get; set; }
            public string SessionName { get; set; }
            public DateTime SessionDate { get; set; }
            public DateTime FeedbackDate { get; set; }
            public int OverallRating { get; set; }
            public int DifficultyRating { get; set; }
            public int EnergyLevel { get; set; }
            public bool CompletedAllExercises { get; set; }
            public string Comments { get; set; }
        }

        public class WorkoutTemplateViewModel
        {
            public int WorkoutTemplateId { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
            public DateTime CreatedDate { get; set; }
        }

        public class ChartDataPoint
        {
            public DateTime Date { get; set; }
            public decimal TotalVolume { get; set; }
        }

        public class ExerciseProgressViewModel
        {
            public List<DateTime> Dates { get; set; } = new List<DateTime>();
            public List<ExerciseProgressItem> Exercises { get; set; } = new List<ExerciseProgressItem>();
        }

        public class ExerciseProgressItem
        {
            public string Name { get; set; }
            public string Color { get; set; }
            public List<decimal?> Weights { get; set; } = new List<decimal?>();
        }
    }
}