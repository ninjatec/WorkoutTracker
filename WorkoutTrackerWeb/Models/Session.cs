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
        public required string Name { get; set; }
        public required DateTime datetime { get; set; }
        public required int UserId { get; set; }
        public required User User { get; set; }
        public ICollection<Excercise> Excercises { get; set; }
    }
}