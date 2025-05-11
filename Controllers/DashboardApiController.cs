using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Dashboard;
using WorkoutTrackerWeb.Services.Export;

namespace WorkoutTrackerWeb.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize]
    public class DashboardApiController : ApiBaseController
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IDashboardService _dashboardService;
        private readonly IPdfExportService _pdfExportService;
        private readonly ICsvExportService _csvExportService;
        private readonly IUserService _userService;
        private readonly ILogger<DashboardApiController> _logger;

        public DashboardApiController(
            WorkoutTrackerWebContext context,
            IDashboardService dashboardService,
            IPdfExportService pdfExportService,
            ICsvExportService csvExportService,
            IUserService userService,
            ILogger<DashboardApiController> logger)
        {
            _context = context;
            _dashboardService = dashboardService;
            _pdfExportService = pdfExportService;
            _csvExportService = csvExportService;
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("export/pdf")]
        public async Task<IActionResult> ExportPdf([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                // Get current user
                var user = await _context.GetCurrentUserAsync();
                if (user == null)
                {
                    return Unauthorized();
                }

                // Get dashboard data
                var metrics = await _dashboardService.GetDashboardMetricsAsync(user.UserId, startDate, endDate);
                var volumeProgress = await _dashboardService.GetVolumeProgressChartDataAsync(user.UserId, startDate, endDate);
                var workoutFrequency = await _dashboardService.GetWorkoutFrequencyChartDataAsync(user.UserId, startDate, endDate);
                var personalBests = await _dashboardService.GetPersonalBestsAsync(user.UserId, startDate, endDate);

                // Generate PDF
                var pdfBytes = await _pdfExportService.GenerateDashboardPdfAsync(
                    user.Name,
                    startDate,
                    endDate,
                    metrics,
                    volumeProgress.ToList(),
                    workoutFrequency.ToList(),
                    personalBests.ToList());

                // Return PDF file
                string fileName = $"workout_dashboard_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting dashboard to PDF");
                return StatusCode(500, "Error generating PDF export");
            }
        }

        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportCsv([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                // Get current user
                var user = await _context.GetCurrentUserAsync();
                if (user == null)
                {
                    return Unauthorized();
                }

                // Get dashboard data
                var metrics = await _dashboardService.GetDashboardMetricsAsync(user.UserId, startDate, endDate);
                var volumeProgress = await _dashboardService.GetVolumeProgressChartDataAsync(user.UserId, startDate, endDate);
                var workoutFrequency = await _dashboardService.GetWorkoutFrequencyChartDataAsync(user.UserId, startDate, endDate);
                var personalBests = await _dashboardService.GetPersonalBestsAsync(user.UserId, startDate, endDate);

                // Generate CSV
                var csvBytes = await _csvExportService.GenerateDashboardCsvAsync(
                    user.Name,
                    startDate,
                    endDate,
                    metrics,
                    volumeProgress.ToList(),
                    workoutFrequency.ToList(),
                    personalBests.ToList());

                // Return CSV file
                string fileName = $"workout_dashboard_{startDate:yyyy-MM-dd}_to_{endDate:yyyy-MM-dd}.csv";
                return File(csvBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting dashboard to CSV");
                return StatusCode(500, "Error generating CSV export");
            }
        }
    }
}
