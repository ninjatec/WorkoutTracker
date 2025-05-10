using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuickWorkoutController : ControllerBase
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly QuickWorkoutService _quickWorkoutService;
        private readonly ILogger<QuickWorkoutController> _logger;

        public QuickWorkoutController(
            WorkoutTrackerWebContext context,
            QuickWorkoutService quickWorkoutService,
            ILogger<QuickWorkoutController> logger)
        {
            _context = context;
            _quickWorkoutService = quickWorkoutService;
            _logger = logger;
        }

        // POST: api/QuickWorkout/FinishSession/5
        [HttpPost("FinishSession/{id}")]
        public async Task<IActionResult> FinishSession(int id)
        {
            try
            {
                var session = await _quickWorkoutService.FinishQuickWorkoutSessionAsync(id);
                return Ok(new { message = "Workout finished successfully", session });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when finishing workout {WorkoutId}", id);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finishing workout {WorkoutId}", id);
                return StatusCode(500, new { error = "An error occurred while finishing the workout" });
            }
        }
    }
}
