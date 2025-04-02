using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models
{
    public class Rep
    {
        public int RepId { get; set; }
        public float weight { get; set; } = 0;
        public int repnumber { get; set; } = 0;
        public bool success { get; set; } = true; //true = success, false = fail
        public Set Sets { get; set; }

    }
}