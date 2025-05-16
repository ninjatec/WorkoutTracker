// filepath: /Users/marccoxall/Documents/projects/WorkoutTracker/Models/WorkoutTemplateSet.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models
{
    public class WorkoutTemplateSet
    {
        public int WorkoutTemplateSetId { get; set; }
        
        // Foreign key for WorkoutTemplateExercise
        public int WorkoutTemplateExerciseId { get; set; }
        
        // Foreign key for Settype (e.g., regular, superset, drop set)
        public int SettypeId { get; set; }
        
        // Default values for the set
        [Display(Name = "Default Reps")]
        public int DefaultReps { get; set; } = 0;
        
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Default Weight (kg)")]
        public decimal DefaultWeight { get; set; } = 0;
        
        // Sequence number for ordering sets within the exercise
        [Display(Name = "Sequence")]
        public int SequenceNum { get; set; } = 0;
        
        [Display(Name = "Rest Time")]
        public TimeSpan? RestTime { get; set; } = TimeSpan.FromSeconds(60);
        
        [StringLength(200)]
        [Display(Name = "Notes")]
        public string Notes { get; set; } = "";
        
        [StringLength(200)]
        [Display(Name = "Description")]
        public string Description { get; set; } = "";
        
        // Navigation properties
        public WorkoutTemplateExercise WorkoutTemplateExercise { get; set; }
        public Settype Settype { get; set; }
    }
}