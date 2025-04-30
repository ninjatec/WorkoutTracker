using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using WorkoutTrackerWeb.Models.Coaching;

namespace WorkoutTrackerWeb.Models.Identity
{
    // This class extends IdentityUser with additional navigation properties for coaching relationships
    public class AppUser : IdentityUser
    {
        // Coach navigation properties
        public virtual ICollection<CoachClientRelationship> CoachRelationships { get; set; } = new List<CoachClientRelationship>();
        
        // Client navigation properties
        public virtual ICollection<CoachClientRelationship> ClientRelationships { get; set; } = new List<CoachClientRelationship>();
        
        // Audit fields
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
    }
}