using System;
using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.Models
{
    public class WhitelistedIp
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [MaxLength(45)] // Supports both IPv4 and IPv6 addresses
        public string IpAddress { get; set; }
        
        [MaxLength(255)]
        public string Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(100)]
        public string CreatedBy { get; set; }
    }
}