using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Represents a note created by a coach for a client
    /// </summary>
    public class CoachNote
    {
        /// <summary>
        /// Gets or sets the ID of the note
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the coach-client relationship
        /// </summary>
        public int CoachClientRelationshipId { get; set; }

        /// <summary>
        /// Gets or sets the coach-client relationship
        /// </summary>
        [ForeignKey("CoachClientRelationshipId")]
        public CoachClientRelationship Relationship { get; set; }

        /// <summary>
        /// Gets or sets the content of the note
        /// </summary>
        [Required]
        public string Content { get; set; } = "";

        /// <summary>
        /// Gets or sets the date when the note was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the date when the note was last updated
        /// </summary>
        public DateTime? UpdatedDate { get; set; }

        /// <summary>
        /// Gets or sets whether the note is visible to the client
        /// </summary>
        public bool IsVisibleToClient { get; set; }

        /// <summary>
        /// Gets or sets the category or tag for the note
        /// </summary>
        [MaxLength(50)]
        public string Category { get; set; } = "";
    }
}