using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Alerting
{
    public class AlertHistory
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int AlertId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string MetricName { get; set; }
        
        [Required]
        [StringLength(50)]
        public string MetricCategory { get; set; }
        
        [Required]
        public AlertSeverity Severity { get; set; }
        
        [Required]
        public double ThresholdValue { get; set; }
        
        [Required]
        public double ActualValue { get; set; }
        
        [Required]
        public ThresholdDirection Direction { get; set; }
        
        [Required]
        public DateTime TriggeredAt { get; set; }
        
        public DateTime? ResolvedAt { get; set; }
        
        public bool WasAcknowledged { get; set; }
        
        public DateTime? AcknowledgedAt { get; set; }
        
        [StringLength(100)]
        public string AcknowledgedBy { get; set; }
        
        [StringLength(500)]
        public string AcknowledgementNote { get; set; }
        
        public bool WasEscalated { get; set; }
        
        public TimeSpan? TimeToResolve { get; set; }
        
        public TimeSpan? TimeToAcknowledge { get; set; }
        
        [StringLength(500)]
        public string Details { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}