using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services.Cache;
using Microsoft.Extensions.Options;

namespace WorkoutTrackerWeb.Services
{
    public class WorkoutDataService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<WorkoutDataService> _logger;
        private readonly IQueryResultCacheService _queryCache;
        private readonly QueryResultCacheOptions _cacheOptions;

        // Event for progress updates
        public event EventHandler<ProgressUpdateEventArgs> OnProgressUpdate;

        public WorkoutDataService(
            WorkoutTrackerWebContext context, 
            ILogger<WorkoutDataService> logger,
            IQueryResultCacheService queryCache,
            IOptions<QueryResultCacheOptions> cacheOptions)
        {
            _context = context;
            _logger = logger;
            _queryCache = queryCache;
            _cacheOptions = cacheOptions.Value;
        }

        public async Task<List<WorkoutSession>> GetUserSessionsAsync(int userId)
        {
            // Use the query cache service with automatic key generation
            return await _queryCache.GetOrCreateQueryResultAsync(
                keyPrefix: "user:sessions",
                queryParameters: new { UserId = userId },
                queryFunc: async () => {
                    _logger.LogDebug("Cache miss for user sessions. Loading from database for user {UserId}", userId);
                    
                    // Use AsSplitQuery to avoid cartesian explosion and improve performance
                    // This breaks a single complex query into multiple simpler queries
                    var workoutSessions = await _context.WorkoutSessions
                        .Where(ws => ws.UserId == userId)
                        .OrderByDescending(ws => ws.StartDateTime)
                        .AsSplitQuery()
                        .Include(ws => ws.WorkoutExercises)
                            .ThenInclude(we => we.ExerciseType)
                        .Include(ws => ws.WorkoutExercises)
                            .ThenInclude(we => we.WorkoutSets)
                        .AsNoTracking() // No tracking for read-only operations
                        .ToListAsync();

                    // Process in memory to avoid SQL null issues
                    var processedSessions = workoutSessions
                        .Select(ws => new WorkoutSession
                        {
                            WorkoutSessionId = ws.WorkoutSessionId,
                            Name = ws.Name ?? "Unnamed Session",
                            UserId = ws.UserId,
                            StartDateTime = ws.StartDateTime,
                            Duration = ws.Duration,
                            CaloriesBurned = ws.CaloriesBurned ?? 0m,
                            Notes = ws.Notes ?? string.Empty,
                            WorkoutExercises = ws.WorkoutExercises?
                                .Where(we => we.ExerciseType?.Name != null)
                                .Select(we => new WorkoutExercise
                                {
                                    WorkoutExerciseId = we.WorkoutExerciseId,
                                    ExerciseTypeId = we.ExerciseTypeId,
                                    WorkoutSessionId = we.WorkoutSessionId,
                                    ExerciseType = we.ExerciseType != null ? new ExerciseType
                                    {
                                        ExerciseTypeId = we.ExerciseType.ExerciseTypeId,
                                        Name = we.ExerciseType.Name ?? "Unknown Exercise",
                                        Description = we.ExerciseType.Description ?? string.Empty,
                                        Type = we.ExerciseType.Type ?? string.Empty,
                                        Muscle = we.ExerciseType.Muscle ?? string.Empty,
                                        Equipment = we.ExerciseType.Equipment ?? string.Empty,
                                        Difficulty = we.ExerciseType.Difficulty ?? string.Empty
                                    } : null,
                                    WorkoutSets = we.WorkoutSets?.Select(s => new WorkoutSet
                                    {
                                        WorkoutSetId = s.WorkoutSetId,
                                        WorkoutExerciseId = s.WorkoutExerciseId,
                                        Reps = s.Reps,
                                        Weight = s.Weight,
                                        IsCompleted = s.IsCompleted
                                    }).ToList() ?? new List<WorkoutSet>()
                                }).ToList() ?? new List<WorkoutExercise>()
                        })
                        .ToList();

                    return processedSessions;
                },
                // Cache for 20 minutes, which is a good balance for workout data that doesn't change too frequently
                expiration: TimeSpan.FromMinutes(20),
                slidingExpiration: false
            );
        }

