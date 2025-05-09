using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models
{
    /// <summary>
    /// Represents an exercise performed during a workout session
    /// </summary>
    public class WorkoutExercise
    {
        public int WorkoutExerciseId { get; set; }
        
        // Foreign key for WorkoutSession
        public int WorkoutSessionId { get; set; }
        
        // Foreign key for ExerciseType
        public int ExerciseTypeId { get; set; }
        
        // Foreign key for Equipment (optional)
        public int? EquipmentId { get; set; }
        
        [Display(Name = "Sequence Number")]
        public int SequenceNum { get; set; }
        
        [Display(Name = "Order Index")]
        public int OrderIndex { get; set; }
        
        [StringLength(100)]
        [Display(Name = "Notes")]
        public string Notes { get; set; } = "";
        
        [Display(Name = "Start Time")]
        public DateTime? StartTime { get; set; }
        
        [Display(Name = "End Time")]
        public DateTime? EndTime { get; set; }
        
        [Display(Name = "Rest Period (seconds)")]
        public int? RestPeriodSeconds { get; set; }
        
        // Navigation properties - Use ForeignKey attribute to be explicit
        [ForeignKey(nameof(WorkoutSessionId))]
        public virtual WorkoutSession WorkoutSession { get; set; }
        
        // Remove ForeignKey attribute to rely on convention and DbContext configuration
        // This prevents conflicting with the shadow property configuration in the DbContext
        public virtual ExerciseType ExerciseType { get; set; }
        
        [ForeignKey(nameof(EquipmentId))]
        public virtual Equipment Equipment { get; set; }
        
        // Collection navigation properties
        public virtual ICollection<WorkoutSet> WorkoutSets { get; set; } = new List<WorkoutSet>();
        
        // Compatibility property for code that expects Sets
        [NotMapped]
        public ICollection<WorkoutSet> Sets => WorkoutSets;
    }
    
    /// <summary>
    /// Represents equipment used for exercises
    /// </summary>
    public class Equipment
    {
        public int EquipmentId { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string Name { get; set; } = "";
        
        [StringLength(200)]
        [Display(Name = "Description")]
        public string Description { get; set; } = "";
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }
}