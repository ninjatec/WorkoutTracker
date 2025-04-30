using System;
using System.Collections.Generic;
using System.Linq;
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
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GoalsController : ControllerBase
    {
        private readonly GoalOperationsService _goalOperationsService;
        private readonly GoalQueryService _goalQueryService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<GoalsController> _logger;

        public GoalsController(
            GoalOperationsService goalOperationsService,
            GoalQueryService goalQueryService,
            UserManager<AppUser> userManager,
            ILogger<GoalsController> logger)
        {
            _goalOperationsService = goalOperationsService;
            _goalQueryService = goalQueryService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Gets all goals for the current user
        /// </summary>
        /// <param name="includeCompleted">Whether to include completed goals (default: true)</param>
        /// <param name="category">Optional category filter</param>
        /// <returns>Collection of goals</returns>
        [HttpGet]
        public async Task<IActionResult> GetGoals(
            [FromQuery] bool includeCompleted = true,
            [FromQuery] string category = null)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                // Get goals using the query service
                var goals = await _goalQueryService.GetGoalsForUserAsync(userId);

                // Apply filters
                if (!includeCompleted)
                {
                    goals = goals.Where(g => !g.IsCompleted && !g.CompletedDate.HasValue);
                }

                if (!string.IsNullOrEmpty(category) && Enum.TryParse<GoalCategory>(category, true, out var categoryEnum))
                {
                    goals = goals.Where(g => g.Category == categoryEnum);
                }

                // Map to a simplified view model
                var result = goals.Select(g => new
                {
                    Id = g.Id,
                    Description = g.Description,
                    Category = g.Category.ToString(),
                    CustomCategory = g.CustomCategory,
                    TargetDate = g.TargetDate,
                    IsCompleted = g.IsCompleted || g.CompletedDate.HasValue,
                    Progress = g.ProgressPercentage,
                    CreatedDate = g.CreatedDate,
                    MeasurementType = g.MeasurementType,
                    MeasurementUnit = g.MeasurementUnit,
                    StartValue = g.StartValue,
                    CurrentValue = g.CurrentValue,
                    TargetValue = g.TargetValue,
                    IsCoachCreated = g.IsCoachCreated
                });

                _logger.LogInformation("Retrieved {Count} goals for user {UserId}", result.Count(), userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving goals for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving goals");
            }
        }

        /// <summary>
        /// Gets a specific goal by ID
        /// </summary>
        /// <param name="id">Goal ID</param>
        /// <returns>Goal details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGoal(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var goal = await _goalOperationsService.GetGoalDetailAsync(id, userId);
                if (goal == null)
                    return NotFound();

                // Map to detailed view model including milestones
                var milestones = await _goalOperationsService.ExportGoalsAsync(userId, true);
                var goalDetail = milestones.FirstOrDefault(g => g.Id == id);

                if (goalDetail == null)
                {
                    // Fallback if not found in export
                    return Ok(new
                    {
                        Id = goal.Id,
                        Description = goal.Description,
                        Category = goal.Category.ToString(),
                        CustomCategory = goal.CustomCategory,
                        TargetDate = goal.TargetDate,
                        IsCompleted = goal.IsCompleted || goal.CompletedDate.HasValue,
                        Progress = goal.ProgressPercentage,
                        CreatedDate = goal.CreatedDate,
                        MeasurementType = goal.MeasurementType,
                        MeasurementUnit = goal.MeasurementUnit,
                        StartValue = goal.StartValue,
                        CurrentValue = goal.CurrentValue,
                        TargetValue = goal.TargetValue,
                        IsCoachCreated = goal.IsCoachCreated,
                        Milestones = new List<object>()
                    });
                }

                return Ok(goalDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving goal {GoalId} for user {UserId}", id, userId);
                return StatusCode(500, "An error occurred while retrieving goal details");
            }
        }

        /// <summary>
        /// Creates a new goal
        /// </summary>
        /// <param name="model">Goal creation data</param>
        /// <returns>Created goal</returns>
        [HttpPost]
        public async Task<IActionResult> CreateGoal([FromBody] GoalCreateModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Create goal entity from model
                var goal = new ClientGoal
                {
                    Description = model.Description,
                    Category = model.Category,
                    CustomCategory = model.CustomCategory,
                    TargetDate = model.TargetDate,
                    MeasurementType = model.MeasurementType,
                    MeasurementUnit = model.MeasurementUnit,
                    StartValue = model.StartValue,
                    CurrentValue = model.CurrentValue ?? model.StartValue,
                    TargetValue = model.TargetValue,
                    Notes = model.Notes,
                    IsVisibleToCoach = model.IsVisibleToCoach
                };

                // Use operations service to create goal with permission checking
                var createdGoal = await _goalOperationsService.CreateGoalAsync(goal, userId);
                if (createdGoal == null)
                    return StatusCode(500, "Failed to create goal");

                _logger.LogInformation("Created goal {GoalId} for user {UserId}", createdGoal.Id, userId);
                return CreatedAtAction(nameof(GetGoal), new { id = createdGoal.Id }, new
                {
                    Id = createdGoal.Id,
                    Description = createdGoal.Description,
                    Category = createdGoal.Category.ToString(),
                    TargetDate = createdGoal.TargetDate,
                    Progress = createdGoal.ProgressPercentage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating goal for user {UserId}", userId);
                return StatusCode(500, "An error occurred while creating the goal");
            }
        }

        /// <summary>
        /// Updates an existing goal
        /// </summary>
        /// <param name="id">Goal ID</param>
        /// <param name="model">Updated goal data</param>
        /// <returns>No content</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGoal(int id, [FromBody] GoalUpdateModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Create goal entity from model
                var goal = new ClientGoal
                {
                    Id = id,
                    Description = model.Description,
                    Category = model.Category,
                    CustomCategory = model.CustomCategory,
                    TargetDate = model.TargetDate,
                    MeasurementType = model.MeasurementType,
                    MeasurementUnit = model.MeasurementUnit,
                    StartValue = model.StartValue,
                    TargetValue = model.TargetValue,
                    Notes = model.Notes,
                    IsVisibleToCoach = model.IsVisibleToCoach
                };

                // Use operations service to update goal with permission checking
                var updatedGoal = await _goalOperationsService.UpdateGoalAsync(id, goal, userId);
                if (updatedGoal == null)
                    return NotFound();

                _logger.LogInformation("Updated goal {GoalId} for user {UserId}", id, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating goal {GoalId} for user {UserId}", id, userId);
                return StatusCode(500, "An error occurred while updating the goal");
            }
        }

        /// <summary>
        /// Updates progress on a goal
        /// </summary>
        /// <param name="id">Goal ID</param>
        /// <param name="model">Progress update data</param>
        /// <returns>No content</returns>
        [HttpPut("{id}/progress")]
        public async Task<IActionResult> UpdateProgress(int id, [FromBody] GoalProgressUpdateModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updatedGoal = await _goalOperationsService.UpdateGoalProgressAsync(
                    id, model.NewValue, model.ProgressNotes, userId);

                if (updatedGoal == null)
                    return NotFound();

                _logger.LogInformation("Updated progress for goal {GoalId} by user {UserId}, new value: {NewValue}", 
                    id, userId, model.NewValue);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress for goal {GoalId} for user {UserId}", id, userId);
                return StatusCode(500, "An error occurred while updating goal progress");
            }
        }

        /// <summary>
        /// Marks a goal as completed
        /// </summary>
        /// <param name="id">Goal ID</param>
        /// <returns>No content</returns>
        [HttpPut("{id}/complete")]
        public async Task<IActionResult> CompleteGoal(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var completedGoal = await _goalOperationsService.CompleteGoalAsync(id, userId);
                if (completedGoal == null)
                    return NotFound();

                _logger.LogInformation("Completed goal {GoalId} by user {UserId}", id, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing goal {GoalId} for user {UserId}", id, userId);
                return StatusCode(500, "An error occurred while completing the goal");
            }
        }

        /// <summary>
        /// Deletes a goal
        /// </summary>
        /// <param name="id">Goal ID</param>
        /// <returns>No content</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGoal(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var result = await _goalOperationsService.DeleteGoalAsync(id, userId);
                if (!result)
                    return NotFound();

                _logger.LogInformation("Deleted goal {GoalId} by user {UserId}", id, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting goal {GoalId} for user {UserId}", id, userId);
                return StatusCode(500, "An error occurred while deleting the goal");
            }
        }
        
        /// <summary>
        /// Exports all goals for the current user in a structured format
        /// </summary>
        /// <param name="includeCompleted">Whether to include completed goals</param>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        /// <returns>Exported goals data</returns>
        [HttpGet("export")]
        public async Task<IActionResult> ExportGoals(
            [FromQuery] bool includeCompleted = true,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var exportedGoals = await _goalOperationsService.ExportGoalsAsync(
                    userId, includeCompleted, startDate, endDate);

                _logger.LogInformation("Exported {Count} goals for user {UserId}", exportedGoals.Count(), userId);
                return Ok(exportedGoals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting goals for user {UserId}", userId);
                return StatusCode(500, "An error occurred while exporting goals");
            }
        }
    }

    public class GoalCreateModel
    {
        public string Description { get; set; }
        public GoalCategory Category { get; set; }
        public string CustomCategory { get; set; }
        public DateTime TargetDate { get; set; }
        public string MeasurementType { get; set; }
        public string MeasurementUnit { get; set; }
        public decimal? StartValue { get; set; }
        public decimal? CurrentValue { get; set; }
        public decimal? TargetValue { get; set; }
        public string Notes { get; set; }
        public bool IsVisibleToCoach { get; set; } = true;
    }

    public class GoalUpdateModel
    {
        public string Description { get; set; }
        public GoalCategory Category { get; set; }
        public string CustomCategory { get; set; }
        public DateTime TargetDate { get; set; }
        public string MeasurementType { get; set; }
        public string MeasurementUnit { get; set; }
        public decimal? StartValue { get; set; }
        public decimal? TargetValue { get; set; }
        public string Notes { get; set; }
        public bool IsVisibleToCoach { get; set; }
    }

    public class GoalProgressUpdateModel
    {
        public decimal NewValue { get; set; }
        public string ProgressNotes { get; set; }
    }
}