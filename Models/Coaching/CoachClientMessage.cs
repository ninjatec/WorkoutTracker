using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Represents a message between a coach and a client
    /// </summary>
    public class CoachClientMessage
    {
        /// <summary>
        /// Gets or sets the ID of the message
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
        /// Gets or sets the content of the message
        /// </summary>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the date when the message was sent
        /// </summary>
        public DateTime SentDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets whether the message was sent by the coach
        /// </summary>
        public bool IsFromCoach { get; set; }

        /// <summary>
        /// Gets or sets whether the message has been read by the recipient
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// Gets or sets the date when the message was read
        /// </summary>
        public DateTime? ReadDate { get; set; }

        /// <summary>
        /// Gets or sets the optional attachment URL
        /// </summary>
        public string AttachmentUrl { get; set; }

        /// <summary>
        /// Gets or sets the type of attachment
        /// </summary>
        [MaxLength(50)]
        public string AttachmentType { get; set; }
    }
}