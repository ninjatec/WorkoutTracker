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
using WorkoutTrackerWeb.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore; // Add this for Entity Framework extensions
using Newtonsoft.Json; // Add this for JSON serialization
using WorkoutTrackerWeb.Pages.BackgroundJobs; // For TrainAIImportData model
using System.IO; // Add this for file operations

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
                    var context = scope.ServiceProvider.GetRequiredService<WorkoutTrackerWeb.Data.WorkoutTrackerWebContext>();
                    var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();
                    
                    // Initialize progress reporting with retries for connection issues
                    var initProgress = new JobProgress { 
                        Status = "Initializing deletion...", 
                        PercentComplete = 0,
                        Details = "Preparing to delete workout data"
                    };
                    
                    // Send progress updates to both the specific client and the job group
                    await SendJobUpdateWithRetriesAsync(connectionId, jobId, initProgress);
                    
                    // Validate user before proceeding
                    var user = await context.User.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
                    
                    if (user == null)
                    {
                        _logger.LogWarning("User with IdentityUserId {IdentityUserId} not found directly. Attempting alternative lookup methods.", identityUserId);
                        
                        // Try to find the identity user first to make sure it exists
                        var identityUser = await userManager.FindByIdAsync(identityUserId);
                        if (identityUser == null)
                        {
                            var errorProgress = new JobProgress { 
                                Status = "Error", 
                                PercentComplete = 0,
                                ErrorMessage = $"Identity user not found. Please contact support with reference: {jobId}"
                            };
                            await SendJobUpdateWithRetriesAsync(connectionId, jobId, errorProgress);
                            throw new Exception($"Identity user not found for ID: {identityUserId}");
                        }
                        
                        // Create a new application user record
                        _logger.LogInformation("Creating new application user for identity user {IdentityUserId}", identityUserId);
                        user = new Models.User
                        {
                            IdentityUserId = identityUserId,
                            Name = identityUser.UserName ?? "User" // Use username or fallback
                        };
                        
                        context.User.Add(user);
                        await context.SaveChangesAsync();
                        
                        _logger.LogInformation("Created new user {UserId} for identity user {IdentityUserId}", 
                            user.UserId, identityUserId);
                    }
                    
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

        // Queue TrainAI import as a background job using a more serialization-friendly approach
        public string QueueTrainAIImport(int userId, List<TrainAIWorkout> workouts, string connectionId)
        {
            _logger.LogInformation($"Queuing TrainAI import job for user {userId} with {workouts.Count} workouts");
            
            try
            {
                // Validate inputs
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
                
                // Create a serialization-friendly parameter object instead of passing complex objects directly
                var importData = new TrainAIImportData
                {
                    UserId = userId,
                    Workouts = workouts,
                    ConnectionId = connectionId
                };
                
                // Verify we can serialize it to avoid issues
                try 
                {
                    // Test serialization to ensure data can be sent to Redis/SQL
                    var testSerialization = JsonConvert.SerializeObject(
                        importData, 
                        new JsonSerializerSettings 
                        { 
                            TypeNameHandling = TypeNameHandling.None,
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                        });
                    
                    _logger.LogDebug("Serialization test successful, payload size: {SizeBytes} bytes", 
                        testSerialization.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to serialize import data");
                    throw new InvalidOperationException("Unable to process workout data for background import. The data may be corrupted.", ex);
                }

                // Use try-catch to identify specific Hangfire issues
                string jobId;
                try
                {
                    // First verify Hangfire is operational with a simple ping job
                    var pingJobId = BackgroundJob.Enqueue(() => Console.WriteLine($"Hangfire ping at {DateTime.Now}"));
                    _logger.LogDebug("Hangfire ping successful with job ID: {PingJobId}", pingJobId);
                    
                    // Now enqueue the real job with explicit type arguments to avoid serialization issues
                    jobId = BackgroundJob.Enqueue<BackgroundJobService>(x => 
                        x.ProcessTrainAIImportAsync(importData));
                    
                    _logger.LogInformation("Successfully queued TrainAI import job with ID {JobId}", jobId);
                    
                    // Add the connection to a SignalR group for this job
                    if (!string.IsNullOrEmpty(connectionId))
                    {
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
                    _logger.LogError(iex, "Hangfire initialization error. Checking schema health...");
                    
                    // Try to get diagnostic info to help troubleshoot
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        try
                        {
                            var hangfireInit = scope.ServiceProvider.GetRequiredService<Services.Hangfire.IHangfireInitializationService>();
                            var diagnosticInfo = hangfireInit.GetDiagnosticInfo();
                            _logger.LogError("Hangfire diagnostics: {DiagnosticInfo}", diagnosticInfo);
                            
                            // Try to repair the schema
                            var repairResult = hangfireInit.RepairHangfireSchemaAsync().GetAwaiter().GetResult();
                            _logger.LogWarning("Hangfire schema repair attempt result: {RepairResult}", repairResult);
                        }
                        catch (Exception diagEx)
                        {
                            _logger.LogError(diagEx, "Error getting Hangfire diagnostics");
                        }
                    }
                    
                    throw new InvalidOperationException("Background job system not initialized. Please try again or contact support if the issue persists.", iex);
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

        // New method to process import from serialization-friendly data
        public async Task ProcessTrainAIImportAsync(TrainAIImportData importData)
        {
            // Extract parameters from the data wrapper
            int userId = importData.UserId;
            List<TrainAIWorkout> workouts = importData.Workouts;
            string connectionId = importData.ConnectionId;
            
            _logger.LogInformation("Starting TrainAI import from serialized data: UserId={UserId}, Workouts={WorkoutCount}", 
                userId, workouts?.Count ?? 0);
                
            // Delegate to the original implementation
            await ImportTrainAIWorkoutsAsync(userId, workouts, connectionId);
        }

        // This method is kept as private or internal now since we'll call it through ProcessTrainAIImportAsync
        private async Task ImportTrainAIWorkoutsAsync(int userId, List<TrainAIWorkout> workouts, string connectionId)
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
                            await _hubContext.Clients.Client(connectionId).SendAsync("receiveProgress", progress);
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
                        await _hubContext.Clients.Group(groupName).SendAsync("receiveProgress", progress);
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

        // Queue JSON import as a background job
        public string QueueJsonImport(int userId, string jsonContent, bool skipExisting, string connectionId)
        {
            _logger.LogInformation($"Queuing JSON import job for user {userId}");
            
            try
            {
                // Validate inputs
                if (userId <= 0)
                {
                    _logger.LogError("Invalid userId: {UserId}", userId);
                    throw new ArgumentException("Invalid user ID", nameof(userId));
                }
                
                if (string.IsNullOrEmpty(jsonContent))
                {
                    _logger.LogError("No JSON content provided for import");
                    throw new ArgumentException("No JSON content provided for import", nameof(jsonContent));
                }
                
                // Create a serialization-friendly parameter object
                var importData = new JsonImportData
                {
                    UserId = userId,
                    JsonContent = jsonContent,
                    SkipExisting = skipExisting,
                    ConnectionId = connectionId
                };
                
                // Queue the job
                string jobId;
                try
                {
                    // Enqueue the job with explicit type arguments to avoid serialization issues
                    jobId = BackgroundJob.Enqueue<BackgroundJobService>(x => 
                        x.ProcessJsonImportAsync(importData));
                    
                    _logger.LogInformation("Successfully queued JSON import job with ID {JobId}", jobId);
                    
                    // Add the connection to a SignalR group for this job
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var importHub = scope.ServiceProvider.GetRequiredService<IHubContext<ImportProgressHub>>();
                            importHub.Groups.AddToGroupAsync(connectionId, $"job_{jobId}").GetAwaiter().GetResult();
                            _logger.LogDebug("Added connection {ConnectionId} to group job_{JobId}", connectionId, jobId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error when queuing JSON import job");
                    throw;
                }
                
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue JSON import job for user {UserId}", userId);
                throw; // Re-throw to show error to user
            }
        }

        // Method to process JSON import as a background job
        public async Task ProcessJsonImportAsync(JsonImportData importData)
        {
            // Extract parameters from the data wrapper
            int userId = importData.UserId;
            string jsonContent = importData.JsonContent;
            bool skipExisting = importData.SkipExisting;
            string connectionId = importData.ConnectionId;
            
            _logger.LogInformation("Starting JSON import from serialized data: UserId={UserId}", userId);
                
            // Delegate to the actual implementation
            await ImportJsonDataAsync(userId, jsonContent, skipExisting, connectionId);
        }
        
        // This method will be called by Hangfire in the background
        private async Task ImportJsonDataAsync(int userId, string jsonContent, bool skipExisting, string connectionId)
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
            
            _logger.LogInformation("Starting background JSON import operation for user {UserId}, job {JobId}, connectionId {ConnectionId}", 
                userId, jobId, connectionId);
            
            try
            {
                // Create a new service scope to get required services
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dataPortabilityService = scope.ServiceProvider.GetRequiredService<WorkoutDataPortabilityService>();
                    
                    // Initialize progress reporting with retries for connection issues
                    var initProgress = new JobProgress { 
                        Status = "Initializing import...", 
                        PercentComplete = 0,
                        Details = "Preparing to process JSON data"
                    };
                    
                    // Send progress updates to both the specific client and the job group
                    await SendJobUpdateWithRetriesAsync(connectionId, jobId, initProgress);
                    
                    // Set up the progress callback with more detail
                    dataPortabilityService.OnProgressUpdate = async (jobProgress) => {
                        // Add the job ID to the progress data for logging
                        _logger.LogDebug("Job {JobId} progress: {Status} {PercentComplete}%", 
                            jobId, jobProgress.Status, jobProgress.PercentComplete);
                            
                        await SendJobUpdateWithRetriesAsync(connectionId, jobId, jobProgress);
                    };
                    
                    // Execute the actual import operation
                    var startTime = DateTime.UtcNow;
                    var result = await dataPortabilityService.ImportUserDataAsync(userId, jsonContent, skipExisting);
                    var duration = DateTime.UtcNow - startTime;
                    
                    _logger.LogInformation("Import completed in {Duration} with result: success={Success}, message={Message}", 
                        duration, result.success, result.message);
                    
                    // Report completion or failure
                    if (result.success)
                    {
                        var finalProgress = new JobProgress { 
                            Status = "Completed", 
                            PercentComplete = 100,
                            Details = $"Successfully imported data in {duration.TotalSeconds:F1} seconds",
                            ProcessedItems = result.importedItems?.Count ?? 0,
                            TotalItems = result.importedItems?.Count ?? 0
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
                            TotalItems = 0
                        };
                        await SendJobUpdateWithRetriesAsync(connectionId, jobId, errorProgress);
                        
                        // Re-throw to mark the job as failed in Hangfire
                        throw new Exception($"Import failed: {result.message}");
                    }
                }
                
                _logger.LogInformation("Successfully completed JSON import for user {UserId}, job {JobId}", userId, jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during background JSON import for user {UserId}, job {JobId}", userId, jobId);
                
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

        // Queue JSON import from file as a background job
        public string QueueJsonImportFromFile(int userId, string filePath, bool skipExisting, string connectionId, bool deleteFileWhenDone = false)
        {
            _logger.LogInformation($"Queuing JSON import from file job for user {userId}, file path: {filePath}");
            
            try
            {
                // Validate inputs
                if (userId <= 0)
                {
                    _logger.LogError("Invalid userId: {UserId}", userId);
                    throw new ArgumentException("Invalid user ID", nameof(userId));
                }
                
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    _logger.LogError("File not found or invalid file path: {FilePath}", filePath);
                    throw new ArgumentException("File not found or invalid file path", nameof(filePath));
                }
                
                // Create a serialization-friendly parameter object
                var importData = new JsonFileImportData
                {
                    UserId = userId,
                    FilePath = filePath,
                    SkipExisting = skipExisting,
                    ConnectionId = connectionId,
                    DeleteFileWhenDone = deleteFileWhenDone
                };
                
                // Queue the job
                string jobId;
                try
                {
                    // Enqueue the job with explicit type arguments to avoid serialization issues
                    jobId = BackgroundJob.Enqueue<BackgroundJobService>(x => 
                        x.ProcessJsonFileImportAsync(importData));
                    
                    _logger.LogInformation("Successfully queued JSON file import job with ID {JobId}", jobId);
                    
                    // Add the connection to a SignalR group for this job
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var importHub = scope.ServiceProvider.GetRequiredService<IHubContext<ImportProgressHub>>();
                            importHub.Groups.AddToGroupAsync(connectionId, $"job_{jobId}").GetAwaiter().GetResult();
                            _logger.LogDebug("Added connection {ConnectionId} to group job_{JobId}", connectionId, jobId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error when queuing JSON file import job");
                    throw;
                }
                
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue JSON file import job for user {UserId}", userId);
                throw; // Re-throw to show error to user
            }
        }

        // Method to process JSON import from file as a background job
        public async Task ProcessJsonFileImportAsync(JsonFileImportData importData)
        {
            // Extract parameters from the data wrapper
            int userId = importData.UserId;
            string filePath = importData.FilePath;
            bool skipExisting = importData.SkipExisting;
            string connectionId = importData.ConnectionId;
            bool deleteFileWhenDone = importData.DeleteFileWhenDone;
            
            _logger.LogInformation("Starting JSON import from file: UserId={UserId}, FilePath={FilePath}", 
                userId, filePath);
                
            // Delegate to the actual implementation
            await ImportJsonDataFromFileAsync(userId, filePath, skipExisting, connectionId, deleteFileWhenDone);
        }
        
        // This method will be called by Hangfire in the background to process JSON from a file
        private async Task ImportJsonDataFromFileAsync(int userId, string filePath, bool skipExisting, string connectionId, bool deleteFileWhenDone)
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
            
            _logger.LogInformation("Starting background JSON file import for user {UserId}, job {JobId}, file {FilePath}", 
                userId, jobId, filePath);
            
            try
            {
                // Verify the file exists
                if (!System.IO.File.Exists(filePath))
                {
                    throw new FileNotFoundException($"The import file was not found at path: {filePath}");
                }
                
                // Get the file size for progress reporting
                var fileInfo = new FileInfo(filePath);
                long fileSize = fileInfo.Length;
                
                // Read the file content
                string jsonContent;
                using (var reader = new StreamReader(filePath))
                {
                    jsonContent = await reader.ReadToEndAsync();
                }
                
                _logger.LogInformation("Successfully read JSON file of size {FileSizeBytes} bytes", fileSize);
                
                // Create a new service scope to get required services
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dataPortabilityService = scope.ServiceProvider.GetRequiredService<WorkoutDataPortabilityService>();
                    
                    // Initialize progress reporting with retries for connection issues
                    var initProgress = new JobProgress { 
                        Status = "Initializing import...", 
                        PercentComplete = 0,
                        Details = $"Preparing to process JSON data ({fileSize / (1024.0 * 1024.0):F2}MB)"
                    };
                    
                    // Send progress updates to both the specific client and the job group
                    await SendJobUpdateWithRetriesAsync(connectionId, jobId, initProgress);
                    
                    // Set up the progress callback with more detail
                    dataPortabilityService.OnProgressUpdate = async (jobProgress) => {
                        // Add the job ID to the progress data for logging
                        _logger.LogDebug("Job {JobId} progress: {Status} {PercentComplete}%", 
                            jobId, jobProgress.Status, jobProgress.PercentComplete);
                            
                        await SendJobUpdateWithRetriesAsync(connectionId, jobId, jobProgress);
                    };
                    
                    // Execute the actual import operation
                    var startTime = DateTime.UtcNow;
                    var result = await dataPortabilityService.ImportUserDataAsync(userId, jsonContent, skipExisting);
                    var duration = DateTime.UtcNow - startTime;
                    
                    _logger.LogInformation("File import completed in {Duration} with result: success={Success}, message={Message}", 
                        duration, result.success, result.message);
                    
                    // Report completion or failure
                    if (result.success)
                    {
                        var finalProgress = new JobProgress { 
                            Status = "Completed", 
                            PercentComplete = 100,
                            Details = $"Successfully imported data in {duration.TotalSeconds:F1} seconds",
                            ProcessedItems = result.importedItems?.Count ?? 0,
                            TotalItems = result.importedItems?.Count ?? 0
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
                            TotalItems = 0
                        };
                        await SendJobUpdateWithRetriesAsync(connectionId, jobId, errorProgress);
                        
                        // Re-throw to mark the job as failed in Hangfire
                        throw new Exception($"Import failed: {result.message}");
                    }
                }
                
                _logger.LogInformation("Successfully completed JSON file import for user {UserId}, job {JobId}", userId, jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during background JSON file import for user {UserId}, job {JobId}", userId, jobId);
                
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
            finally
            {
                // Clean up the temporary file if requested
                if (deleteFileWhenDone && !string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                {
                    try 
                    {
                        System.IO.File.Delete(filePath);
                        _logger.LogInformation("Deleted temporary import file: {FilePath}", filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error deleting temporary import file: {FilePath}", filePath);
                    }
                }
            }
        }

        // Queue TrainAI CSV import as a background job
        public string QueueTrainAICsvImport(int userId, string filePath, string connectionId, bool deleteFileWhenDone = true)
        {
            _logger.LogInformation($"Queuing TrainAI CSV import job for user {userId}, file path: {filePath}");
            
            try
            {
                // Validate inputs
                if (userId <= 0)
                {
                    _logger.LogError("Invalid userId: {UserId}", userId);
                    throw new ArgumentException("Invalid user ID", nameof(userId));
                }
                
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                {
                    _logger.LogError("File not found or invalid file path: {FilePath}", filePath);
                    throw new ArgumentException("File not found or invalid file path", nameof(filePath));
                }
                
                // Create a serialization-friendly parameter object
                var importData = new TrainAICsvImportData
                {
                    UserId = userId,
                    FilePath = filePath,
                    ConnectionId = connectionId,
                    DeleteFileWhenDone = deleteFileWhenDone
                };
                
                // Queue the job
                string jobId;
                try
                {
                    // Enqueue the job with explicit type arguments to avoid serialization issues
                    jobId = BackgroundJob.Enqueue<BackgroundJobService>(x => 
                        x.ProcessTrainAICsvFileImportAsync(importData));
                    
                    _logger.LogInformation("Successfully queued TrainAI CSV file import job with ID {JobId}", jobId);
                    
                    // Add the connection to a SignalR group for this job
                    if (!string.IsNullOrEmpty(connectionId))
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var importHub = scope.ServiceProvider.GetRequiredService<IHubContext<ImportProgressHub>>();
                            importHub.Groups.AddToGroupAsync(connectionId, $"job_{jobId}").GetAwaiter().GetResult();
                            _logger.LogDebug("Added connection {ConnectionId} to group job_{JobId}", connectionId, jobId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error when queuing TrainAI CSV file import job");
                    throw;
                }
                
                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue TrainAI CSV file import job for user {UserId}", userId);
                throw; // Re-throw to show error to user
            }
        }

        // Method to process TrainAI CSV import from file as a background job
        public async Task ProcessTrainAICsvFileImportAsync(TrainAICsvImportData importData)
        {
            // Extract parameters from the data wrapper
            int userId = importData.UserId;
            string filePath = importData.FilePath;
            string connectionId = importData.ConnectionId;
            bool deleteFileWhenDone = importData.DeleteFileWhenDone;
            
            _logger.LogInformation("Starting TrainAI CSV import from file: UserId={UserId}, FilePath={FilePath}", 
                userId, filePath);
                
            // Delegate to the actual implementation
            await ImportTrainAICsvFromFileAsync(userId, filePath, connectionId, deleteFileWhenDone);
        }
        
        // This method will be called by Hangfire in the background to process TrainAI CSV from a file
        private async Task ImportTrainAICsvFromFileAsync(int userId, string filePath, string connectionId, bool deleteFileWhenDone)
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
            
            _logger.LogInformation("Starting background TrainAI CSV file import for user {UserId}, job {JobId}, file {FilePath}", 
                userId, jobId, filePath);
            
            try
            {
                // Verify the file exists
                if (!System.IO.File.Exists(filePath))
                {
                    throw new FileNotFoundException($"The import file was not found at path: {filePath}");
                }
                
                // Get the file size for progress reporting
                var fileInfo = new FileInfo(filePath);
                long fileSize = fileInfo.Length;
                
                // Create a new service scope to get required services
                using (var scope = _serviceProvider.CreateScope())
                {
                    var trainAIImportService = scope.ServiceProvider.GetRequiredService<TrainAIImportService>();
                    
                    // Initialize progress reporting with retries for connection issues
                    var initProgress = new JobProgress { 
                        Status = "Processing CSV file...", 
                        PercentComplete = 5,
                        Details = $"Parsing CSV data ({fileSize / (1024.0 * 1024.0):F2}MB)"
                    };
                    
                    // Send progress updates
                    await SendJobUpdateWithRetriesAsync(connectionId, jobId, initProgress);
                    
                    // Parse the CSV file directly from disk to avoid loading it all into memory
                    List<TrainAIWorkout> workouts;
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        workouts = await trainAIImportService.ParseTrainAICsvAsync(fileStream);
                    }
                    
                    if (workouts.Count == 0)
                    {
                        _logger.LogWarning("No workouts found in file: {FilePath}", filePath);
                        var noWorkoutsProgress = new JobProgress { 
                            Status = "Error", 
                            PercentComplete = 0,
                            ErrorMessage = "No workouts found in the CSV file."
                        };
                        await SendJobUpdateWithRetriesAsync(connectionId, jobId, noWorkoutsProgress);
                        throw new InvalidOperationException("No workouts found in the CSV file.");
                    }
                    
                    // Update progress after parsing
                    var parseCompleteProgress = new JobProgress { 
                        Status = "File parsed successfully", 
                        PercentComplete = 10,
                        Details = $"Found {workouts.Count} workouts with {workouts.Sum(w => w.Sets.Count)} sets",
                        TotalItems = workouts.Count,
                        ProcessedItems = 0
                    };
                    await SendJobUpdateWithRetriesAsync(connectionId, jobId, parseCompleteProgress);
                    
                    // Set up the progress callback for the import process
                    trainAIImportService.OnProgressUpdateV2 = async (jobProgress) => {
                        // Scale the progress to start from 10% (after parsing)
                        if (jobProgress.PercentComplete < 100)
                        {
                            jobProgress.PercentComplete = 10 + (int)(jobProgress.PercentComplete * 0.9);
                        }
                        
                        // Add the job ID to the progress data for logging
                        _logger.LogDebug("Job {JobId} progress: {Status} {PercentComplete}%", 
                            jobId, jobProgress.Status, jobProgress.PercentComplete);
                            
                        await SendJobUpdateWithRetriesAsync(connectionId, jobId, jobProgress);
                    };
                    
                    // Execute the actual import operation
                    var startTime = DateTime.UtcNow;
                    var result = await trainAIImportService.ImportTrainAIWorkoutsAsync(userId, workouts);
                    var duration = DateTime.UtcNow - startTime;
                    
                    _logger.LogInformation("CSV import completed in {Duration} with result: success={Success}, message={Message}", 
                        duration, result.success, result.message);
                    
                    // Report completion or failure
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
                        throw new Exception($"Import failed: {result.message}");
                    }
                }
                
                _logger.LogInformation("Successfully completed TrainAI CSV file import for user {UserId}, job {JobId}", userId, jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during background TrainAI CSV file import for user {UserId}, job {JobId}", userId, jobId);
                
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
            finally
            {
                // Clean up the temporary file if requested
                if (deleteFileWhenDone && !string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                {
                    try 
                    {
                        System.IO.File.Delete(filePath);
                        _logger.LogInformation("Deleted temporary import file: {FilePath}", filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error deleting temporary import file: {FilePath}", filePath);
                    }
                }
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

    // Data wrapper for JSON import
    public class JsonImportData
    {
        public int UserId { get; set; }
        public string JsonContent { get; set; }
        public bool SkipExisting { get; set; }
        public string ConnectionId { get; set; }
    }

    // Data wrapper for JSON file import
    public class JsonFileImportData
    {
        public int UserId { get; set; }
        public string FilePath { get; set; }
        public bool SkipExisting { get; set; }
        public string ConnectionId { get; set; }
        public bool DeleteFileWhenDone { get; set; }
    }

    // Data wrapper for TrainAI CSV import
    public class TrainAICsvImportData
    {
        public int UserId { get; set; }
        public string FilePath { get; set; }
        public string ConnectionId { get; set; }
        public bool DeleteFileWhenDone { get; set; }
    }
}