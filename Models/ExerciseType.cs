using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.Models
{
    public class ExerciseType
    {
        public int ExerciseTypeId { get; set; }
        
        [Required]
        [StringLength(100, MinimumLength = 3)]
        [Display(Name = "Exercise Type")]
        public string Name { get; set; } = string.Empty;
        
        // Navigation property for related Sets
        public ICollection<Set>? Sets { get; set; }
    }
}