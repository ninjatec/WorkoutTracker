using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Services.Dashboard
{
    /// <summary>
    /// Repository interface for dashboard-specific data access
    /// </summary>
    public interface IDashboardRepository
    {
        /// <summary>
        /// Gets all workout sessions for a user within a date range
        /// </summary>
        Task<IEnumerable<WorkoutSession>> GetUserSessionsAsync(int userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets the total volume lifted by exercise type for a user within a date range
        /// </summary>
        Task<Dictionary<string, decimal>> GetVolumeByExerciseTypeAsync(int userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets the count of workouts by date for a user within a date range
        /// </summary>
        Task<Dictionary<DateTime, int>> GetWorkoutCountByDateAsync(int userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets personal best records for each exercise type within a date range
        /// </summary>
        Task<Dictionary<string, List<WorkoutSet>>> GetPersonalBestsAsync(int userId, DateTime startDate, DateTime endDate);
    }
}
