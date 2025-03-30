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
        public required int UserId { get; set; }
        public User User { get; set; }
        public ICollection<Set> Set { get; set; } 
        public ICollection<Excercise> Excercise { get; set; }
        public ICollection<Rep> Rep { get; set; } 
    }
}