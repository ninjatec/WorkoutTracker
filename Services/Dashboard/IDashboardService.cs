using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkoutTrackerWeb.ViewModels.Dashboard;

namespace WorkoutTrackerWeb.Services.Dashboard
{
    /// <summary>
    /// Service interface for dashboard operations
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Gets all dashboard metrics for a user within a date range
        /// </summary>
        Task<DashboardMetrics> GetDashboardMetricsAsync(int userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets volume progress data for charting
        /// </summary>
        Task<IEnumerable<ChartData>> GetVolumeProgressChartDataAsync(int userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets workout frequency data for charting
        /// </summary>
        Task<IEnumerable<ChartData>> GetWorkoutFrequencyChartDataAsync(int userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets personal best records for display
        /// </summary>
        Task<IEnumerable<PersonalBest>> GetPersonalBestsAsync(int userId, DateTime startDate, DateTime endDate);
    }
}
