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
        private readonly ILogger<QuickWorkoutModel> _logger;

        public QuickWorkoutModel(
            WorkoutTrackerWebContext context,
            QuickWorkoutService quickWorkoutService,
            ILogger<QuickWorkoutModel> logger)
        {
            _context = context;
            _quickWorkoutService = quickWorkoutService;
            _logger = logger;
        }

        [BindProperty]
        public QuickWorkoutViewModel QuickWorkout { get; set; } = new QuickWorkoutViewModel();

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string muscleGroup = null)
        {
            // Get active session if one exists
            await CheckForActiveSessionAsync();
            
            // Set selected muscle group if provided
            QuickWorkout.SelectedMuscleGroup = muscleGroup;
            
            // Populate UI data
            await PopulateFormDataAsync();
            
            // Set status message from TempData
            QuickWorkout.StatusMessage = StatusMessage;
            
            // Generate default session name if none exists
            if (string.IsNullOrEmpty(QuickWorkout.NewSessionName))
            {
                QuickWorkout.NewSessionName = GenerateDefaultSessionName();
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostCreateSessionAsync(bool finishCurrent = false)
        {
            try
            {
                // Check if we need to finish an existing session first
                if (finishCurrent)
                {
                    await CheckForActiveSessionAsync();
                    
                    if (QuickWorkout.HasActiveSession && QuickWorkout.CurrentSession != null)
                    {
                        // Finish the current workout session
                        await _quickWorkoutService.FinishQuickWorkoutSessionAsync(
                            QuickWorkout.CurrentSession.SessionId);
                    }
                }
                
                // Get the session name from the form, use default if empty
                var sessionName = Request.Form["NewSessionName"].ToString();
                if (string.IsNullOrEmpty(sessionName))
                {
                    sessionName = GenerateDefaultSessionName();
                }
                
                // Get the start time from the form if available
                DateTime? startTime = null;
                if (Request.Form["QuickWorkout.StartTime"].Count > 0)
                {
                    if (DateTime.TryParse(Request.Form["QuickWorkout.StartTime"], out DateTime parsedStartTime))
                    {
                        startTime = parsedStartTime;
                    }
                }
                
                // Create a new quick workout session with the specified start time
                var session = await _quickWorkoutService.CreateQuickWorkoutSessionAsync(sessionName, startTime);
                
                // Set success message
                StatusMessage = $"Successfully created new quick workout: {session.Name}";
                
                // Redirect to the same page to refresh
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quick workout session");
                StatusMessage = $"Error creating session: {ex.Message}";
                return RedirectToPage();
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
                // Check for active session BEFORE trying to access QuickWorkout.CurrentSession
                await CheckForActiveSessionAsync();
                
                if (QuickWorkout.CurrentSession == null || !QuickWorkout.HasActiveSession)
                {
                    StatusMessage = "No active session found. Please create one first.";
                    return RedirectToPage();
                }
                
                // Add the set to the session
                var set = await _quickWorkoutService.AddQuickSetAsync(
                    QuickWorkout.CurrentSession.SessionId,
                    QuickWorkout.ExerciseTypeId,
                    QuickWorkout.SettypeId,
                    QuickWorkout.Weight,
                    QuickWorkout.NumberReps);
                
                // Add the reps
                await _quickWorkoutService.AddRepsToSetAsync(
                    set.SetId,
                    QuickWorkout.Weight,
                    QuickWorkout.NumberReps,
                    QuickWorkout.AllSuccessful);
                
                // Set success message
                StatusMessage = "Set added successfully!";
                
                // Redirect to refresh the page
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding quick set");
                StatusMessage = $"Error adding set: {ex.Message}";
                return RedirectToPage();
            }
        }
        
        public async Task<IActionResult> OnPostFinishSessionAsync()
        {
            try
            {
                // Check for active session
                await CheckForActiveSessionAsync();
                
                if (QuickWorkout.CurrentSession == null || !QuickWorkout.HasActiveSession)
                {
                    StatusMessage = "No active session found to finish.";
                    return RedirectToPage();
                }
                
                // Get the session details before finishing it
                var sessionName = QuickWorkout.CurrentSession.Name;
                var sessionId = QuickWorkout.CurrentSession.SessionId;
                
                // Use the provided end time from the form if available, otherwise use current time
                DateTime endTime = DateTime.Now;
                if (Request.Form["QuickWorkout.EndTime"].Count > 0)
                {
                    if (DateTime.TryParse(Request.Form["QuickWorkout.EndTime"], out DateTime parsedEndTime))
                    {
                        endTime = parsedEndTime;
                    }
                }
                
                // Finish the current workout session with the end time
                await _quickWorkoutService.FinishQuickWorkoutSessionAsync(sessionId, endTime);
                
                // Force clearing of the current session
                QuickWorkout.HasActiveSession = false;
                QuickWorkout.CurrentSession = null;
                QuickWorkout.RecentSets = new List<Set>();
                
                // Set success message
                StatusMessage = $"Successfully finished workout: {sessionName}";
                
                // Redirect to the same page with a parameter to prevent caching
                return RedirectToPage(new { t = DateTime.Now.Ticks });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finishing workout session");
                StatusMessage = $"Error finishing session: {ex.Message}";
                return RedirectToPage();
            }
        }
        
        public async Task<IActionResult> OnGetFilterByMuscleGroupAsync(string muscleGroup)
        {
            // Redirect to the page with the muscle group filter
            return RedirectToPage(new { muscleGroup });
        }
        
        public IActionResult OnGetViewSession(int sessionId)
        {
            // Redirect to session details
            return RedirectToPage("/Sessions/Details", new { id = sessionId });
        }

        /// <summary>
        /// API handler that returns exercises for a specific muscle group in JSON format
        /// </summary>
        public async Task<IActionResult> OnGetExercisesByMuscleGroupAsync(string muscleGroup)
        {
            // Check if this is an AJAX request
            if (!Request.Headers["X-Requested-With"].Equals("XMLHttpRequest"))
            {
                return RedirectToPage(new { muscleGroup });
            }

            if (string.IsNullOrEmpty(muscleGroup))
            {
                return new JsonResult(new List<object>());
            }

            // Query the database for exercises with the given muscle group
            var exercises = await _context.ExerciseType
                .Where(e => e.Muscle.Contains(muscleGroup))
                .OrderBy(e => e.Name)
                .Select(e => new 
                {
                    id = e.ExerciseTypeId.ToString(),
                    name = e.Name,
                    muscle = e.Muscle
                })
                .ToListAsync();

            // Return the exercises as JSON
            return new JsonResult(exercises);
        }
        
        private async Task CheckForActiveSessionAsync()
        {
            // Check if there's an active session in progress
            var hasActiveSession = await _quickWorkoutService.HasActiveQuickWorkoutAsync();
            QuickWorkout.HasActiveSession = hasActiveSession;
            
            if (hasActiveSession)
            {
                // Get the latest session
                QuickWorkout.CurrentSession = await _quickWorkoutService.GetLatestQuickWorkoutSessionAsync();
                
                if (QuickWorkout.CurrentSession != null)
                {
                    // Get the latest sets for this session
                    QuickWorkout.RecentSets = await _context.Set
                        .Include(s => s.ExerciseType)
                        .Include(s => s.Settype)
                        .Where(s => s.SessionId == QuickWorkout.CurrentSession.SessionId)
                        .OrderByDescending(s => s.SetId)
                        .Take(5)
                        .ToListAsync();
                }
            }
        }
        
        private async Task PopulateFormDataAsync()
        {
            // Get recent and favorite exercises
            QuickWorkout.RecentExercises = await _quickWorkoutService.GetRecentExercisesAsync(8);
            QuickWorkout.FavoriteExercises = await _quickWorkoutService.GetFavoriteExercisesAsync(8);
            
            // Get filtered exercises by muscle group if selected
            var exerciseQuery = _context.ExerciseType.AsQueryable();
            
            if (!string.IsNullOrEmpty(QuickWorkout.SelectedMuscleGroup))
            {
                exerciseQuery = exerciseQuery.Where(e => e.Muscle.Contains(QuickWorkout.SelectedMuscleGroup));
            }
            
            // Set up dropdown lists
            QuickWorkout.ExerciseTypeSelectList = new SelectList(
                await exerciseQuery.OrderBy(e => e.Name).ToListAsync(),
                "ExerciseTypeId", "Name");
            
            // Get all set types
            var setTypes = await _quickWorkoutService.GetSetTypesAsync();
            
            // Create the select list for set types
            QuickWorkout.SetTypeSelectList = new SelectList(
                setTypes,
                "SettypeId", "Name");
            
            // Set default set type to "Normal" if it exists and no value is already selected
            if (QuickWorkout.SettypeId == 0)
            {
                // Find the "Normal" set type (case insensitive) 
                var normalSetType = setTypes.FirstOrDefault(s => s.Name.Equals("Normal", StringComparison.OrdinalIgnoreCase));
                
                // If found, set it as the default
                if (normalSetType != null)
                {
                    QuickWorkout.SettypeId = normalSetType.SettypeId;
                }
            }
        }
        
        /// <summary>
        /// Generates a default session name based on current date and time
        /// </summary>
        private string GenerateDefaultSessionName()
        {
            var now = DateTime.Now;
            string dayPart;
            
            if (now.Hour >= 5 && now.Hour < 12)
            {
                dayPart = "Morning";
            }
            else if (now.Hour >= 12 && now.Hour < 17)
            {
                dayPart = "Afternoon";
            }
            else if (now.Hour >= 17 && now.Hour < 22)
            {
                dayPart = "Evening";
            }
            else
            {
                dayPart = "Night";
            }
            
            return $"{dayPart} Workout - {now:MMM d}";
        }
    }
}