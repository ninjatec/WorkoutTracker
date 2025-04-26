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
            
            return Page();
        }

        public async Task<IActionResult> OnPostCreateSessionAsync()
        {
            try
            {
                // Create a new quick workout session
                var session = await _quickWorkoutService.CreateQuickWorkoutSessionAsync(
                    QuickWorkout.NewSessionName);
                
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
                if (QuickWorkout.CurrentSession == null)
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
                
            QuickWorkout.SetTypeSelectList = new SelectList(
                await _quickWorkoutService.GetSetTypesAsync(),
                "SettypeId", "Name");
        }
    }
}