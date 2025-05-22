using System;
using System.ComponentModel.DataAnnotations;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.ViewModels
{
    public class WorkoutSetViewModel
    {
        public int WorkoutSetId { get; set; }
        
        [Required]
        public int WorkoutExerciseId { get; set; }
        
        public int? SettypeId { get; set; }
        
        [Display(Name = "Set Number")]
        public int SetNumber { get; set; }
        
        [Range(0, 1000, ErrorMessage = "Reps must be between 0 and 1000")]
        public int? Reps { get; set; }
        
        [Range(0, 10000, ErrorMessage = "Weight must be between 0 and 10000 kg")]
        public decimal? Weight { get; set; }
        
        public string Notes { get; set; }
        
        public string ExerciseName { get; set; }
        
        public string SetTypeName { get; set; }
        
        // Format the weight and reps for display
        public string FormattedWeight => Weight.HasValue ? $"{Weight:N1}" : "-";
        public string FormattedReps => Reps.HasValue ? Reps.ToString() : "-";
        
        // Convert from model to view model
        public static WorkoutSetViewModel FromModel(WorkoutSet model)
        {
            if (model == null)
                return null;
                
            return new WorkoutSetViewModel
            {
                WorkoutSetId = model.WorkoutSetId,
                WorkoutExerciseId = model.WorkoutExerciseId,
                SettypeId = model.SettypeId,
                SetNumber = model.SetNumber,
                Reps = model.Reps,
                Weight = model.Weight,
                Notes = model.Notes,
                ExerciseName = model.WorkoutExercise?.ExerciseType?.Name,
                SetTypeName = model.Settype?.Name
            };
        }
        
        // Convert to model
        public WorkoutSet ToModel()
        {
            return new WorkoutSet
            {
                WorkoutSetId = WorkoutSetId,
                WorkoutExerciseId = WorkoutExerciseId,
                SettypeId = SettypeId,
                SetNumber = SetNumber,
                Reps = Reps,
                Weight = Weight,
                Notes = Notes ?? string.Empty,
                Timestamp = DateTime.Now,
                Status = "Active"
            };
        }
    }
}