        public async Task<Dictionary<string, int>> GetUserMetricsAsync(int userId, DateTime startDate)
        {
            // Use the query cache service with automatic key generation
            return await _queryCache.GetOrCreateQueryResultAsync(
                keyPrefix: "user:metrics",
                queryParameters: new { UserId = userId, StartDate = startDate },
                queryFunc: async () => {
                    _logger.LogDebug("Cache miss for user metrics. Loading from database for user {UserId} from {StartDate}", userId, startDate);
                    
                    var metrics = new Dictionary<string, int>();
                    
                    try {
                        // Use a more efficient query approach to avoid N+1 queries
                        // Get total workout count directly
                        metrics["TotalWorkouts"] = await _context.WorkoutSessions
                            .Where(ws => ws.UserId == userId && ws.StartDateTime >= startDate)
                            .CountAsync();

                        // Get total exercises with a single query
                        metrics["TotalExercises"] = await _context.WorkoutExercises
                            .Where(we => we.WorkoutSession.UserId == userId && 
                                         we.WorkoutSession.StartDateTime >= startDate)
                            .CountAsync();
                        
                        // Get total sets with a single query
                        metrics["TotalSets"] = await _context.WorkoutSets
                            .Where(ws => ws.WorkoutExercise.WorkoutSession.UserId == userId && 
                                          ws.WorkoutExercise.WorkoutSession.StartDateTime >= startDate)
                            .CountAsync();
                        
                        // Get sum of reps with a single query
                        metrics["TotalReps"] = await _context.WorkoutSets
                            .Where(ws => ws.WorkoutExercise.WorkoutSession.UserId == userId && 
                                          ws.WorkoutExercise.WorkoutSession.StartDateTime >= startDate)
                            .SumAsync(ws => ws.Reps ?? 0);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error getting user metrics for user {UserId}", userId);
                        // Provide default values in case of error
                        metrics["TotalWorkouts"] = 0;
                        metrics["TotalExercises"] = 0;
                        metrics["TotalSets"] = 0;
                        metrics["TotalReps"] = 0;
                    }
                    
                    return metrics;
                }, 
                // Cache for 15 minutes as metrics are aggregated data that don't change too frequently
                expiration: TimeSpan.FromMinutes(15),
                slidingExpiration: false
            );
        }

        public async Task<Dictionary<string, int>> GetWorkoutFrequencyAsync(int userId, int days)
        {
            // Use the query cache service with automatic key generation
            return await _queryCache.GetOrCreateQueryResultAsync(
                keyPrefix: "user:frequency",
                queryParameters: new { UserId = userId, Days = days },
                queryFunc: async () => {
                    _logger.LogDebug("Cache miss for workout frequency. Loading from database for user {UserId} for {Days} days", userId, days);
                    
                    var startDate = DateTime.Now.AddDays(-days);
                    var frequency = new Dictionary<string, int>();
                    
                    // Initialize dictionary with zero values for all days in range
                    for (int i = 0; i < days; i++)
                    {
                        var date = DateTime.Now.AddDays(-i).Date.ToString("yyyy-MM-dd");
                        frequency[date] = 0;
                    }

                    // Get the workout counts grouped by date directly from the database
                    var workoutCounts = await _context.WorkoutSessions
                        .Where(ws => ws.UserId == userId && ws.StartDateTime >= startDate)
                        .GroupBy(ws => ws.StartDateTime.Date)
                        .Select(g => new { Date = g.Key, Count = g.Count() })
                        .ToListAsync();

                    // Update frequency dictionary with actual counts
                    foreach (var item in workoutCounts)
                    {
                        string dateKey = item.Date.ToString("yyyy-MM-dd");
                        if (frequency.ContainsKey(dateKey))
                        {
                            frequency[dateKey] = item.Count;
                        }
                    }

                    return frequency;
                },
                // Cache for 1 hour as frequency data is less likely to change often
                expiration: TimeSpan.FromHours(1),
                slidingExpiration: false
            );
        }

        public async Task<int> GetTotalWorkoutsAsync(int userId)
        {
            // Use the query cache service with automatic key generation, but with Int wrapper object
            var result = await _queryCache.GetOrCreateQueryResultAsync<IntWrapper>(
                keyPrefix: "user:total-workouts",
                queryParameters: new { UserId = userId },
                queryFunc: async () => {
                    _logger.LogDebug("Cache miss for total workouts. Loading from database for user {UserId}", userId);
                    
                    int count = await _context.WorkoutSessions
                        .CountAsync(ws => ws.UserId == userId);
                    
                    return new IntWrapper { Value = count };
                },
                // Cache for 30 minutes as this is a simple count that doesn't change too frequently
                expiration: TimeSpan.FromMinutes(30),
                slidingExpiration: false
            );
            
            return result.Value;
        }

