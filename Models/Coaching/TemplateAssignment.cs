using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Represents a workout template assignment to a client or group
    /// </summary>
    public class TemplateAssignment
    {
        public int TemplateAssignmentId { get; set; }
        
        // Foreign key for WorkoutTemplate
        public int WorkoutTemplateId { get; set; }
        
        // Foreign key for client User
        public int ClientUserId { get; set; }
        
        // Foreign key for coach User
        public int CoachUserId { get; set; }
        
        // Optional group association
        [StringLength(50)]
        public string ClientGroupName { get; set; }
        
        [Display(Name = "Assigned Date")]
        public DateTime AssignedDate { get; set; } = DateTime.Now;
        
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Now;
        
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }
        
        [StringLength(500)]
        [Display(Name = "Coach Notes")]
        public string CoachNotes { get; set; }
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Client Notified")]
        public bool ClientNotified { get; set; } = false;
        
        // Navigation properties
        public WorkoutTemplate WorkoutTemplate { get; set; }
        
        [ForeignKey("ClientUserId")]
        public User Client { get; set; }
        
        [ForeignKey("CoachUserId")]
        public User Coach { get; set; }
    }
}