using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.ExerciseTypes
{
    [Authorize(Roles = "Admin")]
    public class PendingSelectionsModel : PageModel
    {
        private readonly ExerciseTypeService _exerciseService;
        private readonly ILogger<PendingSelectionsModel> _logger;

        public PendingSelectionsModel(
            ExerciseTypeService exerciseService,
            ILogger<PendingSelectionsModel> logger)
        {
            _exerciseService = exerciseService;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public List<PendingExerciseSelection> PendingSelections { get; private set; }
        
        public int PendingSelectionsCount { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Get all pending selections regardless of job ID
            PendingSelections = await _exerciseService.GetAllPendingSelectionsAsync();
            PendingSelectionsCount = PendingSelections.Count;
            
            return Page();
        }
        
        public async Task<IActionResult> OnPostResolveSelectionAsync(int pendingSelectionId, int selectedApiExerciseIndex)
        {
            try
            {
                var updatedExercise = await _exerciseService.ResolvePendingSelectionWithoutJobAsync(pendingSelectionId, selectedApiExerciseIndex);
                
                if (updatedExercise != null)
                {
                    StatusMessage = $"Successfully updated exercise '{updatedExercise.Name}' with selected data.";
                    
                    // Reload the remaining pending selections
                    PendingSelections = await _exerciseService.GetAllPendingSelectionsAsync();
                    PendingSelectionsCount = PendingSelections.Count;
                    
                    if (PendingSelectionsCount == 0)
                    {
                        StatusMessage += " All selections have been resolved.";
                    }
                }
                else
                {
                    StatusMessage = "Failed to update exercise with selected data.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving pending selection {PendingSelectionId}", pendingSelectionId);
                StatusMessage = $"Error: {ex.Message}";
            }
            
            return Page();
        }
    }
}