        /// <summary>
        /// Deletes all workout data for a user and invalidates related caches
        /// </summary>
        public async Task<bool> DeleteAllWorkoutDataAsync(int userId)
        {
            _logger.LogInformation($"Starting deletion of all workout data for user {userId}");
            
            // Report initial progress
            ReportProgress(0, "Started workout data deletion");
            
            try
            {
                // Use the execution strategy provided by the context to handle retries properly
                bool result = await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () => 
                {
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            // Step 1: Delete WorkoutSets
                            var workoutSessions = await _context.WorkoutSessions
                                .Where(ws => ws.UserId == userId)
                                .Select(ws => ws.WorkoutSessionId)
                                .ToListAsync();
                            
                            if (workoutSessions.Count == 0)
                            {
                                ReportProgress(100, "No workout data found to delete");
                                await transaction.CommitAsync();
                                return true;
                            }
                            
                            ReportProgress(10, $"Found {workoutSessions.Count} workout sessions to delete");
                            
                            // Get associated workout exercises
                            var exerciseIds = await _context.WorkoutExercises
                                .Where(we => workoutSessions.Contains(we.WorkoutSessionId))
                                .Select(we => we.WorkoutExerciseId)
                                .ToListAsync();
                            
                            // Delete workout sets
                            var sets = await _context.WorkoutSets
                                .Where(ws => exerciseIds.Contains(ws.WorkoutExerciseId))
                                .ToListAsync();
                            
                            _context.WorkoutSets.RemoveRange(sets);
                            await _context.SaveChangesAsync();
                            
                            ReportProgress(40, $"Deleted {sets.Count} workout sets");
                            
                            // Step 2: Delete WorkoutExercises
                            var exercises = await _context.WorkoutExercises
                                .Where(we => workoutSessions.Contains(we.WorkoutSessionId))
                                .ToListAsync();
                            
                            _context.WorkoutExercises.RemoveRange(exercises);
                            await _context.SaveChangesAsync();
                            
                            ReportProgress(70, $"Deleted {exercises.Count} workout exercises");
                            
                            // Step 3: Delete WorkoutSessions
                            var sessions = await _context.WorkoutSessions
                                .Where(ws => ws.UserId == userId)
                                .ToListAsync();
                            
                            _context.WorkoutSessions.RemoveRange(sessions);
                            await _context.SaveChangesAsync();
                            
                            ReportProgress(90, $"Deleted {sessions.Count} workout sessions");
                            
                            // Legacy session data removal not needed as tables have been migrated and removed
                            
                            await transaction.CommitAsync();
                            
                            ReportProgress(100, "Successfully deleted all workout data");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError(ex, $"Error deleting workout data for user {userId}");
                            ReportProgress(100, $"Error: {ex.Message}");
                            return false;
                        }
                    }
                });
                
                if (result)
                {
                    // Invalidate all cached data for this user after successful deletion
                    await InvalidateUserCachesAsync(userId);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing delete strategy for user {userId}");
                ReportProgress(100, $"Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Invalidates all cached data for a specific user
        /// </summary>
        public async Task InvalidateUserCachesAsync(int userId)
        {
            _logger.LogInformation("Invalidating all cached workout data for user {UserId}", userId);
            
            // Invalidate user-specific cache prefixes
            await _queryCache.InvalidateQueryResultsByPrefixAsync($"user:sessions:{userId}");
            await _queryCache.InvalidateQueryResultsByPrefixAsync($"user:metrics:{userId}");
            await _queryCache.InvalidateQueryResultsByPrefixAsync($"user:frequency:{userId}");
            await _queryCache.InvalidateQueryResultsByPrefixAsync($"user:total-workouts:{userId}");
        }

        private void ReportProgress(int percentComplete, string message)
        {
            _logger.LogInformation($"Progress: {percentComplete}% - {message}");
            OnProgressUpdate?.Invoke(this, new ProgressUpdateEventArgs
            {
                PercentComplete = percentComplete,
                Message = message
            });
        }
    }

    /// <summary>
    /// A simple wrapper class for int values to use with the cache service
    /// </summary>
    public class IntWrapper
    {
        public int Value { get; set; }
    }
}