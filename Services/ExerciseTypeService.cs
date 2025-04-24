using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Api;

namespace WorkoutTrackerWeb.Services
{
    /// <summary>
    /// Service for managing exercise types, including API Ninjas integration
    /// </summary>
    public class ExerciseTypeService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ExerciseApiService _apiService;
        private readonly ILogger<ExerciseTypeService> _logger;

        public ExerciseTypeService(
            WorkoutTrackerWebContext context,
            ExerciseApiService apiService,
            ILogger<ExerciseTypeService> logger)
        {
            _context = context;
            _apiService = apiService;
            _logger = logger;
        }

        /// <summary>
        /// Searches for exercise types in the database
        /// </summary>
        /// <param name="searchString">Text to search for in exercise name</param>
        /// <returns>A collection of matching exercise types</returns>
        public async Task<IEnumerable<ExerciseType>> SearchExerciseTypesAsync(string searchString)
        {
            if (string.IsNullOrWhiteSpace(searchString))
            {
                return await _context.ExerciseType
                    .OrderBy(e => e.Name)
                    .Take(20)
                    .ToListAsync();
            }

            return await _context.ExerciseType
                .Where(e => e.Name.Contains(searchString))
                .OrderBy(e => e.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Adds a new exercise type from API data
        /// </summary>
        /// <param name="apiExercise">The API exercise data</param>
        /// <returns>The newly created exercise type</returns>
        public async Task<ExerciseType> AddExerciseFromApiAsync(ExerciseApiResponse apiExercise)
        {
            var exerciseType = new ExerciseType
            {
                Name = apiExercise.Name,
                Type = apiExercise.Type,
                Muscle = apiExercise.Muscle,
                Equipment = apiExercise.Equipment,
                Difficulty = apiExercise.Difficulty,
                Instructions = apiExercise.Instructions,
                Description = $"{apiExercise.Name} - {apiExercise.Type} exercise for {apiExercise.Muscle}",
                IsFromApi = true,
                LastUpdated = DateTime.UtcNow
            };

            _context.ExerciseType.Add(exerciseType);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added new exercise from API: {Name}", exerciseType.Name);

            return exerciseType;
        }

        /// <summary>
        /// Updates an existing exercise type with API data
        /// </summary>
        /// <param name="exerciseTypeId">The ID of the exercise to update</param>
        /// <param name="apiExercise">The API exercise data</param>
        /// <returns>The updated exercise type</returns>
        public async Task<ExerciseType> UpdateExerciseFromApiAsync(int exerciseTypeId, ExerciseApiResponse apiExercise)
        {
            var exerciseType = await _context.ExerciseType
                .FirstOrDefaultAsync(e => e.ExerciseTypeId == exerciseTypeId);

            if (exerciseType == null)
            {
                throw new KeyNotFoundException($"Exercise type with ID {exerciseTypeId} not found");
            }

            // Only update if it's an API-sourced exercise
            if (!exerciseType.IsFromApi)
            {
                throw new InvalidOperationException("Cannot update a manually created exercise with API data");
            }

            exerciseType.Name = apiExercise.Name;
            exerciseType.Type = apiExercise.Type;
            exerciseType.Muscle = apiExercise.Muscle;
            exerciseType.Equipment = apiExercise.Equipment;
            exerciseType.Difficulty = apiExercise.Difficulty;
            exerciseType.Instructions = apiExercise.Instructions;
            exerciseType.Description = $"{apiExercise.Name} - {apiExercise.Type} exercise for {apiExercise.Muscle}";
            exerciseType.LastUpdated = DateTime.UtcNow;

            _context.Update(exerciseType);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated exercise from API: {Name}", exerciseType.Name);

            return exerciseType;
        }

        /// <summary>
        /// Imports exercises from the API Ninjas service based on search parameters
        /// </summary>
        /// <param name="searchParams">The search parameters</param>
        /// <returns>Summary of the import operation (added, updated, skipped)</returns>
        public async Task<(int added, int updated, int skipped)> ImportExercisesFromApiAsync(ExerciseSearchParams searchParams)
        {
            var apiExercises = await _apiService.SearchExercisesAsync(searchParams);
            int added = 0, updated = 0, skipped = 0;

            foreach (var apiExercise in apiExercises)
            {
                // Check if this exercise already exists in our database
                var existingExercise = await _context.ExerciseType
                    .FirstOrDefaultAsync(e => e.Name == apiExercise.Name);

                if (existingExercise == null)
                {
                    // Add new exercise
                    await AddExerciseFromApiAsync(apiExercise);
                    added++;
                }
                else if (existingExercise.IsFromApi)
                {
                    // Update existing API-sourced exercise
                    await UpdateExerciseFromApiAsync(existingExercise.ExerciseTypeId, apiExercise);
                    updated++;
                }
                else
                {
                    // Skip manually created exercises
                    skipped++;
                }
            }

            _logger.LogInformation("Exercise import completed: {Added} added, {Updated} updated, {Skipped} skipped",
                added, updated, skipped);

            return (added, updated, skipped);
        }

        /// <summary>
        /// Gets related exercises that target the same muscle group
        /// </summary>
        /// <param name="muscleGroup">The muscle group to find related exercises for</param>
        /// <param name="currentExerciseId">The current exercise ID to exclude from results</param>
        /// <param name="limit">Maximum number of results to return</param>
        /// <returns>A collection of related exercises</returns>
        public async Task<IEnumerable<ExerciseType>> GetRelatedExercisesAsync(string muscleGroup, int currentExerciseId, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(muscleGroup))
            {
                return new List<ExerciseType>();
            }

            return await _context.ExerciseType
                .Where(e => e.Muscle == muscleGroup && e.ExerciseTypeId != currentExerciseId)
                .OrderBy(e => e.Name)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Enriches an existing exercise with data from the API based on its name
        /// </summary>
        /// <param name="exerciseTypeId">ID of the exercise to enrich</param>
        /// <returns>Updated exercise with API data or null if no match found</returns>
        public async Task<ExerciseType> EnrichExistingExerciseFromApiAsync(int exerciseTypeId)
        {
            var exercise = await _context.ExerciseType
                .FirstOrDefaultAsync(e => e.ExerciseTypeId == exerciseTypeId);

            if (exercise == null)
            {
                _logger.LogWarning("Exercise with ID {ExerciseId} not found", exerciseTypeId);
                return null;
            }

            // Search for this exercise by name in the API
            var searchParams = new ExerciseSearchParams { Name = exercise.Name };
            var apiExercises = await _apiService.SearchExercisesAsync(searchParams);

            // Find the closest match by name
            var bestMatch = apiExercises.FirstOrDefault(a => 
                a.Name.Equals(exercise.Name, StringComparison.OrdinalIgnoreCase));

            // If no exact match, try to find a partial match
            if (bestMatch == null && apiExercises.Any())
            {
                bestMatch = apiExercises.First();
                _logger.LogInformation("No exact match found for '{Name}', using closest match '{MatchName}'", 
                    exercise.Name, bestMatch.Name);
            }

            if (bestMatch == null)
            {
                _logger.LogWarning("No API data found for exercise '{Name}'", exercise.Name);
                return null;
            }

            // Fill in any empty fields with data from the API
            if (string.IsNullOrEmpty(exercise.Type))
                exercise.Type = bestMatch.Type;
                
            if (string.IsNullOrEmpty(exercise.Muscle))
                exercise.Muscle = bestMatch.Muscle;
                
            if (string.IsNullOrEmpty(exercise.Equipment))
                exercise.Equipment = bestMatch.Equipment;
                
            if (string.IsNullOrEmpty(exercise.Difficulty))
                exercise.Difficulty = bestMatch.Difficulty;
                
            if (string.IsNullOrEmpty(exercise.Instructions))
                exercise.Instructions = bestMatch.Instructions;
                
            // Update the last updated timestamp
            exercise.LastUpdated = DateTime.UtcNow;
            
            // Don't change the IsFromApi flag as this is still a manually created exercise
            // that's just been enriched with API data
            
            _context.Update(exercise);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Enriched exercise '{Name}' with API data", exercise.Name);
            
            return exercise;
        }

        /// <summary>
        /// Finds and enriches multiple exercises that have empty fields
        /// </summary>
        /// <returns>Summary of the enrichment operation</returns>
        public async Task<(int found, int enriched, int failed)> EnrichExercisesWithEmptyFieldsAsync()
        {
            // Find all exercises with at least one empty field
            var exercises = await _context.ExerciseType
                .Where(e => string.IsNullOrEmpty(e.Type) || 
                            string.IsNullOrEmpty(e.Muscle) || 
                            string.IsNullOrEmpty(e.Equipment) || 
                            string.IsNullOrEmpty(e.Difficulty) || 
                            string.IsNullOrEmpty(e.Instructions))
                .ToListAsync();

            int found = exercises.Count;
            int enriched = 0;
            int failed = 0;

            foreach (var exercise in exercises)
            {
                try
                {
                    // Search for this exercise by name in the API
                    var searchParams = new ExerciseSearchParams { Name = exercise.Name };
                    var apiExercises = await _apiService.SearchExercisesAsync(searchParams);

                    // Find the closest match by name
                    var bestMatch = apiExercises.FirstOrDefault(a => 
                        a.Name.Equals(exercise.Name, StringComparison.OrdinalIgnoreCase));

                    // If no exact match, try to find a partial match
                    if (bestMatch == null && apiExercises.Any())
                    {
                        bestMatch = apiExercises.First();
                    }

                    if (bestMatch == null)
                    {
                        _logger.LogWarning("No API data found for exercise '{Name}'", exercise.Name);
                        failed++;
                        continue;
                    }

                    // Fill in any empty fields with data from the API
                    bool changed = false;
                    
                    if (string.IsNullOrEmpty(exercise.Type))
                    {
                        exercise.Type = bestMatch.Type;
                        changed = true;
                    }
                    
                    if (string.IsNullOrEmpty(exercise.Muscle))
                    {
                        exercise.Muscle = bestMatch.Muscle;
                        changed = true;
                    }
                    
                    if (string.IsNullOrEmpty(exercise.Equipment))
                    {
                        exercise.Equipment = bestMatch.Equipment;
                        changed = true;
                    }
                    
                    if (string.IsNullOrEmpty(exercise.Difficulty))
                    {
                        exercise.Difficulty = bestMatch.Difficulty;
                        changed = true;
                    }
                    
                    if (string.IsNullOrEmpty(exercise.Instructions))
                    {
                        exercise.Instructions = bestMatch.Instructions;
                        changed = true;
                    }
                    
                    if (changed)
                    {
                        // Update the last updated timestamp
                        exercise.LastUpdated = DateTime.UtcNow;
                        
                        // Don't change the IsFromApi flag
                        
                        _context.Update(exercise);
                        enriched++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enriching exercise '{Name}'", exercise.Name);
                    failed++;
                }
            }
            
            // Save all changes at once
            if (enriched > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Enriched {Enriched} exercises with API data", enriched);
            }
            
            return (found, enriched, failed);
        }
    }
}