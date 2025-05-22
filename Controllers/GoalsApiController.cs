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
using WorkoutTrackerWeb.Attributes;

namespace WorkoutTrackerWeb.Controllers
{
    [Route("api/goals")]
    [ApiController]
    [Authorize]
    public class GoalsApiController : ApiBaseController
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
        [ETag(cacheDurationSeconds: 300)] // 5 minutes cache duration
        public async Task<IActionResult> GetGoals([FromQuery] bool includeCompleted = true)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return UnauthorizedResponse<IEnumerable<GoalExportDto>>();
                }

                var goals = await _goalOperationsService.ExportGoalsAsync(userId, includeCompleted);
                
                var metadata = new Dictionary<string, object>();
                int totalCount = goals != null ? goals.Count() : 0;
                metadata.Add("totalCount", totalCount);
                metadata.Add("includeCompleted", includeCompleted);
                metadata.Add("timestamp", DateTime.UtcNow);
                
                return SuccessResponse(goals, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving goals for user");
                return ErrorResponse<IEnumerable<GoalExportDto>>("An error occurred while retrieving goals");
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
        [ETag(cacheDurationSeconds: 300)] // 5 minutes cache duration
        public async Task<IActionResult> GetGoal(int id)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return UnauthorizedResponse<ClientGoal>();
                }

                var goal = await _goalOperationsService.GetGoalDetailAsync(id, userId);
                if (goal == null)
                {
                    return NotFoundResponse<ClientGoal>($"Goal with ID {id} was not found");
                }

                var metadata = new Dictionary<string, object>
                {
                    { "goalId", id },
                    { "requestTime", DateTime.UtcNow }
                };
                
                return SuccessResponse(goal, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving goal {GoalId}", id);
                return ErrorResponse<ClientGoal>($"An error occurred while retrieving goal {id}");
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
                    return BadRequestResponse<ClientGoal>("Invalid goal data");
                }

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return UnauthorizedResponse<ClientGoal>();
                }

                var createdGoal = await _goalOperationsService.CreateGoalAsync(goal, userId);
                if (createdGoal == null)
                {
                    return BadRequestResponse<ClientGoal>("Failed to create goal");
                }

                var metadata = new Dictionary<string, object>
                {
                    { "goalId", createdGoal.Id },
                    { "createdAt", DateTime.UtcNow }
                };
                
                // Using custom response to set 201 status code
                Response.StatusCode = 201;
                return SuccessResponse(createdGoal, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating goal for user");
                return ErrorResponse<ClientGoal>("An error occurred while creating the goal");
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
                    return BadRequestResponse<ClientGoal>("Invalid goal data");
                }

                if (id != goal.Id)
                {
                    return BadRequestResponse<ClientGoal>("Goal ID mismatch");
                }

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return UnauthorizedResponse<ClientGoal>();
                }

                var updatedGoal = await _goalOperationsService.UpdateGoalAsync(id, goal, userId);
                if (updatedGoal == null)
                {
                    return NotFoundResponse<ClientGoal>($"Goal with ID {id} was not found");
                }

                var metadata = new Dictionary<string, object>
                {
                    { "goalId", id },
                    { "updatedAt", DateTime.UtcNow }
                };
                
                return SuccessResponse(updatedGoal, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating goal {GoalId}", id);
                return ErrorResponse<ClientGoal>($"An error occurred while updating goal {id}");
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
                    return BadRequestResponse<ClientGoal>("Invalid progress update data");
                }

                var userId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(userId))
                {
                    return UnauthorizedResponse<ClientGoal>();
                }

                var updatedGoal = await _goalOperationsService.UpdateGoalProgressAsync(
                    id, progressUpdate.NewValue, progressUpdate.Notes, userId);
                    
                if (updatedGoal == null)
                {
                    return NotFoundResponse<ClientGoal>($"Goal with ID {id} was not found");
                }

                var metadata = new Dictionary<string, object>
                {
                    { "goalId", id },
                    { "previousProgress", progressUpdate.NewValue },
                    { "updatedAt", DateTime.UtcNow }
                };
                
                return SuccessResponse(updatedGoal, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress for goal {GoalId}", id);
                return ErrorResponse<ClientGoal>($"An error occurred while updating progress for goal {id}");
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
                    return UnauthorizedResponse<ClientGoal>();
                }

                var completedGoal = await _goalOperationsService.CompleteGoalAsync(id, userId);
                if (completedGoal == null)
                {
                    return NotFoundResponse<ClientGoal>($"Goal with ID {id} was not found");
                }

                var metadata = new Dictionary<string, object>
                {
                    { "goalId", id },
                    { "completedAt", DateTime.UtcNow }
                };
                
                return SuccessResponse(completedGoal, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing goal {GoalId}", id);
                return ErrorResponse<ClientGoal>($"An error occurred while completing goal {id}");
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
                    return UnauthorizedResponse<object>();
                }

                var result = await _goalOperationsService.DeleteGoalAsync(id, userId);
                if (!result)
                {
                    return NotFoundResponse<object>($"Goal with ID {id} was not found");
                }

                return NoContentResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting goal {GoalId}", id);
                return ErrorResponse<object>($"An error occurred while deleting goal {id}");
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
        [ETag(cacheDurationSeconds: 600)] // 10 minutes cache duration for exports
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
                    return UnauthorizedResponse<IEnumerable<GoalExportDto>>();
                }

                var goals = await _goalOperationsService.ExportGoalsAsync(
                    userId, includeCompleted, startDate, endDate);
                
                var metadata = new Dictionary<string, object>();
                int totalCount = goals != null ? goals.Count() : 0;
                metadata.Add("totalCount", totalCount);
                metadata.Add("includeCompleted", includeCompleted);
                metadata.Add("startDate", startDate);
                metadata.Add("endDate", endDate);
                metadata.Add("exportTimestamp", DateTime.UtcNow);
                    
                return SuccessResponse(goals, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting goals");
                return ErrorResponse<IEnumerable<GoalExportDto>>("An error occurred while exporting goals");
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