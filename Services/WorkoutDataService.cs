using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using WorkoutTrackerWeb.Pages.Reports;

namespace WorkoutTrackerWeb.Services
{
    public class WorkoutDataService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<WorkoutDataService> _logger;
        private readonly IDistributedCache _cache;
        private const int BatchSize = 500; // Increased batch size for better performance
        
        // Event for progress reporting
        public Action<JobProgress> OnProgressUpdate { get; set; }
        
        public WorkoutDataService(
            WorkoutTrackerWebContext context,
            ILogger<WorkoutDataService> logger,
            IDistributedCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        // Helper method to invalidate the cached report data when data changes
        private void InvalidateReportCache(int userId)
        {
            try
            {
                _logger.LogInformation($"Invalidating report cache for user {userId}");
                IndexModel.InvalidateCache(_cache, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error invalidating cache for user {userId}");
                // We don't want cache invalidation failures to break data operations
                // Just log and continue
            }
        }

        public async Task DeleteAllWorkoutDataAsync(string identityUserId)
        {
            _logger.LogInformation($"Starting to delete all workout data for user {identityUserId}");
            
            // First get the application User record associated with the Identity user
            var user = await _context.User
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);

            if (user == null)
            {
                _logger.LogWarning($"User with IdentityUserId {identityUserId} not found");
                throw new ArgumentException("User not found", nameof(identityUserId));
            }
            
            // Calculate total count of items to delete for progress tracking
            var totalSessions = await _context.Session.CountAsync(s => s.UserId == user.UserId);
            var sessionIds = await _context.Session
                .Where(s => s.UserId == user.UserId)
                .Select(s => s.SessionId)
                .ToListAsync();
                
            var totalSets = await _context.Set
                .Where(s => sessionIds.Contains(s.SessionId))
                .CountAsync();
                
            var setIds = await _context.Set
                .Where(s => sessionIds.Contains(s.SessionId))
                .Select(s => s.SetId)
                .ToListAsync();
                
            var totalReps = await _context.Rep
                .Where(r => setIds.Contains(r.SetsSetId ?? 0))
                .CountAsync();
            
            var totalItems = totalReps + totalSets + totalSessions;
            int deletedItems = 0;
            
            // Initialize progress
            ReportProgress("Starting deletion", 0, totalItems, deletedItems, "Preparing");
            
            // Use a transaction to ensure all deletions succeed or none do
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // First, bulk delete all reps directly with a single SQL operation
                _logger.LogInformation("Deleting reps in bulk");
                string deleteRepsQuery = @"
                    DELETE r FROM Rep r
                    INNER JOIN [Set] s ON r.SetsSetId = s.SetId
                    INNER JOIN Session ses ON s.SessionId = ses.SessionId
                    WHERE ses.UserId = @userId";
                
                int repsDeleted = await _context.Database.ExecuteSqlRawAsync(deleteRepsQuery, 
                    new Microsoft.Data.SqlClient.SqlParameter("@userId", user.UserId));
                
                deletedItems += repsDeleted > 0 ? repsDeleted : totalReps;
                ReportProgress("Deleted all repetitions", 
                    (deletedItems * 100) / Math.Max(totalItems, 1), 
                    totalItems, deletedItems, "Deleting sets");
                
                // Then bulk delete all sets
                _logger.LogInformation("Deleting sets in bulk");
                string deleteSetsQuery = @"
                    DELETE s FROM [Set] s
                    INNER JOIN Session ses ON s.SessionId = ses.SessionId
                    WHERE ses.UserId = @userId";
                
                int setsDeleted = await _context.Database.ExecuteSqlRawAsync(deleteSetsQuery, 
                    new Microsoft.Data.SqlClient.SqlParameter("@userId", user.UserId));
                
                deletedItems += setsDeleted > 0 ? setsDeleted : totalSets;
                ReportProgress("Deleted all sets", 
                    (deletedItems * 100) / Math.Max(totalItems, 1), 
                    totalItems, deletedItems, "Deleting sessions");
                
                // Finally delete the sessions
                _logger.LogInformation("Deleting sessions in bulk");
                string deleteSessionsQuery = @"
                    DELETE FROM Session
                    WHERE UserId = @userId";
                
                int sessionsDeleted = await _context.Database.ExecuteSqlRawAsync(deleteSessionsQuery, 
                    new Microsoft.Data.SqlClient.SqlParameter("@userId", user.UserId));
                
                deletedItems += sessionsDeleted > 0 ? sessionsDeleted : totalSessions;
                
                // If the SQL approach doesn't work for some reason, fall back to EF
                if (repsDeleted == 0 && setsDeleted == 0 && sessionsDeleted == 0)
                {
                    _logger.LogWarning("SQL-based bulk delete did not affect any rows, falling back to EF Core");
                    await DeleteAllWorkoutDataWithEFAsync(user.UserId, totalItems);
                }
                
                await transaction.CommitAsync();
                
                // Invalidate cache after successful deletion
                InvalidateReportCache(user.UserId);
                
                // Final progress report
                ReportProgress("Deletion complete", 100, totalItems, deletedItems, "Completed");
                _logger.LogInformation($"Successfully deleted all workout data for user {identityUserId}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error deleting workout data for user {identityUserId}");
                ReportProgress("Error during deletion", 0, totalItems, deletedItems, "Error", ex.Message);
                throw;
            }
        }
        
