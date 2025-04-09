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
        public ICollection<Excercise>? Excercises { get; set; }
    }
}