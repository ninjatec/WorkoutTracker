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
using System.IO;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Hubs;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.DataPortability
{
    [Authorize]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100MB max file size
    [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)] // Also set form limit
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
        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string jobId = null)
        {
            _logger.LogInformation("ImportTrainAI OnGetAsync called with jobId={JobId}", jobId);
            
            // If we have a job ID from a previous submission or from the query string,
            // check if it's still in progress and set up the UI to show progress
            if (!string.IsNullOrEmpty(jobId))
            {
                JobId = jobId;
                (IsJobInProgress, JobState, ErrorMessage) = await CheckJobStatusAsync(jobId);
                
                _logger.LogInformation("Job status check: IsJobInProgress={IsJobInProgress}, JobState={JobState}, HasError={HasError}", 
                    IsJobInProgress, JobState, !string.IsNullOrEmpty(ErrorMessage));
                
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
                    Message = "Import job failed.";
                    
                    // If we already have an error message from the job status check, use it
                    if (!string.IsNullOrEmpty(ErrorMessage))
                    {
                        Message += $" Error: {ErrorMessage}";
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
                (IsJobInProgress, JobState, ErrorMessage) = await CheckJobStatusAsync(JobId);
                
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
                ErrorMessage = ex.ToString();
                return Page();
            }
        }
        
        private async Task<(bool IsInProgress, string State, string ErrorMessage)> CheckJobStatusAsync(string jobId)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                    return (false, "Unknown", null);
                
                // First check with our service if it's in progress
                bool isInProgress = _backgroundJobService.IsJobInProgress(jobId);
                
                // If it's in progress, return immediately
                if (isInProgress)
                    return (true, "Processing", null);
                
                // If not in progress, check more detailed status using Hangfire's API
                string state = "Unknown";
                string errorMessage = null;
                
                try
                {
                    var monitoringApi = JobStorage.Current.GetMonitoringApi();
                    
                    // Check failed jobs
                    var failedJobs = monitoringApi.FailedJobs(0, 1000);
                    if (failedJobs.Any(j => j.Key == jobId))
                    {
                        var failedJob = failedJobs.FirstOrDefault(j => j.Key == jobId);
                        errorMessage = !string.IsNullOrEmpty(failedJob.Value?.ExceptionMessage) 
                            ? failedJob.Value.ExceptionMessage 
                            : "An error occurred during job execution. See logs for details.";
                        return (false, "Failed", errorMessage);
                    }
                    
                    // Check succeeded jobs
                    var succeededJobs = monitoringApi.SucceededJobs(0, 1000);
                    if (succeededJobs.Any(j => j.Key == jobId))
                        return (false, "Succeeded", null);
                    
                    // Check scheduled jobs
                    var scheduledJobs = monitoringApi.ScheduledJobs(0, 1000);
                    if (scheduledJobs.Any(j => j.Key == jobId))
                        return (true, "Scheduled", null);
                    
                    // Check enqueued jobs
                    var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, 1000);
                    if (enqueuedJobs.Any(j => j.Key == jobId))
                        return (true, "Enqueued", null);
                    
                    // Check processing jobs
                    var processingJobs = monitoringApi.ProcessingJobs(0, 1000);
                    if (processingJobs.Any(j => j.Key == jobId))
                        return (true, "Processing", null);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking job state for JobId={JobId}", jobId);
                    // Fall back to the isInProgress check result
                    return (isInProgress, isInProgress ? "Processing" : "Unknown", null);
                }
                
                return (false, state, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking job status for JobId={JobId}", jobId);
                return (false, "Error", ex.Message);
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