using System;
using System.ComponentModel.DataAnnotations;

namespace WorkoutTrackerWeb.Models
{
    public class LoginHistory
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(450)]
        public string IdentityUserId { get; set; }
        
        [Required]
        public DateTime LoginTime { get; set; }
        
        [Required]
        [StringLength(45)]
        public string IpAddress { get; set; }
        
        [StringLength(512)]
        public string UserAgent { get; set; }
        
        public bool IsSuccessful { get; set; }
        
        [StringLength(100)]
        public string DeviceType { get; set; }
        
        [StringLength(100)]
        public string Platform { get; set; }
    }
}