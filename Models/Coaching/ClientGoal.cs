using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Represents a goal set for a client by a coach or created by the user themselves
    /// </summary>
    public class ClientGoal
    {
        /// <summary>
        /// Gets or sets the ID of the goal
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the coach-client relationship (can be null for user-created goals)
        /// </summary>
        public int? CoachClientRelationshipId { get; set; }

        /// <summary>
        /// Gets or sets the coach-client relationship (can be null for user-created goals)
        /// </summary>
        [ForeignKey("CoachClientRelationshipId")]
        public CoachClientRelationship Relationship { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who created/owns this goal
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets whether this goal was created by a coach (false for user-created goals)
        /// </summary>
        public bool IsCoachCreated { get; set; } = false;

        /// <summary>
        /// Gets or sets the description of the goal
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the category of the goal
        /// </summary>
        public GoalCategory Category { get; set; } = GoalCategory.Other;

        /// <summary>
        /// Gets or sets a custom category name if Category is set to Other
        /// </summary>
        [MaxLength(50)]
        public string CustomCategory { get; set; }

        /// <summary>
        /// Gets or sets the date when the goal was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the target date for completing the goal
        /// </summary>
        public DateTime TargetDate { get; set; }

        /// <summary>
        /// Gets or sets the date when the goal was completed
        /// </summary>
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Gets or sets the type of measurement for the goal (e.g., Weight, Reps, Time)
        /// </summary>
        [MaxLength(50)]
        public string MeasurementType { get; set; }

        /// <summary>
        /// Gets or sets the starting value for the goal
        /// </summary>
        public decimal? StartValue { get; set; }

        /// <summary>
        /// Gets or sets the current value for the goal
        /// </summary>
        public decimal? CurrentValue { get; set; }

        /// <summary>
        /// Gets or sets the target value for the goal
        /// </summary>
        public decimal? TargetValue { get; set; }

        /// <summary>
        /// Gets or sets the unit of measurement (e.g., kg, lbs, seconds)
        /// </summary>
        [MaxLength(20)]
        public string MeasurementUnit { get; set; }

        /// <summary>
        /// Gets or sets the notes associated with the goal
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Gets or sets whether the goal is active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the goal is completed
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Gets or sets whether the goal is visible to the coach
        /// </summary>
        public bool IsVisibleToCoach { get; set; } = true;

        /// <summary>
        /// Gets or sets the frequency of automated progress tracking (Daily, Weekly, Monthly)
        /// </summary>
        [MaxLength(20)]
        public string TrackingFrequency { get; set; }

        /// <summary>
        /// Gets or sets the last date when progress was updated
        /// </summary>
        public DateTime? LastProgressUpdate { get; set; }

        /// <summary>
        /// Gets or sets the completion criteria type (e.g., ReachValue, CompleteBefore)
        /// </summary>
        [MaxLength(30)]
        public string CompletionCriteria { get; set; }

        /// <summary>
        /// Gets the progress of the goal as a percentage
        /// </summary>
        [NotMapped]
        public int ProgressPercentage 
        { 
            get 
            {
                // If the goal is completed, return 100%
                if (IsCompleted || CompletedDate.HasValue)
                    return 100;

                // If there is no measurement, calculate progress based on time
                if (string.IsNullOrEmpty(MeasurementType) || !StartValue.HasValue || !CurrentValue.HasValue || !TargetValue.HasValue)
                {
                    var totalDays = (TargetDate - CreatedDate).TotalDays;
                    if (totalDays <= 0)
                        return 0;

                    var elapsedDays = (DateTime.UtcNow - CreatedDate).TotalDays;
                    var timeProgress = Math.Min(100, (int)((elapsedDays / totalDays) * 100));
                    return timeProgress;
                }

                // Calculate progress based on measurement values
                var startToTarget = Convert.ToDouble(TargetValue.Value - StartValue.Value);
                if (Math.Abs(startToTarget) < 0.0001)
                    return 100;

                var startToCurrent = Convert.ToDouble(CurrentValue.Value - StartValue.Value);
                var progress = (int)((startToCurrent / startToTarget) * 100);
                
                // Ensure progress is between 0 and 100
                return Math.Min(100, Math.Max(0, progress));
            }
        }
    }
}