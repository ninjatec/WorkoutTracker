using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.ViewModels;

namespace WorkoutTrackerWeb.Pages.Workouts
{
    [Authorize]
    public class QuickWorkoutModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly QuickWorkoutService _quickWorkoutService;
        private readonly ExerciseSelectionService _exerciseSelectionService;
        private readonly ILogger<QuickWorkoutModel> _logger;

        public QuickWorkoutModel(
            WorkoutTrackerWebContext context,
            QuickWorkoutService quickWorkoutService,
            ExerciseSelectionService exerciseSelectionService,
            ILogger<QuickWorkoutModel> logger)
        {
            _context = context;
            _quickWorkoutService = quickWorkoutService;
            _exerciseSelectionService = exerciseSelectionService;
            _logger = logger;
        }

        [BindProperty]
        public QuickWorkoutViewModel QuickWorkout { get; set; } = new QuickWorkoutViewModel();

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string muscleGroup = null)
        {
            await CheckForActiveSessionAsync();
            await PopulateFormDataAsync(muscleGroup);
            return Page();
        }

        public async Task<IActionResult> OnPostCreateSessionAsync(bool finishCurrent = false)
        {
            try
            {
                if (finishCurrent)
                {
                    await CheckForActiveSessionAsync();
                    
                    if (QuickWorkout.HasActiveSession && QuickWorkout.CurrentSession != null)
                    {
                        await _quickWorkoutService.FinishQuickWorkoutSessionAsync(
                            QuickWorkout.CurrentSession.WorkoutSessionId);
                    }
                }
                
                var sessionName = Request.Form["NewSessionName"].ToString();
                if (string.IsNullOrEmpty(sessionName))
                {
                    sessionName = GenerateDefaultSessionName();
                }
                
                DateTime? startTime = null;
                if (DateTime.TryParse(Request.Form["StartTime"].ToString(), out DateTime parsedTime))
                {
                    startTime = parsedTime;
                }
                
                var workoutSession = await _quickWorkoutService.CreateQuickWorkoutSessionAsync(sessionName, startTime);
                
                QuickWorkout.CurrentSession = workoutSession;
                QuickWorkout.HasActiveSession = true;
                
                StatusMessage = "New workout session created successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quick workout session");
                ModelState.AddModelError(string.Empty, "Error creating workout session.");
                await PopulateFormDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAddSetAsync()
        {
            if (!ModelState.IsValid)
            {
                await CheckForActiveSessionAsync();
                await PopulateFormDataAsync();
                return Page();
            }
            
            try
            {
                await CheckForActiveSessionAsync();
                
                if (QuickWorkout.CurrentSession == null || !QuickWorkout.HasActiveSession)
                {
                    StatusMessage = "No active session found. Please create one first.";
                    return RedirectToPage();
                }
                
                var workoutExercise = await _quickWorkoutService.AddQuickWorkoutExerciseAsync(
                    QuickWorkout.CurrentSession.WorkoutSessionId,
                    QuickWorkout.ExerciseTypeId,
                    QuickWorkout.SettypeId,
                    QuickWorkout.Weight,
                    QuickWorkout.NumberReps);
                
                QuickWorkout.LastAddedExercise = workoutExercise;
                StatusMessage = "Set added successfully.";
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding set to workout");
                ModelState.AddModelError(string.Empty, "Error adding set to workout.");
                await PopulateFormDataAsync();
                return Page();
            }
        }

        private async Task CheckForActiveSessionAsync()
        {
            var hasActiveSession = await _quickWorkoutService.HasActiveQuickWorkoutAsync();
            QuickWorkout.HasActiveSession = hasActiveSession;
            
            if (hasActiveSession)
            {
                QuickWorkout.CurrentSession = await _quickWorkoutService.GetLatestQuickWorkoutSessionAsync();
                
                if (QuickWorkout.CurrentSession != null)
                {
                    QuickWorkout.RecentWorkoutSets = await _context.WorkoutSets
                        .Include(ws => ws.WorkoutExercise)
                            .ThenInclude(we => we.ExerciseType)
                        .Include(ws => ws.WorkoutExercise)
                            .ThenInclude(we => we.WorkoutSession)
                        .Where(ws => ws.WorkoutExercise.WorkoutSessionId == QuickWorkout.CurrentSession.WorkoutSessionId)
                        .OrderByDescending(ws => ws.WorkoutSetId)
                        .Take(5)
                        .ToListAsync();
                }
            }
        }

        private async Task PopulateFormDataAsync(string muscleGroup = null)
        {
            // Get exercises by muscle group or all exercises if no muscle group specified
            if (!string.IsNullOrEmpty(muscleGroup))
            {
                try 
                {
                    var exercisesWithMuscles = await _exerciseSelectionService.GetExercisesByMuscleGroupAsync(muscleGroup);
                    QuickWorkout.ExerciseTypes = exercisesWithMuscles.Select(e => e.ExerciseType).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting exercises by muscle group: {MuscleGroup}", muscleGroup);
                    // Fallback to getting all exercise types if the muscle group query fails
                    QuickWorkout.ExerciseTypes = await _context.ExerciseType
                        .OrderBy(e => e.Name)
                        .ToListAsync();
                }
            }
            else 
            {
                QuickWorkout.ExerciseTypes = await _context.ExerciseType
                    .OrderBy(e => e.Name)
                    .ToListAsync();
            }
            
            // Get set types
            QuickWorkout.SetTypes = await _context.Settype
                .OrderBy(s => s.Name)
                .ToListAsync();

            // Get all available muscle groups
            try
            {
                QuickWorkout.MuscleGroups = await _exerciseSelectionService.GetAllMuscleGroupsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all muscle groups");
                QuickWorkout.MuscleGroups = new List<string>();
            }
            
            // Get recent and favorite exercises
            try
            {
                QuickWorkout.RecentExercises = await _quickWorkoutService.GetRecentExercisesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent exercises");
                QuickWorkout.RecentExercises = new List<ExerciseTypeWithUseCount>();
            }
            
            try
            {
                QuickWorkout.FavoriteExercises = await _quickWorkoutService.GetFavoriteExercisesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting favorite exercises");
                QuickWorkout.FavoriteExercises = new List<ExerciseType>();
            }

            // Set the selected muscle group
            QuickWorkout.SelectedMuscleGroup = muscleGroup;
        }

        private string GenerateDefaultSessionName()
        {
            return $"Quick Workout {DateTime.Now:yyyy-MM-dd HH:mm}";
        }
    }

    public class QuickWorkoutViewModel
    {
        public bool HasActiveSession { get; set; }
        public WorkoutSession CurrentSession { get; set; }
        public WorkoutExercise LastAddedExercise { get; set; }
        public List<WorkoutSet> RecentWorkoutSets { get; set; } = new List<WorkoutSet>();
        
        public int ExerciseTypeId { get; set; }
        public int SettypeId { get; set; }
        public decimal Weight { get; set; }
        public int NumberReps { get; set; }
        
        public List<ExerciseType> ExerciseTypes { get; set; } = new List<ExerciseType>();
        public List<Settype> SetTypes { get; set; } = new List<Settype>();
        public List<string> MuscleGroups { get; set; } = new List<string>();
        public List<ExerciseTypeWithUseCount> RecentExercises { get; set; } = new List<ExerciseTypeWithUseCount>();
        public List<ExerciseType> FavoriteExercises { get; set; } = new List<ExerciseType>();
        
        // Track current muscle group filter
        public string SelectedMuscleGroup { get; set; }
    }
}