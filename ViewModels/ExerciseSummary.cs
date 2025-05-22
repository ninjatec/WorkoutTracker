using System;
using System.Collections.Generic;

namespace WorkoutTrackerWeb.ViewModels
{
    /// <summary>
    /// Summary view model for displaying exercise information in session details
    /// </summary>
    public class ExerciseSummary
    {
        public string ExerciseName { get; set; }
        public int TotalSets { get; set; }
        public decimal MaxWeight { get; set; }
        public int TotalReps { get; set; }
        public decimal VolumeLifted { get; set; }
    }
}