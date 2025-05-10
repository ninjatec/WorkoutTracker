using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Controllers
{
    // Request model for reordering sets
    public class ReorderSetsRequest
    {
        public int WorkoutExerciseId { get; set; }
        public List<int> SetIds { get; set; } = new List<int>();
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WorkoutSetsApiController : ControllerBase
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<WorkoutSetsApiController> _logger;

        public WorkoutSetsApiController(
            WorkoutTrackerWebContext context,
            IUserService userService,
            ILogger<WorkoutSetsApiController> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        // GET: api/WorkoutSetsApi/SettypeOptions
        [HttpGet("SettypeOptions")]
        public async Task<IActionResult> GetSettypeOptions()
        {
            var settypes = await _context.Set<Settype>()
                .Select(s => new { id = s.SettypeId, name = s.Name })
                .ToListAsync();

            return Ok(settypes);
        }

        // GET: api/WorkoutSetsApi/5
        [HttpGet("{id}")]
        public async Task<ActionResult<WorkoutSet>> GetWorkoutSet(int id)
        {
            var workoutSet = await _context.WorkoutSets
                .Include(ws => ws.WorkoutExercise)
                    .ThenInclude(we => we.ExerciseType)
                .Include(ws => ws.Settype)
                .FirstOrDefaultAsync(ws => ws.WorkoutSetId == id);

            if (workoutSet == null)
            {
                return NotFound();
            }

            // Check if the user owns this set
            var userId = await _userService.GetCurrentUserIdAsync();
            var sessionUserId = await _context.WorkoutExercises
                .Where(we => we.WorkoutExerciseId == workoutSet.WorkoutExerciseId)
                .Join(_context.WorkoutSessions,
                    we => we.WorkoutSessionId,
                    ws => ws.WorkoutSessionId,
                    (we, ws) => ws.UserId)
                .FirstOrDefaultAsync();

            if (sessionUserId != userId)
            {
                return Forbid();
            }

            return workoutSet;
        }

        // POST: api/WorkoutSetsApi
        [HttpPost]
        public async Task<ActionResult<WorkoutSet>> PostWorkoutSet(WorkoutSet workoutSet)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if the user owns the exercise this set belongs to
            var userId = await _userService.GetCurrentUserIdAsync();
            var sessionUserId = await _context.WorkoutExercises
                .Where(we => we.WorkoutExerciseId == workoutSet.WorkoutExerciseId)
                .Join(_context.WorkoutSessions,
                    we => we.WorkoutSessionId,
                    ws => ws.WorkoutSessionId,
                    (we, ws) => ws.UserId)
                .FirstOrDefaultAsync();

            if (sessionUserId != userId)
            {
                return Forbid();
            }

            // Determine the next sequence number
            int nextSequenceNumber = await _context.WorkoutSets
                .Where(ws => ws.WorkoutExerciseId == workoutSet.WorkoutExerciseId)
                .Select(ws => ws.SequenceNum)
                .DefaultIfEmpty(0)
                .MaxAsync() + 1;

            workoutSet.SequenceNum = nextSequenceNumber;
            workoutSet.SetNumber = nextSequenceNumber;
            workoutSet.Timestamp = DateTime.UtcNow;
            workoutSet.Status = "Active";
            
            // Set default values for empty strings
            workoutSet.Notes = workoutSet.Notes ?? string.Empty;

            _context.WorkoutSets.Add(workoutSet);
            await _context.SaveChangesAsync();

            // Reload the complete workout set with navigation properties
            var createdSet = await _context.WorkoutSets
                .Include(ws => ws.WorkoutExercise)
                    .ThenInclude(we => we.ExerciseType)
                .Include(ws => ws.Settype)
                .FirstOrDefaultAsync(ws => ws.WorkoutSetId == workoutSet.WorkoutSetId);

            return CreatedAtAction(nameof(GetWorkoutSet), new { id = workoutSet.WorkoutSetId }, createdSet);
        }

        // PUT: api/WorkoutSetsApi/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWorkoutSet(int id, WorkoutSet workoutSet)
        {
            if (id != workoutSet.WorkoutSetId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if the user owns this set
            var userId = await _userService.GetCurrentUserIdAsync();
            var sessionUserId = await _context.WorkoutExercises
                .Where(we => we.WorkoutExerciseId == workoutSet.WorkoutExerciseId)
                .Join(_context.WorkoutSessions,
                    we => we.WorkoutSessionId,
                    ws => ws.WorkoutSessionId,
                    (we, ws) => ws.UserId)
                .FirstOrDefaultAsync();

            if (sessionUserId != userId)
            {
                return Forbid();
            }

            var existingSet = await _context.WorkoutSets.FindAsync(id);
            if (existingSet == null)
            {
                return NotFound();
            }

            // Update properties but preserve some values
            existingSet.Weight = workoutSet.Weight;
            existingSet.Reps = workoutSet.Reps;
            existingSet.Notes = workoutSet.Notes ?? string.Empty;
            existingSet.SettypeId = workoutSet.SettypeId;
            
            // Add timestamp for tracking when the set was last updated
            existingSet.Timestamp = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WorkoutSetExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // PUT: api/WorkoutSetsApi/ReorderSets
        [HttpPut("ReorderSets")]
        public async Task<IActionResult> ReorderSets([FromBody] ReorderSetsRequest request)
        {
            if (request == null || request.SetIds == null || !request.SetIds.Any())
            {
                return BadRequest("No sets to reorder");
            }

            // Get current user ID for ownership verification
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized();
            }

            // Verify user owns all sets in the batch
            var setIds = request.SetIds;
            var exerciseId = request.WorkoutExerciseId;

            // First check if the user owns the workout session
            var sessionUserId = await _context.WorkoutExercises
                .Where(we => we.WorkoutExerciseId == exerciseId)
                .Join(_context.WorkoutSessions,
                    we => we.WorkoutSessionId,
                    ws => ws.WorkoutSessionId,
                    (we, ws) => ws.UserId)
                .FirstOrDefaultAsync();

            if (sessionUserId != userId)
            {
                return Forbid();
            }

            // Get all sets for this exercise
            var sets = await _context.WorkoutSets
                .Where(ws => ws.WorkoutExerciseId == exerciseId)
                .ToListAsync();

            // Verify all sets exist and are part of this exercise
            foreach (var setId in setIds)
            {
                if (!sets.Any(s => s.WorkoutSetId == setId))
                {
                    return BadRequest($"Set with id {setId} not found or doesn't belong to this exercise");
                }
            }

            // Update the sequence numbers
            for (int i = 0; i < setIds.Count; i++)
            {
                var set = sets.First(s => s.WorkoutSetId == setIds[i]);
                set.SequenceNum = i + 1;
                set.SetNumber = i + 1;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Sets reordered successfully" });
        }

        // DELETE: api/WorkoutSetsApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkoutSet(int id)
        {
            var workoutSet = await _context.WorkoutSets
                .Include(ws => ws.WorkoutExercise)
                .FirstOrDefaultAsync(ws => ws.WorkoutSetId == id);

            if (workoutSet == null)
            {
                return NotFound();
            }

            // Check if the user owns this set
            var userId = await _userService.GetCurrentUserIdAsync();
            var sessionUserId = await _context.WorkoutExercises
                .Where(we => we.WorkoutExerciseId == workoutSet.WorkoutExerciseId)
                .Join(_context.WorkoutSessions,
                    we => we.WorkoutSessionId,
                    ws => ws.WorkoutSessionId,
                    (we, ws) => ws.UserId)
                .FirstOrDefaultAsync();

            if (sessionUserId != userId)
            {
                return Forbid();
            }

            _context.WorkoutSets.Remove(workoutSet);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool WorkoutSetExists(int id)
        {
            return _context.WorkoutSets.Any(e => e.WorkoutSetId == id);
        }
    }
}
