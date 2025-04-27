using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Models.Coaching
{
    public class CoachClientRelationship
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(450)]
        [Display(Name = "Coach")]
        public string CoachId { get; set; }
        
        [Required]
        [StringLength(450)]
        [Display(Name = "Client")]
        public string ClientId { get; set; }
        
        [Display(Name = "Status")]
        public RelationshipStatus Status { get; set; } = RelationshipStatus.Pending;
        
        [Display(Name = "Start Date")]
        public DateTime? StartDate { get; set; }
        
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }
        
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Last Modified")]
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
        
        [StringLength(1000)]
        [Display(Name = "Notes")]
        public string Notes { get; set; }
        
        // Navigation properties
        [ForeignKey("CoachId")]
        public AppUser Coach { get; set; }
        
        [ForeignKey("ClientId")]
        public AppUser Client { get; set; }
        
        // One-to-One relationship with CoachPermission
        public CoachPermission Permissions { get; set; }
    }
    
    public enum RelationshipStatus
    {
        [Display(Name = "Pending")]
        Pending = 0,
        
        [Display(Name = "Active")]
        Active = 1,
        
        [Display(Name = "Paused")]
        Paused = 2,
        
        [Display(Name = "Ended")]
        Ended = 3,
        
        [Display(Name = "Declined")]
        Declined = 4
    }
}