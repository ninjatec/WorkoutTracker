// filepath: /Users/marccoxall/Documents/projects/WorkoutTracker/Models/WorkoutTemplateExercise.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models
{
    public class WorkoutTemplateExercise
    {
        public int WorkoutTemplateExerciseId { get; set; }
        
        // Foreign key for WorkoutTemplate
        public int WorkoutTemplateId { get; set; }
        
        // Foreign key for ExerciseType
        public int ExerciseTypeId { get; set; }
        
        // Sequence number for ordering exercises within the template
        [Display(Name = "Sequence")]
        public int SequenceNum { get; set; } = 0;
        
        // Order index for sorting exercises
        [Display(Name = "Order")]
        public int OrderIndex { get; set; } = 0;
        
        // Equipment reference
        public int? EquipmentId { get; set; }
        
        // Set and rep counts
        [Display(Name = "Sets")]
        public int Sets { get; set; } = 3;
        
        [Display(Name = "Min Reps")]
        public int MinReps { get; set; } = 8;
        
        [Display(Name = "Max Reps")]
        public int MaxReps { get; set; } = 12;
        
        [Display(Name = "Rest (seconds)")]
        public int RestSeconds { get; set; } = 60;
        
        [StringLength(200)]
        [Display(Name = "Notes")]
        public string Notes { get; set; } = "";
        
        // Navigation properties
        public WorkoutTemplate WorkoutTemplate { get; set; }
        
        [ForeignKey("ExerciseTypeId")]
        public ExerciseType ExerciseType { get; set; }
        
        [ForeignKey("EquipmentId")]
        public Equipment Equipment { get; set; }
        
        // Navigation property to template sets
        public ICollection<WorkoutTemplateSet> TemplateSets { get; set; } = new List<WorkoutTemplateSet>();
    }
}