using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Shared
{
    public class SessionModel : SharedPageModel
    {
        private readonly WorkoutTrackerWebContext _context;

        public SessionModel(
            WorkoutTrackerWebContext context,
            IShareTokenService shareTokenService,
            ILogger<SessionModel> logger)
            : base(shareTokenService, logger)
        {
            _context = context;
        }

        public Session Session { get; set; }
        public Dictionary<string, List<Models.Set>> ExerciseSets { get; set; } = new Dictionary<string, List<Models.Set>>();
        public Dictionary<int, List<Rep>> SetReps { get; set; } = new Dictionary<int, List<Rep>>();

        public async Task<IActionResult> OnGetAsync(int id, string token = null)
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

            // Check if the user has access to this specific session
            if (ShareToken.SessionId.HasValue && ShareToken.SessionId.Value != id)
            {
                _logger.LogWarning("User attempted to access session {SessionId} but token only grants access to session {TokenSessionId}", 
                    id, ShareToken.SessionId.Value);
                return RedirectToPage("./AccessDenied");
            }

            // Get session details
            Session = await _context.Session
                .FirstOrDefaultAsync(s => s.SessionId == id && s.UserId == ShareToken.UserId);

            if (Session == null)
            {
                _logger.LogWarning("Session {SessionId} not found for user {UserId}", id, ShareToken.UserId);
                return Page();
            }

            // Get sets for this session
            var sets = await _context.Set
                .Include(s => s.ExerciseType)
                .Where(s => s.SessionId == id)
                .OrderBy(s => s.ExerciseTypeId)
                .ThenBy(s => s.SequenceNum)
                .ToListAsync();

            // Group sets by exercise name
            foreach (var set in sets)
            {
                string exerciseName = set.ExerciseType?.Name ?? "Unknown Exercise";
                
                if (!ExerciseSets.ContainsKey(exerciseName))
                {
                    ExerciseSets[exerciseName] = new List<Models.Set>();
                }
                
                ExerciseSets[exerciseName].Add(set);
            }

            // Get reps for all sets
            if (sets.Any())
            {
                var setIds = sets.Select(s => s.SetId).ToList();
                var reps = await _context.Rep
                    .Where(r => setIds.Contains((int)r.SetsSetId))
                    .ToListAsync();

                // Group reps by set ID
                foreach (var rep in reps)
                {
                    int setId = (int)rep.SetsSetId;
                    
                    if (!SetReps.ContainsKey(setId))
                    {
                        SetReps[setId] = new List<Rep>();
                    }
                    
                    SetReps[setId].Add(rep);
                }
            }

            return Page();
        }
    }
}