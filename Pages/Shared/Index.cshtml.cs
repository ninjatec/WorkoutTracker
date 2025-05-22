using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Dtos;

namespace WorkoutTrackerWeb.Pages.Shared
{
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IShareTokenService _shareTokenService;

        public IndexModel(
            WorkoutTrackerWebContext context,
            IShareTokenService shareTokenService)
        {
            _context = context;
            _shareTokenService = shareTokenService;
        }

        public IList<WorkoutSession> WorkoutSessions { get; set; }
        public ShareTokenDto ShareToken { get; set; }

        public async Task<IActionResult> OnGetAsync([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return NotFound();
            }

            var validationResponse = await _shareTokenService.ValidateTokenAsync(token);
            if (!validationResponse.IsValid)
            {
                return BadRequest(validationResponse.Message);
            }

            ShareToken = validationResponse.ShareToken;

            // If token is for specific session, only get that one
            if (ShareToken.WorkoutSessionId.HasValue)
            {
                var session = await _context.WorkoutSessions
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.ExerciseType)
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.WorkoutSets)
                    .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == ShareToken.WorkoutSessionId);

                if (session == null)
                    return NotFound();

                WorkoutSessions = new List<WorkoutSession> { session };
            }
            else
            {
                // Get all sessions for the user
                WorkoutSessions = await _context.WorkoutSessions
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.ExerciseType)
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.WorkoutSets)
                    .Where(ws => ws.UserId == ShareToken.UserId)
                    .OrderByDescending(ws => ws.StartDateTime)
                    .ToListAsync();
            }

            return Page();
        }
    }
}