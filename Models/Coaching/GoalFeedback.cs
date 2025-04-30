using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Represents coach feedback on a client's goal
    /// </summary>
    public class GoalFeedback
    {
        public int Id { get; set; }
        
        // Foreign key for ClientGoal
        public int GoalId { get; set; }
        
        // Foreign key for coach's identity user
        public string CoachId { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "Feedback Type")]
        public string FeedbackType { get; set; }
        
        [Required]
        [StringLength(1000)]
        [Display(Name = "Message")]
        public string Message { get; set; }
        
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Client Read")]
        public bool IsRead { get; set; } = false;
        
        [Display(Name = "Client Read Date")]
        public DateTime? ReadDate { get; set; }
        
        // Navigation properties
        [ForeignKey("GoalId")]
        public ClientGoal Goal { get; set; }
        
        [ForeignKey("CoachId")]
        public AppUser Coach { get; set; }
    }
}