using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Represents a group of clients managed by a coach
    /// </summary>
    public class ClientGroup
    {
        /// <summary>
        /// Gets or sets the ID of the client group
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the client group
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the description of the client group
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; } = "";

        /// <summary>
        /// Gets or sets the ID of the coach who owns this group
        /// </summary>
        [Required]
        public string CoachId { get; set; } = "";

        /// <summary>
        /// Gets or sets the coach who owns this group
        /// </summary>
        [ForeignKey("CoachId")]
        public AppUser Coach { get; set; }

        /// <summary>
        /// Gets or sets the date when the group was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the date when the group was last modified
        /// </summary>
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the color code for the group (for UI display)
        /// </summary>
        [MaxLength(10)]
        public string ColorCode { get; set; } = "";

        /// <summary>
        /// Gets or sets the client relationships in this group
        /// </summary>
        public ICollection<CoachClientRelationship> ClientRelationships { get; set; } 
            = new List<CoachClientRelationship>();
    }
}