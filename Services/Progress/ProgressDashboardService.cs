using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Extensions;

namespace WorkoutTrackerWeb.Services.Progress
{
    public class ProgressDashboardService : IProgressDashboardService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<ProgressDashboardService> _logger;

        public ProgressDashboardService(WorkoutTrackerWebContext context, ILogger<ProgressDashboardService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<WorkoutMetric>> GetVolumeSeriesAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Get cached metrics from the database if they exist
                var cachedMetrics = await _context.WorkoutMetrics
                    .Where(m => m.UserId == userId)
                    .Where(m => m.MetricType == "Volume")
                    .Where(m => m.Date >= startDate.Date && m.Date <= endDate.Date)
                    .OrderBy(m => m.Date)
                    .ToListAsync();

                if (cachedMetrics.Any())
                {
                    _logger.LogInformation("Retrieved {Count} volume metrics from cache for user {UserId}", 
                        cachedMetrics.Count, userId);
                    return cachedMetrics;
                }

                // If no cached metrics are found, calculate them on the fly
                var volumeMetrics = await CalculateVolumeMetricsAsync(userId, startDate, endDate);
                _logger.LogInformation("Calculated {Count} volume metrics for user {UserId}", 
                    volumeMetrics.Count(), userId);
                
                return volumeMetrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving volume metrics for user {UserId}", userId);
                return Enumerable.Empty<WorkoutMetric>();
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<WorkoutMetric>> GetIntensitySeriesAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Get cached metrics from the database if they exist
                var cachedMetrics = await _context.WorkoutMetrics
                    .Where(m => m.UserId == userId)
                    .Where(m => m.MetricType == "Intensity")
                    .Where(m => m.Date >= startDate.Date && m.Date <= endDate.Date)
                    .OrderBy(m => m.Date)
                    .ToListAsync();

                if (cachedMetrics.Any())
                {
                    _logger.LogInformation("Retrieved {Count} intensity metrics from cache for user {UserId}", 
                        cachedMetrics.Count, userId);
                    return cachedMetrics;
                }

                // If no cached metrics are found, calculate them on the fly
                var intensityMetrics = await CalculateIntensityMetricsAsync(userId, startDate, endDate);
                _logger.LogInformation("Calculated {Count} intensity metrics for user {UserId}", 
                    intensityMetrics.Count(), userId);
                
                return intensityMetrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving intensity metrics for user {UserId}", userId);
                return Enumerable.Empty<WorkoutMetric>();
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<WorkoutMetric>> GetConsistencySeriesAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Get cached metrics from the database if they exist
                var cachedMetrics = await _context.WorkoutMetrics
                    .Where(m => m.UserId == userId)
                    .Where(m => m.MetricType == "Consistency")
                    .Where(m => m.Date >= startDate.Date && m.Date <= endDate.Date)
                    .OrderBy(m => m.Date)
                    .ToListAsync();

                if (cachedMetrics.Any())
                {
                    _logger.LogInformation("Retrieved {Count} consistency metrics from cache for user {UserId}", 
                        cachedMetrics.Count, userId);
                    return cachedMetrics;
                }

                // If no cached metrics are found, calculate them on the fly
                var consistencyMetrics = await CalculateConsistencyMetricsAsync(userId, startDate, endDate);
                _logger.LogInformation("Calculated {Count} consistency metrics for user {UserId}", 
                    consistencyMetrics.Count(), userId);
                
                return consistencyMetrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving consistency metrics for user {UserId}", userId);
                return Enumerable.Empty<WorkoutMetric>();
            }
        }

        /// <inheritdoc />
        public async Task<int> CalculateAndStoreMissingMetricsAsync()
        {
            try
            {
                // Get all completed sessions that don't have metrics calculated yet
                // We'll do this by checking which sessions have completion dates but no metrics in the WorkoutMetrics table
                var completedSessions = await _context.WorkoutSessions
                    .Where(s => s.IsCompleted && s.CompletedDate != null)
                    .ToListAsync();

                int count = 0;
                foreach (var session in completedSessions)
                {
                    // Check if metrics already exist for this session
                    var hasMetrics = await _context.WorkoutMetrics
                        .AnyAsync(m => m.UserId == session.UserId && 
                                       m.Date.Date == session.CompletedDate.Value.Date &&
                                       m.AdditionalData.Contains(session.WorkoutSessionId.ToString()));

                    if (!hasMetrics)
                    {
                        bool success = await CalculateAndStoreMetricsForSessionAsync(session.WorkoutSessionId);
                        if (success) count++;
                    }
                }

                _logger.LogInformation("Calculated metrics for {Count} sessions", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating missing metrics");
                return 0;
            }
        }

        /// <inheritdoc />
        public async Task<bool> CalculateAndStoreMetricsForSessionAsync(int workoutSessionId)
        {
            try
            {
                // Get the session with its exercises and sets
                var session = await _context.WorkoutSessions
                    .Include(s => s.WorkoutExercises)
                    .ThenInclude(e => e.WorkoutSets)
                    .FirstOrDefaultAsync(s => s.WorkoutSessionId == workoutSessionId);

                if (session == null || !session.IsCompleted || !session.CompletedDate.HasValue)
                {
                    _logger.LogWarning("Cannot calculate metrics for session {SessionId} - session is null or not completed", workoutSessionId);
                    return false;
                }

                // Calculate metrics for the session
                var date = session.CompletedDate.Value.Date;
                var userId = session.UserId;

                // Calculate volume metric
                decimal totalVolume = CalculateTotalVolumeForSession(session);
                var volumeMetric = new WorkoutMetric
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Date = date,
                    MetricType = "Volume",
                    Value = totalVolume,
                    AdditionalData = JsonSerializer.Serialize(new { SessionIds = new[] { workoutSessionId } }),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Calculate intensity metric
                decimal averageIntensity = CalculateAverageIntensityForSession(session);
                var intensityMetric = new WorkoutMetric
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Date = date,
                    MetricType = "Intensity",
                    Value = averageIntensity,
                    AdditionalData = JsonSerializer.Serialize(new { SessionIds = new[] { workoutSessionId } }),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // For consistency metric, we need to check if a metric already exists for this week
                DateTime startOfWeek = date.StartOfWeek(DayOfWeek.Monday);
                DateTime endOfWeek = startOfWeek.AddDays(6);

                var existingConsistencyMetric = await _context.WorkoutMetrics
                    .FirstOrDefaultAsync(m => m.UserId == userId && 
                                           m.MetricType == "Consistency" && 
                                           m.Date >= startOfWeek &&
                                           m.Date <= endOfWeek);

                if (existingConsistencyMetric != null)
                {
                    // Update existing consistency metric
                    var additionalData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        existingConsistencyMetric.AdditionalData ?? "{}");
                    
                    var sessionIds = new List<int>();
                    if (additionalData.TryGetValue("SessionIds", out var sessionIdsObj))
                    {
                        sessionIds = JsonSerializer.Deserialize<List<int>>(sessionIdsObj.ToString());
                    }
                    
                    if (!sessionIds.Contains(workoutSessionId))
                    {
                        sessionIds.Add(workoutSessionId);
                        existingConsistencyMetric.Value = sessionIds.Count;
                        existingConsistencyMetric.AdditionalData = JsonSerializer.Serialize(
                            new { SessionIds = sessionIds });
                        existingConsistencyMetric.UpdatedAt = DateTime.UtcNow;
                        _context.WorkoutMetrics.Update(existingConsistencyMetric);
                    }
                }
                else
                {
                    // Create new consistency metric for this week
                    var consistencyMetric = new WorkoutMetric
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Date = startOfWeek, // Use start of week for consistency metrics
                        MetricType = "Consistency",
                        Value = 1, // First workout of the week
                        AdditionalData = JsonSerializer.Serialize(new { SessionIds = new[] { workoutSessionId } }),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.WorkoutMetrics.Add(consistencyMetric);
                }

                // Try to merge with existing metrics for the same day if possible
                await MergeOrAddMetricAsync(volumeMetric);
                await MergeOrAddMetricAsync(intensityMetric);
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Calculated and stored metrics for session {SessionId}", workoutSessionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating metrics for session {SessionId}", workoutSessionId);
                return false;
            }
        }

        #region Private Helper Methods

        private async Task MergeOrAddMetricAsync(WorkoutMetric metric)
        {
            // Check if a metric already exists for this user, date, and type
            var existingMetric = await _context.WorkoutMetrics
                .FirstOrDefaultAsync(m => m.UserId == metric.UserId && 
                                       m.Date.Date == metric.Date.Date && 
                                       m.MetricType == metric.MetricType);

            if (existingMetric != null)
            {
                // Merge with existing metric
                var additionalDataObj = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    existingMetric.AdditionalData ?? "{}");
                
                var newDataObj = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    metric.AdditionalData ?? "{}");
                
                if (additionalDataObj.TryGetValue("SessionIds", out var existingSessionIdsObj) &&
                    newDataObj.TryGetValue("SessionIds", out var newSessionIdsObj))
                {
                    var existingSessionIds = JsonSerializer.Deserialize<List<int>>(existingSessionIdsObj.ToString());
                    var newSessionIds = JsonSerializer.Deserialize<List<int>>(newSessionIdsObj.ToString());
                    
                    // Combine session IDs
                    foreach (var sessionId in newSessionIds)
                    {
                        if (!existingSessionIds.Contains(sessionId))
                        {
                            existingSessionIds.Add(sessionId);
                        }
                    }
                    
                    additionalDataObj["SessionIds"] = existingSessionIds;
                }
                
                // For volume, we sum the values
                if (metric.MetricType == "Volume")
                {
                    existingMetric.Value += metric.Value;
                }
                // For intensity, we take the average
                else if (metric.MetricType == "Intensity")
                {
                    existingMetric.Value = (existingMetric.Value + metric.Value) / 2;
                }
                // For consistency, we would have already handled this in the calling method
                
                existingMetric.AdditionalData = JsonSerializer.Serialize(additionalDataObj);
                existingMetric.UpdatedAt = DateTime.UtcNow;
                _context.WorkoutMetrics.Update(existingMetric);
            }
            else
            {
                // Add new metric
                _context.WorkoutMetrics.Add(metric);
            }
        }

        private async Task<IEnumerable<WorkoutMetric>> CalculateVolumeMetricsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            var result = new List<WorkoutMetric>();
            
            // Get all completed workout sessions in the date range
            var sessions = await _context.WorkoutSessions
                .Include(s => s.WorkoutExercises)
                .ThenInclude(e => e.WorkoutSets)
                .Where(s => s.UserId == userId)
                .Where(s => s.IsCompleted && s.CompletedDate.HasValue)
                .Where(s => s.CompletedDate >= startDate && s.CompletedDate <= endDate)
                .ToListAsync();

            // Group sessions by date
            var sessionsByDate = sessions
                .GroupBy(s => s.CompletedDate.Value.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Create a metric for each date
            foreach (var date in sessionsByDate.Keys.OrderBy(d => d))
            {
                var dailySessions = sessionsByDate[date];
                decimal totalVolume = 0;
                var sessionIds = new List<int>();
                
                foreach (var session in dailySessions)
                {
                    totalVolume += CalculateTotalVolumeForSession(session);
                    sessionIds.Add(session.WorkoutSessionId);
                }
                
                var metric = new WorkoutMetric
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Date = date,
                    MetricType = "Volume",
                    Value = totalVolume,
                    AdditionalData = JsonSerializer.Serialize(new { SessionIds = sessionIds }),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                result.Add(metric);
                
                // Cache this metric in the database for future use
                await _context.WorkoutMetrics.AddAsync(metric);
            }
            
            await _context.SaveChangesAsync();
            return result;
        }

        private async Task<IEnumerable<WorkoutMetric>> CalculateIntensityMetricsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            var result = new List<WorkoutMetric>();
            
            // Get all completed workout sessions in the date range
            var sessions = await _context.WorkoutSessions
                .Include(s => s.WorkoutExercises)
                .ThenInclude(e => e.WorkoutSets)
                .Where(s => s.UserId == userId)
                .Where(s => s.IsCompleted && s.CompletedDate.HasValue)
                .Where(s => s.CompletedDate >= startDate && s.CompletedDate <= endDate)
                .ToListAsync();

            // Group sessions by date
            var sessionsByDate = sessions
                .GroupBy(s => s.CompletedDate.Value.Date)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Create a metric for each date
            foreach (var date in sessionsByDate.Keys.OrderBy(d => d))
            {
                var dailySessions = sessionsByDate[date];
                decimal totalIntensity = 0;
                int sessionCount = 0;
                var sessionIds = new List<int>();
                
                foreach (var session in dailySessions)
                {
                    decimal sessionIntensity = CalculateAverageIntensityForSession(session);
                    if (sessionIntensity > 0)
                    {
                        totalIntensity += sessionIntensity;
                        sessionCount++;
                        sessionIds.Add(session.WorkoutSessionId);
                    }
                }
                
                decimal averageIntensity = sessionCount > 0 ? totalIntensity / sessionCount : 0;
                
                var metric = new WorkoutMetric
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Date = date,
                    MetricType = "Intensity",
                    Value = averageIntensity,
                    AdditionalData = JsonSerializer.Serialize(new { SessionIds = sessionIds }),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                result.Add(metric);
                
                // Cache this metric in the database for future use
                await _context.WorkoutMetrics.AddAsync(metric);
            }
            
            await _context.SaveChangesAsync();
            return result;
        }

        private async Task<IEnumerable<WorkoutMetric>> CalculateConsistencyMetricsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            var result = new List<WorkoutMetric>();
            
            // Get all completed workout sessions in the date range
            var sessions = await _context.WorkoutSessions
                .Where(s => s.UserId == userId)
                .Where(s => s.IsCompleted && s.CompletedDate.HasValue)
                .Where(s => s.CompletedDate >= startDate && s.CompletedDate <= endDate)
                .ToListAsync();

            // Group sessions by week (start of week = Monday)
            var sessionsByWeek = sessions
                .GroupBy(s => s.CompletedDate.Value.Date.StartOfWeek(DayOfWeek.Monday))
                .ToDictionary(g => g.Key, g => g.ToList());

            // Create a metric for each week
            foreach (var weekStart in sessionsByWeek.Keys.OrderBy(d => d))
            {
                var weeklySessions = sessionsByWeek[weekStart];
                var sessionIds = weeklySessions.Select(s => s.WorkoutSessionId).ToList();
                
                var metric = new WorkoutMetric
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Date = weekStart,
                    MetricType = "Consistency",
                    Value = weeklySessions.Count,
                    AdditionalData = JsonSerializer.Serialize(new { SessionIds = sessionIds }),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                result.Add(metric);
                
                // Cache this metric in the database for future use
                await _context.WorkoutMetrics.AddAsync(metric);
            }
            
            await _context.SaveChangesAsync();
            return result;
        }

        private decimal CalculateTotalVolumeForSession(WorkoutSession session)
        {
            decimal totalVolume = 0;
            
            foreach (var exercise in session.WorkoutExercises)
            {
                foreach (var set in exercise.WorkoutSets)
                {
                    // Volume = Weight × Reps (for resistance exercises)
                    // For cardio exercises, we could use duration or distance instead
                    decimal weight = set.Weight ?? 0;
                    int reps = set.Reps ?? 0;
                    totalVolume += weight * reps;
                }
            }
            
            return totalVolume;
        }

        private decimal CalculateAverageIntensityForSession(WorkoutSession session)
        {
            decimal totalIntensity = 0;
            int setCount = 0;
            
            foreach (var exercise in session.WorkoutExercises)
            {
                foreach (var set in exercise.WorkoutSets)
                {
                    // Intensity = Weight per rep
                    decimal weight = set.Weight ?? 0;
                    int reps = set.Reps ?? 0;
                    
                    if (reps > 0 && weight > 0)
                    {
                        totalIntensity += weight;
                        setCount++;
                    }
                }
            }
            
            return setCount > 0 ? totalIntensity / setCount : 0;
        }

        #endregion
    }

    // Extension method for DateTime to get start of week
    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
    }
}