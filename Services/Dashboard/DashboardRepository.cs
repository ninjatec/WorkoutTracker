using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Services.Dashboard
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<DashboardRepository> _logger;

        public DashboardRepository(WorkoutTrackerWebContext context, ILogger<DashboardRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<WorkoutSession>> GetUserSessionsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.WorkoutSessions
                    .Include(s => s.WorkoutExercises)
                        .ThenInclude(e => e.WorkoutSets)
                    .Include(s => s.WorkoutExercises)
                        .ThenInclude(e => e.ExerciseType)
                    .Where(s => s.UserId == userId &&
                               s.StartDateTime >= startDate &&
                               s.StartDateTime <= endDate &&
                               s.IsCompleted)
                    .OrderBy(s => s.StartDateTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user sessions for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Dictionary<string, decimal>> GetVolumeByExerciseTypeAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var exercises = await _context.WorkoutExercises
                    .Include(e => e.WorkoutSets)
                    .Include(e => e.ExerciseType)
                    .Include(e => e.WorkoutSession)
                    .Where(e => e.WorkoutSession.UserId == userId &&
                               e.WorkoutSession.StartDateTime >= startDate &&
                               e.WorkoutSession.StartDateTime <= endDate &&
                               e.WorkoutSession.IsCompleted &&
                               e.ExerciseType != null)
                    .ToListAsync();

                return exercises
                    .GroupBy(e => e.ExerciseType.Name)
                    .ToDictionary(
                        g => g.Key ?? "Unknown Exercise",
                        g => g.SelectMany(e => e.WorkoutSets)
                            .Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0))
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving volume by exercise type for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Dictionary<DateTime, int>> GetWorkoutCountByDateAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var workouts = await _context.WorkoutSessions
                    .Where(s => s.UserId == userId &&
                               s.StartDateTime >= startDate &&
                               s.StartDateTime <= endDate &&
                               s.IsCompleted)
                    .ToListAsync();

                return workouts
                    .GroupBy(s => s.StartDateTime.Date)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Count()
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workout count by date for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Dictionary<string, List<WorkoutSet>>> GetPersonalBestsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var sets = await _context.WorkoutSets
                    .Include(s => s.WorkoutExercise)
                        .ThenInclude(e => e.ExerciseType)
                    .Include(s => s.WorkoutExercise)
                        .ThenInclude(e => e.WorkoutSession)
                    .Where(s => s.WorkoutExercise.WorkoutSession.UserId == userId &&
                               s.WorkoutExercise.WorkoutSession.StartDateTime >= startDate &&
                               s.WorkoutExercise.WorkoutSession.StartDateTime <= endDate &&
                               s.WorkoutExercise.WorkoutSession.IsCompleted &&
                               s.WorkoutExercise.ExerciseType != null &&
                               s.Weight > 0)
                    .ToListAsync();

                return sets
                    .GroupBy(s => s.WorkoutExercise.ExerciseType.Name)
                    .ToDictionary(
                        g => g.Key ?? "Unknown Exercise",
                        g => g.OrderByDescending(s => s.Weight)
                            .ThenByDescending(s => s.Reps)
                            .ToList()
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving personal bests for user {UserId}", userId);
                throw;
            }
        }
    }
}
