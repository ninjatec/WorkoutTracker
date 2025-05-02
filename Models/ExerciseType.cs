#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WorkoutTrackerWeb.Models
{
    /// <summary>
    /// Represents a type of exercise that can be performed in a workout
    /// </summary>
    public class ExerciseType
    {
        public int ExerciseTypeId { get; set; }
        
        [Required]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "Exercise Type")]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;
        
        // Extended properties from API Ninjas
        [StringLength(50)]
        [Display(Name = "Exercise Type")]
        public string? Type { get; set; }
        
        [StringLength(50)]
        [Display(Name = "Muscle Group")]
        public string? Muscle { get; set; }
        
        [StringLength(100)]
        [Display(Name = "Primary Muscles")]
        public string? PrimaryMuscles { get; set; }
        
        [StringLength(200)]
        [Display(Name = "Secondary Muscles")]
        public string? SecondaryMuscles { get; set; }
        
        [StringLength(100)]
        [Display(Name = "Equipment")]
        public string? Equipment { get; set; }
        
        [StringLength(20)]
        [Display(Name = "Difficulty")]
        public string? Difficulty { get; set; }
        
        [StringLength(4000)]
        [Display(Name = "Instructions")]
        public string? Instructions { get; set; }
        
        [Display(Name = "From API")]
        public bool IsFromApi { get; set; } = false;
        
        [DataType(DataType.DateTime)]
        [Display(Name = "Last Updated")]
        public DateTime? LastUpdated { get; set; }
        
        // Category for filtering and grouping
        [StringLength(50)]
        [Display(Name = "Category")]
        public string Category { get; set; } = "Other";

        [StringLength(50)]
        [Display(Name = "Primary Muscle Group")]
        public string? PrimaryMuscleGroup { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Calories Per Minute")]
        public decimal? CaloriesPerMinute { get; set; }
        
        // WorkoutSession-based collection navigation properties
        public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
        public ICollection<WorkoutTemplateExercise> TemplateExercises { get; set; } = new List<WorkoutTemplateExercise>();
    }
}