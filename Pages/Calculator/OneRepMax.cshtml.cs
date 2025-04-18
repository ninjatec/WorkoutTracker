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
using WorkoutTrackerweb.Data;
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

        public async Task OnGetAsync()
        {
            await LoadUserDataAsync();
            await PopulateExerciseTypesAsync();
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

            // Create a new session for this 1RM calculation
            var session = new Session
            {
                Name = $"1RM Calculation - {DateTime.Now.ToString("dd/MM/yyyy")}",
                datetime = DateTime.Now,
                UserId = currentUserId.Value
            };

            _context.Session.Add(session);
            await _context.SaveChangesAsync();

            // Create a set with the 1RM result
            var set = new Set
            {
                Description = $"1RM Calculation",
                Notes = $"Estimated 1RM: {AverageOneRepMax:F2}kg (avg of 7 formulas) based on {Weight}kg for {Reps} reps",
                SessionId = session.SessionId,
                ExerciseTypeId = ExerciseTypeId.Value,
                SettypeId = 1, // Assuming 1 is a default settype
                NumberReps = Reps,
                Weight = Weight
            };

            _context.Set.Add(set);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"1RM calculation saved: {AverageOneRepMax:F2}kg for {await GetExerciseNameAsync(set.ExerciseTypeId)}";
            return RedirectToPage("/Sessions/Details", new { id = session.SessionId });
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
            
            var recentSets = await _context.Set
                .Include(s => s.ExerciseType)
                .Include(s => s.Session)
                .Include(s => s.Settype)  // Include the set type
                .Where(s => 
                    s.Session.UserId == currentUserId && 
                    s.Session.datetime >= reportPeriodAgo &&
                    s.NumberReps >= 1 && 
                    s.NumberReps <= 10 &&
                    s.Weight > 0 &&
                    s.Settype.Name.ToLower() != "warmup")  // Changed to lowercase comparison
                .OrderByDescending(s => s.Session.datetime)
                .ToListAsync();

            // Populate RecentSets with the most recent 15 sets
            RecentSets = recentSets
                .Take(15)
                .Select(s => new RecentSetData 
                {
                    SetId = s.SetId,
                    ExerciseName = s.ExerciseType.Name,
                    SessionName = s.Session.Name,
                    SessionDate = s.Session.datetime,
                    Weight = s.Weight,
                    Reps = s.NumberReps,
                    EstimatedOneRM = CalculateAverageOneRM(s.Weight, s.NumberReps)
                })
                .ToList();

            // Group sets by exercise type
            var exerciseGroups = recentSets
                .GroupBy(s => new { s.ExerciseTypeId, s.ExerciseType.Name });
            
            // Dictionary to store max 1RM per exercise for sorting
            var exerciseMaxOneRMs = new Dictionary<string, decimal>();
            var allExerciseSummaries = new List<ExerciseSummaryData>();
            
            // Calculate the different estimates for each exercise
            foreach (var exerciseGroup in exerciseGroups)
            {
                var maxOneRM = exerciseGroup.Select(s => CalculateAverageOneRM(s.Weight, s.NumberReps)).Max();
                exerciseMaxOneRMs[exerciseGroup.Key.Name] = maxOneRM;
                
                // Calculate results for each formula
                var formulas = new Dictionary<string, Func<decimal, int, decimal>>
                {
                    { "Brzycki", CalculateBrzycki },
                    { "Epley", CalculateEpley },
                    { "Lander", CalculateLander },
                    { "Lombardi", CalculateLombardi },
                    { "Mayhew", CalculateMayhew },
                    { "O'Conner", CalculateOConner },
                    { "Wathan", CalculateWathan },
                    { "Average", CalculateAverageOneRM }
                };

                foreach (var formula in formulas)
                {
                    var oneRMs = exerciseGroup.Select(s => formula.Value(s.Weight, s.NumberReps)).ToList();
                    
                    if (oneRMs.Any())
                    {
                        allExerciseSummaries.Add(new ExerciseSummaryData
                        {
                            ExerciseTypeId = exerciseGroup.Key.ExerciseTypeId,
                            ExerciseName = exerciseGroup.Key.Name,
                            Formula = formula.Key,
                            EstimatedOneRM = oneRMs.Max(),
                            SetsCount = exerciseGroup.Count(),
                            FirstSetDate = exerciseGroup.Min(s => s.Session.datetime),
                            LastSetDate = exerciseGroup.Max(s => s.Session.datetime)
                        });
                    }
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
                .ThenBy(s => s.Formula == "Average" ? 0 : 1) // Ensure "Average" appears first
                .ToList();
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