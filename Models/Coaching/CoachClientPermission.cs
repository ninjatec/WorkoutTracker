using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Represents the permissions a coach has for managing a client
    /// </summary>
    public class CoachClientPermission
    {
        /// <summary>
        /// Gets or sets the ID of the permission
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
        /// Gets or sets whether the coach can view the client's workouts
        /// </summary>
        public bool CanViewWorkouts { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the coach can create workouts for the client
        /// </summary>
        public bool CanCreateWorkouts { get; set; } = false;

        /// <summary>
        /// Gets or sets whether the coach can modify workouts for the client
        /// </summary>
        public bool CanModifyWorkouts { get; set; } = false;

        /// <summary>
        /// Gets or sets whether the coach can edit workouts for the client
        /// </summary>
        public bool CanEditWorkouts { get; set; } = false;

        /// <summary>
        /// Gets or sets whether the coach can delete workouts for the client
        /// </summary>
        public bool CanDeleteWorkouts { get; set; } = false;

        /// <summary>
        /// Gets or sets whether the coach can view personal information about the client
        /// </summary>
        public bool CanViewPersonalInfo { get; set; } = false;

        /// <summary>
        /// Gets or sets whether the coach can create goals for the client
        /// </summary>
        public bool CanCreateGoals { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the coach can view the client's progress reports
        /// </summary>
        public bool CanViewReports { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the coach can send messages to the client
        /// </summary>
        public bool CanMessage { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the coach can create workout templates
        /// </summary>
        public bool CanCreateTemplates { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the coach can assign workout templates to the client
        /// </summary>
        public bool CanAssignTemplates { get; set; } = true;

        /// <summary>
        /// Gets or sets the date when the permissions were last modified
        /// </summary>
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;
    }
}