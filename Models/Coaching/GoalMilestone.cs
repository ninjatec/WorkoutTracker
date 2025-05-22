using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Represents a milestone in goal progress tracking
    /// </summary>
    public class GoalMilestone
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Foreign key to ClientGoal
        /// </summary>
        public int GoalId { get; set; }
        
        /// <summary>
        /// Navigation property to the parent goal
        /// </summary>
        public virtual ClientGoal Goal { get; set; }
        
        /// <summary>
        /// Date when this milestone was recorded
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// The value at this milestone
        /// </summary>
        [Column(TypeName = "decimal(10, 2)")]
        public decimal Value { get; set; }
        
        /// <summary>
        /// Progress percentage at this milestone
        /// </summary>
        public int ProgressPercentage { get; set; }
        
        /// <summary>
        /// Optional notes about this milestone
        /// </summary>
        public string Notes { get; set; }
        
        /// <summary>
        /// Whether this milestone was automatically generated
        /// </summary>
        public bool IsAutomaticUpdate { get; set; }
        
        /// <summary>
        /// Source of this progress update (manual, workout, etc.)
        /// </summary>
        [MaxLength(50)]
        public string Source { get; set; }
        
        /// <summary>
        /// Gets or sets a reference ID if this milestone is linked to another entity
        /// </summary>
        public string ReferenceId { get; set; }
    }
}