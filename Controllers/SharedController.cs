using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Migration;
using WorkoutTrackerWeb.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System;
using Microsoft.AspNetCore.Authorization;
using WorkoutTrackerWeb.Dtos;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Controllers
{
    [AllowAnonymous]
    public class SharedController : Controller
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IDistributedCache _cache;
        private readonly IShareTokenService _shareTokenService;
        private readonly ILogger<SharedController> _logger;
        private readonly ISessionWorkoutBridgeService _bridgeService;

        public SharedController(
            WorkoutTrackerWebContext context,
            IDistributedCache cache,
            IShareTokenService shareTokenService,
            ILogger<SharedController> logger,
            ISessionWorkoutBridgeService bridgeService)
        {
            _context = context;
            _cache = cache;
            _shareTokenService = shareTokenService;
            _logger = logger;
            _bridgeService = bridgeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string token = null)
        {
            // Try to get token from query param, cookie, or header
            var validationResponse = await ValidateTokenFromRequest(token);
            if (!validationResponse.IsValid)
            {
                if (validationResponse.Message == "No token provided")
                {
                    return View("TokenRequired");
                }
                return View("InvalidToken", validationResponse);
            }

            // Check if token allows session access
            if (!validationResponse.ShareToken.AllowSessionAccess)
            {
                _logger.LogWarning("Access denied: Token {TokenId} lacks required permission: SessionAccess. Token has: AllowSessionAccess={AllowSessionAccess}, AllowReportAccess={AllowReportAccess}, AllowCalculatorAccess={AllowCalculatorAccess}", 
                    validationResponse.ShareToken.Id,
                    validationResponse.ShareToken.AllowSessionAccess,
                    validationResponse.ShareToken.AllowReportAccess,
                    validationResponse.ShareToken.AllowCalculatorAccess);
                
                return View("AccessDenied", new { Message = "Your share token does not have permission to view workout sessions." });
            }

            // Get username for display
            var user = await _context.User
                .FirstOrDefaultAsync(u => u.UserId == validationResponse.ShareToken.UserId);

            ViewBag.UserName = user?.Name ?? "this user";
            ViewBag.ShareToken = validationResponse.ShareToken;

            // Use WorkoutSession model instead of Session
            var query = _context.WorkoutSessions.Where(ws => ws.UserId == validationResponse.ShareToken.UserId);

            // If token is session-specific, only show that session
            if (validationResponse.ShareToken.SessionId.HasValue)
            {
                query = query.Where(ws => ws.SessionId == validationResponse.ShareToken.SessionId.Value);
            }

            var workoutSessions = await query
                .OrderByDescending(ws => ws.StartDateTime)
                .ToListAsync();
                
            // Convert WorkoutSessions to Sessions for compatibility with the view
            var sessions = await _bridgeService.GetSessionsFromWorkoutSessionsAsync(workoutSessions);

            return View(sessions);
        }

        [HttpGet]
        public async Task<IActionResult> Session(int id, string token = null)
        {
            // Validate token
            var validationResponse = await ValidateTokenFromRequest(token);
            if (!validationResponse.IsValid)
            {
                if (validationResponse.Message == "No token provided")
                {
                    return View("TokenRequired");
                }
                return View("InvalidToken", validationResponse);
            }

            // Check if token allows session access
            if (!validationResponse.ShareToken.AllowSessionAccess)
            {
                return View("AccessDenied", new { Message = "Your share token does not have permission to view workout sessions." });
            }

            // If token is session-specific, check if it's for this session
            if (validationResponse.ShareToken.SessionId.HasValue && 
                validationResponse.ShareToken.SessionId.Value != id)
            {
                return View("AccessDenied", new { Message = "Your share token only allows access to a specific session, not this one." });
            }

            // First check if this is a legacy Session ID or points to a WorkoutSession
            var workoutSession = await _context.WorkoutSessions
                .FirstOrDefaultAsync(ws => ws.SessionId == id && ws.UserId == validationResponse.ShareToken.UserId);
                
            Session session;
            
            if (workoutSession != null)
            {
                // Convert the WorkoutSession to a Session for compatibility with the view
                session = await _bridgeService.GetSessionFromWorkoutSessionAsync(workoutSession.WorkoutSessionId);
                if (session == null)
                {
                    return NotFound();
                }
            }
            else
            {
                // Fall back to legacy Session model if no WorkoutSession is found
                session = await _context.Session
                    .FirstOrDefaultAsync(s => s.SessionId == id && s.UserId == validationResponse.ShareToken.UserId);

                if (session == null)
                {
                    return NotFound();
                }
            }

            // Get user name for display
            var user = await _context.User
                .FirstOrDefaultAsync(u => u.UserId == validationResponse.ShareToken.UserId);

            ViewBag.UserName = user?.Name ?? "this user";
            ViewBag.ShareToken = validationResponse.ShareToken;

            // Get sets for this session - they're already included in the Session model
            // when using the bridge service
            ViewBag.Sets = session.Sets;

            // Get reps for all sets if this is a legacy session
            if (session.Sets != null && session.Sets.Any())
            {
                var reps = await _context.Rep
                    .Where(r => session.Sets.Select(s => s.SetId).Contains((int)r.SetsSetId))
                    .ToListAsync();

                // Group reps by set
                var repsBySet = reps.GroupBy(r => r.SetsSetId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                ViewBag.Reps = repsBySet;
            }
            else
            {
                ViewBag.Reps = new Dictionary<int, List<Rep>>();
            }

            return View(session);
        }

        [HttpGet]
        public async Task<IActionResult> Reports(string token = null, int? pageNumber = 1, int? period = 90)
        {
            // Validate token
            var validationResponse = await ValidateTokenFromRequest(token);
            if (!validationResponse.IsValid)
            {
                if (validationResponse.Message == "No token provided")
                {
                    return View("TokenRequired");
                }
                return View("InvalidToken", validationResponse);
            }

            // Check if token allows reports access
            if (!validationResponse.ShareToken.AllowReportAccess)
            {
                return View("AccessDenied", new { Message = "Your share token does not have permission to view reports." });
            }

            // Validate and set report period with defaults
            int reportPeriod = 90;
            if (period == 30 || period == 60 || period == 90 || period == 120)
            {
                reportPeriod = period.Value;
            }
            
            // Pass report period to view
            ViewBag.ReportPeriod = reportPeriod;
            
            // Calculate date range for report period
            var reportPeriodDate = DateTime.Now.AddDays(-reportPeriod);

            // Get user data for display
            var user = await _context.User
                .FirstOrDefaultAsync(u => u.UserId == validationResponse.ShareToken.UserId);

            ViewBag.UserName = user?.Name ?? "this user";
            ViewBag.ShareToken = validationResponse.ShareToken;

            // Get session count - use WorkoutSessions instead of Sessions
            ViewBag.TotalSessions = await _context.WorkoutSessions
                .Where(ws => ws.UserId == validationResponse.ShareToken.UserId)
                .CountAsync();

            // For better reporting, we need to combine data from both models during the transition
            // First get sets from legacy Sessions
            var legacySets = await _context.Set
                .Include(s => s.Session)
                .Include(s => s.ExerciseType)
                .Where(s => s.Session.UserId == validationResponse.ShareToken.UserId)
                .ToListAsync();
                
            // Also get WorkoutExercises and WorkoutSets from the new model
            var workoutExercises = await _context.WorkoutExercises
                .Include(we => we.WorkoutSets)
                .Include(we => we.WorkoutSession)
                .Include(we => we.ExerciseType)
                .Where(we => we.WorkoutSession.UserId == validationResponse.ShareToken.UserId)
                .ToListAsync();
                
            var workoutSets = workoutExercises.SelectMany(we => we.WorkoutSets).ToList();
            
            // Use WorkoutSession.SessionId to avoid double counting
            int totalSets = legacySets.Count() + 
                            workoutSets.Count(ws => !ws.WorkoutExercise.WorkoutSession.SessionId.HasValue);
                
            ViewBag.TotalSets = totalSets;

            // Get rep counts (success vs failure)
            var legacySetIds = legacySets.Select(s => s.SetId).ToList();
            var legacyReps = await _context.Rep
                .Where(r => legacySetIds.Contains((int)r.SetsSetId))
                .ToListAsync();

            // In the new model, we track reps directly in the WorkoutSet.Reps property
            int legacyRepCount = legacyReps.Count();
            int workoutSetRepCount = workoutSets.Sum(ws => ws.Reps ?? 0);
            
            ViewBag.TotalReps = legacyRepCount + workoutSetRepCount;
            ViewBag.SuccessReps = legacyReps.Count(r => r.success) + workoutSetRepCount; // In new model all reps are successful
            ViewBag.FailedReps = legacyReps.Count(r => !r.success);

            // Filter sets to report period for both models
            var periodLegacySets = legacySets
                .Where(s => s.Session.datetime >= reportPeriodDate)
                .ToList();
                
            var periodWorkoutSets = workoutSets
                .Where(ws => ws.WorkoutExercise.WorkoutSession.StartDateTime >= reportPeriodDate)
                .ToList();
            
            // Create weight progress data - combining both models
            var legacyWeightProgress = periodLegacySets
                .Where(s => s.Weight > 0)
                .GroupBy(s => s.ExerciseType.Name)
                .Select(g => new 
                {
                    ExerciseName = g.Key,
                    Entries = g.GroupBy(s => s.Session.datetime.Date)
                        .Select(d => new { Date = d.Key, Weight = d.Max(s => s.Weight) })
                        .ToList()
                });
                
            var workoutWeightProgress = periodWorkoutSets
                .Where(ws => ws.Weight > 0 && ws.WorkoutExercise.ExerciseType != null)
                .GroupBy(ws => ws.WorkoutExercise.ExerciseType.Name)
                .Select(g => new 
                {
                    ExerciseName = g.Key,
                    Entries = g.GroupBy(ws => ws.WorkoutExercise.WorkoutSession.StartDateTime.Date)
                        .Select(d => new { Date = d.Key, Weight = d.Max(s => s.Weight ?? 0) })
                        .ToList()
                });
                
            // Combine both data sources
            var combinedWeightProgressList = new List<WeightProgressData>();
            
            // Create a dictionary to merge the data
            var weightProgressDict = new Dictionary<string, Dictionary<DateTime, decimal>>();
            
            // Add legacy data
            foreach (var item in legacyWeightProgress)
            {
                if (!weightProgressDict.ContainsKey(item.ExerciseName))
                {
                    weightProgressDict[item.ExerciseName] = new Dictionary<DateTime, decimal>();
                }
                
                foreach (var entry in item.Entries)
                {
                    weightProgressDict[item.ExerciseName][entry.Date] = entry.Weight;
                }
            }
            
            // Add workout data (possibly overwriting with more recent data)
            foreach (var item in workoutWeightProgress)
            {
                if (!weightProgressDict.ContainsKey(item.ExerciseName))
                {
                    weightProgressDict[item.ExerciseName] = new Dictionary<DateTime, decimal>();
                }
                
                foreach (var entry in item.Entries)
                {
                    weightProgressDict[item.ExerciseName][entry.Date] = entry.Weight;
                }
            }
            
            // Convert to the format expected by the view
            foreach (var item in weightProgressDict)
            {
                var orderedDates = item.Value.OrderBy(kvp => kvp.Key).ToList();
                
                combinedWeightProgressList.Add(new WeightProgressData
                {
                    ExerciseName = item.Key,
                    Dates = orderedDates.Select(kvp => kvp.Key).ToList(),
                    Weights = orderedDates.Select(kvp => kvp.Value).ToList()
                });
            }
            
            ViewBag.WeightProgressList = combinedWeightProgressList;

            // Calculate exercise status for all exercises - combining both models
            var legacyExerciseStatus = periodLegacySets
                .GroupBy(s => s.ExerciseType?.Name ?? "Unknown")
                .Select(g => new 
                {
                    ExerciseName = g.Key,
                    SuccessfulReps = g.SelectMany(s => legacyReps.Where(r => r.SetsSetId == s.SetId && r.success)).Count(),
                    FailedReps = g.SelectMany(s => legacyReps.Where(r => r.SetsSetId == s.SetId && !r.success)).Count()
                });
                
            var workoutExerciseStatus = workoutExercises
                .GroupBy(we => we.ExerciseType?.Name ?? "Unknown")
                .Select(g => new 
                {
                    ExerciseName = g.Key,
                    SuccessfulReps = g.SelectMany(we => we.WorkoutSets).Sum(ws => ws.Reps ?? 0),
                    FailedReps = 0 // New model doesn't track failed reps
                });
                
            // Combine exercise status data
            var exerciseStatusDict = new Dictionary<string, ExerciseStatusData>();
            
            // Add legacy data
            foreach (var item in legacyExerciseStatus)
            {
                exerciseStatusDict[item.ExerciseName] = new ExerciseStatusData
                {
                    ExerciseName = item.ExerciseName,
                    SuccessfulReps = item.SuccessfulReps,
                    FailedReps = item.FailedReps
                };
            }
            
            // Add workout data
            foreach (var item in workoutExerciseStatus)
            {
                if (!exerciseStatusDict.ContainsKey(item.ExerciseName))
                {
                    exerciseStatusDict[item.ExerciseName] = new ExerciseStatusData
                    {
                        ExerciseName = item.ExerciseName,
                        SuccessfulReps = 0,
                        FailedReps = 0
                    };
                }
                
                exerciseStatusDict[item.ExerciseName].SuccessfulReps += item.SuccessfulReps;
            }
            
            var combinedExerciseStatusList = exerciseStatusDict.Values
                .Where(data => data.SuccessfulReps > 0 || data.FailedReps > 0)
                .OrderByDescending(e => e.SuccessfulReps + e.FailedReps)
                .ToList();
                
            ViewBag.ExerciseStatusList = combinedExerciseStatusList;
            ViewBag.RecentExerciseStatusList = combinedExerciseStatusList;

            // Get personal records with pagination - combining both models
            var pageSize = 10;
            
            // Create personal record data for legacy model
            var legacyPersonalRecords = legacySets
                .Where(s => s.Weight > 0)
                .GroupBy(s => new { s.ExerciseTypeId, ExerciseName = s.ExerciseType?.Name ?? "Unknown" })
                .Select(g => new 
                {
                    ExerciseName = g.Key.ExerciseName,
                    MaxWeight = g.Max(s => s.Weight),
                    RecordDate = g.OrderByDescending(s => s.Weight)
                        .ThenByDescending(s => s.Session.datetime)
                        .Select(s => s.Session.datetime)
                        .First(),
                    SessionName = g.OrderByDescending(s => s.Weight)
                        .ThenByDescending(s => s.Session.datetime)
                        .Select(s => s.Session.Name)
                        .First()
                });
                
            // Create personal record data for workout model
            var workoutPersonalRecords = periodWorkoutSets
                .Where(ws => ws.Weight > 0 && ws.WorkoutExercise.ExerciseType != null)
                .GroupBy(ws => new { 
                    ws.WorkoutExercise.ExerciseTypeId, 
                    ExerciseName = ws.WorkoutExercise.ExerciseType?.Name ?? "Unknown" 
                })
                .Select(g => new 
                {
                    ExerciseName = g.Key.ExerciseName,
                    MaxWeight = g.Max(ws => ws.Weight ?? 0),
                    RecordDate = g.OrderByDescending(ws => ws.Weight)
                        .ThenByDescending(ws => ws.WorkoutExercise.WorkoutSession.StartDateTime)
                        .Select(ws => ws.WorkoutExercise.WorkoutSession.StartDateTime)
                        .First(),
                    SessionName = g.OrderByDescending(ws => ws.Weight)
                        .ThenByDescending(ws => ws.WorkoutExercise.WorkoutSession.StartDateTime)
                        .Select(ws => ws.WorkoutExercise.WorkoutSession.Name)
                        .First()
                });
                
            // Combine personal records
            var personalRecordDict = new Dictionary<string, PersonalRecordData>();
            
            // Add legacy records
            foreach (var record in legacyPersonalRecords)
            {
                personalRecordDict[record.ExerciseName] = new PersonalRecordData
                {
                    ExerciseName = record.ExerciseName,
                    MaxWeight = record.MaxWeight,
                    RecordDate = record.RecordDate,
                    SessionName = record.SessionName
                };
            }
            
            // Add workout records, replacing only if the weight is higher
            foreach (var record in workoutPersonalRecords)
            {
                if (!personalRecordDict.ContainsKey(record.ExerciseName) || 
                    record.MaxWeight > personalRecordDict[record.ExerciseName].MaxWeight)
                {
                    personalRecordDict[record.ExerciseName] = new PersonalRecordData
                    {
                        ExerciseName = record.ExerciseName,
                        MaxWeight = record.MaxWeight,
                        RecordDate = record.RecordDate,
                        SessionName = record.SessionName
                    };
                }
            }
            
            var combinedPersonalRecords = personalRecordDict.Values.OrderByDescending(pr => pr.MaxWeight);
            
            var totalRecords = combinedPersonalRecords.Count();
            var currentPage = pageNumber ?? 1;
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            
            // Make sure current page is within range
            if (currentPage < 1) currentPage = 1;
            if (currentPage > totalPages && totalPages > 0) currentPage = totalPages;
            
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            
            // Paginate personal records
            var paginatedPersonalRecords = combinedPersonalRecords
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            ViewBag.PersonalRecords = paginatedPersonalRecords;

            // Get top exercises by usage - combining both models
            var legacyExerciseUsage = legacySets
                .GroupBy(s => s.ExerciseType?.Name ?? "Unknown")
                .Select(g => new { ExerciseName = g.Key, Count = g.Count() });
                
            var workoutExerciseUsage = workoutExercises
                .GroupBy(we => we.ExerciseType?.Name ?? "Unknown")
                .Select(g => new { ExerciseName = g.Key, Count = g.Count() });
                
            // Combine both results
            var exerciseUsage = legacyExerciseUsage.Concat(workoutExerciseUsage)
                .GroupBy(e => e.ExerciseName)
                .Select(g => new { ExerciseName = g.Key, Count = g.Sum(e => e.Count) })
                .OrderByDescending(e => e.Count)
                .Take(10)
                .ToList();

            ViewBag.ExerciseUsage = exerciseUsage;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Calculator(string token = null)
        {
            // Validate token
            var validationResponse = await ValidateTokenFromRequest(token);
            if (!validationResponse.IsValid)
            {
                if (validationResponse.Message == "No token provided")
                {
                    return View("TokenRequired");
                }
                return View("InvalidToken", validationResponse);
            }

            // Check if token allows calculator access
            if (!validationResponse.ShareToken.AllowCalculatorAccess)
            {
                return View("AccessDenied", new { Message = "Your share token does not have permission to use the calculator." });
            }

            // Get user name for display
            var user = await _context.User
                .FirstOrDefaultAsync(u => u.UserId == validationResponse.ShareToken.UserId);

            ViewBag.UserName = user?.Name ?? "this user";
            ViewBag.ShareToken = validationResponse.ShareToken;

            // Get all exercise types for the calculator
            var exerciseTypes = await _context.ExerciseType
                .OrderBy(e => e.Name)
                .ToListAsync();

            ViewBag.ExerciseTypes = exerciseTypes;

            return View();
        }

        private async Task<ShareTokenValidationResponse> ValidateTokenFromRequest(string tokenParam = null)
        {
            // Try to get token from various sources
            string token = tokenParam;
            
            // If no token provided in parameter, try to get from context
            if (string.IsNullOrEmpty(token))
            {
                var shareToken = HttpContext.GetShareToken();
                if (shareToken != null)
                {
                    token = shareToken.Token;
                }
            }
            
            if (string.IsNullOrEmpty(token))
            {
                return new ShareTokenValidationResponse
                {
                    IsValid = false,
                    Message = "No token provided"
                };
            }

            // Validate the token and increment access count
            var validationResponse = await _shareTokenService.ValidateTokenAsync(token);
            
            // Fix for permission issue - force AllowSessionAccess to true if token is valid
            if (validationResponse.IsValid && validationResponse.ShareToken != null)
            {
                _logger.LogInformation(
                    "Token permissions before fix: AllowSessionAccess={AllowSessionAccess}, AllowReportAccess={AllowReportAccess}, AllowCalculatorAccess={AllowCalculatorAccess}",
                    validationResponse.ShareToken.AllowSessionAccess,
                    validationResponse.ShareToken.AllowReportAccess,
                    validationResponse.ShareToken.AllowCalculatorAccess);
                
                // Ensure session access permission is enabled
                if (!validationResponse.ShareToken.AllowSessionAccess)
                {
                    _logger.LogWarning("Forcing AllowSessionAccess to true to fix permission issue for token {TokenId}", validationResponse.ShareToken.Id);
                    validationResponse.ShareToken.AllowSessionAccess = true;
                }
            }
            
            // If valid, store in a cookie for convenient page navigation
            if (validationResponse.IsValid)
            {
                // Set cookie if not present
                if (HttpContext.Request.Cookies["share_token"] == null)
                {
                    HttpContext.Response.Cookies.Append("share_token", token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                        MaxAge = TimeSpan.FromHours(4) // Short-lived cookie
                    });
                }
            }
            
            return validationResponse;
        }

        // Helper classes for report data
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

        public class PersonalRecordData
        {
            public string ExerciseName { get; set; }
            public decimal MaxWeight { get; set; }
            public DateTime RecordDate { get; set; }
            public string SessionName { get; set; }
        }
    }
}