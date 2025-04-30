using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkoutTrackerWeb.Models.Coaching
{
    /// <summary>
    /// Represents a possible substitution relationship between exercises
    /// </summary>
    public class ExerciseSubstitution
    {
        public int ExerciseSubstitutionId { get; set; }
        
        // Foreign key for primary ExerciseType
        public int PrimaryExerciseTypeId { get; set; }
        
        // Foreign key for substitute ExerciseType
        public int SubstituteExerciseTypeId { get; set; }
        
        [Range(1, 100)]
        [Display(Name = "Equivalence Percentage")]
        public int EquivalencePercentage { get; set; } = 100;
        
        [StringLength(50)]
        [Display(Name = "Movement Pattern")]
        public string MovementPattern { get; set; } // Push, Pull, Squat, Hinge, Lunge, etc.
        
        [StringLength(200)]
        [Display(Name = "Equipment Required")]
        public string EquipmentRequired { get; set; } // Comma-separated list
        
        [StringLength(200)]
        [Display(Name = "Muscles Targeted")]
        public string MusclesTargeted { get; set; } // Comma-separated list
        
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        [Display(Name = "Created By Coach")]
        public int CreatedByCoachId { get; set; }
        
        [Display(Name = "Is Global")]
        public bool IsGlobal { get; set; } = false;
        
        [StringLength(500)]
        [Display(Name = "Notes")]
        public string Notes { get; set; }
        
        // Navigation properties
        [ForeignKey("PrimaryExerciseTypeId")]
        public ExerciseType PrimaryExercise { get; set; }
        
        [ForeignKey("SubstituteExerciseTypeId")]
        public ExerciseType SubstituteExercise { get; set; }
        
        [ForeignKey("CreatedByCoachId")]
        public User CreatedByCoach { get; set; }
    }
    
    /// <summary>
    /// Represents client-specific exercise exclusions (e.g., due to injuries)
    /// </summary>
    public class ClientExerciseExclusion
    {
        public int ClientExerciseExclusionId { get; set; }
        
        // Foreign keys
        public int ClientUserId { get; set; }
        public int ExerciseTypeId { get; set; }
        
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Now;
        
        [Display(Name = "End Date")]
        public DateTime? EndDate { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Reason")]
        public string Reason { get; set; }
        
        [StringLength(500)]
        [Display(Name = "Notes")]
        public string Notes { get; set; }
        
        [Display(Name = "Created By Coach")]
        public int? CreatedByCoachId { get; set; }
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        [ForeignKey("ClientUserId")]
        public User Client { get; set; }
        
        public ExerciseType ExerciseType { get; set; }
        
        [ForeignKey("CreatedByCoachId")]
        public User CreatedByCoach { get; set; }
    }
    
    /// <summary>
    /// Tracks equipment available to a client for intelligent substitutions
    /// </summary>
    public class ClientEquipment
    {
        public int ClientEquipmentId { get; set; }
        
        // Foreign key for client User
        public int ClientUserId { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Equipment Name")]
        public string EquipmentName { get; set; }
        
        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; }
        
        [Display(Name = "Is Available")]
        public bool IsAvailable { get; set; } = true;
        
        [Display(Name = "Location")]
        public string Location { get; set; } // Home, Gym, etc.
        
        // Navigation property
        [ForeignKey("ClientUserId")]
        public User Client { get; set; }
    }
}