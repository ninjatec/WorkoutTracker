using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Models;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Services
{
    public class ImportProgress
    {
        public int TotalWorkouts { get; set; }
        public int CurrentWorkout { get; set; }
        public string CurrentWorkoutName { get; set; }
        public int TotalSets { get; set; }
        public int CurrentSet { get; set; }
        public string CurrentExercise { get; set; }
        public int TotalReps { get; set; }
        public int ProcessedReps { get; set; }
        public int PercentComplete => TotalReps > 0 ? (ProcessedReps * 100) / TotalReps : (TotalWorkouts > 0 ? (CurrentWorkout * 100) / TotalWorkouts : 0);
    }

    public class TrainAIWorkout
    {
        public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalDuration { get; set; }
        public int TotalSets { get; set; }
        public int BurnedCalories { get; set; }
        public decimal TotalTVL { get; set; }
        public List<TrainAISet> Sets { get; set; } = new();
    }

    public class TrainAISet
    {
        public string Exercise { get; set; }
        public int Reps { get; set; }
        public decimal Weight { get; set; }
        public int RestTime { get; set; }
    }

    public class TrainAIImportService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<TrainAIImportService> _logger;
        private const int BATCH_SIZE = 100; // Increased batch size
        private const int PROGRESS_UPDATE_FREQUENCY = 50; // Update progress every 50 reps
        
        // Legacy progress update event
        public event Action<ImportProgress> OnProgressUpdate;
        
        // Modern progress update action for the background job system
        public Action<JobProgress> OnProgressUpdateV2 { get; set; }

        public TrainAIImportService(
            WorkoutTrackerWebContext context,
            ILogger<TrainAIImportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private async Task<ExerciseType> GetOrCreateExerciseTypeAsync(string exerciseName)
        {
            // Normalize exercise name and search case-insensitively
            var normalizedName = exerciseName.Trim();
            var existingExercise = await _context.ExerciseType
                .FirstOrDefaultAsync(e => e.Name.ToLower() == normalizedName.ToLower());

            if (existingExercise != null)
            {
                return existingExercise;
            }

            // Check for similar names to prevent duplicates
            var similarExercises = await _context.ExerciseType
                .Where(e => EF.Functions.Like(e.Name.ToLower(), $"%{normalizedName.ToLower()}%") 
                        || normalizedName.ToLower().Contains(e.Name.ToLower()))
                .ToListAsync();

            if (similarExercises.Any())
            {
                // Use the most similar existing exercise
                return similarExercises.OrderBy(e => 
                    LevenshteinDistance(e.Name.ToLower(), normalizedName.ToLower()))
                    .First();
            }

            // Create new exercise type if no match found
            var newExercise = new ExerciseType
            {
                Name = normalizedName,
                Description = $"Imported from TrainAI: {normalizedName}"
            };
            _context.ExerciseType.Add(newExercise);
            await _context.SaveChangesAsync();
            return newExercise;
        }

        // Helper method to calculate string similarity
        private static int LevenshteinDistance(string s1, string s2)
        {
            var costs = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                costs[i, 0] = i;
            
            for (int j = 0; j <= s2.Length; j++)
                costs[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    costs[i, j] = Math.Min(
                        Math.Min(costs[i - 1, j] + 1, costs[i, j - 1] + 1),
                        costs[i - 1, j - 1] + cost);
                }
            }

            return costs[s1.Length, s2.Length];
        }

        public async Task<List<TrainAIWorkout>> ParseTrainAICsvAsync(Stream csvStream)
        {
            var workouts = new List<TrainAIWorkout>();
            var currentWorkout = new TrainAIWorkout();

            using var reader = new StreamReader(csvStream);

            // Configure CsvHelper to be more lenient
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                BadDataFound = null,
                HeaderValidated = null,
                IgnoreBlankLines = true,
                HasHeaderRecord = false
            };

            using var csv = new CsvReader(reader, config);

            while (await csv.ReadAsync())
            {
                try
                {
                    var line = csv.GetRecord<dynamic>();
                    if (line == null) continue;

                    // Get array of values from dynamic row
                    var fields = ((IDictionary<string, object>)line).Values
                        .Select(v => v?.ToString()?.Trim() ?? "")
                        .ToList();

                    // Skip empty rows
                    if (fields.Count == 0 || string.IsNullOrWhiteSpace(fields[0])) continue;

                    // Parse workout header
                    if (fields[0].EndsWith("Workout", StringComparison.OrdinalIgnoreCase))
                    {
                        if (currentWorkout.Sets.Count > 0)
                        {
                            workouts.Add(currentWorkout);
                        }
                        currentWorkout = new TrainAIWorkout { Name = fields[0] };
                    }
                    // Parse workout metadata
                    else if (fields[0].StartsWith("Workout Start", StringComparison.OrdinalIgnoreCase) && fields.Count > 1)
                    {
                        currentWorkout.StartTime = DateTime.Parse(fields[1], CultureInfo.InvariantCulture);
                    }
                    else if (fields[0].StartsWith("Workout End", StringComparison.OrdinalIgnoreCase) && fields.Count > 1)
                    {
                        currentWorkout.EndTime = DateTime.Parse(fields[1], CultureInfo.InvariantCulture);
                    }
                    else if (fields[0].StartsWith("Total Duration", StringComparison.OrdinalIgnoreCase) && fields.Count > 1)
                    {
                        currentWorkout.TotalDuration = int.Parse(fields[1], CultureInfo.InvariantCulture);
                    }
                    else if (fields[0].StartsWith("Total Sets", StringComparison.OrdinalIgnoreCase) && fields.Count > 1)
                    {
                        currentWorkout.TotalSets = int.Parse(fields[1], CultureInfo.InvariantCulture);
                    }
                    else if (fields[0].StartsWith("Burned Calories", StringComparison.OrdinalIgnoreCase) && fields.Count > 1)
                    {
                        currentWorkout.BurnedCalories = int.Parse(fields[1], CultureInfo.InvariantCulture);
                    }
                    else if (fields[0].StartsWith("Total TVL", StringComparison.OrdinalIgnoreCase) && fields.Count > 1)
                    {
                        currentWorkout.TotalTVL = decimal.Parse(fields[1], CultureInfo.InvariantCulture);
                    }
                    // Parse exercise data - ensure we have enough fields and it's not a header row
                    else if (fields[0] != "All Sets" && fields[0] != "Exercise" && !string.IsNullOrWhiteSpace(fields[0]) && fields.Count >= 3)
                    {
                        try
                        {
                            currentWorkout.Sets.Add(new TrainAISet
                            {
                                Exercise = fields[0],
                                Reps = int.Parse(fields[1], CultureInfo.InvariantCulture),
                                Weight = decimal.Parse(fields[2], CultureInfo.InvariantCulture),
                                RestTime = fields.Count > 3 && !string.IsNullOrWhiteSpace(fields[3]) 
                                    ? int.Parse(fields[3], CultureInfo.InvariantCulture) 
                                    : 0
                            });
                        }
                        catch (FormatException)
                        {
                            // Skip rows that can't be parsed as exercise data
                            continue;
                        }
                    }
                }
                catch (Exception)
                {
                    // Skip problematic rows
                    continue;
                }
            }

            // Add the last workout
            if (currentWorkout.Sets.Count > 0)
            {
                workouts.Add(currentWorkout);
            }

            return workouts;
        }

        public async Task<(bool success, string message, List<string> importedItems)> ImportTrainAIWorkoutsAsync(
            int userId, 
            List<TrainAIWorkout> workouts)
        {
            var importedItems = new List<string>();
            var progress = new ImportProgress { TotalWorkouts = workouts.Count };
            var batchedReps = new List<Rep>();
            var progressCounter = 0;

            try
            {
                _logger.LogInformation($"Starting TrainAI import for user {userId} with {workouts.Count} workouts");
                
                // Pre-calculate total reps for better progress reporting
                progress.TotalReps = workouts.Sum(w => w.Sets.Sum(s => s.Reps));
                ReportProgress(progress, "Starting import", 0);

                foreach (var workout in workouts)
                {
                    progress.CurrentWorkout++;
                    progress.CurrentWorkoutName = workout.Name;
                    progress.TotalSets = workout.Sets.Count;
                    progress.CurrentSet = 0;
                    
                    ReportProgress(progress, $"Processing workout {progress.CurrentWorkout}/{progress.TotalWorkouts}: {workout.Name}", 
                        progress.PercentComplete);

                    // Create a strategy and use it for all database operations in this workout
                    var strategy = _context.Database.CreateExecutionStrategy();
                    
                    await strategy.ExecuteAsync(async () => 
                    {
                        // Use transaction within execution strategy
                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            // Create session
                            var session = new Session
                            {
                                Name = workout.Name,
                                datetime = workout.StartTime,
                                UserId = userId
                            };

                            _context.Session.Add(session);
                            await _context.SaveChangesAsync();
                            importedItems.Add($"Session: {session.Name} ({session.datetime})");

                            // Process sets with optimized batching
                            foreach (var trainAiSet in workout.Sets)
                            {
                                progress.CurrentSet++;
                                progress.CurrentExercise = trainAiSet.Exercise;
                                
                                ReportProgress(progress, $"Processing set {progress.CurrentSet}/{progress.TotalSets}: {trainAiSet.Exercise}", 
                                    progress.PercentComplete);

                                var exerciseType = await GetOrCreateExerciseTypeAsync(trainAiSet.Exercise);
                                if (!importedItems.Contains($"Exercise Type: {exerciseType.Name}"))
                                {
                                    importedItems.Add($"Exercise Type: {exerciseType.Name}");
                                }

                                var setType = await _context.Settype
                                    .FirstOrDefaultAsync(s => s.Name == "Normal");

                                if (setType == null)
                                {
                                    setType = new Settype
                                    {
                                        Name = "Normal",
                                        Description = "Regular working set"
                                    };
                                    _context.Settype.Add(setType);
                                    await _context.SaveChangesAsync();
                                    importedItems.Add($"Set Type: {setType.Name}");
                                }

                                var set = new Set
                                {
                                    Description = $"Imported from TrainAI",
                                    Notes = $"Rest time: {trainAiSet.RestTime} seconds",
                                    ExerciseTypeId = exerciseType.ExerciseTypeId,
                                    SettypeId = setType.SettypeId,
                                    NumberReps = trainAiSet.Reps,
                                    Weight = trainAiSet.Weight,
                                    SessionId = session.SessionId
                                };

                                _context.Set.Add(set);
                                await _context.SaveChangesAsync();

                                // Create reps with optimized progress reporting
                                for (int i = 0; i < trainAiSet.Reps; i++)
                                {
                                    batchedReps.Add(new Rep
                                    {
                                        weight = trainAiSet.Weight,
                                        repnumber = i + 1,
                                        success = true,
                                        SetsSetId = set.SetId
                                    });
                                    
                                    progress.ProcessedReps++;
                                    progressCounter++;

                                    // Update progress less frequently to reduce overhead
                                    if (progressCounter >= PROGRESS_UPDATE_FREQUENCY)
                                    {
                                        ReportProgress(progress, $"Processing reps ({progress.ProcessedReps}/{progress.TotalReps})", 
                                            progress.PercentComplete);
                                        progressCounter = 0;
                                    }

                                    // Save in larger batches
                                    if (batchedReps.Count >= BATCH_SIZE)
                                    {
                                        await SaveRepsBatchAsync(batchedReps);
                                        batchedReps.Clear();
                                    }
                                }
                            }

                            // Save any remaining reps in the batch
                            if (batchedReps.Any())
                            {
                                await SaveRepsBatchAsync(batchedReps);
                                batchedReps.Clear();
                            }

                            await transaction.CommitAsync();
                        }
                        catch (Exception)
                        {
                            await transaction.RollbackAsync();
                            throw;
                        }
                    });
                }

                // Ensure final progress is reported
                ReportProgress(progress, "Import completed successfully", 100);
                _logger.LogInformation($"Successfully completed TrainAI import for user {userId}");

                return (true, "Import completed successfully", importedItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"TrainAI import failed for user {userId}");
                ReportProgress(progress, $"Import failed: {ex.Message}", progress.PercentComplete, ex.Message);
                return (false, $"Import failed: {ex.Message}", importedItems);
            }
        }

        private async Task SaveRepsBatchAsync(List<Rep> reps)
        {
            // Use AddRangeAsync for better performance with large batches
            await _context.Rep.AddRangeAsync(reps);
            await _context.SaveChangesAsync();
        }
        
        // Helper method to report progress through both the legacy and new systems
        private void ReportProgress(ImportProgress progress, string status, int percentComplete, string errorMessage = null)
        {
            // Report using legacy event
            OnProgressUpdate?.Invoke(progress);
            
            // Report using the new JobProgress system for background jobs
            if (OnProgressUpdateV2 != null)
            {
                var jobProgress = new JobProgress
                {
                    Status = status,
                    PercentComplete = percentComplete,
                    CurrentItem = progress.CurrentWorkoutName,
                    ProcessedItems = progress.ProcessedReps,
                    TotalItems = progress.TotalReps,
                    Details = $"{progress.CurrentExercise} (Set {progress.CurrentSet}/{progress.TotalSets})",
                    ErrorMessage = errorMessage
                };
                
                OnProgressUpdateV2(jobProgress);
            }
        }
    }
}