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
    public interface IVolumeCalculationService
    {
        Task<double> CalculateSessionVolumeAsync(int sessionId);
        double CalculateSetVolume(Set set);
        Task<Dictionary<string, double>> CalculateVolumeByExerciseTypeAsync(int sessionId);
        
        // New methods for WorkoutSession
        Task<double> CalculateWorkoutSessionVolumeAsync(int workoutSessionId);
        double CalculateWorkoutSetVolume(WorkoutSet workoutSet);
        Task<Dictionary<string, double>> CalculateVolumeByExerciseTypeForWorkoutSessionAsync(int workoutSessionId);
    }

    public class VolumeCalculationService : IVolumeCalculationService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<VolumeCalculationService> _logger;
        private const string CacheKeyPrefix = "VolumeCalculation_";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public VolumeCalculationService(
            WorkoutTrackerWebContext context,
            IMemoryCache cache,
            ILogger<VolumeCalculationService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        // Legacy method - kept for backward compatibility
        public async Task<double> CalculateSessionVolumeAsync(int sessionId)
        {
            string cacheKey = $"{CacheKeyPrefix}Session_{sessionId}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out double cachedVolume))
            {
                return cachedVolume;
            }

            try
            {
                // Get all sets for the session
                var sets = await _context.Set
                    .Where(s => s.SessionId == sessionId)
                    .Include(s => s.ExerciseType)
                    .ToListAsync();

                double totalVolume = 0;

                foreach (var set in sets)
                {
                    totalVolume += CalculateSetVolume(set);
                }

                // Cache the result
                _cache.Set(cacheKey, totalVolume, CacheDuration);
                return totalVolume;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating session volume for session {SessionId}", sessionId);
                return 0;
            }
        }

        // Legacy method - kept for backward compatibility
        public double CalculateSetVolume(Set set)
        {
            if (set == null || set.NumberReps == 0 || set.Weight == 0)
            {
                return 0;
            }

            // Basic volume calculation: weight × reps
            // This could be enhanced with different formulas for different exercises
            return (double)set.Weight * set.NumberReps;
        }

        // Legacy method - kept for backward compatibility
        public async Task<Dictionary<string, double>> CalculateVolumeByExerciseTypeAsync(int sessionId)
        {
            string cacheKey = $"{CacheKeyPrefix}SessionByExercise_{sessionId}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out Dictionary<string, double> cachedVolumes))
            {
                return cachedVolumes;
            }

            try
            {
                // Get all sets for the session grouped by exercise type
                var sets = await _context.Set
                    .Where(s => s.SessionId == sessionId)
                    .Include(s => s.ExerciseType)
                    .ToListAsync();

                var volumeByExercise = new Dictionary<string, double>();

                foreach (var set in sets)
                {
                    if (set.ExerciseType == null) continue;

                    string exerciseName = set.ExerciseType.Name;
                    double setVolume = CalculateSetVolume(set);

                    if (volumeByExercise.ContainsKey(exerciseName))
                    {
                        volumeByExercise[exerciseName] += setVolume;
                    }
                    else
                    {
                        volumeByExercise[exerciseName] = setVolume;
                    }
                }

                // Cache the result
                _cache.Set(cacheKey, volumeByExercise, CacheDuration);
                return volumeByExercise;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating volume by exercise for session {SessionId}", sessionId);
                return new Dictionary<string, double>();
            }
        }

        // New method for WorkoutSession
        public async Task<double> CalculateWorkoutSessionVolumeAsync(int workoutSessionId)
        {
            string cacheKey = $"{CacheKeyPrefix}WorkoutSession_{workoutSessionId}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out double cachedVolume))
            {
                return cachedVolume;
            }

            try
            {
                // Get all workout exercises and their sets for the workout session
                var workoutExercises = await _context.WorkoutExercises
                    .Where(we => we.WorkoutSessionId == workoutSessionId)
                    .Include(we => we.WorkoutSets)
                    .ToListAsync();

                double totalVolume = 0;

                foreach (var exercise in workoutExercises)
                {
                    foreach (var set in exercise.WorkoutSets)
                    {
                        totalVolume += CalculateWorkoutSetVolume(set);
                    }
                }

                // Cache the result
                _cache.Set(cacheKey, totalVolume, CacheDuration);
                return totalVolume;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating workout session volume for session {WorkoutSessionId}", workoutSessionId);
                return 0;
            }
        }

        // New method for WorkoutSet
        public double CalculateWorkoutSetVolume(WorkoutSet workoutSet)
        {
            if (workoutSet == null || !workoutSet.Reps.HasValue || !workoutSet.Weight.HasValue || 
                workoutSet.Reps.Value == 0 || workoutSet.Weight.Value == 0)
            {
                return 0;
            }

            // Basic volume calculation: weight × reps
            return (double)workoutSet.Weight.Value * workoutSet.Reps.Value;
        }

        // New method to calculate volume by exercise type for WorkoutSession
        public async Task<Dictionary<string, double>> CalculateVolumeByExerciseTypeForWorkoutSessionAsync(int workoutSessionId)
        {
            string cacheKey = $"{CacheKeyPrefix}WorkoutSessionByExercise_{workoutSessionId}";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out Dictionary<string, double> cachedVolumes))
            {
                return cachedVolumes;
            }

            try
            {
                // Get all workout exercises and their sets for the workout session
                var workoutExercises = await _context.WorkoutExercises
                    .Where(we => we.WorkoutSessionId == workoutSessionId)
                    .Include(we => we.ExerciseType)
                    .Include(we => we.WorkoutSets)
                    .ToListAsync();

                var volumeByExercise = new Dictionary<string, double>();

                foreach (var exercise in workoutExercises)
                {
                    if (exercise.ExerciseType == null) continue;

                    string exerciseName = exercise.ExerciseType.Name;
                    double exerciseVolume = 0;

                    foreach (var set in exercise.WorkoutSets)
                    {
                        exerciseVolume += CalculateWorkoutSetVolume(set);
                    }

                    if (volumeByExercise.ContainsKey(exerciseName))
                    {
                        volumeByExercise[exerciseName] += exerciseVolume;
                    }
                    else
                    {
                        volumeByExercise[exerciseName] = exerciseVolume;
                    }
                }

                // Cache the result
                _cache.Set(cacheKey, volumeByExercise, CacheDuration);
                return volumeByExercise;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating volume by exercise for workout session {WorkoutSessionId}", workoutSessionId);
                return new Dictionary<string, double>();
            }
        }
    }
}