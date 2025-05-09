using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Calculations;
using WorkoutTrackerWeb.ViewModels;

namespace WorkoutTrackerWeb.Pages.Sessions
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IVolumeCalculationService _volumeCalculationService;
        private readonly ICalorieCalculationService _calorieCalculationService;
        private readonly IUserService _userService;

        public DetailsModel(
            WorkoutTrackerWebContext context,
            IVolumeCalculationService volumeCalculationService,
            ICalorieCalculationService calorieCalculationService,
            IUserService userService)
        {
            _context = context;
            _volumeCalculationService = volumeCalculationService;
            _calorieCalculationService = calorieCalculationService;
            _userService = userService;
        }

        public WorkoutSession WorkoutSession { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalCalories { get; set; }
        public Dictionary<string, decimal> VolumeByExercise { get; set; }
        public List<SelectListItem> SortOptions { get; set; }
        public List<ExerciseSummary> ExerciseSummaries { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }

            // Fetch the workout session without including the related entities
            var workoutSession = await _context.WorkoutSessions
                .FirstOrDefaultAsync(m => m.WorkoutSessionId == id && m.UserId == currentUserId);
                
            if (workoutSession == null)
            {
                return NotFound();
            }
            
            // Now fetch the related entities separately to avoid the EF Core relation issues
            // Get all workout exercises for this session
            var workoutExercises = await _context.WorkoutExercises
                .Where(we => we.WorkoutSessionId == workoutSession.WorkoutSessionId)
                .OrderBy(we => we.OrderIndex)
                .ToListAsync();
                
            // Get all exercise types for these exercises
            var exerciseTypeIds = workoutExercises
                .Select(we => we.ExerciseTypeId)
                .Distinct()
                .ToList();
                
            var exerciseTypes = await _context.ExerciseType
                .Where(et => exerciseTypeIds.Contains(et.ExerciseTypeId))
                .ToListAsync();
                
            // Get all workout sets for these exercises
            var workoutExerciseIds = workoutExercises
                .Select(we => we.WorkoutExerciseId)
                .ToList();
                
            var workoutSets = await _context.WorkoutSets
                .Where(ws => workoutExerciseIds.Contains(ws.WorkoutExerciseId))
                .OrderBy(ws => ws.SetNumber)
                .ToListAsync();
                
            // Get all equipment used in these exercises
            var equipmentIds = workoutExercises
                .Where(we => we.EquipmentId.HasValue)
                .Select(we => we.EquipmentId.Value)
                .Distinct()
                .ToList();
                
            var equipment = await _context.Set<Equipment>()
                .Where(e => equipmentIds.Contains(e.EquipmentId))
                .ToListAsync();
                
            // Now manually connect the entities
            foreach (var exercise in workoutExercises)
            {
                // Link exercise type
                exercise.ExerciseType = exerciseTypes
                    .FirstOrDefault(et => et.ExerciseTypeId == exercise.ExerciseTypeId);
                    
                // Link equipment
                if (exercise.EquipmentId.HasValue)
                {
                    exercise.Equipment = equipment
                        .FirstOrDefault(e => e.EquipmentId == exercise.EquipmentId);
                }
                
                // Link workout sets
                exercise.WorkoutSets = workoutSets
                    .Where(ws => ws.WorkoutExerciseId == exercise.WorkoutExerciseId)
                    .ToList();
            }
            
            // Finally, connect exercises to session
            workoutSession.WorkoutExercises = workoutExercises;
            WorkoutSession = workoutSession;
            
            // Calculate total volume
            TotalVolume = _volumeCalculationService.CalculateWorkoutSessionVolume(workoutSession);

            // Calculate volume by exercise
            VolumeByExercise = _volumeCalculationService.CalculateSessionVolume(workoutSession);

            // Calculate total calories
            TotalCalories = _calorieCalculationService.CalculateCalories(workoutSession);
                
            // Create a summary of the exercises in this session
            ExerciseSummaries = workoutSession.WorkoutExercises
                .Select(we => new ExerciseSummary
                {
                    ExerciseName = we.ExerciseType?.Name ?? "Unknown Exercise",
                    TotalSets = we.WorkoutSets.Count,
                    MaxWeight = we.WorkoutSets.Any(s => s.Weight.HasValue) 
                        ? we.WorkoutSets.Max(s => s.Weight ?? 0) 
                        : 0,
                    TotalReps = we.WorkoutSets.Sum(s => s.Reps ?? 0),
                    VolumeLifted = we.WorkoutSets.Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0))
                })
                .ToList();
                
            return Page();
        }
    }
}
