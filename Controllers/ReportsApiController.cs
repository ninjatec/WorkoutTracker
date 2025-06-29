using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Reports;
using WorkoutTrackerWeb.Services.Redis;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Attributes;
using System.Threading;

namespace WorkoutTrackerWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsApiController : ApiBaseController
    {
        private readonly IReportsService _reportsService;
        private readonly UserService _userService;
        private readonly ILogger<ReportsApiController> _logger;
        private readonly IResilientCacheService _cacheService;
        
        // Default cache duration of 15 minutes for report data
        private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(15);
        
        // Timeout for large dataset queries (longer for calorie data)
        private static readonly TimeSpan VolumeQueryTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan CalorieQueryTimeout = TimeSpan.FromSeconds(20);

        public ReportsApiController(
            IReportsService reportsService,
            UserService userService,
            IResilientCacheService cacheService,
            ILogger<ReportsApiController> logger)
        {
            _reportsService = reportsService;
            _userService = userService;
            _logger = logger;
            _cacheService = cacheService;
        }

        [HttpGet("weight-progress")]
        [ETag(cacheDurationSeconds: 900)] // 15 minutes cache duration
        public async Task<IActionResult> GetWeightProgress(int days = 90, int limit = 5)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return UnauthorizedResponse<object>();
            }

            try
            {
                string cacheKey = $"reports:weight-progress:{userId}:{days}:{limit}";
                
                var result = await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () => await _reportsService.GetWeightProgressAsync(userId.Value, days, limit),
                    DefaultCacheDuration);
                
                var metadata = new Dictionary<string, object>
                {
                    { "days", days },
                    { "limit", limit },
                    { "generatedAt", DateTime.UtcNow }
                };
                    
                return SuccessResponse(result, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weight progress for user {UserId} with days={Days}, limit={Limit}", userId, days, limit);
                return ErrorResponse<object>("Unable to load weight progress data", 500, 
                    new Dictionary<string, object> { { "suggestion", "Please try with a shorter time period" } });
            }
        }

        [HttpGet("exercise-status")]
        [ETag(cacheDurationSeconds: 900)] // 15 minutes cache duration
        public async Task<IActionResult> GetExerciseStatus(int days = 90, int limit = 10)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return UnauthorizedResponse<object>();
            }

            try
            {
                string cacheKey = $"reports:exercise-status:{userId}:{days}:{limit}";
                
                var result = await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () => await _reportsService.GetExerciseStatusAsync(userId.Value, days, limit),
                    DefaultCacheDuration);
                
                var metadata = new Dictionary<string, object>
                {
                    { "days", days },
                    { "limit", limit },
                    { "generatedAt", DateTime.UtcNow }
                };
                    
                return SuccessResponse(result, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exercise status for user {UserId} with days={Days}, limit={Limit}", userId, days, limit);
                return ErrorResponse<object>("Unable to load exercise status data", 500, 
                    new Dictionary<string, object> { { "suggestion", "Please try with a shorter time period" } });
            }
        }

        [HttpGet("volume-over-time")]
        [ETag(cacheDurationSeconds: 900)] // 15 minutes cache duration
        public async Task<IActionResult> GetVolumeOverTime(int days = 90)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return UnauthorizedResponse<object>();
            }

            try
            {
                string cacheKey = $"reports:volume-over-time:{userId}:{days}";
                
                // For larger date ranges, use a longer cache duration
                var cacheDuration = days > 60 ? TimeSpan.FromHours(2) : DefaultCacheDuration;
                
                // Use token to limit query execution time
                using var cts = new CancellationTokenSource(VolumeQueryTimeout);
                
                var result = await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () => 
                    {
                        var startDate = DateTime.UtcNow.AddDays(-days);
                        var endDate = DateTime.UtcNow;
                        return await _reportsService.GetVolumeOverTimeAsync(userId.Value, startDate, endDate, cts.Token);
                    },
                    cacheDuration);
                
                var metadata = new Dictionary<string, object>
                {
                    { "days", days },
                    { "startDate", DateTime.UtcNow.AddDays(-days) },
                    { "endDate", DateTime.UtcNow },
                    { "dataPoints", result != null ? result.Count : 0 },
                    { "generatedAt", DateTime.UtcNow }
                };
                    
                return SuccessResponse(result, metadata);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Volume data query timed out for user {UserId} with days={Days}", userId, days);
                return ErrorResponse<object>("Unable to load volume chart data", 500, 
                    new Dictionary<string, object> { { "suggestion", "Please try with a shorter time period" } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting volume over time for user {UserId} with days={Days}", userId, days);
                return ErrorResponse<object>("Unable to load volume chart data", 500, 
                    new Dictionary<string, object> { { "suggestion", "Please try with a shorter time period" } });
            }
        }

        [HttpGet("workout-distribution")]
        [ETag(cacheDurationSeconds: 1800)] // 30 minutes cache duration
        public async Task<IActionResult> GetWorkoutDistribution(int days = 90)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return UnauthorizedResponse<object>();
            }

            try
            {
                string cacheKey = $"reports:workout-distribution:{userId}:{days}";
                
                var result = await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () => 
                    {
                        var startDate = DateTime.UtcNow.AddDays(-days);
                        var endDate = DateTime.UtcNow;
                        var byDay = await _reportsService.GetWorkoutsByDayOfWeekAsync(userId.Value, startDate, endDate);
                        var byHour = await _reportsService.GetWorkoutsByHourAsync(userId.Value, startDate, endDate);
                        
                        return new { byDay, byHour };
                    },
                    DefaultCacheDuration);
                
                var metadata = new Dictionary<string, object>
                {
                    { "days", days },
                    { "startDate", DateTime.UtcNow.AddDays(-days) },
                    { "endDate", DateTime.UtcNow },
                    { "generatedAt", DateTime.UtcNow }
                };
                    
                return SuccessResponse(result, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workout distribution for user {UserId} with days={Days}", userId, days);
                return ErrorResponse<object>("Unable to load workout distribution data", 500, 
                    new Dictionary<string, object> { { "suggestion", "Please try with a shorter time period" } });
            }
        }

        [HttpGet("exercise-distribution")]
        [ETag(cacheDurationSeconds: 1800)] // 30 minutes cache duration
        public async Task<IActionResult> GetExerciseDistribution(int days = 90)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return UnauthorizedResponse<object>();
            }

            try
            {
                string cacheKey = $"reports:exercise-distribution:{userId}:{days}";
                
                var result = await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () => 
                    {
                        var startDate = DateTime.UtcNow.AddDays(-days);
                        var endDate = DateTime.UtcNow;
                        var byMuscleGroup = await _reportsService.GetExerciseDistributionByMuscleGroupAsync(userId.Value, startDate, endDate);
                        var byType = await _reportsService.GetExerciseDistributionByTypeAsync(userId.Value, startDate, endDate);
                        
                        return new { byMuscleGroup, byType };
                    },
                    DefaultCacheDuration);
                
                var metadata = new Dictionary<string, object>
                {
                    { "days", days },
                    { "startDate", DateTime.UtcNow.AddDays(-days) },
                    { "endDate", DateTime.UtcNow },
                    { "generatedAt", DateTime.UtcNow }
                };
                    
                return SuccessResponse(result, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exercise distribution for user {UserId} with days={Days}", userId, days);
                return ErrorResponse<object>("Unable to load exercise distribution data", 500, 
                    new Dictionary<string, object> { { "suggestion", "Please try again later" } });
            }
        }

        [HttpGet("dashboard-metrics")]
        [ETag(cacheDurationSeconds: 900)] // 15 minutes cache duration
        public async Task<IActionResult> GetDashboardMetrics(int days = 90)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return UnauthorizedResponse<object>();
            }

            try
            {
                string cacheKey = $"reports:dashboard-metrics:{userId}:{days}";
                
                // For calorie data, use a longer cache duration for large ranges
                var cacheDuration = days > 60 ? TimeSpan.FromHours(3) : DefaultCacheDuration;
                
                // Use cancellation token to prevent long-running queries
                using var cts = new CancellationTokenSource(CalorieQueryTimeout);
                
                var result = await _cacheService.GetOrCreateAsync(
                    cacheKey,
                    async () => await _reportsService.GetUserMetricsAsync(userId.Value, days, cts.Token),
                    cacheDuration);
                
                var metadata = new Dictionary<string, object>
                {
                    { "days", days },
                    { "startDate", DateTime.UtcNow.AddDays(-days) },
                    { "endDate", DateTime.UtcNow },
                    { "generatedAt", DateTime.UtcNow }
                };
                    
                return SuccessResponse(result, metadata);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Dashboard metrics query timed out for user {UserId} with days={Days}", userId, days);
                return ErrorResponse<object>("Unable to load calorie chart data", 500, 
                    new Dictionary<string, object> { { "suggestion", "Please try with a shorter time period" } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard metrics for user {UserId} with days={Days}", userId, days);
                return ErrorResponse<object>("Unable to load dashboard metrics", 500, 
                    new Dictionary<string, object> { { "suggestion", "Please try with a shorter time period" } });
            }
        }
        
        [HttpGet("clear-cache")]
        [Authorize(Roles = "Admin,Coach")]
        public async Task<IActionResult> ClearReportCache(int? userId = null)
        {
            try
            {
                // If userId is provided, clear cache for specific user
                var operationMetadata = new Dictionary<string, object>();
                
                if (userId.HasValue)
                {
                    await _cacheService.RemoveAsync($"reports:weight-progress:{userId}:*");
                    await _cacheService.RemoveAsync($"reports:exercise-status:{userId}:*");
                    await _cacheService.RemoveAsync($"reports:volume-over-time:{userId}:*");
                    await _cacheService.RemoveAsync($"reports:workout-distribution:{userId}:*");
                    await _cacheService.RemoveAsync($"reports:exercise-distribution:{userId}:*");
                    await _cacheService.RemoveAsync($"reports:dashboard-metrics:{userId}:*");
                    
                    _logger.LogInformation("Cleared report cache for user {UserId}", userId);
                    operationMetadata.Add("userId", userId);
                    operationMetadata.Add("action", "clear cache");
                    operationMetadata.Add("clearTime", DateTime.UtcNow);
                    
                    return SuccessResponse(new { message = $"Cache cleared for user {userId}" }, operationMetadata);
                }
                else
                {
                    // For admin, just get current user's cache cleared
                    var currentUserId = await _userService.GetCurrentUserIdAsync();
                    if (currentUserId.HasValue)
                    {
                        await _cacheService.RemoveAsync($"reports:weight-progress:{currentUserId}:*");
                        await _cacheService.RemoveAsync($"reports:exercise-status:{currentUserId}:*");
                        await _cacheService.RemoveAsync($"reports:volume-over-time:{currentUserId}:*");
                        await _cacheService.RemoveAsync($"reports:workout-distribution:{currentUserId}:*");
                        await _cacheService.RemoveAsync($"reports:exercise-distribution:{currentUserId}:*");
                        await _cacheService.RemoveAsync($"reports:dashboard-metrics:{currentUserId}:*");
                        
                        _logger.LogInformation("Cleared report cache for current user {UserId}", currentUserId);
                        operationMetadata.Add("userId", currentUserId);
                        operationMetadata.Add("action", "clear cache");
                        operationMetadata.Add("clearTime", DateTime.UtcNow);
                        
                        return SuccessResponse(new { message = "Your report cache has been cleared" }, operationMetadata);
                    }
                }
                
                return BadRequestResponse<object>("No user ID provided");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing report cache for userId={UserId}", userId);
                return ErrorResponse<object>("Unable to clear cache", 500, 
                    new Dictionary<string, object> { { "suggestion", "Please try again later" } });
            }
        }
    }
}