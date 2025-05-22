using System;

namespace WorkoutTrackerWeb.ViewModels.Dashboard
{
    /// <summary>
    /// View model for personal best records
    /// </summary>
    public class PersonalBest
    {
        public string ExerciseName { get; set; }
        public decimal Weight { get; set; }
        public int Reps { get; set; }
        public DateTime AchievedDate { get; set; }
        public decimal EstimatedOneRM { get; set; }
    }
}
