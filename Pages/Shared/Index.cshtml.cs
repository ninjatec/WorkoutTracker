using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Shared
{
    public class IndexModel : SharedPageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IDistributedCache _cache;
        private readonly UserService _userService;

        public IndexModel(
            WorkoutTrackerWebContext context,
            IDistributedCache cache,
            IShareTokenService shareTokenService,
            UserService userService,
            ILogger<IndexModel> logger) 
            : base(shareTokenService, logger)
        {
            _context = context;
            _cache = cache;
            _userService = userService;
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

            // Get sessions for this user
            var query = _context.Session.Where(s => s.UserId == ShareToken.UserId);

            // If token is session-specific, only show that session
            if (ShareToken.SessionId.HasValue)
            {
                query = query.Where(s => s.SessionId == ShareToken.SessionId.Value);
            }

            Sessions = await query.OrderByDescending(s => s.datetime).ToListAsync();

            // Get stats for each session
            await LoadSessionStatsAsync();

            return Page();
        }

        private async Task LoadSessionStatsAsync()
        {
            if (!Sessions.Any()) return;

            // Get all session IDs
            var sessionIds = Sessions.Select(s => s.SessionId).ToList();

            // Get sets for all sessions - exclude the problematic SequenceNum column
            var sets = await _context.Set
                .Include(s => s.ExerciseType)
                .Where(s => sessionIds.Contains(s.SessionId))
                .Select(s => new
                {
                    s.SetId,
                    s.SessionId,
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

            // Calculate stats for each session
            foreach (var session in Sessions)
            {
                var sessionSets = sets.Where(s => s.SessionId == session.SessionId).ToList();
                var exerciseCount = sessionSets.Select(s => s.ExerciseTypeId).Distinct().Count();
                var setCount = sessionSets.Count;
                
                // Calculate total volume (weight * reps)
                decimal totalVolume = 0;
                foreach (var set in sessionSets)
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
}