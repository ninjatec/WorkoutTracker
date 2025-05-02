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
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Identity;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Services
{
    public class ExportWorkoutDataOptions
    {
        public int UserId { get; set; }
        public string ExportPath { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class ImportWorkoutDataOptions
    {
        public int UserId { get; set; }
        public string ImportFile { get; set; }
        public bool SkipExisting { get; set; }
    }

    public class WorkoutImportDto
    {
        public string Name { get; set; }
        public DateTime DateTime { get; set; }
        public List<WorkoutSetImportDto> Sets { get; set; } = new List<WorkoutSetImportDto>();

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Name) && Sets != null && Sets.Any();
        }
    }

    public class WorkoutSetImportDto
    {
        public int ExerciseTypeId { get; set; }
        public int SequenceNum { get; set; }
        public int? Reps { get; set; }
        public decimal? Weight { get; set; }
        public string Notes { get; set; }
    }

    public class WorkoutDataPortabilityService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<WorkoutDataPortabilityService> _logger;
        private readonly UserManager<AppUser> _userManager;

        // Add event handler for progress updates
        public event EventHandler<ProgressUpdateEventArgs> OnProgressUpdate;

        public WorkoutDataPortabilityService(
            IServiceProvider serviceProvider,
            WorkoutTrackerWebContext context,
            ILogger<WorkoutDataPortabilityService> logger,
            UserManager<AppUser> userManager)
        {
            _serviceProvider = serviceProvider;
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<WorkoutExport> ExportUserDataAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var user = await _context.User.FindAsync(userId);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }

            var appUser = await _userManager.FindByIdAsync(user.IdentityUserId);
            var email = appUser?.Email ?? "unknown@example.com";

            var export = new WorkoutExport
            {
                ExportDate = DateTime.UtcNow,
                Version = "1.0",
                User = new UserExport 
                { 
                    Name = user.Name,
                    Email = email
                },
                Sessions = new List<SessionExport>(),
                ExerciseTypes = new List<ExerciseTypeExport>(),
                SetTypes = new List<SetTypeExport>()
            };

            var sessions = await GetUserSessionsAsync(userId, startDate, endDate);
            ReportProgress("Starting export...", 0, sessions.Count);

            int processedCount = 0;
            foreach (var session in sessions)
            {
                var sessionExport = new SessionExport
                {
                    Name = session.Name,
                    DateTime = session.StartDateTime,
                    Sets = new List<SetExport>()
                };

                foreach (var exercise in session.WorkoutExercises)
                {
                    foreach (var set in exercise.WorkoutSets)
                    {
                        var setExport = new SetExport
                        {
                            Notes = set.Notes ?? string.Empty, // Handle null notes
                            ExerciseTypeName = exercise.ExerciseType?.Name ?? string.Empty, // Handle null exercise type name
                            SetTypeName = set.Settype?.Name ?? string.Empty, // Handle null set type name
                            NumberReps = set.Reps ?? 0,
                            Weight = set.Weight ?? 0,
                            Reps = new List<RepExport>()
                        };

                        sessionExport.Sets.Add(setExport);
                    }
                }

                export.Sessions.Add(sessionExport);
                processedCount++;
                
                ReportProgress("Exporting session data...", processedCount, sessions.Count, session.Name);
            }

            // Add all exercise types
            var exerciseTypes = await _context.ExerciseType.ToListAsync();
            foreach (var exerciseType in exerciseTypes)
            {
                export.ExerciseTypes.Add(new ExerciseTypeExport
                {
                    Name = exerciseType.Name,
                    Description = exerciseType.Description ?? ""
                });
            }

            // Add all set types
            var setTypes = await _context.Settype.ToListAsync();
            foreach (var setType in setTypes)
            {
                export.SetTypes.Add(new SetTypeExport
                {
                    Name = setType.Name,
                    Description = setType.Description ?? ""
                });
            }

            ReportProgress("Export completed", sessions.Count, sessions.Count);
            return export;
        }

        public async Task<(bool success, string message, List<string> importedItems)> ImportUserDataAsync(
            int userId, 
            string jsonData, 
            bool skipExisting = true)
        {
            var importedItems = new List<string>();
            
            try
            {
                var importData = JsonSerializer.Deserialize<List<WorkoutImportDto>>(jsonData);
                int importedCount = 0;
                
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    foreach (var sessionData in importData)
                    {
                        if (!sessionData.IsValid())
                        {
                            _logger.LogWarning("Skipping invalid session data");
                            continue;
                        }

                        // Skip if session already exists
                        if (skipExisting && await _context.WorkoutSessions.AnyAsync(s => 
                            s.UserId == userId && 
                            s.Name == sessionData.Name && 
                            s.StartDateTime == sessionData.DateTime))
                        {
                            _logger.LogInformation($"Skipping existing session: {sessionData.Name}");
                            continue;
                        }

                        var session = new WorkoutSession
                        {
                            UserId = userId,
                            Name = sessionData.Name,
                            StartDateTime = sessionData.DateTime,
                            Status = "Completed",
                            IsCompleted = true
                        };

                        _context.WorkoutSessions.Add(session);
                        await _context.SaveChangesAsync();

                        // Group sets by exercise type
                        var exerciseGroups = sessionData.Sets.GroupBy(s => s.ExerciseTypeId);
                        foreach (var exerciseGroup in exerciseGroups)
                        {
                            var workoutExercise = new WorkoutExercise
                            {
                                WorkoutSessionId = session.WorkoutSessionId,
                                ExerciseTypeId = exerciseGroup.Key,
                                SequenceNum = exerciseGroup.Min(s => s.SequenceNum)
                            };

                            _context.WorkoutExercises.Add(workoutExercise);
                            await _context.SaveChangesAsync();

                            foreach (var setData in exerciseGroup)
                            {
                                var workoutSet = new WorkoutSet
                                {
                                    WorkoutExerciseId = workoutExercise.WorkoutExerciseId,
                                    SequenceNum = setData.SequenceNum,
                                    Reps = setData.Reps,
                                    Weight = setData.Weight,
                                    Notes = setData.Notes,
                                    IsCompleted = true
                                };

                                _context.WorkoutSets.Add(workoutSet);
                            }
                        }

                        await _context.SaveChangesAsync();
                        importedItems.Add(sessionData.Name);
                        importedCount++;

                        ReportProgress("Importing workout data...", importedCount, importData.Count, sessionData.Name);
                    }

                    await transaction.CommitAsync();
                    return (true, $"Successfully imported {importedCount} workout sessions", importedItems);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error during workout data import");
                    return (false, $"Error importing workout data: {ex.Message}", importedItems);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing workout data JSON");
                return (false, $"Error parsing workout data: {ex.Message}", importedItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during workout data import");
                return (false, $"Unexpected error: {ex.Message}", importedItems);
            }
        }

        private async Task<List<WorkoutSession>> GetUserSessionsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.WorkoutSessions
                .AsSplitQuery() // Use split queries for better performance with multiple includes
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.WorkoutSets)
                        .ThenInclude(s => s.Settype)
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.ExerciseType)
                .Where(ws => ws.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(s => s.StartDateTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.StartDateTime <= endDate.Value);

            return await query.OrderByDescending(s => s.StartDateTime).ToListAsync();
        }

        private void ReportProgress(string status, int current, int total, string currentItem = null, string errorMessage = null)
        {
            var progress = (double)current / total * 100;
            _logger.LogInformation($"Progress: {progress:F1}% - {status}");
            if (!string.IsNullOrEmpty(currentItem))
                _logger.LogInformation($"Processing: {currentItem}");
            if (!string.IsNullOrEmpty(errorMessage))
                _logger.LogError($"Error: {errorMessage}");
                
            // Trigger the OnProgressUpdate event
            OnProgressUpdate?.Invoke(this, new ProgressUpdateEventArgs
            {
                PercentComplete = (int)progress,
                Message = string.IsNullOrEmpty(currentItem) ? status : $"{status} - {currentItem}"
            });
        }

        private async Task<string> GetUserEmailAsync(User user)
        {
            var identityUser = await _userManager.FindByIdAsync(user.IdentityUserId);
            return identityUser?.Email ?? "unknown";
        }
    }

    public class WorkoutExport
    {
        public DateTime ExportDate { get; set; }
        public string Version { get; set; }
        public UserExport User { get; set; }
        public List<SessionExport> Sessions { get; set; }
        public List<ExerciseTypeExport> ExerciseTypes { get; set; }
        public List<SetTypeExport> SetTypes { get; set; }
    }

    public class UserExport
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public class SessionExport
    {
        public string Name { get; set; }
        public DateTime DateTime { get; set; }
        public List<SetExport> Sets { get; set; }
    }

    public class SetExport
    {
        public string Notes { get; set; }
        public int NumberReps { get; set; }
        public decimal Weight { get; set; }
        public string ExerciseTypeName { get; set; }
        public string SetTypeName { get; set; }
        public List<RepExport> Reps { get; set; }
    }

    public class RepExport
    {
        public int RepNumber { get; set; }
        public decimal Weight { get; set; }
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