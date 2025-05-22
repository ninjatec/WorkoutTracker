using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Alerting
{
    public class AlertThreshold
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string MetricName { get; set; }

        [Required]
        [StringLength(50)]
        public string MetricCategory { get; set; }

        [Required]
        public double WarningThreshold { get; set; }

        [Required]
        public double CriticalThreshold { get; set; }

        [Required]
        public ThresholdDirection Direction { get; set; }

        public bool EmailEnabled { get; set; }

        public bool NotificationEnabled { get; set; }

        public int? EscalationMinutes { get; set; }

        [StringLength(200)]
        public string Description { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; }

        [Required]
        [StringLength(100)]
        public string UpdatedBy { get; set; }

        public bool IsEnabled { get; set; } = true;
    }

    public enum ThresholdDirection
    {
        Above,      // Alert when value is above threshold (e.g., CPU usage > 90%)
        Below,      // Alert when value is below threshold (e.g., Disk space < 10%)
        Equal,      // Alert when value equals threshold
        NotEqual    // Alert when value doesn't equal threshold
    }
}