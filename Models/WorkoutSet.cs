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
        
        [Range(0, int.MaxValue, ErrorMessage = "Reps must be greater than 0")]
        [Display(Name = "Reps")]
        public int? Reps { get; set; }
        
        [Display(Name = "Min Reps Target")]
        public int? TargetMinReps { get; set; }
        
        [Display(Name = "Max Reps Target")]
        public int? TargetMaxReps { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Weight must be greater than 0")]
        [Display(Name = "Weight (kg)")]
        public decimal? Weight { get; set; }
        
        [Display(Name = "Duration (seconds)")]
        public int? DurationSeconds { get; set; }
        
        [Display(Name = "Distance (meters)")]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal? Distance { get; set; }
        
        [Display(Name = "Intensity")]
        [Range(1, 10)]
        public int? Intensity { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "RPE (Rate of Perceived Exertion)")]
        public decimal? RPE { get; set; }
        
        [Display(Name = "Rest Period (seconds)")]
        public int? RestSeconds { get; set; }
        
        public TimeSpan? RestTime { get; set; }
        
        [Display(Name = "Completed")]
        public bool IsCompleted { get; set; }
        
        [StringLength(100)]
        [Display(Name = "Notes")]
        public string Notes { get; set; } = "";
        
        [StringLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";
        
        [Display(Name = "Timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public bool IsWarmup { get; set; }
        
        public int? TargetReps { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TargetWeight { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? EstimatedOneRM { get; set; }
        
        // Navigation properties
        [ForeignKey("WorkoutExerciseId")]
        public virtual WorkoutExercise WorkoutExercise { get; set; }
        
        [ForeignKey("SettypeId")]
        public Settype Settype { get; set; }
    }
}