using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkoutTrackerWeb.Services;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;
using Hangfire;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using WorkoutTrackerWeb.Hubs;
using System.Text.Json;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Pages.DataPortability
{
    [Authorize]
    [ValidateAntiForgeryToken]
    public class ImportModel : PageModel
    {
        private readonly WorkoutDataPortabilityService _portabilityService;
        private readonly UserService _userService;
        private readonly BackgroundJobService _backgroundJobService;
        private readonly ILogger<ImportModel> _logger;
        private readonly IHubContext<ImportProgressHub> _hubContext;

        public ImportModel(
            WorkoutDataPortabilityService portabilityService,
            UserService userService,
            BackgroundJobService backgroundJobService,
            IHubContext<ImportProgressHub> hubContext,
            ILogger<ImportModel> logger)
        {
            _portabilityService = portabilityService;
            _userService = userService;
            _backgroundJobService = backgroundJobService;
            _hubContext = hubContext;
            _logger = logger;
        }

        [BindProperty]
        public IFormFile ImportFile { get; set; }

        [BindProperty]
        public bool SkipExisting { get; set; } = true;

        public string Message { get; set; }
        public bool Success { get; set; }
        public List<string> ImportedItems { get; set; } = new();
        
        // Properties to track background job status
        public string JobId { get; set; }
        public string JobState { get; set; }
        public string ErrorMessage { get; set; }
        public long FileSizeBytes { get; set; }

        public async Task<IActionResult> OnGetAsync(string jobId = null)
        {
            // If a job ID is provided, check its status
            if (!string.IsNullOrEmpty(jobId))
            {
                JobId = jobId;
                var (isInProgress, state) = await CheckJobStatusAsync(jobId);
                JobState = state;
                
                // If the job completed successfully, we're done
                if (state == "Succeeded")
                {
                    Success = true;
                    Message = "Import completed successfully.";
                }
                // If the job failed, show an error message
                else if (state == "Failed")
                {
                    Success = false;
                    Message = "Import failed.";
                    ErrorMessage = await GetJobErrorMessageAsync(jobId);
                }
                // If the job is still in progress, we're waiting
                else if (isInProgress)
                {
                    Message = $"Import in progress (Status: {state})";
                }
            }
            
            return Page();
        }

        // Update the OnPostAsync method to use shared storage instead of local file system
        public async Task<IActionResult> OnPostAsync()
        {
            if (ImportFile == null || ImportFile.Length == 0)
            {
                Message = "Please select a file to import";
                Success = false;
                return Page();
            }

            try
            {
                var userId = await _userService.GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    throw new InvalidOperationException("User not found");
                }

                // Store file size for UI display
                FileSizeBytes = ImportFile.Length;
                _logger.LogInformation("Processing import file of size {FileSizeBytes} bytes", FileSizeBytes);
                
                // Use connection ID for realtime progress updates
                string connectionId = HttpContext.Connection.Id;
                string jobId;
                
                // Save to temp file first, this will be used only temporarily
                string tempFilePath = Path.GetTempFileName();
                
                try 
                {
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await ImportFile.CopyToAsync(fileStream);
                    }
                    
                    // Queue background job to process the saved file through shared storage
                    // This will move the file from local temp storage to Redis/shared storage
                    jobId = _backgroundJobService.QueueJsonImportFromFile(
                        userId.Value, tempFilePath, SkipExisting, connectionId, true); // true = delete temp file when done
                    
                    _logger.LogInformation("Queued JSON import from file job {JobId} for user {UserId}, temp file: {TempFile}", 
                        jobId, userId.Value, tempFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving temp file for import");
                    
                    // Clean up if possible
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        try { System.IO.File.Delete(tempFilePath); } catch { /* ignore */ }
                    }
                    
                    throw;
                }
                
                // Add the connection to the job-specific group
                await _hubContext.Groups.AddToGroupAsync(connectionId, $"job_{jobId}");
                
                // Show immediate feedback
                await SendInitialProgressUpdateAsync(jobId, ImportFile.Length);
                
                // Store the job ID to allow checking status
                JobId = jobId;
                JobState = "Enqueued";
                Success = true;
                Message = "Import job has been started. You can watch the progress on this page or leave and check back later.";
                
                // Return the page with job tracking info
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting import job");
                Message = $"Import failed: {ex.Message}";
                Success = false;
                return Page();
            }
        }

        // Send an initial progress update to show the UI is working
        private async Task SendInitialProgressUpdateAsync(string jobId, long fileSize)
        {
            try 
            {
                // Send an initial progress update to show the job is starting
                var initialProgress = new Models.JobProgress
                {
                    JobId = jobId,
                    Status = JobStatus.Starting,
                    PercentComplete = 0,
                    TotalItems = 1,
                    ProcessedItems = 0,
                    CurrentStage = $"Preparing to import JSON data ({(fileSize / 1024.0 / 1024.0):F2}MB)"
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

        // Update the OnPostLargeFileAsync method to use shared storage instead of local file system
        public async Task<IActionResult> OnPostLargeFileAsync()
        {
            if (ImportFile == null || ImportFile.Length == 0)
            {
                return new JsonResult(new { success = false, message = "Please select a file to import" });
            }

            try
            {
                var userId = await _userService.GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return new JsonResult(new { success = false, message = "User not found" });
                }

                // Store file size for UI display
                FileSizeBytes = ImportFile.Length;
                _logger.LogInformation("Processing large file upload via AJAX: {FileSizeBytes} bytes", FileSizeBytes);
                
                // For AJAX uploads, save to temp file first, will be moved to shared storage
                string tempFilePath = Path.GetTempFileName();
                string jobId;
                string connectionId = HttpContext.Connection.Id;
                
                try 
                {
                    using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                    {
                        await ImportFile.CopyToAsync(fileStream);
                    }
                    
                    // Queue background job to process the saved file through shared storage
                    jobId = _backgroundJobService.QueueJsonImportFromFile(
                        userId.Value, tempFilePath, SkipExisting, connectionId, true); // true = delete temp file when done
                    
                    _logger.LogInformation("AJAX upload: Queued JSON import job {JobId} for user {UserId}, temp file: {TempFile}", 
                        jobId, userId.Value, tempFilePath);
                    
                    // Add the connection to the job-specific group
                    await _hubContext.Groups.AddToGroupAsync(connectionId, $"job_{jobId}");
                    
                    // Send initial progress update
                    await SendInitialProgressUpdateAsync(jobId, ImportFile.Length);
                    
                    // Return success with job ID
                    return new JsonResult(new { 
                        success = true, 
                        jobId = jobId, 
                        message = "File uploaded successfully and processing has started.",
                        fileSizeMB = Math.Round(ImportFile.Length / (1024.0 * 1024.0), 2)
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing large file upload");
                    
                    // Clean up if possible
                    if (System.IO.File.Exists(tempFilePath))
                    {
                        try { System.IO.File.Delete(tempFilePath); } catch { /* ignore */ }
                    }
                    
                    return new JsonResult(new { success = false, message = $"Upload failed: {ex.Message}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during large file upload");
                return new JsonResult(new { success = false, message = $"Upload failed: {ex.Message}" });
            }
        }
        
        // Check the status of a background job
        private Task<(bool IsInProgress, string State)> CheckJobStatusAsync(string jobId)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                    return Task.FromResult<(bool, string)>((false, "Unknown"));
                
                // First check with our service if it's in progress
                bool isInProgress = _backgroundJobService.IsJobInProgress(jobId);
                
                // If it's in progress, return immediately
                if (isInProgress)
                    return Task.FromResult<(bool, string)>((true, "Processing"));
                
                // If not in progress, check more detailed status using Hangfire's API
                string state = "Unknown";
                
                var storage = JobStorage.Current;
                var monitoringApi = storage.GetMonitoringApi();
                
                // Check failed jobs
                var failedJobs = monitoringApi.FailedJobs(0, 1000);
                if (failedJobs.Any(j => j.Key == jobId))
                    return Task.FromResult<(bool, string)>((false, "Failed"));
                
                // Check succeeded jobs
                var succeededJobs = monitoringApi.SucceededJobs(0, 1000);
                if (succeededJobs.Any(j => j.Key == jobId))
                    return Task.FromResult<(bool, string)>((false, "Succeeded"));
                
                // Check scheduled jobs (jobs that will run in the future)
                var scheduledJobs = monitoringApi.ScheduledJobs(0, 1000);
                if (scheduledJobs.Any(j => j.Key == jobId))
                    return Task.FromResult<(bool, string)>((true, "Scheduled"));
                
                // Check enqueued jobs
                var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, 1000);
                if (enqueuedJobs.Any(j => j.Key == jobId))
                    return Task.FromResult<(bool, string)>((true, "Enqueued"));
                
                return Task.FromResult<(bool, string)>((false, state));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking job status for {JobId}", jobId);
                return Task.FromResult<(bool, string)>((false, "Error"));
            }
        }
        
        // Get error message for a failed job
        private Task<string> GetJobErrorMessageAsync(string jobId)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                    return Task.FromResult("Unknown error");
                
                var storage = JobStorage.Current;
                var monitoringApi = storage.GetMonitoringApi();
                var failedJobs = monitoringApi.FailedJobs(0, 1000);
                
                var job = failedJobs.FirstOrDefault(j => j.Key == jobId);
                if (job.Value != null)
                {
                    return Task.FromResult(job.Value.ExceptionMessage);
                }
                
                return Task.FromResult("No error details available");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job error message for {JobId}", jobId);
                return Task.FromResult("Error retrieving job details");
            }
        }
    }
}