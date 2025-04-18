using System;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server; // Add this for PerformContext
using Hangfire.Storage; // Add this for JobActivatorScope
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Hubs;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using WorkoutTrackerweb.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore; // Add this for Entity Framework extensions

namespace WorkoutTrackerWeb.Services
{
    public class BackgroundJobService
    {
        private readonly ILogger<BackgroundJobService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<ImportProgressHub> _hubContext;

        public BackgroundJobService(
            ILogger<BackgroundJobService> logger,
            IServiceProvider serviceProvider,
            IHubContext<ImportProgressHub> hubContext)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
        }

        // Queue delete all workout data as a background job
        public string QueueDeleteAllWorkoutData(string userId, string connectionId)
        {
            _logger.LogInformation($"Queuing delete all workout data job for user {userId}");
            try
            {
                string jobId = BackgroundJob.Enqueue(() => DeleteAllWorkoutDataAsync(userId, connectionId));
                _logger.LogInformation($"Successfully queued delete data job with ID {jobId}");
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue delete data job for user {UserId}", userId);
                throw; // Re-throw to show error to user
            }
        }

        // This method will be called by Hangfire in the background
        public async Task DeleteAllWorkoutDataAsync(string identityUserId, string connectionId)
        {
            // Get job ID from context
            string jobId = "unknown";
            try 
            {
                // Try to get the job ID from the PerformContext if available through JobActivator
                var context = JobActivatorScope.Current?.Resolve(typeof(PerformContext)) as PerformContext;
                if (context != null)
                {
                    jobId = context.BackgroundJob.Id;
                    _logger.LogInformation("Resolved job ID {JobId} from PerformContext", jobId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get job ID from context");
            }
            
            _logger.LogInformation("Starting background delete operation for user {IdentityUserId}, job {JobId}, connectionId {ConnectionId}", 
                identityUserId, jobId, connectionId);
            
            try
            {
                // Create a new service scope to get required services
                using (var scope = _serviceProvider.CreateScope())
                {
                    var workoutDataService = scope.ServiceProvider.GetRequiredService<WorkoutDataService>();
                    
                    // Initialize progress reporting with retries for connection issues
                    var initProgress = new JobProgress { 
                        Status = "Initializing deletion...", 
                        PercentComplete = 0,
                        Details = "Preparing to delete workout data"
                    };
                    
                    // Send progress updates to both the specific client and the job group
                    await SendJobUpdateWithRetriesAsync(connectionId, jobId, initProgress);
                    
                    // Set up the progress callback
                    workoutDataService.OnProgressUpdate = async (jobProgress) => {
                        // Add the job ID to the progress data for logging
                        _logger.LogDebug("Job {JobId} progress: {Status} {PercentComplete}%", 
                            jobId, jobProgress.Status, jobProgress.PercentComplete);
                            
                        await SendJobUpdateWithRetriesAsync(connectionId, jobId, jobProgress);
                    };
                    
                    // Execute the actual delete operation
                    var startTime = DateTime.UtcNow;
                    await workoutDataService.DeleteAllWorkoutDataAsync(identityUserId);
                    var duration = DateTime.UtcNow - startTime;
                    
                    _logger.LogInformation("Delete operation completed in {Duration} seconds", duration.TotalSeconds);
                    
                    // Report completion
                    var finalProgress = new JobProgress { 
                        Status = "Completed", 
                        PercentComplete = 100,
                        Details = $"Successfully deleted all workout data in {duration.TotalSeconds:F1} seconds"
                    };
                    await SendJobUpdateWithRetriesAsync(connectionId, jobId, finalProgress);
                }
                
                _logger.LogInformation("Successfully completed delete operation for user {IdentityUserId}, job {JobId}", 
                    identityUserId, jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during background delete operation for user {IdentityUserId}, job {JobId}", 
                    identityUserId, jobId);
                
                // Report error
                var errorProgress = new JobProgress { 
                    Status = "Error", 
                    PercentComplete = 0,
                    ErrorMessage = "An error occurred while deleting workout data: " + ex.Message 
                };
                await SendJobUpdateWithRetriesAsync(connectionId, jobId, errorProgress);
                
                // Re-throw to ensure Hangfire marks the job as failed
                throw;
            }
        }

        // Queue TrainAI import as a background job
        public string QueueTrainAIImport(int userId, List<TrainAIWorkout> workouts, string connectionId)
        {
            _logger.LogInformation($"Queuing TrainAI import job for user {userId} with {workouts.Count} workouts");
            
            try
            {
                // Validate inputs to identify potential serialization issues
                if (userId <= 0)
                {
                    _logger.LogError("Invalid userId: {UserId}", userId);
                    throw new ArgumentException("Invalid user ID", nameof(userId));
                }
                
                if (workouts == null || workouts.Count == 0)
                {
                    _logger.LogError("No workouts provided for import (null or empty collection)");
                    throw new ArgumentException("No workouts provided for import", nameof(workouts));
                }
                
                if (string.IsNullOrEmpty(connectionId))
                {
                    _logger.LogWarning("Empty connectionId provided. Real-time updates may not work.");
                    // Don't throw, just warn - connection ID can be empty in some scenarios
                }

                // Log details for troubleshooting
                _logger.LogDebug("TrainAI import details: {WorkoutCount} workouts, connectionId: {ConnectionId}", 
                    workouts.Count, connectionId);

                // Use try-catch to identify specific Hangfire issues
                string jobId;
                try
                {
                    // Attempt to create the background job
                    jobId = BackgroundJob.Enqueue(() => ImportTrainAIWorkoutsAsync(userId, workouts, connectionId));
                    _logger.LogInformation("Successfully queued TrainAI import job with ID {JobId}", jobId);
                    
                    // Add the connection to a SignalR group for this job
                    // This allows the client to receive updates even if they reconnect
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        // We need to do this when queueing, not just in the background job
                        // because by then the client may have already connected to SignalR
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var importHub = scope.ServiceProvider.GetRequiredService<IHubContext<ImportProgressHub>>();
                            importHub.Groups.AddToGroupAsync(connectionId, $"job_{jobId}").GetAwaiter().GetResult();
                            _logger.LogDebug("Added connection {ConnectionId} to group job_{JobId}", connectionId, jobId);
                        }
                    }
                }
                catch (InvalidOperationException iex)
                {
                    // This typically happens when Hangfire storage is not properly initialized
                    _logger.LogError(iex, "Hangfire initialization error when queuing TrainAI import job. Check Hangfire configuration.");
                    throw new InvalidOperationException("Background job system not initialized. Please contact support.", iex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unknown error when queuing TrainAI import job");
                    throw;
                }
                
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue TrainAI import job for user {UserId}", userId);
                throw; // Re-throw to show error to user
            }
        }

        // This method will be called by Hangfire in the background
        public async Task ImportTrainAIWorkoutsAsync(int userId, List<TrainAIWorkout> workouts, string connectionId)
        {
            // Get job ID from context
            string jobId = "unknown";
            try 
            {
                // Try to get the job ID from the PerformContext if available through JobActivator
                var context = JobActivatorScope.Current?.Resolve(typeof(PerformContext)) as PerformContext;
                if (context != null)
                {
                    jobId = context.BackgroundJob.Id;
                    _logger.LogInformation("Resolved job ID {JobId} from PerformContext", jobId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get job ID from context");
            }
            
            _logger.LogInformation("Starting background TrainAI import operation for user {UserId}, job {JobId}, connectionId {ConnectionId}", 
                userId, jobId, connectionId);
            
            try
            {
                // Create a new service scope to get required services
                using (var scope = _serviceProvider.CreateScope())
                {
                    var trainAIImportService = scope.ServiceProvider.GetRequiredService<TrainAIImportService>();
                    
                    // Initialize progress reporting with retries for connection issues
                    var initProgress = new JobProgress { 
                        Status = "Initializing import...", 
                        PercentComplete = 0,
                        TotalItems = workouts.Count,
                        ProcessedItems = 0,
                        Details = "Preparing data structures and validating workouts"
                    };
                    
                    // Send progress updates to both the specific client and the job group
                    await SendJobUpdateWithRetriesAsync(connectionId, jobId, initProgress);
                    
                    // Set up the progress callback with more detail
                    trainAIImportService.OnProgressUpdateV2 = async (jobProgress) => {
                        // Add the job ID to the progress data for logging
                        _logger.LogDebug("Job {JobId} progress: {Status} {PercentComplete}%", 
                            jobId, jobProgress.Status, jobProgress.PercentComplete);
                            
                        await SendJobUpdateWithRetriesAsync(connectionId, jobId, jobProgress);
                    };
                    
                    // Log before starting the actual import
                    _logger.LogInformation("Beginning import process for {WorkoutCount} workouts, user {UserId}, job {JobId}", 
                        workouts.Count, userId, jobId);
                    
                    // Execute the actual import operation
                    var startTime = DateTime.UtcNow;
                    var result = await trainAIImportService.ImportTrainAIWorkoutsAsync(userId, workouts);
                    var duration = DateTime.UtcNow - startTime;
                    
                    _logger.LogInformation("Import completed in {Duration} with result: success={Success}, message={Message}", 
                        duration, result.success, result.message);
                    
                    // Report completion or failure if not already reported
                    if (result.success)
                    {
                        var finalProgress = new JobProgress { 
                            Status = "Completed", 
                            PercentComplete = 100,
                            Details = $"Successfully imported {workouts.Count} workouts in {duration.TotalSeconds:F1} seconds",
                            ProcessedItems = workouts.Count,
                            TotalItems = workouts.Count
                        };
                        await SendJobUpdateWithRetriesAsync(connectionId, jobId, finalProgress);
                    }
                    else if (!string.IsNullOrEmpty(result.message))
                    {
                        var errorProgress = new JobProgress { 
                            Status = "Error", 
                            PercentComplete = 0,
                            ErrorMessage = result.message,
                            ProcessedItems = result.importedItems?.Count ?? 0,
                            TotalItems = workouts.Count
                        };
                        await SendJobUpdateWithRetriesAsync(connectionId, jobId, errorProgress);
                        
                        // Re-throw to mark the job as failed in Hangfire
                        throw new Exception($"Import failed: {result.message}");
                    }
                }
                
                _logger.LogInformation("Successfully completed TrainAI import for user {UserId}, job {JobId}", userId, jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during background TrainAI import for user {UserId}, job {JobId}", userId, jobId);
                
                // Report error
                var errorProgress = new JobProgress { 
                    Status = "Error", 
                    PercentComplete = 0,
                    ErrorMessage = "An error occurred during import: " + ex.Message 
                };
                await SendJobUpdateWithRetriesAsync(connectionId, jobId, errorProgress);
                
                // Re-throw to ensure Hangfire marks the job as failed
                throw;
            }
        }
        
        // Enhanced version of SendJobUpdateAsync with retries for resilience
        private async Task SendJobUpdateWithRetriesAsync(string connectionId, string jobId, JobProgress progress, int maxRetries = 3)
        {
            int retryCount = 0;
            bool success = false;
            
            while (!success && retryCount < maxRetries)
            {
                try
                {
                    // First try direct connection if available
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        try
                        {
                            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveProgress", progress);
                            _logger.LogDebug("Sent progress update to connectionId={ConnectionId}: {Status} {PercentComplete}%", 
                                connectionId, progress.Status, progress.PercentComplete);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to send progress to connectionId={ConnectionId}, will try group", connectionId);
                        }
                    }
                    
                    // Always send to job group for reliability with reconnects
                    if (!string.IsNullOrEmpty(jobId))
                    {
                        string groupName = $"job_{jobId}";
                        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveProgress", progress);
                        _logger.LogDebug("Sent progress update to group {GroupName}: {Status} {PercentComplete}%", 
                            groupName, progress.Status, progress.PercentComplete);
                    }
                    
                    success = true;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogError(ex, "Error sending job progress update (attempt {RetryCount}/{MaxRetries})", 
                        retryCount, maxRetries);
                    
                    if (retryCount < maxRetries)
                    {
                        // Exponential backoff between retries
                        await Task.Delay(100 * (int)Math.Pow(2, retryCount));
                    }
                }
            }
            
            if (!success)
            {
                _logger.LogError("Failed to send progress update after {MaxRetries} attempts. Status: {Status}", 
                    maxRetries, progress.Status);
            }
        }

        // New generic import processing method for the dashboard
        public async Task ProcessImportAsync(string importName)
        {
            _logger.LogInformation($"Processing import job: {importName}");
            
            try
            {
                // Simulate a long-running process
                for (int i = 0; i <= 100; i += 10)
                {
                    _logger.LogDebug($"Import {importName} progress: {i}%");
                    await Task.Delay(500); // Simulate work
                }
                
                _logger.LogInformation($"Import job completed successfully: {importName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing import job: {importName}");
                throw; // Rethrow so Hangfire marks it as failed
            }
        }
        
        // Method to generate reports
        public async Task ProcessReportAsync(string reportName)
        {
            _logger.LogInformation($"Generating report: {reportName}");
            
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
                    
                    // Simulate report generation
                    _logger.LogDebug("Collecting workout data...");
                    await Task.Delay(1000);
                    
                    var sessionCount = await context.Session.CountAsync();
                    var setCount = await context.Set.CountAsync();
                    
                    _logger.LogDebug($"Analyzing {sessionCount} sessions and {setCount} sets...");
                    await Task.Delay(2000);
                    
                    _logger.LogDebug("Finalizing report...");
                    await Task.Delay(500);
                    
                    _logger.LogInformation($"Report '{reportName}' generated successfully with data from {sessionCount} sessions");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating report: {reportName}");
                throw; // Let Hangfire know it failed
            }
        }
        
        // Method to clean up old data
        public async Task ProcessDataCleanupAsync(DateTime olderThan)
        {
            _logger.LogInformation($"Starting data cleanup for records older than {olderThan}");
            
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWebContext>();
                    
                    // This is just a simulation - in a real app you'd delete actual old data
                    _logger.LogDebug("Identifying old records...");
                    await Task.Delay(1000);
                    
                    _logger.LogDebug("Backing up data before deletion...");
                    await Task.Delay(1500);
                    
                    _logger.LogDebug("Removing old records...");
                    await Task.Delay(2000);
                    
                    _logger.LogInformation($"Data cleanup completed for records older than {olderThan}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during data cleanup for records older than {olderThan}");
                throw;
            }
        }
        
        // Helper method to verify Hangfire is properly configured
        public bool ValidateHangfireConfiguration()
        {
            try
            {
                // Attempt to access the Hangfire storage
                var storageConnection = JobStorage.Current.GetConnection();
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                var servers = monitoringApi.Servers();
                
                bool isHangfireRunning = servers.Count() > 0;
                _logger.LogInformation("Hangfire validation: found {ServerCount} active servers", servers.Count());
                
                // Log the names of active servers
                foreach (var server in servers)
                {
                    _logger.LogDebug("Active Hangfire server: {0}, Queues: {1}", 
                        server.Name, string.Join(", ", server.Queues));
                }
                
                return isHangfireRunning;
            }
            catch (InvalidOperationException ioe)
            {
                _logger.LogError(ioe, "Hangfire storage not properly configured or initialized");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Hangfire configuration");
                return false;
            }
        }

        // Helper method to check if a job is in progress
        public bool IsJobInProgress(string jobId)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                    return false;
                
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                
                // Check if job is in the processing jobs list
                var processingJobs = monitoringApi.ProcessingJobs(0, 1000);
                if (processingJobs.Any(j => j.Key == jobId))
                    return true;
                
                // Check if job is in the scheduled jobs list
                var scheduledJobs = monitoringApi.ScheduledJobs(0, 1000);  
                if (scheduledJobs.Any(j => j.Key == jobId))
                    return true;
                
                // Check if job is in the enqueued jobs list
                var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, 1000);
                if (enqueuedJobs.Any(j => j.Key == jobId))
                    return true;
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if job {JobId} is in progress", jobId);
                return false;
            }
        }
    }

    // Common job progress class for consistent reporting
    public class JobProgress
    {
        public string Status { get; set; }
        public int PercentComplete { get; set; }
        public string CurrentItem { get; set; }
        public int ProcessedItems { get; set; }
        public int TotalItems { get; set; }
        public string Details { get; set; }
        public string ErrorMessage { get; set; }
    }
}