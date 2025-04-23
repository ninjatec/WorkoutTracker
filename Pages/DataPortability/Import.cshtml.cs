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

namespace WorkoutTrackerWeb.Pages.DataPortability
{
    [Authorize]
    public class ImportModel : PageModel
    {
        private readonly WorkoutDataPortabilityService _portabilityService;
        private readonly UserService _userService;
        private readonly BackgroundJobService _backgroundJobService;
        private readonly ILogger<ImportModel> _logger;

        public ImportModel(
            WorkoutDataPortabilityService portabilityService,
            UserService userService,
            BackgroundJobService backgroundJobService,
            ILogger<ImportModel> logger)
        {
            _portabilityService = portabilityService;
            _userService = userService;
            _backgroundJobService = backgroundJobService;
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

                using var reader = new StreamReader(ImportFile.OpenReadStream());
                var jsonData = await reader.ReadToEndAsync();

                // Queue the import as a background job
                string connectionId = HttpContext.Connection.Id;
                string jobId = _backgroundJobService.QueueJsonImport(userId.Value, jsonData, SkipExisting, connectionId);
                
                _logger.LogInformation("Queued JSON import background job {JobId} for user {UserId}", jobId, userId.Value);
                
                // Store the job ID to allow checking status
                JobId = jobId;
                JobState = "Enqueued";
                Message = "Import job has been started. Please wait while your data is being processed.";
                
                // Redirect to the same page with the job ID to track progress
                return RedirectToPage(new { jobId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting import job");
                Message = $"Import failed: {ex.Message}";
                Success = false;
                return Page();
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