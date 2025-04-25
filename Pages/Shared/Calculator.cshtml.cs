using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using Microsoft.AspNetCore.OutputCaching;

namespace WorkoutTrackerWeb.Pages.Shared
{
    [OutputCache(PolicyName = "SharedWorkout")]
    public class CalculatorModel : SharedPageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        
        public List<RecentSetData> RecentSets { get; set; } = new List<RecentSetData>();
        public List<ExerciseSummaryData> ExerciseSummaries { get; set; } = new List<ExerciseSummaryData>();
        public int ReportPeriod { get; set; } = 60; // Default to last 60 days for shared access

        public class RecentSetData
        {
            public string ExerciseName { get; set; }
            public string SessionName { get; set; }
            public DateTime SessionDate { get; set; }
            public decimal Weight { get; set; }
            public int Reps { get; set; }
            public decimal EstimatedOneRM { get; set; }
        }

        public class ExerciseSummaryData
        {
            public string ExerciseName { get; set; }
            public string Formula { get; set; }
            public decimal EstimatedOneRM { get; set; }
            public int SetsCount { get; set; }
            public DateTime FirstSetDate { get; set; }
            public DateTime LastSetDate { get; set; }
        }

        public CalculatorModel(
            WorkoutTrackerWebContext context,
            IShareTokenService shareTokenService,
            ILogger<CalculatorModel> logger) 
            : base(shareTokenService, logger)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(string token)
        {
            // Validate token with "calculator" permission
            if (!await ValidateTokenAsync(token, "calculator"))
            {
                // If no token, redirect to token required page
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToPage("/Shared/TokenRequired");
                }
                
                // ValidateTokenAsync will handle redirects for invalid tokens
                return Page();
            }

            await LoadSharedUserDataAsync();
            return Page();
        }

        private async Task LoadSharedUserDataAsync()
        {
            if (ShareToken == null)
            {
                return;
            }

            int userId = ShareToken.UserId;
            var reportPeriodAgo = DateTime.Now.AddDays(-ReportPeriod);
            
            // Get recent sets with 1-10 reps (most accurate for 1RM prediction)
            var recentSets = await _context.Set
                .Include(s => s.ExerciseType)
                .Include(s => s.Session)
                .Include(s => s.Settype)
                .Where(s => 
                    s.Session.UserId == userId && 
                    s.Session.datetime >= reportPeriodAgo &&
                    s.NumberReps >= 1 && 
                    s.NumberReps <= 10 &&
                    s.Weight > 0 &&
                    s.Settype.Name.ToLower() != "warmup")
                .OrderByDescending(s => s.Session.datetime)
                .ToListAsync();

            // Convert to RecentSetData objects for the view
            RecentSets = recentSets
                .Take(10) // Limit to 10 most recent sets
                .Select(s => new RecentSetData 
                {
                    ExerciseName = s.ExerciseType.Name,
                    SessionName = s.Session.Name,
                    SessionDate = s.Session.datetime,
                    Weight = s.Weight,
                    Reps = s.NumberReps,
                    EstimatedOneRM = CalculateAverageOneRM(s.Weight, s.NumberReps)
                })
                .ToList();

            // Group sets by exercise type for summary data
            var exerciseGroups = recentSets
                .GroupBy(s => s.ExerciseType.Name);
            
            var allExerciseSummaries = new List<ExerciseSummaryData>();
            
            // Calculate 1RM estimates for each exercise using different formulas
            foreach (var exerciseGroup in exerciseGroups)
            {
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
                            ExerciseName = exerciseGroup.Key,
                            Formula = formula.Key,
                            EstimatedOneRM = oneRMs.Max(),
                            SetsCount = exerciseGroup.Count(),
                            FirstSetDate = exerciseGroup.Min(s => s.Session.datetime),
                            LastSetDate = exerciseGroup.Max(s => s.Session.datetime)
                        });
                    }
                }
            }
            
            // Sort exercises by their max estimated 1RM
            ExerciseSummaries = allExerciseSummaries
                .OrderBy(s => s.ExerciseName)
                .ThenBy(s => s.Formula == "Average" ? 0 : 1) // Ensure "Average" appears first
                .ToList();
        }

        // 1RM Formula implementations
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

        private static decimal CalculateBrzycki(decimal weight, int reps)
        {
            if (reps >= 37)
                return weight * 100; // Prevent division by zero
            
            return weight * 36m / (37m - reps);
        }

        private static decimal CalculateEpley(decimal weight, int reps)
        {
            return weight * (1m + 0.0333m * reps);
        }

        private static decimal CalculateLander(decimal weight, int reps)
        {
            decimal denominator = 101.3m - 2.67123m * reps;
            if (denominator <= 0)
                return weight * 100; // Prevent division by zero
                
            return (100m * weight) / denominator;
        }

        private static decimal CalculateLombardi(decimal weight, int reps)
        {
            if (reps <= 0)
                return weight;
                
            return weight * (decimal)Math.Pow((double)reps, 0.1);
        }

        private static decimal CalculateMayhew(decimal weight, int reps)
        {
            decimal denominator = 52.2m + (41.9m * (decimal)Math.Exp(-0.055 * reps));
            if (denominator <= 0)
                return weight * 100; // Prevent division by zero
                
            return (100m * weight) / denominator;
        }

        private static decimal CalculateOConner(decimal weight, int reps)
        {
            return weight * (1m + 0.025m * reps);
        }

        private static decimal CalculateWathan(decimal weight, int reps)
        {
            decimal denominator = 48.8m + (53.8m * (decimal)Math.Exp(-0.075 * reps));
            if (denominator <= 0)
                return weight * 100; // Prevent division by zero
                
            return (100m * weight) / denominator;
        }
    }
}