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
                    _logger.LogInformation("OnGetDataAsync: Retrieved {count} volume data points", volumeData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving volume data");
                }

                try
                {
                    intensityData = (await _progressDashboardService.GetIntensitySeriesAsync(currentUser.UserId, StartDate, EndDate)).ToList();
                    _logger.LogInformation("OnGetDataAsync: Retrieved {count} intensity data points", intensityData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving intensity data");
                }

                try
                {
                    consistencyData = (await _progressDashboardService.GetConsistencySeriesAsync(currentUser.UserId, StartDate, EndDate)).ToList();
                    _logger.LogInformation("OnGetDataAsync: Retrieved {count} consistency data points", consistencyData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving consistency data");
                }

                // Define a concrete class for chart data that exactly matches what the client expects
                var chartData = new ChartDataViewModel
                {
                    Volume = CreateChartData(volumeData),
                    Intensity = CreateChartData(intensityData),
                    Consistency = CreateChartData(consistencyData)
                };

                // Log the data structure
                _logger.LogInformation("Chart data structure: Volume ({volumeLabels} labels, {volumeData} data points), " +
                                      "Intensity ({intensityLabels} labels, {intensityData} data points), " +
                                      "Consistency ({consistencyLabels} labels, {consistencyData} data points)",
                    chartData.Volume.Labels.Count, chartData.Volume.Data.Count, 
                    chartData.Intensity.Labels.Count, chartData.Intensity.Data.Count, 
                    chartData.Consistency.Labels.Count, chartData.Consistency.Data.Count);

                // Create serialization options that preserve property names exactly as they are
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = null, // Preserves property names as-is
                    WriteIndented = false // For production, keep responses compact
                };

                // Serialize the result to see exactly what is going to the client
                var serializedJson = System.Text.Json.JsonSerializer.Serialize(chartData, jsonOptions);
                _logger.LogInformation("Serialized JSON for client: {serializedJson}", serializedJson);

                stopwatch.Stop();
                _logger.LogInformation("OnGetDataAsync: Completed loading progress dashboard data in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);

                // Return the result as JSON with explicit serialization options
                return new JsonResult(chartData, jsonOptions);
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

        private ChartSeriesData CreateChartData(IEnumerable<WorkoutMetric> metrics)
        {
            var result = new ChartSeriesData
            {
                Labels = new List<string>(),
                Data = new List<decimal>()
            };
            
            // Ensure metrics is not null to prevent exceptions
            if (metrics == null)
            {
                _logger.LogWarning("CreateChartData: Metrics collection is null");
                // Add placeholder data point if no data exists
                result.Labels.Add(DateTime.Now.ToString("yyyy-MM-dd"));
                result.Data.Add(0);
                return result;
            }
            
            // Handle empty data gracefully
            if (!metrics.Any())
            {
                _logger.LogInformation("CreateChartData: No metrics available for chart");
                
                // Add placeholder data point if no data exists
                result.Labels.Add(DateTime.Now.ToString("yyyy-MM-dd"));
                result.Data.Add(0);
                return result;
            }
            
            foreach (var metric in metrics.OrderBy(m => m.Date))
            {
                result.Labels.Add(metric.Date.ToString("yyyy-MM-dd"));
                result.Data.Add(metric.Value);
            }
            
            return result;
        }

        // Concrete class for chart data that matches client expectations exactly
        public class ChartDataViewModel
        {
            public ChartSeriesData Volume { get; set; }
            public ChartSeriesData Intensity { get; set; }
            public ChartSeriesData Consistency { get; set; }
        }

        // Concrete class for chart series data
        public class ChartSeriesData
        {
            public List<string> Labels { get; set; }
            public List<decimal> Data { get; set; }
        }
    }
}