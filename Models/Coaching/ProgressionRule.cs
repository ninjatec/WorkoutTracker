using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Represents a rule for automatically progressing workout parameters
    /// </summary>
    public class ProgressionRule
    {
        public int ProgressionRuleId { get; set; }
        
        // Foreign key for WorkoutTemplateExercise or WorkoutTemplateSet (optional)
        public int? WorkoutTemplateExerciseId { get; set; }
        public int? WorkoutTemplateSetId { get; set; }
        
        // Foreign key for client User (optional - if client-specific)
        public int? ClientUserId { get; set; }
        
        // Foreign key for coach User
        public int CoachUserId { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "Rule Name")]
        public string Name { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "Rule Type")]
        public string RuleType { get; set; } // Percentage, AbsoluteValue, RPE
        
        [Required]
        [StringLength(50)]
        [Display(Name = "Parameter")]
        public string Parameter { get; set; } // Weight, Reps, Sets, Rest
        
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Increment Value")]
        public decimal IncrementValue { get; set; }
        
        [Display(Name = "Consecutive Successes Required")]
        public int ConsecutiveSuccessesRequired { get; set; } = 2;
        
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Success Threshold")]
        public decimal SuccessThreshold { get; set; } // e.g., RPE <= 7, or completion rate >= 90%
        
        [Display(Name = "Maximum Value")]
        public decimal? MaximumValue { get; set; }
        
        [Display(Name = "Apply Deload")]
        public bool ApplyDeload { get; set; } = false;
        
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Deload Percentage")]
        public decimal DeloadPercentage { get; set; } = 10;
        
        [Display(Name = "Consecutive Failures For Deload")]
        public int ConsecutiveFailuresForDeload { get; set; } = 3;
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        [Display(Name = "Last Modified")]
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;
        
        // Navigation properties
        public WorkoutTemplateExercise WorkoutTemplateExercise { get; set; }
        public WorkoutTemplateSet WorkoutTemplateSet { get; set; }
        
        [ForeignKey("ClientUserId")]
        public User Client { get; set; }
        
        [ForeignKey("CoachUserId")]
        public User Coach { get; set; }
        
        // Navigation to progression history
        public ICollection<ProgressionHistory> ProgressionHistory { get; set; } = new List<ProgressionHistory>();

        public bool CanApplyToWorkout(WorkoutSession session)
        {
            if (session == null)
                return false;

            // Check if this rule applies to the client
            if (ClientUserId.HasValue && session.UserId != ClientUserId.Value)
                return false;

            // If this rule is specific to a template exercise, verify it matches
            if (WorkoutTemplateExerciseId.HasValue && session.WorkoutTemplateId.HasValue)
            {
                var templateId = session.WorkoutTemplateId.Value;
                // Further template-specific checks would go here
            }

            // Add any additional validation logic here
            return true;
        }
    }
    
    /// <summary>
    /// Records history of progression rule applications
    /// </summary>
    public class ProgressionHistory
    {
        public int ProgressionHistoryId { get; set; }
        
        // Foreign key for ProgressionRule
        public int ProgressionRuleId { get; set; }
        
        // Foreign key for WorkoutSession that triggered the progression
        public int? WorkoutSessionId { get; set; }
        
        [Display(Name = "Application Date")]
        public DateTime ApplicationDate { get; set; } = DateTime.Now;
        
        [Required]
        [StringLength(50)]
        [Display(Name = "Action Taken")]
        public string ActionTaken { get; set; } // Increase, Decrease, Deload
        
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Previous Value")]
        public decimal PreviousValue { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "New Value")]
        public decimal NewValue { get; set; }
        
        [StringLength(500)]
        [Display(Name = "Reason")]
        public string Reason { get; set; }
        
        [Display(Name = "Applied Automatically")]
        public bool AppliedAutomatically { get; set; } = true;
        
        [Display(Name = "Coach Override")]
        public bool CoachOverride { get; set; } = false;
        
        // Navigation properties
        public ProgressionRule ProgressionRule { get; set; }
        
        [ForeignKey("WorkoutSessionId")]
        public WorkoutSession WorkoutSession { get; set; }
    }
}