using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Models
{
    public class Reps
    {
        public int RepsId { get; set; }
        public float weight { get; set; } = 0;
        public int repnumber { get; set; } = 0;
        public bool success { get; set; } = true; //true = success, false = fail

        //fully defined relationships
        public int? SessionId { get; set; }
        public int? UserId { get; set; }
        public int? ExcerciseId { get; set; }
        public virtual User User { get; set; }
        public virtual Session Session { get; set; }
        public virtual Excercise Excercise { get; set; } 
    }
}