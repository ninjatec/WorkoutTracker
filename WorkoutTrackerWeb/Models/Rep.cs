using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Models
{
    public class Rep
    {
        public int RepId { get; set; }
        public float weight { get; set; } = 0;
        public int repnumber { get; set; } = 0;
        public bool success { get; set; } = true; //true = success, false = fail

        //fully defined relationships
        public int? SessionId { get; set; }
        public int? UserId { get; set; }
        public int? ExcerciseId { get; set; }
        public virtual required User User { get; set; }
        public virtual required Session Session { get; set; }
        public virtual required Excercise Excercise { get; set; } 
    }
}