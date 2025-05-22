using System;
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
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WorkoutExercisesApiController : ControllerBase
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IUserService _userService;
        private readonly ILogger<WorkoutExercisesApiController> _logger;

        public WorkoutExercisesApiController(
            WorkoutTrackerWebContext context,
            IUserService userService,
            ILogger<WorkoutExercisesApiController> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        // DELETE: api/WorkoutExercisesApi/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkoutExercise(int id)
        {
            // Get the workout exercise with validation
            var workoutExercise = await _context.WorkoutExercises
                .Include(we => we.WorkoutSession)
                .FirstOrDefaultAsync(we => we.WorkoutExerciseId == id);

            if (workoutExercise == null)
            {
                return NotFound();
            }

            // Check if the user owns this exercise
            var userId = await _userService.GetCurrentUserIdAsync();
            if (workoutExercise.WorkoutSession.UserId != userId)
            {
                return Forbid();
            }

            try
            {
                // The cascade delete will handle deleting associated workout sets
                _context.WorkoutExercises.Remove(workoutExercise);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting workout exercise {WorkoutExerciseId}", id);
                return StatusCode(500, "An error occurred while deleting the exercise");
            }
        }
    }
}
