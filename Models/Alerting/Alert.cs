using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Alerting
{
    public class Alert
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int AlertThresholdId { get; set; }
        
        [ForeignKey("AlertThresholdId")]
        public AlertThreshold AlertThreshold { get; set; }
        
        [Required]
        public AlertSeverity Severity { get; set; }
        
        [Required]
        public double CurrentValue { get; set; }
        
        [Required]
        public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ResolvedAt { get; set; }
        
        public bool IsAcknowledged { get; set; }
        
        public DateTime? AcknowledgedAt { get; set; }
        
        [StringLength(100)]
        public string AcknowledgedBy { get; set; }
        
        [StringLength(500)]
        public string AcknowledgementNote { get; set; }
        
        public bool IsEscalated { get; set; }
        
        public DateTime? EscalatedAt { get; set; }
        
        [StringLength(500)]
        public string Details { get; set; }
        
        public bool EmailSent { get; set; }
        
        public DateTime? EmailSentAt { get; set; }
        
        public bool NotificationSent { get; set; }
        
        public DateTime? NotificationSentAt { get; set; }
    }
    
    public enum AlertSeverity
    {
        Warning,
        Critical
    }
}