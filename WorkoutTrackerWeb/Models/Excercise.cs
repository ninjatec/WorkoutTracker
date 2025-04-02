using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models
{
    public class Excercise
    {
        public int ExcerciseId { get; set; }
        public string ExcerciseName { get; set; }
        public Session Sessions { get; set; }
        public ICollection<Set> Sets { get; set; } = new List<Set>();
    }
}