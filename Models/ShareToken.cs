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
        public string Token { get; set; } = "";
        
        [Required]
        public DateTime CreatedAt { get; set; }
        
        [Required]
        public DateTime ExpiresAt { get; set; }
        
        [Required]
        public bool IsActive { get; set; } = true;
        
        public int AccessCount { get; set; } = 0;
        
        public int? MaxAccessCount { get; set; }
        
        public bool AllowSessionAccess { get; set; } = true;
        public bool AllowReportAccess { get; set; } = true;
        public bool AllowCalculatorAccess { get; set; } = true;
        
        [Required]
        public int UserId { get; set; }
        public User User { get; set; }
        
        public int? WorkoutSessionId { get; set; }
        
        [ForeignKey("WorkoutSessionId")]
        public WorkoutSession WorkoutSession { get; set; }
        
        [StringLength(100)]
        public string Name { get; set; } = "";
        
        [StringLength(500)]
        public string Description { get; set; } = "";
        
        [NotMapped]
        public bool IsValid => 
            IsActive && 
            ExpiresAt > DateTime.UtcNow && 
            (MaxAccessCount == null || AccessCount < MaxAccessCount.Value);
            
        [NotMapped]
        public int DaysUntilExpiration => 
            Math.Max(0, (int)(ExpiresAt - DateTime.UtcNow).TotalDays);
            
        [NotMapped]
        public bool HasUsageLimits => MaxAccessCount.HasValue;
        
        [NotMapped]
        public int? RemainingUses => 
            MaxAccessCount.HasValue ? Math.Max(0, MaxAccessCount.Value - AccessCount) : null;
    }
}