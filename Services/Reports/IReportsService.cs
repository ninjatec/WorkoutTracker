using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.ViewModels;

namespace WorkoutTrackerWeb.Services.Reports
{
    /// <summary>
    /// Service interface for retrieving workout report data
    /// </summary>
    public interface IReportsService
    {
        /// <summary>
        /// Gets workout reports data for a specific user
        /// </summary>
        /// <param name="userId">The user ID to get report data for</param>
        /// <param name="period">The time period in days to include in the report</param>
        /// <param name="pageNumber">The page number for personal records pagination</param>
        /// <returns>Report data for display</returns>
        Task<ReportsData> GetReportsDataAsync(int userId, int period = 90, int pageNumber = 1);

        /// <summary>
        /// Gets distribution of exercises by muscle group
        /// </summary>
        Task<Dictionary<string, int>> GetExerciseDistributionByMuscleGroupAsync(int userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets distribution of exercises by exercise type
        /// </summary>
        Task<Dictionary<string, int>> GetExerciseDistributionByTypeAsync(int userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets frequency of exercises performed by a user
        /// </summary>
        Task<Dictionary<string, int>> GetExerciseFrequencyAsync(int userId, DateTime startDate, DateTime endDate, int limit = 10);
        
        /// <summary>
        /// Gets recent workout sessions for a user
        /// </summary>
        Task<List<WorkoutSession>> GetRecentWorkoutSessionsAsync(int userId, int count = 10);
        
        /// <summary>
        /// Gets workout volume over time
        /// </summary>
        Task<Dictionary<DateTime, decimal>> GetVolumeOverTimeAsync(int userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets workout volume by muscle group
        /// </summary>
        Task<Dictionary<string, decimal>> GetVolumeByMuscleGroupAsync(int userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets progress for a specific exercise
        /// </summary>
        Task<Dictionary<string, decimal>> GetProgressForExerciseAsync(int userId, int exerciseTypeId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets workouts by day of week
        /// </summary>
        Task<Dictionary<string, int>> GetWorkoutsByDayOfWeekAsync(int userId, DateTime startDate, DateTime endDate);
        
        /// <summary>
        /// Gets workouts by hour of day
        /// </summary>
        Task<Dictionary<int, int>> GetWorkoutsByHourAsync(int userId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets paginated personal records for a user
        /// </summary>
        Task<PagedResult<PersonalRecord>> GetPersonalRecordsAsync(int userId, int page, int pageSize);

        /// <summary>
        /// Gets weight progress for exercises with optimized performance for large datasets
        /// </summary>
        Task<List<ExerciseWeightProgress>> GetWeightProgressAsync(int userId, int days, int limit = 5);

        /// <summary>
        /// Gets exercise status for a user with optimized performance for large datasets
        /// </summary>
        Task<ExerciseStatusResult> GetExerciseStatusAsync(int userId, int days, int limit = 10);

        /// <summary>
        /// Gets all exercise types
        /// </summary>
        Task<List<string>> GetExerciseTypesAsync();

        /// <summary>
        /// Gets key user metrics for dashboard with optimized performance
        /// </summary>
        Task<object> GetUserMetricsAsync(int userId, int days);
    }

    /// <summary>
    /// Container for workout report data
    /// </summary>
    public class ReportsData
    {
        public int TotalSessions { get; set; }
        public int TotalSets { get; set; }
        public int TotalReps { get; set; }
        public int SuccessReps { get; set; }
        public int FailedReps { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        
        public List<PersonalRecord> PersonalRecords { get; set; } = new List<PersonalRecord>();
        public List<ExerciseStatus> ExerciseStatusList { get; set; } = new List<ExerciseStatus>();
        public List<WeightProgress> WeightProgressList { get; set; } = new List<WeightProgress>();
        public List<RecentExerciseStatus> RecentExerciseStatusList { get; set; } = new List<RecentExerciseStatus>();
    }

    /// <summary>
    /// Container for paginated results
    /// </summary>
    public class PagedResult<T>
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public List<T> Records { get; set; } = new List<T>();
    }

    /// <summary>
    /// Container for exercise status results
    /// </summary>
    public class ExerciseStatusResult
    {
        public List<ExerciseStatusViewModel> AllExercises { get; set; } = new List<ExerciseStatusViewModel>();
        public List<ExerciseStatusViewModel> TopExercises { get; set; } = new List<ExerciseStatusViewModel>();
        public int TotalSuccess { get; set; }
        public int TotalFailed { get; set; }
    }
}