using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Models
{
    public class Session
    {
        public int SessionId { get; set; }
        public required string Name { get; set; }
        public required DateTime datetime { get; set; }
 
        //fully defined relationships
        public int? UserId { get; set; }

        public virtual User User { get; set; }
    }
}