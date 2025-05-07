using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Services.Progress
{
    public interface IProgressDashboardService
    {
        /// <summary>
        /// Gets volume metrics series data (total weight, reps, or time) for a user within a date range
        /// </summary>
        /// <param name="userId">User ID to get metrics for</param>
        /// <param name="startDate">Start date for metrics range</param>
        /// <param name="endDate">End date for metrics range</param>
        /// <returns>List of workout metrics for volume</returns>
        Task<IEnumerable<WorkoutMetric>> GetVolumeSeriesAsync(int userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets intensity metrics series data (average weight per rep) for a user within a date range
        /// </summary>
        /// <param name="userId">User ID to get metrics for</param>
        /// <param name="startDate">Start date for metrics range</param>
        /// <param name="endDate">End date for metrics range</param>
        /// <returns>List of workout metrics for intensity</returns>
        Task<IEnumerable<WorkoutMetric>> GetIntensitySeriesAsync(int userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets consistency metrics series data (workouts per week) for a user within a date range
        /// </summary>
        /// <param name="userId">User ID to get metrics for</param>
        /// <param name="startDate">Start date for metrics range</param>
        /// <param name="endDate">End date for metrics range</param>
        /// <returns>List of workout metrics for consistency</returns>
        Task<IEnumerable<WorkoutMetric>> GetConsistencySeriesAsync(int userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Calculate and store metrics for all completed workouts without metrics
        /// </summary>
        /// <returns>Number of sessions processed</returns>
        Task<int> CalculateAndStoreMissingMetricsAsync();
        
        /// <summary>
        /// Calculate and store metrics for a specific workout session
        /// </summary>
        /// <param name="workoutSessionId">ID of the workout session</param>
        /// <returns>True if metrics were calculated and stored successfully</returns>
        Task<bool> CalculateAndStoreMetricsForSessionAsync(int workoutSessionId);
    }
}