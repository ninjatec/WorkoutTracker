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
        
        // Constants for BMR calculation (Mifflin-St Jeor equation)
        private const decimal DEFAULT_WEIGHT_KG = 70.0m; // Default weight if user hasn't provided one
        private const decimal DEFAULT_HEIGHT_CM = 170.0m; // Default height if user hasn't provided one
        private const int DEFAULT_AGE = 30; // Default age when not known
        private const decimal ACTIVITY_MULTIPLIER_WORKOUT = 1.5m; // Activity multiplier for workout sessions

        public CalorieCalculationService(WorkoutTrackerWebContext context, ILogger<CalorieCalculationService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        // Calculate BMR (Basal Metabolic Rate) using Mifflin-St Jeor equation
        private decimal CalculateBMR(decimal weightKg, decimal heightCm, bool isMale = true)
        {
            // Mifflin-St Jeor equation: 
            // For males: BMR = (10 * weight in kg) + (6.25 * height in cm) - (5 * age) + 5
            // For females: BMR = (10 * weight in kg) + (6.25 * height in cm) - (5 * age) - 161
            
            decimal bmr = (10m * weightKg) + (6.25m * heightCm) - (5m * DEFAULT_AGE);
            
            if (isMale)
            {
                bmr += 5m;
            }
            else
            {
                bmr -= 161m;
            }
            
            return bmr;
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
                
                // Get user data for personalized calculations if available
                decimal userWeight = DEFAULT_WEIGHT_KG;
                decimal userHeight = DEFAULT_HEIGHT_CM;
                decimal userBmrFactor = 1.0m;
                
                if (workoutSession.User != null)
                {
                    // Use the user's actual weight and height if available, otherwise fall back to defaults
                    userWeight = workoutSession.User.WeightKg ?? DEFAULT_WEIGHT_KG;
                    userHeight = workoutSession.User.HeightCm ?? DEFAULT_HEIGHT_CM;
                    
                    // Calculate BMR factor based on user's metrics
                    // This gives us a personalized multiplier based on the user's body metrics
                    decimal bmr = CalculateBMR(userWeight, userHeight);
                    userBmrFactor = bmr / 1800m; // Normalize against a reference BMR of 1800 calories
                    
                    // Ensure the factor is within a reasonable range
                    userBmrFactor = Math.Min(Math.Max(userBmrFactor, 0.7m), 1.5m);
                    
                    _logger.LogInformation($"Using user metrics for calorie calculation: Height={userHeight}cm, Weight={userWeight}kg, BMR factor={userBmrFactor}");
                }

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

                    // Calculate intensity multiplier based on volume and user's weight
                    decimal intensityMultiplier = 1.0m;
                    if (exercise.WorkoutSets.Any())
                    {
                        var avgWeight = exercise.WorkoutSets.Average(s => s.Weight ?? 0);
                        var totalReps = exercise.WorkoutSets.Sum(s => s.Reps ?? 0);
                        
                        // Adjust intensity based on weight lifted relative to user's body weight
                        decimal bodyWeightRatio = userWeight > 0 ? avgWeight / userWeight : 0.5m;
                        intensityMultiplier = 1.0m + (bodyWeightRatio) + (totalReps / 100m);
                    }

                    // Apply user's BMR factor to make calorie calculation personalized
                    intensityMultiplier *= userBmrFactor;
                    
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
                    
                    // Apply user weight factor to volume-based calculation too
                    decimal volumeCalories = totalVolume * CALORIES_PER_KG_LIFTED;
                    
                    // If user weight is available, adjust calories based on weight
                    // (heavier users burn more calories moving weights)
                    if (workoutSession.User?.WeightKg.HasValue == true)
                    {
                        decimal weightFactor = workoutSession.User.WeightKg.Value / DEFAULT_WEIGHT_KG;
                        weightFactor = Math.Min(Math.Max(weightFactor, 0.7m), 1.5m); // Limit the factor's range
                        volumeCalories *= weightFactor;
                    }
                    
                    totalCalories = volumeCalories;
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
                .Include(s => s.User) // Include the user to access height and weight
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
                .Include(s => s.User) // Include the user to access height and weight
                .FirstOrDefaultAsync(s => s.WorkoutSessionId == sessionId);

            if (session == null)
                return new Dictionary<string, decimal>();

            var caloriesByExercise = new Dictionary<string, decimal>();
            
            // Calculate total volume for the session (for potential fallback)
            decimal totalSessionVolume = session.WorkoutExercises
                .SelectMany(e => e.WorkoutSets)
                .Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0));
                
            // Get user data for personalized calculations if available
            decimal userWeight = DEFAULT_WEIGHT_KG;
            decimal userHeight = DEFAULT_HEIGHT_CM;
            decimal userBmrFactor = 1.0m;
            
            if (session.User != null)
            {
                // Use the user's actual weight and height if available, otherwise fall back to defaults
                userWeight = session.User.WeightKg ?? DEFAULT_WEIGHT_KG;
                userHeight = session.User.HeightCm ?? DEFAULT_HEIGHT_CM;
                
                // Calculate BMR factor based on user's metrics
                decimal bmr = CalculateBMR(userWeight, userHeight);
                userBmrFactor = bmr / 1800m; // Normalize against a reference BMR of 1800 calories
                
                // Ensure the factor is within a reasonable range
                userBmrFactor = Math.Min(Math.Max(userBmrFactor, 0.7m), 1.5m);
            }

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
                    
                    // Adjust intensity based on weight lifted relative to user's body weight
                    decimal bodyWeightRatio = userWeight > 0 ? avgWeight / userWeight : 0.5m;
                    intensityMultiplier = 1.0m + bodyWeightRatio + (totalReps / 100m);
                    
                    // Apply user's BMR factor
                    intensityMultiplier *= userBmrFactor;
                }
                
                // Ensure multiplier is at least 1.0
                intensityMultiplier = Math.Max(intensityMultiplier, 1.0m);

                // Calculate calories for this exercise
                decimal calories = (decimal)duration * baseCaloriesPerMinute * intensityMultiplier;
                
                // If calculated calories is too low and we have volume data, use volume-based estimation
                if (calories < MIN_CALORIES_PER_WORKOUT / session.WorkoutExercises.Count && exerciseVolume > 0)
                {
                    decimal volumeCalories = exerciseVolume * CALORIES_PER_KG_LIFTED;
                    
                    // If user weight is available, adjust calories based on weight
                    if (session.User?.WeightKg.HasValue == true)
                    {
                        decimal weightFactor = session.User.WeightKg.Value / DEFAULT_WEIGHT_KG;
                        weightFactor = Math.Min(Math.Max(weightFactor, 0.7m), 1.5m); // Limit the factor's range
                        volumeCalories *= weightFactor;
                    }
                    
                    calories = volumeCalories;
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