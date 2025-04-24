using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Shared
{
    public class ReportsModel : SharedPageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IDistributedCache _cache;
        private readonly UserService _userService;

        public ReportsModel(
            WorkoutTrackerWebContext context,
            IDistributedCache cache,
            IShareTokenService shareTokenService,
            UserService userService,
            ILogger<ReportsModel> logger)
            : base(shareTokenService, logger)
        {
            _context = context;
            _cache = cache;
            _userService = userService;
        }

        public int TotalSessions { get; set; }
        public int TotalSets { get; set; }
        public int TotalReps { get; set; }
        public int SuccessReps { get; set; }
        public int FailedReps { get; set; }
        public int ReportPeriod { get; set; } = 90; // Default to 90 days
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public List<ExerciseUsageData> ExerciseUsage { get; set; } = new List<ExerciseUsageData>();
        public List<VolumeData> RecentVolumes { get; set; } = new List<VolumeData>();
        public List<PersonalRecordData> PersonalRecords { get; set; } = new List<PersonalRecordData>();
        public List<WeightProgressData> WeightProgressList { get; set; } = new List<WeightProgressData>();
        public List<ExerciseStatusData> ExerciseStatusList { get; set; } = new List<ExerciseStatusData>();
        public List<ExerciseStatusData> RecentExerciseStatusList { get; set; } = new List<ExerciseStatusData>();

        public class ExerciseUsageData
        {
            public string ExerciseName { get; set; }
            public int Count { get; set; }
        }

        public class VolumeData
        {
            public DateTime Date { get; set; }
            public decimal Volume { get; set; }
        }

        public class PersonalRecordData
        {
            public string ExerciseName { get; set; }
            public decimal MaxWeight { get; set; }
            public DateTime RecordDate { get; set; }
            public string SessionName { get; set; }
        }

        public class WeightProgressData
        {
            public string ExerciseName { get; set; }
            public List<DateTime> Dates { get; set; } = new List<DateTime>();
            public List<decimal> Weights { get; set; } = new List<decimal>();
        }

        public class ExerciseStatusData
        {
            public string ExerciseName { get; set; }
            public int SuccessfulReps { get; set; }
            public int FailedReps { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string token = null, int? pageNumber = 1, int? period = 90)
        {
            // Validate token and check permissions
            bool isValid = await ValidateTokenAsync(token, "ReportAccess");
            if (!isValid)
            {
                if (string.IsNullOrEmpty(token) && !HttpContext.Request.Cookies.ContainsKey("share_token"))
                {
                    return RedirectToPage("./TokenRequired");
                }
                return RedirectToPage("./InvalidToken");
            }

            // Set report period with defaults
            ReportPeriod = period ?? 90;
            if (ReportPeriod != 30 && ReportPeriod != 60 && ReportPeriod != 90 && ReportPeriod != 120)
            {
                ReportPeriod = 90;
            }

            // Set pagination
            CurrentPage = pageNumber ?? 1;
            if (CurrentPage < 1) CurrentPage = 1;

            // Get user name for display
            var user = await _context.User.FirstOrDefaultAsync(u => u.UserId == ShareToken.UserId);
            UserName = user?.Name ?? "this user";

            // Get total sessions count
            TotalSessions = await _context.Session
                .Where(s => s.UserId == ShareToken.UserId)
                .CountAsync();

            // Calculate date range for report period
            var reportPeriodDate = DateTime.Now.AddDays(-ReportPeriod);

            // Get all sets for this user
            var sets = await _context.Set
                .Include(s => s.Session)
                .Include(s => s.ExerciseType)
                .Where(s => s.Session.UserId == ShareToken.UserId)
                .ToListAsync();

            TotalSets = sets.Count;

            // Get rep counts (success vs failure)
            if (sets.Any())
            {
                var setIds = sets.Select(s => s.SetId).ToList();
                var reps = await _context.Rep
                    .Where(r => setIds.Contains((int)r.SetsSetId))
                    .ToListAsync();

                TotalReps = reps.Count;
                SuccessReps = reps.Count(r => r.success);
                FailedReps = TotalReps - SuccessReps;

                // Get top exercises by usage
                ExerciseUsage = sets
                    .Where(s => s.ExerciseType != null)
                    .GroupBy(s => s.ExerciseType.Name)
                    .Select(g => new ExerciseUsageData
                    {
                        ExerciseName = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(e => e.Count)
                    .Take(10)
                    .ToList();

                // Get recent workout volumes
                RecentVolumes = await GetRecentWorkoutVolumesAsync();

                // Get personal records
                await LoadPersonalRecordsAsync(reportPeriodDate);

                // Get weight progress data
                await LoadWeightProgressDataAsync(reportPeriodDate);

                // Get exercise status data
                await LoadExerciseStatusDataAsync(reportPeriodDate);
            }

            return Page();
        }

        private async Task<List<VolumeData>> GetRecentWorkoutVolumesAsync()
        {
            // Get sessions from the last 30 days
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            
            var sessions = await _context.Session
                .Where(s => s.UserId == ShareToken.UserId && s.datetime >= thirtyDaysAgo)
                .OrderBy(s => s.datetime)
                .ToListAsync();

            var sessionIds = sessions.Select(s => s.SessionId).ToList();
            
            // Get sets for these sessions
            var sets = await _context.Set
                .Where(s => sessionIds.Contains(s.SessionId))
                .ToListAsync();

            var setIds = sets.Select(s => s.SetId).ToList();
            
            // Get reps for these sets
            var reps = await _context.Rep
                .Where(r => setIds.Contains((int)r.SetsSetId))
                .ToListAsync();

            // Group by date and calculate total volume
            var volumeByDate = new Dictionary<DateTime, decimal>();
            
            foreach (var session in sessions)
            {
                var date = session.datetime.Date;
                var sessionSets = sets.Where(s => s.SessionId == session.SessionId).ToList();
                decimal dailyVolume = 0;
                
                foreach (var set in sessionSets)
                {
                    var setReps = reps.Where(r => r.SetsSetId == set.SetId).ToList();
                    
                    if (setReps.Any())
                    {
                        // Calculate volume as sum of weights
                        decimal setVolume = 0;
                        foreach (var rep in setReps)
                        {
                            // Weight is non-nullable (default 0)
                            setVolume += rep.weight;
                        }
                        dailyVolume += setVolume;
                    }
                    else if (set.NumberReps > 0 && set.Weight > 0)
                    {
                        // If no reps, use set data
                        dailyVolume += set.NumberReps * set.Weight;
                    }
                }
                
                if (volumeByDate.ContainsKey(date))
                {
                    volumeByDate[date] += dailyVolume;
                }
                else
                {
                    volumeByDate[date] = dailyVolume;
                }
            }
            
            // Convert to list and ensure we have at least 7 data points
            var result = volumeByDate
                .Select(kvp => new VolumeData { Date = kvp.Key, Volume = kvp.Value })
                .OrderBy(v => v.Date)
                .ToList();
                
            // If we have fewer than 7 data points, add dummy points
            if (result.Count() < 7)
            {
                var earliestDate = result.Any() 
                    ? result.Min(v => v.Date)
                    : DateTime.UtcNow.Date.AddDays(-6);
                    
                for (int i = 0; i < 7; i++)
                {
                    var date = earliestDate.AddDays(i);
                    if (!volumeByDate.ContainsKey(date))
                    {
                        result.Add(new VolumeData { Date = date, Volume = 0 });
                    }
                }
                
                result = result.OrderBy(v => v.Date).ToList();
            }
            
            return result;
        }

        private async Task LoadPersonalRecordsAsync(DateTime reportPeriodDate)
        {
            const int pageSize = 10;

            // Get all reps that have weights and are successful in the period
            var repsWithWeights = await _context.Rep
                .Include(r => r.Set)
                .ThenInclude(s => s.Session)
                .Include(r => r.Set)
                .ThenInclude(s => s.ExerciseType)
                .Where(r => r.Set.Session.UserId == ShareToken.UserId &&
                           r.Set.Session.datetime >= reportPeriodDate &&
                           r.success &&
                           r.weight > 0) // Changed from r.weight.HasValue
                .ToListAsync();

            // Group by exercise type and find max weight for each
            var personalRecordsQuery = repsWithWeights
                .GroupBy(r => new { ExerciseTypeId = r.Set.ExerciseTypeId, ExerciseName = r.Set.ExerciseType?.Name ?? "Unknown" })
                .Select(g => new
                {
                    ExerciseTypeId = g.Key.ExerciseTypeId,
                    ExerciseName = g.Key.ExerciseName,
                    MaxWeightRep = g.OrderByDescending(r => r.weight).FirstOrDefault()
                })
                .Where(x => x.MaxWeightRep != null)
                .OrderByDescending(x => x.MaxWeightRep.weight)
                .ToList();

            // Calculate total pages
            int totalRecords = personalRecordsQuery.Count();
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            // Make sure current page is within range
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            // Paginate personal records
            var pagedRecords = personalRecordsQuery
                .Skip((CurrentPage - 1) * pageSize)
                .Take(pageSize);

            // Map to DTOs
            PersonalRecords = pagedRecords
                .Select(r => new PersonalRecordData
                {
                    ExerciseName = r.ExerciseName,
                    MaxWeight = r.MaxWeightRep.weight, // Changed from r.MaxWeightRep.weight.Value
                    RecordDate = r.MaxWeightRep.Set.Session.datetime,
                    SessionName = r.MaxWeightRep.Set.Session.Name
                })
                .ToList();
        }

        private async Task LoadWeightProgressDataAsync(DateTime reportPeriodDate)
        {
            // Get all successful reps with weights in the period
            var repsWithWeights = await _context.Rep
                .Include(r => r.Set)
                .ThenInclude(s => s.Session)
                .Include(r => r.Set)
                .ThenInclude(s => s.ExerciseType)
                .Where(r => r.Set.Session.UserId == ShareToken.UserId &&
                           r.Set.Session.datetime >= reportPeriodDate &&
                           r.success &&
                           r.weight > 0) // Changed from r.weight.HasValue
                .OrderBy(r => r.Set.Session.datetime)
                .ToListAsync();

            // Group by exercise type and create progress data
            WeightProgressList = repsWithWeights
                .GroupBy(r => new { ExerciseTypeId = r.Set.ExerciseTypeId, ExerciseName = r.Set.ExerciseType?.Name ?? "Unknown" })
                .Select(g => new WeightProgressData
                {
                    ExerciseName = g.Key.ExerciseName,
                    Dates = g.GroupBy(r => r.Set.Session.datetime.Date)
                        .OrderBy(dateGroup => dateGroup.Key)
                        .Select(dateGroup =>
                        {
                            // For each date, get the max weight
                            return dateGroup.Key;
                        }).ToList(),
                    Weights = g.GroupBy(r => r.Set.Session.datetime.Date)
                        .OrderBy(dateGroup => dateGroup.Key)
                        .Select(dateGroup =>
                        {
                            // For each date, get the max weight
                            return dateGroup.Max(r => r.weight); // Changed from r.weight.Value
                        }).ToList()
                })
                .Where(d => d.Dates.Count > 0 && d.Weights.Count > 0)
                .OrderByDescending(d => d.Weights.Max())
                .Take(5) // Top 5 exercises
                .ToList();
        }

        private async Task LoadExerciseStatusDataAsync(DateTime reportPeriodDate)
        {
            // Get all reps in the period
            var reps = await _context.Rep
                .Include(r => r.Set)
                .ThenInclude(s => s.Session)
                .Include(r => r.Set)
                .ThenInclude(s => s.ExerciseType)
                .Where(r => r.Set.Session.UserId == ShareToken.UserId &&
                           r.Set.Session.datetime >= reportPeriodDate)
                .ToListAsync();

            // Group by exercise type and count successful/failed reps
            ExerciseStatusList = reps
                .GroupBy(r => new { ExerciseTypeId = r.Set.ExerciseTypeId, ExerciseName = r.Set.ExerciseType?.Name ?? "Unknown" })
                .Select(g => new ExerciseStatusData
                {
                    ExerciseName = g.Key.ExerciseName,
                    SuccessfulReps = g.Count(r => r.success),
                    FailedReps = g.Count(r => !r.success)
                })
                .OrderByDescending(x => x.SuccessfulReps + x.FailedReps)
                .ToList();

            // Recent exercise status (top 10 by volume)
            RecentExerciseStatusList = ExerciseStatusList
                .OrderByDescending(x => x.SuccessfulReps + x.FailedReps)
                .Take(10)
                .ToList();
        }
    }
}