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
        public int ExerciseTypeId { get; set; }  // Foreign key for ExerciseType
        public ExerciseType ExerciseType { get; set; }  // Navigation property
        public int NumberReps { get; set; } = 0;  // Number of reps in the set
        public int SessionId { get; set; }  // Adding direct reference to Session
        
        // Adding sequence number for ordering sets
        public int SequenceNum { get; set; } = 0;
        
        // Adding weight field as per SoW requirements
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Weight (kg)")]
        public decimal Weight { get; set; }
        
        // Navigation property to Session
        public Session Session { get; set; }
        
        [InverseProperty("Set")]
        public ICollection<Rep> Reps { get; set; } = new List<Rep>();
    }
}