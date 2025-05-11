using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.ViewModels.Dashboard;
using WorkoutTrackerWeb.Services.Calculations;

namespace WorkoutTrackerWeb.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _repository;
        private readonly IVolumeCalculationService _volumeService;
        private readonly ICalorieCalculationService _calorieService;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IDashboardRepository repository,
            IVolumeCalculationService volumeService,
            ICalorieCalculationService calorieService,
            ILogger<DashboardService> logger)
        {
            _repository = repository;
            _volumeService = volumeService;
            _calorieService = calorieService;
            _logger = logger;
        }

        public async Task<DashboardMetrics> GetDashboardMetricsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var sessions = await _repository.GetUserSessionsAsync(userId, startDate, endDate);
                var volumeByExercise = await _repository.GetVolumeByExerciseTypeAsync(userId, startDate, endDate);
                var workoutFrequency = await _repository.GetWorkoutCountByDateAsync(userId, startDate, endDate);
                var personalBestsByExercise = await _repository.GetPersonalBestsAsync(userId, startDate, endDate);

                var metrics = new DashboardMetrics
                {
                    TotalWorkouts = sessions.Count(),
                    TotalVolume = sessions.Sum(s => _volumeService.CalculateWorkoutSessionVolume(s)),
                    TotalCalories = sessions.Sum(s => _calorieService.CalculateCalories(s)),
                    AverageDuration = TimeSpan.FromMinutes(sessions.Average(s => s.Duration)),
                    VolumeByExercise = volumeByExercise,
                    WorkoutFrequency = workoutFrequency,
                    PersonalBests = personalBestsByExercise.Select(kvp => new PersonalBest
                    {
                        ExerciseName = kvp.Key,
                        Weight = kvp.Value.Max(s => s.Weight ?? 0),
                        Reps = kvp.Value.First(s => (s.Weight ?? 0) == kvp.Value.Max(x => x.Weight ?? 0)).Reps ?? 0,
                        AchievedDate = kvp.Value.First().WorkoutExercise?.WorkoutSession?.StartDateTime ?? DateTime.Now,
                        EstimatedOneRM = CalculateEstimatedOneRM(
                            kvp.Value.Max(s => s.Weight ?? 0),
                            kvp.Value.First(s => (s.Weight ?? 0) == kvp.Value.Max(x => x.Weight ?? 0)).Reps ?? 0)
                    }).ToList()
                };

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard metrics for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<ChartData>> GetVolumeProgressChartDataAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var sessions = await _repository.GetUserSessionsAsync(userId, startDate, endDate);
                
                return sessions
                    .GroupBy(s => s.StartDateTime.Date)
                    .Select(g => new ChartData
                    {
                        Date = g.Key,
                        Value = g.Sum(s => _volumeService.CalculateWorkoutSessionVolume(s)),
                        Label = "Volume"
                    })
                    .OrderBy(d => d.Date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting volume progress data for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<ChartData>> GetWorkoutFrequencyChartDataAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var workoutCounts = await _repository.GetWorkoutCountByDateAsync(userId, startDate, endDate);
                
                return workoutCounts
                    .Select(kvp => new ChartData
                    {
                        Date = kvp.Key,
                        Value = kvp.Value,
                        Label = "Workouts"
                    })
                    .OrderBy(d => d.Date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting workout frequency data for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<PersonalBest>> GetPersonalBestsAsync(int userId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var personalBestsByExercise = await _repository.GetPersonalBestsAsync(userId, startDate, endDate);
                
                return personalBestsByExercise.Select(kvp => new PersonalBest
                {
                    ExerciseName = kvp.Key,
                    Weight = kvp.Value.Max(s => s.Weight ?? 0),
                    Reps = kvp.Value.First(s => (s.Weight ?? 0) == kvp.Value.Max(x => x.Weight ?? 0)).Reps ?? 0,
                    AchievedDate = kvp.Value.First().WorkoutExercise?.WorkoutSession?.StartDateTime ?? DateTime.Now,
                    EstimatedOneRM = CalculateEstimatedOneRM(
                        kvp.Value.Max(s => s.Weight ?? 0),
                        kvp.Value.First(s => (s.Weight ?? 0) == kvp.Value.Max(x => x.Weight ?? 0)).Reps ?? 0)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personal bests for user {UserId}", userId);
                throw;
            }
        }

        private decimal CalculateEstimatedOneRM(decimal weight, int reps)
        {
            // Brzycki Formula
            return weight * (decimal)(1 + (0.033 * reps));
        }
    }
}
