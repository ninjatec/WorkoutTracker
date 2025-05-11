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

                var sessionsList = sessions.ToList(); // Materialize the query once

                var metrics = new DashboardMetrics
                {
                    TotalWorkouts = sessionsList.Count,
                    TotalVolume = sessionsList.Any() ? sessionsList.Sum(s => _volumeService.CalculateWorkoutSessionVolume(s)) : 0,
                    TotalCalories = sessionsList.Any() ? sessionsList.Sum(s => _calorieService.CalculateCalories(s)) : 0,
                    AverageDuration = sessionsList.Any() ? TimeSpan.FromMinutes(sessionsList.Average(s => s.Duration)) : TimeSpan.Zero,
                    VolumeByExercise = volumeByExercise ?? new Dictionary<string, decimal>(),
                    WorkoutFrequency = workoutFrequency ?? new Dictionary<DateTime, int>(),
                    PersonalBests = (await GetPersonalBestsAsync(userId, startDate, endDate)).ToList()
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
                
                if (!sessions.Any())
                {
                    return Enumerable.Empty<ChartData>();
                }

                return sessions
                    .OrderBy(s => s.StartDateTime)
                    .Select(s => new ChartData
                    {
                        Label = s.StartDateTime.ToString("MM/dd"),
                        Value = _volumeService.CalculateWorkoutSessionVolume(s)
                    });
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
                var frequency = await _repository.GetWorkoutCountByDateAsync(userId, startDate, endDate);
                
                if (frequency == null || !frequency.Any())
                {
                    return Enumerable.Empty<ChartData>();
                }

                return frequency
                    .OrderBy(kvp => kvp.Key)
                    .Select(kvp => new ChartData
                    {
                        Label = kvp.Key.ToString("MM/dd"),
                        Value = kvp.Value
                    });
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
                var personalBests = await _repository.GetPersonalBestsAsync(userId, startDate, endDate);
                return PopulatePersonalBests(personalBests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personal bests for user {UserId}", userId);
                throw;
            }
        }

        private IEnumerable<PersonalBest> PopulatePersonalBests(Dictionary<string, List<WorkoutSet>> personalBestsByExercise)
        {
            if (personalBestsByExercise == null || !personalBestsByExercise.Any())
            {
                return Enumerable.Empty<PersonalBest>();
            }

            return personalBestsByExercise.Select(kvp =>
            {
                var bestSet = kvp.Value
                    .OrderByDescending(s => s.Weight * s.Reps)
                    .FirstOrDefault();

                if (bestSet == null)
                {
                    return null;
                }

                if (!bestSet.Weight.HasValue || !bestSet.Reps.HasValue) 
                    return null;

                return new PersonalBest
                {
                    ExerciseName = kvp.Key,
                    Weight = bestSet.Weight.Value,
                    Reps = bestSet.Reps.Value,
                    AchievedDate = bestSet.WorkoutExercise?.WorkoutSession?.StartDateTime ?? DateTime.UtcNow,
                    EstimatedOneRM = CalculateOneRepMax(bestSet.Weight.Value, bestSet.Reps.Value)
                };
            }).Where(pb => pb != null);
        }

        private static decimal CalculateOneRepMax(decimal weight, int reps)
        {
            if (reps == 0) return 0;
            // Using the Brzycki formula: 1RM = weight × (36 / (37 - reps))
            return weight * 36m / (37m - reps);
        }
    }
}
