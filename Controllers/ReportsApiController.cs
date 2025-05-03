using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Reports;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsApiController : ControllerBase
    {
        private readonly IReportsService _reportsService;
        private readonly UserService _userService;
        private readonly ILogger<ReportsApiController> _logger;

        public ReportsApiController(
            IReportsService reportsService,
            UserService userService,
            ILogger<ReportsApiController> logger)
        {
            _reportsService = reportsService;
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("weight-progress")]
        public async Task<IActionResult> GetWeightProgress(int days = 90, int limit = 5)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var result = await _reportsService.GetWeightProgressAsync(userId.Value, days, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weight progress");
                return StatusCode(500, "An error occurred while retrieving weight progress data");
            }
        }

        [HttpGet("exercise-status")]
        public async Task<IActionResult> GetExerciseStatus(int days = 90, int limit = 10)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var result = await _reportsService.GetExerciseStatusAsync(userId.Value, days, limit);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exercise status");
                return StatusCode(500, "An error occurred while retrieving exercise status data");
            }
        }

        [HttpGet("volume-over-time")]
        public async Task<IActionResult> GetVolumeOverTime(int days = 90)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                var endDate = DateTime.UtcNow;
                var result = await _reportsService.GetVolumeOverTimeAsync(userId.Value, startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting volume over time");
                return StatusCode(500, "An error occurred while retrieving volume data");
            }
        }

        [HttpGet("workout-distribution")]
        public async Task<IActionResult> GetWorkoutDistribution(int days = 90)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                var endDate = DateTime.UtcNow;
                var byDay = await _reportsService.GetWorkoutsByDayOfWeekAsync(userId.Value, startDate, endDate);
                var byHour = await _reportsService.GetWorkoutsByHourAsync(userId.Value, startDate, endDate);
                
                return Ok(new { byDay, byHour });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workout distribution");
                return StatusCode(500, "An error occurred while retrieving workout distribution data");
            }
        }

        [HttpGet("exercise-distribution")]
        public async Task<IActionResult> GetExerciseDistribution(int days = 90)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                var endDate = DateTime.UtcNow;
                var byMuscleGroup = await _reportsService.GetExerciseDistributionByMuscleGroupAsync(userId.Value, startDate, endDate);
                var byType = await _reportsService.GetExerciseDistributionByTypeAsync(userId.Value, startDate, endDate);
                
                return Ok(new { byMuscleGroup, byType });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exercise distribution");
                return StatusCode(500, "An error occurred while retrieving exercise distribution data");
            }
        }

        [HttpGet("dashboard-metrics")]
        public async Task<IActionResult> GetDashboardMetrics(int days = 90)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                var metrics = await _reportsService.GetUserMetricsAsync(userId.Value, days);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard metrics");
                return StatusCode(500, "An error occurred while retrieving dashboard metrics");
            }
        }
    }
}