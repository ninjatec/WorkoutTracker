using System;
using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.Models
{
    public class Feedback
    {
        public int FeedbackId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = "";
        
        [Required]
        [StringLength(5000)]
        public string Message { get; set; } = "";
        
        [StringLength(100)]
        public string? ContactEmail { get; set; }
        
        public DateTime SubmissionDate { get; set; } = DateTime.Now;
        
        public FeedbackType Type { get; set; }
        
        public FeedbackStatus Status { get; set; } = FeedbackStatus.New;
        
        [StringLength(1000)]
        public string? AdminNotes { get; set; }
        
        // Link to the user who submitted the feedback
        public int? UserId { get; set; }
        public User? User { get; set; }
        
        // New fields for enhanced admin management
        public FeedbackPriority? Priority { get; set; }
        
        public string? AssignedToAdminId { get; set; }
        
        public DateTime? LastUpdated { get; set; }
        
        public DateTime? EstimatedCompletionDate { get; set; }
        
        [StringLength(500)]
        public string? PublicResponse { get; set; }
        
        // Track internal status changes
        public bool IsPublished { get; set; } = false;
        
        // Additional metadata
        public string? Category { get; set; }
        public string? BrowserInfo { get; set; }
        public string? DeviceInfo { get; set; }
    }
    
    public enum FeedbackType
    {
        BugReport,
        FeatureRequest,
        GeneralFeedback,
        Question
    }
    
    public enum FeedbackStatus
    {
        New,
        InProgress,
        Completed,
        Rejected
    }
    
    public enum FeedbackPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}