using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services.Progress;
using WorkoutTrackerWeb.Data;

namespace WorkoutTrackerWeb.Pages.Progress
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IProgressDashboardService _progressDashboardService;
        private readonly WorkoutTrackerWebContext _context;

        public IndexModel(
            ILogger<IndexModel> logger, 
            IProgressDashboardService progressDashboardService,
            WorkoutTrackerWebContext context)
        {
            _logger = logger;
            _progressDashboardService = progressDashboardService;
            _context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-3).Date;
        public DateTime EndDate { get; set; } = DateTime.Now.Date;

        // The user model to access the current user's information
        public User UserModel { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Get the current user
                var currentUser = await _context.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return NotFound("User not found.");
                }

                UserModel = currentUser;

                // Parse date range from query parameters if provided
                if (DateTime.TryParse(Request.Query["start"], out var start))
                {
                    StartDate = start.Date;
                }

                if (DateTime.TryParse(Request.Query["end"], out var end))
                {
                    EndDate = end.Date;
                }

                // Validate date range
                if (EndDate < StartDate)
                {
                    EndDate = StartDate.AddMonths(3);
                }

                // Don't allow ranges longer than 1 year
                if ((EndDate - StartDate).TotalDays > 365)
                {
                    EndDate = StartDate.AddYears(1);
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading progress dashboard");
                StatusMessage = "Error: An error occurred while loading the dashboard.";
                return Page();
            }
        }

        [OutputCache(Duration = 300, VaryByQueryKeys = new[] { "start", "end" })]
        public async Task<IActionResult> OnGetDataAsync()
        {
            try
            {
                // Get the current user
                var currentUser = await _context.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    return NotFound("User not found.");
                }

                // Parse date range from query parameters
                if (DateTime.TryParse(Request.Query["start"], out var start))
                {
                    StartDate = start.Date;
                }

                if (DateTime.TryParse(Request.Query["end"], out var end))
                {
                    EndDate = end.Date;
                }

                // Validate date range
                if (EndDate < StartDate)
                {
                    EndDate = StartDate.AddMonths(3);
                }

                // Don't allow ranges longer than 1 year
                if ((EndDate - StartDate).TotalDays > 365)
                {
                    EndDate = StartDate.AddYears(1);
                }

                // Calculate metrics that need to be calculated
                await _progressDashboardService.CalculateAndStoreMissingMetricsAsync();

                // Get metrics from the service
                var volumeData = await _progressDashboardService.GetVolumeSeriesAsync(currentUser.UserId, StartDate, EndDate);
                var intensityData = await _progressDashboardService.GetIntensitySeriesAsync(currentUser.UserId, StartDate, EndDate);
                var consistencyData = await _progressDashboardService.GetConsistencySeriesAsync(currentUser.UserId, StartDate, EndDate);

                // Format data for charts
                var result = new
                {
                    Volume = FormatMetricsForChart(volumeData),
                    Intensity = FormatMetricsForChart(intensityData),
                    Consistency = FormatMetricsForChart(consistencyData)
                };

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting progress dashboard data");
                return StatusCode(500, new { error = "An error occurred while loading the dashboard data." });
            }
        }

        private object FormatMetricsForChart(IEnumerable<WorkoutMetric> metrics)
        {
            var labels = new List<string>();
            var data = new List<decimal>();
            
            foreach (var metric in metrics.OrderBy(m => m.Date))
            {
                labels.Add(metric.Date.ToString("yyyy-MM-dd"));
                data.Add(metric.Value);
            }
            
            return new
            {
                Labels = labels,
                Data = data
            };
        }
    }
}