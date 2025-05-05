using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Hubs
{
    /// <summary>
    /// Represents connection options for SignalR clients
    /// </summary>
    public class SignalRConnectionOptions
    {
        /// <summary>
        /// Indicates if the client is on a mobile device
        /// </summary>
        public bool IsMobileClient { get; set; }
        
        /// <summary>
        /// If true, the server will reduce heartbeat frequency to save battery on mobile
        /// </summary>
        public bool EnableReducedHeartbeat { get; set; }
        
        /// <summary>
        /// Indicates if the client is on a metered connection
        /// </summary>
        public bool IsMeteredConnection { get; set; }
        
        /// <summary>
        /// Special options for minimal data usage on metered connections
        /// </summary>
        public bool EnableDataSavingMode { get; set; }
    }

    public class ImportProgressHub : Hub
    {
        private readonly ILogger<ImportProgressHub> _logger;
        private readonly SignalRMessageBatchService _batchService;
        private static readonly ConcurrentDictionary<string, string> _connectionJobMapping = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, SignalRConnectionOptions> _connectionOptions = new ConcurrentDictionary<string, SignalRConnectionOptions>();

        public ImportProgressHub(
            ILogger<ImportProgressHub> logger,
            SignalRMessageBatchService batchService)
        {
            _logger = logger;
            _batchService = batchService;
        }

        // Traditional method for updating import progress
        public async Task UpdateProgress(int workoutProgress, int totalWorkouts, string currentWorkout, 
            int processedReps, int totalReps, string currentExercise)
        {
            _logger.LogDebug("UpdateProgress called: workout {WorkoutProgress}/{TotalWorkouts}, reps {ProcessedReps}/{TotalReps}", 
                workoutProgress, totalWorkouts, processedReps, totalReps);

            // Create job progress data
            var progress = new Models.JobProgress
            {
                Status = JobStatus.InProgress,
                CurrentStage = $"Processing {currentWorkout}",
                PercentComplete = totalReps > 0 ? (processedReps * 100) / totalReps : 
                    (totalWorkouts > 0 ? (workoutProgress * 100) / totalWorkouts : 0),
                ProcessedItems = processedReps,
                TotalItems = totalReps,
                CustomData = new Dictionary<string, object>
                {
                    ["currentWorkout"] = currentWorkout,
                    ["currentExercise"] = currentExercise,
                    ["details"] = $"Set {processedReps}/{totalReps} of {currentExercise}"
                }
            };
            
            // We don't know the jobId here, so we'll use the connection ID as the batch key
            if (_connectionJobMapping.TryGetValue(Context.ConnectionId, out string jobId) && !string.IsNullOrEmpty(jobId))
            {
                // Add the job ID
                progress.JobId = jobId;
                _batchService.QueueProgressUpdate(jobId, progress);
            }
            else
            {
                // No job ID mapping, send immediately to the caller only
                await Clients.Caller.SendAsync("receiveProgress", progress);
            }
        }
        
        // New method for generic job progress updates
        public async Task SendProgress(Models.JobProgress progress, string jobId = null)
        {
            _logger.LogDebug("SendProgress called: {Status} {PercentComplete}% for job {JobId}", 
                progress.Status, progress.PercentComplete, jobId);
            
            // If no job ID provided but we have one mapped for this connection, use that
            if (string.IsNullOrEmpty(jobId) && _connectionJobMapping.TryGetValue(Context.ConnectionId, out string mappedJobId))
            {
                jobId = mappedJobId;
                progress.JobId = jobId;
                _logger.LogDebug("Using mapped job ID {JobId} for connection {ConnectionId}", jobId, Context.ConnectionId);
            }
            
            // Queue the update for batch sending
            if (!string.IsNullOrEmpty(jobId))
            {
                _batchService.QueueProgressUpdate(jobId, progress);
            }
            else
            {
                // No job ID, so send immediately to the caller
                await Clients.Caller.SendAsync("receiveProgress", progress);
            }
        }
        
        // Method to store connection ID for background job notifications
        public async Task RegisterForJobUpdates(string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                _logger.LogWarning("Client {ConnectionId} attempted to register for updates with empty jobId", 
                    Context.ConnectionId);
                    
                await Clients.Caller.SendAsync("JobRegistrationStatus", new { 
                    success = false, 
                    jobId = (string)null, 
                    message = "Invalid job ID" 
                });
                return;
            }
            
            _logger.LogInformation("Client {ConnectionId} registered for job updates on {JobId}", 
                Context.ConnectionId, jobId);
                
            // Clear any existing groups for this connection
            await LeaveAllJobGroups();
            
            // Add to the specific job group
            string groupName = $"job_{jobId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            // Store the mapping for reconnection
            _connectionJobMapping[Context.ConnectionId] = jobId;
            
            // Send the latest progress for this job if available
            var latestProgress = _batchService.GetLatestProgressForJob(jobId);
            if (latestProgress != null)
            {
                await Clients.Caller.SendAsync("receiveProgress", latestProgress);
            }
            
            // Acknowledge registration success
            await Clients.Caller.SendAsync("JobRegistrationStatus", new { 
                success = true, 
                jobId, 
                message = "Successfully registered for job updates" 
            });
        }
        
        // Method to leave all job groups (called during reconnection to clean up)
        private async Task LeaveAllJobGroups()
        {
            try
            {
                // Remove from any existing job group, if we have that info
                if (_connectionJobMapping.TryRemove(Context.ConnectionId, out string oldJobId))
                {
                    string oldGroupName = $"job_{oldJobId}";
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, oldGroupName);
                    _logger.LogDebug("Removed connection {ConnectionId} from job group {GroupName}", 
                        Context.ConnectionId, oldGroupName);
                }
                
                // Also leave any other groups the connection might be in
                var groups = new[] { "pending_import" };
                foreach (var group in groups)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving job groups for connection {ConnectionId}", Context.ConnectionId);
            }
        }

        // Store connection options for the client
        public async Task SetConnectionOptions(SignalRConnectionOptions options)
        {
            if (options == null)
            {
                _logger.LogWarning("Client {ConnectionId} sent null connection options", Context.ConnectionId);
                return;
            }
            
            _connectionOptions[Context.ConnectionId] = options;
            _logger.LogInformation("Client {ConnectionId} set connection options: IsMobile={IsMobile}, ReducedHeartbeat={ReducedHeartbeat}", 
                Context.ConnectionId, options.IsMobileClient, options.EnableReducedHeartbeat);
            
            // Acknowledge receipt
            await Clients.Caller.SendAsync("ConnectionOptionsStatus", new { 
                success = true, 
                message = "Connection options received" 
            });
        }
        
        // Handle connection events for resilience and monitoring
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            
            // Notify the client that the connection is established
            await Clients.Caller.SendAsync("ConnectionStatus", new { 
                isConnected = true, 
                connectionId = Context.ConnectionId 
            });
            
            await base.OnConnectedAsync();
        }
        
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            if (exception != null)
            {
                _logger.LogWarning(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
            }
            else
            {
                _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
            }
            
            // Keep the job mapping for the reconnection window
            // We'll clean it up after a timeout if they don't reconnect
            
            // Remove connection options
            _connectionOptions.TryRemove(Context.ConnectionId, out _);
            
            await base.OnDisconnectedAsync(exception);
        }
        
        // Method to handle reconnection
        public async Task ReconnectToJobGroup(string previousConnectionId, string jobId)
        {
            _logger.LogInformation("Client {NewConnectionId} requesting reconnection for job {JobId} (previous: {PreviousConnectionId})", 
                Context.ConnectionId, jobId, previousConnectionId);
                
            // Register for the job updates with the new connection ID
            await RegisterForJobUpdates(jobId);
            
            // Acknowledge reconnection success
            await Clients.Caller.SendAsync("ReconnectionStatus", new {
                success = true,
                jobId,
                message = "Successfully reconnected to job updates"
            });
        }
        
        // Method to ping the server (for keepalives from mobile clients)
        public async Task Ping()
        {
            // Just respond with the current timestamp
            await Clients.Caller.SendAsync("Pong", new {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }
    }
}