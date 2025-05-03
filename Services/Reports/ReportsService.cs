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
            // This is the legacy method - now we'll call the new methods
            var result = new ReportsData
            {
                CurrentPage = pageNumber
            };

            try
            {
                // Get personal records
                var prData = await GetPersonalRecordsAsync(userId, pageNumber, 10);
                result.PersonalRecords = prData.Records;
                result.TotalPages = prData.TotalPages;

                // Get exercise status
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
                
                // Calculate totals
                result.TotalReps = result.SuccessReps + result.FailedReps;
                
                // Get the total workout sessions in the period
                var startDate = DateTime.UtcNow.AddDays(-period);
                result.TotalSessions = await _context.WorkoutSessions
                    .Where(s => s.UserId == userId && s.StartDateTime >= startDate)
                    .CountAsync();
                
                // Get the total sets in the period
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
                // Using PrimaryMuscleGroup instead of MuscleGroup based on updated model
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
                // Load the data first to handle null values in memory rather than in SQL
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
                    
                // Process in memory to avoid SQL NULL exceptions
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
                // Calculate volume for each workout session since TotalVolume property doesn't exist
                var volumeByDay = await _context.WorkoutSessions
                    .Where(s => s.UserId == userId && 
                                s.StartDateTime >= startDate &&
                                s.StartDateTime <= endDate)
                    .GroupBy(s => s.StartDateTime.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        // Calculate volume as sum of weight * reps across all exercises and sets
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
                        // Calculate volume as sum of weight * reps across all sets
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
                
                // Convert day numbers to day names
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
                // Calculate skip for pagination
                var skip = (page - 1) * pageSize;
                
                // Query for personal records from workout sets with explicit null checks
                // Since there's no IsPersonalRecord property, get the max weight for each exercise
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
                
                // Get total count for pagination with same null checks
                var totalCount = await _context.WorkoutSets
                    .Where(s => s.WorkoutExercise != null && 
                               s.WorkoutExercise.WorkoutSession != null &&
                               s.WorkoutExercise.WorkoutSession.UserId == userId &&
                               s.IsCompleted && !s.IsWarmup)
                    .CountAsync();
                
                // Calculate total pages
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
                // Calculate the start date based on the number of days
                var startDate = DateTime.UtcNow.AddDays(-days);
                
                // Get exercise types that the user has performed
                var exerciseTypeIds = await _context.WorkoutExercises
                    .Where(e => e.WorkoutSession != null && 
                           e.WorkoutSession.UserId == userId && 
                           e.WorkoutSession.StartDateTime >= startDate &&
                           e.ExerciseTypeId > 0 &&
                           e.ExerciseType != null)
                    .Select(e => e.ExerciseTypeId)
                    .Distinct()
                    .ToListAsync();
                
                // For each exercise type, get the weight progress
                foreach (var exerciseTypeId in exerciseTypeIds)
                {
                    var exerciseType = await _context.ExerciseType.FindAsync(exerciseTypeId);
                    
                    // Skip if no exercise type or name is null
                    if (exerciseType == null || string.IsNullOrEmpty(exerciseType.Name)) continue;
                    
                    // Get all sets for this exercise type with explicit null checking
                    var sets = await _context.WorkoutSets
                        .Where(s => s.WorkoutExercise != null &&
                                   s.WorkoutExercise.ExerciseType != null &&
                                   s.WorkoutExercise.ExerciseTypeId == exerciseTypeId && 
                                   s.WorkoutExercise.WorkoutSession != null &&
                                   s.WorkoutExercise.WorkoutSession.UserId == userId &&
                                   s.WorkoutExercise.WorkoutSession.StartDateTime >= startDate &&
                                   s.Weight.HasValue) // Ensure weight is not null
                        .OrderBy(s => s.WorkoutExercise.WorkoutSession.StartDateTime)
                        .Select(s => new
                        {
                            Date = s.WorkoutExercise.WorkoutSession.StartDateTime,
                            Weight = s.Weight ?? 0,
                            Reps = s.Reps ?? 0,
                            WorkoutSessionId = s.WorkoutExercise.WorkoutSessionId
                        })
                        .ToListAsync();
                    
                    // Skip if no sets
                    if (!sets.Any()) continue;
                    
                    // Create progress points
                    var progressPoints = sets.Select(s => new WeightProgressPoint
                    {
                        Date = s.Date,
                        Weight = s.Weight,
                        Reps = s.Reps,
                        WorkoutSessionId = s.WorkoutSessionId
                    }).ToList();
                    
                    // Create exercise weight progress
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
        
        public async Task<ExerciseStatusResult> GetExerciseStatusAsync(int userId, int days)
        {
            var result = new ExerciseStatusResult();
            
            try
            {
                // Calculate the start date based on the number of days
                var startDate = DateTime.UtcNow.AddDays(-days);
                
                // Get exercise status by exerciseType with comprehensive null checking
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
                
                // Convert to view models
                result.AllExercises = exerciseStats.Select(e => new ExerciseStatusViewModel
                {
                    ExerciseId = e.ExerciseId,
                    ExerciseName = e.ExerciseName ?? "Unknown Exercise",
                    SuccessReps = e.SuccessReps,
                    FailedReps = e.FailedReps,
                    LastPerformed = e.LastPerformed
                }).ToList();
                
                // Get top 5 most recently used exercises
                result.TopExercises = result.AllExercises
                    .OrderByDescending(e => e.LastPerformed)
                    .Take(5)
                    .ToList();
                
                // Calculate totals
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
    
    // Model classes to support legacy code
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