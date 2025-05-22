using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models
{
    public class Rep
    {
        public int RepId { get; set; }
        
        public int WorkoutSetId { get; set; }
        
        public int RepNumber { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Weight { get; set; }
        
        public bool Success { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        [ForeignKey("WorkoutSetId")]
        public WorkoutSet WorkoutSet { get; set; }
    }
}