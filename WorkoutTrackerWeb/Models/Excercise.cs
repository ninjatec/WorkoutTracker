using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Models
{
    public class Excercise
    {
        public int ExcerciseId { get; set; }
        public required string Name { get; set; }

        //fully defined relationships
        public int? SessionId { get; set; }
        public int? UserId { get; set; }
        public virtual required User User { get; set; }
        public virtual required Session Session { get; set; }
    }
}