using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Coaching
{
    public class CoachPermission
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Can View Workouts")]
        public bool CanViewWorkouts { get; set; } = true;
        
        [Required]
        [Display(Name = "Can Create Workouts")]
        public bool CanCreateWorkouts { get; set; } = false;
        
        [Required]
        [Display(Name = "Can Edit Workouts")]
        public bool CanEditWorkouts { get; set; } = false;
        
        [Required]
        [Display(Name = "Can Delete Workouts")]
        public bool CanDeleteWorkouts { get; set; } = false;
        
        [Required]
        [Display(Name = "Can View Reports")]
        public bool CanViewReports { get; set; } = true;
        
        [Required]
        [Display(Name = "Can Create Templates")]
        public bool CanCreateTemplates { get; set; } = true;
        
        [Required]
        [Display(Name = "Can Assign Templates")]
        public bool CanAssignTemplates { get; set; } = true;
        
        [Required]
        [Display(Name = "Can View Personal Info")]
        public bool CanViewPersonalInfo { get; set; } = false;
        
        [Required]
        [Display(Name = "Can Create Goals")]
        public bool CanCreateGoals { get; set; } = true;
        
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Last Modified")]
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
        
        // Navigation property for relationship
        public int CoachClientRelationshipId { get; set; }
        public CoachClientRelationship Relationship { get; set; }
    }
}