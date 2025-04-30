using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Represents an activity performed by a client that may be of interest to their coach
    /// </summary>
    public class ClientActivity
    {
        public int Id { get; set; }
        
        // Foreign key for client's identity user
        public string ClientId { get; set; }
        
        // Foreign key for coach's identity user (optional)
        public string CoachId { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; }
        
        [Required]
        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; }
        
        [Display(Name = "Activity Date")]
        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Coach Viewed")]
        public bool IsViewedByCoach { get; set; } = false;
        
        [Display(Name = "Coach View Date")]
        public DateTime? ViewedByCoachDate { get; set; }
        
        [StringLength(50)]
        [Display(Name = "Related Entity Type")]
        public string RelatedEntityType { get; set; }
        
        [StringLength(50)]
        [Display(Name = "Related Entity ID")]
        public string RelatedEntityId { get; set; }
        
        // Navigation properties
        [ForeignKey("ClientId")]
        public AppUser Client { get; set; }
        
        [ForeignKey("CoachId")]
        public AppUser Coach { get; set; }
    }
}