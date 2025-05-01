using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models
{
    /// <summary>
    /// Represents a workout session tracked by the user
    /// </summary>
    public class WorkoutSession
    {
        public int WorkoutSessionId { get; set; }
        
        // Foreign key for User
        public int UserId { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Session Name")]
        public string Name { get; set; }
        
        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; }
        
        [Display(Name = "Start Date/Time")]
        public DateTime StartDateTime { get; set; } = DateTime.Now;
        
        [Display(Name = "End Date/Time")]
        public DateTime? EndDateTime { get; set; }
        
        [Display(Name = "Completed Date")]
        public DateTime? CompletedDate { get; set; }
        
        [Display(Name = "Duration (minutes)")]
        public int Duration { get; set; }
        
        [Display(Name = "Is Completed")]
        public bool IsCompleted { get; set; }
        
        [Display(Name = "Templates Used")]
        [StringLength(200)]
        public string TemplatesUsed { get; set; }
        
        // Foreign key for workout template (optional)
        public int? WorkoutTemplateId { get; set; }
        
        // Foreign key for template assignment (optional)
        public int? TemplateAssignmentId { get; set; }
        
        [Display(Name = "Start Time")]
        public DateTime? StartTime { get; set; }
        
        [Display(Name = "From Coach")]
        public bool IsFromCoach { get; set; }
        
        [StringLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; }
        
        // Foreign key to original Session (for migration tracking)
        public int? SessionId { get; set; }
        
        // Navigation properties
        [ForeignKey("UserId")]
        public User User { get; set; }
        
        [ForeignKey("WorkoutTemplateId")]
        public WorkoutTemplate WorkoutTemplate { get; set; }
        
        [ForeignKey("SessionId")]
        public Session Session { get; set; }
        
        // Collection navigation properties
        public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
        
        // Navigation to workout feedback
        public WorkoutTrackerWeb.Models.Coaching.WorkoutFeedback WorkoutFeedback { get; set; }
    }
}