using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Models
{
    public class Settype
    {
        public int SettypeId { get; set; }
        public required string Name { get; set; } = "";
        public required string Description { get; set; } = "";
    }
}