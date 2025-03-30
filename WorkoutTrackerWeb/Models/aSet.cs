using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Models
{
    public class aSet
    {
        public int aSetId { get; set; }
        public string Description { get; set; } = "";
        public string Notes { get; set; } = "";
        public bool Type { get; set; } = false; //true = weight, false = time

        //fully defined relationships
        public int? SessionId { get; set; }
        public int? UserId { get; set; }
        public int? ExcerciseId { get; set; }
        public virtual required User User { get; set; }
        public virtual required Session Session { get; set; }
        public virtual required Excercise Excercise { get; set; }
    }
}