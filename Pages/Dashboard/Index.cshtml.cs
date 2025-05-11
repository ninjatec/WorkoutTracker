using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Services.Dashboard;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.ViewModels.Dashboard;

namespace WorkoutTrackerWeb.Pages.Dashboard
{
    [Authorize]
    [OutputCache(PolicyName = DashboardCachePolicy.PolicyName)]
    public class IndexModel : PageModel
    {
        private readonly IUserService _userService;
        private readonly WorkoutTrackerWebContext _context;
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<IndexModel> _logger;

        public DashboardMetrics Metrics { get; set; }
        public IEnumerable<ChartData> VolumeProgress { get; set; }
        public IEnumerable<ChartData> WorkoutFrequency { get; set; }
        public SelectList ExerciseTypes { get; set; }

        public IndexModel(
            IUserService userService,
            WorkoutTrackerWebContext context,
            IDashboardService dashboardService,
            ILogger<IndexModel> logger)
        {
            _userService = userService;
            _context = context;
            _dashboardService = dashboardService;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToPage("/Identity/Account/Login");
            }
            int userId = user.UserId;

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30); // Default to last 30 days

            try
            {
                Metrics = await _dashboardService.GetDashboardMetricsAsync(userId, startDate, endDate);
                VolumeProgress = await _dashboardService.GetVolumeProgressChartDataAsync(userId, startDate, endDate);
                WorkoutFrequency = await _dashboardService.GetWorkoutFrequencyChartDataAsync(userId, startDate, endDate);

                var exerciseTypes = await _context.ExerciseType
                    .Select(et => new { et.ExerciseTypeId, et.Name })
                    .ToListAsync();
                ExerciseTypes = new SelectList(exerciseTypes, "ExerciseTypeId", "Name");

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data for user {UserId}", userId);
                return RedirectToPage("/Error");
            }
        }

        public async Task<IActionResult> OnGetChartDataAsync(DateTime? startDate, DateTime? endDate)
        {
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return new JsonResult(new { error = "Not authenticated" }) { StatusCode = 401 };
            }
            int userId = user.UserId;

            var end = endDate ?? DateTime.UtcNow;
            var start = startDate ?? end.AddDays(-30);

            try
            {
                var volumeProgress = (await _dashboardService.GetVolumeProgressChartDataAsync(userId, start, end))
                    .Select(d => new { date = d.Date.ToString("yyyy-MM-dd"), value = d.Value, label = d.Label });
                var workoutFrequency = (await _dashboardService.GetWorkoutFrequencyChartDataAsync(userId, start, end))
                    .Select(d => new { date = d.Date.ToString("yyyy-MM-dd"), value = d.Value, label = d.Label });
                var personalBests = (await _dashboardService.GetPersonalBestsAsync(userId, start, end))
                    .Select(pb => new {
                        exerciseName = pb.ExerciseName,
                        weight = pb.Weight,
                        reps = pb.Reps,
                        estimatedOneRM = pb.EstimatedOneRM,
                        achievedDate = pb.AchievedDate.ToString("yyyy-MM-dd")
                    });
                var metrics = await _dashboardService.GetDashboardMetricsAsync(userId, start, end);
                var volumeByExercise = metrics.VolumeByExercise;

                // Log data for debugging
                _logger.LogInformation("Dashboard data: VolumeProgress count: {VolumeProgressCount}, WorkoutFrequency count: {WorkoutFrequencyCount}, PersonalBests count: {PersonalBestsCount}, VolumeByExercise count: {VolumeByExerciseCount}",
                    volumeProgress.Count(), workoutFrequency.Count(), personalBests.Count(), volumeByExercise.Count);

                return new JsonResult(new
                {
                    volumeProgress,
                    workoutFrequency,
                    personalBests,
                    volumeByExercise
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard chart data for user {UserId}", userId);
                return new JsonResult(new { error = "Failed to load chart data." }) { StatusCode = 500 };
            }
        }
    }
}
