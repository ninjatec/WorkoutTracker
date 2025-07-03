using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Hangfire.Storage;
using Hangfire;
using System.Linq;
using Newtonsoft.Json;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobStatusController : ControllerBase
    {
        private readonly ILogger<JobStatusController> _logger;
        private readonly BackgroundJobService _backgroundJobService;

        public JobStatusController(
            ILogger<JobStatusController> logger,
            BackgroundJobService backgroundJobService)
        {
            _logger = logger;
            _backgroundJobService = backgroundJobService;
        }

        [HttpGet("{jobId}")]
        public async Task<IActionResult> GetJobStatus(string jobId)
        {
            if (string.IsNullOrEmpty(jobId))
            {
                return BadRequest("Job ID is required");
            }

            try
            {
                var storage = JobStorage.Current;
                var monitoringApi = storage.GetMonitoringApi();
                string state = "Unknown";
                string errorMessage = null;
                JobProgress progress = null;

                // Check for processing jobs
                var processingJobs = monitoringApi.ProcessingJobs(0, 1000);
                if (processingJobs.Any(j => j.Key == jobId))
                {
                    state = "Processing";
                    
                    // Try to get progress for processing jobs
                    progress = await GetProgressForJobAsync(jobId);
                }
                
                // Check for succeeded jobs
                var succeededJobs = monitoringApi.SucceededJobs(0, 1000);
                if (succeededJobs.Any(j => j.Key == jobId))
                {
                    state = "Succeeded";
                }
                
                // Check for failed jobs
                var failedJobs = monitoringApi.FailedJobs(0, 1000);
                var failedJob = failedJobs.FirstOrDefault(j => j.Key == jobId);
                if (failedJob.Key == jobId)
                {
                    state = "Failed";
                    errorMessage = failedJob.Value?.ExceptionMessage;
                }
                
                // Check for scheduled jobs
                var scheduledJobs = monitoringApi.ScheduledJobs(0, 1000);
                if (scheduledJobs.Any(j => j.Key == jobId))
                {
                    state = "Scheduled";
                }
                
                // Check for enqueued jobs
                var enqueuedJobs = monitoringApi.EnqueuedJobs("default", 0, 1000);
                if (enqueuedJobs.Any(j => j.Key == jobId))
                {
                    state = "Enqueued";
                }

                // Also check if the job is in progress with our service
                if (state == "Unknown" && _backgroundJobService.IsJobInProgress(jobId))
                {
                    state = "Processing";
                    
                    // Try to get progress
                    progress = await GetProgressForJobAsync(jobId);
                }

                return Ok(new { jobId, state, errorMessage, progress });
            }
            catch (Exception ex)
            {
                var sanitizedJobId = jobId.Replace(Environment.NewLine, "").Replace("\n", "").Replace("\r", "");
                _logger.LogError(ex, "Error getting job status for job {JobId}", sanitizedJobId);
                return StatusCode(500, new { message = "An error occurred while checking job status", error = ex.Message });
            }
        }
        
        private Task<JobProgress> GetProgressForJobAsync(string jobId)
        {
            try
            {
                // Try to get progress from job parameters
                using (var connection = JobStorage.Current.GetConnection())
                {
                    var jobData = connection.GetJobData(jobId);
                    if (jobData?.Job != null)
                    {
                        // Check if job has parameters that might contain progress data
                        var parameters = jobData.Job.Args;
                        if (parameters != null && parameters.Count > 0)
                        {
                            // Look for JobProgress objects in the parameters
                            foreach (var param in parameters)
                            {
                                if (param is JobProgress progressParam)
                                {
                                    return Task.FromResult(progressParam);
                                }
                            }
                        }
                        
                        // Also check for progress data stored in job state
                        var storageConnection = JobStorage.Current.GetConnection();
                        var hash = storageConnection.GetAllEntriesFromHash($"job:{jobId}:progress");
                        
                        if (hash != null && hash.ContainsKey("progress"))
                        {
                            var progressJson = hash["progress"];
                            if (!string.IsNullOrEmpty(progressJson))
                            {
                                try
                                {
                                    return Task.FromResult(JsonConvert.DeserializeObject<JobProgress>(progressJson));
                                }
                                catch
                                {
                                    // Ignore deserialization errors
                                }
                            }
                        }
                        
                        // If no progress found but job is still in progress, provide a fallback progress
                        // to prevent UI from getting stuck at upload completion
                        var state = jobData.State;
                        if (state == "Processing" || state == "Enqueued")
                        {
                            // For jobs that are processing but have no explicit progress data,
                            // return a generic progress object to keep the UI updated
                            return Task.FromResult(new JobProgress
                            {
                                Status = $"Processing job in {state} state...",
                                PercentComplete = state == "Processing" ? 30 : 10, // Show some progress to indicate activity
                                Details = "Please wait while your file is being processed"
                            });
                        }
                    }
                }
                
                // No progress found
                return Task.FromResult<JobProgress>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving progress data for job {JobId}", jobId);
                return Task.FromResult<JobProgress>(null);
            }
        }
    }
}