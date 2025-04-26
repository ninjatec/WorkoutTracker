// filepath: /Users/marccoxall/Documents/projects/WorkoutTracker/Models/WorkoutTemplate.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models
{
    public class WorkoutTemplate
    {
        public int WorkoutTemplateId { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Template Name")]
        public string Name { get; set; }
        
        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; } = "";
        
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        [Display(Name = "Last Modified")]
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;
        
        // Metadata fields
        [Display(Name = "Is Public")]
        public bool IsPublic { get; set; } = false;
        
        [StringLength(50)]
        [Display(Name = "Category")]
        public string Category { get; set; } = "";
        
        [StringLength(200)]
        [Display(Name = "Tags")]
        public string Tags { get; set; } = "";
        
        // Foreign key for User
        public int UserId { get; set; }
        
        // Navigation property to User
        public User User { get; set; }
        
        // Navigation property to WorkoutTemplateExercises
        public ICollection<WorkoutTemplateExercise> TemplateExercises { get; set; } = new List<WorkoutTemplateExercise>();
    }
}