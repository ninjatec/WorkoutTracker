using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace WorkoutTrackerWeb.Models
{
    public class Session
    {
        public int SessionId { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "Name")]
        public string Name { get; set; }
        public DateTime datetime { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public ICollection<Set>? Sets { get; set; }
        
        [StringLength(500)]
        public string Notes { get; set; } = "";
        
        [NotMapped]
        public TimeSpan TotalWorkoutTime 
        { 
            get 
            {
                // Calculate workout time based on session duration or sets
                return datetime.TimeOfDay; // Default implementation
            }
        }
    }
}