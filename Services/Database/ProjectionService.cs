using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.ViewModels;

namespace WorkoutTrackerWeb.Services.Database
{
    /// <summary>
    /// Service for efficient data projection to minimize data transfer
    /// </summary>
    public class ProjectionService
    {
        private readonly WorkoutTrackerWebContext _context;

        public ProjectionService(WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Projects workout sessions to lightweight view models containing only necessary data
        /// </summary>
        public IQueryable<WorkoutSessionSummaryViewModel> ProjectToWorkoutSessionSummary(IQueryable<WorkoutSession> query)
        {
            return query.Select(ws => new WorkoutSessionSummaryViewModel
            {
                WorkoutSessionId = ws.WorkoutSessionId,
                Name = ws.Name ?? "Unnamed Session",
                UserId = ws.UserId,
                StartDateTime = ws.StartDateTime,
                Duration = ws.Duration,
                CaloriesBurned = ws.CaloriesBurned,
                IsCompleted = ws.IsCompleted,
                ExerciseCount = ws.WorkoutExercises.Count,
                TotalSets = ws.WorkoutExercises.Sum(we => we.WorkoutSets.Count)
            });
        }

        /// <summary>
        /// Projects workout exercises to lightweight view models containing only necessary data
        /// </summary>
        public IQueryable<ExerciseSummaryViewModel> ProjectToExerciseSummary(IQueryable<WorkoutExercise> query)
        {
            return query.Select(we => new ExerciseSummaryViewModel
            {
                WorkoutExerciseId = we.WorkoutExerciseId,
                ExerciseName = we.ExerciseType.Name ?? "Unknown Exercise",
                ExerciseTypeId = we.ExerciseTypeId,
                SequenceNum = we.SequenceNum,
                SetCount = we.WorkoutSets.Count,
                TotalWeight = we.WorkoutSets.Sum(s => s.Weight ?? 0),
                TotalReps = we.WorkoutSets.Sum(s => s.Reps ?? 0)
            });
        }

        /// <summary>
        /// Efficiently gets exercise progress data for a specific exercise type
        /// </summary>
        public async Task<List<WeightProgressData>> GetExerciseProgressDataAsync(
            int userId, int exerciseTypeId, DateTime startDate, int maxPoints = 50)
        {
            return await _context.WorkoutSets
                .Where(s => s.WorkoutExercise.ExerciseTypeId == exerciseTypeId &&
                           s.WorkoutExercise.WorkoutSession.UserId == userId &&
                           s.WorkoutExercise.WorkoutSession.StartDateTime >= startDate &&
                           s.Weight > 0)
                .GroupBy(s => new { Date = s.WorkoutExercise.WorkoutSession.StartDateTime.Date })
                .OrderBy(g => g.Key.Date)
                .Select(g => new WeightProgressData
                {
                    Date = g.Key.Date,
                    MaxWeight = g.Max(s => s.Weight ?? 0),
                    MaxReps = g.Where(s => s.Weight == g.Max(s2 => s2.Weight ?? 0))
                               .Max(s => s.Reps ?? 0)
                })
                .Take(maxPoints)
                .ToListAsync();
        }
        
        /// <summary>
        /// Gets summary data for a user's recent workout sessions with minimal data transfer
        /// </summary>
        public async Task<List<WorkoutSessionSummaryViewModel>> GetRecentWorkoutSummariesAsync(
            int userId, DateTime startDate, int count = 10)
        {
            return await _context.WorkoutSessions
                .Where(ws => ws.UserId == userId && ws.StartDateTime >= startDate)
                .OrderByDescending(ws => ws.StartDateTime)
                .Take(count)
                .Select(ws => new WorkoutSessionSummaryViewModel
                {
                    WorkoutSessionId = ws.WorkoutSessionId,
                    Name = ws.Name ?? "Unnamed Session",
                    StartDateTime = ws.StartDateTime,
                    Duration = ws.Duration,
                    CaloriesBurned = ws.CaloriesBurned,
                    IsCompleted = ws.IsCompleted,
                    ExerciseCount = ws.WorkoutExercises.Count,
                    TotalSets = ws.WorkoutExercises.Sum(we => we.WorkoutSets.Count),
                    TotalVolume = ws.WorkoutExercises
                                   .SelectMany(we => we.WorkoutSets)
                                   .Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0))
                })
                .ToListAsync();
        }
        
        /// <summary>
        /// Gets batch exercise performance metrics for efficient rendering
        /// </summary>
        public async Task<Dictionary<int, ExercisePerformanceMetrics>> GetExercisePerformanceMetricsAsync(
            int userId, List<int> exerciseIds, DateTime startDate)
        {
            var metrics = await _context.WorkoutSets
                .Where(s => s.WorkoutExercise.WorkoutSession.UserId == userId &&
                           exerciseIds.Contains(s.WorkoutExercise.ExerciseTypeId) &&
                           s.WorkoutExercise.WorkoutSession.StartDateTime >= startDate)
                .GroupBy(s => s.WorkoutExercise.ExerciseTypeId)
                .Select(g => new
                {
                    ExerciseTypeId = g.Key,
                    MaxWeight = g.Max(s => s.Weight ?? 0),
                    AvgWeight = g.Average(s => s.Weight ?? 0),
                    TotalSets = g.Count(),
                    CompletedSets = g.Count(s => s.IsCompleted),
                    TotalVolume = g.Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0)),
                    LastUsed = g.Max(s => s.WorkoutExercise.WorkoutSession.StartDateTime)
                })
                .ToListAsync();

            return metrics.ToDictionary(
                m => m.ExerciseTypeId,
                m => new ExercisePerformanceMetrics
                {
                    MaxWeight = m.MaxWeight,
                    AvgWeight = m.AvgWeight,
                    TotalSets = m.TotalSets,
                    CompletionRate = m.TotalSets > 0 ? (decimal)m.CompletedSets / m.TotalSets : 0,
                    TotalVolume = m.TotalVolume,
                    LastUsed = m.LastUsed
                }
            );
        }
    }

    // Data transfer objects for the projection service
    public class WorkoutSessionSummaryViewModel
    {
        public int WorkoutSessionId { get; set; }
        public string Name { get; set; }
        public int UserId { get; set; }
        public DateTime StartDateTime { get; set; }
        public int Duration { get; set; }
        public decimal? CaloriesBurned { get; set; }
        public bool IsCompleted { get; set; }
        public int ExerciseCount { get; set; }
        public int TotalSets { get; set; }
        public decimal TotalVolume { get; set; }
    }

    public class ExerciseSummaryViewModel
    {
        public int WorkoutExerciseId { get; set; }
        public string ExerciseName { get; set; }
        public int ExerciseTypeId { get; set; }
        public int SequenceNum { get; set; }
        public int SetCount { get; set; }
        public decimal TotalWeight { get; set; }
        public int TotalReps { get; set; }
    }

    public class WeightProgressData
    {
        public DateTime Date { get; set; }
        public decimal MaxWeight { get; set; }
        public int MaxReps { get; set; }
    }

    public class ExercisePerformanceMetrics
    {
        public decimal MaxWeight { get; set; }
        public decimal AvgWeight { get; set; }
        public int TotalSets { get; set; }
        public decimal CompletionRate { get; set; }
        public decimal TotalVolume { get; set; }
        public DateTime LastUsed { get; set; }
    }
}