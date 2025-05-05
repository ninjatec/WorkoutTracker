using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Services.Database
{
    /// <summary>
    /// Service providing pre-compiled database queries for frequently executed operations
    /// to improve performance by avoiding query recompilation
    /// </summary>
    public static class CompiledQueries
    {
        // Workout Session Queries
        
        /// <summary>
        /// Compiled query to get a workout session with all related exercises and sets
        /// </summary>
        public static readonly Func<WorkoutTrackerWebContext, int, Task<WorkoutSession>> GetWorkoutSessionWithDetailsAsync =
            EF.CompileAsyncQuery((WorkoutTrackerWebContext context, int workoutSessionId) =>
                context.WorkoutSessions
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.ExerciseType)
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.WorkoutSets)
                            .ThenInclude(s => s.Settype)
                    .FirstOrDefault(ws => ws.WorkoutSessionId == workoutSessionId));
                
        /// <summary>
        /// Compiled query to get recent workout sessions for a user
        /// </summary>
        public static readonly Func<WorkoutTrackerWebContext, int, int, Task<List<WorkoutSession>>> GetRecentWorkoutSessionsAsync =
            EF.CompileAsyncQuery((WorkoutTrackerWebContext context, int userId, int count) =>
                context.WorkoutSessions
                    .Where(ws => ws.UserId == userId)
                    .OrderByDescending(ws => ws.StartDateTime)
                    .Take(count)
                    .ToList());
                
        /// <summary>
        /// Compiled query to get workout volume totals by date range
        /// </summary>
        public static readonly Func<WorkoutTrackerWebContext, int, DateTime, DateTime, Task<List<VolumeByDate>>> GetVolumeByDateRangeAsync =
            EF.CompileAsyncQuery((WorkoutTrackerWebContext context, int userId, DateTime startDate, DateTime endDate) =>
                context.WorkoutSessions
                    .Where(ws => ws.UserId == userId && 
                                ws.CompletedDate >= startDate && 
                                ws.CompletedDate <= endDate)
                    .GroupBy(ws => ws.CompletedDate)
                    .Select(g => new VolumeByDate
                    {
                        Date = g.Key ?? DateTime.MinValue,
                        TotalVolume = g.SelectMany(s => s.WorkoutExercises)
                                     .SelectMany(e => e.WorkoutSets)
                                     .Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0))
                    })
                    .ToList());
        
        // Exercise Type Queries
        
        /// <summary>
        /// Compiled query to get popular exercise types for a user
        /// </summary>
        public static readonly Func<WorkoutTrackerWebContext, int, int, Task<List<ExerciseTypeUsage>>> GetPopularExerciseTypesAsync =
            EF.CompileAsyncQuery((WorkoutTrackerWebContext context, int userId, int count) =>
                context.WorkoutExercises
                    .Where(we => we.WorkoutSession.UserId == userId)
                    .GroupBy(we => new { we.ExerciseTypeId, Name = we.ExerciseType.Name })
                    .OrderByDescending(g => g.Count())
                    .Take(count)
                    .Select(g => new ExerciseTypeUsage
                    {
                        ExerciseTypeId = g.Key.ExerciseTypeId,
                        ExerciseName = g.Key.Name,
                        UsageCount = g.Count()
                    })
                    .ToList());
        
        /// <summary>
        /// Compiled query to get weight progression for an exercise
        /// </summary>
        public static readonly Func<WorkoutTrackerWebContext, int, int, DateTime, Task<List<WeightProgressByDate>>> GetWeightProgressionAsync =
            EF.CompileAsyncQuery((WorkoutTrackerWebContext context, int userId, int exerciseTypeId, DateTime startDate) =>
                context.WorkoutSets
                    .Where(s => s.WorkoutExercise.WorkoutSession.UserId == userId &&
                             s.WorkoutExercise.ExerciseTypeId == exerciseTypeId &&
                             s.WorkoutExercise.WorkoutSession.CompletedDate >= startDate)
                    .GroupBy(s => new
                    {
                        Date = (s.WorkoutExercise.WorkoutSession.CompletedDate ?? DateTime.MinValue).Date
                    })
                    .Select(g => new WeightProgressByDate
                    {
                        Date = g.Key.Date,
                        MaxWeight = g.Max(s => s.Weight ?? 0)
                    })
                    .ToList());
    }

    // Value objects used by compiled queries
    public class VolumeByDate
    {
        public DateTime Date { get; set; }
        public decimal TotalVolume { get; set; }
    }

    public class ExerciseTypeUsage
    {
        public int ExerciseTypeId { get; set; }
        public string ExerciseName { get; set; }
        public int UsageCount { get; set; }
    }

    public class WeightProgressByDate
    {
        public DateTime Date { get; set; }
        public decimal MaxWeight { get; set; }
    }
}