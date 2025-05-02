using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace WorkoutTrackerWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SharedWorkoutController : ControllerBase
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IDistributedCache _cache;
        private readonly IShareTokenService _shareTokenService;
        private readonly QuickWorkoutService _quickWorkoutService;

        public SharedWorkoutController(
            WorkoutTrackerWebContext context,
            IDistributedCache cache,
            IShareTokenService shareTokenService,
            QuickWorkoutService quickWorkoutService)
        {
            _context = context;
            _cache = cache;
            _shareTokenService = shareTokenService;
            _quickWorkoutService = quickWorkoutService;
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions([FromQuery] string token)
        {
            var validationResponse = await _shareTokenService.ValidateTokenAsync(token, false);
            if (!validationResponse.IsValid)
            {
                return BadRequest(new { error = validationResponse.Message });
            }

            if (!validationResponse.ShareToken.AllowSessionAccess)
            {
                return Forbid();
            }

            // Check if token is for a specific workout session
            if (validationResponse.ShareToken.WorkoutSessionId.HasValue)
            {
                var session = await _context.WorkoutSessions
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.WorkoutSets)
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.ExerciseType)
                    .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == validationResponse.ShareToken.WorkoutSessionId.Value);

                return Ok(new[] { session });
            }

            // Get all sessions for the user
            var sessions = await _context.WorkoutSessions
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.WorkoutSets)
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.ExerciseType)
                .Where(ws => ws.UserId == validationResponse.ShareToken.UserId)
                .OrderByDescending(ws => ws.StartDateTime)
                .ToListAsync();

            return Ok(sessions);
        }

        [HttpGet("sessions/{id}")]
        public async Task<IActionResult> GetSession(int id, [FromQuery] string token)
        {
            var validationResponse = await _shareTokenService.ValidateTokenAsync(token, false);
            if (!validationResponse.IsValid)
            {
                return BadRequest(new { error = validationResponse.Message });
            }

            if (!validationResponse.ShareToken.AllowSessionAccess)
            {
                return Forbid();
            }

            var session = await _context.WorkoutSessions
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.WorkoutSets)
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.ExerciseType)
                .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == id);

            if (session == null)
            {
                return NotFound();
            }

            // Check if token is for a specific workout session
            if (validationResponse.ShareToken.WorkoutSessionId.HasValue &&
                validationResponse.ShareToken.WorkoutSessionId.Value != id)
            {
                return Forbid();
            }

            return Ok(session);
        }

        [HttpGet("sessions/{id}/sets")]
        public async Task<IActionResult> GetSets(int id, [FromQuery] string token)
        {
            var validationResponse = await _shareTokenService.ValidateTokenAsync(token, false);
            if (!validationResponse.IsValid)
            {
                return BadRequest(new { error = validationResponse.Message });
            }

            if (!validationResponse.ShareToken.AllowSessionAccess)
            {
                return Forbid();
            }

            var session = await _context.WorkoutSessions
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.WorkoutSets)
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.ExerciseType)
                .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == id);

            if (session == null)
            {
                return NotFound();
            }

            // Check if token is for a specific workout session
            if (validationResponse.ShareToken.WorkoutSessionId.HasValue &&
                validationResponse.ShareToken.WorkoutSessionId.Value != id)
            {
                return Forbid();
            }

            var sets = session.WorkoutExercises
                .SelectMany(we => we.WorkoutSets)
                .OrderBy(ws => ws.SequenceNum)
                .ToList();

            return Ok(sets);
        }
    }
}