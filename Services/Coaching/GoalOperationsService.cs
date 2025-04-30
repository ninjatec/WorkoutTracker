using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Services.Coaching
{
    /// <summary>
    /// Service for managing goal operations with proper permission checking and optimized queries
    /// </summary>
    public class GoalOperationsService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly GoalQueryService _goalQueryService;
        private readonly GoalProgressService _goalProgressService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<GoalOperationsService> _logger;

        public GoalOperationsService(
            WorkoutTrackerWebContext context,
            GoalQueryService goalQueryService,
            GoalProgressService goalProgressService,
            UserManager<AppUser> userManager,
            ILogger<GoalOperationsService> logger)
        {
            _context = context;
            _goalQueryService = goalQueryService;
            _goalProgressService = goalProgressService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Checks if a user has permission to view a specific goal
        /// </summary>
        /// <param name="goalId">The ID of the goal</param>
        /// <param name="userId">The ID of the user trying to access the goal</param>
        /// <returns>True if the user has permission to view the goal, false otherwise</returns>
        public async Task<bool> CanViewGoalAsync(int goalId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            var goal = await _context.ClientGoals
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == goalId);

            if (goal == null)
                return false;

            // Users can always view their own goals
            if (goal.UserId == userId)
                return true;

            // Check if the user is a coach with access to this goal
            if (goal.CoachClientRelationshipId.HasValue)
            {
                var relationship = await _context.CoachClientRelationships
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == goal.CoachClientRelationshipId.Value && 
                                            r.CoachId == userId && 
                                            r.Status == RelationshipStatus.Active);
                if (relationship != null)
                    return true;
            }

            // Check if the user is a coach of the client who created the goal
            if (!string.IsNullOrEmpty(goal.UserId) && goal.IsVisibleToCoach)
            {
                var relationship = await _context.CoachClientRelationships
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.ClientId == goal.UserId && 
                                            r.CoachId == userId && 
                                            r.Status == RelationshipStatus.Active);
                if (relationship != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a user has permission to edit a specific goal
        /// </summary>
        /// <param name="goalId">The ID of the goal</param>
        /// <param name="userId">The ID of the user trying to edit the goal</param>
        /// <returns>True if the user has permission to edit the goal, false otherwise</returns>
        public async Task<bool> CanEditGoalAsync(int goalId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            var goal = await _context.ClientGoals
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == goalId);

            if (goal == null)
                return false;

            // Users can edit their own goals if they created them
            if (goal.UserId == userId && !goal.IsCoachCreated)
                return true;

            // Coaches can edit goals they created
            if (goal.CoachClientRelationshipId.HasValue && goal.IsCoachCreated)
            {
                var relationship = await _context.CoachClientRelationships
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == goal.CoachClientRelationshipId.Value && 
                                            r.CoachId == userId && 
                                            r.Status == RelationshipStatus.Active);
                if (relationship != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a user has permission to update progress on a specific goal
        /// </summary>
        /// <param name="goalId">The ID of the goal</param>
        /// <param name="userId">The ID of the user trying to update progress</param>
        /// <returns>True if the user has permission to update progress, false otherwise</returns>
        public async Task<bool> CanUpdateProgressAsync(int goalId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            var goal = await _context.ClientGoals
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == goalId);

            if (goal == null)
                return false;

            // Users can update progress on their own goals
            if (goal.UserId == userId)
                return true;

            // Coaches can update progress on goals they created or for their clients
            if (goal.CoachClientRelationshipId.HasValue)
            {
                var relationship = await _context.CoachClientRelationships
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == goal.CoachClientRelationshipId.Value && 
                                            r.CoachId == userId && 
                                            r.Status == RelationshipStatus.Active);
                if (relationship != null)
                    return true;
            }

            // Coach can update client goals that are visible to coaches
            if (!string.IsNullOrEmpty(goal.UserId) && goal.IsVisibleToCoach)
            {
                var relationship = await _context.CoachClientRelationships
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.ClientId == goal.UserId && 
                                            r.CoachId == userId && 
                                            r.Status == RelationshipStatus.Active);
                if (relationship != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a new goal with proper permission checking
        /// </summary>
        /// <param name="goal">The goal to create</param>
        /// <param name="creatorId">The ID of the user creating the goal</param>
        /// <param name="targetClientId">The ID of the client if created by a coach, null otherwise</param>
        /// <returns>The created goal if successful, null otherwise</returns>
        public async Task<ClientGoal> CreateGoalAsync(ClientGoal goal, string creatorId, string targetClientId = null)
        {
            if (string.IsNullOrEmpty(creatorId))
                throw new ArgumentNullException(nameof(creatorId));

            // Check if a coach-client relationship exists when creating a goal for a client
            if (!string.IsNullOrEmpty(targetClientId))
            {
                var relationship = await _context.CoachClientRelationships
                    .FirstOrDefaultAsync(r => r.CoachId == creatorId && 
                                           r.ClientId == targetClientId && 
                                           r.Status == RelationshipStatus.Active);
                
                if (relationship == null)
                {
                    _logger.LogWarning("Coach {CoachId} attempted to create a goal for client {ClientId} without an active relationship", 
                        creatorId, targetClientId);
                    return null;
                }

                // Set up the goal as coach-created
                goal.CoachClientRelationshipId = relationship.Id;
                goal.UserId = targetClientId;
                goal.IsCoachCreated = true;
            }
            else
            {
                // Set up the goal as user-created
                goal.UserId = creatorId;
                goal.IsCoachCreated = false;
            }

            // Ensure creation date is set
            goal.CreatedDate = DateTime.UtcNow;

            // Save to database
            _context.ClientGoals.Add(goal);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Goal created: {GoalId}, Creator: {CreatorId}, Client: {ClientId}, IsCoachCreated: {IsCoachCreated}", 
                goal.Id, creatorId, targetClientId ?? creatorId, goal.IsCoachCreated);

            return goal;
        }

        /// <summary>
        /// Updates an existing goal with proper permission checking
        /// </summary>
        /// <param name="goalId">The ID of the goal to update</param>
        /// <param name="updatedGoal">The updated goal data</param>
        /// <param name="userId">The ID of the user making the update</param>
        /// <returns>The updated goal if successful, null otherwise</returns>
        public async Task<ClientGoal> UpdateGoalAsync(int goalId, ClientGoal updatedGoal, string userId)
        {
            if (!await CanEditGoalAsync(goalId, userId))
            {
                _logger.LogWarning("User {UserId} attempted to update goal {GoalId} without permission", userId, goalId);
                return null;
            }

            var existingGoal = await _context.ClientGoals.FindAsync(goalId);
            if (existingGoal == null)
            {
                _logger.LogWarning("Goal {GoalId} not found during update attempt", goalId);
                return null;
            }

            // Update goal properties (preserving values that shouldn't change)
            existingGoal.Description = updatedGoal.Description;
            existingGoal.Category = updatedGoal.Category;
            existingGoal.CustomCategory = updatedGoal.CustomCategory;
            existingGoal.TargetDate = updatedGoal.TargetDate;
            existingGoal.MeasurementType = updatedGoal.MeasurementType;
            existingGoal.MeasurementUnit = updatedGoal.MeasurementUnit;
            existingGoal.StartValue = updatedGoal.StartValue;
            existingGoal.TargetValue = updatedGoal.TargetValue;
            existingGoal.Notes = updatedGoal.Notes;
            existingGoal.IsVisibleToCoach = updatedGoal.IsVisibleToCoach;
            existingGoal.TrackingFrequency = updatedGoal.TrackingFrequency;

            // If measurement type has changed, reset current value
            if (existingGoal.MeasurementType != updatedGoal.MeasurementType)
            {
                existingGoal.CurrentValue = updatedGoal.StartValue;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Goal {GoalId} updated by user {UserId}", goalId, userId);
            return existingGoal;
        }

        /// <summary>
        /// Updates progress on a goal with proper permission checking
        /// </summary>
        /// <param name="goalId">The ID of the goal</param>
        /// <param name="newValue">The new progress value</param>
        /// <param name="progressNotes">Optional notes about the progress update</param>
        /// <param name="userId">The ID of the user updating progress</param>
        /// <returns>The updated goal if successful, null otherwise</returns>
        public async Task<ClientGoal> UpdateGoalProgressAsync(int goalId, decimal newValue, string progressNotes, string userId)
        {
            if (!await CanUpdateProgressAsync(goalId, userId))
            {
                _logger.LogWarning("User {UserId} attempted to update progress on goal {GoalId} without permission", userId, goalId);
                return null;
            }

            try
            {
                // Delegate to the progress service for the actual update
                var goal = await _goalProgressService.UpdateGoalProgressManuallyAsync(goalId, newValue, progressNotes);
                
                _logger.LogInformation("Goal {GoalId} progress updated by user {UserId}, new value: {NewValue}", 
                    goalId, userId, newValue);
                
                return goal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress for goal {GoalId} by user {UserId}", goalId, userId);
                return null;
            }
        }

        /// <summary>
        /// Marks a goal as completed with proper permission checking
        /// </summary>
        /// <param name="goalId">The ID of the goal</param>
        /// <param name="userId">The ID of the user marking as completed</param>
        /// <returns>The completed goal if successful, null otherwise</returns>
        public async Task<ClientGoal> CompleteGoalAsync(int goalId, string userId)
        {
            if (!await CanUpdateProgressAsync(goalId, userId))
            {
                _logger.LogWarning("User {UserId} attempted to complete goal {GoalId} without permission", userId, goalId);
                return null;
            }

            var goal = await _context.ClientGoals.FindAsync(goalId);
            if (goal == null)
            {
                _logger.LogWarning("Goal {GoalId} not found during completion attempt", goalId);
                return null;
            }

            // Mark as completed
            goal.IsCompleted = true;
            goal.CompletedDate = DateTime.UtcNow;
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Goal {GoalId} marked as completed by user {UserId}", goalId, userId);
            return goal;
        }

        /// <summary>
        /// Deletes a goal with proper permission checking
        /// </summary>
        /// <param name="goalId">The ID of the goal</param>
        /// <param name="userId">The ID of the user deleting the goal</param>
        /// <returns>True if deletion was successful, false otherwise</returns>
        public async Task<bool> DeleteGoalAsync(int goalId, string userId)
        {
            if (!await CanEditGoalAsync(goalId, userId))
            {
                _logger.LogWarning("User {UserId} attempted to delete goal {GoalId} without permission", userId, goalId);
                return false;
            }

            var goal = await _context.ClientGoals.FindAsync(goalId);
            if (goal == null)
            {
                _logger.LogWarning("Goal {GoalId} not found during deletion attempt", goalId);
                return false;
            }

            // Check related entities that need to be deleted
            var milestones = await _context.GoalMilestones
                .Where(m => m.GoalId == goalId)
                .ToListAsync();

            var feedback = await _context.GoalFeedback
                .Where(f => f.GoalId == goalId)
                .ToListAsync();

            // Use a transaction to ensure all related data is deleted consistently
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Delete related records first
                    if (milestones.Any())
                    {
                        _context.GoalMilestones.RemoveRange(milestones);
                    }

                    if (feedback.Any())
                    {
                        _context.GoalFeedback.RemoveRange(feedback);
                    }

                    // Delete the goal itself
                    _context.ClientGoals.Remove(goal);
                    
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Goal {GoalId} deleted by user {UserId}", goalId, userId);
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error deleting goal {GoalId} by user {UserId}", goalId, userId);
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets detailed goal information including milestones and feedback
        /// </summary>
        /// <param name="goalId">The ID of the goal</param>
        /// <param name="userId">The ID of the user requesting the data</param>
        /// <returns>The goal with detailed information if accessible, null otherwise</returns>
        public async Task<ClientGoal> GetGoalDetailAsync(int goalId, string userId)
        {
            if (!await CanViewGoalAsync(goalId, userId))
            {
                _logger.LogWarning("User {UserId} attempted to view goal {GoalId} without permission", userId, goalId);
                return null;
            }

            // Use optimized query with eager loading of related entities
            var goal = await _context.ClientGoals
                .Include(g => g.Relationship)
                .ThenInclude(r => r.Coach)
                .Include(g => g.Relationship)
                .ThenInclude(r => r.Client)
                .FirstOrDefaultAsync(g => g.Id == goalId);

            if (goal == null)
                return null;

            // Include milestones
            var milestones = await _context.GoalMilestones
                .Where(m => m.GoalId == goalId)
                .OrderByDescending(m => m.Date)
                .ToListAsync();

            // Include feedback
            var feedback = await _context.GoalFeedback
                .Where(f => f.GoalId == goalId)
                .Include(f => f.Coach)
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();

            // Manually attach the related entities to avoid tracking issues
            foreach (var milestone in milestones)
            {
                milestone.Goal = goal;
            }

            foreach (var feedbackItem in feedback)
            {
                feedbackItem.Goal = goal;
            }

            return goal;
        }

        /// <summary>
        /// Exports a user's goals in a structured format for API integration
        /// </summary>
        /// <param name="userId">The ID of the user whose goals to export</param>
        /// <param name="includeCompleted">Whether to include completed goals</param>
        /// <param name="startDate">Optional start date filter</param>
        /// <param name="endDate">Optional end date filter</param>
        /// <returns>A collection of goals in a structured format for API consumption</returns>
        public async Task<IEnumerable<GoalExportDto>> ExportGoalsAsync(
            string userId,
            bool includeCompleted = true,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            // Get goals using the query service
            var goals = await _goalQueryService.GetGoalsForUserAsync(userId);

            // Apply filters
            if (!includeCompleted)
            {
                goals = goals.Where(g => !g.IsCompleted && !g.CompletedDate.HasValue);
            }

            if (startDate.HasValue)
            {
                goals = goals.Where(g => g.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                goals = goals.Where(g => g.CreatedDate <= endDate.Value);
            }

            // Get milestone data for progress tracking
            var goalIds = goals.Select(g => g.Id).ToList();
            var milestones = await _context.GoalMilestones
                .Where(m => goalIds.Contains(m.GoalId))
                .OrderBy(m => m.Date)
                .ToListAsync();

            // Group milestones by goal ID for easy access
            var milestonesByGoal = milestones.GroupBy(m => m.GoalId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Transform to DTO format
            var result = new List<GoalExportDto>();
            foreach (var goal in goals)
            {
                var goalMilestones = milestonesByGoal.TryGetValue(goal.Id, out var milestoneList)
                    ? milestoneList.Select(m => new GoalMilestoneDto
                    {
                        Date = m.Date,
                        Value = m.Value,
                        ProgressPercentage = m.ProgressPercentage,
                        Notes = m.Notes,
                        Source = m.Source
                    }).ToList()
                    : new List<GoalMilestoneDto>();

                result.Add(new GoalExportDto
                {
                    Id = goal.Id,
                    Description = goal.Description,
                    Category = goal.Category.ToString(),
                    CustomCategory = goal.CustomCategory,
                    CreatedDate = goal.CreatedDate,
                    TargetDate = goal.TargetDate,
                    CompletedDate = goal.CompletedDate,
                    IsCompleted = goal.IsCompleted,
                    MeasurementType = goal.MeasurementType,
                    MeasurementUnit = goal.MeasurementUnit,
                    StartValue = goal.StartValue,
                    CurrentValue = goal.CurrentValue,
                    TargetValue = goal.TargetValue,
                    ProgressPercentage = goal.ProgressPercentage,
                    Milestones = goalMilestones,
                    IsCoachCreated = goal.IsCoachCreated
                });
            }

            return result;
        }
    }

    /// <summary>
    /// DTO for goal export and API integration
    /// </summary>
    public class GoalExportDto
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string CustomCategory { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime TargetDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public bool IsCompleted { get; set; }
        public string MeasurementType { get; set; }
        public string MeasurementUnit { get; set; }
        public decimal? StartValue { get; set; }
        public decimal? CurrentValue { get; set; }
        public decimal? TargetValue { get; set; }
        public int ProgressPercentage { get; set; }
        public List<GoalMilestoneDto> Milestones { get; set; }
        public bool IsCoachCreated { get; set; }
    }

    /// <summary>
    /// DTO for goal milestone export
    /// </summary>
    public class GoalMilestoneDto
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public int ProgressPercentage { get; set; }
        public string Notes { get; set; }
        public string Source { get; set; }
    }
}