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
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace WorkoutTrackerWeb.Pages.Reports
{
    [Authorize]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Client, NoStore = false)]
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger _logger;
        private readonly IVolumeCalculationService _volumeCalculationService;
        private readonly ICalorieCalculationService _calorieCalculationService;
        private readonly IServiceProvider _serviceProvider;
        public const int PageSize = 10;
        private const int CacheDurationMinutes = 5;
        private const int QueryTimeoutSeconds = 10;

        // Flag to indicate if cache is available (Redis)
        private bool _isCacheAvailable = true;

        public IndexModel(
            WorkoutTrackerWebContext context, 
            IDistributedCache cache,
            IVolumeCalculationService volumeCalculationService,
            ICalorieCalculationService calorieCalculationService,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _cache = cache;
            _logger = Log.ForContext<IndexModel>();
            _volumeCalculationService = volumeCalculationService;
            _calorieCalculationService = calorieCalculationService;
            _serviceProvider = serviceProvider;
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
            if (!_isCacheAvailable)
            {
                return null;
            }

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
                // Log the error and mark cache as unavailable
                _logger.Warning(ex, "Cache unavailable. Error retrieving data from cache with key {CacheKey}", cacheKey);
                _isCacheAvailable = false;
                return null;
            }
        }

        private void SetCache<T>(string cacheKey, T data) where T : class
        {
            if (!_isCacheAvailable)
            {
                return;
            }

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
                // Log the error and mark cache as unavailable
                _logger.Warning(ex, "Cache unavailable. Failed to cache data with key {CacheKey}", cacheKey);
                _isCacheAvailable = false;
            }
        }

        private async Task LoadUserMetricsAsync()
        {
            try {
                var sessions = await _context.WorkoutSessions
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.ExerciseType)
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.WorkoutSets)
                    .Where(ws => ws.UserId == UserId)
                    .OrderByDescending(ws => ws.StartDateTime)
                    .ToListAsync(); // Load data into memory first, then filter
                    
                // Process data in memory to safely handle null values
                var processedSessions = sessions
                    .Where(ws => ws != null)
                    .Select(ws => new WorkoutSession
                    {
                        WorkoutSessionId = ws.WorkoutSessionId,
                        UserId = ws.UserId,
                        Name = ws.Name ?? "Unnamed Session",
                        Description = ws.Description ?? string.Empty,
                        StartDateTime = ws.StartDateTime,
                        Duration = ws.Duration,
                        CaloriesBurned = ws.CaloriesBurned ?? 0m,
                        Notes = ws.Notes ?? string.Empty,
                        IsCompleted = ws.IsCompleted,
                        WorkoutExercises = ws.WorkoutExercises != null ?
                            ws.WorkoutExercises
                                .Where(we => we != null && we.ExerciseType != null && !string.IsNullOrEmpty(we.ExerciseType.Name))
                                .Select(we => new WorkoutExercise
                                {
                                    WorkoutExerciseId = we.WorkoutExerciseId,
                                    ExerciseTypeId = we.ExerciseTypeId,
                                    ExerciseType = we.ExerciseType,
                                    WorkoutSessionId = ws.WorkoutSessionId,
                                    WorkoutSession = null, // Avoid circular references
                                    WorkoutSets = we.WorkoutSets != null ?
                                        we.WorkoutSets
                                            .Where(s => s != null)
                                            .Select(s => new WorkoutSet
                                            {
                                                WorkoutSetId = s.WorkoutSetId,
                                                Reps = s.Reps,
                                                Weight = s.Weight,
                                                IsCompleted = s.IsCompleted,
                                                WorkoutExerciseId = we.WorkoutExerciseId
                                            })
                                            .ToList() :
                                        new List<WorkoutSet>()
                                })
                                .ToList() : 
                            new List<WorkoutExercise>()
                    })
                    .ToList();

                // Initialize dictionaries to avoid null reference issues
                var exerciseVolumes = new Dictionary<string, decimal>();
                var exerciseCalories = new Dictionary<string, decimal>();
                var exerciseWeightProgresses = new Dictionary<string, List<WeightProgressPoint>>();

                // Initialize basic metrics even if there's no data
                WeightProgressList = new List<WeightProgressData>();
                VolumeDataList = new List<VolumeData>();
                CalorieDataList = new List<CalorieData>();
                OverallStatus = new RepStatusData { SuccessfulReps = 0, FailedReps = 0 };
                ExerciseStatusList = new List<ExerciseStatusData>();
                RecentExerciseStatusList = new List<ExerciseStatusData>();
                
                if (processedSessions.Any())
                {
                    // Process sessions for exercise metrics
                    foreach (var session in processedSessions.OrderBy(s => s.StartDateTime))
                    {
                        try {
                            // Get volume data for this session
                            var sessionVolumes = _volumeCalculationService.CalculateSessionVolume(session);
                            var sessionCalories = await _calorieCalculationService.CalculateSessionCaloriesByExercise(session.WorkoutSessionId);

                            // Aggregate volumes
                            foreach (var kvp in sessionVolumes)
                            {
                                if (string.IsNullOrEmpty(kvp.Key)) continue;
                                
                                string exercise = kvp.Key;
                                decimal volume = kvp.Value;
                                if (!exerciseVolumes.ContainsKey(exercise))
                                    exerciseVolumes[exercise] = 0;
                                exerciseVolumes[exercise] += volume;
                            }

                            // Aggregate calories
                            foreach (var kvp in sessionCalories)
                            {
                                if (string.IsNullOrEmpty(kvp.Key)) continue;
                                
                                string exercise = kvp.Key;
                                decimal calories = kvp.Value;
                                if (!exerciseCalories.ContainsKey(exercise))
                                    exerciseCalories[exercise] = 0;
                                exerciseCalories[exercise] += calories;
                            }

                            // Track weight progress data
                            foreach (var exercise in session.WorkoutExercises)
                            {
                                if (exercise.ExerciseType == null || string.IsNullOrEmpty(exercise.ExerciseType.Name))
                                    continue;
                                    
                                var exerciseName = exercise.ExerciseType.Name;
                                
                                // Get max weight for this exercise in this session
                                var maxWeightSet = exercise.WorkoutSets
                                    .Where(s => s.Weight.HasValue && s.Weight > 0)
                                    .OrderByDescending(s => s.Weight)
                                    .FirstOrDefault();
                                    
                                if (maxWeightSet != null)
                                {
                                    if (!exerciseWeightProgresses.ContainsKey(exerciseName))
                                        exerciseWeightProgresses[exerciseName] = new List<WeightProgressPoint>();
                                        
                                    exerciseWeightProgresses[exerciseName].Add(new WeightProgressPoint {
                                        Date = session.StartDateTime,
                                        Weight = maxWeightSet.Weight.Value
                                    });
                                }
                            }

                            // Calculate rep success/failure statistics
                            foreach (var exercise in session.WorkoutExercises)
                            {
                                if (exercise.ExerciseType == null || string.IsNullOrEmpty(exercise.ExerciseType.Name))
                                    continue;
                                    
                                var exerciseName = exercise.ExerciseType.Name;
                                var successfulReps = exercise.WorkoutSets.Sum(s => s.IsCompleted && s.Reps.HasValue ? s.Reps.Value : 0);
                                var failedReps = exercise.WorkoutSets.Sum(s => !s.IsCompleted && s.Reps.HasValue ? s.Reps.Value : 0);
                                
                                // Update overall stats
                                OverallStatus.SuccessfulReps += successfulReps;
                                OverallStatus.FailedReps += failedReps;
                                
                                // Update per-exercise stats
                                var existingExercise = ExerciseStatusList.FirstOrDefault(e => e.ExerciseName == exerciseName);
                                if (existingExercise != null)
                                {
                                    existingExercise.SuccessfulReps += successfulReps;
                                    existingExercise.FailedReps += failedReps;
                                }
                                else
                                {
                                    ExerciseStatusList.Add(new ExerciseStatusData {
                                        ExerciseName = exerciseName,
                                        SuccessfulReps = successfulReps,
                                        FailedReps = failedReps
                                    });
                                }
                            }
                        }
                        catch (Exception ex) {
                            _logger.Warning(ex, "Error processing session {SessionId} for metrics", session.WorkoutSessionId);
                            // Continue processing other sessions even if one fails
                        }
                    }
                    
                    // Build weight progress data (even if there's just one data point)
                    foreach (var kvp in exerciseWeightProgresses)
                    {
                        if (kvp.Value.Count > 0)
                        {
                            var progressData = new WeightProgressData {
                                ExerciseName = kvp.Key,
                                Dates = kvp.Value.Select(p => p.Date).ToList(),
                                Weights = kvp.Value.Select(p => p.Weight).ToList()
                            };
                            
                            // If there's only one data point, duplicate it to allow plotting (many chart libraries need at least 2 points)
                            if (progressData.Dates.Count == 1)
                            {
                                // Add a second point 1 day before (makes it look like the weight just started)
                                progressData.Dates.Insert(0, progressData.Dates[0].AddDays(-1));
                                progressData.Weights.Insert(0, progressData.Weights[0]);
                            }
                            
                            WeightProgressList.Add(progressData);
                        }
                    }
                    
                    // Build volume data list (even if there's just one data point)
                    foreach (var exerciseGroup in processedSessions
                        .SelectMany(s => s.WorkoutExercises)
                        .Where(e => e.ExerciseType != null)
                        .GroupBy(e => e.ExerciseType.Name))
                    {
                        var exerciseName = exerciseGroup.Key;
                        var sessionsList = exerciseGroup
                            .Select(e => e.WorkoutSession)
                            .Where(s => s != null)
                            .OrderBy(s => s.StartDateTime)
                            .ToList();
                        
                        if (sessionsList.Any())
                        {
                            var volumeData = new VolumeData {
                                ExerciseName = exerciseName,
                                Dates = new List<DateTime>(),
                                Volumes = new List<double>()
                            };
                            
                            foreach (var s in sessionsList.OrderBy(s => s.StartDateTime))
                            {
                                var volume = s.WorkoutExercises
                                    .Where(e => e.ExerciseType?.Name == exerciseName)
                                    .Sum(e => e.WorkoutSets
                                        .Sum(set => (set.Weight ?? 0) * (set.Reps ?? 0)));
                                        
                                volumeData.Dates.Add(s.StartDateTime);
                                volumeData.Volumes.Add((double)volume);
                                volumeData.TotalVolume += (double)volume;
                            }
                            
                            // If there's only one data point, duplicate it to allow plotting
                            if (volumeData.Dates.Count == 1)
                            {
                                // Add a second point 1 day before (makes it look like the volume just started)
                                volumeData.Dates.Insert(0, volumeData.Dates[0].AddDays(-1));
                                volumeData.Volumes.Insert(0, 0); // Start from zero volume
                            }
                            
                            VolumeDataList.Add(volumeData);
                        }
                    }
                    
                    // Build calories data - calculate total calories by exercise
                    foreach (var exerciseGroup in processedSessions
                        .SelectMany(s => s.WorkoutExercises)
                        .Where(e => e.ExerciseType != null)
                        .GroupBy(e => e.ExerciseType.Name))
                    {
                        var exerciseName = exerciseGroup.Key;
                        
                        var calorieData = new CalorieData {
                            ExerciseName = exerciseName,
                            Dates = new List<DateTime>(),
                            Calories = new List<double>()
                        };
                        
                        // Get total calories from the already collected data
                        if (exerciseCalories.TryGetValue(exerciseName, out var totalCals))
                        {
                            calorieData.TotalCalories = (double)totalCals;
                        }
                        
                        // Even if we don't have detailed time series data, at least set the total
                        CalorieDataList.Add(calorieData);
                    }
                    
                    // Build recent exercise status list (top 10 by total reps)
                    RecentExerciseStatusList = ExerciseStatusList
                        .OrderByDescending(e => e.SuccessfulReps + e.FailedReps)
                        .Take(10)
                        .ToList();

                    // For empty list, add dummy item
                    if (!RecentExerciseStatusList.Any())
                    {
                        RecentExerciseStatusList.Add(new ExerciseStatusData { 
                            ExerciseName = "No Exercise Data", 
                            SuccessfulReps = 0, 
                            FailedReps = 0 
                        });
                    }
                }

                // Get top exercises by volume
                TopExercisesByVolume = exerciseVolumes.Any() ? 
                    exerciseVolumes
                        .OrderByDescending(kv => kv.Value)
                        .Take(5)
                        .ToDictionary(kv => kv.Key, kv => kv.Value) :
                    new Dictionary<string, decimal>();

                // Get total volume and calories
                TotalVolume = exerciseVolumes.Values.Sum();
                TotalWorkoutVolume = (double)TotalVolume; // Set TotalWorkoutVolume from TotalVolume
                TotalCalories = exerciseCalories.Values.Sum();
                TotalCaloriesBurned = (double)TotalCalories; // Set TotalCaloriesBurned from TotalCalories

                // Calculate workout trends over the selected period
                var periodWorkouts = processedSessions
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
                ExerciseFrequencies = periodWorkouts.Any() ?
                    periodWorkouts
                        .SelectMany(ws => ws.WorkoutExercises
                            .Where(we => we.ExerciseType != null && !string.IsNullOrEmpty(we.ExerciseType.Name))
                            .Select(we => we.ExerciseType.Name))
                        .GroupBy(name => name)
                        .OrderByDescending(g => g.Count())
                        .Take(10)
                        .ToDictionary(g => g.Key, g => g.Count()) :
                    new Dictionary<string, int>();

                // Calculate volume trends
                VolumeTrends = periodWorkouts.Any() ?
                    periodWorkouts
                        .GroupBy(ws => ws.StartDateTime.Date)
                        .OrderBy(g => g.Key)
                        .Select(g => new TrendPoint 
                        {
                            Date = g.Key,
                            Value = g.Sum(ws => 
                                ws.WorkoutExercises
                                    .Where(we => we != null)
                                    .Sum(we => 
                                        we.WorkoutSets
                                            .Where(s => s != null)
                                            .Sum(s => 
                                                (s.Weight ?? 0) * (s.Reps ?? 0))))
                        })
                        .ToList() :
                    new List<TrendPoint>();
                
                // If there's only one trend point, add a zero point before it to allow plotting a line
                if (VolumeTrends.Count == 1)
                {
                    VolumeTrends.Insert(0, new TrendPoint
                    {
                        Date = VolumeTrends[0].Date.AddDays(-1),
                        Value = 0
                    });
                }
            }
            catch (Exception ex) {
                _logger.Error(ex, "Failed to load user metrics");
                InitializeEmptyMetricsData();
            }
        }

        // Helper class for weight progress tracking
        private class WeightProgressPoint
        {
            public DateTime Date { get; set; }
            public decimal Weight { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? pageNumber = 1, int? period = 90)
        {
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            UserId = user.UserId;

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

            // Performance optimization: Create a separate connection with timeout
            using (var scope = _serviceProvider.CreateScope())
            {
                var optionsBuilder = new DbContextOptionsBuilder<WorkoutTrackerWebContext>();
                optionsBuilder.UseSqlServer(_context.Database.GetConnectionString(), 
                    options => options.CommandTimeout(QueryTimeoutSeconds));

                using (var timeoutContext = new WorkoutTrackerWebContext(optionsBuilder.Options))
                {
                    try
                    {
                        // Try to get cached data for each report component separately
                        var overallStatusCacheKey = GetCacheKey($"OverallStatus_{ReportPeriod}", UserId);
                        var exerciseStatusCacheKey = GetCacheKey($"ExerciseStatus_{ReportPeriod}", UserId);
                        var recentExerciseStatusCacheKey = GetCacheKey($"RecentExerciseStatus_{ReportPeriod}", UserId);
                        var weightProgressCacheKey = GetCacheKey($"WeightProgress_{ReportPeriod}", UserId);
                        var volumeDataCacheKey = GetCacheKey($"VolumeData_{ReportPeriod}", UserId);
                        var calorieDataCacheKey = GetCacheKey($"CalorieData_{ReportPeriod}", UserId);

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
                            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(QueryTimeoutSeconds)))
                            {
                                // Optimize queries by combining related data fetching
                                // Break down into smaller chunks with pagination to prevent timeouts
                                var allWorkoutSets = new List<WorkoutSet>();
                                var pageSize = 500;
                                var page = 0;
                                bool hasMoreData = true;

                                while (hasMoreData)
                                {
                                    var pagedSets = await timeoutContext.WorkoutSets
                                        .Include(ws => ws.WorkoutExercise)
                                            .ThenInclude(we => we.WorkoutSession)
                                        .Include(ws => ws.WorkoutExercise)
                                            .ThenInclude(we => we.ExerciseType)
                                        .Where(ws => ws.WorkoutExercise != null &&
                                                  ws.WorkoutExercise.WorkoutSession != null &&
                                                  ws.WorkoutExercise.WorkoutSession.UserId == UserId &&
                                                  ws.WorkoutExercise.WorkoutSession.StartDateTime >= reportPeriodDate)
                                        .OrderBy(ws => ws.WorkoutSetId) // For pagination consistency
                                        .Skip(page * pageSize)
                                        .Take(pageSize)
                                        .AsNoTracking()
                                        .ToListAsync(cts.Token);

                                    allWorkoutSets.AddRange(pagedSets);
                                    
                                    if (pagedSets.Count < pageSize)
                                    {
                                        hasMoreData = false;
                                    }
                                    page++;
                                }

                                // Filter out records with null exercise types or names in memory
                                allWorkoutSets = allWorkoutSets
                                    .Where(ws => ws.WorkoutExercise?.ExerciseType != null && 
                                              !string.IsNullOrEmpty(ws.WorkoutExercise.ExerciseType.Name))
                                    .ToList();

                                // Get all workout sessions in the period with pagination
                                var allWorkoutSessions = new List<WorkoutSession>();
                                page = 0;
                                hasMoreData = true;

                                while (hasMoreData)
                                {
                                    var pagedSessions = await timeoutContext.WorkoutSessions
                                        .Where(ws => ws.UserId == UserId && ws.StartDateTime >= reportPeriodDate)
                                        .OrderBy(ws => ws.WorkoutSessionId)
                                        .Skip(page * pageSize)
                                        .Take(pageSize)
                                        .AsNoTracking()
                                        .ToListAsync(cts.Token);

                                    allWorkoutSessions.AddRange(pagedSessions);
                                    
                                    if (pagedSessions.Count < pageSize)
                                    {
                                        hasMoreData = false;
                                    }
                                    page++;
                                }

                                // Calculate required data and cache it
                                // Processing logic remains the same but uses our optimized data retrieval
                                OverallStatus = GetFromCache<RepStatusData>(overallStatusCacheKey);
                                ExerciseStatusList = GetFromCache<List<ExerciseStatusData>>(exerciseStatusCacheKey);
                                RecentExerciseStatusList = GetFromCache<List<ExerciseStatusData>>(recentExerciseStatusCacheKey);
                                WeightProgressList = GetFromCache<List<WeightProgressData>>(weightProgressCacheKey);
                                VolumeDataList = GetFromCache<List<VolumeData>>(volumeDataCacheKey);
                                CalorieDataList = GetFromCache<List<CalorieData>>(calorieDataCacheKey);

                                // Personal records are paginated and should be fetched fresh
                                var personalRecordsCacheKey = GetCacheKey($"PersonalRecords_Page{pageNumber}", UserId);
                                var paginationInfoCacheKey = GetCacheKey("PaginationInfo", UserId);
                                
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
                                    try
                                    {
                                        _logger.Information("Fetching personal record data from database for user {UserId}", UserId);
                                        
                                        // Force a reload if the cache isn't invalidating properly
                                        _isCacheAvailable = true;
                                        var forceReload = true;
                                        
                                        if (forceReload || allWorkoutSets.Count == 0)
                                        {
                                            // If we don't have workout sets data loaded yet, query them directly
                                            using (var innerCts = new CancellationTokenSource(TimeSpan.FromSeconds(QueryTimeoutSeconds)))
                                            {
                                                allWorkoutSets = await timeoutContext.WorkoutSets
                                                    .Include(ws => ws.WorkoutExercise)
                                                        .ThenInclude(we => we.WorkoutSession)
                                                    .Include(ws => ws.WorkoutExercise)
                                                        .ThenInclude(we => we.ExerciseType)
                                                    .Where(ws => ws.WorkoutExercise != null &&
                                                                ws.WorkoutExercise.WorkoutSession != null &&
                                                                ws.WorkoutExercise.WorkoutSession.UserId == UserId)
                                                    .AsNoTracking()
                                                    .ToListAsync(innerCts.Token);
                                            }
                                        }
                                        
                                        // Process workout sets to get personal records
                                        var groupedSets = allWorkoutSets
                                            .Where(ws => ws.Weight.HasValue && ws.Weight > 0)
                                            .GroupBy(ws => new { 
                                                ExerciseTypeId = ws.WorkoutExercise.ExerciseTypeId, 
                                                ExerciseName = ws.WorkoutExercise.ExerciseType?.Name ?? "Unknown Exercise" 
                                            })
                                            .Select(g => {
                                                // Get the workout set with maximum weight
                                                var maxWeightSet = g.OrderByDescending(ws => ws.Weight)
                                                    .ThenByDescending(ws => ws.WorkoutExercise.WorkoutSession.StartDateTime)
                                                    .FirstOrDefault();

                                                // Safely retrieve session name
                                                string sessionName = "Unnamed Session";
                                                if (maxWeightSet?.WorkoutExercise?.WorkoutSession != null) {
                                                    sessionName = maxWeightSet.WorkoutExercise.WorkoutSession.Name ?? "Unnamed Session";
                                                }

                                                return new PersonalRecordData {
                                                    ExerciseName = g.Key.ExerciseName,
                                                    MaxWeight = g.Max(ws => ws.Weight ?? 0),
                                                    RecordDate = maxWeightSet?.WorkoutExercise?.WorkoutSession?.StartDateTime ?? DateTime.Now,
                                                    SessionName = sessionName
                                                };
                                            })
                                            .OrderByDescending(pr => pr.MaxWeight)
                                            .ToList();

                                        // Always show at least one record for UI display purposes (even if empty)
                                        if (!groupedSets.Any())
                                        {
                                            groupedSets.Add(new PersonalRecordData
                                            {
                                                ExerciseName = "No personal records found",
                                                MaxWeight = 0,
                                                RecordDate = DateTime.Now,
                                                SessionName = "N/A"
                                            });
                                        }

                                        // Handle pagination
                                        var count = groupedSets.Count;
                                        CurrentPage = pageNumber ?? 1;
                                        TotalPages = (int)Math.Ceiling(count / (double)PageSize);
                                        TotalPages = Math.Max(1, TotalPages); // At least 1 page
                                        
                                        // Store pagination info in cache
                                        var newPaginationInfo = new PaginationInfo
                                        {
                                            CurrentPage = CurrentPage,
                                            TotalPages = TotalPages
                                        };
                                        SetCache(paginationInfoCacheKey, newPaginationInfo);

                                        // Apply pagination
                                        PersonalRecords = groupedSets
                                            .Skip((CurrentPage - 1) * PageSize)
                                            .Take(PageSize)
                                            .ToList();
                                            
                                        // Store personal records in cache
                                        SetCache(personalRecordsCacheKey, PersonalRecords);
                                        _logger.Information("Successfully retrieved and cached {Count} personal records", PersonalRecords.Count());
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.Error(ex, "Error retrieving personal records for user {UserId}", UserId);
                                        // Provide empty results rather than crashing
                                        PersonalRecords = new List<PersonalRecordData> {
                                            new PersonalRecordData {
                                                ExerciseName = "No personal records found",
                                                MaxWeight = 0,
                                                RecordDate = DateTime.Now,
                                                SessionName = "N/A"
                                            }
                                        };
                                        CurrentPage = pageNumber ?? 1;
                                        TotalPages = 1;
                                    }
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Warning("Query timeout occurred while loading reports data");
                        // Provide minimal data to show
                        InitializeEmptyReportData();
                        ViewData["ErrorMessage"] = "Report data took too long to load. Try selecting a shorter time period.";
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Error loading report data");
                        // Provide minimal data to show
                        InitializeEmptyReportData();
                        ViewData["ErrorMessage"] = "An error occurred while loading reports. Please try again later.";
                    }
                }
            }

            // Ensure we have user metrics data even if there's no data
            await EnsureUserMetricsDataAsync();

            return Page();
        }
        
        private void InitializeEmptyReportData()
        {
            OverallStatus = new RepStatusData { SuccessfulReps = 0, FailedReps = 0 };
            ExerciseStatusList = new List<ExerciseStatusData>();
            RecentExerciseStatusList = new List<ExerciseStatusData>();
            WeightProgressList = new List<WeightProgressData>();
            VolumeDataList = new List<VolumeData>();
            CalorieDataList = new List<CalorieData>();
            PersonalRecords = new List<PersonalRecordData>();
            CurrentPage = 1;
            TotalPages = 1;
        }

        private async Task EnsureUserMetricsDataAsync()
        {
            if (TopExercisesByVolume == null)
            {
                TopExercisesByVolume = new Dictionary<string, decimal>();
                TotalVolume = 0;
                TotalCalories = 0;
                TotalWorkouts = 0;
                AverageWorkoutDuration = 0;
                CompletionRate = 0;
                ExerciseFrequencies = new Dictionary<string, int>();
                VolumeTrends = new List<TrendPoint>();
                
                try
                {
                    await LoadUserMetricsAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to load user metrics for fallback data");
                }
            }
        }
        
        private void InitializeEmptyMetricsData()
        {
            TopExercisesByVolume = new Dictionary<string, decimal>();
            TotalVolume = 0;
            TotalCalories = 0;
            TotalWorkouts = 0;
            AverageWorkoutDuration = 0;
            CompletionRate = 0;
            ExerciseFrequencies = new Dictionary<string, int>();
            VolumeTrends = new List<TrendPoint>();
        }

        // Method to invalidate cache when data changes
        public static void InvalidateCache(IDistributedCache cache, int userId)
        {
            try
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
                
                // We don't know which page the user was on, so invalidate pages 1-5 which cover most cases
                for (int i = 1; i <= 5; i++)
                {
                    cache.Remove($"Reports:PersonalRecords_Page{i}:{userId}");
                }
            }
            catch (Exception ex)
            {
                // Log but don't crash if cache is unavailable
                Log.ForContext<IndexModel>().Warning(ex, "Failed to invalidate cache for user {UserId}", userId);
            }
        }
    }
}