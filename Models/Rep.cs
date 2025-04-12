using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WorkoutTrackerWeb.Models
{
    public class Rep
    {
        public int RepId { get; set; }
        
        [Column(TypeName = "decimal(6, 2)")]
        public decimal weight { get; set; } = 0;
        
        public int repnumber { get; set; } = 0;
        public bool success { get; set; } = true; //true = success, false = fail
        
        public int? SetsSetId { get; set; }
        [ForeignKey("SetsSetId")]
        [DeleteBehavior(DeleteBehavior.Cascade)]
        public Set Sets { get; set; }
    }
}