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
using System.Linq;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Hubs;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Pages.BackgroundJobs;
using WorkoutTrackerWeb.Models.Identity;

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
        private readonly UserManager<AppUser> _userManager;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<ImportTrainAIModel> _logger;
        private readonly ISharedStorageService _sharedStorageService;

        public ImportTrainAIModel(
            TrainAIImportService importService,
            BackgroundJobService backgroundJobService,
            IHubContext<ImportProgressHub> hubContext,
            UserManager<AppUser> userManager,
            WorkoutTrackerWebContext context,
            ILogger<ImportTrainAIModel> logger,
            ISharedStorageService sharedStorageService)
        {
            _importService = importService;
            _backgroundJobService = backgroundJobService;
            _hubContext = hubContext;
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _sharedStorageService = sharedStorageService;
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
        public long FileSizeBytes { get; set; }

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
                // Store file size for display
                FileSizeBytes = ImportFile.Length;
                _logger.LogInformation("Processing CSV import: {FileName}, size: {FileSize:N0} bytes", 
                    ImportFile.FileName, ImportFile.Length);
                
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
                _logger.LogInformation("Connection ID: {ConnectionId}", connectionId);
                
                // Add the connection to a group for progress updates
                await _hubContext.Groups.AddToGroupAsync(connectionId, "pending_import");

                // Process all files as background jobs to avoid any timeout issues
                return await HandleLargeFileUploadAsync(user.UserId, connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during TrainAI import: {Message}", ex.Message);
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
        
        private async Task SendInitialProgressUpdateAsync(string jobId, int workoutCount, long? fileSize = null)
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
                    Details = fileSize.HasValue ? 
                        $"Preparing to process {(fileSize.Value / (1024.0 * 1024.0)).ToString("F2")} MB file" : 
                        "Preparing to import workouts"
                };
                
                // Send to both the individual connection and the job group
                string connectionId = HttpContext.Connection.Id;
                if (!string.IsNullOrEmpty(connectionId))
                {
                    await _hubContext.Clients.Client(connectionId).SendAsync("receiveProgress", initialProgress);
                    _logger.LogInformation("Sent initial progress update to connectionId={ConnectionId}", connectionId);
                }
                
                await _hubContext.Clients.Group($"job_{jobId}").SendAsync("receiveProgress", initialProgress);
                _logger.LogInformation("Sent initial progress update to job group={JobGroup}", $"job_{jobId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending initial progress update");
                // Ignore errors - this is just a nice-to-have initial feedback
            }
        }

        private async Task<IActionResult> HandleLargeFileUploadAsync(int userId, string connectionId)
        {
            try
            {
                _logger.LogInformation("Using background processing for large file: {FileName}, {Size:N0} bytes",
                    ImportFile.FileName, ImportFile.Length);
                
                // For larger files, save to temporary storage
                string tempFilePath = Path.Combine(Path.GetTempPath(), $"trainai_import_{Guid.NewGuid()}.csv");
                
                try
                {
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await ImportFile.CopyToAsync(fileStream);
                    }
                    
                    _logger.LogInformation("Large file saved to temporary location: {TempFilePath}", tempFilePath);
                    
                    // Queue background job to process the file
                    JobId = _backgroundJobService.QueueTrainAICsvImport(
                        userId, 
                        tempFilePath, 
                        connectionId, 
                        true); // Delete the temp file when done
                    
                    _logger.LogInformation("Large file import queued with job ID: {JobId}", JobId);
                    
                    // Add the connection to the job group
                    await _hubContext.Groups.AddToGroupAsync(connectionId, $"job_{JobId}");
                    
                    // Update the job state
                    (IsJobInProgress, JobState, ErrorMessage) = await CheckJobStatusAsync(JobId);
                    
                    // Send initial progress update with file size information
                    await SendInitialProgressUpdateAsync(JobId, 0, ImportFile.Length);
                    
                    Success = true;
                    Message = $"Your file ({(ImportFile.Length / (1024.0 * 1024.0)).ToString("F2")} MB) has been uploaded and queued for processing. " +
                             $"Job ID: {JobId}. You can track the progress on this page or come back later.";
                    
                    return Page();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing large file: {Message}", ex.Message);
                    
                    // Clean up temp file if there was an error
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        try { System.IO.File.Delete(tempFilePath); } 
                        catch (Exception deleteEx) { 
                            _logger.LogWarning(deleteEx, "Error cleaning up temp file: {TempFilePath}", tempFilePath);
                        }
                    }
                    
                    throw; // Re-throw to be caught by outer handler
                }
            }
            catch (Exception ex)
            {
                // This catch block handles any exceptions from the try block above
                Success = false;
                Message = $"Import failed: {ex.Message}";
                ErrorMessage = ex.ToString();
                return Page();
            }
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostLargeFileAsync()
        {
            try
            {
                _logger.LogInformation("Processing large CSV file upload via AJAX");
                
                if (ImportFile == null || ImportFile.Length == 0)
                {
                    return new JsonResult(new { success = false, message = "Please select a file to import" });
                }

                // Store file size for the UI
                FileSizeBytes = ImportFile.Length;
                _logger.LogInformation("Processing large file upload: {FileName}, size: {FileSize:N0} bytes", 
                    ImportFile.FileName, ImportFile.Length);

                // Get current user
                var identityUser = await _userManager.GetUserAsync(User);
                if (identityUser == null)
                {
                    return new JsonResult(new { success = false, message = "User not found" });
                }

                // Get or create the corresponding User record
                var user = await _context.User.FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id);
                if (user == null)
                {
                    user = new Models.User
                    {
                        IdentityUserId = identityUser.Id,
                        Name = identityUser.UserName ?? "User" // Use username or fallback
                    };
                    _context.User.Add(user);
                    await _context.SaveChangesAsync();
                }

                // For large files, save to a temporary file first
                string tempFilePath = Path.GetTempFileName();
                _logger.LogInformation("Saving large file to temporary location: {TempFile}", tempFilePath);
                
                string connectionId = HttpContext.Connection.Id;
                string jobId;
                
                try 
                {
                    // Save the file to disk to avoid keeping it in memory
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await ImportFile.CopyToAsync(fileStream);
                    }
                    
                    // Create a background job to process the file
                    jobId = _backgroundJobService.QueueTrainAICsvImport(
                        user.UserId, 
                        tempFilePath, 
                        connectionId, 
                        true); // Delete the temp file when done
                    
                    _logger.LogInformation("Large file upload queued with job ID: {JobId}", jobId);

                    // Set the job ID for the view
                    JobId = jobId;
                    
                    // Add the connection to the job group
                    await _hubContext.Groups.AddToGroupAsync(connectionId, $"job_{jobId}");
                    
                    // Send initial progress update to show the job is starting
                    await SendInitialProgressUpdateAsync(jobId, 1, ImportFile.Length);
                    
                    return new JsonResult(new { 
                        success = true, 
                        jobId = jobId, 
                        message = "Your file has been uploaded and is being processed. You can track the progress on this page.",
                        fileSizeMb = Math.Round(ImportFile.Length / (1024.0 * 1024.0), 2)
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing large file upload: {Message}", ex.Message);
                    
                    // Clean up the temp file if there was an error
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        try { System.IO.File.Delete(tempFilePath); } 
                        catch { /* ignore cleanup errors */ }
                    }
                    
                    return new JsonResult(new { 
                        success = false, 
                        message = $"Error processing file: {ex.Message}" 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in OnPostLargeFileAsync: {Message}", ex.Message);
                return new JsonResult(new { 
                    success = false, 
                    message = "An unexpected error occurred while processing your file." 
                });
            }
        }
    }
}