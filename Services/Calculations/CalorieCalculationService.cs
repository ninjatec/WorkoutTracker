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
                    ? (workoutSession.EndDateTime.Value - workoutSession.StartDateTime).TotalMinutes
                    : 45; // Default to 45 minutes if no end time

                decimal totalCalories = 0;
                
                foreach (var exercise in workoutSession.WorkoutExercises)
                {
                    // Base calorie burn rate per minute for the exercise type
                    decimal baseCaloriesPerMinute = exercise.ExerciseType?.CaloriesPerMinute ?? 5.0m;

                    // Calculate exercise duration
                    var exerciseDuration = (exercise.EndTime - exercise.StartTime)?.TotalMinutes ?? duration / workoutSession.WorkoutExercises.Count;

                    // Calculate intensity multiplier based on volume
                    decimal intensityMultiplier = 1.0m;
                    if (exercise.WorkoutSets.Any())
                    {
                        var avgWeight = exercise.WorkoutSets.Average(s => s.Weight ?? 0);
                        var totalReps = exercise.WorkoutSets.Sum(s => s.Reps ?? 0);
                        intensityMultiplier = 1.0m + (avgWeight / 100m) + (totalReps / 100m);
                    }

                    totalCalories += (decimal)exerciseDuration * baseCaloriesPerMinute * intensityMultiplier;
                }

                return Math.Round(totalCalories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating calories for workout session {Id}", workoutSession.WorkoutSessionId);
                throw;
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

            foreach (var exercise in session.WorkoutExercises)
            {
                if (exercise.ExerciseType == null) continue;

                decimal baseCaloriesPerMinute = exercise.ExerciseType.CaloriesPerMinute ?? 5m;
                var duration = (exercise.EndTime - exercise.StartTime)?.TotalMinutes ?? 
                    (session.EndDateTime.HasValue ? (session.EndDateTime.Value - session.StartDateTime).TotalMinutes / session.WorkoutExercises.Count : 15);
                
                decimal intensityMultiplier = 1.0m;
                if (exercise.WorkoutSets.Any())
                {
                    var avgWeight = exercise.WorkoutSets.Average(s => s.Weight ?? 0);
                    var totalReps = exercise.WorkoutSets.Sum(s => s.Reps ?? 0);
                    intensityMultiplier = 1.0m + (avgWeight / 100m) + (totalReps / 100m);
                }

                var calories = (decimal)duration * baseCaloriesPerMinute * intensityMultiplier;
                
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