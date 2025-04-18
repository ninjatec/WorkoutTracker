using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Hubs
{
    public class ImportProgressHub : Hub
    {
        private readonly ILogger<ImportProgressHub> _logger;

        public ImportProgressHub(ILogger<ImportProgressHub> logger)
        {
            _logger = logger;
        }

        // Traditional method for updating import progress
        public async Task UpdateProgress(int workoutProgress, int totalWorkouts, string currentWorkout, 
            int processedReps, int totalReps, string currentExercise)
        {
            _logger.LogDebug("UpdateProgress called: workout {WorkoutProgress}/{TotalWorkouts}, reps {ProcessedReps}/{TotalReps}", 
                workoutProgress, totalWorkouts, processedReps, totalReps);
                
            await Clients.All.SendAsync("UpdateProgress", new 
            {
                workoutProgress,
                totalWorkouts,
                currentWorkout,
                processedReps,
                totalReps,
                currentExercise,
                percentComplete = totalReps > 0 ? (processedReps * 100) / totalReps : 
                    (totalWorkouts > 0 ? (workoutProgress * 100) / totalWorkouts : 0)
            });
        }
        
        // New method for generic job progress updates
        public async Task SendProgress(JobProgress progress)
        {
            _logger.LogDebug("SendProgress called: {Status} {PercentComplete}%", 
                progress.Status, progress.PercentComplete);
                
            await Clients.Caller.SendAsync("ReceiveProgress", progress);
        }
        
        // Method to store connection ID for background job notifications
        public async Task RegisterForJobUpdates(string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                _logger.LogWarning("Client {ConnectionId} attempted to register for updates with empty jobId", 
                    Context.ConnectionId);
                return;
            }
            
            _logger.LogInformation("Client {ConnectionId} registered for job updates on {JobId}", 
                Context.ConnectionId, jobId);
                
            // Clear any existing groups for this connection
            await LeaveAllJobGroups();
            
            // Add to the specific job group
            string groupName = $"job_{jobId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            // Acknowledge registration success
            await Clients.Caller.SendAsync("JobRegistrationStatus", new { success = true, jobId, message = "Successfully registered for job updates" });
        }
        
        // Method to leave all job groups (called during reconnection to clean up)
        private async Task LeaveAllJobGroups()
        {
            try
            {
                // We can't enumerate all groups a connection is in, so we rely on the client to tell us which job ID
                // it was previously registered for. This is a simplified cleanup mechanism.
                var groups = await Task.FromResult(new[] { "pending_import" });
                
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
        
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            
            // Notify the client that the connection is established
            await Clients.Caller.SendAsync("ConnectionStatus", new { isConnected = true, connectionId = Context.ConnectionId });
            
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
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}