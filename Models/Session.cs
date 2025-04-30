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
        
        [Display(Name = "Start Date/Time")]
        public DateTime StartDateTime { get; set; } = DateTime.Now;
        
        [Display(Name = "End Date/Time")]
        public DateTime? endtime { get; set; }
        
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
                if (endtime.HasValue)
                {
                    return endtime.Value - datetime;
                }
                // Fallback to default implementation if no end time
                return datetime.TimeOfDay;
            }
        }
        
        // Total volume of the workout (weight × reps across all sets)
        [NotMapped]
        [Display(Name = "Total Volume")]
        public decimal TotalVolume { get; set; }
        
        // Estimated calories burned during the workout
        [NotMapped]
        [Display(Name = "Estimated Calories")]
        public int EstimatedCalories { get; set; }
        
        // Status from the related WorkoutSession model (not stored in database)
        [NotMapped]
        public string WorkoutSessionStatus { get; set; }
    }
}