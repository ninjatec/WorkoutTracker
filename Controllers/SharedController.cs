using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System;
using Microsoft.AspNetCore.Authorization;

namespace WorkoutTrackerWeb.Controllers
{
    [AllowAnonymous]
    public class SharedController : Controller
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IDistributedCache _cache;
        private readonly IShareTokenService _shareTokenService;

        public SharedController(
            WorkoutTrackerWebContext context,
            IDistributedCache cache,
            IShareTokenService shareTokenService)
        {
            _context = context;
            _cache = cache;
            _shareTokenService = shareTokenService;
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
                .Include(s => s.SetType)
                .Where(s => s.SessionId == id)
                .OrderBy(s => s.SetId)
                .ToListAsync();

            ViewBag.Sets = sets;

            // Get reps for all sets
            var reps = await _context.Rep
                .Where(r => sets.Select(s => s.SetId).Contains(r.SetsSetId))
                .ToListAsync();

            // Group reps by set
            var repsBySet = reps.GroupBy(r => r.SetsSetId)
                .ToDictionary(g => g.Key, g => g.ToList());

            ViewBag.Reps = repsBySet;

            return View(session);
        }

        [HttpGet]
        public async Task<IActionResult> Reports(string token = null)
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

            // Get user name for display
            var user = await _context.User
                .FirstOrDefaultAsync(u => u.UserId == validationResponse.ShareToken.UserId);

            ViewBag.UserName = user?.Name ?? "this user";
            ViewBag.ShareToken = validationResponse.ShareToken;

            // Get session count
            ViewBag.TotalSessions = await _context.Session
                .Where(s => s.UserId == validationResponse.ShareToken.UserId)
                .CountAsync();

            // Get total sets count
            var sets = await _context.Set
                .Include(s => s.Session)
                .Where(s => s.Session.UserId == validationResponse.ShareToken.UserId)
                .ToListAsync();

            ViewBag.TotalSets = sets.Count;

            // Get rep counts (success vs failure)
            var reps = await _context.Rep
                .Where(r => sets.Select(s => s.SetId).Contains(r.SetsSetId))
                .ToListAsync();

            ViewBag.TotalReps = reps.Count;
            ViewBag.SuccessReps = reps.Count(r => r.success);
            ViewBag.FailedReps = reps.Count(r => !r.success);

            // Get top 5 exercises by usage
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
            string token = tokenParam ?? HttpContext.GetShareToken();
            
            if (string.IsNullOrEmpty(token))
            {
                return new ShareTokenValidationResponse
                {
                    IsValid = false,
                    Message = "No token provided"
                };
            }

            // Validate the token and increment access count
            var validationResponse = await _shareTokenService.ValidateAndIncrementTokenUsage(token);
            
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
    }
}