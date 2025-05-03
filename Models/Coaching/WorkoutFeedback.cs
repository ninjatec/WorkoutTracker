using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Represents client feedback on a completed workout
    /// </summary>
    public class WorkoutFeedback
    {
        public int WorkoutFeedbackId { get; set; }
        
        // Foreign key for WorkoutSession
        public int WorkoutSessionId { get; set; }
        
        [ForeignKey("WorkoutSessionId")]
        public WorkoutSession WorkoutSession { get; set; }
        
        // Foreign key for client User
        public int ClientUserId { get; set; }
        
        [Display(Name = "Feedback Date")]
        public DateTime FeedbackDate { get; set; } = DateTime.Now;
        
        [Range(1, 10)]
        [Display(Name = "Overall Rating")]
        public int OverallRating { get; set; }
        
        [Range(1, 10)]
        [Display(Name = "Difficulty Rating")]
        public int DifficultyRating { get; set; }
        
        [Range(1, 10)]
        [Display(Name = "Energy Level")]
        public int EnergyLevel { get; set; }
        
        [StringLength(1000)]
        [Display(Name = "Comments")]
        public string Comments { get; set; }
        
        [Display(Name = "Completed All Exercises")]
        public bool CompletedAllExercises { get; set; } = true;
        
        [StringLength(1000)]
        [Display(Name = "Incomplete Reason")]
        public string IncompleteReason { get; set; }
        
        [Display(Name = "Coach Notified")]
        public bool CoachNotified { get; set; } = false;
        
        [Display(Name = "Coach Viewed")]
        public bool CoachViewed { get; set; } = false;
        
        // Navigation properties
        [ForeignKey("ClientUserId")]
        public User Client { get; set; }
        
        // Navigation to per-exercise feedback
        // Making this virtual to optimize lazy loading behavior
        public virtual ICollection<ExerciseFeedback> ExerciseFeedbacks { get; set; } = new List<ExerciseFeedback>();
    }
    
    /// <summary>
    /// Represents feedback on a specific exercise within a workout
    /// </summary>
    public class ExerciseFeedback
    {
        public int ExerciseFeedbackId { get; set; }
        
        // Foreign key for WorkoutFeedback
        public int WorkoutFeedbackId { get; set; }
        
        // Foreign key for WorkoutSet instead of Set
        public int WorkoutSetId { get; set; }
        
        [Range(1, 10)]
        [Display(Name = "RPE (Rate of Perceived Exertion)")]
        public int RPE { get; set; }
        
        [Range(1, 10)]
        [Display(Name = "Difficulty")]
        public int Difficulty { get; set; }
        
        [Display(Name = "Too Heavy")]
        public bool TooHeavy { get; set; } = false;
        
        [Display(Name = "Too Light")]
        public bool TooLight { get; set; } = false;
        
        [StringLength(500)]
        [Display(Name = "Comments")]
        public string Comments { get; set; }
        
        // Navigation properties
        [ForeignKey("WorkoutFeedbackId")]
        public virtual WorkoutFeedback WorkoutFeedback { get; set; }
        
        [ForeignKey("WorkoutSetId")]
        public WorkoutSet WorkoutSet { get; set; }
    }
}