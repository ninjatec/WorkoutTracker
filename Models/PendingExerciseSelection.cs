using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WorkoutTrackerWeb.Models.Api;

namespace WorkoutTrackerWeb.Models
{
    /// <summary>
    /// Represents an exercise awaiting user selection from similar API exercises
    /// </summary>
    public class PendingExerciseSelection
    {
        public int Id { get; set; }
        
        [Required]
        public string JobId { get; set; } = string.Empty;
        
        [Required]
        public int ExerciseTypeId { get; set; }
        
        [Required]
        public string ExerciseName { get; set; } = string.Empty;
        
        [Required]
        public string ApiResults { get; set; } = string.Empty;
        
        public bool IsResolved { get; set; } = false;
        
        public int? SelectedApiExerciseIndex { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [DataType(DataType.DateTime)]
        public DateTime? ResolvedAt { get; set; }
        
        // Non-persisted properties for UI display
        [NotMapped]
        public List<ExerciseApiResponse> ApiExerciseOptions { get; set; } = new List<ExerciseApiResponse>();
        
        [NotMapped]
        public ExerciseType? ExerciseType { get; set; }
    }
}