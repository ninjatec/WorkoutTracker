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
            var userId = await _userService.GetCurrentUserIdAsync();
            if (!userId.HasValue || userId.Value <= 0)
            {
                return RedirectToPage("/Identity/Account/Login");
            }

            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30); // Default to last 30 days

            try
            {
                Metrics = await _dashboardService.GetDashboardMetricsAsync(userId.Value, startDate, endDate);
                VolumeProgress = await _dashboardService.GetVolumeProgressChartDataAsync(userId.Value, startDate, endDate);
                WorkoutFrequency = await _dashboardService.GetWorkoutFrequencyChartDataAsync(userId.Value, startDate, endDate);

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
    }
}
