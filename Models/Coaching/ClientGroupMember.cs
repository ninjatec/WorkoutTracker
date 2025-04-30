using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Represents a client's membership in a client group
    /// </summary>
    public class ClientGroupMember
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public int ClientGroupId { get; set; }
        
        [ForeignKey("ClientGroupId")]
        public ClientGroup ClientGroup { get; set; }
        
        [Required]
        public int CoachClientRelationshipId { get; set; }
        
        [ForeignKey("CoachClientRelationshipId")]
        public CoachClientRelationship Relationship { get; set; }
        
        [Required]
        public DateTime AddedDate { get; set; }
    }
}