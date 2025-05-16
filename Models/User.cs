using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Name")]
        public string Name { get; set; }
        
        // Link to ASP.NET Identity User
        [StringLength(450)]
        public string? IdentityUserId { get; set; }
        
        // Height in centimeters
        [Display(Name = "Height")]
        [Range(0, 300)]
        [Column(TypeName = "decimal(5, 2)")]
        public decimal? HeightCm { get; set; }
        
        // Weight in kilograms
        [Display(Name = "Weight")]
        [Range(0, 500)]
        [Column(TypeName = "decimal(5, 2)")]
        public decimal? WeightKg { get; set; }
        
        // Use InverseProperty to correctly define the relationship
        [InverseProperty("User")]
        public ICollection<WorkoutSession> WorkoutSessions { get; set; } = new List<WorkoutSession>();
    }
}