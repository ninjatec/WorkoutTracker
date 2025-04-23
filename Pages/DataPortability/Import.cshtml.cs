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

namespace WorkoutTrackerWeb.Pages.DataPortability
{
    [Authorize]
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
                
                // For files > 5MB, use a different approach to avoid timeouts
                bool isLargeFile = ImportFile.Length > 5 * 1024 * 1024; // 5MB threshold
                string connectionId = HttpContext.Connection.Id;
                string jobId;
                
                if (isLargeFile)
                {
                    _logger.LogInformation("Large file detected ({SizeMB:F2}MB), using streamlined import approach", 
                        ImportFile.Length / (1024.0 * 1024.0));
                    
                    // For large files, save to temp file and process asynchronously
                    // to avoid Cloudflare 504 timeouts
                    string tempFilePath = Path.GetTempFileName();
                    
                    try 
                    {
                        using (var fileStream = new FileStream(tempFilePath, FileMode.Create))
                        {
                            await ImportFile.CopyToAsync(fileStream);
                        }
                        
                        // Queue background job to process the saved file
                        jobId = _backgroundJobService.QueueJsonImportFromFile(
                            userId.Value, tempFilePath, SkipExisting, connectionId, true); // true = delete file when done
                        
                        _logger.LogInformation("Queued JSON import from file job {JobId} for user {UserId}, temp file: {TempFile}", 
                            jobId, userId.Value, tempFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving temp file for large import");
                        
                        // Clean up if possible
                        if (System.IO.File.Exists(tempFilePath))
                        {
                            try { System.IO.File.Delete(tempFilePath); } catch { /* ignore */ }
                        }
                        
                        throw;
                    }
                }
                else
                {
                    // For smaller files, use the original approach
                    using var reader = new StreamReader(ImportFile.OpenReadStream());
                    var jsonData = await reader.ReadToEndAsync();

                    // Queue the import as a background job
                    jobId = _backgroundJobService.QueueJsonImport(userId.Value, jsonData, SkipExisting, connectionId);
                    
                    _logger.LogInformation("Queued standard JSON import job {JobId} for user {UserId}", jobId, userId.Value);
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
                var initialProgress = new JobProgress
                {
                    Status = "Starting import...",
                    PercentComplete = 0,
                    TotalItems = 1,
                    ProcessedItems = 0,
                    Details = $"Preparing to import JSON data ({(fileSize / 1024.0 / 1024.0):F2}MB)"
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
        
        // Check the status of a background job
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
                
                // Check scheduled jobs (jobs that will run in the future)
                var scheduledJobs = monitoringApi.ScheduledJobs(0, 1000);
                if (scheduledJobs.Any(j => j.Key == jobId))
                    return (true, "Scheduled");
                
                // Check enqueued jobs
                var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, 1000);
                if (enqueuedJobs.Any(j => j.Key == jobId))
                    return (true, "Enqueued");
                
                return (false, state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking job status for {JobId}", jobId);
                return (false, "Error");
            }
        }
        
        // Get error message for a failed job
        private async Task<string> GetJobErrorMessageAsync(string jobId)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                    return "Unknown error";
                
                var storage = JobStorage.Current;
                var monitoringApi = storage.GetMonitoringApi();
                var failedJobs = monitoringApi.FailedJobs(0, 1000);
                
                var job = failedJobs.FirstOrDefault(j => j.Key == jobId);
                if (job.Value != null)
                {
                    return job.Value.ExceptionMessage;
                }
                
                return "No error details available";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job error message for {JobId}", jobId);
                return "Error retrieving job details";
            }
        }
    }
}