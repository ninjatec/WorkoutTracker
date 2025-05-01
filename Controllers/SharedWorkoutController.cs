using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Migration;
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
        private readonly QuickWorkoutService _quickWorkoutService;
        private readonly ISessionWorkoutBridgeService _bridgeService;

        public SharedWorkoutController(
            WorkoutTrackerWebContext context,
            IDistributedCache cache,
            IShareTokenService shareTokenService,
            QuickWorkoutService quickWorkoutService,
            ISessionWorkoutBridgeService bridgeService)
        {
            _context = context;
            _cache = cache;
            _shareTokenService = shareTokenService;
            _quickWorkoutService = quickWorkoutService;
            _bridgeService = bridgeService;
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

            // Step 1: Query WorkoutSessions first (new model)
            var query = _context.WorkoutSessions.Where(ws => ws.UserId == tokenData.UserId);

            // If token is session-specific, only return that session
            if (tokenData.SessionId.HasValue)
            {
                // Get the WorkoutSession linked to the specified SessionId
                query = query.Where(ws => ws.SessionId == tokenData.SessionId.Value);
            }

            // Execute query and convert to Sessions for backward compatibility
            var workoutSessions = await query
                .OrderByDescending(ws => ws.StartDateTime)
                .ToListAsync();
                
            // Use the bridge service to convert WorkoutSessions to Sessions
            var sessions = await _bridgeService.GetSessionsFromWorkoutSessionsAsync(workoutSessions);

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

            // Step 1: Check if we're dealing with a legacy Session ID or a new WorkoutSession ID
            var workoutSession = await _context.WorkoutSessions
                .FirstOrDefaultAsync(ws => ws.SessionId == id && ws.UserId == tokenData.UserId);
                
            // Step 2: If found, convert to Session for compatibility
            if (workoutSession != null)
            {
                var session = await _bridgeService.GetSessionFromWorkoutSessionAsync(workoutSession.WorkoutSessionId);
                return Ok(session);
            }
            
            // Step 3: If not found, use the legacy approach as fallback
            var session2 = await _context.Session
                .FirstOrDefaultAsync(s => s.SessionId == id && s.UserId == tokenData.UserId);

            if (session2 == null)
            {
                return NotFound();
            }

            return Ok(session2);
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

            // Step 1: Check if this is a legacy Session or a new WorkoutSession
            var workoutSession = await _context.WorkoutSessions
                .FirstOrDefaultAsync(ws => ws.SessionId == id && ws.UserId == tokenData.UserId);
                
            if (workoutSession != null)
            {
                // Step 2: If it's a new WorkoutSession, use the bridge service to get Sets
                var session = await _bridgeService.GetSessionFromWorkoutSessionAsync(workoutSession.WorkoutSessionId);
                
                if (session == null)
                {
                    return NotFound();
                }
                
                // If token is session-specific, verify it's for this session
                if (tokenData.SessionId.HasValue && tokenData.SessionId.Value != id)
                {
                    return Forbid();
                }
                
                return Ok(session.Sets);
            }
            
            // Step 3: Fall back to legacy approach if not found
            var legacySession = await _context.Session
                .FirstOrDefaultAsync(s => s.SessionId == id && s.UserId == tokenData.UserId);

            if (legacySession == null)
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

            // Get total sessions count - now using WorkoutSessions
            int totalSessions = await _context.WorkoutSessions
                .Where(ws => ws.UserId == tokenData.UserId)
                .CountAsync();

            // For set and rep counting, we still need to use the bridge service
            // to get complete data during the transition
            
            // Get legacy sets
            var legacySets = await _context.Set
                .Include(s => s.Session)
                .Where(s => s.Session.UserId == tokenData.UserId)
                .ToListAsync();
                
            // Get workout sets from the new model
            var workoutExercises = await _context.WorkoutExercises
                .Include(we => we.WorkoutSets)
                .Include(we => we.WorkoutSession)
                .Where(we => we.WorkoutSession.UserId == tokenData.UserId)
                .ToListAsync();
                
            var workoutSets = workoutExercises.SelectMany(we => we.WorkoutSets).ToList();
            
            // Calculate totals (avoid double counting sets that are in both models)
            int totalSets = legacySets.Count() + workoutSets.Count(ws => !ws.WorkoutExercise.WorkoutSession.SessionId.HasValue);
                
            // Get legacy reps
            var legacySetIds = legacySets.Select(s => s.SetId).ToList();
            var legacyReps = await _context.Rep
                .Where(r => legacySetIds.Contains((int)r.SetsSetId))
                .ToListAsync();

            int totalReps = legacyReps.Count();
            int successReps = legacyReps.Count(r => r.success);
            int failedReps = totalReps - successReps;

            // Get top exercises by usage, combining both models
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

        [HttpGet("sessions/{id}/has-completed-sets")]
        [ShareTokenAuthorize("SessionAccess")]
        public async Task<ActionResult<bool>> HasCompletedSets(int id)
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

            // Use the service to check if the session has completed sets
            var result = await _quickWorkoutService.HasCompletedSetsAsync(id);
            return Ok(result);
        }
    }
}