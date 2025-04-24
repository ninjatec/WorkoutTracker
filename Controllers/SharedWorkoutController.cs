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
using System.Text.Json;
using System;
using WorkoutTrackerWeb.Dtos;

namespace WorkoutTrackerWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SharedWorkoutController : ControllerBase
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IDistributedCache _cache;
        private readonly IShareTokenService _shareTokenService;

        public SharedWorkoutController(
            WorkoutTrackerWebContext context,
            IDistributedCache cache,
            IShareTokenService shareTokenService)
        {
            _context = context;
            _cache = cache;
            _shareTokenService = shareTokenService;
        }

        [HttpGet("sessions")]
        [ShareTokenAuthorize("SessionAccess")]
        public async Task<ActionResult<IEnumerable<Session>>> GetSessions()
        {
            // Get the validated token
            var tokenData = HttpContext.Items["ShareTokenData"] as ShareToken;
            if (tokenData == null)
            {
                return Unauthorized();
            }

            // Create query to get sessions for this user
            var query = _context.Session.Where(s => s.UserId == tokenData.UserId);

            // If token is session-specific, only return that session
            if (tokenData.SessionId.HasValue)
            {
                query = query.Where(s => s.SessionId == tokenData.SessionId.Value);
            }

            // Execute query and return results
            var sessions = await query
                .OrderByDescending(s => s.datetime)
                .ToListAsync();

            return Ok(sessions);
        }

        [HttpGet("sessions/{id}")]
        [ShareTokenAuthorize("SessionAccess")]
        public async Task<ActionResult<Session>> GetSession(int id)
        {
            // Get the validated token
            var tokenData = HttpContext.Items["ShareTokenData"] as ShareToken;
            if (tokenData == null)
            {
                return Unauthorized();
            }

            // If token is session-specific, verify it's for this session
            if (tokenData.SessionId.HasValue && tokenData.SessionId.Value != id)
            {
                return Forbid();
            }

            // Get the session
            var session = await _context.Session
                .FirstOrDefaultAsync(s => s.SessionId == id && s.UserId == tokenData.UserId);

            if (session == null)
            {
                return NotFound();
            }

            return Ok(session);
        }

        [HttpGet("sessions/{id}/sets")]
        [ShareTokenAuthorize("SessionAccess")]
        public async Task<ActionResult<IEnumerable<Set>>> GetSets(int id)
        {
            // Get the validated token
            var tokenData = HttpContext.Items["ShareTokenData"] as ShareToken;
            if (tokenData == null)
            {
                return Unauthorized();
            }

            // Verify the session belongs to the user
            var session = await _context.Session
                .FirstOrDefaultAsync(s => s.SessionId == id && s.UserId == tokenData.UserId);

            if (session == null)
            {
                return NotFound();
            }

            // If token is session-specific, verify it's for this session
            if (tokenData.SessionId.HasValue && tokenData.SessionId.Value != id)
            {
                return Forbid();
            }

            // Get sets for this session
            var sets = await _context.Set
                .Include(s => s.ExerciseType)
                .Include(s => s.Settype)
                .Where(s => s.SessionId == id)
                .OrderBy(s => s.SetId)
                .ToListAsync();

            return Ok(sets);
        }

        [HttpGet("sets/{id}/reps")]
        [ShareTokenAuthorize("SessionAccess")]
        public async Task<ActionResult<IEnumerable<Rep>>> GetReps(int id)
        {
            // Get the validated token
            var tokenData = HttpContext.Items["ShareTokenData"] as ShareToken;
            if (tokenData == null)
            {
                return Unauthorized();
            }

            // Verify the set belongs to a session owned by the user
            var set = await _context.Set
                .Include(s => s.Session)
                .FirstOrDefaultAsync(s => s.SetId == id);

            if (set == null)
            {
                return NotFound();
            }

            if (set.Session.UserId != tokenData.UserId)
            {
                return Forbid();
            }

            // If token is session-specific, verify it's for this session
            if (tokenData.SessionId.HasValue && tokenData.SessionId.Value != set.SessionId)
            {
                return Forbid();
            }

            // Get reps for this set
            var reps = await _context.Rep
                .Where(r => r.SetsSetId == id)
                .OrderBy(r => r.repnumber)
                .ToListAsync();

            return Ok(reps);
        }

        [HttpGet("exercise-types")]
        [ShareTokenAuthorize("SessionAccess")]
        public async Task<ActionResult<IEnumerable<ExerciseType>>> GetExerciseTypes()
        {
            var exerciseTypes = await _context.ExerciseType
                .OrderBy(e => e.Name)
                .ToListAsync();

            return Ok(exerciseTypes);
        }

        [HttpGet("set-types")]
        [ShareTokenAuthorize("SessionAccess")]
        public async Task<ActionResult<IEnumerable<Settype>>> GetSetTypes()
        {
            var setTypes = await _context.Settype
                .OrderBy(s => s.Name)
                .ToListAsync();

            return Ok(setTypes);
        }

        [HttpGet("reports/stats")]
        [ShareTokenAuthorize("ReportAccess")]
        public async Task<ActionResult<object>> GetStats()
        {
            // Get the validated token
            var tokenData = HttpContext.Items["ShareTokenData"] as ShareToken;
            if (tokenData == null)
            {
                return Unauthorized();
            }

            // Get total sessions count
            int totalSessions = await _context.Session
                .Where(s => s.UserId == tokenData.UserId)
                .CountAsync();

            // Get total sets and reps
            var sets = await _context.Set
                .Include(s => s.Session)
                .Where(s => s.Session.UserId == tokenData.UserId)
                .ToListAsync();

            int totalSets = sets.Count();

            var setIds = sets.Select(s => s.SetId).ToList();
            var reps = await _context.Rep
                .Where(r => setIds.Contains((int)r.SetsSetId))
                .ToListAsync();

            int totalReps = reps.Count();
            int successReps = reps.Count(r => r.success);
            int failedReps = totalReps - successReps;

            // Get top exercises by usage
            var exerciseUsage = sets
                .GroupBy(s => s.ExerciseType?.Name ?? "Unknown")
                .Select(g => new { ExerciseName = g.Key, Count = g.Count() })
                .OrderByDescending(e => e.Count)
                .Take(10)
                .ToList();

            return Ok(new
            {
                TotalSessions = totalSessions,
                TotalSets = totalSets,
                TotalReps = totalReps,
                SuccessReps = successReps,
                FailedReps = failedReps,
                ExerciseUsage = exerciseUsage
            });
        }
    }
}