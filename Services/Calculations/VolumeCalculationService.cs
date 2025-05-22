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
    public interface IVolumeCalculationService
    {
        decimal CalculateWorkoutSessionVolume(WorkoutSession workoutSession);
        decimal CalculateExerciseVolume(WorkoutExercise exercise);
        Dictionary<string, decimal> CalculateSessionVolume(WorkoutSession session);
        double CalculateSetVolume(WorkoutSet set);
    }

    public class VolumeCalculationService : IVolumeCalculationService
    {
        private readonly ILogger<VolumeCalculationService> _logger;

        public VolumeCalculationService(ILogger<VolumeCalculationService> logger)
        {
            _logger = logger;
        }

        public decimal CalculateWorkoutSessionVolume(WorkoutSession workoutSession)
        {
            try
            {
                if (workoutSession == null)
                {
                    return 0;
                }

                decimal totalVolume = 0;

                foreach (var exercise in workoutSession.WorkoutExercises)
                {
                    var exerciseVolume = CalculateExerciseVolume(exercise);
                    totalVolume += exerciseVolume;
                }

                return totalVolume;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total volume for workout session {Id}", workoutSession.WorkoutSessionId);
                throw;
            }
        }

        public decimal CalculateExerciseVolume(WorkoutExercise exercise)
        {
            try
            {
                if (exercise == null || !exercise.WorkoutSets.Any())
                {
                    return 0;
                }

                decimal exerciseVolume = 0;

                foreach (var set in exercise.WorkoutSets)
                {
                    if (set.Weight.HasValue && set.Reps.HasValue)
                    {
                        exerciseVolume += set.Weight.Value * set.Reps.Value;
                    }
                }

                return exerciseVolume;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating volume for exercise {Id}", exercise.WorkoutExerciseId);
                throw;
            }
        }

        public Dictionary<string, decimal> CalculateSessionVolume(WorkoutSession session)
        {
            try
            {
                if (session == null)
                {
                    return new Dictionary<string, decimal>();
                }

                var volumeByExercise = new Dictionary<string, decimal>();

                foreach (var exercise in session.WorkoutExercises)
                {
                    if (exercise.ExerciseType == null) continue;

                    var exerciseName = exercise.ExerciseType.Name ?? "Unknown Exercise";
                    var volume = CalculateExerciseVolume(exercise);

                    if (volumeByExercise.ContainsKey(exerciseName))
                    {
                        volumeByExercise[exerciseName] += volume;
                    }
                    else
                    {
                        volumeByExercise[exerciseName] = volume;
                    }
                }

                return volumeByExercise;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating volume by exercise for session {Id}", session.WorkoutSessionId);
                throw;
            }
        }

        public double CalculateSetVolume(WorkoutSet set)
        {
            try
            {
                if (set == null || !set.Weight.HasValue || !set.Reps.HasValue)
                {
                    return 0;
                }

                return (double)(set.Weight.Value * set.Reps.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating volume for set {Id}", set.WorkoutSetId);
                throw;
            }
        }
    }
}