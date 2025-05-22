using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
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

    // Add custom DataRow class for import
    public class DataRow
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
        
        public object this[string name]
        {
            get => _data.TryGetValue(name, out var value) ? value : null;
            set => _data[name] = value;
        }
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
        private const int BATCH_SIZE = 250; // Increased batch size for better performance
        private const int PROGRESS_UPDATE_FREQUENCY = 100; // Less frequent updates to reduce overhead
        
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

        private async Task<ExerciseType> GetOrCreateExerciseTypeAsync(string name)
        {
            var exerciseType = await _context.ExerciseType
                .FirstOrDefaultAsync(et => et.Name == name);

            if (exerciseType == null)
            {
                exerciseType = new ExerciseType { Name = name };
                _context.ExerciseType.Add(exerciseType);
                await _context.SaveChangesAsync();
            }

            return exerciseType;
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
                        if (DateTime.TryParse(fields[1], CultureInfo.InvariantCulture, out DateTime endTime))
                        {
                            currentWorkout.EndTime = endTime;
                        }
                    }
                    else if (fields[0].StartsWith("Total Duration", StringComparison.OrdinalIgnoreCase) && fields.Count > 1)
                    {
                        if (fields[1] != "N/A" && int.TryParse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture, out int duration))
                        {
                            currentWorkout.TotalDuration = duration;
                        }
                    }
                    else if (fields[0].StartsWith("Total Sets", StringComparison.OrdinalIgnoreCase) && fields.Count > 1)
                    {
                        if (fields[1] != "N/A" && int.TryParse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture, out int totalSets))
                        {
                            currentWorkout.TotalSets = totalSets;
                        }
                    }
                    else if (fields[0].StartsWith("Burned Calories", StringComparison.OrdinalIgnoreCase) && fields.Count > 1)
                    {
                        if (fields[1] != "N/A" && int.TryParse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture, out int calories))
                        {
                            currentWorkout.BurnedCalories = calories;
                        }
                    }
                    else if (fields[0].StartsWith("Total TVL", StringComparison.OrdinalIgnoreCase) && fields.Count > 1)
                    {
                        if (fields[1] != "N/A" && decimal.TryParse(fields[1], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal tvl))
                        {
                            currentWorkout.TotalTVL = tvl;
                        }
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

            try
            {
                _logger.LogInformation($"Starting TrainAI import for user {userId} with {workouts.Count} workouts");
                
                // Pre-calculate total reps for better progress reporting
                progress.TotalReps = workouts.Sum(w => w.Sets.Sum(s => s.Reps));
                ReportProgress(progress, "Starting import", 0);

                importedItems = await ImportWorkoutSessionsAsync(workouts, userId);

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

        private async Task<List<string>> ImportWorkoutSessionsAsync(List<TrainAIWorkout> workouts, int userId)
        {
            var importedItems = new List<string>();

            foreach (var workout in workouts)
            {
                var session = new WorkoutSession
                {
                    Name = workout.Name,
                    StartDateTime = workout.StartTime,
                    EndDateTime = workout.EndTime,
                    Duration = workout.TotalDuration,
                    UserId = userId,
                    IsCompleted = true,
                    Status = "Completed"
                };

                _context.WorkoutSessions.Add(session);
                await _context.SaveChangesAsync();
                importedItems.Add($"WorkoutSession: {session.Name} ({session.StartDateTime})");

                // Group sets by exercise name to create WorkoutExercises
                var exerciseGroups = workout.Sets.GroupBy(s => s.Exercise);
                var sequenceNum = 0;

                foreach (var exerciseGroup in exerciseGroups)
                {
                    var exerciseType = await GetOrCreateExerciseTypeAsync(exerciseGroup.Key);
                    
                    var workoutExercise = new WorkoutExercise
                    {
                        WorkoutSessionId = session.WorkoutSessionId,
                        ExerciseTypeId = exerciseType.ExerciseTypeId,
                        SequenceNum = sequenceNum++
                    };

                    _context.WorkoutExercises.Add(workoutExercise);
                    await _context.SaveChangesAsync();

                    var setSequence = 0;
                    foreach (var trainAiSet in exerciseGroup)
                    {
                        var workoutSet = new WorkoutSet
                        {
                            WorkoutExerciseId = workoutExercise.WorkoutExerciseId,
                            SequenceNum = setSequence++,
                            Weight = trainAiSet.Weight,
                            Reps = trainAiSet.Reps,
                            Notes = trainAiSet.RestTime > 0 ? $"Rest: {trainAiSet.RestTime}s" : null,
                            IsCompleted = true // Explicitly set to true to indicate successful completion
                        };

                        _context.WorkoutSets.Add(workoutSet);
                    }

                    await _context.SaveChangesAsync();
                    importedItems.Add($"Exercise: {exerciseType.Name} with {exerciseGroup.Count()} sets");
                }
            }

            return importedItems;
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