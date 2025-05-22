using System;

namespace WorkoutTrackerWeb.Areas.Coach.ViewModels
{
    public class WorkoutStatsViewModel
    {
        public int TotalSessions { get; set; }
        public int CompletedSessions { get; set; }
        public decimal AverageDuration { get; set; }
        public int TotalExercises { get; set; }
    }
}