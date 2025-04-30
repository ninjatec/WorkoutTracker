using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using System;

namespace WorkoutTrackerWeb.ViewModels
{
    public class QuickWorkoutViewModel
    {
        // Active session information
        public Session CurrentSession { get; set; }
        
        // For adding new sets
        [Display(Name = "Exercise")]
        [Required]
        public int ExerciseTypeId { get; set; }
        
        [Display(Name = "Set Type")]
        [Required]
        public int SettypeId { get; set; }
        
        [Display(Name = "Weight (kg)")]
        [Required]
        [Range(0, 1000)]
        public decimal Weight { get; set; } = 0;
        
        [Display(Name = "Number of Reps")]
        [Required]
        [Range(1, 100)]
        public int NumberReps { get; set; } = 1;
        
        [Display(Name = "All Reps Successful")]
        public bool AllSuccessful { get; set; } = true;
        
        // For creating a new session
        [Display(Name = "Session Name")]
        [StringLength(50)]
        public string NewSessionName { get; set; }
        
        // For finishing a session
        [Display(Name = "End Time")]
        public DateTime? EndTime { get; set; } = DateTime.Now;
        
        // For UI display
        public bool HasActiveSession { get; set; }
        public List<Set> RecentSets { get; set; } = new List<Set>();
        public List<ExerciseTypeWithUseCount> RecentExercises { get; set; } = new List<ExerciseTypeWithUseCount>();
        public List<ExerciseType> FavoriteExercises { get; set; } = new List<ExerciseType>();
        
        // Dropdowns for the UI
        public SelectList ExerciseTypeSelectList { get; set; }
        public SelectList SetTypeSelectList { get; set; }
        
        // Selected muscle group for filtering
        [Display(Name = "Muscle Group")]
        public string SelectedMuscleGroup { get; set; }
        
        // Common muscle groups for filtering
        public List<string> MuscleGroups { get; } = new List<string>
        {
            "abdominals", "abductors", "adductors", "biceps", "calves", 
            "chest", "forearms", "glutes", "hamstrings", "lats", 
            "lower_back", "middle_back", "neck", "quadriceps", 
            "traps", "triceps"
        };
        
        // Feedback messages
        public string StatusMessage { get; set; }
    }
}