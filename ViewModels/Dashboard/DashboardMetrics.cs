using System;
using System.Collections.Generic;

namespace WorkoutTrackerWeb.ViewModels.Dashboard
{
    /// <summary>
    /// View model for dashboard metrics
    /// </summary>
    public class DashboardMetrics
    {
        public int TotalWorkouts { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalCalories { get; set; }
        public TimeSpan AverageDuration { get; set; }
        public Dictionary<string, decimal> VolumeByExercise { get; set; } = new();
        public Dictionary<DateTime, int> WorkoutFrequency { get; set; } = new();
        public List<PersonalBest> PersonalBests { get; set; } = new();
    }
}
