using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models
{
    public class ShareToken
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Token { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; }
        
        [Required]
        public DateTime ExpiresAt { get; set; }
        
        [Required]
        public bool IsActive { get; set; } = true;
        
        // The number of times this token has been accessed
        public int AccessCount { get; set; } = 0;
        
        // Optional maximum number of times the token can be accessed (null = unlimited)
        public int? MaxAccessCount { get; set; }
        
        // Access control flags
        public bool AllowSessionAccess { get; set; } = true;
        public bool AllowReportAccess { get; set; } = true;
        public bool AllowCalculatorAccess { get; set; } = true;
        
        // User who created this share token
        [Required]
        public int UserId { get; set; }
        public User User { get; set; }
        
        // Optional specific session to share (null = share all sessions)
        public int? SessionId { get; set; }
        public Session Session { get; set; }
        
        // Name assigned to this share by the creator
        [StringLength(100)]
        public string Name { get; set; }
        
        // Optional note/description for this share
        [StringLength(500)]
        public string Description { get; set; }
        
        // Check if token is valid for use
        [NotMapped]
        public bool IsValid => 
            IsActive && 
            ExpiresAt > DateTime.UtcNow && 
            (MaxAccessCount == null || AccessCount < MaxAccessCount);
            
        // Calculate days until expiration
        [NotMapped]
        public int DaysUntilExpiration => 
            Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalDays);
            
        // Check if token has usage limits
        [NotMapped]
        public bool HasUsageLimits => MaxAccessCount.HasValue;
        
        // Calculate remaining uses if limited
        [NotMapped]
        public int? RemainingUses => 
            MaxAccessCount.HasValue ? Math.Max(0, MaxAccessCount.Value - AccessCount) : null;
    }
}