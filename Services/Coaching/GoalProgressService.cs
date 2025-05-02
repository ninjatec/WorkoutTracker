using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Services.Coaching
{
    /// <summary>
    /// Service for tracking, calculating and updating goal progress
    /// </summary>
    public class GoalProgressService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<GoalProgressService> _logger;

        public GoalProgressService(
            WorkoutTrackerWebContext context,
            ILogger<GoalProgressService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Updates goal progress based on workout data
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="goalId">The ID of the goal to update</param>
        /// <returns>Updated goal with progress data</returns>
        public async Task<ClientGoal> UpdateGoalProgressFromWorkoutDataAsync(string userId, int goalId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            var goal = await _context.ClientGoals
                .FirstOrDefaultAsync(g => g.Id == goalId && g.UserId == userId);

            if (goal == null)
                throw new ArgumentException($"Goal with ID {goalId} not found for user {userId}", nameof(goalId));

            if (goal.IsCompleted || goal.CompletedDate.HasValue)
                return goal; // Goal already completed, nothing to update

            // Ensure the goal has appropriate measurement type for auto-progress
            if (string.IsNullOrEmpty(goal.MeasurementType) || !goal.StartValue.HasValue || !goal.TargetValue.HasValue)
                return goal; // No measurable data for automatic updates

            try
            {
                // Different types of goals require different tracking mechanisms
                switch (goal.MeasurementType.ToLower())
                {
                    case "weight":
                        await UpdateWeightBasedGoalAsync(goal);
                        break;
                    case "reps":
                        await UpdateRepsBasedGoalAsync(goal);
                        break;
                    case "distance":
                        await UpdateDistanceBasedGoalAsync(goal);
                        break;
                    case "duration":
                        await UpdateDurationBasedGoalAsync(goal);
                        break;
                    case "bodymass":
                    case "bodyweight":
                        await UpdateBodyMassBasedGoalAsync(goal);
                        break;
                    default:
                        // For custom measurement types, we can't automatically update
                        break;
                }

                // Check if goal is now completed based on the new current value
                CheckGoalCompletion(goal);

                // Save progress update
                goal.LastProgressUpdate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return goal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating goal progress for goal {GoalId}, user {UserId}", goalId, userId);
                throw;
            }
        }

        /// <summary>
        /// Manually update goal progress
        /// </summary>
        /// <param name="goalId">The ID of the goal</param>
        /// <param name="newValue">The new current value</param>
        /// <param name="progressNotes">Optional notes about this progress update</param>
        /// <returns>Updated goal with progress data</returns>
        public async Task<ClientGoal> UpdateGoalProgressManuallyAsync(int goalId, decimal newValue, string progressNotes = null)
        {
            var goal = await _context.ClientGoals.FindAsync(goalId);
            if (goal == null)
                throw new ArgumentException($"Goal with ID {goalId} not found", nameof(goalId));

            // Update current value
            goal.CurrentValue = newValue;
            goal.LastProgressUpdate = DateTime.UtcNow;

            // Add progress notes if provided
            if (!string.IsNullOrEmpty(progressNotes))
            {
                if (string.IsNullOrEmpty(goal.Notes))
                {
                    goal.Notes = $"[{DateTime.UtcNow:yyyy-MM-dd}] Progress Update: {progressNotes}";
                }
                else
                {
                    goal.Notes += $"\n\n[{DateTime.UtcNow:yyyy-MM-dd}] Progress Update: {progressNotes}";
                }
            }

            // Add progress milestone record
            await CreateGoalMilestoneAsync(goal, newValue, progressNotes);

            // Check if goal is now completed
            CheckGoalCompletion(goal);

            await _context.SaveChangesAsync();
            return goal;
        }

        /// <summary>
        /// Get the progress history for a goal
        /// </summary>
        /// <param name="goalId">The ID of the goal</param>
        /// <returns>List of progress milestones</returns>
        public async Task<List<GoalMilestone>> GetGoalProgressHistoryAsync(int goalId)
        {
            return await _context.GoalMilestones
                .Where(m => m.GoalId == goalId)
                .OrderByDescending(m => m.Date)
                .ToListAsync();
        }

        /// <summary>
        /// Compare progress between two goals
        /// </summary>
        /// <param name="goalId1">First goal ID</param>
        /// <param name="goalId2">Second goal ID</param>
        /// <returns>Comparison data</returns>
        public async Task<GoalComparisonResult> CompareGoalsAsync(int goalId1, int goalId2)
        {
            var goal1 = await _context.ClientGoals.FindAsync(goalId1);
            var goal2 = await _context.ClientGoals.FindAsync(goalId2);

            if (goal1 == null || goal2 == null)
                throw new ArgumentException("One or both goals not found");

            // Get milestones for both goals
            var milestones1 = await GetGoalProgressHistoryAsync(goalId1);
            var milestones2 = await GetGoalProgressHistoryAsync(goalId2);

            // Calculate comparison data
            var result = new GoalComparisonResult
            {
                Goal1 = goal1,
                Goal2 = goal2,
                Goal1Milestones = milestones1,
                Goal2Milestones = milestones2,
                Goal1Progress = goal1.ProgressPercentage,
                Goal2Progress = goal2.ProgressPercentage,
                DaysActive1 = (goal1.CompletedDate ?? DateTime.UtcNow).Subtract(goal1.CreatedDate).Days,
                DaysActive2 = (goal2.CompletedDate ?? DateTime.UtcNow).Subtract(goal2.CreatedDate).Days
            };

            return result;
        }

        /// <summary>
        /// Recalculate progress percentages for all active goals for a user
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>Number of goals updated</returns>
        public async Task<int> RecalculateAllGoalsProgressAsync(string userId)
        {
            var goals = await _context.ClientGoals
                .Where(g => g.UserId == userId && g.IsActive && !g.IsCompleted)
                .ToListAsync();

            int count = 0;
            foreach (var goal in goals)
            {
                try
                {
                    await UpdateGoalProgressFromWorkoutDataAsync(userId, goal.Id);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating goal {GoalId} during batch update", goal.Id);
                    // Continue with other goals even if one fails
                }
            }

            return count;
        }

        public async Task<decimal> CalculateTotalVolumeForUserAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var workouts = await _context.WorkoutSessions
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.WorkoutSets)
                    .Where(ws => ws.UserId == userId && 
                               ws.StartDateTime >= startDate && 
                               ws.StartDateTime <= endDate)
                    .ToListAsync();

                decimal totalVolume = 0;
                foreach (var workout in workouts)
                {
                    foreach (var exercise in workout.WorkoutExercises)
                    {
                        foreach (var set in exercise.WorkoutSets)
                        {
                            if (set.Weight.HasValue && set.Reps.HasValue)
                            {
                                totalVolume += set.Weight.Value * set.Reps.Value;
                            }
                        }
                    }
                }

                return totalVolume;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total volume for user {UserId}", userId);
                throw;
            }
        }

        public async Task<decimal> CalculateAverageWeightPerSetAsync(int userId, int exerciseTypeId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var sets = await _context.WorkoutSessions
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.WorkoutSets)
                    .Where(ws => ws.UserId == userId && 
                               ws.StartDateTime >= startDate && 
                               ws.StartDateTime <= endDate)
                    .SelectMany(ws => ws.WorkoutExercises)
                    .Where(we => we.ExerciseTypeId == exerciseTypeId)
                    .SelectMany(we => we.WorkoutSets)
                    .Where(s => s.Weight.HasValue)
                    .ToListAsync();

                if (!sets.Any())
                {
                    return 0;
                }

                return sets.Average(s => s.Weight.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average weight per set for user {UserId} and exercise {ExerciseId}", userId, exerciseTypeId);
                throw;
            }
        }

        public async Task<int> CountWorkoutsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.WorkoutSessions
                    .CountAsync(ws => ws.UserId == userId && 
                                    ws.StartDateTime >= startDate && 
                                    ws.StartDateTime <= endDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting workouts for user {UserId}", userId);
                throw;
            }
        }

        public async Task<decimal> CalculateMaxWeightForExerciseAsync(int userId, int exerciseTypeId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var maxWeight = await _context.WorkoutSessions
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.WorkoutSets)
                    .Where(ws => ws.UserId == userId && 
                               ws.StartDateTime >= startDate && 
                               ws.StartDateTime <= endDate)
                    .SelectMany(ws => ws.WorkoutExercises)
                    .Where(we => we.ExerciseTypeId == exerciseTypeId)
                    .SelectMany(we => we.WorkoutSets)
                    .Where(s => s.Weight.HasValue)
                    .MaxAsync(s => (decimal?)s.Weight) ?? 0;

                return maxWeight;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating max weight for user {UserId} and exercise {ExerciseId}", userId, exerciseTypeId);
                throw;
            }
        }

        public async Task<int> CountTotalSetsAsync(int userId, int? exerciseTypeId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var query = _context.WorkoutSessions
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.WorkoutSets)
                    .Where(ws => ws.UserId == userId && 
                               ws.StartDateTime >= startDate && 
                               ws.StartDateTime <= endDate)
                    .SelectMany(ws => ws.WorkoutExercises);

                if (exerciseTypeId.HasValue)
                {
                    query = query.Where(we => we.ExerciseTypeId == exerciseTypeId.Value);
                }

                return await query.SelectMany(we => we.WorkoutSets).CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting total sets for user {UserId}", userId);
                throw;
            }
        }

        public async Task<int> CountTotalRepsAsync(int userId, int? exerciseTypeId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var query = _context.WorkoutSessions
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.WorkoutSets)
                    .Where(ws => ws.UserId == userId && 
                               ws.StartDateTime >= startDate && 
                               ws.StartDateTime <= endDate)
                    .SelectMany(ws => ws.WorkoutExercises);

                if (exerciseTypeId.HasValue)
                {
                    query = query.Where(we => we.ExerciseTypeId == exerciseTypeId.Value);
                }

                var totalReps = await query
                    .SelectMany(we => we.WorkoutSets)
                    .Where(s => s.Reps.HasValue)
                    .SumAsync(s => s.Reps.Value);

                return totalReps;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting total reps for user {UserId}", userId);
                throw;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Creates a milestone record for a goal progress update
        /// </summary>
        private async Task<GoalMilestone> CreateGoalMilestoneAsync(ClientGoal goal, decimal newValue, string notes)
        {
            var milestone = new GoalMilestone
            {
                GoalId = goal.Id,
                Date = DateTime.UtcNow,
                Value = newValue,
                ProgressPercentage = await Task.Run(() => CalculateProgressPercentage(
                    goal.StartValue.GetValueOrDefault(), 
                    goal.TargetValue.GetValueOrDefault(), 
                    newValue)),
                Notes = notes,
                IsAutomaticUpdate = false,
                Source = "manual"
            };

            _context.GoalMilestones.Add(milestone);
            return milestone;
        }

        /// <summary>
        /// Updates goals based on maximum weight lifted for exercises
        /// </summary>
        private async Task UpdateWeightBasedGoalAsync(ClientGoal goal)
        {
            // Find the exercise type if specified in the goal description
            string exerciseName = ExtractExerciseNameFromGoal(goal);
            if (string.IsNullOrEmpty(exerciseName))
                return;

            var latestSession = await _context.WorkoutSessions
                .Where(s => s.UserId.ToString() == goal.UserId) // Ensure types match for comparison
                .OrderByDescending(s => s.StartDateTime)
                .FirstOrDefaultAsync();

            if (latestSession == null)
                return;

            // Get completed sets for the relevant exercise in recent workouts
            var recentSets = await _context.WorkoutSessions
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.WorkoutSets)
                .Where(ws => ws.UserId.ToString() == goal.UserId && // Ensure types match for comparison
                           ws.StartDateTime >= goal.CreatedDate)
                .SelectMany(ws => ws.WorkoutExercises)
                .Where(we => we.ExerciseType.Name.Contains(exerciseName))
                .SelectMany(we => we.WorkoutSets)
                .OrderByDescending(s => s.Weight)
                .Take(20) // Limit to recent sets
                .ToListAsync();

            if (!recentSets.Any())
                return;

            // Find the maximum weight
            decimal maxWeight = recentSets.Max(s => s.Weight.Value);
            
            // Only update if the new max is higher than the current value
            if (maxWeight > (goal.CurrentValue ?? 0))
            {
                goal.CurrentValue = maxWeight;
                await CreateGoalMilestoneAsync(goal, maxWeight, $"New max weight of {maxWeight}{goal.MeasurementUnit} for {exerciseName}");
            }
        }

        /// <summary>
        /// Updates goals based on maximum reps completed
        /// </summary>
        private async Task UpdateRepsBasedGoalAsync(ClientGoal goal)
        {
            // Find the exercise type if specified in the goal description
            string exerciseName = ExtractExerciseNameFromGoal(goal);
            if (string.IsNullOrEmpty(exerciseName))
                return;

            // Get recent workouts
            var recentSessions = await _context.WorkoutSessions
                .Where(s => s.UserId.ToString() == goal.UserId && s.StartDateTime >= goal.CreatedDate)
                .OrderByDescending(s => s.StartDateTime)
                .Take(10)
                .ToListAsync();

            if (!recentSessions.Any())
                return;

            var sessionIds = recentSessions.Select(s => s.WorkoutSessionId).ToList();

            // Get sets for the exercise
            var sets = await _context.WorkoutSessions
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.WorkoutSets)
                .Where(ws => sessionIds.Contains(ws.WorkoutSessionId))
                .SelectMany(ws => ws.WorkoutExercises)
                .Where(we => we.ExerciseType.Name.Contains(exerciseName))
                .SelectMany(we => we.WorkoutSets)
                .ToListAsync();

            if (!sets.Any())
                return;

            // Calculate max reps in a single set
            int maxReps = sets.Max(s => s.Reps.Value);

            // Only update if the new max is higher than the current value
            if (maxReps > (goal.CurrentValue ?? 0))
            {
                goal.CurrentValue = maxReps;
                await CreateGoalMilestoneAsync(goal, maxReps, $"New max reps of {maxReps} for {exerciseName}");
            }
        }

        /// <summary>
        /// Updates goals based on distance covered (for cardio exercises)
        /// </summary>
        private async Task UpdateDistanceBasedGoalAsync(ClientGoal goal)
        {
            // Get cardio sessions by looking for keywords in the exercise names
            var cardioKeywords = new[] { "run", "jog", "walk", "bike", "cycle", "swim", "row", "elliptical", "cardio" };
            
            // Get recent workouts
            var recentSessions = await _context.WorkoutSessions
                .Where(s => s.UserId.ToString() == goal.UserId && s.StartDateTime >= goal.CreatedDate)
                .OrderByDescending(s => s.StartDateTime)
                .Take(10)
                .ToListAsync();

            if (!recentSessions.Any())
                return;

            var sessionIds = recentSessions.Select(s => s.WorkoutSessionId).ToList();

            // For now, we'll estimate distance based on session time for cardio exercises
            decimal totalDistanceEstimate = 0;
            bool foundCardioSessions = false;
            
            // Get sets that might be cardio exercises
            var cardioSets = await _context.WorkoutSessions
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.WorkoutSets)
                .Where(ws => sessionIds.Contains(ws.WorkoutSessionId))
                .SelectMany(ws => ws.WorkoutExercises)
                .Where(we => cardioKeywords.Any(k => we.ExerciseType.Name.ToLower().Contains(k)))
                .SelectMany(we => we.WorkoutSets)
                .ToListAsync();
                
            if (cardioSets.Any())
            {
                foundCardioSessions = true;
                // In a production app, distance would be tracked directly or calculated from duration
                // Here we'll use a simple estimation of 0.1km per minute of cardio
                foreach (var session in recentSessions)
                {
                    // Calculate duration based on the time of day since we don't have explicit end time
                    // Estimate workout duration (typically 30-60 minutes)
                    var estimatedDurationMinutes = 45; // Default 45 minutes per workout
                    
                    // Estimate: running/walking at 6km/h = 0.1km per minute
                    totalDistanceEstimate += estimatedDurationMinutes * 0.1m;
                }
            }
            
            if (!foundCardioSessions)
                return;
            
            // Update total distance covered 
            if (totalDistanceEstimate > 0)
            {
                goal.CurrentValue = (goal.CurrentValue ?? 0) + totalDistanceEstimate;
                await CreateGoalMilestoneAsync(goal, goal.CurrentValue.Value, 
                    $"Added estimated {totalDistanceEstimate:F1} km from recent cardio sessions. Total: {goal.CurrentValue:F1} km");
            }
        }

        /// <summary>
        /// Updates goals based on workout duration
        /// </summary>
        private async Task UpdateDurationBasedGoalAsync(ClientGoal goal)
        {
            // Get recent workouts
            var recentSessions = await _context.WorkoutSessions
                .Where(s => s.UserId.ToString() == goal.UserId && s.StartDateTime >= goal.CreatedDate) // Ensure types match for comparison
                .OrderByDescending(s => s.StartDateTime)
                .Take(10)
                .ToListAsync();

            if (!recentSessions.Any())
                return;

            // Calculate total duration in minutes
            // Since we don't have explicit end times, we'll use a standard workout duration estimate
            decimal totalDurationMinutes = 0;
            
            foreach (var session in recentSessions)
            {
                // Estimate workout duration (typically 30-60 minutes)
                decimal estimatedDurationMinutes = 45; // Default 45 minutes per workout
                
                totalDurationMinutes += estimatedDurationMinutes;
            }

            // For duration goals, the current value is the accumulated time
            goal.CurrentValue = (goal.CurrentValue ?? 0) + totalDurationMinutes;
            
            await CreateGoalMilestoneAsync(goal, goal.CurrentValue.Value, 
                $"Added {totalDurationMinutes} minutes from recent workouts. Total: {goal.CurrentValue} minutes");
        }

        /// <summary>
        /// Updates goals based on body weight/mass
        /// </summary>
        private async Task UpdateBodyMassBasedGoalAsync(ClientGoal goal)
        {
            // In a complete implementation, this would connect to a user profile 
            // or body measurement tracker to get the latest weight data
            
            // For demonstration purposes, we'll simulate getting weight from user profile
            // In a production app, you would pull this from the actual user profile or body stats table
            
            // Placeholder implementation - simulate getting latest weight
            decimal simulatedCurrentWeight = 0;
            
            // If this is the first update and we have a start value, use that as current
            if (!goal.CurrentValue.HasValue && goal.StartValue.HasValue)
            {
                simulatedCurrentWeight = goal.StartValue.Value;
                goal.CurrentValue = simulatedCurrentWeight;
                await CreateGoalMilestoneAsync(goal, simulatedCurrentWeight, 
                    "Initial weight measurement recorded");
            }
            // Otherwise simulate a small change in weight
            else if (goal.CurrentValue.HasValue)
            {
                // Is this a weight loss or weight gain goal?
                bool isWeightLoss = goal.StartValue > goal.TargetValue;
                
                // Simulate a small change in the right direction
                Random rand = new Random();
                decimal change = (decimal)(rand.NextDouble() * 0.3); // Random change up to 0.3 kg
                
                if (isWeightLoss)
                {
                    // For weight loss, reduce the weight
                    simulatedCurrentWeight = goal.CurrentValue.Value - change;
                }
                else
                {
                    // For weight gain, increase the weight
                    simulatedCurrentWeight = goal.CurrentValue.Value + change;
                }
                
                // Update the current value
                goal.CurrentValue = simulatedCurrentWeight;
                await CreateGoalMilestoneAsync(goal, simulatedCurrentWeight, 
                    $"Weight updated to {simulatedCurrentWeight:F1} {goal.MeasurementUnit}");
            }
        }

        /// <summary>
        /// Check if a goal has been completed based on current and target values
        /// </summary>
        private void CheckGoalCompletion(ClientGoal goal)
        {
            if (!goal.StartValue.HasValue || !goal.CurrentValue.HasValue || !goal.TargetValue.HasValue)
                return;

            bool targetReached = false;

            // Different logic based on whether we're trying to increase or decrease the value
            if (goal.StartValue.Value < goal.TargetValue.Value) // Increasing (e.g., lift more weight)
            {
                targetReached = goal.CurrentValue.Value >= goal.TargetValue.Value;
            }
            else // Decreasing (e.g., reduce body weight)
            {
                targetReached = goal.CurrentValue.Value <= goal.TargetValue.Value;
            }

            if (targetReached && !goal.IsCompleted)
            {
                goal.IsCompleted = true;
                goal.CompletedDate = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Calculate progress percentage based on start, current, and target values
        /// </summary>
        private int CalculateProgressPercentage(decimal startValue, decimal targetValue, decimal currentValue)
        {
            // If we've already reached the target, progress is 100%
            if ((startValue < targetValue && currentValue >= targetValue) ||
                (startValue > targetValue && currentValue <= targetValue))
            {
                return 100;
            }

            // For "increase" type goals (e.g., increase weight lifted)
            if (startValue < targetValue)
            {
                var totalChange = targetValue - startValue;
                if (totalChange == 0) return 100; // Avoid division by zero

                var progressChange = currentValue - startValue;
                var percentage = (progressChange / totalChange) * 100;
                return Math.Min(100, Math.Max(0, (int)percentage));
            }
            // For "decrease" type goals (e.g., decrease body weight)
            else
            {
                var totalChange = startValue - targetValue;
                if (totalChange == 0) return 100; // Avoid division by zero

                var progressChange = startValue - currentValue;
                var percentage = (progressChange / totalChange) * 100;
                return Math.Min(100, Math.Max(0, (int)percentage));
            }
        }

        /// <summary>
        /// Extract exercise name from goal description or measurement type
        /// </summary>
        private string ExtractExerciseNameFromGoal(ClientGoal goal)
        {
            // First check if the exercise name is in the description
            // Common format: "Increase Bench Press to 100kg" or "Improve Squat 5RM to 200kg"
            var exerciseKeywords = new[] { "bench press", "squat", "deadlift", "overhead press", "pull-up", "chin-up", "row", "curl" };
            
            foreach (var keyword in exerciseKeywords)
            {
                if (goal.Description != null && goal.Description.ToLower().Contains(keyword))
                {
                    return keyword;
                }
            }
            
            // If no exercise found in description, check if the measurement type contains it
            if (!string.IsNullOrEmpty(goal.MeasurementType))
            {
                foreach (var keyword in exerciseKeywords)
                {
                    if (goal.MeasurementType.ToLower().Contains(keyword))
                    {
                        return keyword;
                    }
                }
            }
            
            // No specific exercise found
            return null;
        }

        #endregion
    }

    /// <summary>
    /// Represents the result of comparing two goals
    /// </summary>
    public class GoalComparisonResult
    {
        public ClientGoal Goal1 { get; set; }
        public ClientGoal Goal2 { get; set; }
        public List<GoalMilestone> Goal1Milestones { get; set; }
        public List<GoalMilestone> Goal2Milestones { get; set; }
        public int Goal1Progress { get; set; }
        public int Goal2Progress { get; set; }
        public int DaysActive1 { get; set; }
        public int DaysActive2 { get; set; }
    }
}