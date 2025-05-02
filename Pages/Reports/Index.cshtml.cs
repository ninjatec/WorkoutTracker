using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Serilog.ILogger;
using WorkoutTrackerWeb.Services.Calculations;

namespace WorkoutTrackerWeb.Pages.Reports
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger _logger;
        private readonly IVolumeCalculationService _volumeCalculationService;
        private readonly ICalorieCalculationService _calorieCalculationService;
        public const int PageSize = 10;
        private const int CacheDurationMinutes = 5;

        public IndexModel(
            WorkoutTrackerWebContext context, 
            IDistributedCache cache,
            IVolumeCalculationService volumeCalculationService,
            ICalorieCalculationService calorieCalculationService)
        {
            _context = context;
            _cache = cache;
            _logger = Log.ForContext<IndexModel>();
            _volumeCalculationService = volumeCalculationService;
            _calorieCalculationService = calorieCalculationService;
        }

        // Class for pagination info to replace tuple
        public class PaginationInfo
        {
            public int CurrentPage { get; set; }
            public int TotalPages { get; set; }
        }

        public class RepStatusData
        {
            public int SuccessfulReps { get; set; }
            public int FailedReps { get; set; }
        }

        public class ExerciseStatusData
        {
            public string ExerciseName { get; set; }
            public int SuccessfulReps { get; set; }
            public int FailedReps { get; set; }
        }

        public class WeightProgressData
        {
            public string ExerciseName { get; set; }
            public List<DateTime> Dates { get; set; } = new List<DateTime>();
            public List<decimal> Weights { get; set; } = new List<decimal>();
        }

        public class PersonalRecordData
        {
            public string ExerciseName { get; set; }
            public decimal MaxWeight { get; set; }
            public DateTime RecordDate { get; set; }
            public string SessionName { get; set; }
        }

        public class VolumeData
        {
            public string ExerciseName { get; set; }
            public double TotalVolume { get; set; }
            public List<DateTime> Dates { get; set; } = new List<DateTime>();
            public List<double> Volumes { get; set; } = new List<double>();
        }

        public class CalorieData
        {
            public string ExerciseName { get; set; }
            public double TotalCalories { get; set; }
            public List<DateTime> Dates { get; set; } = new List<DateTime>();
            public List<double> Calories { get; set; } = new List<double>();
        }

        public class TrendPoint
        {
            public DateTime Date { get; set; }
            public decimal Value { get; set; }
        }

        public RepStatusData OverallStatus { get; set; }
        public List<ExerciseStatusData> ExerciseStatusList { get; set; }
        public List<ExerciseStatusData> RecentExerciseStatusList { get; set; }
        public List<WeightProgressData> WeightProgressList { get; set; } = new List<WeightProgressData>();
        public List<PersonalRecordData> PersonalRecords { get; set; } = new List<PersonalRecordData>();
        public List<VolumeData> VolumeDataList { get; set; } = new List<VolumeData>();
        public double TotalWorkoutVolume { get; set; }
        public List<CalorieData> CalorieDataList { get; set; } = new List<CalorieData>();
        public double TotalCaloriesBurned { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int ReportPeriod { get; set; } = 90; // Default to 90 days
        public Dictionary<string, decimal> TopExercisesByVolume { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalCalories { get; set; }
        public int TotalWorkouts { get; set; }
        public double AverageWorkoutDuration { get; set; }
        public double CompletionRate { get; set; }
        public Dictionary<string, int> ExerciseFrequencies { get; set; }
        public List<TrendPoint> VolumeTrends { get; set; }
        public DateTime PeriodStart => DateTime.Now.AddDays(-ReportPeriod);
        public DateTime PeriodEnd => DateTime.Now;
        public int UserId { get; set; }

        private string GetCacheKey(string key, int userId) => $"Reports:{key}:{userId}";

        private T GetFromCache<T>(string cacheKey) where T : class
        {
            try
            {
                var cachedData = _cache.Get(cacheKey);
                if (cachedData == null)
                {
                    return null;
                }
                
                var cachedString = Encoding.UTF8.GetString(cachedData);
                return JsonSerializer.Deserialize<T>(cachedString);
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the request
                _logger.Warning(ex, "Error retrieving data from cache with key {CacheKey}", cacheKey);
                return null;
            }
        }

        private void SetCache<T>(string cacheKey, T data) where T : class
        {
            try
            {
                var cacheOptions = new DistributedCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(CacheDurationMinutes))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30)); // Safeguard absolute expiration
                
                var serializedData = JsonSerializer.Serialize(data);
                var encodedData = Encoding.UTF8.GetBytes(serializedData);
                
                _cache.Set(cacheKey, encodedData, cacheOptions);
            }
            catch (Exception ex) 
            {
                // Log the error but don't fail the request - just continue without caching
                _logger.Warning(ex, "Failed to cache data with key {CacheKey}", cacheKey);
            }
        }

        private async Task LoadUserMetricsAsync()
        {
            var sessions = await _context.WorkoutSessions
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.ExerciseType)
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.WorkoutSets)
                .Where(ws => ws.UserId == UserId)
                .OrderByDescending(ws => ws.StartDateTime)
                .ToListAsync();

            // Calculate exercise metrics
            var exerciseVolumes = new Dictionary<string, decimal>();
            var exerciseCalories = new Dictionary<string, decimal>();

            foreach (var session in sessions)
            {
                // Get volume data for this session
                var sessionVolumes = _volumeCalculationService.CalculateSessionVolume(session);
                var sessionCalories = await _calorieCalculationService.CalculateSessionCaloriesByExercise(session.WorkoutSessionId);

                // Aggregate volumes
                foreach (var kvp in sessionVolumes)
                {
                    string exercise = kvp.Key;
                    decimal volume = kvp.Value;
                    if (!exerciseVolumes.ContainsKey(exercise))
                        exerciseVolumes[exercise] = 0;
                    exerciseVolumes[exercise] += volume;
                }

                // Aggregate calories
                foreach (var kvp in sessionCalories)
                {
                    string exercise = kvp.Key;
                    decimal calories = kvp.Value;
                    if (!exerciseCalories.ContainsKey(exercise))
                        exerciseCalories[exercise] = 0;
                    exerciseCalories[exercise] += calories;
                }
            }

            // Get top exercises by volume
            TopExercisesByVolume = exerciseVolumes
                .OrderByDescending(kv => kv.Value)
                .Take(5)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            // Get total volume and calories
            TotalVolume = exerciseVolumes.Values.Sum();
            TotalCalories = exerciseCalories.Values.Sum();

            // Calculate workout trends over the selected period
            var periodWorkouts = sessions
                .Where(ws => ws.StartDateTime >= PeriodStart && ws.StartDateTime <= PeriodEnd)
                .OrderBy(ws => ws.StartDateTime)
                .ToList();

            TotalWorkouts = periodWorkouts.Count;
            AverageWorkoutDuration = periodWorkouts.Any() 
                ? periodWorkouts.Average(ws => ws.Duration) 
                : 0;

            // Calculate completion rate
            var completedWorkouts = periodWorkouts.Count(ws => ws.IsCompleted);
            CompletionRate = TotalWorkouts > 0 
                ? (completedWorkouts * 100.0) / TotalWorkouts 
                : 0;

            // Get exercise frequencies
            ExerciseFrequencies = periodWorkouts
                .SelectMany(ws => ws.WorkoutExercises
                    .Select(we => we.ExerciseType.Name))
                .GroupBy(name => name)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());

            // Calculate volume trends
            VolumeTrends = periodWorkouts
                .GroupBy(ws => ws.StartDateTime.Date)
                .OrderBy(g => g.Key)
                .Select(g => new TrendPoint 
                {
                    Date = g.Key,
                    Value = g.Sum(ws => 
                        ws.WorkoutExercises.Sum(we => 
                            we.WorkoutSets.Sum(s => 
                                (s.Weight ?? 0) * (s.Reps ?? 0))))
                })
                .ToList();
        }

        public async Task<IActionResult> OnGetAsync(int? pageNumber = 1, int? period = 90)
        {
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Set report period with validation (default to 90 if invalid)
            if (period == 30 || period == 60 || period == 90 || period == 120)
            {
                ReportPeriod = period.Value;
            }
            else
            {
                ReportPeriod = 90; // Default if invalid value provided
            }

            var reportPeriodDate = DateTime.Now.AddDays(-ReportPeriod);

            // Try to get cached data for each report component separately
            var overallStatusCacheKey = GetCacheKey($"OverallStatus_{ReportPeriod}", user.UserId);
            var exerciseStatusCacheKey = GetCacheKey($"ExerciseStatus_{ReportPeriod}", user.UserId);
            var recentExerciseStatusCacheKey = GetCacheKey($"RecentExerciseStatus_{ReportPeriod}", user.UserId);
            var weightProgressCacheKey = GetCacheKey($"WeightProgress_{ReportPeriod}", user.UserId);
            var volumeDataCacheKey = GetCacheKey($"VolumeData_{ReportPeriod}", user.UserId);
            var calorieDataCacheKey = GetCacheKey($"CalorieData_{ReportPeriod}", user.UserId);

            // Check and retrieve from cache or compute each report component
            OverallStatus = GetFromCache<RepStatusData>(overallStatusCacheKey);
            ExerciseStatusList = GetFromCache<List<ExerciseStatusData>>(exerciseStatusCacheKey);
            RecentExerciseStatusList = GetFromCache<List<ExerciseStatusData>>(recentExerciseStatusCacheKey);
            WeightProgressList = GetFromCache<List<WeightProgressData>>(weightProgressCacheKey);
            VolumeDataList = GetFromCache<List<VolumeData>>(volumeDataCacheKey);
            CalorieDataList = GetFromCache<List<CalorieData>>(calorieDataCacheKey);

            // If any of the cached items are missing, we need to query the database
            if (OverallStatus == null || ExerciseStatusList == null || 
                RecentExerciseStatusList == null || WeightProgressList == null ||
                VolumeDataList == null || CalorieDataList == null)
            {
                // Optimize queries by combining related data fetching
                var allWorkoutSets = await _context.WorkoutSets
                    .Include(ws => ws.WorkoutExercise)
                        .ThenInclude(we => we.WorkoutSession)
                    .Include(ws => ws.WorkoutExercise)
                        .ThenInclude(we => we.ExerciseType)
                    .Where(ws => ws.WorkoutExercise.WorkoutSession.UserId == user.UserId && 
                                ws.WorkoutExercise.WorkoutSession.StartDateTime >= reportPeriodDate &&
                                ws.WorkoutExercise.ExerciseType != null)
                    .AsNoTracking()
                    .ToListAsync();

                // Get all workout sessions in the period
                var allWorkoutSessions = await _context.WorkoutSessions
                    .Where(ws => ws.UserId == user.UserId && ws.StartDateTime >= reportPeriodDate)
                    .OrderBy(ws => ws.StartDateTime)
                    .AsNoTracking()
                    .ToListAsync();

                // Calculate overall status if not in cache
                if (OverallStatus == null)
                {
                    // Since we don't have direct rep success tracking in WorkoutSet, we'll use completed sets as successful
                    OverallStatus = new RepStatusData
                    {
                        SuccessfulReps = allWorkoutSets.Where(ws => ws.IsCompleted).Sum(ws => ws.Reps ?? 0),
                        FailedReps = allWorkoutSets.Where(ws => !ws.IsCompleted).Sum(ws => ws.Reps ?? 0)
                    };
                    SetCache(overallStatusCacheKey, OverallStatus);
                }

                // Calculate exercise status if not in cache
                if (ExerciseStatusList == null)
                {
                    ExerciseStatusList = allWorkoutSets
                        .Where(ws => ws.WorkoutExercise?.ExerciseType?.Name != null)
                        .GroupBy(ws => ws.WorkoutExercise.ExerciseType.Name)
                        .Select(g => new ExerciseStatusData
                        {
                            ExerciseName = g.Key,
                            SuccessfulReps = g.Where(ws => ws.IsCompleted).Sum(ws => ws.Reps ?? 0),
                            FailedReps = g.Where(ws => !ws.IsCompleted).Sum(ws => ws.Reps ?? 0)
                        })
                        .Where(data => data.SuccessfulReps > 0 || data.FailedReps > 0)
                        .ToList();
                    
                    SetCache(exerciseStatusCacheKey, ExerciseStatusList);
                }

                // Calculate recent exercise status - uses the same period for consistency
                if (RecentExerciseStatusList == null)
                {
                    RecentExerciseStatusList = allWorkoutSets
                        .Where(ws => ws.WorkoutExercise?.ExerciseType?.Name != null)
                        .GroupBy(ws => ws.WorkoutExercise.ExerciseType.Name)
                        .Select(g => new ExerciseStatusData
                        {
                            ExerciseName = g.Key,
                            SuccessfulReps = g.Where(ws => ws.IsCompleted).Sum(ws => ws.Reps ?? 0),
                            FailedReps = g.Where(ws => !ws.IsCompleted).Sum(ws => ws.Reps ?? 0)
                        })
                        .Where(data => data.SuccessfulReps > 0 || data.FailedReps > 0)
                        .ToList();
                    
                    SetCache(recentExerciseStatusCacheKey, RecentExerciseStatusList);
                }

                // Calculate weight progress if not in cache
                if (WeightProgressList == null)
                {
                    WeightProgressList = allWorkoutSets
                        .Where(ws => ws.Weight > 0 && ws.WorkoutExercise?.ExerciseType?.Name != null)
                        .GroupBy(ws => ws.WorkoutExercise.ExerciseType.Name)
                        .Select(g => new WeightProgressData
                        {
                            ExerciseName = g.Key,
                            Dates = g.GroupBy(ws => ws.WorkoutExercise.WorkoutSession.StartDateTime.Date)
                                .Select(d => d.Key)
                                .OrderBy(d => d)
                                .ToList(),
                            Weights = g.GroupBy(ws => ws.WorkoutExercise.WorkoutSession.StartDateTime.Date)
                                .Select(d => d.Max(ws => ws.Weight ?? 0))
                                .OrderBy(d => d)
                                .ToList()
                        })
                        .Where(w => w.Weights.Any())
                        .ToList();
                    
                    SetCache(weightProgressCacheKey, WeightProgressList);
                }

                // Calculate volume data if not in cache
                if (VolumeDataList == null)
                {
                    var exerciseData = allWorkoutSets
                        .Where(ws => ws.WorkoutExercise?.ExerciseType?.Name != null)
                        .GroupBy(ws => ws.WorkoutExercise.ExerciseType.Name)
                        .ToDictionary(
                            g => g.Key,
                            g => g.ToList()
                        );

                    var sessionData = allWorkoutSets
                        .GroupBy(ws => ws.WorkoutExercise.WorkoutSession.StartDateTime.Date)
                        .OrderBy(g => g.Key)
                        .ToList();

                    VolumeDataList = exerciseData.Select(ed => {
                        var volumeData = new VolumeData {
                            ExerciseName = ed.Key,
                            TotalVolume = ed.Value.Sum(ws => _volumeCalculationService.CalculateSetVolume(ws))
                        };

                        // Add data points for each date
                        foreach (var dateGroup in sessionData)
                        {
                            volumeData.Dates.Add(dateGroup.Key);
                            
                            // Find sets for this exercise on this date
                            var setsOnDate = dateGroup
                                .Where(ws => ws.WorkoutExercise.ExerciseType.Name == ed.Key)
                                .ToList();

                            // Calculate volume for this date
                            double volumeOnDate = setsOnDate.Sum(ws => _volumeCalculationService.CalculateSetVolume(ws));
                            volumeData.Volumes.Add(volumeOnDate);
                        }

                        return volumeData;
                    })
                    .OrderByDescending(v => v.TotalVolume)
                    .ToList();

                    TotalWorkoutVolume = VolumeDataList.Sum(v => v.TotalVolume);
                    
                    SetCache(volumeDataCacheKey, VolumeDataList);
                }
                else
                {
                    TotalWorkoutVolume = VolumeDataList.Sum(v => v.TotalVolume);
                }

                // Calculate calorie data if not in cache
                if (CalorieDataList == null)
                {
                    // Get all sessions with their total calories
                    var sessionCalories = new Dictionary<int, double>();
                    foreach (var session in allWorkoutSessions)
                    {
                        var calories = await _calorieCalculationService.CalculateSessionCaloriesAsync(session.WorkoutSessionId);
                        sessionCalories[session.WorkoutSessionId] = (double)calories;
                    }

                    var exerciseData = allWorkoutSets
                        .Where(ws => ws.WorkoutExercise?.ExerciseType?.Name != null)
                        .GroupBy(ws => ws.WorkoutExercise.ExerciseType.Name)
                        .ToDictionary(
                            g => g.Key,
                            g => g.ToList()
                        );

                    var sessionDates = allWorkoutSessions
                        .GroupBy(ws => ws.StartDateTime.Date)
                        .OrderBy(g => g.Key)
                        .ToDictionary(
                            g => g.Key,
                            g => g.ToList()
                        );

                    CalorieDataList = new List<CalorieData>();
                    
                    // For each exercise type, calculate calories over time
                    foreach (var ed in exerciseData)
                    {
                        var calorieData = new CalorieData {
                            ExerciseName = ed.Key
                        };

                        // Calculate calories for each exercise
                        double totalCalories = 0;
                        foreach (var set in ed.Value)
                        {
                            // Create a weighted distribution of calories based on volume proportion
                            double setVolume = _volumeCalculationService.CalculateSetVolume(set);
                            double sessionVolume = allWorkoutSets
                                .Where(ws => ws.WorkoutExercise.WorkoutSessionId == set.WorkoutExercise.WorkoutSessionId)
                                .Sum(ws => _volumeCalculationService.CalculateSetVolume(ws));
                            
                            double proportion = sessionVolume > 0 ? setVolume / sessionVolume : 0;
                            int sessionId = set.WorkoutExercise.WorkoutSessionId;
                            double sessionCalorie = sessionCalories.ContainsKey(sessionId) ? sessionCalories[sessionId] : 0;
                            double caloriesForSet = proportion * sessionCalorie;
                            
                            totalCalories += caloriesForSet;
                        }
                        
                        calorieData.TotalCalories = totalCalories;

                        // Add data points for each date
                        foreach (var dateEntry in sessionDates)
                        {
                            DateTime date = dateEntry.Key;
                            calorieData.Dates.Add(date);
                            
                            // Find sessions on this date
                            var sessionsOnDate = dateEntry.Value;
                            
                            // Calculate calories for this exercise on this date
                            double caloriesOnDate = 0;
                            foreach (var session in sessionsOnDate)
                            {
                                // Get sets for this exercise in this session
                                var setsInSession = allWorkoutSets
                                    .Where(ws => ws.WorkoutExercise.WorkoutSessionId == session.WorkoutSessionId && 
                                               ws.WorkoutExercise.ExerciseType.Name == ed.Key)
                                    .ToList();
                                
                                // Calculate proportion of volume for this exercise
                                double exerciseVolume = setsInSession.Sum(ws => _volumeCalculationService.CalculateSetVolume(ws));
                                double totalSessionVolume = allWorkoutSets
                                    .Where(ws => ws.WorkoutExercise.WorkoutSessionId == session.WorkoutSessionId)
                                    .Sum(ws => _volumeCalculationService.CalculateSetVolume(ws));
                                
                                double proportion = totalSessionVolume > 0 ? exerciseVolume / totalSessionVolume : 0;
                                double sessionCalorie = sessionCalories.ContainsKey(session.WorkoutSessionId) ? 
                                    sessionCalories[session.WorkoutSessionId] : 0;
                                
                                caloriesOnDate += proportion * sessionCalorie;
                            }
                            
                            calorieData.Calories.Add(caloriesOnDate);
                        }

                        CalorieDataList.Add(calorieData);
                    }

                    CalorieDataList = CalorieDataList
                        .OrderByDescending(c => c.TotalCalories)
                        .ToList();

                    TotalCaloriesBurned = CalorieDataList.Sum(c => c.TotalCalories);
                    
                    SetCache(calorieDataCacheKey, CalorieDataList);
                }
                else
                {
                    TotalCaloriesBurned = CalorieDataList.Sum(c => c.TotalCalories);
                }
            }

            // Personal records are paginated and should be fetched fresh
            var personalRecordsCacheKey = GetCacheKey($"PersonalRecords_Page{pageNumber}", user.UserId);
            var paginationInfoCacheKey = GetCacheKey("PaginationInfo", user.UserId);
            
            // Try to get pagination info from cache
            var paginationInfo = GetFromCache<PaginationInfo>(paginationInfoCacheKey);
            if (paginationInfo != null)
            {
                CurrentPage = paginationInfo.CurrentPage;
                TotalPages = paginationInfo.TotalPages;
            }
            
            // Try to get personal records from cache
            PersonalRecords = GetFromCache<List<PersonalRecordData>>(personalRecordsCacheKey);
            
            // If not in cache or different page, fetch from database
            if (PersonalRecords == null || (paginationInfo != null && paginationInfo.CurrentPage != pageNumber))
            {
                var query = _context.WorkoutSets
                    .Include(ws => ws.WorkoutExercise)
                        .ThenInclude(we => we.WorkoutSession)
                    .Include(ws => ws.WorkoutExercise)
                        .ThenInclude(we => we.ExerciseType)
                    .Where(ws => ws.WorkoutExercise.WorkoutSession.UserId == user.UserId && 
                               ws.WorkoutExercise.ExerciseType != null && 
                               ws.Weight > 0)
                    .GroupBy(ws => new { ws.WorkoutExercise.ExerciseTypeId, ws.WorkoutExercise.ExerciseType.Name })
                    .Where(g => g.Key.Name != null)
                    .Select(g => new PersonalRecordData
                    {
                        ExerciseName = g.Key.Name,
                        MaxWeight = g.Max(ws => ws.Weight ?? 0),
                        RecordDate = g.OrderByDescending(ws => ws.Weight)
                            .ThenByDescending(ws => ws.WorkoutExercise.WorkoutSession.StartDateTime)
                            .Select(ws => ws.WorkoutExercise.WorkoutSession.StartDateTime)
                            .First(),
                        SessionName = g.OrderByDescending(ws => ws.Weight)
                            .ThenByDescending(ws => ws.WorkoutExercise.WorkoutSession.StartDateTime)
                            .Select(ws => ws.WorkoutExercise.WorkoutSession.Name ?? "Unnamed Session")
                            .First()
                    })
                    .OrderByDescending(pr => pr.MaxWeight);

                var count = await query.CountAsync();
                CurrentPage = pageNumber ?? 1;
                TotalPages = (int)Math.Ceiling(count / (double)PageSize);
                
                // Store pagination info in cache
                var newPaginationInfo = new PaginationInfo
                {
                    CurrentPage = CurrentPage,
                    TotalPages = TotalPages
                };
                SetCache(paginationInfoCacheKey, newPaginationInfo);

                PersonalRecords = await query
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .AsNoTracking()
                    .ToListAsync();
                    
                // Store personal records in cache
                SetCache(personalRecordsCacheKey, PersonalRecords);
            }

            return Page();
        }
        
        // Method to invalidate cache when data changes
        public static void InvalidateCache(IDistributedCache cache, int userId)
        {
            // Remove cached report data for all time periods
            int[] periods = new[] { 30, 60, 90, 120 };
            foreach (var period in periods)
            {
                cache.Remove($"Reports:OverallStatus_{period}:{userId}");
                cache.Remove($"Reports:ExerciseStatus_{period}:{userId}");
                cache.Remove($"Reports:RecentExerciseStatus_{period}:{userId}");
                cache.Remove($"Reports:WeightProgress_{period}:{userId}");
                cache.Remove($"Reports:VolumeData_{period}:{userId}");
                cache.Remove($"Reports:CalorieData_{period}:{userId}");
            }
            
            cache.Remove($"Reports:PaginationInfo:{userId}");
            
            // We don't know which page the user was on, so we can't invalidate specific page cache
            // A better approach would be to store the list of cached pages and invalidate all of them
            // But for simplicity, we'll just invalidate page 1 which is the most common
            cache.Remove($"Reports:PersonalRecords_Page1:{userId}");
        }
    }
}