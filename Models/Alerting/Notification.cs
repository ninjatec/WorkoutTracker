using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Alerting
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        
        public int? AlertId { get; set; }
        
        [ForeignKey("AlertId")]
        public Alert Alert { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; }
        
        [Required]
        [StringLength(1000)]
        public string Message { get; set; }
        
        [Required]
        public NotificationType Type { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsRead { get; set; }
        
        public DateTime? ReadAt { get; set; }
        
        [StringLength(500)]
        public string Url { get; set; }
    }
    
    public enum NotificationType
    {
        Info,
        Warning,
        Critical,
        Success
    }
}