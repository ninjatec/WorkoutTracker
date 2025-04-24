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

namespace WorkoutTrackerWeb.Pages.Reports
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger _logger;
        public const int PageSize = 10;
        private const int CacheDurationMinutes = 5;

        public IndexModel(WorkoutTrackerWebContext context, IDistributedCache cache)
        {
            _context = context;
            _cache = cache;
            _logger = Log.ForContext<IndexModel>();
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

        public RepStatusData OverallStatus { get; set; }
        public List<ExerciseStatusData> ExerciseStatusList { get; set; }
        public List<ExerciseStatusData> RecentExerciseStatusList { get; set; }
        public List<WeightProgressData> WeightProgressList { get; set; } = new List<WeightProgressData>();
        public List<PersonalRecordData> PersonalRecords { get; set; } = new List<PersonalRecordData>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int ReportPeriod { get; set; } = 90; // Default to 90 days

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

            // Check and retrieve from cache or compute each report component
            OverallStatus = GetFromCache<RepStatusData>(overallStatusCacheKey);
            ExerciseStatusList = GetFromCache<List<ExerciseStatusData>>(exerciseStatusCacheKey);
            RecentExerciseStatusList = GetFromCache<List<ExerciseStatusData>>(recentExerciseStatusCacheKey);
            WeightProgressList = GetFromCache<List<WeightProgressData>>(weightProgressCacheKey);

            // If any of the cached items are missing, we need to query the database
            if (OverallStatus == null || ExerciseStatusList == null || 
                RecentExerciseStatusList == null || WeightProgressList == null)
            {
                // Optimize queries by combining related data fetching
                var allSetsWithReps = await _context.Set
                    .Include(s => s.Session)
                    .Include(s => s.ExerciseType)
                    .Include(s => s.Reps)
                    .Where(s => s.Session.UserId == user.UserId && s.Session.datetime >= reportPeriodDate)
                    .AsNoTracking()
                    .ToListAsync();

                // Calculate overall status if not in cache
                if (OverallStatus == null)
                {
                    var allReps = allSetsWithReps.SelectMany(s => s.Reps).ToList();
                    OverallStatus = new RepStatusData
                    {
                        SuccessfulReps = allReps.Count(r => r.success),
                        FailedReps = allReps.Count(r => !r.success)
                    };
                    SetCache(overallStatusCacheKey, OverallStatus);
                }

                // Calculate exercise status if not in cache
                if (ExerciseStatusList == null)
                {
                    ExerciseStatusList = allSetsWithReps
                        .GroupBy(s => s.ExerciseType.Name)
                        .Select(g => new ExerciseStatusData
                        {
                            ExerciseName = g.Key,
                            SuccessfulReps = g.SelectMany(s => s.Reps).Count(r => r.success),
                            FailedReps = g.SelectMany(s => s.Reps).Count(r => !r.success)
                        })
                        .Where(data => data.SuccessfulReps > 0 || data.FailedReps > 0)
                        .ToList();
                    
                    SetCache(exerciseStatusCacheKey, ExerciseStatusList);
                }

                // Calculate recent exercise status - uses the same period for consistency
                if (RecentExerciseStatusList == null)
                {
                    RecentExerciseStatusList = allSetsWithReps
                        .GroupBy(s => s.ExerciseType.Name)
                        .Select(g => new ExerciseStatusData
                        {
                            ExerciseName = g.Key,
                            SuccessfulReps = g.SelectMany(s => s.Reps).Count(r => r.success),
                            FailedReps = g.SelectMany(s => s.Reps).Count(r => !r.success)
                        })
                        .Where(data => data.SuccessfulReps > 0 || data.FailedReps > 0)
                        .ToList();
                    
                    SetCache(recentExerciseStatusCacheKey, RecentExerciseStatusList);
                }

                // Calculate weight progress if not in cache
                if (WeightProgressList == null)
                {
                    WeightProgressList = allSetsWithReps
                        .Where(s => s.Weight > 0)
                        .GroupBy(s => s.ExerciseType.Name)
                        .Select(g => new WeightProgressData
                        {
                            ExerciseName = g.Key,
                            Dates = g.GroupBy(s => s.Session.datetime.Date)
                                .Select(d => d.Key)
                                .OrderBy(d => d)
                                .ToList(),
                            Weights = g.GroupBy(s => s.Session.datetime.Date)
                                .Select(d => d.Max(s => s.Weight))
                                .OrderBy(d => d)
                                .ToList()
                        })
                        .Where(w => w.Weights.Any())
                        .ToList();
                    
                    SetCache(weightProgressCacheKey, WeightProgressList);
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
                var query = _context.Set
                    .Include(s => s.Session)
                    .Include(s => s.ExerciseType)
                    .Where(s => s.Session.UserId == user.UserId && s.Weight > 0)
                    .GroupBy(s => new { s.ExerciseTypeId, s.ExerciseType.Name })
                    .Select(g => new PersonalRecordData
                    {
                        ExerciseName = g.Key.Name,
                        MaxWeight = g.Max(s => s.Weight),
                        RecordDate = g.OrderByDescending(s => s.Weight)
                            .ThenByDescending(s => s.Session.datetime)
                            .Select(s => s.Session.datetime)
                            .First(),
                        SessionName = g.OrderByDescending(s => s.Weight)
                            .ThenByDescending(s => s.Session.datetime)
                            .Select(s => s.Session.Name)
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
            }
            
            cache.Remove($"Reports:PaginationInfo:{userId}");
            
            // We don't know which page the user was on, so we can't invalidate specific page cache
            // A better approach would be to store the list of cached pages and invalidate all of them
            // But for simplicity, we'll just invalidate page 1 which is the most common
            cache.Remove($"Reports:PersonalRecords_Page1:{userId}");
        }
    }
}