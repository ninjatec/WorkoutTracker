using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Models
{
    public class Excercise
    {
        public int ExcerciseId { get; set; }
        public required string ExcerciseName { get; set; }
        public required Session Session { get; set; }
        public ICollection<Set> Set { get; set; } 
    }
}