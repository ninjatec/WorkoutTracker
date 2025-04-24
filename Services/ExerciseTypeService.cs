using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Api;
using Microsoft.AspNetCore.SignalR;
using WorkoutTrackerWeb.Hubs;
using System.Threading;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

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
        private readonly IHubContext<ImportProgressHub> _hubContext;
        private readonly ExerciseSelectionService _selectionService;
        private readonly IServiceScopeFactory _scopeFactory;

        public ExerciseTypeService(
            WorkoutTrackerWebContext context,
            ExerciseApiService apiService,
            ILogger<ExerciseTypeService> logger,
            IServiceScopeFactory scopeFactory,
            ExerciseSelectionService selectionService = null,
            IHubContext<ImportProgressHub> hubContext = null)
        {
            _context = context;
            _apiService = apiService;
            _logger = logger;
            _hubContext = hubContext;
            _selectionService = selectionService;
            _scopeFactory = scopeFactory;
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

        /// <summary>
        /// Enriches multiple exercises with empty fields asynchronously as a background task
        /// </summary>
        /// <param name="jobId">Unique identifier for the background job</param>
        /// <param name="cancellationToken">Cancellation token for stopping the task</param>
        /// <param name="autoSelectMatches">Whether to automatically select the first match or wait for user input</param>
        /// <returns>Task representing the background operation</returns>
        public async Task EnrichExercisesBackgroundAsync(string jobId, bool autoSelectMatches = false, CancellationToken cancellationToken = default)
        {
            try
            {
                // Create a new scope for this background operation to avoid using a disposed context
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
                
                // Get a new instance of the API service from the current scope
                var apiService = scope.ServiceProvider.GetRequiredService<ExerciseApiService>();
                
                // Find all exercises with at least one empty field
                var exercises = await dbContext.ExerciseType
                    .Where(e => string.IsNullOrEmpty(e.Type) || 
                                string.IsNullOrEmpty(e.Muscle) || 
                                string.IsNullOrEmpty(e.Equipment) || 
                                string.IsNullOrEmpty(e.Difficulty) || 
                                string.IsNullOrEmpty(e.Instructions))
                    .ToListAsync(cancellationToken);

                int found = exercises.Count;
                int enriched = 0;
                int failed = 0;
                int pending = 0;
                int processed = 0;

                // Send initial progress update
                await SendProgressUpdateAsync(jobId, 0, found, enriched, failed, pending, "Starting enrichment process...");

                foreach (var exercise in exercises)
                {
                    // Check if the operation was cancelled
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Exercise enrichment job {JobId} was cancelled after processing {Processed} exercises", 
                            jobId, processed);
                        
                        await SendProgressUpdateAsync(jobId, processed, found, enriched, failed, pending,
                            "Operation cancelled by user or server.");
                            
                        break;
                    }

                    try
                    {
                        processed++;
                        
                        // Update progress about every 5% or at least every 5 items
                        if (processed % Math.Max(1, Math.Min(5, found / 20)) == 0 || processed == found)
                        {
                            await SendProgressUpdateAsync(jobId, processed, found, enriched, failed, pending,
                                $"Processing exercise: {exercise.Name}");
                        }

                        // Search for this exercise by name in the API
                        var searchParams = new ExerciseSearchParams { Name = exercise.Name };
                        var apiExercises = await apiService.SearchExercisesAsync(searchParams);

                        // Check for cancellation after the API call
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        // Find the closest match by name
                        var exactMatch = apiExercises.FirstOrDefault(a => 
                            a.Name.Equals(exercise.Name, StringComparison.OrdinalIgnoreCase));

                        // If no exact match, check if we should create a pending selection or auto-select
                        if (exactMatch == null && apiExercises.Any())
                        {
                            if (autoSelectMatches)
                            {
                                // Auto-select the first match
                                exactMatch = apiExercises.First();
                                _logger.LogInformation("Auto-selecting first match '{MatchName}' for exercise '{Name}'",
                                    exactMatch.Name, exercise.Name);
                            }
                            else 
                            {
                                // Get the selection service from the current scope if needed
                                var selectionService = scope.ServiceProvider.GetService<ExerciseSelectionService>();
                                
                                if (selectionService != null)
                                {
                                    // Create a pending selection for user input
                                    await selectionService.CreatePendingSelectionAsync(
                                        jobId, 
                                        exercise.ExerciseTypeId, 
                                        exercise.Name, 
                                        apiExercises);
                                    
                                    pending++;
                                    
                                    _logger.LogInformation("Created pending selection for exercise '{Name}' with {MatchCount} possible matches", 
                                        exercise.Name, apiExercises.Count);
                                    
                                    await SendProgressUpdateAsync(jobId, processed, found, enriched, failed, pending,
                                        $"Pending user selection for exercise: {exercise.Name}");
                                    
                                    continue; // Skip to next exercise
                                }
                                else
                                {
                                    // Fall back to first match if selection service not available
                                    exactMatch = apiExercises.First();
                                    _logger.LogWarning("Selection service not available, auto-selecting first match '{MatchName}' for exercise '{Name}'",
                                        exactMatch.Name, exercise.Name);
                                }
                            }
                        }

                        if (exactMatch == null)
                        {
                            _logger.LogWarning("No API data found for exercise '{Name}'", exercise.Name);
                            failed++;
                            continue;
                        }

                        // Fill in any empty fields with data from the API
                        bool changed = false;
                        
                        if (string.IsNullOrEmpty(exercise.Type))
                        {
                            exercise.Type = exactMatch.Type;
                            changed = true;
                        }
                        
                        if (string.IsNullOrEmpty(exercise.Muscle))
                        {
                            exercise.Muscle = exactMatch.Muscle;
                            changed = true;
                        }
                        
                        if (string.IsNullOrEmpty(exercise.Equipment))
                        {
                            exercise.Equipment = exactMatch.Equipment;
                            changed = true;
                        }
                        
                        if (string.IsNullOrEmpty(exercise.Difficulty))
                        {
                            exercise.Difficulty = exactMatch.Difficulty;
                            changed = true;
                        }
                        
                        if (string.IsNullOrEmpty(exercise.Instructions))
                        {
                            exercise.Instructions = exactMatch.Instructions;
                            changed = true;
                        }
                        
                        if (changed)
                        {
                            // Update the last updated timestamp
                            exercise.LastUpdated = DateTime.UtcNow;
                            
                            // Don't change the IsFromApi flag
                            
                            dbContext.Update(exercise);
                            
                            // Save changes for each exercise to avoid losing progress on failure
                            await dbContext.SaveChangesAsync(cancellationToken);
                            enriched++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing {ExerciseName}: {ErrorMessage}", exercise.Name, ex.Message);
                        failed++;
                        
                        // Send progress update after an error
                        await SendProgressUpdateAsync(jobId, processed, found, enriched, failed, pending, 
                            $"Error processing {exercise.Name}: {ex.Message}");
                    }
                }
                
                // Check if we have pending selections
                string status;
                int percentComplete;
                
                if (pending > 0)
                {
                    status = $"Partially completed. Enriched {enriched} of {found} exercises. {pending} exercises require your input.";
                    percentComplete = (int)((double)(processed - pending) / found * 100);
                }
                else
                {
                    status = cancellationToken.IsCancellationRequested 
                        ? "Operation cancelled" 
                        : $"Completed. Enriched {enriched} of {found} exercises.";
                    
                    percentComplete = cancellationToken.IsCancellationRequested 
                        ? (int)((double)processed / found * 100) 
                        : 100;
                }
                
                // Send final progress update
                await SendProgressUpdateAsync(jobId, processed, found, enriched, failed, pending, status, percentComplete);
                
                _logger.LogInformation("Exercise enrichment job {JobId} completed: found {Found}, enriched {Enriched}, failed {Failed}, pending {Pending}", 
                    jobId, found, enriched, failed, pending);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in enrichment background job {JobId}", jobId);
                
                // Send error progress update
                await SendProgressUpdateAsync(jobId, 0, 0, 0, 0, 0,
                    $"Error: {ex.Message}", 100, true);
            }
        }

        /// <summary>
        /// Sends a progress update via SignalR
        /// </summary>
        private async Task SendProgressUpdateAsync(
            string jobId, 
            int processed, 
            int total, 
            int enriched, 
            int failed, 
            int pending,
            string status, 
            int? percentComplete = null,
            bool isError = false)
        {
            // Skip if hub context was not provided
            if (_hubContext == null)
            {
                return;
            }

            // Calculate percent complete if not provided
            int percent = percentComplete ?? (total > 0 ? (int)((double)processed / total * 100) : 0);
            
            // Create progress object
            var progress = new WorkoutTrackerWeb.Services.JobProgress
            {
                Status = status,
                PercentComplete = percent,
                TotalItems = total,
                ProcessedItems = processed,
                Details = $"Enriched: {enriched}, Failed: {failed}, Pending: {pending}, Remaining: {total - processed}",
                ErrorMessage = isError ? status : null,
                CurrentItem = pending > 0 ? $"{pending} exercises need your selection" : null
            };
            
            // Send to the job group
            string groupName = $"job_{jobId}";
            await _hubContext.Clients.Group(groupName).SendAsync("receiveProgress", progress);
            
            _logger.LogDebug("Progress update sent to job {JobId}: {Processed}/{Total} ({Percent}%)", 
                jobId, processed, total, percent);
        }
        
        /// <summary>
        /// Checks if a job has any pending exercise selections
        /// </summary>
        public async Task<bool> HasPendingSelectionsAsync(string jobId)
        {
            if (_selectionService == null)
            {
                return false;
            }
            
            return await _selectionService.HasPendingSelectionsForJobAsync(jobId);
        }
        
        /// <summary>
        /// Gets the count of pending selections for a job
        /// </summary>
        public async Task<int> GetPendingSelectionsCountAsync(string jobId)
        {
            if (_selectionService == null)
            {
                return 0;
            }
            
            return await _selectionService.GetPendingSelectionsCountForJobAsync(jobId);
        }
        
        /// <summary>
        /// Gets all pending exercise selections for a job
        /// </summary>
        public async Task<List<PendingExerciseSelection>> GetPendingSelectionsAsync(string jobId)
        {
            if (_selectionService == null)
            {
                return new List<PendingExerciseSelection>();
            }
            
            return await _selectionService.GetPendingSelectionsForJobAsync(jobId);
        }
        
        /// <summary>
        /// Resolves a pending exercise selection by applying the selected API exercise data
        /// </summary>
        public async Task<ExerciseType> ResolvePendingSelectionAsync(int pendingSelectionId, int selectedApiExerciseIndex)
        {
            if (_selectionService == null)
            {
                throw new InvalidOperationException("Exercise selection service is not available");
            }
            
            return await _selectionService.ResolvePendingSelectionAsync(pendingSelectionId, selectedApiExerciseIndex);
        }

        /// <summary>
        /// Resolves a pending exercise selection by applying the selected API exercise data
        /// This method doesn't require an active job
        /// </summary>
        public async Task<ExerciseType> ResolvePendingSelectionWithoutJobAsync(int pendingSelectionId, int selectedApiExerciseIndex)
        {
            if (_selectionService == null)
            {
                throw new InvalidOperationException("Exercise selection service is not available");
            }
            
            return await _selectionService.ResolvePendingSelectionIndependentAsync(pendingSelectionId, selectedApiExerciseIndex);
        }

        /// <summary>
        /// Gets all pending exercise selections that have not been resolved yet
        /// </summary>
        public async Task<List<PendingExerciseSelection>> GetAllPendingSelectionsAsync()
        {
            if (_selectionService == null)
            {
                return new List<PendingExerciseSelection>();
            }
            
            return await _selectionService.GetAllPendingSelectionsAsync();
        }
        
        /// <summary>
        /// Gets the count of all pending selections
        /// </summary>
        public async Task<int> GetAllPendingSelectionsCountAsync()
        {
            if (_selectionService == null)
            {
                return 0;
            }
            
            return await _selectionService.GetAllPendingSelectionsCountAsync();
        }
    }
}