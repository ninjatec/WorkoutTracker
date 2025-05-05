using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.ViewModels;

namespace WorkoutTrackerWeb.Services.Reports
{
    // Add this missing extension class
    internal static class DateTimeExtensions
    {
        public static DateTime GetStartOfWeek(DateTime dt)
        {
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
            return dt.Date.AddDays(-1 * diff);
        }
    }

    public class ReportsService : IReportsService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<ReportsService> _logger;

        public ReportsService(
            WorkoutTrackerWebContext context,
            ILogger<ReportsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ReportsData> GetReportsDataAsync(int userId, int period = 90, int pageNumber = 1)
        {
            var result = new ReportsData
            {
                CurrentPage = pageNumber
            };

            try
            {
                var prData = await GetPersonalRecordsAsync(userId, pageNumber, 10);
                result.PersonalRecords = prData.Records;
                result.TotalPages = prData.TotalPages;

                var statusData = await GetExerciseStatusAsync(userId, period);
                result.ExerciseStatusList = statusData.AllExercises.Select(e => new ExerciseStatus
                {
                    ExerciseId = e.ExerciseId,
                    ExerciseName = e.ExerciseName,
                    SuccessReps = e.SuccessReps,
                    FailedReps = e.FailedReps
                }).ToList();
                
                result.RecentExerciseStatusList = statusData.TopExercises.Select(e => new RecentExerciseStatus
                {
                    ExerciseName = e.ExerciseName,
                    LastPerformed = e.LastPerformed
                }).ToList();
                
                result.SuccessReps = statusData.TotalSuccess;
                result.FailedReps = statusData.TotalFailed;
                
                result.TotalReps = result.SuccessReps + result.FailedReps;
                
                var startDate = DateTime.UtcNow.AddDays(-period);
                result.TotalSessions = await _context.WorkoutSessions
                    .Where(s => s.UserId == userId && s.StartDateTime >= startDate)
                    .CountAsync();
                
                result.TotalSets = await _context.WorkoutExercises
                    .Where(e => e.WorkoutSession.UserId == userId && e.WorkoutSession.StartDateTime >= startDate)
                    .SelectMany(e => e.WorkoutSets)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reports data for user {UserId}", userId);
            }
            
            return result;
        }

        public async Task<Dictionary<string, int>> GetExerciseDistributionByMuscleGroupAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var distribution = await _context.WorkoutExercises
                    .Where(e => e.WorkoutSession != null && 
                                e.WorkoutSession.UserId == userId && 
                                e.WorkoutSession.StartDateTime >= startDate &&
                                e.WorkoutSession.StartDateTime <= endDate &&
                                e.ExerciseType != null)
                    .GroupBy(e => e.ExerciseType.PrimaryMuscleGroup)
                    .Select(g => new
                    {
                        MuscleGroup = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.MuscleGroup ?? "Unknown", x => x.Count);
                
                return distribution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exercise distribution by muscle group for user {UserId}", userId);
                return new Dictionary<string, int>();
            }
        }

