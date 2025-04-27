using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Represents the status of a coach-client relationship
    /// </summary>
    public enum RelationshipStatus
    {
        /// <summary>
        /// The invitation is pending client acceptance
        /// </summary>
        Pending = 0,
        
        /// <summary>
        /// The relationship is active
        /// </summary>
        Active = 1,
        
        /// <summary>
        /// The relationship is temporarily paused
        /// </summary>
        Paused = 2,
        
        /// <summary>
        /// The relationship has ended
        /// </summary>
        Ended = 3,
        
        /// <summary>
        /// The relationship was rejected by the client
        /// </summary>
        Rejected = 4
    }

    /// <summary>
    /// Represents a relationship between a coach and a client
    /// </summary>
    public class CoachClientRelationship
    {
        /// <summary>
        /// Gets or sets the ID of the relationship
        /// </summary>
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the coach user
        /// </summary>
        [Required]
        public string CoachId { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the client user
        /// </summary>
        [Required]
        public string ClientId { get; set; }
        
        /// <summary>
        /// Gets or sets the coach user
        /// </summary>
        [ForeignKey("CoachId")]
        public AppUser Coach { get; set; }
        
        /// <summary>
        /// Gets or sets the client user
        /// </summary>
        [ForeignKey("ClientId")]
        public AppUser Client { get; set; }
        
        /// <summary>
        /// Gets or sets the status of the relationship
        /// </summary>
        [Required]
        public RelationshipStatus Status { get; set; } = RelationshipStatus.Pending;
        
        /// <summary>
        /// Gets or sets the date when the relationship was created
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets the date when the relationship was last modified
        /// </summary>
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Gets or sets the date when the relationship became active
        /// </summary>
        public DateTime? StartDate { get; set; }
        
        /// <summary>
        /// Gets or sets the date when the relationship ended
        /// </summary>
        public DateTime? EndDate { get; set; }
        
        /// <summary>
        /// Gets or sets the optional invitation token for pending relationships
        /// </summary>
        public string InvitationToken { get; set; }
        
        /// <summary>
        /// Gets or sets the expiry date for the invitation token
        /// </summary>
        public DateTime? InvitationExpiryDate { get; set; }
        
        /// <summary>
        /// Gets or sets the client group ID
        /// </summary>
        public int? ClientGroupId { get; set; }
        
        /// <summary>
        /// Gets or sets the client group
        /// </summary>
        [ForeignKey("ClientGroupId")]
        public ClientGroup ClientGroup { get; set; }
        
        /// <summary>
        /// Gets or sets the permissions for this relationship
        /// </summary>
        public CoachClientPermission Permissions { get; set; }
        
        /// <summary>
        /// Gets or sets the notes for this relationship
        /// </summary>
        public ICollection<CoachNote> Notes { get; set; } = new List<CoachNote>();
    }
}