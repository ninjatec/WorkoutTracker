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
using WorkoutTrackerWeb.Services.Calculations;
using Microsoft.AspNetCore.OutputCaching;

namespace WorkoutTrackerWeb.Pages.Shared
{
    [OutputCache(PolicyName = "SharedWorkout")]
    public class CalculatorModel : SharedPageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IVolumeCalculationService _volumeCalculationService;

        public List<RecentSetData> RecentSets { get; set; } = new List<RecentSetData>();
        public List<ExerciseSummaryData> ExerciseSummaries { get; set; } = new List<ExerciseSummaryData>();
        public int ReportPeriod { get; set; } = 60; // Default to last 60 days for shared access

        public class RecentSetData
        {
            public DateTime Date { get; set; }
            public string ExerciseName { get; set; }
            public decimal? Weight { get; set; }
            public int? Reps { get; set; }
            public string SessionName { get; set; }
            public DateTime SessionDate { get; set; }
            public decimal EstimatedOneRM { get; set; }
        }

        public class ExerciseSummaryData
        {
            public string ExerciseName { get; set; }
            public decimal MaxWeight { get; set; }
            public int TotalReps { get; set; }
            public decimal TotalVolume { get; set; }
            public string Formula { get; set; }
            public decimal EstimatedOneRM { get; set; }
            public int SetsCount { get; set; }
            public DateTime FirstSetDate { get; set; }
            public DateTime LastSetDate { get; set; }
        }

        public CalculatorModel(
            ITokenValidationService tokenValidationService,
            WorkoutTrackerWebContext context, 
            IVolumeCalculationService volumeCalculationService,
            ILogger<CalculatorModel> logger)
            : base(tokenValidationService, logger)
        {
            _context = context;
            _volumeCalculationService = volumeCalculationService;
        }

        public async Task<IActionResult> OnGetAsync(string token = null)
        {
            // Set token for validation
            Token = token;
            var isValid = await ValidateShareTokenAsync();
            if (!isValid)
            {
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToPage("./TokenRequired");
                }
                return RedirectToPage("./InvalidToken", new { Message = "Invalid or expired token" });
            }

            // Check if token allows calculator access
            if (!SharedTokenData.IsValid)
            {
                _logger.LogWarning("Token does not have permission to access calculator");
                return RedirectToPage("./AccessDenied", new { Message = "Your share token does not have permission to use the calculators." });
            }

            // Fetch data for calculators
            var userId = SharedTokenData.UserId;

            await LoadSharedUserDataAsync();
            return Page();
        }

        private async Task LoadSharedUserDataAsync()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-ReportPeriod);

            // Get recent sets for the shared user
            var recentSets = await _context.WorkoutSessions
                .Where(ws => ws.UserId == SharedTokenData.UserId)
                .Where(ws => ws.StartDateTime >= cutoffDate)
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.ExerciseType)
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.WorkoutSets)
                .OrderByDescending(ws => ws.StartDateTime)
                .ToListAsync();

            RecentSets.Clear();
            ExerciseSummaries.Clear();
            var exerciseSummaries = new Dictionary<string, ExerciseSummaryData>();

            foreach (var session in recentSets)
            {
                foreach (var exercise in session.WorkoutExercises)
                {
                    var exerciseName = exercise.ExerciseType?.Name ?? "Unknown Exercise";

                    foreach (var set in exercise.WorkoutSets.Where(s => s.IsCompleted))
                    {
                        if (set.Weight.HasValue && set.Reps.HasValue)
                        {
                            // Add to recent sets
                            RecentSets.Add(new RecentSetData
                            {
                                Date = session.StartDateTime,
                                ExerciseName = exerciseName,
                                Weight = set.Weight,
                                Reps = set.Reps
                            });

                            // Update exercise summary
                            if (!exerciseSummaries.ContainsKey(exerciseName))
                            {
                                exerciseSummaries[exerciseName] = new ExerciseSummaryData
                                {
                                    ExerciseName = exerciseName,
                                    MaxWeight = set.Weight.Value,
                                    TotalReps = 0,
                                    TotalVolume = 0
                                };
                            }

                            var summary = exerciseSummaries[exerciseName];
                            summary.MaxWeight = Math.Max(summary.MaxWeight, set.Weight.Value);
                            summary.TotalReps += set.Reps.Value;
                            summary.TotalVolume += set.Weight.Value * set.Reps.Value;
                        }
                    }
                }
            }

            RecentSets = RecentSets.OrderByDescending(s => s.Date).Take(10).ToList();
            ExerciseSummaries = exerciseSummaries.Values.OrderByDescending(s => s.TotalVolume).ToList();
        }
    }
}