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

            // First try to find an exact match by name
            var bestMatch = apiExercises.FirstOrDefault(a => 
                a.Name.Equals(exercise.Name, StringComparison.OrdinalIgnoreCase));

            // If no exact match, look for similar exercises (contains)
            if (bestMatch == null && apiExercises.Any())
            {
                // Find exercises that contain the name or vice versa
                var similarMatches = apiExercises
                    .Where(a => a.Name.Contains(exercise.Name, StringComparison.OrdinalIgnoreCase) || 
                               exercise.Name.Contains(a.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                if (similarMatches.Any())
                {
                    bestMatch = similarMatches.First();
                    _logger.LogInformation("Found similar match '{MatchName}' for exercise '{Name}'", 
                        bestMatch.Name, exercise.Name);
                }
                else
                {
                    // Fall back to first result if no similar matches
                    bestMatch = apiExercises.First();
                    _logger.LogInformation("No similar matches found for '{Name}', using first result '{MatchName}'", 
                        exercise.Name, bestMatch.Name);
                }
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
            // Find exercises that need enrichment - an exercise needs enrichment if ANY of these core fields are empty:
            // 1. Missing Type field
            // 2. Missing Muscle field
            // 3. Missing Difficulty field
            var exercises = await _context.ExerciseType
                .Where(e => string.IsNullOrEmpty(e.Type) || 
                            string.IsNullOrEmpty(e.Muscle) || 
                            string.IsNullOrEmpty(e.Difficulty))
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

                    // First try for an exact match by name
                    var bestMatch = apiExercises.FirstOrDefault(a => 
                        a.Name.Equals(exercise.Name, StringComparison.OrdinalIgnoreCase));

                    // If no exact match, look for similar exercises (contains)
                    if (bestMatch == null && apiExercises.Any())
                    {
                        // Find exercises that contain the name or vice versa
                        var similarMatches = apiExercises
                            .Where(a => a.Name.Contains(exercise.Name, StringComparison.OrdinalIgnoreCase) || 
                                      exercise.Name.Contains(a.Name, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        if (similarMatches.Any())
                        {
                            bestMatch = similarMatches.First();
                            _logger.LogInformation("Found similar match '{MatchName}' for exercise '{Name}'", 
                                bestMatch.Name, exercise.Name);
                        }
                        else
                        {
                            // Fall back to first result if no similar matches
                            bestMatch = apiExercises.First();
                            _logger.LogInformation("No similar matches found for '{Name}', using first result '{MatchName}'", 
                                exercise.Name, bestMatch.Name);
                        }
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
                    
                    if (string.IsNullOrEmpty(exercise.Difficulty))
                    {
                        exercise.Difficulty = bestMatch.Difficulty;
                        changed = true;
                    }
                    
                    // Also update optional fields if they're empty
                    if (string.IsNullOrEmpty(exercise.Equipment))
                    {
                        exercise.Equipment = bestMatch.Equipment;
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
                
                // Get required services from the current scope
                var apiService = scope.ServiceProvider.GetRequiredService<ExerciseApiService>();
                var selectionService = scope.ServiceProvider.GetService<ExerciseSelectionService>();
                
                // Find exercises that need enrichment - an exercise needs enrichment if ANY of these conditions are true:
                // 1. Missing Type field
                // 2. Missing Muscle field
                // 3. Missing Difficulty field
                // This new query considers an exercise as "already enriched" if Type, Muscle and Difficulty are all populated
                var exercises = await dbContext.ExerciseType
                    .Where(e => string.IsNullOrEmpty(e.Type) || 
                                string.IsNullOrEmpty(e.Muscle) || 
                                string.IsNullOrEmpty(e.Difficulty))
                    .ToListAsync(cancellationToken);

                int found = exercises.Count;
                int enriched = 0;
                int failed = 0;
                int pending = 0;
                int processed = 0;
                int alreadyEnriched = 0;

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

                        // Use our enhanced search method that includes word permutations
                        var apiExercises = await SearchExercisesByNameAsync(exercise.Name, cancellationToken);

                        // Check for cancellation after the API call
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        // Check if we found any results
                        if (apiExercises.Count == 0)
                        {
                            _logger.LogWarning("No API data found for exercise '{Name}', even with word permutations", exercise.Name);
                            failed++;
                            continue;
                        }

                        // Process results based on whether they're exact matches, similar matches, or word permutation matches
                        var exactMatch = apiExercises.FirstOrDefault(a => 
                            a.Name.Equals(exercise.Name, StringComparison.OrdinalIgnoreCase));
                        
                        // If we don't have an exact match but have results, look for similar matches or permutations
                        if (exactMatch == null)
                        {
                            // Check if any of the results are permutation matches (they'll be marked)
                            var permutationMatches = apiExercises
                                .Where(a => a.SearchInfo?.Contains("Word Order Match:") == true)
                                .ToList();
                            
                            // Standard similar matches (contains)
                            var similarMatches = apiExercises
                                .Where(a => string.IsNullOrEmpty(a.SearchInfo) && 
                                           (a.Name.Contains(exercise.Name, StringComparison.OrdinalIgnoreCase) || 
                                            exercise.Name.Contains(a.Name, StringComparison.OrdinalIgnoreCase)))
                                .ToList();
                            
                            // Combine results with similar matches first, then permutation matches
                            var allPotentialMatches = similarMatches.Concat(permutationMatches).ToList();
                            
                            if (allPotentialMatches.Any())
                            {
                                if (autoSelectMatches)
                                {
                                    // Auto-select the first match
                                    exactMatch = allPotentialMatches.First();
                                    _logger.LogInformation("Auto-selecting match '{MatchName}' for exercise '{Name}'",
                                        exactMatch.Name, exercise.Name);
                                }
                                else if (selectionService != null)
                                {
                                    // Create a pending selection for user input with all potential matches
                                    await selectionService.CreatePendingSelectionAsync(
                                        jobId, 
                                        exercise.ExerciseTypeId, 
                                        exercise.Name, 
                                        allPotentialMatches);
                                    
                                    pending++;
                                    
                                    _logger.LogInformation("Created pending selection for exercise '{Name}' with {MatchCount} potential matches", 
                                        exercise.Name, allPotentialMatches.Count);
                                    
                                    await SendProgressUpdateAsync(jobId, processed, found, enriched, failed, pending,
                                        $"Pending user selection for exercise: {exercise.Name}");
                                    
                                    continue; // Skip to next exercise
                                }
                                else
                                {
                                    // Fall back to first match if selection service not available
                                    exactMatch = allPotentialMatches.First();
                                    _logger.LogWarning("Selection service not available, auto-selecting first match '{MatchName}' for exercise '{Name}'",
                                        exactMatch.Name, exercise.Name);
                                }
                            }
                            else if (autoSelectMatches)
                            {
                                // Auto-select the first result if there are no similar or permutation matches
                                exactMatch = apiExercises.First();
                                _logger.LogInformation("No similar matches, auto-selecting first result '{MatchName}' for exercise '{Name}'",
                                    exactMatch.Name, exercise.Name);
                            }
                            else if (selectionService != null)
                            {
                                // Create a pending selection for user input with all API results
                                await selectionService.CreatePendingSelectionAsync(
                                    jobId, 
                                    exercise.ExerciseTypeId, 
                                    exercise.Name, 
                                    apiExercises);
                                
                                pending++;
                                
                                _logger.LogInformation("No similar matches, created pending selection for exercise '{Name}' with {MatchCount} possible matches", 
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

                        // At this point, we should have a match to apply (either exact, similar, or first result)
                        if (exactMatch == null)
                        {
                            _logger.LogWarning("No API data found for exercise '{Name}'", exercise.Name);
                            failed++;
                            continue;
                        }

                        // Extract actual exercise name if it has the permutation marker
                        string actualName = exactMatch.Name;

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
                        
                        if (string.IsNullOrEmpty(exercise.Difficulty))
                        {
                            exercise.Difficulty = exactMatch.Difficulty;
                            changed = true;
                        }
                        
                        // Also update optional fields if they're empty
                        if (string.IsNullOrEmpty(exercise.Equipment))
                        {
                            exercise.Equipment = exactMatch.Equipment;
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
                            
                            // Add note about using permutation if applicable
                            string additionalInfo = exactMatch.SearchInfo?.Contains("Word Order Match:") == true 
                                ? " using word permutation match" 
                                : "";
                            
                            _logger.LogInformation("Enriched exercise '{Name}' with data from '{MatchName}'{AdditionalInfo}", 
                                exercise.Name, actualName, additionalInfo);
                        }
                        else
                        {
                            _logger.LogInformation("No changes needed for exercise '{Name}' - already has core data populated", exercise.Name);
                            alreadyEnriched++;
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

        /// <summary>
        /// Searches for exercises by name, providing a more flexible search when exact matches aren't found
        /// </summary>
        /// <param name="name">Exercise name to search for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of matching API exercises</returns>
        public async Task<List<ExerciseApiResponse>> SearchExercisesByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new List<ExerciseApiResponse>();
            }

            // Try exact match first
            var searchParams = new ExerciseSearchParams { Name = name };
            var results = await _apiService.SearchExercisesAsync(searchParams);
            
            // If no results found, try searching with words in different order
            if ((results == null || results.Count == 0) && !string.IsNullOrWhiteSpace(name))
            {
                // Split the name into words and filter out empty strings
                var words = name.Split(new[] { ' ', '-', '_', ',' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Only proceed if we have multiple words (single word already failed in the exact match)
                if (words.Length > 1)
                {
                    _logger.LogInformation("No exact matches found for '{Name}', trying word permutations", name);
                    
                    try {
                        // Generate permutations of the words
                        var permutations = GenerateWordPermutations(words, 3); // Limit to 3 permutations to avoid API spam
                        
                        foreach (var permutation in permutations)
                        {
                            // Skip if it's identical to the original (should be caught by exact match)
                            if (permutation.Equals(name, StringComparison.OrdinalIgnoreCase))
                                continue;
                            
                            // Check for cancellation
                            cancellationToken.ThrowIfCancellationRequested();
                            
                            _logger.LogInformation("Trying word permutation: '{Permutation}' for '{Name}'", permutation, name);
                            
                            // Try searching with this permutation
                            var permutationParams = new ExerciseSearchParams { Name = permutation };
                            var permutationResults = await _apiService.SearchExercisesAsync(permutationParams);
                            
                            // If we got results, add them to our list with searchInfo about the permutation
                            if (permutationResults != null && permutationResults.Count > 0)
                            {
                                foreach (var result in permutationResults)
                                {
                                    if (result != null)
                                    {
                                        result.SearchInfo = $"Word Order Match: {permutation}";
                                    }
                                }
                                
                                results ??= new List<ExerciseApiResponse>();
                                results.AddRange(permutationResults.Where(r => r != null));
                            }
                        }
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, "Error while generating word permutations for '{Name}'", name);
                    }
                }
            }
            
            // If still no results found or we want to try for partial matches regardless
            if ((results == null || results.Count < 3) && !string.IsNullOrWhiteSpace(name))
            {
                try {
                    // Try searching for partial word matches
                    var partialMatchResults = await SearchForPartialWordMatchesAsync(name, cancellationToken);
                    
                    if (partialMatchResults.Count > 0)
                    {
                        results ??= new List<ExerciseApiResponse>();
                        
                        // Check for duplicates before adding
                        foreach (var partialMatch in partialMatchResults)
                        {
                            if (partialMatch != null && !results.Any(r => r != null && r.Name != null && 
                                r.Name.Equals(partialMatch.Name, StringComparison.OrdinalIgnoreCase)))
                            {
                                results.Add(partialMatch);
                            }
                        }
                    }
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Error while searching for partial word matches for '{Name}'", name);
                }
            }
            
            return results ?? new List<ExerciseApiResponse>();
        }

        /// <summary>
        /// Searches for exercises with partial word matches to the given exercise name
        /// </summary>
        /// <param name="name">Exercise name to search for partial matches</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of exercises with partial word matches</returns>
        private async Task<List<ExerciseApiResponse>> SearchForPartialWordMatchesAsync(string name, CancellationToken cancellationToken = default)
        {
            var results = new List<ExerciseApiResponse>();
            
            // Split the exercise name into individual words
            var words = name.Split(new[] { ' ', '-', '_', ',' }, StringSplitOptions.RemoveEmptyEntries)
                         .Where(w => w.Length >= 4) // Only use words with at least 4 characters to avoid common words
                         .ToList();
            
            if (words.Count == 0)
                return results;
            
            _logger.LogInformation("Searching for partial word matches using {Count} words from '{Name}'", words.Count, name);
            
            // For each significant word in the name, search for exercises containing it
            foreach (var word in words)
            {
                // Skip very common words that might return too many matches
                if (IsCommonWord(word))
                    continue;
                
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();
                
                // Search for exercises containing this word
                var wordSearchParams = new ExerciseSearchParams { Name = word };
                var wordResults = await _apiService.SearchExercisesAsync(wordSearchParams);
                
                if (wordResults != null && wordResults.Count > 0)
                {
                    _logger.LogInformation("Found {Count} potential partial matches using word '{Word}'", wordResults.Count, word);
                    
                    // Mark these as partial word matches
                    foreach (var result in wordResults)
                    {
                        result.SearchInfo = $"Partial Word Match: {word}";
                    }
                    
                    // Add matches that aren't exact replicas of the original name
                    foreach (var match in wordResults)
                    {
                        if (!name.Equals(match.Name, StringComparison.OrdinalIgnoreCase) && 
                            !results.Any(r => r.Name.Equals(match.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            results.Add(match);
                        }
                    }
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Checks if a word is a common word that should be excluded from partial matching
        /// </summary>
        private bool IsCommonWord(string word)
        {
            // Lowercase for comparison
            word = word.ToLowerInvariant();
            
            // List of common words in exercise names that aren't specific enough for searches
            var commonWords = new[] 
            { 
                "with", "using", "exercise", "workout", "training", "body", "weight",
                "machine", "assisted", "free", "weights", "dumbbell", "barbell" 
            };
            
            return commonWords.Contains(word);
        }

        /// <summary>
        /// Generates different word orders from an array of words
        /// </summary>
        /// <param name="words">Array of words to permute</param>
        /// <param name="maxPermutations">Maximum number of permutations to return</param>
        /// <returns>List of permuted word strings</returns>
        private List<string> GenerateWordPermutations(string[] words, int maxPermutations)
        {
            var result = new List<string>();
            
            // Add simple reordering of words
            // Example: "Bench Press" -> "Press Bench"
            if (words.Length == 2)
            {
                result.Add($"{words[1]} {words[0]}");
            }
            // For 3+ words, try some common useful permutations
            else if (words.Length >= 3)
            {
                // First word moved to end
                var firstToEnd = words.Skip(1).Concat(new[] { words[0] });
                result.Add(string.Join(" ", firstToEnd));
                
                // Last word moved to beginning
                var lastToStart = new[] { words[words.Length - 1] }.Concat(words.Take(words.Length - 1));
                result.Add(string.Join(" ", lastToStart));
                
                // If we have exactly 3 words, add one more permutation
                if (words.Length == 3)
                {
                    // Middle, first, last
                    result.Add($"{words[1]} {words[0]} {words[2]}");
                }
            }
            
            // Limit the number of permutations to avoid API spam
            return result.Take(maxPermutations).ToList();
        }

        /// <summary>
        /// Determines if an exercise type is already sufficiently enriched
        /// based on having the core fields (Type, Muscle, and Difficulty) populated
        /// </summary>
        /// <param name="exerciseType">The exercise type to check</param>
        /// <returns>True if the exercise is considered already enriched</returns>
        private bool IsExerciseSufficientlyEnriched(ExerciseType exerciseType)
        {
            // Consider an exercise enriched if Type, Muscle, and Difficulty are all populated
            return !string.IsNullOrEmpty(exerciseType.Type) && 
                   !string.IsNullOrEmpty(exerciseType.Muscle) && 
                   !string.IsNullOrEmpty(exerciseType.Difficulty);
        }
    }
}