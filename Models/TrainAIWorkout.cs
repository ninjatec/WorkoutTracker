using System;
using System.Collections.Generic;

namespace WorkoutTrackerWeb.Models
{
    public class TrainAIWorkout
    {
        public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalDuration { get; set; }
        public int TotalSets { get; set; }
        public int BurnedCalories { get; set; }
        public decimal TotalTVL { get; set; }
        public List<TrainAISet> Sets { get; set; } = new();
    }

    public class TrainAISet
    {
        public string Exercise { get; set; }
        public int Reps { get; set; }
        public decimal Weight { get; set; }
        public int RestTime { get; set; }
    }
}