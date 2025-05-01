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
using WorkoutTrackerWeb.Services.Migration;
using WorkoutTrackerWeb.Services.Calculations;
using Microsoft.AspNetCore.OutputCaching;

namespace WorkoutTrackerWeb.Pages.Shared
{
    [OutputCache(PolicyName = "SharedWorkout")]
    public class IndexModel : SharedPageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IDistributedCache _cache;
        private readonly UserService _userService;
        private readonly ISessionWorkoutBridgeService _bridgeService;
        private readonly VolumeCalculationService _volumeCalculationService;

        public IndexModel(
            WorkoutTrackerWebContext context,
            IDistributedCache cache,
            IShareTokenService shareTokenService,
            UserService userService,
            ISessionWorkoutBridgeService bridgeService,
            VolumeCalculationService volumeCalculationService,
            ILogger<IndexModel> logger) 
            : base(shareTokenService, logger)
        {
            _context = context;
            _cache = cache;
            _userService = userService;
            _bridgeService = bridgeService;
            _volumeCalculationService = volumeCalculationService;
        }

        public List<Session> Sessions { get; set; } = new List<Session>();
        public Dictionary<int, SessionStatsModel> SessionStats { get; set; } = new Dictionary<int, SessionStatsModel>();

        public class SessionStatsModel
        {
            public int ExerciseCount { get; set; }
            public int SetCount { get; set; }
            public decimal TotalVolume { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string token = null)
        {
            // Validate token and check permissions
            bool isValid = await ValidateTokenAsync(token, "SessionAccess");
            if (!isValid)
            {
                if (string.IsNullOrEmpty(token) && !HttpContext.Request.Cookies.ContainsKey("share_token"))
                {
                    return RedirectToPage("./TokenRequired");
                }
                return RedirectToPage("./InvalidToken");
            }

            // Get user name for display
            var user = await _context.User.FirstOrDefaultAsync(u => u.UserId == ShareToken.UserId);
            UserName = user?.Name ?? "this user";

            // Using WorkoutSession instead of Session as the primary data source
            var query = _context.WorkoutSessions.Where(ws => ws.UserId == ShareToken.UserId);

            // If token is session-specific, only show that session
            if (ShareToken.SessionId.HasValue)
            {
                query = query.Where(ws => ws.SessionId == ShareToken.SessionId.Value);
            }

            var workoutSessions = await query.OrderByDescending(ws => ws.StartDateTime).ToListAsync();
            
            // Convert WorkoutSessions to Sessions for compatibility with the view
            Sessions = await _bridgeService.GetSessionsFromWorkoutSessionsAsync(workoutSessions);

            // Get stats for each session
            await LoadSessionStatsAsync();

            return Page();
        }

        private async Task LoadSessionStatsAsync()
        {
            if (!Sessions.Any()) return;

            // Calculate stats for each session
            foreach (var session in Sessions)
            {
                // Check if this session has a WorkoutSession equivalent
                var workoutSession = await _context.WorkoutSessions
                    .FirstOrDefaultAsync(ws => ws.SessionId == session.SessionId);
                
                if (workoutSession != null)
                {
                    // Use the volume calculation service for WorkoutSession
                    var totalVolume = await _volumeCalculationService.CalculateWorkoutSessionVolumeAsync(workoutSession.WorkoutSessionId);
                    
                    // Get exercise count and set count for the workout session
                    var workoutExercises = await _context.WorkoutExercises
                        .Include(we => we.WorkoutSets)
                        .Where(we => we.WorkoutSessionId == workoutSession.WorkoutSessionId)
                        .ToListAsync();
                        
                    var exerciseCount = workoutExercises.Count;
                    var setCount = workoutExercises.Sum(we => we.WorkoutSets.Count);
                    
                    SessionStats[session.SessionId] = new SessionStatsModel
                    {
                        ExerciseCount = exerciseCount,
                        SetCount = setCount,
                        TotalVolume = (decimal)totalVolume
                    };
                }
                else
                {
                    // Fall back to legacy approach for sessions without WorkoutSession equivalent
                    await CalculateLegacySessionStatsAsync(session);
                }
            }
        }
        
        private async Task CalculateLegacySessionStatsAsync(Session session)
        {
            // Get sets for this session
            var sets = await _context.Set
                .Include(s => s.ExerciseType)
                .Where(s => s.SessionId == session.SessionId)
                .Select(s => new
                {
                    s.SetId,
                    s.ExerciseTypeId,
                    s.NumberReps,
                    s.Weight
                })
                .ToListAsync();

            // Get all set IDs
            var setIds = sets.Select(s => s.SetId).ToList();

            // Get reps for all sets
            var reps = await _context.Rep
                .Where(r => setIds.Contains((int)r.SetsSetId))
                .ToListAsync();

            // Calculate stats
            var exerciseCount = sets.Select(s => s.ExerciseTypeId).Distinct().Count();
            var setCount = sets.Count;
            
            // Calculate total volume (weight * reps)
            decimal totalVolume = 0;
            foreach (var set in sets)
            {
                var setReps = reps.Where(r => r.SetsSetId == set.SetId).ToList();
                if (setReps.Any())
                {
                    // Sum all rep weights - handle nulls properly
                    decimal setVolume = 0;
                    foreach (var rep in setReps)
                    {
                        // Create a proper nullable decimal variable
                        decimal? weight = rep.weight;
                        if (weight.HasValue)
                        {
                            setVolume += weight.Value;
                        }
                    }
                    totalVolume += setVolume;
                }
                else if (set.NumberReps > 0 && set.Weight > 0)
                {
                    totalVolume += set.NumberReps * set.Weight;
                }
            }

            SessionStats[session.SessionId] = new SessionStatsModel
            {
                ExerciseCount = exerciseCount,
                SetCount = setCount,
                TotalVolume = totalVolume
            };
        }
    }
}