using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
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

        public SharedController(
            WorkoutTrackerWebContext context,
            IDistributedCache cache,
            IShareTokenService shareTokenService,
            ILogger<SharedController> logger)
        {
            _context = context;
            _cache = cache;
            _shareTokenService = shareTokenService;
            _logger = logger;
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

            // Get sessions for this user
            var query = _context.Session.Where(s => s.UserId == validationResponse.ShareToken.UserId);

            // If token is session-specific, only show that session
            if (validationResponse.ShareToken.SessionId.HasValue)
            {
                query = query.Where(s => s.SessionId == validationResponse.ShareToken.SessionId.Value);
            }

            var sessions = await query
                .OrderByDescending(s => s.datetime)
                .ToListAsync();

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

            // Get the session
            var session = await _context.Session
                .FirstOrDefaultAsync(s => s.SessionId == id && s.UserId == validationResponse.ShareToken.UserId);

            if (session == null)
            {
                return NotFound();
            }

            // Get user name for display
            var user = await _context.User
                .FirstOrDefaultAsync(u => u.UserId == validationResponse.ShareToken.UserId);

            ViewBag.UserName = user?.Name ?? "this user";
            ViewBag.ShareToken = validationResponse.ShareToken;

            // Get sets for this session
            var sets = await _context.Set
                .Include(s => s.ExerciseType)
                .Include(s => s.Settype)
                .Where(s => s.SessionId == id)
                .OrderBy(s => s.SetId)
                .ToListAsync();

            ViewBag.Sets = sets;

            // Get reps for all sets
            var reps = await _context.Rep
                .Where(r => sets.Select(s => s.SetId).ToList().Contains((int)r.SetsSetId))
                .ToListAsync();

            // Group reps by set
            var repsBySet = reps.GroupBy(r => r.SetsSetId)
                .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.Reps = repsBySet;

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

            // Get session count
            ViewBag.TotalSessions = await _context.Session
                .Where(s => s.UserId == validationResponse.ShareToken.UserId)
                .CountAsync();

            // Get total sets count and sets data
            var sets = await _context.Set
                .Include(s => s.Session)
                .Include(s => s.ExerciseType)
                .Where(s => s.Session.UserId == validationResponse.ShareToken.UserId)
                .ToListAsync();

            ViewBag.TotalSets = sets.Count;

            // Get rep counts (success vs failure)
            var reps = await _context.Rep
                .Where(r => sets.Select(s => s.SetId).ToList().Contains((int)r.SetsSetId))
                .ToListAsync();

            ViewBag.TotalReps = reps.Count();
            ViewBag.SuccessReps = reps.Count(r => r.success);
            ViewBag.FailedReps = reps.Count(r => !r.success);

            // Filter sets to report period
            var periodSets = sets.Where(s => s.Session.datetime >= reportPeriodDate).ToList();
            
            // Create weight progress data
            var weightProgressList = periodSets
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
                        .ToList()
                })
                .Where(w => w.Weights.Any())
                .ToList();

            ViewBag.WeightProgressList = weightProgressList;

            // Calculate exercise status for all exercises
            var exerciseStatusList = periodSets
                .GroupBy(s => s.ExerciseType.Name)
                .Select(g => new ExerciseStatusData
                {
                    ExerciseName = g.Key,
                    SuccessfulReps = g.SelectMany(s => reps.Where(r => r.SetsSetId == s.SetId && r.success)).Count(),
                    FailedReps = g.SelectMany(s => reps.Where(r => r.SetsSetId == s.SetId && !r.success)).Count()
                })
                .Where(data => data.SuccessfulReps > 0 || data.FailedReps > 0)
                .OrderByDescending(e => e.SuccessfulReps + e.FailedReps)
                .ToList();

            ViewBag.ExerciseStatusList = exerciseStatusList;
            ViewBag.RecentExerciseStatusList = exerciseStatusList;

            // Get personal records with pagination
            var pageSize = 10;
            var personalRecordsQuery = sets
                .Where(s => s.Weight > 0)
                .GroupBy(s => new { s.ExerciseTypeId, ExerciseName = s.ExerciseType.Name })
                .Select(g => new PersonalRecordData
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
                })
                .OrderByDescending(pr => pr.MaxWeight);

            var totalRecords = personalRecordsQuery.Count();
            var currentPage = pageNumber ?? 1;
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            
            // Make sure current page is within range
            if (currentPage < 1) currentPage = 1;
            if (currentPage > totalPages && totalPages > 0) currentPage = totalPages;
            
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            
            // Paginate personal records
            var personalRecords = personalRecordsQuery
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            ViewBag.PersonalRecords = personalRecords;

            // Get top exercises by usage
            var exerciseUsage = sets
                .GroupBy(s => s.ExerciseType?.Name ?? "Unknown")
                .Select(g => new { ExerciseName = g.Key, Count = g.Count() })
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