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
using System.Diagnostics;
using Microsoft.Data.SqlClient;

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
                    _logger.LogWarning("OnGetAsync: User not found");
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
                    _logger.LogInformation("OnGetAsync: Fixing invalid date range - end date is before start date");
                    EndDate = StartDate.AddMonths(3);
                }

                // Don't allow ranges longer than 1 year
                if ((EndDate - StartDate).TotalDays > 365)
                {
                    _logger.LogInformation("OnGetAsync: Limiting date range to 1 year");
                    EndDate = StartDate.AddYears(1);
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading progress dashboard");
                StatusMessage = "Error: An error occurred while loading the dashboard. Please try refreshing the page.";
                return Page();
            }
        }

        [OutputCache(Duration = 300, VaryByQueryKeys = new[] { "start", "end" })]
        public async Task<IActionResult> OnGetDataAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("OnGetDataAsync: Loading progress dashboard data");
                
                // Get the current user
                var currentUser = await _context.GetCurrentUserAsync();
                if (currentUser == null)
                {
                    _logger.LogWarning("OnGetDataAsync: User not found");
                    return NotFound(new { error = "User not found." });
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
                    _logger.LogInformation("OnGetDataAsync: Fixing invalid date range - end date is before start date");
                    EndDate = StartDate.AddMonths(3);
                }

                // Don't allow ranges longer than 1 year
                if ((EndDate - StartDate).TotalDays > 365)
                {
                    _logger.LogInformation("OnGetDataAsync: Limiting date range to 1 year");
                    EndDate = StartDate.AddYears(1);
                }

                // Calculate metrics that need to be calculated
                try
                {
                    await _progressDashboardService.CalculateAndStoreMissingMetricsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating missing metrics");
                    // Continue to retrieve existing metrics - don't fail completely
                }

                // Get metrics from the service - handle each independently to prevent total failure
                var volumeData = new List<WorkoutMetric>();
                var intensityData = new List<WorkoutMetric>();
                var consistencyData = new List<WorkoutMetric>();

                try
                {
                    volumeData = (await _progressDashboardService.GetVolumeSeriesAsync(currentUser.UserId, StartDate, EndDate)).ToList();
                    _logger.LogDebug("OnGetDataAsync: Retrieved {count} volume data points", volumeData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving volume data");
                }

                try
                {
                    intensityData = (await _progressDashboardService.GetIntensitySeriesAsync(currentUser.UserId, StartDate, EndDate)).ToList();
                    _logger.LogDebug("OnGetDataAsync: Retrieved {count} intensity data points", intensityData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving intensity data");
                }

                try
                {
                    consistencyData = (await _progressDashboardService.GetConsistencySeriesAsync(currentUser.UserId, StartDate, EndDate)).ToList();
                    _logger.LogDebug("OnGetDataAsync: Retrieved {count} consistency data points", consistencyData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving consistency data");
                }

                // Format data for charts
                var result = new
                {
                    Volume = FormatMetricsForChart(volumeData),
                    Intensity = FormatMetricsForChart(intensityData),
                    Consistency = FormatMetricsForChart(consistencyData)
                };

                stopwatch.Stop();
                _logger.LogInformation("OnGetDataAsync: Completed loading progress dashboard data in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);

                return new JsonResult(result);
            }
            catch (SqlException sqlEx)
            {
                stopwatch.Stop();
                _logger.LogError(sqlEx, "Database error getting progress dashboard data. Duration: {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return StatusCode(500, new { error = "A database error occurred. Our team has been notified." });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error getting progress dashboard data. Duration: {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
                return StatusCode(500, new { error = "An error occurred while loading the dashboard data." });
            }
        }

        private object FormatMetricsForChart(IEnumerable<WorkoutMetric> metrics)
        {
            var labels = new List<string>();
            var data = new List<decimal>();
            
            // Ensure metrics is not null to prevent exceptions
            if (metrics == null)
            {
                _logger.LogWarning("FormatMetricsForChart: Metrics collection is null");
                return new
                {
                    Labels = Array.Empty<string>(),
                    Data = Array.Empty<decimal>()
                };
            }
            
            // Handle empty data gracefully
            if (!metrics.Any())
            {
                _logger.LogInformation("FormatMetricsForChart: No metrics available for chart");
                
                // Add placeholder data point if no data exists
                labels.Add(DateTime.Now.ToString("yyyy-MM-dd"));
                data.Add(0);
                
                return new
                {
                    Labels = labels,
                    Data = data
                };
            }
            
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