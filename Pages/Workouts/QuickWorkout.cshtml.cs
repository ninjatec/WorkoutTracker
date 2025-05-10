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
        private readonly UserService _userService;

        public QuickWorkoutModel(
            WorkoutTrackerWebContext context,
            QuickWorkoutService quickWorkoutService,
            ExerciseSelectionService exerciseSelectionService,
            ILogger<QuickWorkoutModel> logger,
            UserService userService)
        {
            _context = context;
            _quickWorkoutService = quickWorkoutService;
            _exerciseSelectionService = exerciseSelectionService;
            _logger = logger;
            _userService = userService;
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
                await PopulateCurrentSessionDataAsync();
            }
        }

        private async Task PopulateCurrentSessionDataAsync()
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null) return;

            // Get the active workout session for this user
            var activeSession = await _context.WorkoutSessions
                .Where(ws => ws.UserId == userId && ws.EndDateTime == null)
                .OrderByDescending(ws => ws.StartDateTime)
                .FirstOrDefaultAsync();

            if (activeSession == null)
            {
                QuickWorkout.HasActiveSession = false;
                QuickWorkout.CurrentSession = null;
                return;
            }

            QuickWorkout.HasActiveSession = true;
            QuickWorkout.CurrentSession = activeSession;

            // Changed: Load workout exercises and related data separately to avoid the ExerciseTypeId1 issue
            // First load workout exercises for the session
            var workoutExercises = await _context.WorkoutExercises
                .Where(we => we.WorkoutSessionId == activeSession.WorkoutSessionId)
                .OrderBy(we => we.OrderIndex)
                .ToListAsync();
                
            if (!workoutExercises.Any()) return;
            
            // Get the exercise type IDs
            var exerciseTypeIds = workoutExercises.Select(we => we.ExerciseTypeId).Distinct().ToList();
            
            // Load exercise types separately
            var exerciseTypes = await _context.ExerciseType
                .Where(et => exerciseTypeIds.Contains(et.ExerciseTypeId))
                .ToListAsync();
                
            // Get workout exercise IDs
            var workoutExerciseIds = workoutExercises.Select(we => we.WorkoutExerciseId).ToList();
            
            // Load workout sets separately including set types
            var workoutSets = await _context.WorkoutSets
                .Include(ws => ws.Settype)
                .Where(ws => workoutExerciseIds.Contains(ws.WorkoutExerciseId))
                .OrderByDescending(ws => ws.Timestamp)
                .Take(10)  // Only get the 10 most recent sets
                .ToListAsync();
                
            // Manually connect the entities
            foreach (var exercise in workoutExercises)
            {
                exercise.ExerciseType = exerciseTypes.FirstOrDefault(et => et.ExerciseTypeId == exercise.ExerciseTypeId);
            }
            
            // Get the most recently added exercise
            var lastExercise = workoutExercises.OrderByDescending(we => we.OrderIndex).FirstOrDefault();
            if (lastExercise != null)
            {
                QuickWorkout.LastAddedExercise = lastExercise;
            }
            
            // Get the recent workout sets and link them to their exercises
            QuickWorkout.RecentWorkoutSets = workoutSets;
            foreach (var set in QuickWorkout.RecentWorkoutSets)
            {
                set.WorkoutExercise = workoutExercises.FirstOrDefault(we => we.WorkoutExerciseId == set.WorkoutExerciseId);
            }
        }

        private async Task PopulateFormDataAsync(string muscleGroup = null)
        {
            try
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
                
                // Set default SetTypeId to "Working" type
                var workingSetType = QuickWorkout.SetTypes.FirstOrDefault(st => st.Name == "Working");
                if (workingSetType != null)
                {
                    QuickWorkout.SettypeId = workingSetType.SettypeId;
                }

                // Get all available muscle groups with improved error handling
                try
                {
                    QuickWorkout.MuscleGroups = await _exerciseSelectionService.GetAllMuscleGroupsAsync();
                    // Additional safety check
                    if (QuickWorkout.MuscleGroups == null)
                    {
                        QuickWorkout.MuscleGroups = new List<string>();
                    }
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
                    if (QuickWorkout.RecentExercises == null)
                    {
                        QuickWorkout.RecentExercises = new List<ExerciseTypeWithUseCount>();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting recent exercises");
                    QuickWorkout.RecentExercises = new List<ExerciseTypeWithUseCount>();
                }
                
                try
                {
                    QuickWorkout.FavoriteExercises = await _quickWorkoutService.GetFavoriteExercisesAsync();
                    if (QuickWorkout.FavoriteExercises == null)
                    {
                        QuickWorkout.FavoriteExercises = new List<ExerciseType>();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting favorite exercises");
                    QuickWorkout.FavoriteExercises = new List<ExerciseType>();
                }

                // Set the selected muscle group
                QuickWorkout.SelectedMuscleGroup = muscleGroup;
            }
            catch (Exception ex)
            {
                // Global exception handler for the entire form data population process
                _logger.LogError(ex, "Unhandled exception in PopulateFormDataAsync");
                
                // Initialize default empty collections to prevent null reference exceptions in the view
                QuickWorkout.ExerciseTypes ??= new List<ExerciseType>();
                QuickWorkout.SetTypes ??= new List<Settype>();
                QuickWorkout.MuscleGroups ??= new List<string>();
                QuickWorkout.RecentExercises ??= new List<ExerciseTypeWithUseCount>();
                QuickWorkout.FavoriteExercises ??= new List<ExerciseType>();
            }
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