using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Models
{
    public class User
    {
        public int UserId { get; set; }
        public required string Name { get; set; }    
    }
}