using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.ExerciseTypes
{
    [Authorize(Roles = "Admin")]
    public class EnrichExercisesModel : PageModel
    {
        private readonly ExerciseTypeService _exerciseService;
        private readonly ILogger<EnrichExercisesModel> _logger;

        public EnrichExercisesModel(
            ExerciseTypeService exerciseService,
            ILogger<EnrichExercisesModel> logger)
        {
            _exerciseService = exerciseService;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public (int found, int enriched, int failed)? EnrichmentResult { get; private set; }

        public void OnGet()
        {
            // No action needed for GET
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                _logger.LogInformation("Starting bulk exercise enrichment process");
                
                // Enrich exercises with empty fields
                var result = await _exerciseService.EnrichExercisesWithEmptyFieldsAsync();
                
                EnrichmentResult = result;
                
                if (result.enriched > 0)
                {
                    StatusMessage = $"Success: Enriched {result.enriched} exercises with API data.";
                }
                else if (result.found == 0)
                {
                    StatusMessage = "No exercises with empty fields were found.";
                }
                else
                {
                    StatusMessage = $"Found {result.found} exercises with empty fields, but could not enrich any of them.";
                }
                
                _logger.LogInformation("Bulk enrichment completed: found {Found}, enriched {Enriched}, failed {Failed}", 
                    result.found, result.enriched, result.failed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk exercise enrichment");
                StatusMessage = $"Error: {ex.Message}";
            }
            
            return Page();
        }
    }
}