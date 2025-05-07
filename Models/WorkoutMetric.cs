using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models
{
    /// <summary>
    /// Represents aggregated workout metrics for user progress tracking
    /// </summary>
    public class WorkoutMetric
    {
        [Key]
        public Guid Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }
        
        [Required]
        public string MetricType { get; set; } // "Volume", "Intensity", "Consistency"
        
        [Required]
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Value { get; set; }
        
        // Optional additional data in JSON format
        public string AdditionalData { get; set; }
        
        // Timestamps for tracking
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual User User { get; set; }
    }
}