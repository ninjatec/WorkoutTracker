using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models
{
    public class Set
    {
        public int SetId { get; set; }
        public string Description { get; set; } = "";
        public string Notes { get; set; } = "";
        public int SettypeId { get; set; }  // Foreign key for Settype
        public Settype Settype { get; set; }  // Navigation property
        public int ExerciseId { get; set; }  // Foreign key for Exercise
        public Excercise Exercise { get; set; }  // Navigation property
        public int NumberReps { get; set; } = 0;  // Number of reps in the set
        
        [InverseProperty("Sets")]
        public ICollection<Rep> Reps { get; set; } = new List<Rep>();
    }
}