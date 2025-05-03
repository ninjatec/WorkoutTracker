using Microsoft.AspNetCore.Authorization;
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
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Calculator
{
    [Authorize]
    public class OneRepMaxModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;

        public OneRepMaxModel(WorkoutTrackerWebContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        [BindProperty]
        [Required]
        [Range(0.25, 1000, ErrorMessage = "Weight must be between 0.25kg and 1000kg")]
        public decimal Weight { get; set; }

        [BindProperty]
        [Required]
        [Range(1, 20, ErrorMessage = "Reps must be between 1 and 20")]
        public int Reps { get; set; }

        [BindProperty]
        public int? ExerciseTypeId { get; set; }
        
        public SelectList ExerciseTypeSelectList { get; set; }

        public Dictionary<string, decimal> OneRepMaxResults { get; set; }

        public decimal AverageOneRepMax { get; set; }
        
        // User's workout set data for plotting
        public List<CalculatorSetData> UserSets { get; set; }
        
        // Max values for chart scaling
        public double MaxOneRepMax { get; set; }
        public decimal MaxWeight { get; set; }
        public int MaxReps { get; set; }
        
        // Recent sets that can be used to calculate 1RM
        public List<RecentSetData> RecentSets { get; set; } = new List<RecentSetData>();
        
        // Aggregated 1RM data by exercise and calculation method
        public List<ExerciseSummaryData> ExerciseSummaries { get; set; } = new List<ExerciseSummaryData>();
        
        // Pagination parameters
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        
        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 5;  // Number of exercise groups per page
        
        [BindProperty(SupportsGet = true)]
        public int ReportPeriod { get; set; } = 90;  // Default to 90 days
        
        public int TotalPages { get; set; }
        
        public int TotalExercises { get; set; }
        
        // Available page sizes for dropdown
        public readonly List<int> PageSizeOptions = new List<int> { 5, 10, 15, 25, 50 };
        
        // Available report periods for dropdown
        public readonly List<int> ReportPeriodOptions = new List<int> { 30, 60, 90, 120 };
        
        public class RecentSetData
        {
            public int SetId { get; set; }
            public string ExerciseName { get; set; }
            public string SessionName { get; set; }
            public DateTime SessionDate { get; set; }
            public decimal Weight { get; set; }
            public int Reps { get; set; }
            public decimal EstimatedOneRM { get; set; }
        }
        
        public class ExerciseSummaryData
        {
            public int ExerciseTypeId { get; set; }
            public string ExerciseName { get; set; }
            public string Formula { get; set; }
            public decimal EstimatedOneRM { get; set; }
            public int SetsCount { get; set; }
            public DateTime FirstSetDate { get; set; }
            public DateTime LastSetDate { get; set; }
        }

        public class CalculatorSetData
        {
            public DateTime Date { get; set; }
            public double Weight { get; set; }
            public int Reps { get; set; }
            public double OneRepMax { get; set; }
            public double EstimatedOneRepMax { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return NotFound();
            }

            var sets = await _context.WorkoutSets
                .Include(s => s.WorkoutExercise)
                    .ThenInclude(we => we.ExerciseType)
                .Include(s => s.WorkoutExercise)
                    .ThenInclude(we => we.WorkoutSession)
                .Where(s => s.WorkoutExercise.WorkoutSession.UserId == user.UserId)
                .Where(s => s.Weight.HasValue && s.Reps.HasValue)
                .Where(s => s.WorkoutExercise.ExerciseType != null)
                .Where(s => s.WorkoutExercise.ExerciseType.ExerciseTypeId > 0)  // Ensure valid ExerciseType
                .Where(s => s.WorkoutExercise.ExerciseType.Name != null)        // Filter out null names
                .Where(s => !string.IsNullOrEmpty(s.WorkoutExercise.ExerciseType.Name)) // Additional null check
                .OrderByDescending(s => s.WorkoutExercise.WorkoutSession.StartDateTime)
                .Take(1000)
                .ToListAsync();

            UserSets = new List<CalculatorSetData>();
            MaxOneRepMax = 0;
            MaxWeight = 0;
            MaxReps = 0;

            foreach (var set in sets)
            {
                var oneRepMax = CalculateOneRepMax(set.Weight.Value, set.Reps.Value);
                UserSets.Add(new CalculatorSetData
                {
                    // Add explicit conversion from decimal to double
                    Weight = (double)set.Weight.Value,
                    Reps = set.Reps.Value,
                    OneRepMax = oneRepMax
                });
                
                MaxOneRepMax = Math.Max(MaxOneRepMax, oneRepMax);
                MaxWeight = Math.Max(MaxWeight, set.Weight.Value);
                MaxReps = Math.Max(MaxReps, set.Reps.Value);
            }

            // Load the user's 1RM data for the selected time period
            await LoadUserDataAsync();
            await PopulateExerciseTypesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadUserDataAsync();
                await PopulateExerciseTypesAsync();
                return Page();
            }

            // Calculate 1RM using different formulas
            OneRepMaxResults = new Dictionary<string, decimal>
            {
                { "Brzycki", CalculateBrzycki(Weight, Reps) },
                { "Epley", CalculateEpley(Weight, Reps) },
                { "Lander", CalculateLander(Weight, Reps) },
                { "Lombardi", CalculateLombardi(Weight, Reps) },
                { "Mayhew", CalculateMayhew(Weight, Reps) },
                { "O'Conner", CalculateOConner(Weight, Reps) },
                { "Wathan", CalculateWathan(Weight, Reps) }
            };

            // Calculate the average 1RM
            AverageOneRepMax = OneRepMaxResults.Values.Average();

            await LoadUserDataAsync();
            await PopulateExerciseTypesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostSaveResultAsync()
        {
            if (!ModelState.IsValid || !ExerciseTypeId.HasValue)
            {
                await LoadUserDataAsync();
                await PopulateExerciseTypesAsync();
                return Page();
            }

            // Calculate the 1RM average again
            OneRepMaxResults = new Dictionary<string, decimal>
            {
                { "Brzycki", CalculateBrzycki(Weight, Reps) },
                { "Epley", CalculateEpley(Weight, Reps) },
                { "Lander", CalculateLander(Weight, Reps) },
                { "Lombardi", CalculateLombardi(Weight, Reps) },
                { "Mayhew", CalculateMayhew(Weight, Reps) },
                { "O'Conner", CalculateOConner(Weight, Reps) },
                { "Wathan", CalculateWathan(Weight, Reps) }
            };

            AverageOneRepMax = OneRepMaxResults.Values.Average();

            // Get current user
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Create a new workout session for this 1RM calculation
            var workoutSession = new WorkoutSession
            {
                Name = $"1RM Calculation - {DateTime.Now:dd/MM/yyyy}",
                StartDateTime = DateTime.Now,
                UserId = currentUserId.Value,
                Status = "Completed"
            };

            _context.WorkoutSessions.Add(workoutSession);
            await _context.SaveChangesAsync();

            // Create a workout exercise
            var workoutExercise = new WorkoutExercise
            {
                WorkoutSessionId = workoutSession.WorkoutSessionId,
                ExerciseTypeId = ExerciseTypeId.Value,
                Notes = $"1RM Calculation"
            };

            _context.WorkoutExercises.Add(workoutExercise);
            await _context.SaveChangesAsync();

            // Create a workout set with the 1RM result
            var workoutSet = new WorkoutSet
            {
                WorkoutExerciseId = workoutExercise.WorkoutExerciseId,
                Notes = $"Estimated 1RM: {AverageOneRepMax:F2}kg (avg of 7 formulas) based on {Weight}kg for {Reps} reps",
                Reps = Reps,
                Weight = Weight,
                IsWarmup = false
            };

            _context.WorkoutSets.Add(workoutSet);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"1RM calculation saved: {AverageOneRepMax:F2}kg for {await GetExerciseNameAsync(workoutExercise.ExerciseTypeId)}";
            return RedirectToPage("/Sessions/Details", new { id = workoutSession.WorkoutSessionId });
        }

        private async Task LoadUserDataAsync()
        {
            // Get current user ID
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return;
            }

            // Get recent sets (last ReportPeriod days) with 1-10 reps (most accurate for 1RM prediction)
            var reportPeriodAgo = DateTime.Now.AddDays(-ReportPeriod);
            
            var recentSets = await _context.WorkoutSets
                .Include(ws => ws.WorkoutExercise)
                    .ThenInclude(we => we.ExerciseType)
                .Include(ws => ws.WorkoutExercise)
                    .ThenInclude(we => we.WorkoutSession)
                .Where(ws => 
                    ws.WorkoutExercise.WorkoutSession.UserId == currentUserId && 
                    ws.WorkoutExercise.WorkoutSession.StartDateTime >= reportPeriodAgo &&
                    ws.WorkoutExercise.ExerciseType != null &&
                    ws.Reps >= 1 && 
                    ws.Reps <= 10 &&
                    ws.Weight > 0 &&
                    !ws.IsWarmup)
                .OrderByDescending(ws => ws.WorkoutExercise.WorkoutSession.StartDateTime)
                .ToListAsync();

            // Populate RecentSets with the most recent 15 sets
            RecentSets = recentSets
                .Take(15)
                .Select(s => new RecentSetData 
                {
                    SetId = s.WorkoutSetId,
                    ExerciseName = s.WorkoutExercise.ExerciseType.Name,
                    SessionName = s.WorkoutExercise.WorkoutSession.Name,
                    SessionDate = s.WorkoutExercise.WorkoutSession.StartDateTime,
                    Weight = s.Weight.Value,
                    Reps = s.Reps.Value,
                    EstimatedOneRM = CalculateAverageOneRM(s.Weight.Value, s.Reps.Value)
                })
                .ToList();

            // Group sets by exercise type
            var exerciseGroups = recentSets
                .Where(s => s.WorkoutExercise?.ExerciseType?.Name != null)
                .GroupBy(s => new { s.WorkoutExercise.ExerciseTypeId, s.WorkoutExercise.ExerciseType.Name });
            
            // Dictionary to store max 1RM per exercise for sorting
            var exerciseMaxOneRMs = new Dictionary<string, decimal>();
            var allExerciseSummaries = new List<ExerciseSummaryData>();
            
            // Calculate the different estimates for each exercise
            foreach (var exerciseGroup in exerciseGroups)
            {
                var exerciseName = exerciseGroup.Key.Name;
                var exerciseOneRMs = new Dictionary<string, decimal>();
                
                // Calculate max 1RM for each formula
                foreach (var set in exerciseGroup)
                {
                    var setWeight = set.Weight.Value;
                    var setReps = set.Reps.Value;
                    
                    exerciseOneRMs["Brzycki"] = Math.Max(
                        exerciseOneRMs.GetValueOrDefault("Brzycki"), 
                        CalculateBrzycki(setWeight, setReps));
                        
                    exerciseOneRMs["Epley"] = Math.Max(
                        exerciseOneRMs.GetValueOrDefault("Epley"), 
                        CalculateEpley(setWeight, setReps));
                        
                    exerciseOneRMs["Lander"] = Math.Max(
                        exerciseOneRMs.GetValueOrDefault("Lander"), 
                        CalculateLander(setWeight, setReps));
                        
                    exerciseOneRMs["Lombardi"] = Math.Max(
                        exerciseOneRMs.GetValueOrDefault("Lombardi"), 
                        CalculateLombardi(setWeight, setReps));
                        
                    exerciseOneRMs["Mayhew"] = Math.Max(
                        exerciseOneRMs.GetValueOrDefault("Mayhew"), 
                        CalculateMayhew(setWeight, setReps));
                        
                    exerciseOneRMs["O'Conner"] = Math.Max(
                        exerciseOneRMs.GetValueOrDefault("O'Conner"), 
                        CalculateOConner(setWeight, setReps));
                        
                    exerciseOneRMs["Wathan"] = Math.Max(
                        exerciseOneRMs.GetValueOrDefault("Wathan"), 
                        CalculateWathan(setWeight, setReps));
                }
                
                // Calculate average max 1RM across all formulas
                var averageOneRM = exerciseOneRMs.Values.Average();
                exerciseOneRMs["Average"] = averageOneRM;
                
                // Store max 1RM for sorting
                exerciseMaxOneRMs[exerciseName] = averageOneRM;
                
                // Create summary entries for each formula
                foreach (var formula in exerciseOneRMs)
                {
                    allExerciseSummaries.Add(new ExerciseSummaryData
                    {
                        ExerciseTypeId = exerciseGroup.Key.ExerciseTypeId,
                        ExerciseName = exerciseName,
                        Formula = formula.Key,
                        EstimatedOneRM = formula.Value,
                        SetsCount = exerciseGroup.Count(),
                        FirstSetDate = exerciseGroup.Min(s => s.WorkoutExercise.WorkoutSession.StartDateTime),
                        LastSetDate = exerciseGroup.Max(s => s.WorkoutExercise.WorkoutSession.StartDateTime)
                    });
                }
            }
            
            // Sort all exercise summaries by their max 1RM weight
            var sortedExerciseGroups = allExerciseSummaries
                .GroupBy(s => s.ExerciseName)
                .OrderByDescending(g => exerciseMaxOneRMs[g.Key]);
            
            TotalExercises = sortedExerciseGroups.Count();
            TotalPages = (int)Math.Ceiling(TotalExercises / (double)PageSize);
            
            // Apply pagination and flatten results back to a list
            ExerciseSummaries = sortedExerciseGroups
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .SelectMany(g => g)
                .OrderBy(s => s.ExerciseName)
                .ThenBy(s => s.Formula == "Average" ? 0 : 1)
                .ToList();
        }

        private async Task LoadUserDataAsync(int userId, int exerciseTypeId)
        {
            // Get recent sessions for this user and exercise
            var sessions = await _context.WorkoutSessions
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.WorkoutSets)
                .Where(ws => ws.UserId == userId)
                .OrderByDescending(ws => ws.StartDateTime)
                .Take(10)
                .ToListAsync();

            List<CalculatorSetData> setData = new();
            foreach (var session in sessions)
            {
                foreach (var exercise in session.WorkoutExercises.Where(we => we.ExerciseTypeId == exerciseTypeId))
                {
                    foreach (var set in exercise.WorkoutSets)
                    {
                        if (set.Weight.HasValue && set.Reps.HasValue)
                        {
                            var oneRepMaxValue = CalculateOneRepMax(set.Weight.Value, set.Reps.Value);
                            setData.Add(new CalculatorSetData
                            {
                                Date = session.StartDateTime,
                                Weight = (double)set.Weight.Value,
                                Reps = set.Reps.Value,
                                OneRepMax = oneRepMaxValue,
                                EstimatedOneRepMax = oneRepMaxValue
                            });
                        }
                    }
                }
            }

            UserSets = setData.OrderByDescending(s => s.Date).ToList();
            if (setData.Any())
            {
                MaxOneRepMax = setData.Max(s => s.EstimatedOneRepMax);
                MaxWeight = setData.Max(s => (decimal)s.Weight);
                MaxReps = setData.Max(s => s.Reps);
            }
        }

        private async Task PopulateExerciseTypesAsync()
        {
            ExerciseTypeSelectList = new SelectList(
                await _context.ExerciseType.OrderBy(e => e.Name).ToListAsync(),
                "ExerciseTypeId",
                "Name"
            );
        }
        
        private async Task<string> GetExerciseNameAsync(int exerciseTypeId)
        {
            var exerciseType = await _context.ExerciseType.FindAsync(exerciseTypeId);
            return exerciseType?.Name ?? "Unknown Exercise";
        }
        
        private static decimal CalculateAverageOneRM(decimal weight, int reps)
        {
            return new[] {
                CalculateBrzycki(weight, reps),
                CalculateEpley(weight, reps),
                CalculateLander(weight, reps),
                CalculateLombardi(weight, reps),
                CalculateMayhew(weight, reps),
                CalculateOConner(weight, reps),
                CalculateWathan(weight, reps)
            }.Average();
        }

        // Calculate average one rep max across all formulas
        private double CalculateOneRepMax(decimal weight, int reps)
        {
            return (double)CalculateAverageOneRM(weight, reps);
        }

        // 1RM Formula implementations
        private static decimal CalculateBrzycki(decimal weight, int reps)
        {
            // Prevent division by zero or negative numbers when reps is 37 or higher
            if (reps >= 37)
                return weight * 100; // Return a very high value as a fallback
            
            return weight * 36m / (37m - reps);
        }

        private static decimal CalculateEpley(decimal weight, int reps)
        {
            return weight * (1m + 0.0333m * reps);
        }

        private static decimal CalculateLander(decimal weight, int reps)
        {
            // Prevent division by zero if the denominator approaches zero or goes negative
            decimal denominator = 101.3m - 2.67123m * reps;
            if (denominator <= 0)
                return weight * 100; // Return a very high value as a fallback
                
            return (100m * weight) / denominator;
        }

        private static decimal CalculateLombardi(decimal weight, int reps)
        {
            // Prevent negative or zero input to Math.Pow
            if (reps <= 0)
                return weight;
                
            return weight * (decimal)Math.Pow((double)reps, 0.1);
        }

        private static decimal CalculateMayhew(decimal weight, int reps)
        {
            // Prevent division by zero
            decimal denominator = 52.2m + (41.9m * (decimal)Math.Exp(-0.055 * reps));
            if (denominator <= 0)
                return weight * 100; // Return a very high value as a fallback
                
            return (100m * weight) / denominator;
        }

        private static decimal CalculateOConner(decimal weight, int reps)
        {
            return weight * (1m + 0.025m * reps);
        }

        private static decimal CalculateWathan(decimal weight, int reps)
        {
            // Prevent division by zero
            decimal denominator = 48.8m + (53.8m * (decimal)Math.Exp(-0.075 * reps));
            if (denominator <= 0)
                return weight * 100; // Return a very high value as a fallback
                
            return (100m * weight) / denominator;
        }
    }
}