        // Fallback method using EF Core for deletion
        private async Task DeleteAllWorkoutDataWithEFAsync(int userId, int totalItems)
        {
            int deletedItems = 0;
            int processedSessions = 0;
            
            // Process sessions in batches
            while (true)
            {
                var sessionBatch = await _context.Session
                    .Where(s => s.UserId == userId)
                    .Take(BatchSize)
                    .ToListAsync();

                if (!sessionBatch.Any())
                {
                    break;
                }

                foreach (var session in sessionBatch)
                {
                    processedSessions++;
                    ReportProgress("Processing session", 
                        (deletedItems * 100) / Math.Max(totalItems, 1), 
                        totalItems, deletedItems, $"Session {processedSessions}");
                    
                    // Get all sets for this session to optimize queries
                    var sets = await _context.Set
                        .Where(s => s.SessionId == session.SessionId)
                        .ToListAsync();
                        
                    foreach (var set in sets)
                    {
                        // Delete reps in batches for performance
                        while (true)
                        {
                            var repsBatch = await _context.Rep
                                .Where(r => r.SetsSetId == set.SetId)
                                .Take(BatchSize)
                                .ToListAsync();

                            if (!repsBatch.Any())
                            {
                                break;
                            }

                            _context.Rep.RemoveRange(repsBatch);
                            await _context.SaveChangesAsync();
                            deletedItems += repsBatch.Count;
                            
                            ReportProgress("Deleting reps", 
                                (deletedItems * 100) / Math.Max(totalItems, 1), 
                                totalItems, deletedItems, $"Session {processedSessions}");
                        }
                    }
                    
                    // Delete all sets for this session at once
                    if (sets.Any())
                    {
                        _context.Set.RemoveRange(sets);
                        await _context.SaveChangesAsync();
                        deletedItems += sets.Count;
                        
                        ReportProgress("Deleting sets", 
                            (deletedItems * 100) / Math.Max(totalItems, 1), 
                            totalItems, deletedItems, $"Session {processedSessions}");
                    }
                }
                
                // Delete the processed session batch
                _context.Session.RemoveRange(sessionBatch);
                await _context.SaveChangesAsync();
                deletedItems += sessionBatch.Count;
                
                ReportProgress("Deleting sessions", 
                    (deletedItems * 100) / Math.Max(totalItems, 1), 
                    totalItems, deletedItems, $"Completed {processedSessions} sessions");
            }
        }
        
        private void ReportProgress(string status, int percentComplete, int totalItems, int processedItems, string details, string error = null)
        {
            if (OnProgressUpdate != null)
            {
                var progress = new JobProgress
                {
                    Status = status,
                    PercentComplete = percentComplete,
                    TotalItems = totalItems,
                    ProcessedItems = processedItems,
                    Details = details,
                    ErrorMessage = error
                };
                
                OnProgressUpdate(progress);
            }
        }
    }
}