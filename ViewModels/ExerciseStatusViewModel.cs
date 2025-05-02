using System;

namespace WorkoutTrackerWeb.ViewModels
{
    public class ExerciseStatusViewModel
    {
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; }
        public int SuccessReps { get; set; }
        public int FailedReps { get; set; }
        public int TotalReps => SuccessReps + FailedReps;
        public decimal SuccessRate => TotalReps > 0 ? (decimal)SuccessReps / TotalReps * 100 : 0;
        public DateTime LastPerformed { get; set; }
        
        // Property to support Reports view
        public int SuccessfulReps => SuccessReps;
    }
}