using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Services.Calculations
{
    public interface ICalorieCalculationService
    {
        Task<double> CalculateSessionCaloriesAsync(int sessionId);
        Task<double> CalculateSetCaloriesAsync(int setId);
    }

    public class CalorieCalculationService : ICalorieCalculationService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CalorieCalculationService> _logger;
        private const string CacheKeyPrefix = "CalorieCalculation_";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        // MET values for different exercise categories
        // MET = Metabolic Equivalent of Task, 1 MET = 1 kcal/kg/hour at rest
        private readonly Dictionary<string, double> _metValues = new()
        {
            // Strength training
            {"strength_light", 3.0},
            {"strength_moderate", 5.0},
            {"strength_vigorous", 6.0},
            {"circuit_training", 8.0},
            
            // Cardio exercises
            {"running", 10.0},
            {"cycling", 8.0},
            {"rowing", 7.0},
            {"elliptical", 5.0},
            
            // Bodyweight exercises
            {"bodyweight_moderate", 3.5},
            {"bodyweight_vigorous", 8.0},
            
            // Fallback values
            {"default", 5.0},
            {"rest", 1.2}
        };

        // Mapping of exercise types to MET categories
        private readonly Dictionary<string, List<string>> _exerciseTypeCategories = new()
        {
            {"strength_light", new List<string> {"dumbbell curl", "tricep extension", "lateral raise"}},
            {"strength_moderate", new List<string> {"bench press", "shoulder press", "leg press", "leg curl", "leg extension"}},
            {"strength_vigorous", new List<string> {"squat", "deadlift", "clean", "snatch", "lunge"}},
            {"circuit_training", new List<string> {"circuit", "hiit", "crossfit"}},
            {"running", new List<string> {"run", "sprint", "jog", "treadmill"}},
            {"cycling", new List<string> {"bike", "cycle", "spinning"}},
            {"rowing", new List<string> {"row", "rowing machine"}},
            {"elliptical", new List<string> {"elliptical", "cross trainer"}},
            {"bodyweight_moderate", new List<string> {"crunch", "plank", "sit-up"}},
            {"bodyweight_vigorous", new List<string> {"push-up", "pull-up", "chin-up", "burpee"}}
        };

        // Default weight in kg for calorie calculations if user weight not available
        private const double DefaultUserWeight = 70.0;

        public CalorieCalculationService(
            WorkoutTrackerWebContext context,
            IMemoryCache cache,
            ILogger<CalorieCalculationService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<double> CalculateSessionCaloriesAsync(int sessionId)
        {
            string cacheKey = $"{CacheKeyPrefix}Session_{sessionId}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out double cachedCalories))
            {
                return cachedCalories;
            }

            try
            {
                // Get the session with related data
                var session = await _context.Session
                    .Include(s => s.Sets)
                    .ThenInclude(s => s.ExerciseType)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                {
                    _logger.LogWarning("Session {SessionId} not found when calculating calories", sessionId);
                    return 0;
                }

                // Get user weight if available (fallback to default)
                double userWeight = await GetUserWeightAsync(session.UserId);
                
                // Calculate workout duration in hours - using a simple duration estimation
                double durationHours = EstimateSessionDuration(session.Sets?.Count ?? 0);

                // Get average MET value for the session
                double averageMet = CalculateAverageMetForSession(session);
                
                // Calculate calories using the formula: MET × Weight (kg) × Duration (hours)
                double calories = averageMet * userWeight * durationHours;
                
                // Cache the result
                _cache.Set(cacheKey, calories, CacheDuration);
                return calories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating session calories for session {SessionId}", sessionId);
                return 0;
            }
        }

        public async Task<double> CalculateSetCaloriesAsync(int setId)
        {
            string cacheKey = $"{CacheKeyPrefix}Set_{setId}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out double cachedCalories))
            {
                return cachedCalories;
            }

            try
            {
                // Get the set with related data
                var set = await _context.Set
                    .Include(s => s.ExerciseType)
                    .Include(s => s.Session)
                    .FirstOrDefaultAsync(s => s.SetId == setId);

                if (set == null)
                {
                    _logger.LogWarning("Set {SetId} not found when calculating calories", setId);
                    return 0;
                }

                // Get user weight (fallback to default)
                double userWeight = await GetUserWeightAsync(set.Session.UserId);
                
                // Estimate set duration in hours (including rest)
                double setDurationHours = EstimateSetDuration(set.NumberReps);
                
                // Get MET value for this exercise
                double met = GetMetValueForExercise(set.ExerciseType?.Name ?? "unknown", set.Weight);
                
                // Calculate calories
                double calories = met * userWeight * setDurationHours;
                
                // Cache the result
                _cache.Set(cacheKey, calories, CacheDuration);
                return calories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating set calories for set {SetId}", setId);
                return 0;
            }
        }

        private async Task<double> GetUserWeightAsync(int userId) 
        {
            // TODO: In a future implementation, retrieve the user's actual weight
            // For now, use the default weight
            return DefaultUserWeight;
        }

        private double CalculateAverageMetForSession(Models.Session session)
        {
            if (session?.Sets == null || !session.Sets.Any())
            {
                return _metValues["default"];
            }

            double totalMet = 0;
            int count = 0;

            foreach (var set in session.Sets)
            {
                if (set.ExerciseType != null)
                {
                    totalMet += GetMetValueForExercise(set.ExerciseType.Name, set.Weight);
                    count++;
                }
            }

            return count > 0 ? totalMet / count : _metValues["default"];
        }

        private double GetMetValueForExercise(string exerciseName, decimal weight)
        {
            if (string.IsNullOrEmpty(exerciseName))
            {
                return _metValues["default"];
            }

            // Convert to lowercase for case-insensitive matching
            string lowerName = exerciseName.ToLower();

            // Look through each category and check if the exercise name contains any of the keywords
            foreach (var category in _exerciseTypeCategories)
            {
                if (category.Value.Any(keyword => lowerName.Contains(keyword)))
                {
                    return _metValues[category.Key];
                }
            }

            // Determine intensity based on weight for strength exercises
            if (weight > 100) 
            {
                return _metValues["strength_vigorous"];
            }
            else if (weight > 50)
            {
                return _metValues["strength_moderate"];
            }
            else if (weight > 0)
            {
                return _metValues["strength_light"];
            }

            // Default fallback
            return _metValues["default"];
        }

        private double EstimateSessionDuration(int setCount)
        {
            // Rough estimation: each set takes about 2 minutes (including rest)
            // Minimum 20 minutes, maximum 120 minutes
            double estimatedMinutes = Math.Max(20, Math.Min(120, setCount * 2));
            return estimatedMinutes / 60.0; // Convert to hours
        }

        private double EstimateSetDuration(int reps)
        {
            // Estimate: each rep takes 3 seconds + 60 seconds rest
            double estimatedSeconds = (reps * 3) + 60;
            return estimatedSeconds / 3600.0; // Convert to hours
        }
    }
}