using System;

namespace WorkoutTrackerWeb.Models
{
    public class PersonalRecord
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; }
        public decimal Weight { get; set; }
        public int Reps { get; set; }
        public DateTime Date { get; set; }
        public int WorkoutSessionId { get; set; }
        public string WorkoutSessionName { get; set; }
        
        // Properties to support Reports view
        public decimal MaxWeight => Weight;
        public DateTime RecordDate => Date;
    }
}