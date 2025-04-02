using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models
{
    public class Settype
    {
        public int SettypeId { get; set; }
        public required string Name { get; set; } = "";
        public required string Description { get; set; } = "";
    }
}