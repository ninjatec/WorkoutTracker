using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Models
{
    public class Set
    {
        public int SetId { get; set; }
        public string Description { get; set; } = "";
        public string Notes { get; set; } = "";
        public bool Type { get; set; } = false; //true = weight, false = time
        public required Excercise Excercises { get; set; }
        public ICollection<Rep> Reps { get; set; } = new List<Rep>();
    }
}