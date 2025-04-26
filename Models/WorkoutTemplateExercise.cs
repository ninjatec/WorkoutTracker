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
        
        [StringLength(200)]
        [Display(Name = "Notes")]
        public string Notes { get; set; } = "";
        
        // Navigation properties
        public WorkoutTemplate WorkoutTemplate { get; set; }
        public ExerciseType ExerciseType { get; set; }
        
        // Navigation property to template sets
        public ICollection<WorkoutTemplateSet> TemplateSets { get; set; } = new List<WorkoutTemplateSet>();
    }
}