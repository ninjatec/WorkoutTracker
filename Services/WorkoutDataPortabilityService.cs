using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using System.Linq;

namespace WorkoutTrackerWeb.Services
{
    public class WorkoutDataPortabilityService
    {
        private readonly ILogger<WorkoutDataPortabilityService> _logger;
        private readonly IServiceProvider _serviceProvider;

        // Progress update delegate for background processing
        public Action<JobProgress> OnProgressUpdate { get; set; }

        public WorkoutDataPortabilityService(
            ILogger<WorkoutDataPortabilityService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task<WorkoutExport> ExportUserDataAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            // Create a new DB context for this operation
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
            
            // Get user info
            var user = await context.User.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            // Create export data
            var export = new WorkoutExport
            {
                ExportDate = DateTime.UtcNow,
                Version = "1.0",
                UserName = user.Name,
                Sessions = new List<SessionExport>(),
                ExerciseTypes = new List<ExerciseTypeExport>(),
                SetTypes = new List<SetTypeExport>()
            };

            // Prepare query for sessions based on provided date range
            var sessionQuery = context.Session.Where(s => s.UserId == userId);
            if (startDate.HasValue)
            {
                sessionQuery = sessionQuery.Where(s => s.datetime >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                sessionQuery = sessionQuery.Where(s => s.datetime <= endDate.Value);
            }

            // Fetch and process sessions
            var sessions = await sessionQuery
                .Include(s => s.Sets)
                .ThenInclude(s => s.Reps)
                .Include(s => s.Sets)
                .ThenInclude(s => s.ExerciseType)
                .Include(s => s.Sets)
                .ThenInclude(s => s.Settype)
                .ToListAsync();

            // Report initial progress for background processing
            ReportProgress("Starting export...", 0, sessions.Count);

            int processedCount = 0;
            foreach (var session in sessions)
            {
                var sessionExport = new SessionExport
                {
                    Name = session.Name,
                    DateTime = session.datetime,
                    Sets = new List<SetExport>()
                };

                foreach (var set in session.Sets)
                {
                    var setExport = new SetExport
                    {
                        Description = set.Description,
                        Notes = set.Notes,
                        NumberReps = set.NumberReps,
                        Weight = set.Weight,
                        ExerciseTypeName = set.ExerciseType?.Name,
                        SetTypeName = set.Settype?.Name,
                        Reps = new List<RepExport>()
                    };

                    if (set.Reps != null)
                    {
                        foreach (var rep in set.Reps)
                        {
                            setExport.Reps.Add(new RepExport
                            {
                                RepNumber = rep.repnumber,
                                Weight = rep.weight,
                                Success = rep.success
                            });
                        }
                    }

                    sessionExport.Sets.Add(setExport);
                }

                export.Sessions.Add(sessionExport);
                processedCount++;
                
                // Report progress every session
                ReportProgress("Exporting session data...", processedCount, sessions.Count, session.Name);
            }

            // Add all exercise types
            var exerciseTypes = await context.ExerciseType.ToListAsync();
            foreach (var exerciseType in exerciseTypes)
            {
                export.ExerciseTypes.Add(new ExerciseTypeExport
                {
                    Name = exerciseType.Name,
                    Description = exerciseType.Description ?? ""
                });
            }

            // Add all set types
            var setTypes = await context.Settype.ToListAsync();
            foreach (var setType in setTypes)
            {
                export.SetTypes.Add(new SetTypeExport
                {
                    Name = setType.Name,
                    Description = setType.Description ?? ""
                });
            }

            // Report completion
            ReportProgress("Export completed", sessions.Count, sessions.Count);

            return export;
        }

        public async Task<(bool success, string message, List<string> importedItems)> ImportUserDataAsync(
            int userId, 
            string jsonData, 
            bool skipExisting = true)
        {
            try
            {
                // Create a new DB context for this operation
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
                
                // Report initial progress
                ReportProgress("Starting import...", 0, 100, "Parsing JSON data");

                var import = JsonSerializer.Deserialize<WorkoutExport>(jsonData);
                var importedItems = new List<string>();
                
                // Get total items to process for progress tracking
                int totalItems = 
                    (import.ExerciseTypes?.Count ?? 0) + 
                    (import.SetTypes?.Count ?? 0) + 
                    (import.Sessions?.Count ?? 0);
                    
                int processedItems = 0;

                // Report parsed items
                ReportProgress("JSON parsed successfully", 5, 100, 
                    $"Found {import.Sessions?.Count ?? 0} sessions, " +
                    $"{import.ExerciseTypes?.Count ?? 0} exercise types, " +
                    $"{import.SetTypes?.Count ?? 0} set types");

                // Import exercise types if they don't exist
                if (import.ExerciseTypes != null && import.ExerciseTypes.Count > 0)
                {
                    foreach (var exerciseType in import.ExerciseTypes)
                    {
                        if (!await context.ExerciseType.AnyAsync(e => e.Name == exerciseType.Name))
                        {
                            context.ExerciseType.Add(new ExerciseType
                            {
                                Name = exerciseType.Name,
                                Description = exerciseType.Description
                            });
                            importedItems.Add($"Exercise Type: {exerciseType.Name}");
                        }
                        processedItems++;
                        ReportProgress("Importing exercise types...", processedItems, totalItems, exerciseType.Name);
                    }
                }

                // Import set types if they don't exist
                if (import.SetTypes != null && import.SetTypes.Count > 0)
                {
                    foreach (var setType in import.SetTypes)
                    {
                        if (!await context.Settype.AnyAsync(s => s.Name == setType.Name))
                        {
                            context.Settype.Add(new Settype
                            {
                                Name = setType.Name,
                                Description = setType.Description
                            });
                            importedItems.Add($"Set Type: {setType.Name}");
                        }
                        processedItems++;
                        ReportProgress("Importing set types...", processedItems, totalItems, setType.Name);
                    }
                }

                await context.SaveChangesAsync();
                ReportProgress("Base types imported", 20, 100, "Beginning session import");

                // Import sessions and their related data
                if (import.Sessions != null && import.Sessions.Count > 0)
                {
                    int sessionCount = import.Sessions.Count;
                    int currentSession = 0;
                    int percentComplete = 20; // Start from 20% after base types
                    
                    foreach (var sessionExport in import.Sessions)
                    {
                        currentSession++;
                        
                        // Calculate percentage: 20% (base) + (current/total * 80%)
                        percentComplete = 20 + (int)((currentSession / (float)sessionCount) * 80);
                        ReportProgress($"Importing session {currentSession}/{sessionCount}", 
                            percentComplete, 100, sessionExport.Name);
                            
                        if (skipExisting && await context.Session.AnyAsync(s => 
                            s.UserId == userId && 
                            s.Name == sessionExport.Name && 
                            s.datetime == sessionExport.DateTime))
                        {
                            _logger.LogInformation("Skipping existing session: {Name} on {Date}", 
                                sessionExport.Name, sessionExport.DateTime);
                            processedItems++;
                            continue;
                        }

                        var session = new Models.Session
                        {
                            Name = sessionExport.Name,
                            datetime = sessionExport.DateTime,
                            UserId = userId
                        };

                        context.Session.Add(session);
                        await context.SaveChangesAsync(); // Save to get session ID
                        importedItems.Add($"Session: {sessionExport.Name} ({sessionExport.DateTime})");

                        // Load existing exercise and set types for reference
                        var exerciseTypes = await context.ExerciseType.ToDictionaryAsync(e => e.Name, e => e);
                        var setTypes = await context.Settype.ToDictionaryAsync(s => s.Name, s => s);

                        if (sessionExport.Sets != null && sessionExport.Sets.Count > 0)
                        {
                            foreach (var setExport in sessionExport.Sets)
                            {
                                // Find or create the exercise type
                                ExerciseType exerciseType = null;
                                if (!string.IsNullOrEmpty(setExport.ExerciseTypeName) && 
                                    exerciseTypes.TryGetValue(setExport.ExerciseTypeName, out exerciseType))
                                {
                                    // Exercise type exists
                                }
                                else if (!string.IsNullOrEmpty(setExport.ExerciseTypeName))
                                {
                                    // Create new exercise type if it doesn't exist
                                    exerciseType = new ExerciseType { Name = setExport.ExerciseTypeName };
                                    context.ExerciseType.Add(exerciseType);
                                    await context.SaveChangesAsync();
                                    exerciseTypes[exerciseType.Name] = exerciseType;
                                    importedItems.Add($"Exercise Type: {exerciseType.Name}");
                                }

                                // Find or create the set type
                                Settype setType = null;
                                if (!string.IsNullOrEmpty(setExport.SetTypeName) && 
                                    setTypes.TryGetValue(setExport.SetTypeName, out setType))
                                {
                                    // Set type exists
                                }
                                else if (!string.IsNullOrEmpty(setExport.SetTypeName))
                                {
                                    // Create new set type if it doesn't exist
                                    setType = new Settype { Name = setExport.SetTypeName };
                                    context.Settype.Add(setType);
                                    await context.SaveChangesAsync();
                                    setTypes[setType.Name] = setType;
                                    importedItems.Add($"Set Type: {setType.Name}");
                                }

                                // Create the set
                                var set = new Set
                                {
                                    Description = setExport.Description,
                                    Notes = setExport.Notes,
                                    NumberReps = setExport.NumberReps,
                                    Weight = setExport.Weight,
                                    ExerciseTypeId = exerciseType?.ExerciseTypeId ?? 0, // Fixed null conversion
                                    SettypeId = setType?.SettypeId ?? 0, // Fixed null conversion
                                    SessionId = session.SessionId
                                };

                                context.Set.Add(set);
                                await context.SaveChangesAsync(); // Save to get set ID

                                // Add reps if present
                                if (setExport.Reps != null && setExport.Reps.Count > 0)
                                {
                                    foreach (var repExport in setExport.Reps)
                                    {
                                        var rep = new Rep
                                        {
                                            repnumber = repExport.RepNumber,
                                            weight = repExport.Weight,
                                            success = repExport.Success,
                                            SetsSetId = set.SetId
                                        };

                                        context.Rep.Add(rep);
                                    }

                                    await context.SaveChangesAsync();
                                }
                            }
                        }
                        
                        processedItems++;
                    }
                }

                // Final progress update
                ReportProgress("Import completed successfully", 100, 100, 
                    $"Imported {importedItems.Count} items");

                return (true, "Import completed successfully", importedItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during import");
                
                // Report error progress
                ReportProgress("Import failed", 0, 100, "Error", ex.Message);
                
                return (false, $"Import failed: {ex.Message}", new List<string>());
            }
        }
        
        // Helper method to report progress for background processing
        private void ReportProgress(string status, int current, int total, string currentItem = null, string errorMessage = null)
        {
            if (OnProgressUpdate == null) return;
            
            int percentComplete = total > 0 ? (int)((current / (float)total) * 100) : 0;
            percentComplete = Math.Min(percentComplete, 100); // Ensure we don't exceed 100%
            
            var progress = new JobProgress
            {
                Status = status,
                PercentComplete = percentComplete,
                ProcessedItems = current,
                TotalItems = total,
                CurrentItem = currentItem,
                Details = currentItem,
                ErrorMessage = errorMessage
            };
            
            OnProgressUpdate(progress);
        }
    }

    public class WorkoutExport
    {
        public DateTime ExportDate { get; set; }
        public string Version { get; set; }
        public string UserName { get; set; }
        public List<SessionExport> Sessions { get; set; }
        public List<ExerciseTypeExport> ExerciseTypes { get; set; }
        public List<SetTypeExport> SetTypes { get; set; }
    }

    public class SessionExport
    {
        public string Name { get; set; }
        public DateTime DateTime { get; set; }
        public List<SetExport> Sets { get; set; }
    }

    public class SetExport
    {
        public string Description { get; set; }
        public string Notes { get; set; }
        public int NumberReps { get; set; }
        public decimal Weight { get; set; } // Changed from double to decimal
        public string ExerciseTypeName { get; set; }
        public string SetTypeName { get; set; }
        public List<RepExport> Reps { get; set; }
    }

    public class RepExport
    {
        public int RepNumber { get; set; }
        public decimal Weight { get; set; } // Changed from double to decimal
        public bool Success { get; set; }
    }

    public class ExerciseTypeExport
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class SetTypeExport
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}