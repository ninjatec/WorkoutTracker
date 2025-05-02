using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkoutTrackerWeb.ViewModels
{
    public class ExerciseWeightProgress
    {
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; }
        public List<WeightProgressPoint> ProgressPoints { get; set; } = new List<WeightProgressPoint>();
        public decimal MaxWeight { get; set; }
        public decimal MinWeight { get; set; }
        
        // Properties to support Reports view
        public List<DateTime> Dates => ProgressPoints?.Select(p => p.Date).ToList() ?? new List<DateTime>();
        public List<decimal> Weights => ProgressPoints?.Select(p => p.Weight).ToList() ?? new List<decimal>();
    }

    public class WeightProgressPoint
    {
        public DateTime Date { get; set; }
        public decimal Weight { get; set; }
        public int Reps { get; set; }
        public int WorkoutSessionId { get; set; }
    }
}