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
                return await _context.WorkoutSessions
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.StartDateTime)
                    .Take(count)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent workout sessions for user {UserId}", userId);
                return new List<WorkoutSession>();
            }
        }

        public async Task<Dictionary<DateTime, decimal>> GetVolumeOverTimeAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var volumeByDay = await _context.WorkoutSessions
                    .Where(s => s.UserId == userId && 
                                s.StartDateTime >= startDate &&
                                s.StartDateTime <= endDate)
                    .GroupBy(s => s.StartDateTime.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Volume = g.SelectMany(s => s.WorkoutExercises)
                                  .SelectMany(e => e.WorkoutSets)
                                  .Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0))
                    })
                    .OrderBy(x => x.Date)
                    .ToDictionaryAsync(x => x.Date, x => x.Volume);
                
                return volumeByDay;
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
        
        public async Task<List<ExerciseWeightProgress>> GetWeightProgressAsync(int userId, int days)
        {
            var result = new List<ExerciseWeightProgress>();
            
            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                
                var exerciseTypeIds = await _context.WorkoutExercises
                    .Where(e => e.WorkoutSession != null && 
                           e.WorkoutSession.UserId == userId && 
                           e.WorkoutSession.StartDateTime >= startDate &&
                           e.ExerciseTypeId > 0 &&
                           e.ExerciseType != null)
                    .Select(e => e.ExerciseTypeId)
                    .Distinct()
                    .ToListAsync();
                
                foreach (var exerciseTypeId in exerciseTypeIds)
                {
                    var exerciseType = await _context.ExerciseType.FindAsync(exerciseTypeId);
                    
                    if (exerciseType == null || string.IsNullOrEmpty(exerciseType.Name)) continue;
                    
                    var sets = await _context.WorkoutSets
                        .Where(s => s.WorkoutExercise != null &&
                                   s.WorkoutExercise.ExerciseType != null &&
                                   s.WorkoutExercise.ExerciseTypeId == exerciseTypeId && 
                                   s.WorkoutExercise.WorkoutSession != null &&
                                   s.WorkoutExercise.WorkoutSession.UserId == userId &&
                                   s.WorkoutExercise.WorkoutSession.StartDateTime >= startDate &&
                                   s.Weight.HasValue)
                        .OrderBy(s => s.WorkoutExercise.WorkoutSession.StartDateTime)
                        .Select(s => new
                        {
                            Date = s.WorkoutExercise.WorkoutSession.StartDateTime,
                            Weight = s.Weight ?? 0,
                            Reps = s.Reps ?? 0,
                            WorkoutSessionId = s.WorkoutExercise.WorkoutSessionId
                        })
                        .ToListAsync();
                    
                    if (!sets.Any()) continue;
                    
                    var progressPoints = sets.Select(s => new WeightProgressPoint
                    {
                        Date = s.Date,
                        Weight = s.Weight,
                        Reps = s.Reps,
                        WorkoutSessionId = s.WorkoutSessionId
                    }).ToList();
                    
                    var weightProgress = new ExerciseWeightProgress
                    {
                        ExerciseId = exerciseTypeId,
                        ExerciseName = exerciseType.Name,
                        ProgressPoints = progressPoints,
                        MaxWeight = progressPoints.Any() ? progressPoints.Max(p => p.Weight) : 0,
                        MinWeight = progressPoints.Any() ? progressPoints.Min(p => p.Weight) : 0
                    };
                    
                    result.Add(weightProgress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weight progress for user {UserId}", userId);
            }
            
            return result;
        }
        
        public async Task<List<ExerciseWeightProgress>> GetWeightProgressAsync(int userId, int days, int limit = 5)
        {
            var result = new List<ExerciseWeightProgress>();
            
            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                
                var topExerciseIds = await _context.WorkoutExercises
                    .Where(e => e.WorkoutSession != null && 
                           e.WorkoutSession.UserId == userId && 
                           e.WorkoutSession.StartDateTime >= startDate)
                    .GroupBy(e => e.ExerciseTypeId)
                    .OrderByDescending(g => g.Count())
                    .Take(limit)
                    .Select(g => g.Key)
                    .ToListAsync();
                
                foreach (var exerciseTypeId in topExerciseIds)
                {
                    var exerciseType = await _context.ExerciseType.FindAsync(exerciseTypeId);
                    
                    if (exerciseType == null || string.IsNullOrEmpty(exerciseType.Name)) continue;
                    
                    var weightDataPoints = await _context.WorkoutSets
                        .Where(s => s.WorkoutExercise != null &&
                                   s.WorkoutExercise.ExerciseTypeId == exerciseTypeId && 
                                   s.WorkoutExercise.WorkoutSession != null &&
                                   s.WorkoutExercise.WorkoutSession.UserId == userId &&
                                   s.WorkoutExercise.WorkoutSession.StartDateTime >= startDate &&
                                   s.Weight.HasValue)
                        .GroupBy(s => s.WorkoutExercise.WorkoutSession.StartDateTime.Date)
                        .OrderBy(g => g.Key)
                        .Select(g => new
                        {
                            Date = g.Key,
                            MaxWeight = g.Max(s => s.Weight.Value),
                            MaxReps = g.Where(s => s.Weight == g.Max(s2 => s2.Weight)).Max(s => s.Reps ?? 0)
                        })
                        .ToListAsync();
                    
                    if (!weightDataPoints.Any()) continue;
                    
                    var progressPoints = weightDataPoints.Select(dp => new WeightProgressPoint
                    {
                        Date = dp.Date,
                        Weight = dp.MaxWeight,
                        Reps = dp.MaxReps,
                        WorkoutSessionId = 0
                    }).ToList();
                    
                    var weightProgress = new ExerciseWeightProgress
                    {
                        ExerciseId = exerciseTypeId,
                        ExerciseName = exerciseType.Name,
                        ProgressPoints = progressPoints,
                        MaxWeight = progressPoints.Any() ? progressPoints.Max(p => p.Weight) : 0,
                        MinWeight = progressPoints.Any() ? progressPoints.Min(p => p.Weight) : 0
                    };
                    
                    result.Add(weightProgress);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting optimized weight progress for user {UserId}", userId);
            }
            
            return result;
        }

        public async Task<ExerciseStatusResult> GetExerciseStatusAsync(int userId, int days, int limit = 10)
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
                    .OrderByDescending(e => e.SuccessReps + e.FailedReps)
                    .Take(limit)
                    .ToListAsync();
                
                result.AllExercises = exerciseStats.Select(e => new ExerciseStatusViewModel
                {
                    ExerciseId = e.ExerciseId,
                    ExerciseName = e.ExerciseName ?? "Unknown Exercise",
                    SuccessReps = e.SuccessReps,
                    FailedReps = e.FailedReps,
                    LastPerformed = e.LastPerformed
                }).ToList();
                
                result.TopExercises = exerciseStats
                    .OrderByDescending(e => e.LastPerformed)
                    .Take(5)
                    .Select(e => new ExerciseStatusViewModel
                    {
                        ExerciseId = e.ExerciseId,
                        ExerciseName = e.ExerciseName ?? "Unknown Exercise",
                        SuccessReps = e.SuccessReps,
                        FailedReps = e.FailedReps,
                        LastPerformed = e.LastPerformed
                    })
                    .ToList();
                
                result.TotalSuccess = result.AllExercises.Sum(e => e.SuccessReps);
                result.TotalFailed = result.AllExercises.Sum(e => e.FailedReps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting optimized exercise status for user {UserId}", userId);
            }
            
            return result;
        }

        public async Task<object> GetUserMetricsAsync(int userId, int days)
        {
            try
            {
                var startDate = DateTime.UtcNow.AddDays(-days);
                
                var sessionCount = await _context.WorkoutSessions
                    .Where(s => s.UserId == userId && s.StartDateTime >= startDate)
                    .CountAsync();
                    
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
                    .FirstOrDefaultAsync() ?? new { TotalSets = 0, TotalReps = 0, SuccessReps = 0, FailedReps = 0 };

                var totalVolume = await _context.WorkoutSets
                    .Where(s => s.WorkoutExercise != null &&
                               s.WorkoutExercise.WorkoutSession != null &&
                               s.WorkoutExercise.WorkoutSession.UserId == userId && 
                               s.WorkoutExercise.WorkoutSession.StartDateTime >= startDate)
                    .SumAsync(s => (s.Weight ?? 0) * (s.Reps ?? 0));
                    
                return new
                {
                    SessionCount = sessionCount,
                    SetCount = totalStats.TotalSets,
                    RepCount = totalStats.TotalReps,
                    SuccessReps = totalStats.SuccessReps,
                    FailedReps = totalStats.FailedReps,
                    TotalVolume = totalVolume
                };
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
                    TotalVolume = 0m
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