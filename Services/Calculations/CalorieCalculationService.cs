using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Services.Calculations
{
    public interface ICalorieCalculationService
    {
        decimal CalculateCalories(WorkoutSession workoutSession);
        Task<decimal> CalculateSessionCaloriesAsync(int sessionId);
        Task<Dictionary<string, decimal>> CalculateSessionCaloriesByExercise(int sessionId);
    }

    public class CalorieCalculationService : ICalorieCalculationService
    {
        private readonly ILogger<CalorieCalculationService> _logger;
        private readonly WorkoutTrackerWebContext _context;

        // Constants for volume-based calorie estimates
        private const decimal CALORIES_PER_KG_LIFTED = 0.15m;
        private const decimal MIN_CALORIES_PER_MINUTE = 2.0m;
        private const decimal DEFAULT_WORKOUT_DURATION_MINUTES = 45.0m;
        private const decimal MIN_CALORIES_PER_WORKOUT = 50.0m;

        public CalorieCalculationService(WorkoutTrackerWebContext context, ILogger<CalorieCalculationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public decimal CalculateCalories(WorkoutSession workoutSession)
        {
            try
            {
                if (workoutSession == null)
                {
                    return 0;
                }

                // Get workout duration in minutes - approximate if end time not set
                var duration = workoutSession.EndDateTime.HasValue
                    ? (decimal)(workoutSession.EndDateTime.Value - workoutSession.StartDateTime).TotalMinutes
                    : DEFAULT_WORKOUT_DURATION_MINUTES; // Default to 45 minutes if no end time

                // Duration must be positive
                duration = Math.Max(duration, 0);

                decimal totalCalories = 0;
                decimal totalVolume = 0;

                // Calculate total workout volume (used as fallback)
                foreach (var exercise in workoutSession.WorkoutExercises)
                {
                    totalVolume += exercise.WorkoutSets.Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0));
                }
                
                foreach (var exercise in workoutSession.WorkoutExercises)
                {
                    // Base calorie burn rate per minute for the exercise type
                    decimal baseCaloriesPerMinute = exercise.ExerciseType?.CaloriesPerMinute ?? MIN_CALORIES_PER_MINUTE;
                    baseCaloriesPerMinute = Math.Max(baseCaloriesPerMinute, MIN_CALORIES_PER_MINUTE);

                    // Calculate exercise duration
                    var exerciseDuration = (exercise.EndTime - exercise.StartTime)?.TotalMinutes != null 
                        ? (decimal)(exercise.EndTime - exercise.StartTime).Value.TotalMinutes 
                        : duration / workoutSession.WorkoutExercises.Count;
                    exerciseDuration = Math.Max(exerciseDuration, 0);

                    // Calculate intensity multiplier based on volume
                    decimal intensityMultiplier = 1.0m;
                    if (exercise.WorkoutSets.Any())
                    {
                        var avgWeight = exercise.WorkoutSets.Average(s => s.Weight ?? 0);
                        var totalReps = exercise.WorkoutSets.Sum(s => s.Reps ?? 0);
                        intensityMultiplier = 1.0m + (avgWeight / 100m) + (totalReps / 100m);
                    }

                    // Ensure multiplier is at least 1.0
                    intensityMultiplier = Math.Max(intensityMultiplier, 1.0m);

                    // Calculate calories for this exercise
                    var exerciseCalories = exerciseDuration * baseCaloriesPerMinute * intensityMultiplier;
                    
                    // Add to total
                    totalCalories += exerciseCalories;
                }

                // If calculated calories is too low, use volume-based estimation as fallback
                if (totalCalories < MIN_CALORIES_PER_WORKOUT && totalVolume > 0)
                {
                    _logger.LogInformation($"Using volume-based calorie calculation fallback for session {workoutSession.WorkoutSessionId}. Volume: {totalVolume}kg");
                    totalCalories = totalVolume * CALORIES_PER_KG_LIFTED;
                }

                // Ensure result is at least the minimum calories
                totalCalories = Math.Max(totalCalories, MIN_CALORIES_PER_WORKOUT);

                return Math.Round(totalCalories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating calories for workout session {Id}", workoutSession.WorkoutSessionId);
                // Return a safe fallback value instead of throwing
                return MIN_CALORIES_PER_WORKOUT;
            }
        }

        public async Task<decimal> CalculateSessionCaloriesAsync(int sessionId)
        {
            var session = await _context.WorkoutSessions
                .Include(s => s.WorkoutExercises)
                    .ThenInclude(e => e.WorkoutSets)
                .Include(s => s.WorkoutExercises)
                    .ThenInclude(e => e.ExerciseType)
                .FirstOrDefaultAsync(s => s.WorkoutSessionId == sessionId);

            if (session == null)
                return 0;

            return CalculateCalories(session);
        }

        public async Task<Dictionary<string, decimal>> CalculateSessionCaloriesByExercise(int sessionId)
        {
            var session = await _context.WorkoutSessions
                .Include(s => s.WorkoutExercises)
                    .ThenInclude(e => e.WorkoutSets)
                .Include(s => s.WorkoutExercises)
                    .ThenInclude(e => e.ExerciseType)
                .FirstOrDefaultAsync(s => s.WorkoutSessionId == sessionId);

            if (session == null)
                return new Dictionary<string, decimal>();

            var caloriesByExercise = new Dictionary<string, decimal>();
            
            // Calculate total volume for the session (for potential fallback)
            decimal totalSessionVolume = session.WorkoutExercises
                .SelectMany(e => e.WorkoutSets)
                .Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0));

            foreach (var exercise in session.WorkoutExercises)
            {
                if (exercise.ExerciseType == null) continue;

                decimal baseCaloriesPerMinute = exercise.ExerciseType.CaloriesPerMinute ?? MIN_CALORIES_PER_MINUTE;
                baseCaloriesPerMinute = Math.Max(baseCaloriesPerMinute, MIN_CALORIES_PER_MINUTE);
                
                decimal duration;
                if (exercise.EndTime.HasValue && exercise.StartTime.HasValue)
                {
                    duration = (decimal)(exercise.EndTime.Value - exercise.StartTime.Value).TotalMinutes;
                }
                else if (session.EndDateTime.HasValue)
                {
                    duration = (decimal)(session.EndDateTime.Value - session.StartDateTime).TotalMinutes / session.WorkoutExercises.Count;
                }
                else
                {
                    duration = 15m; // Default value
                }
                
                // Duration must be positive
                duration = Math.Max(duration, 0);
                
                decimal intensityMultiplier = 1.0m;
                decimal exerciseVolume = 0;
                
                if (exercise.WorkoutSets.Any())
                {
                    var avgWeight = exercise.WorkoutSets.Average(s => s.Weight ?? 0);
                    var totalReps = exercise.WorkoutSets.Sum(s => s.Reps ?? 0);
                    exerciseVolume = exercise.WorkoutSets.Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0));
                    intensityMultiplier = 1.0m + (avgWeight / 100m) + (totalReps / 100m);
                }
                
                // Ensure multiplier is at least 1.0
                intensityMultiplier = Math.Max(intensityMultiplier, 1.0m);

                // Calculate calories for this exercise
                decimal calories = (decimal)duration * baseCaloriesPerMinute * intensityMultiplier;
                
                // If calculated calories is too low and we have volume data, use volume-based estimation
                if (calories < MIN_CALORIES_PER_WORKOUT / session.WorkoutExercises.Count && exerciseVolume > 0)
                {
                    calories = exerciseVolume * CALORIES_PER_KG_LIFTED;
                }
                
                // Ensure we have positive calories
                calories = Math.Max(calories, 5.0m);
                
                var exerciseName = exercise.ExerciseType.Name ?? "Unknown Exercise";
                if (caloriesByExercise.ContainsKey(exerciseName))
                {
                    caloriesByExercise[exerciseName] += calories;
                }
                else
                {
                    caloriesByExercise[exerciseName] = calories;
                }
            }

            return caloriesByExercise;
        }
    }
}