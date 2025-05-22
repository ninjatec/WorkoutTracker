using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Api.JobStatus
{
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly BackgroundJobService _backgroundJobService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            BackgroundJobService backgroundJobService,
            ILogger<IndexModel> logger)
        {
            _backgroundJobService = backgroundJobService;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            return new JsonResult(new { message = "Use /{id} to check job status" });
        }

        public IActionResult OnGetJobStatus(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Job ID is required");
            }

            try
            {
                // Check if the job is in progress using our service
                bool isInProgress = _backgroundJobService.IsJobInProgress(id);
                
                string state = "Unknown";
                DateTime? createdAt = null;
                string errorMessage = null;
                
                try
                {
                    // Try to get job data from Hangfire
                    var storage = JobStorage.Current;
                    var monitoringApi = storage.GetMonitoringApi();
                    
                    // First check processing jobs
                    var processingJobs = monitoringApi.ProcessingJobs(0, 1000);
                    if (processingJobs.Any(j => j.Key == id))
                    {
                        var jobData = processingJobs.First(j => j.Key == id).Value;
                        state = "Processing";
                        createdAt = jobData?.StartedAt;
                    }
                    
                    // Check failed jobs
                    var failedJobs = monitoringApi.FailedJobs(0, 1000);
                    if (failedJobs.Any(j => j.Key == id))
                    {
                        var jobData = failedJobs.First(j => j.Key == id).Value;
                        state = "Failed";
                        createdAt = jobData?.FailedAt;
                        errorMessage = jobData?.ExceptionMessage;
                        if (string.IsNullOrEmpty(errorMessage) && jobData?.ExceptionDetails != null)
                        {
                            errorMessage = "Error occurred during job execution. See logs for details.";
                        }
                    }
                    
                    // Check succeeded jobs
                    var succeededJobs = monitoringApi.SucceededJobs(0, 1000);
                    if (succeededJobs.Any(j => j.Key == id))
                    {
                        var jobData = succeededJobs.First(j => j.Key == id).Value;
                        state = "Succeeded";
                        createdAt = jobData?.SucceededAt;
                    }
                    
                    // Check scheduled jobs
                    var scheduledJobs = monitoringApi.ScheduledJobs(0, 1000);
                    if (scheduledJobs.Any(j => j.Key == id))
                    {
                        var jobData = scheduledJobs.First(j => j.Key == id).Value;
                        state = "Scheduled";
                        createdAt = jobData?.ScheduledAt;
                    }
                    
                    // Check enqueued jobs
                    var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, 1000);
                    if (enqueuedJobs.Any(j => j.Key == id))
                    {
                        var jobData = enqueuedJobs.First(j => j.Key == id).Value;
                        state = "Enqueued";
                        createdAt = jobData?.EnqueuedAt;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting detailed job info for {JobId}", id);
                    // Fall back to the isInProgress flag we got from the service
                    state = isInProgress ? "Processing" : "Unknown";
                }
                
                // Prepare response data
                var resultData = new
                {
                    jobId = id,
                    isInProgress = isInProgress,
                    state = state,
                    createdAt = createdAt,
                    errorMessage = errorMessage
                };
                
                return new JsonResult(resultData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status for job {JobId}", id);
                return StatusCode(500, new { error = "Error getting job status", message = ex.Message });
            }
        }

        private IActionResult BadRequest(string message)
        {
            Response.StatusCode = 400;
            return new JsonResult(new { error = message });
        }

        private IActionResult StatusCode(int statusCode, object value)
        {
            Response.StatusCode = statusCode;
            return new JsonResult(value);
        }
    }
}