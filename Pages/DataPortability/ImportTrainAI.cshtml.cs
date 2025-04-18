using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Hubs;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.DataPortability
{
    [Authorize]
    public class ImportTrainAIModel : PageModel
    {
        private readonly TrainAIImportService _importService;
        private readonly BackgroundJobService _backgroundJobService;
        private readonly IHubContext<ImportProgressHub> _hubContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<ImportTrainAIModel> _logger;

        public ImportTrainAIModel(
            TrainAIImportService importService,
            BackgroundJobService backgroundJobService,
            IHubContext<ImportProgressHub> hubContext,
            UserManager<IdentityUser> userManager,
            WorkoutTrackerWebContext context,
            ILogger<ImportTrainAIModel> logger)
        {
            _importService = importService;
            _backgroundJobService = backgroundJobService;
            _hubContext = hubContext;
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public IFormFile ImportFile { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<string> ImportedItems { get; set; } = new();
        public string JobId { get; set; }
        public bool IsJobInProgress { get; set; }
        public string JobState { get; set; }

        public async Task<IActionResult> OnGetAsync(string jobId = null)
        {
            _logger.LogInformation("ImportTrainAI OnGetAsync called with jobId={JobId}", jobId);
            
            // If we have a job ID from a previous submission or from the query string,
            // check if it's still in progress and set up the UI to show progress
            if (!string.IsNullOrEmpty(jobId))
            {
                JobId = jobId;
                (IsJobInProgress, JobState) = await CheckJobStatusAsync(jobId);
                
                _logger.LogInformation("Job status check: IsJobInProgress={IsJobInProgress}, JobState={JobState}", 
                    IsJobInProgress, JobState);
                
                if (IsJobInProgress)
                {
                    Success = true;
                    Message = "Import job is in progress. You can watch the progress on this page or leave and check back later.";
                }
                else if (JobState == "Succeeded")
                {
                    Success = true;
                    Message = "Import job completed successfully.";
                }
                else if (JobState == "Failed")
                {
                    Success = false;
                    Message = "Import job failed. Check the error message below for details.";
                    
                    // Try to get the exception details
                    var exceptionDetails = await GetJobExceptionDetailsAsync(jobId);
                    if (!string.IsNullOrEmpty(exceptionDetails))
                    {
                        Message += $" Error: {exceptionDetails}";
                    }
                }
                else
                {
                    // Job is in an unknown state
                    Message = $"Import job status: {JobState}. Check the job details for more information.";
                }
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("ImportTrainAI OnPostAsync called");
            
            if (ImportFile == null || ImportFile.Length == 0)
            {
                Message = "Please select a file to import";
                return Page();
            }

            try
            {
                // Parse the CSV file
                using var stream = ImportFile.OpenReadStream();
                var workouts = await _importService.ParseTrainAICsvAsync(stream);

                if (workouts.Count == 0)
                {
                    Message = "No workouts found in the file";
                    return Page();
                }

                // Get current user
                var identityUser = await _userManager.GetUserAsync(User);
                if (identityUser == null)
                {
                    Message = "User not found";
                    return Page();
                }

                // Get or create the corresponding User record
                var user = await _context.User.FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id);
                if (user == null)
                {
                    user = new Models.User
                    {
                        IdentityUserId = identityUser.Id,
                        Name = identityUser.UserName
                    };
                    _context.User.Add(user);
                    await _context.SaveChangesAsync();
                }

                // Get SignalR connection ID for real-time progress updates
                string connectionId = HttpContext.Connection.Id;
                _logger.LogInformation("Submitting import with connectionId={ConnectionId}", connectionId);
                
                // Set up the hub connection before queuing the job
                await _hubContext.Groups.AddToGroupAsync(connectionId, "pending_import");
                
                // Queue the import as a background job
                JobId = _backgroundJobService.QueueTrainAIImport(user.UserId, workouts, connectionId);
                _logger.LogInformation("Job queued with ID={JobId}", JobId);
                
                // Add the connection to the job-specific group
                await _hubContext.Groups.AddToGroupAsync(connectionId, $"job_{JobId}");
                
                // Update the job state
                (IsJobInProgress, JobState) = await CheckJobStatusAsync(JobId);
                
                // Show immediate feedback
                await SendInitialProgressUpdateAsync(JobId, workouts.Count);
                
                Success = true;
                Message = $"Import job started with {workouts.Count} workouts. Job ID: {JobId}. Status: {JobState}. " +
                          $"The process will run in the background. You can watch the progress on this page or leave and check back later.";
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during TrainAI import");
                Success = false;
                Message = $"Import failed: {ex.Message}";
                return Page();
            }
        }
        
        private async Task<(bool IsInProgress, string State)> CheckJobStatusAsync(string jobId)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                    return (false, "Unknown");
                
                // First check with our service if it's in progress
                bool isInProgress = _backgroundJobService.IsJobInProgress(jobId);
                
                // If it's in progress, return immediately
                if (isInProgress)
                    return (true, "Processing");
                
                // If not in progress, check more detailed status using Hangfire's API
                string state = "Unknown";
                
                try
                {
                    var storage = JobStorage.Current;
                    var monitoringApi = storage.GetMonitoringApi();
                    
                    // Check failed jobs
                    var failedJobs = monitoringApi.FailedJobs(0, 1000);
                    if (failedJobs.Any(j => j.Key == jobId))
                        return (false, "Failed");
                    
                    // Check succeeded jobs
                    var succeededJobs = monitoringApi.SucceededJobs(0, 1000);
                    if (succeededJobs.Any(j => j.Key == jobId))
                        return (false, "Succeeded");
                    
                    // Check scheduled jobs
                    var scheduledJobs = monitoringApi.ScheduledJobs(0, 1000);
                    if (scheduledJobs.Any(j => j.Key == jobId))
                        return (true, "Scheduled");
                    
                    // Check enqueued jobs
                    var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, 1000);
                    if (enqueuedJobs.Any(j => j.Key == jobId))
                        return (true, "Enqueued");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking job state for JobId={JobId}", jobId);
                    // Fall back to the isInProgress check result
                    return (isInProgress, isInProgress ? "Processing" : "Unknown");
                }
                
                return (false, state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking job status for JobId={JobId}", jobId);
                return (false, "Error");
            }
        }
        
        private async Task<string> GetJobExceptionDetailsAsync(string jobId)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                    return null;
                
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                var failedJobs = monitoringApi.FailedJobs(0, 1000);
                var failedJob = failedJobs.FirstOrDefault(j => j.Key == jobId);
                
                if (failedJob.Key == jobId && failedJob.Value != null)
                {
                    return !string.IsNullOrEmpty(failedJob.Value.ExceptionMessage) 
                        ? failedJob.Value.ExceptionMessage 
                        : "An error occurred during job execution. See logs for details.";
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exception details for JobId={JobId}", jobId);
                return "Error retrieving job details";
            }
        }
        
        private async Task SendInitialProgressUpdateAsync(string jobId, int workoutCount)
        {
            try 
            {
                // Send an initial progress update to show the job is starting
                var initialProgress = new JobProgress
                {
                    Status = "Starting import...",
                    PercentComplete = 0,
                    TotalItems = workoutCount,
                    ProcessedItems = 0,
                    Details = "Preparing to import workouts"
                };
                
                // Send to both the individual connection and the job group
                string connectionId = HttpContext.Connection.Id;
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveProgress", initialProgress);
                    _logger.LogInformation("Sent initial progress update to connectionId={ConnectionId}", connectionId);
                }
                
                await _hubContext.Clients.Group($"job_{jobId}").SendAsync("ReceiveProgress", initialProgress);
                _logger.LogInformation("Sent initial progress update to job group={JobGroup}", $"job_{jobId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending initial progress update");
                // Ignore errors - this is just a nice-to-have initial feedback
            }
        }
    }
}