using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Models.Api;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.ExerciseTypes
{
    [Authorize(Roles = "Admin")]
    public class ApiImportModel : PageModel
    {
        private readonly ExerciseApiService _apiService;
        private readonly ExerciseTypeService _exerciseTypeService;
        private readonly ILogger<ApiImportModel> _logger;

        public ApiImportModel(
            ExerciseApiService apiService,
            ExerciseTypeService exerciseTypeService,
            ILogger<ApiImportModel> logger)
        {
            _apiService = apiService;
            _exerciseTypeService = exerciseTypeService;
            _logger = logger;
        }

        [BindProperty]
        public ExerciseSearchParams SearchParams { get; set; } = new ExerciseSearchParams();

        public IEnumerable<ExerciseApiResponse> SearchResults { get; set; } = Enumerable.Empty<ExerciseApiResponse>();

        public bool IsSearched { get; set; }
        public bool IsError { get; set; }
        public string StatusMessage { get; set; }

        public void OnGet()
        {
            // Initialize the page
            IsSearched = false;
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            try
            {
                IsSearched = true;
                SearchResults = await _apiService.SearchExercisesAsync(SearchParams);
                
                _logger.LogInformation("API search completed with {Count} results", SearchResults.Count());
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching exercises from API");
                StatusMessage = $"Error searching exercises: {ex.Message}";
                IsError = true;
                IsSearched = false;
                return Page();
            }
        }

        public async Task<IActionResult> OnPostImportAsync(string name, string type, string muscle, 
                                                    string difficulty, string equipment, string instructions)
        {
            try
            {
                // Create the API exercise response manually from form values
                var apiExercise = new ExerciseApiResponse
                {
                    Name = name,
                    Type = type,
                    Muscle = muscle,
                    Difficulty = difficulty,
                    Equipment = equipment,
                    Instructions = instructions
                };
                
                // Import the exercise
                var existingExercise = await _exerciseTypeService.SearchExerciseTypesAsync(name)
                    .ContinueWith(t => t.Result.FirstOrDefault(e => e.Name == name));
                
                if (existingExercise != null)
                {
                    if (existingExercise.IsFromApi)
                    {
                        await _exerciseTypeService.UpdateExerciseFromApiAsync(existingExercise.ExerciseTypeId, apiExercise);
                        StatusMessage = $"Updated exercise '{name}' successfully.";
                    }
                    else
                    {
                        StatusMessage = $"Exercise '{name}' already exists as a manually created exercise and cannot be overwritten.";
                        IsError = true;
                    }
                }
                else
                {
                    await _exerciseTypeService.AddExerciseFromApiAsync(apiExercise);
                    StatusMessage = $"Imported exercise '{name}' successfully.";
                }
                
                // Keep search results displayed
                IsSearched = true;
                SearchResults = await _apiService.SearchExercisesAsync(SearchParams);
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing exercise {Name}", name);
                StatusMessage = $"Error importing exercise: {ex.Message}";
                IsError = true;
                
                // Keep search results displayed
                IsSearched = true;
                SearchResults = await _apiService.SearchExercisesAsync(SearchParams);
                
                return Page();
            }
        }

        public async Task<IActionResult> OnPostImportAllAsync(bool importAll)
        {
            if (!importAll)
            {
                return RedirectToPage();
            }
            
            try
            {
                var result = await _exerciseTypeService.ImportExercisesFromApiAsync(SearchParams);
                
                StatusMessage = $"Import completed: {result.added} exercises added, {result.updated} updated, {result.skipped} skipped.";
                
                // Keep search results displayed
                IsSearched = true;
                SearchResults = await _apiService.SearchExercisesAsync(SearchParams);
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing all exercises");
                StatusMessage = $"Error importing exercises: {ex.Message}";
                IsError = true;
                
                // Keep search results displayed
                IsSearched = true;
                SearchResults = await _apiService.SearchExercisesAsync(SearchParams);
                
                return Page();
            }
        }
    }
}