        public async Task<Dictionary<string, int>> GetExerciseDistributionByTypeAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var distribution = await _context.WorkoutExercises
                    .Where(e => e.WorkoutSession != null &&
                                e.WorkoutSession.UserId == userId && 
                                e.WorkoutSession.StartDateTime >= startDate &&
                                e.WorkoutSession.StartDateTime <= endDate &&
                                e.ExerciseType != null)
                    .GroupBy(e => e.ExerciseType.Type)
                    .Select(g => new
                    {
                        Type = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.Type ?? "Unknown", x => x.Count);
                
                return distribution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exercise distribution by type for user {UserId}", userId);
                return new Dictionary<string, int>();
            }
        }

        public async Task<Dictionary<string, int>> GetExerciseFrequencyAsync(int userId, DateTime startDate, DateTime endDate, int limit = 10)
        {
            try
            {
                var exercises = await _context.WorkoutExercises
                    .Where(e => e.WorkoutSession != null && 
                                e.WorkoutSession.UserId == userId && 
                                e.WorkoutSession.StartDateTime >= startDate &&
                                e.WorkoutSession.StartDateTime <= endDate &&
                                e.ExerciseType != null)
                    .Select(e => new 
                    {
                        ExerciseName = e.ExerciseType.Name
                    })
                    .ToListAsync();
                    
                return exercises
                    .Where(x => !string.IsNullOrEmpty(x.ExerciseName))
                    .GroupBy(x => x.ExerciseName)
                    .Select(g => new
                    {
                        ExerciseName = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(limit)
                    .ToDictionary(x => x.ExerciseName, x => x.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exercise frequency for user {UserId}", userId);
                return new Dictionary<string, int>();
            }
        }

        public async Task<List<WorkoutSession>> GetRecentWorkoutSessionsAsync(int userId, int count = 10)
        {
            try
            {
                // Use the compiled query instead of building a new query each time
                return await Services.Database.CompiledQueries.GetRecentWorkoutSessionsAsync(_context, userId, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent workout sessions for user {UserId}", userId);
                return new List<WorkoutSession>();
            }
        }

        public async Task<Dictionary<DateTime, decimal>> GetVolumeOverTimeAsync(int userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting volume over time for user {UserId} from {StartDate} to {EndDate}", 
                    userId, startDate, endDate);
                
                // Calculate date span to determine appropriate aggregation
                var dateSpan = (endDate - startDate).TotalDays;
                _logger.LogDebug("Date span for volume data is {DateSpan} days", dateSpan);
                
                // For large date ranges, use compiled query and aggregate in memory
                var volumeData = await Services.Database.CompiledQueries.GetVolumeByDateRangeAsync(
                    _context, userId, startDate, endDate);
                    
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                // Aggregate based on date span
                if (dateSpan > 90)
                {
                    _logger.LogDebug("Large date range detected ({DateSpan} days), aggregating by week", dateSpan);
                    
                    // Group by week in memory
                    return volumeData
                        .GroupBy(v => DateTimeExtensions.GetStartOfWeek(v.Date))
                        .Select(g => new
                        {
                            Date = g.Key,
                            Volume = g.Sum(v => v.TotalVolume)
                        })
                        .OrderBy(x => x.Date)
                        .ToDictionary(x => x.Date, x => x.Volume);
                }
                else if (dateSpan > 30)
                {
                    _logger.LogDebug("Medium date range detected ({DateSpan} days), aggregating by 3 days", dateSpan);
                    
                    // Group by 3-day periods in memory
                    return volumeData
                        .GroupBy(v => v.Date.Date.AddDays(-(v.Date.Date.Subtract(startDate.Date).Days % 3)))
                        .Select(g => new
                        {
                            Date = g.Key,
                            Volume = g.Sum(v => v.TotalVolume)
                        })
                        .OrderBy(x => x.Date)
                        .ToDictionary(x => x.Date, x => x.Volume);
                }
                
                // Default case - return daily data
                _logger.LogDebug("Standard date range detected ({DateSpan} days), showing daily data", dateSpan);
                return volumeData.ToDictionary(x => x.Date, x => x.TotalVolume);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Volume over time query was canceled for user {UserId}", userId);
                throw; // Let the controller handle the cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting volume over time for user {UserId}", userId);
                return new Dictionary<DateTime, decimal>();
            }
        }

        public async Task<Dictionary<string, decimal>> GetVolumeByMuscleGroupAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var volumeByMuscleGroup = await _context.WorkoutExercises
                    .Where(e => e.WorkoutSession != null && 
                                e.WorkoutSession.UserId == userId && 
                                e.WorkoutSession.StartDateTime >= startDate &&
                                e.WorkoutSession.StartDateTime <= endDate &&
                                e.ExerciseType != null)
                    .GroupBy(e => e.ExerciseType.PrimaryMuscleGroup)
                    .Select(g => new
                    {
                        MuscleGroup = g.Key,
                        Volume = g.SelectMany(e => e.WorkoutSets)
                                 .Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0))
                    })
                    .ToDictionaryAsync(x => x.MuscleGroup ?? "Unknown", x => x.Volume);
                
                return volumeByMuscleGroup;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting volume by muscle group for user {UserId}", userId);
                return new Dictionary<string, decimal>();
            }
        }

        public async Task<Dictionary<string, decimal>> GetProgressForExerciseAsync(int userId, int exerciseTypeId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var progress = await _context.WorkoutExercises
                    .Where(e => e.WorkoutSession != null &&
                                e.WorkoutSession.UserId == userId && 
                                e.ExerciseTypeId == exerciseTypeId &&
                                e.WorkoutSession.StartDateTime >= startDate &&
                                e.WorkoutSession.StartDateTime <= endDate)
                    .OrderBy(e => e.WorkoutSession.StartDateTime)
                    .Select(e => new
                    {
                        Date = e.WorkoutSession.StartDateTime.Date.ToString("yyyy-MM-dd"),
                        MaxWeight = e.WorkoutSets.Any(s => s.Weight.HasValue) 
                                    ? e.WorkoutSets.Max(s => s.Weight ?? 0)
                                    : 0
                    })
                    .GroupBy(x => x.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        MaxWeight = g.Max(x => x.MaxWeight)
                    })
                    .ToDictionaryAsync(x => x.Date, x => x.MaxWeight);
                
                return progress;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting progress for exercise {ExerciseId} for user {UserId}", exerciseTypeId, userId);
                return new Dictionary<string, decimal>();
            }
        }

        public async Task<Dictionary<string, int>> GetWorkoutsByDayOfWeekAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var dayNames = new[]
                {
                    "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
                };
                
                var workoutsByDay = await _context.WorkoutSessions
                    .Where(s => s.UserId == userId && 
                                s.StartDateTime >= startDate &&
                                s.StartDateTime <= endDate)
                    .GroupBy(s => s.StartDateTime.DayOfWeek)
                    .Select(g => new
                    {
                        DayOfWeek = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => (int)x.DayOfWeek, x => x.Count);
                
                var result = new Dictionary<string, int>();
                foreach (var kvp in workoutsByDay)
                {
                    result[dayNames[kvp.Key]] = kvp.Value;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workouts by day of week for user {UserId}", userId);
                return new Dictionary<string, int>();
            }
        }

        public async Task<Dictionary<int, int>> GetWorkoutsByHourAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var workoutsByHour = await _context.WorkoutSessions
                    .Where(s => s.UserId == userId && 
                                s.StartDateTime >= startDate &&
                                s.StartDateTime <= endDate)
                    .GroupBy(s => s.StartDateTime.Hour)
                    .Select(g => new
                    {
                        Hour = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.Hour, x => x.Count);
                
                return workoutsByHour;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workouts by hour for user {UserId}", userId);
                return new Dictionary<int, int>();
            }
        }

        public async Task<PagedResult<PersonalRecord>> GetPersonalRecordsAsync(int userId, int page, int pageSize)
        {
            var result = new PagedResult<PersonalRecord>
            {
                CurrentPage = page
            };
            
            try
            {
                var skip = (page - 1) * pageSize;
                
                var personalRecords = await _context.WorkoutSets
                    .Where(s => s.WorkoutExercise != null && 
                               s.WorkoutExercise.WorkoutSession != null &&
                               s.WorkoutExercise.WorkoutSession.UserId == userId &&
                               s.WorkoutExercise.ExerciseType != null &&
                               s.WorkoutExercise.ExerciseType.Name != null &&
                               s.IsCompleted && !s.IsWarmup)
                    .OrderByDescending(s => s.WorkoutExercise.WorkoutSession.StartDateTime)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(s => new PersonalRecord
                    {
                        Id = s.WorkoutSetId,
                        UserId = s.WorkoutExercise.WorkoutSession.UserId,
                        ExerciseId = s.WorkoutExercise.ExerciseTypeId,
                        ExerciseName = s.WorkoutExercise.ExerciseType.Name ?? "Unknown Exercise",
                        Weight = s.Weight ?? 0,
                        Reps = s.Reps ?? 0,
                        Date = s.WorkoutExercise.WorkoutSession.StartDateTime,
                        WorkoutSessionId = s.WorkoutExercise.WorkoutSessionId,
                        WorkoutSessionName = s.WorkoutExercise.WorkoutSession.Name ?? "Unnamed Session"
                    })
                    .ToListAsync();
                
                var totalCount = await _context.WorkoutSets
                    .Where(s => s.WorkoutExercise != null && 
                               s.WorkoutExercise.WorkoutSession != null &&
                               s.WorkoutExercise.WorkoutSession.UserId == userId &&
                               s.IsCompleted && !s.IsWarmup)
                    .CountAsync();
                
                result.TotalItems = totalCount;
                result.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
                result.Records = personalRecords;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personal records for user {UserId}", userId);
            }
            
            return result;
        }
        
        public async Task<List<ExerciseWeightProgress>> GetWeightProgressAsync(int userId, int days, int limit = 5)
        {
            var result = new List<ExerciseWeightProgress>();
            
            try
            {
                _logger.LogDebug("Getting weight progress for user {UserId} for the last {Days} days with limit {Limit}", 
                    userId, days, limit);
                
                var startDate = DateTime.UtcNow.AddDays(-days);
                
                // Use command timeout and direct SQL for better performance
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandTimeout = 30; // 30 seconds timeout
                    
                    if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _context.Database.GetDbConnection().OpenAsync();
                    }
                    
                    // First get top exercises by frequency
                    command.CommandText = @"
                        SELECT TOP (@Limit)
                            we.ExerciseTypeId,
                            et.Name
                        FROM WorkoutExercises we
                        JOIN WorkoutSessions sess ON sess.WorkoutSessionId = we.WorkoutSessionId
                        JOIN ExerciseType et ON et.ExerciseTypeId = we.ExerciseTypeId
                        WHERE sess.UserId = @UserId
                          AND sess.StartDateTime >= @StartDate
                          AND et.Name IS NOT NULL
                        GROUP BY we.ExerciseTypeId, et.Name
                        ORDER BY COUNT(*) DESC";
                    
                    var paramUserId = command.CreateParameter();
                    paramUserId.ParameterName = "@UserId";
                    paramUserId.Value = userId;
                    command.Parameters.Add(paramUserId);
                    
                    var paramStartDate = command.CreateParameter();
                    paramStartDate.ParameterName = "@StartDate";
                    paramStartDate.Value = startDate;
                    command.Parameters.Add(paramStartDate);
                    
                    var paramLimit = command.CreateParameter();
                    paramLimit.ParameterName = "@Limit";
                    paramLimit.Value = limit;
                    command.Parameters.Add(paramLimit);
                    
                    _logger.LogDebug("Executing optimized top exercises query for weight progress");
                    
                    var topExercises = new List<(int id, string name)>();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int exerciseId = reader.GetInt32(0);
                            string exerciseName = reader.IsDBNull(1) ? "Unknown Exercise" : reader.GetString(1);
                            topExercises.Add((exerciseId, exerciseName));
                        }
                    }
                    
                    _logger.LogDebug("Retrieved {Count} top exercises", topExercises.Count);
                    
                    // Clear parameters for next command
                    command.Parameters.Clear();
                    
                    // For each top exercise, get weight progress data
                    foreach (var (exerciseId, exerciseName) in topExercises)
                    {
                        _logger.LogDebug("Getting weight progress data for exercise {ExerciseId} ({ExerciseName})", 
                            exerciseId, exerciseName);
                            
                        command.CommandText = @"
                            SELECT 
                                CAST(sess.StartDateTime AS DATE) AS WorkoutDate,
                                MAX(ws.Weight) AS MaxWeight,
                                MAX(CASE WHEN ws.Weight = (
                                        SELECT MAX(ws2.Weight) 
                                        FROM WorkoutSets ws2 
                                        JOIN WorkoutExercises we2 ON we2.WorkoutExerciseId = ws2.WorkoutExerciseId
                                        JOIN WorkoutSessions sess2 ON sess2.WorkoutSessionId = we2.WorkoutSessionId
                                        WHERE we2.ExerciseTypeId = @ExerciseId
                                          AND CAST(sess2.StartDateTime AS DATE) = CAST(sess.StartDateTime AS DATE)
                                          AND sess2.UserId = @UserId
                                    ) THEN ws.Reps ELSE 0 END) AS MaxReps,
                                MIN(sess.WorkoutSessionId) AS SessionId
                            FROM WorkoutSets ws
                            JOIN WorkoutExercises we ON we.WorkoutExerciseId = ws.WorkoutExerciseId
                            JOIN WorkoutSessions sess ON sess.WorkoutSessionId = we.WorkoutSessionId
                            WHERE we.ExerciseTypeId = @ExerciseId
                              AND sess.UserId = @UserId
                              AND sess.StartDateTime >= @StartDate
                              AND ws.Weight IS NOT NULL AND ws.Weight > 0
                            GROUP BY CAST(sess.StartDateTime AS DATE)
                            ORDER BY CAST(sess.StartDateTime AS DATE)";
                        
                        paramUserId = command.CreateParameter();
                        paramUserId.ParameterName = "@UserId";
                        paramUserId.Value = userId;
                        command.Parameters.Add(paramUserId);
                        
                        paramStartDate = command.CreateParameter();
                        paramStartDate.ParameterName = "@StartDate";
                        paramStartDate.Value = startDate;
                        command.Parameters.Add(paramStartDate);
                        
                        var paramExerciseId = command.CreateParameter();
                        paramExerciseId.ParameterName = "@ExerciseId";
                        paramExerciseId.Value = exerciseId;
                        command.Parameters.Add(paramExerciseId);
                        
                        var progressPoints = new List<WeightProgressPoint>();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var date = reader.GetDateTime(0);
                                var maxWeight = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
                                var maxReps = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2));
                                var sessionId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                                
                                progressPoints.Add(new WeightProgressPoint
                                {
                                    Date = date,
                                    Weight = maxWeight,
                                    Reps = maxReps,
                                    WorkoutSessionId = sessionId
                                });
                            }
                        }
                        
                        _logger.LogDebug("Retrieved {Count} weight progress points for exercise {ExerciseId}", 
                            progressPoints.Count, exerciseId);
                        
                        if (progressPoints.Any())
                        {
                            result.Add(new ExerciseWeightProgress
                            {
                                ExerciseId = exerciseId,
                                ExerciseName = exerciseName,
                                ProgressPoints = progressPoints,
                                MaxWeight = progressPoints.Max(p => p.Weight),
                                MinWeight = progressPoints.Min(p => p.Weight)
                            });
                        }
                        
                        // Clear parameters for next iteration
                        command.Parameters.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weight progress for user {UserId}", userId);
            }
            
            return result;
        }

        public async Task<ExerciseStatusResult> GetExerciseStatusAsync(int userId, int days, int limit = 10)
        {
            var result = new ExerciseStatusResult();
            
            try
            {
                _logger.LogDebug("Getting exercise status for user {UserId} for the last {Days} days with limit {Limit}",
                    userId, days, limit);
                
                var startDate = DateTime.UtcNow.AddDays(-days);
                
                // Use a command timeout to handle large datasets
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandTimeout = 30; // 30 seconds timeout
                    
                    if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    {
                        await _context.Database.GetDbConnection().OpenAsync();
                    }
                    
                    // Optimize the query by executing it directly with SQL
                    command.CommandText = @"
                        SELECT TOP (@Limit)
                            we.ExerciseTypeId AS ExerciseId,
                            et.Name AS ExerciseName,
                            SUM(CASE WHEN ws.IsCompleted = 1 THEN 1 ELSE 0 END) AS SuccessReps,
                            SUM(CASE WHEN ws.IsCompleted = 0 THEN 1 ELSE 0 END) AS FailedReps,
                            MAX(sess.StartDateTime) AS LastPerformed
                        FROM WorkoutSets ws
                        JOIN WorkoutExercises we ON we.WorkoutExerciseId = ws.WorkoutExerciseId
                        JOIN WorkoutSessions sess ON sess.WorkoutSessionId = we.WorkoutSessionId
                        JOIN ExerciseType et ON et.ExerciseTypeId = we.ExerciseTypeId
                        WHERE sess.UserId = @UserId
                          AND sess.StartDateTime >= @StartDate
                          AND et.Name IS NOT NULL
                        GROUP BY we.ExerciseTypeId, et.Name
                        ORDER BY SUM(CASE WHEN ws.IsCompleted = 1 THEN 1 ELSE 0 END) + SUM(CASE WHEN ws.IsCompleted = 0 THEN 1 ELSE 0 END) DESC";
                    
                    var paramUserId = command.CreateParameter();
                    paramUserId.ParameterName = "@UserId";
                    paramUserId.Value = userId;
                    command.Parameters.Add(paramUserId);
                    
                    var paramStartDate = command.CreateParameter();
                    paramStartDate.ParameterName = "@StartDate";
                    paramStartDate.Value = startDate;
                    command.Parameters.Add(paramStartDate);
                    
                    var paramLimit = command.CreateParameter();
                    paramLimit.ParameterName = "@Limit";
                    paramLimit.Value = limit;
                    command.Parameters.Add(paramLimit);
                    
                    _logger.LogDebug("Executing optimized exercise status query");
                    var exerciseStats = new List<ExerciseStatusViewModel>();
                    int totalSuccess = 0;
                    int totalFailed = 0;
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var exerciseId = reader.GetInt32(0);
                            var exerciseName = reader.IsDBNull(1) ? "Unknown Exercise" : reader.GetString(1);
                            var successReps = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2));
                            var failedReps = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3));
                            var lastPerformed = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4);
                            
                            totalSuccess += successReps;
                            totalFailed += failedReps;
                            
                            exerciseStats.Add(new ExerciseStatusViewModel
                            {
                                ExerciseId = exerciseId,
                                ExerciseName = exerciseName,
                                SuccessReps = successReps,
                                FailedReps = failedReps,
                                LastPerformed = lastPerformed
                            });
                        }
                    }
                    
                    _logger.LogDebug("Retrieved {Count} exercise status records with {Success} successful reps and {Failed} failed reps",
                        exerciseStats.Count, totalSuccess, totalFailed);
                    
                    result.AllExercises = exerciseStats;
                    
                    // Get top 5 most recently performed exercises
                    result.TopExercises = exerciseStats
                        .OrderByDescending(e => e.LastPerformed)
                        .Take(5)
                        .ToList();
                    
                    result.TotalSuccess = totalSuccess;
                    result.TotalFailed = totalFailed;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exercise status for user {UserId} with limit {Limit}", userId, limit);
                // Initialize with empty data rather than failing
                result.AllExercises = new List<ExerciseStatusViewModel>();
                result.TopExercises = new List<ExerciseStatusViewModel>();
                result.TotalSuccess = 0;
                result.TotalFailed = 0;
            }
            
            return result;
        }

        public async Task<object> GetUserMetricsAsync(int userId, int days, CancellationToken cancellationToken = default)
        {
            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                
                // Check cancellation before expensive operations
                cancellationToken.ThrowIfCancellationRequested();
                
                var sessionCount = await _context.WorkoutSessions
                    .Where(s => s.UserId == userId && s.StartDateTime >= startDate)
                    .CountAsync(cancellationToken);
                
                // Check cancellation before next expensive operation
                cancellationToken.ThrowIfCancellationRequested();   
                
                var totalStats = await _context.WorkoutSets
                    .Where(s => s.WorkoutExercise != null &&
                               s.WorkoutExercise.WorkoutSession != null &&
                               s.WorkoutExercise.WorkoutSession.UserId == userId && 
                               s.WorkoutExercise.WorkoutSession.StartDateTime >= startDate)
                    .GroupBy(_ => 1)
                    .Select(g => new
                    {
                        TotalSets = g.Count(),
                        TotalReps = g.Sum(s => s.Reps ?? 0),
                        SuccessReps = g.Count(s => s.IsCompleted),
                        FailedReps = g.Count(s => !s.IsCompleted)
                    })
                    .FirstOrDefaultAsync(cancellationToken) ?? new { TotalSets = 0, TotalReps = 0, SuccessReps = 0, FailedReps = 0 };

                // Check cancellation before most expensive operation (volume calculation)
                cancellationToken.ThrowIfCancellationRequested();

                // Optimize the query by using a raw SQL approach for large datasets
                decimal totalVolume = 0;
                
                if (days > 90)
                {
                    _logger.LogDebug("Using optimized volume calculation for large date range ({Days} days)", days);
                    
                    // Use direct SQL command with timeout for large datasets
                    using (var command = _context.Database.GetDbConnection().CreateCommand())
                    {
                        command.CommandTimeout = 20; // 20 seconds timeout
                        
                        if (_context.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                        {
                            await _context.Database.GetDbConnection().OpenAsync(cancellationToken);
                        }
                        
                        command.CommandText = @"
                            SELECT COALESCE(SUM((ws.Weight * ws.Reps)), 0)
                            FROM WorkoutSets ws
                            JOIN WorkoutExercises we ON we.WorkoutExerciseId = ws.WorkoutExerciseId
                            JOIN WorkoutSessions sess ON sess.WorkoutSessionId = we.WorkoutSessionId
                            WHERE sess.UserId = @UserId
                              AND sess.StartDateTime >= @StartDate
                              AND ws.Weight IS NOT NULL AND ws.Reps IS NOT NULL";
                        
                        var paramUserId = command.CreateParameter();
                        paramUserId.ParameterName = "@UserId";
                        paramUserId.Value = userId;
                        command.Parameters.Add(paramUserId);
                        
                        var paramStartDate = command.CreateParameter();
                        paramStartDate.ParameterName = "@StartDate";
                        paramStartDate.Value = startDate;
                        command.Parameters.Add(paramStartDate);
                        
                        // Use ExecuteScalarAsync with cancellation token
                        var result = await command.ExecuteScalarAsync(cancellationToken);
                        if (result != null && result != DBNull.Value)
                        {
                            totalVolume = Convert.ToDecimal(result);
                        }
                    }
                }
                else
                {
                    // For smaller date ranges, use LINQ
                    totalVolume = await _context.WorkoutSets
                        .Where(s => s.WorkoutExercise != null &&
                                   s.WorkoutExercise.WorkoutSession != null &&
                                   s.WorkoutExercise.WorkoutSession.UserId == userId && 
                                   s.WorkoutExercise.WorkoutSession.StartDateTime >= startDate)
                        .SumAsync(s => (s.Weight ?? 0) * (s.Reps ?? 0), cancellationToken);
                }
                
                // Calculate calories as a function of volume
                decimal caloriesPerVolumeUnit = 0.15m; // Estimated calories burned per kg of volume
                decimal totalCalories = totalVolume * caloriesPerVolumeUnit;
                
                // Check cancellation before returning result
                cancellationToken.ThrowIfCancellationRequested();
                
                return new
                {
                    SessionCount = sessionCount,
                    SetCount = totalStats.TotalSets,
                    RepCount = totalStats.TotalReps,
                    SuccessReps = totalStats.SuccessReps,
                    FailedReps = totalStats.FailedReps,
                    TotalVolume = totalVolume,
                    TotalCalories = totalCalories,
                    CaloriesByExerciseType = new List<object>() // This would be expanded in a real implementation
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("User metrics query was canceled for user {UserId}", userId);
                throw; // Let the controller handle the cancellation
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user metrics for user {UserId}", userId);
                return new
                {
                    SessionCount = 0,
                    SetCount = 0,
                    RepCount = 0,
                    SuccessReps = 0,
                    FailedReps = 0,
                    TotalVolume = 0m,
                    TotalCalories = 0m,
                    CaloriesByExerciseType = new List<object>()
                };
            }
        }

        public async Task<ExerciseStatusResult> GetExerciseStatusAsync(int userId, int days)
        {
            var result = new ExerciseStatusResult();
            
            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                
                var exerciseStats = await _context.WorkoutSets
                    .Where(s => s.WorkoutExercise != null && 
                                s.WorkoutExercise.WorkoutSession != null &&
                                s.WorkoutExercise.WorkoutSession.UserId == userId && 
                                s.WorkoutExercise.WorkoutSession.StartDateTime >= startDate &&
                                s.WorkoutExercise.ExerciseType != null &&
                                !string.IsNullOrEmpty(s.WorkoutExercise.ExerciseType.Name))
                    .GroupBy(s => new
                    {
                        ExerciseId = s.WorkoutExercise.ExerciseTypeId,
                        ExerciseName = s.WorkoutExercise.ExerciseType.Name
                    })
                    .Select(g => new
                    {
                        ExerciseId = g.Key.ExerciseId,
                        ExerciseName = g.Key.ExerciseName,
                        SuccessReps = g.Count(s => s.IsCompleted), 
                        FailedReps = g.Count(s => !s.IsCompleted),
                        LastPerformed = g.Max(s => s.WorkoutExercise.WorkoutSession.StartDateTime)
                    })
                    .ToListAsync();
                
                result.AllExercises = exerciseStats.Select(e => new ExerciseStatusViewModel
                {
                    ExerciseId = e.ExerciseId,
                    ExerciseName = e.ExerciseName ?? "Unknown Exercise",
                    SuccessReps = e.SuccessReps,
                    FailedReps = e.FailedReps,
                    LastPerformed = e.LastPerformed
                }).ToList();
                
                result.TopExercises = result.AllExercises
                    .OrderByDescending(e => e.LastPerformed)
                    .Take(5)
                    .ToList();
                
                result.TotalSuccess = result.AllExercises.Sum(e => e.SuccessReps);
                result.TotalFailed = result.AllExercises.Sum(e => e.FailedReps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exercise status for user {UserId}", userId);
            }
            
            return result;
        }
        
        public async Task<List<string>> GetExerciseTypesAsync()
        {
            try
            {
                return await _context.ExerciseType
                    .Select(et => et.Name)
                    .OrderBy(n => n)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exercise types");
                return new List<string>();
            }
        }
    }
    
    public class ExerciseStatus 
    {
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; }
        public int SuccessReps { get; set; }
        public int FailedReps { get; set; }
    }
    
    public class RecentExerciseStatus
    {
        public string ExerciseName { get; set; }
        public DateTime LastPerformed { get; set; }
    }
    
    public class WeightProgress
    {
        public string ExerciseName { get; set; }
        public List<ProgressPoint> Points { get; set; } = new List<ProgressPoint>();
    }
    
    public class ProgressPoint
    {
        public DateTime Date { get; set; }
        public decimal Weight { get; set; }
    }
}