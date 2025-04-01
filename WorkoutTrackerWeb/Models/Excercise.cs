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
        public required Session Sessions { get; set; }
        public ICollection<Set> Sets { get; set; } = new List<Set>();
    }
}