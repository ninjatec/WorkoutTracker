using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Api;

namespace WorkoutTrackerWeb.Services
{
    public class ExerciseSelectionService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExerciseSelectionService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ExerciseSelectionService(
            IServiceProvider serviceProvider,
            ILogger<ExerciseSelectionService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            // Setup consistent JSON options for serialization/deserialization
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };
        }

        /// <summary>
        /// Creates a pending exercise selection in the database
        /// </summary>
        public async Task<PendingExerciseSelection> CreatePendingSelectionAsync(
            string jobId, 
            int exerciseTypeId, 
            string exerciseName, 
            List<ExerciseApiResponse> apiExercises)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();

                // Serialize the API exercise results to store in the database
                string apiResultsJson = JsonSerializer.Serialize(apiExercises, _jsonOptions);
                
                var pendingSelection = new PendingExerciseSelection
                {
                    JobId = jobId,
                    ExerciseTypeId = exerciseTypeId,
                    ExerciseName = exerciseName,
                    ApiResults = apiResultsJson,
                    CreatedAt = DateTime.UtcNow
                };
                
                context.PendingExerciseSelection.Add(pendingSelection);
                await context.SaveChangesAsync();
                
                _logger.LogInformation("Created pending exercise selection ID {Id} for exercise {Name} in job {JobId}", 
                    pendingSelection.Id, exerciseName, jobId);
                
                return pendingSelection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating pending exercise selection for {ExerciseName} in job {JobId}", 
                    exerciseName, jobId);
                throw;
            }
        }
        
        /// <summary>
        /// Gets all pending exercise selections for a specific job
        /// </summary>
        public async Task<List<PendingExerciseSelection>> GetPendingSelectionsForJobAsync(string jobId)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
            
            var pendingSelections = await context.PendingExerciseSelection
                .Where(p => p.JobId == jobId && !p.IsResolved)
                .ToListAsync();
                
            foreach (var selection in pendingSelections)
            {
                await LoadSelectionDetailsAsync(selection, context);
            }
            
            return pendingSelections;
        }
        
        /// <summary>
        /// Gets a specific pending exercise selection by ID
        /// </summary>
        public async Task<PendingExerciseSelection> GetPendingSelectionAsync(int id)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
            
            var pendingSelection = await context.PendingExerciseSelection
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (pendingSelection == null)
            {
                return null;
            }
            
            await LoadSelectionDetailsAsync(pendingSelection, context);
            return pendingSelection;
        }
        
        /// <summary>
        /// Resolves a pending exercise selection by applying the selected API exercise data
        /// </summary>
        public async Task<ExerciseType> ResolvePendingSelectionAsync(int pendingSelectionId, int selectedApiExerciseIndex)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
            
            var pendingSelection = await context.PendingExerciseSelection
                .FirstOrDefaultAsync(p => p.Id == pendingSelectionId);
                
            if (pendingSelection == null)
            {
                throw new KeyNotFoundException($"Pending selection with ID {pendingSelectionId} not found");
            }
            
            var exerciseType = await context.ExerciseType
                .FirstOrDefaultAsync(e => e.ExerciseTypeId == pendingSelection.ExerciseTypeId);
                
            if (exerciseType == null)
            {
                throw new KeyNotFoundException($"Exercise type with ID {pendingSelection.ExerciseTypeId} not found");
            }
            
            List<ExerciseApiResponse> apiExercises = null;
            try
            {
                apiExercises = JsonSerializer.Deserialize<List<ExerciseApiResponse>>(pendingSelection.ApiResults, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing API results for pending selection {Id}", pendingSelectionId);
                throw new InvalidOperationException("Could not process the API exercise data", ex);
            }
            
            if (selectedApiExerciseIndex < 0 || selectedApiExerciseIndex >= apiExercises.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedApiExerciseIndex), "Selected exercise index is invalid");
            }
            
            var selectedApiExercise = apiExercises[selectedApiExerciseIndex];
            
            // Check if this is a word permutation match and log it for analytics
            if (!string.IsNullOrEmpty(selectedApiExercise.SearchInfo) && 
                selectedApiExercise.SearchInfo.Contains("Word Order Match:"))
            {
                _logger.LogInformation("Applying word order match to exercise '{Name}': {SearchInfo}", 
                    exerciseType.Name, selectedApiExercise.SearchInfo);
            }
            
            bool changed = false;
            
            if (string.IsNullOrEmpty(exerciseType.Type))
            {
                exerciseType.Type = selectedApiExercise.Type;
                changed = true;
            }
            
            if (string.IsNullOrEmpty(exerciseType.Muscle))
            {
                exerciseType.Muscle = selectedApiExercise.Muscle;
                changed = true;
            }
            
            if (string.IsNullOrEmpty(exerciseType.Equipment))
            {
                exerciseType.Equipment = selectedApiExercise.Equipment;
                changed = true;
            }
            
            if (string.IsNullOrEmpty(exerciseType.Difficulty))
            {
                exerciseType.Difficulty = selectedApiExercise.Difficulty;
                changed = true;
            }
            
            if (string.IsNullOrEmpty(exerciseType.Instructions))
            {
                exerciseType.Instructions = selectedApiExercise.Instructions;
                changed = true;
            }
            
            if (changed)
            {
                exerciseType.LastUpdated = DateTime.UtcNow;
                context.Update(exerciseType);
            }
            
            pendingSelection.IsResolved = true;
            pendingSelection.SelectedApiExerciseIndex = selectedApiExerciseIndex;
            pendingSelection.ResolvedAt = DateTime.UtcNow;
            context.Update(pendingSelection);
            
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Resolved pending selection {Id} for exercise {Name} with API exercise {ApiName}",
                pendingSelection.Id, exerciseType.Name, selectedApiExercise.Name);
                
            return exerciseType;
        }

        /// <summary>
        /// Resolves a pending exercise selection without requiring an active job
        /// </summary>
        public async Task<ExerciseType> ResolvePendingSelectionIndependentAsync(int pendingSelectionId, int selectedApiExerciseIndex)
        {
            // This method works exactly like ResolvePendingSelectionAsync but doesn't require an active job
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
            
            var pendingSelection = await context.PendingExerciseSelection
                .FirstOrDefaultAsync(p => p.Id == pendingSelectionId);
                
            if (pendingSelection == null)
            {
                throw new KeyNotFoundException($"Pending selection with ID {pendingSelectionId} not found");
            }
            
            var exerciseType = await context.ExerciseType
                .FirstOrDefaultAsync(e => e.ExerciseTypeId == pendingSelection.ExerciseTypeId);
                
            if (exerciseType == null)
            {
                throw new KeyNotFoundException($"Exercise type with ID {pendingSelection.ExerciseTypeId} not found");
            }
            
            List<ExerciseApiResponse> apiExercises = null;
            try
            {
                apiExercises = JsonSerializer.Deserialize<List<ExerciseApiResponse>>(pendingSelection.ApiResults, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing API results for pending selection {Id}", pendingSelectionId);
                throw new InvalidOperationException("Could not process the API exercise data", ex);
            }
            
            if (selectedApiExerciseIndex < 0 || selectedApiExerciseIndex >= apiExercises.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedApiExerciseIndex), "Selected exercise index is invalid");
            }
            
            var selectedApiExercise = apiExercises[selectedApiExerciseIndex];
            
            // Check if this is a word permutation match and log it for analytics
            if (!string.IsNullOrEmpty(selectedApiExercise.SearchInfo) && 
                selectedApiExercise.SearchInfo.Contains("Word Order Match:"))
            {
                _logger.LogInformation("Applying word order match to exercise '{Name}': {SearchInfo}", 
                    exerciseType.Name, selectedApiExercise.SearchInfo);
            }
            
            bool changed = false;
            
            if (string.IsNullOrEmpty(exerciseType.Type))
            {
                exerciseType.Type = selectedApiExercise.Type;
                changed = true;
            }
            
            if (string.IsNullOrEmpty(exerciseType.Muscle))
            {
                exerciseType.Muscle = selectedApiExercise.Muscle;
                changed = true;
            }
            
            if (string.IsNullOrEmpty(exerciseType.Equipment))
            {
                exerciseType.Equipment = selectedApiExercise.Equipment;
                changed = true;
            }
            
            if (string.IsNullOrEmpty(exerciseType.Difficulty))
            {
                exerciseType.Difficulty = selectedApiExercise.Difficulty;
                changed = true;
            }
            
            if (string.IsNullOrEmpty(exerciseType.Instructions))
            {
                exerciseType.Instructions = selectedApiExercise.Instructions;
                changed = true;
            }
            
            if (changed)
            {
                exerciseType.LastUpdated = DateTime.UtcNow;
                context.Update(exerciseType);
            }
            
            pendingSelection.IsResolved = true;
            pendingSelection.SelectedApiExerciseIndex = selectedApiExerciseIndex;
            pendingSelection.ResolvedAt = DateTime.UtcNow;
            context.Update(pendingSelection);
            
            await context.SaveChangesAsync();
            
            _logger.LogInformation("Resolved pending selection {Id} for exercise {Name} with API exercise {ApiName} (job-independent)",
                pendingSelection.Id, exerciseType.Name, selectedApiExercise.Name);
                
            return exerciseType;
        }
        
        /// <summary>
        /// Loads additional details for a pending selection
        /// </summary>
        private async Task LoadSelectionDetailsAsync(PendingExerciseSelection selection, WorkoutTrackerWebContext context = null)
        {
            try
            {
                selection.ApiExerciseOptions = JsonSerializer.Deserialize<List<ExerciseApiResponse>>(selection.ApiResults, _jsonOptions);
                
                if (context == null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
                }
                
                selection.ExerciseType = await context.ExerciseType
                    .FirstOrDefaultAsync(e => e.ExerciseTypeId == selection.ExerciseTypeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for pending selection {Id}", selection.Id);
                selection.ApiExerciseOptions = new List<ExerciseApiResponse>();
            }
        }
        
        /// <summary>
        /// Checks if there are any pending exercise selections for a specific job
        /// </summary>
        public async Task<bool> HasPendingSelectionsForJobAsync(string jobId)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
            
            return await context.PendingExerciseSelection
                .AnyAsync(p => p.JobId == jobId && !p.IsResolved);
        }
        
        /// <summary>
        /// Gets the count of pending exercise selections for a specific job
        /// </summary>
        public async Task<int> GetPendingSelectionsCountForJobAsync(string jobId)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
            
            return await context.PendingExerciseSelection
                .CountAsync(p => p.JobId == jobId && !p.IsResolved);
        }

        /// <summary>
        /// Gets all pending exercise selections that have not been resolved yet
        /// </summary>
        public async Task<List<PendingExerciseSelection>> GetAllPendingSelectionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
            
            var pendingSelections = await context.PendingExerciseSelection
                .Where(p => !p.IsResolved)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
                
            foreach (var selection in pendingSelections)
            {
                await LoadSelectionDetailsAsync(selection, context);
            }
            
            return pendingSelections;
        }
        
        /// <summary>
        /// Gets the count of all pending exercise selections that have not been resolved yet
        /// </summary>
        public async Task<int> GetAllPendingSelectionsCountAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
            
            return await context.PendingExerciseSelection
                .CountAsync(p => !p.IsResolved);
        }

        public async Task<List<ExerciseTypeWithUseCount>> GetRecentlyUsedExercisesAsync(int userId, int numberOfResults = 10)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();

            var currentTime = DateTime.UtcNow;
            var pastMonth = currentTime.AddMonths(-1);

            var exerciseUsages = await context.Set
                .Where(s => s.Session.UserId == userId && s.Session.datetime >= pastMonth)
                .GroupBy(s => s.ExerciseTypeId)
                .Select(g => new { ExerciseTypeId = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(numberOfResults)
                .ToListAsync();

            var exerciseIds = exerciseUsages.Select(e => e.ExerciseTypeId).ToList();

            var exerciseTypes = await context.ExerciseType
                .Where(et => exerciseIds.Contains(et.ExerciseTypeId))
                .ToListAsync();

            var result = new List<ExerciseTypeWithUseCount>();
            
            foreach (var usage in exerciseUsages)
            {
                var exerciseType = exerciseTypes.FirstOrDefault(et => et.ExerciseTypeId == usage.ExerciseTypeId);
                if (exerciseType != null)
                {
                    result.Add(new ExerciseTypeWithUseCount
                    {
                        ExerciseType = exerciseType,
                        UseCount = usage.Count
                    });
                }
            }

            return result;
        }

        public async Task<List<ExerciseType>> GetPopularExercisesAsync(int numberOfResults = 10)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();

            var popularExerciseIds = await context.Set
                .GroupBy(s => s.ExerciseTypeId)
                .Select(g => new { ExerciseTypeId = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(numberOfResults)
                .Select(g => g.ExerciseTypeId)
                .ToListAsync();

            var exerciseTypes = await context.ExerciseType
                .Where(et => popularExerciseIds.Contains(et.ExerciseTypeId))
                .ToListAsync();

            return popularExerciseIds
                .Select(id => exerciseTypes.FirstOrDefault(et => et.ExerciseTypeId == id))
                .Where(et => et != null)
                .ToList();
        }

        public async Task<IEnumerable<ExerciseType>> GetExercisesByNameAsync(string searchTerm)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await context.ExerciseType.Take(10).ToListAsync();
            }

            searchTerm = searchTerm.Trim().ToLower();

            return await context.ExerciseType
                .Where(e => e.Name.ToLower().Contains(searchTerm) || 
                           (e.Description != null && e.Description.ToLower().Contains(searchTerm)))
                .Take(20)
                .ToListAsync();
        }

        public async Task<List<ExerciseWithMuscleGroups>> GetExercisesByMuscleGroupAsync(string muscleGroup, int limit = 10)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();

            var exerciseTypes = await context.ExerciseType
                .Where(e => e.Description != null && e.Description.ToLower().Contains(muscleGroup.ToLower()))
                .Take(limit)
                .ToListAsync();

            var result = exerciseTypes.Select(e => new ExerciseWithMuscleGroups
            {
                ExerciseType = e,
                MuscleGroups = ExtractMuscleGroups(e.Description)
            }).ToList();

            return result;
        }

        private List<string> ExtractMuscleGroups(string description)
        {
            if (string.IsNullOrEmpty(description))
                return new List<string>();

            var commonMuscleGroups = new[]
            {
                "chest", "back", "shoulders", "legs", "arms",
                "biceps", "triceps", "forearms", "abs", "core",
                "quads", "hamstrings", "glutes", "calves", "traps",
                "lats", "deltoids", "pectorals", "abdominals"
            };

            description = description.ToLower();
            return commonMuscleGroups
                .Where(muscle => description.Contains(muscle))
                .ToList();
        }
    }

    public class ExerciseTypeWithUseCount
    {
        public ExerciseType ExerciseType { get; set; }
        public int UseCount { get; set; }
    }

    public class ExerciseWithMuscleGroups
    {
        public ExerciseType ExerciseType { get; set; }
        public List<string> MuscleGroups { get; set; }
    }
}