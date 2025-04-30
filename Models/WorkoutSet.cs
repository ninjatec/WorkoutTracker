using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models
{
    /// <summary>
    /// Represents a set of an exercise in a workout session
    /// </summary>
    public class WorkoutSet
    {
        public int WorkoutSetId { get; set; }
        
        // Foreign key for WorkoutExercise
        public int WorkoutExerciseId { get; set; }
        
        // Foreign key for Settype (optional)
        public int? SettypeId { get; set; }
        
        [Display(Name = "Sequence Number")]
        public int SequenceNum { get; set; }
        
        [Display(Name = "Set Number")]
        public int SetNumber { get; set; }
        
        [Display(Name = "Reps")]
        public int? Reps { get; set; }
        
        [Display(Name = "Min Reps Target")]
        public int? TargetMinReps { get; set; }
        
        [Display(Name = "Max Reps Target")]
        public int? TargetMaxReps { get; set; }
        
        [Display(Name = "Weight (kg)")]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal? Weight { get; set; }
        
        [Display(Name = "Duration (seconds)")]
        public int? DurationSeconds { get; set; }
        
        [Display(Name = "Distance (meters)")]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal? DistanceMeters { get; set; }
        
        [Display(Name = "RPE (Rate of Perceived Exertion)")]
        public int? RPE { get; set; }
        
        [Display(Name = "Rest Period (seconds)")]
        public int? RestSeconds { get; set; }
        
        [Display(Name = "Completed")]
        public bool IsCompleted { get; set; }
        
        [StringLength(100)]
        [Display(Name = "Notes")]
        public string Notes { get; set; }
        
        [Display(Name = "Timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        // Navigation properties
        [ForeignKey("WorkoutExerciseId")]
        public WorkoutExercise WorkoutExercise { get; set; }
        
        [ForeignKey("SettypeId")]
        public Settype Settype { get; set; }
    }
}