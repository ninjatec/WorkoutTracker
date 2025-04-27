using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Extensions;

namespace WorkoutTrackerWeb.Pages.Clients
{
    public class ViewWorkoutAssignmentModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;

        public ViewWorkoutAssignmentModel(WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int AssignmentId { get; set; }

        public AssignmentViewModel Assignment { get; set; } = new AssignmentViewModel();
        public int ExerciseCount { get; set; }
        public List<ExerciseViewModel> Exercises { get; set; } = new List<ExerciseViewModel>();
        public List<ScheduleViewModel> UpcomingSchedules { get; set; } = new List<ScheduleViewModel>();
        public List<SessionViewModel> RecentSessions { get; set; } = new List<SessionViewModel>();
        public List<VolumeByExerciseViewModel> VolumeByExerciseType { get; set; } = new List<VolumeByExerciseViewModel>();
        public List<WeeklyActivityViewModel> WeeklyActivity { get; set; } = new List<WeeklyActivityViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Get the current user
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            // Get the template assignment and verify it belongs to the current user
            var assignment = await _context.TemplateAssignments
                .Include(a => a.WorkoutTemplate)
                .ThenInclude(t => t.TemplateExercises)
                .ThenInclude(e => e.ExerciseType)
                .Include(a => a.Coach)
                .FirstOrDefaultAsync(a => a.TemplateAssignmentId == AssignmentId && a.ClientUserId == user.UserId);

            if (assignment == null)
            {
                TempData["ErrorMessage"] = "Workout assignment not found.";
                return RedirectToPage("./Workouts");
            }

            // Populate the assignment view model
            Assignment = new AssignmentViewModel
            {
                TemplateAssignmentId = assignment.TemplateAssignmentId,
                WorkoutTemplateId = assignment.WorkoutTemplateId,
                Name = assignment.Name,
                TemplateName = assignment.WorkoutTemplate.Name,
                TemplateCategory = assignment.WorkoutTemplate.Category,
                Notes = assignment.Notes,
                StartDate = assignment.StartDate,
                EndDate = assignment.EndDate,
                IsActive = assignment.IsActive,
                CoachName = GetCoachName(assignment.Coach),
                CoachId = assignment.CoachUserId
            };

            // Get the exercises from the template
            var templateExercises = assignment.WorkoutTemplate.TemplateExercises
                .OrderBy(e => e.OrderIndex)
                .ToList();

            ExerciseCount = templateExercises.Count();

            // Populate exercises
            foreach (var templateExercise in templateExercises)
            {
                var exercise = new ExerciseViewModel
                {
                    ExerciseTypeId = templateExercise.ExerciseTypeId,
                    Name = templateExercise.ExerciseType.Name,
                    EquipmentName = templateExercise.Equipment?.Name ?? "Bodyweight",
                    Sets = templateExercise.Sets,
                    RepRange = templateExercise.MinReps == templateExercise.MaxReps 
                        ? templateExercise.MinReps.ToString() 
                        : $"{templateExercise.MinReps}-{templateExercise.MaxReps}",
                    RestSeconds = templateExercise.RestSeconds,
                    Notes = templateExercise.Notes,
                    IconClass = GetExerciseIconClass(templateExercise.ExerciseType.Category)
                };

                Exercises.Add(exercise);
            }

            // Get upcoming scheduled workouts
            UpcomingSchedules = await _context.WorkoutSchedules
                .Where(s => s.TemplateAssignmentId == AssignmentId && 
                           s.ClientUserId == user.UserId &&
                           s.ScheduledDateTime > DateTime.Now &&
                           s.IsActive)
                .OrderBy(s => s.ScheduledDateTime)
                .Take(5)
                .Select(s => new ScheduleViewModel
                {
                    WorkoutScheduleId = s.WorkoutScheduleId,
                    Name = s.Name,
                    ScheduledDateTime = s.ScheduledDateTime ?? DateTime.Now
                })
                .ToListAsync();

            // Get recent workout sessions for this assignment
            RecentSessions = await _context.WorkoutSessions
                .Where(s => s.UserId == user.UserId && 
                           s.TemplateAssignmentId == AssignmentId &&
                           s.CompletedDate != null)
                .OrderByDescending(s => s.CompletedDate)
                .Take(10)
                .Select(s => new SessionViewModel
                {
                    WorkoutSessionId = s.WorkoutSessionId,
                    Name = s.Name,
                    CompletedDate = s.CompletedDate ?? DateTime.Now,
                    Duration = s.Duration,
                    TotalVolume = s.WorkoutExercises
                        .SelectMany(e => e.WorkoutSets)
                        .Sum(set => (set.Weight ?? 0) * (set.Reps ?? 0)),
                    HasFeedback = s.WorkoutFeedback != null,
                    OverallRating = s.WorkoutFeedback != null ? s.WorkoutFeedback.OverallRating : 0
                })
                .ToListAsync();

            // Calculate volume by exercise type
            var volumeByExercise = await _context.WorkoutSessions
                .Where(s => s.UserId == user.UserId && 
                           s.TemplateAssignmentId == AssignmentId &&
                           s.CompletedDate >= DateTime.Now.AddDays(-30))
                .SelectMany(s => s.WorkoutExercises)
                .GroupBy(e => e.ExerciseTypeId)
                .Select(g => new 
                {
                    ExerciseTypeId = g.Key,
                    TotalVolume = g.SelectMany(e => e.WorkoutSets)
                                   .Sum(set => (set.Weight ?? 0) * (set.Reps ?? 0))
                })
                .ToListAsync();

            // Get exercise names and create view models
            foreach (var item in volumeByExercise)
            {
                var exerciseType = await _context.ExerciseType.FindAsync(item.ExerciseTypeId);
                if (exerciseType == null) continue;

                VolumeByExerciseType.Add(new VolumeByExerciseViewModel
                {
                    ExerciseTypeId = item.ExerciseTypeId,
                    ExerciseName = exerciseType.Name,
                    TotalVolume = item.TotalVolume
                });
            }

            // If no volume data, add placeholder data
            if (!VolumeByExerciseType.Any())
            {
                foreach (var exercise in Exercises.Take(3))
                {
                    VolumeByExerciseType.Add(new VolumeByExerciseViewModel
                    {
                        ExerciseTypeId = exercise.ExerciseTypeId,
                        ExerciseName = exercise.Name,
                        TotalVolume = 0
                    });
                }
            }

            // Calculate weekly activity
            var startDate = DateTime.Now.AddDays(-42); // 6 weeks ago
            var weeklyWorkouts = await _context.WorkoutSessions
                .Where(s => s.UserId == user.UserId && 
                            s.CompletedDate >= startDate)
                .GroupBy(s => new { Year = s.CompletedDate.Value.Year, Week = GetIso8601WeekOfYear(s.CompletedDate.Value) })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Week,
                    Count = g.Count()
                })
                .OrderBy(w => w.Year)
                .ThenBy(w => w.Week)
                .ToListAsync();

            // Generate week labels and fill in gaps
            var currentWeek = GetIso8601WeekOfYear(DateTime.Now);
            var currentYear = DateTime.Now.Year;

            for (int i = 0; i < 6; i++)
            {
                // Calculate week number with rollover to previous year if necessary
                int weekNumber = currentWeek - i;
                int year = currentYear;
                
                if (weekNumber <= 0)
                {
                    // Handle week numbers from previous year
                    weekNumber = GetIso8601WeeksInYear(year - 1) + weekNumber;
                    year--;
                }

                var weekData = weeklyWorkouts.FirstOrDefault(w => w.Year == year && w.Week == weekNumber);
                var count = weekData?.Count ?? 0;

                WeeklyActivity.Insert(0, new WeeklyActivityViewModel
                {
                    Year = year,
                    WeekNumber = weekNumber,
                    WeekLabel = $"W{weekNumber}",
                    WorkoutCount = count
                });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostStartWorkoutAsync(int assignmentId, string sessionName, string notes)
        {
            // Get the current user
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return Unauthorized();
            }

            // Verify the assignment exists and belongs to the user
            var assignment = await _context.TemplateAssignments
                .Include(a => a.WorkoutTemplate)
                .ThenInclude(t => t.TemplateExercises)
                .ThenInclude(e => e.ExerciseType)
                .FirstOrDefaultAsync(a => a.TemplateAssignmentId == assignmentId && a.ClientUserId == user.UserId);

            if (assignment == null || !assignment.IsActive)
            {
                TempData["ErrorMessage"] = "Workout assignment not found or inactive.";
                return RedirectToPage("./Workouts");
            }

            // Begin transaction to ensure consistency
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Create a new workout session
                var session = new WorkoutSession
                {
                    UserId = user.UserId,
                    Name = sessionName,
                    Description = notes,
                    TemplateAssignmentId = assignmentId,
                    WorkoutTemplateId = assignment.WorkoutTemplateId,
                    StartTime = DateTime.Now,
                    IsFromCoach = true,
                    Status = "In Progress"
                };

                _context.WorkoutSessions.Add(session);
                await _context.SaveChangesAsync();

                // Create workout exercises from the template
                var templateExercises = assignment.WorkoutTemplate.TemplateExercises
                    .OrderBy(e => e.OrderIndex)
                    .ToList();

                foreach (var templateExercise in templateExercises)
                {
                    var workoutExercise = new WorkoutExercise
                    {
                        WorkoutSessionId = session.WorkoutSessionId,
                        ExerciseTypeId = templateExercise.ExerciseTypeId,
                        EquipmentId = templateExercise.EquipmentId,
                        OrderIndex = templateExercise.OrderIndex,
                        Notes = templateExercise.Notes
                    };

                    _context.WorkoutExercises.Add(workoutExercise);
                    await _context.SaveChangesAsync();

                    // Create empty sets based on the template
                    for (int i = 0; i < templateExercise.Sets; i++)
                    {
                        var workoutSet = new WorkoutSet
                        {
                            WorkoutExerciseId = workoutExercise.WorkoutExerciseId,
                            SetNumber = i + 1,
                            TargetMinReps = templateExercise.MinReps,
                            TargetMaxReps = templateExercise.MaxReps,
                            RestSeconds = templateExercise.RestSeconds
                        };

                        _context.WorkoutSets.Add(workoutSet);
                    }

                    await _context.SaveChangesAsync();
                }

                // Commit transaction
                await transaction.CommitAsync();

                return RedirectToPage("./ActiveWorkout", new { sessionId = session.WorkoutSessionId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error starting workout: {ex.Message}";
                return RedirectToPage(new { AssignmentId = assignmentId });
            }
        }

        // Helper methods
        private string GetExerciseIconClass(string category)
        {
            return category switch
            {
                "Barbell" => "fas fa-dumbbell",
                "Dumbbell" => "fas fa-dumbbell",
                "Machine" => "fas fa-robot",
                "Bodyweight" => "fas fa-running",
                "Cardio" => "fas fa-heartbeat",
                "Olympic" => "fas fa-medal",
                "Kettlebell" => "fas fa-weight-hanging",
                "Cable" => "fas fa-exchange-alt",
                "Medicine Ball" => "fas fa-circle",
                "Stretching" => "fas fa-child",
                "TRX/Suspension" => "fas fa-grip-lines",
                _ => "fas fa-dumbbell"
            };
        }

        private static int GetIso8601WeekOfYear(DateTime date)
        {
            var day = (int)System.Globalization.CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
            if (day == 0) day = 7; // Sunday should be 7, not 0
            
            // Add days to get to Thursday (ISO weeks start on Monday and Thursday is used for determining the year)
            date = date.AddDays(4 - day);
            
            // Get the first day of the year
            var startOfYear = new DateTime(date.Year, 1, 1);
            
            // Get the number of days since start of the year
            var daysSinceStartOfYear = (date - startOfYear).Days;
            
            // Add 1 because we're tracking week numbers and there's no week 0
            return (daysSinceStartOfYear / 7) + 1;
        }

        private static int GetIso8601WeeksInYear(int year)
        {
            // Get the last day of the year
            var lastDay = new DateTime(year, 12, 31);
            
            // Check if last day is week 1 of next year
            var lastWeek = GetIso8601WeekOfYear(lastDay);
            
            // If the last day is in week 1, then check previous week
            if (lastWeek == 1)
            {
                lastDay = lastDay.AddDays(-7);
                lastWeek = GetIso8601WeekOfYear(lastDay);
            }
            
            return lastWeek;
        }

        private string GetCoachName(object coach)
        {
            if (coach is WorkoutTrackerWeb.Models.Identity.AppUser appUser)
            {
                return appUser.FullName();
            }
            else if (coach != null)
            {
                // Fallback - try to get any properties that might contain the name
                var type = coach.GetType();
                var nameProperty = type.GetProperty("Name") ?? type.GetProperty("FullName");
                if (nameProperty != null)
                {
                    return nameProperty.GetValue(coach)?.ToString() ?? "Unknown Coach";
                }
            }
            
            return "Unknown Coach";
        }

        // View models for the page
        public class AssignmentViewModel
        {
            public int TemplateAssignmentId { get; set; }
            public int WorkoutTemplateId { get; set; }
            public string Name { get; set; }
            public string TemplateName { get; set; }
            public string TemplateCategory { get; set; }
            public string Notes { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public bool IsActive { get; set; }
            public string CoachName { get; set; }
            public int CoachId { get; set; }
        }

        public class ExerciseViewModel
        {
            public int ExerciseTypeId { get; set; }
            public string Name { get; set; }
            public string EquipmentName { get; set; }
            public int Sets { get; set; }
            public string RepRange { get; set; }
            public int RestSeconds { get; set; }
            public string Notes { get; set; }
            public string IconClass { get; set; }
        }

        public class ScheduleViewModel
        {
            public int WorkoutScheduleId { get; set; }
            public string Name { get; set; }
            public DateTime ScheduledDateTime { get; set; }
        }

        public class SessionViewModel
        {
            public int WorkoutSessionId { get; set; }
            public string Name { get; set; }
            public DateTime CompletedDate { get; set; }
            public int Duration { get; set; }
            public decimal TotalVolume { get; set; }
            public bool HasFeedback { get; set; }
            public int OverallRating { get; set; }
        }

        public class VolumeByExerciseViewModel
        {
            public int ExerciseTypeId { get; set; }
            public string ExerciseName { get; set; }
            public decimal TotalVolume { get; set; }
        }

        public class WeeklyActivityViewModel
        {
            public int Year { get; set; }
            public int WeekNumber { get; set; }
            public string WeekLabel { get; set; }
            public int WorkoutCount { get; set; }
        }
    }
}