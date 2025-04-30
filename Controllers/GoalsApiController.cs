using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;

namespace WorkoutTrackerWeb.Controllers
{
    [Route("api/goals")]
    [ApiController]
    [Authorize]
    public class GoalsApiController : ControllerBase
    {
        private readonly GoalOperationsService _goalOperationsService;
        private readonly GoalQueryService _goalQueryService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<GoalsApiController> _logger;

        public GoalsApiController(
            GoalOperationsService goalOperationsService,
            GoalQueryService goalQueryService,
            UserManager<AppUser> userManager,
            ILogger<GoalsApiController> logger)
        {
            _goalOperationsService = goalOperationsService;
            _goalQueryService = goalQueryService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Gets all goals for the authenticated user
        /// </summary>
        /// <param name="includeCompleted">Whether to include completed goals. Default is true.</param>
        /// <returns>A list of all goals for the user</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<GoalExportDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetGoals([FromQuery] bool includeCompleted = true)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var goals = await _goalOperationsService.ExportGoalsAsync(userId, includeCompleted);
                return Ok(goals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving goals for user");
                return StatusCode(500, "An error occurred while retrieving goals");
            }
        }

        /// <summary>
        /// Gets a specific goal by ID
        /// </summary>
        /// <param name="id">The goal ID</param>
        /// <returns>The goal details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ClientGoal), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetGoal(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var goal = await _goalOperationsService.GetGoalDetailAsync(id, userId);
                if (goal == null)
                {
                    return NotFound();
                }

                return Ok(goal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving goal {GoalId}", id);
                return StatusCode(500, "An error occurred while retrieving the goal");
            }
        }

        /// <summary>
        /// Creates a new goal for the authenticated user
        /// </summary>
        /// <param name="goal">The goal to create</param>
        /// <returns>The created goal</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ClientGoal), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CreateGoal([FromBody] ClientGoal goal)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var createdGoal = await _goalOperationsService.CreateGoalAsync(goal, userId);
                if (createdGoal == null)
                {
                    return BadRequest("Failed to create goal");
                }

                return CreatedAtAction(nameof(GetGoal), new { id = createdGoal.Id }, createdGoal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating goal for user");
                return StatusCode(500, "An error occurred while creating the goal");
            }
        }

        /// <summary>
        /// Updates an existing goal
        /// </summary>
        /// <param name="id">The goal ID</param>
        /// <param name="goal">The updated goal data</param>
        /// <returns>The updated goal</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ClientGoal), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateGoal(int id, [FromBody] ClientGoal goal)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id != goal.Id)
                {
                    return BadRequest("Goal ID mismatch");
                }

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var updatedGoal = await _goalOperationsService.UpdateGoalAsync(id, goal, userId);
                if (updatedGoal == null)
                {
                    return NotFound();
                }

                return Ok(updatedGoal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating goal {GoalId}", id);
                return StatusCode(500, "An error occurred while updating the goal");
            }
        }

        /// <summary>
        /// Updates the progress of a goal
        /// </summary>
        /// <param name="id">The goal ID</param>
        /// <param name="progressUpdate">The progress update data</param>
        /// <returns>The updated goal</returns>
        [HttpPatch("{id}/progress")]
        [ProducesResponseType(typeof(ClientGoal), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateGoalProgress(int id, [FromBody] GoalProgressUpdateDto progressUpdate)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var updatedGoal = await _goalOperationsService.UpdateGoalProgressAsync(
                    id, progressUpdate.NewValue, progressUpdate.Notes, userId);
                    
                if (updatedGoal == null)
                {
                    return NotFound();
                }

                return Ok(updatedGoal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress for goal {GoalId}", id);
                return StatusCode(500, "An error occurred while updating goal progress");
            }
        }

        /// <summary>
        /// Marks a goal as completed
        /// </summary>
        /// <param name="id">The goal ID</param>
        /// <returns>The completed goal</returns>
        [HttpPost("{id}/complete")]
        [ProducesResponseType(typeof(ClientGoal), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CompleteGoal(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var completedGoal = await _goalOperationsService.CompleteGoalAsync(id, userId);
                if (completedGoal == null)
                {
                    return NotFound();
                }

                return Ok(completedGoal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing goal {GoalId}", id);
                return StatusCode(500, "An error occurred while completing the goal");
            }
        }

        /// <summary>
        /// Deletes a goal
        /// </summary>
        /// <param name="id">The goal ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteGoal(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _goalOperationsService.DeleteGoalAsync(id, userId);
                if (!result)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting goal {GoalId}", id);
                return StatusCode(500, "An error occurred while deleting the goal");
            }
        }

        /// <summary>
        /// Exports goals for reporting in various formats
        /// </summary>
        /// <param name="includeCompleted">Whether to include completed goals</param>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        /// <returns>Formatted goal data for reporting</returns>
        [HttpGet("export")]
        [ProducesResponseType(typeof(IEnumerable<GoalExportDto>), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> ExportGoals(
            [FromQuery] bool includeCompleted = true,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var goals = await _goalOperationsService.ExportGoalsAsync(
                    userId, includeCompleted, startDate, endDate);
                    
                return Ok(goals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting goals");
                return StatusCode(500, "An error occurred while exporting goals");
            }
        }
    }

    /// <summary>
    /// DTO for goal progress updates via API
    /// </summary>
    public class GoalProgressUpdateDto
    {
        /// <summary>
        /// The new progress value
        /// </summary>
        public decimal NewValue { get; set; }
        
        /// <summary>
        /// Optional notes about the progress update
        /// </summary>
        public string Notes { get; set; }
    }
}