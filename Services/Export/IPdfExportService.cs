using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkoutTrackerWeb.ViewModels.Dashboard;

namespace WorkoutTrackerWeb.Services.Export
{
    /// <summary>
    /// Service interface for PDF exports
    /// </summary>
    public interface IPdfExportService
    {
        /// <summary>
        /// Generates a PDF export of dashboard data
        /// </summary>
        /// <param name="userName">The user's username</param>
        /// <param name="startDate">Start date for the report period</param>
        /// <param name="endDate">End date for the report period</param>
        /// <param name="metrics">Dashboard metrics</param>
        /// <param name="volumeProgress">Volume progress data</param>
        /// <param name="workoutFrequency">Workout frequency data</param>
        /// <param name="personalBests">Personal bests records</param>
        /// <returns>PDF document as byte array</returns>
        Task<byte[]> GenerateDashboardPdfAsync(
            string userName,
            DateTime startDate,
            DateTime endDate,
            DashboardMetrics metrics,
            List<ChartData> volumeProgress,
            List<ChartData> workoutFrequency,
            List<PersonalBest> personalBests);
    }
}
