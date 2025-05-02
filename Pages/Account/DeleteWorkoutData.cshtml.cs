using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using WorkoutTrackerWeb.Hubs;
using WorkoutTrackerWeb.Services;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using System.Linq;
using System;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Pages.Account
{
    [Authorize]
    public class DeleteWorkoutDataModel : PageModel
    {
        private readonly UserService _userService;
        private readonly BackgroundJobService _backgroundJobService;
        private readonly IHubContext<ImportProgressHub> _hubContext;
        private readonly ILogger<DeleteWorkoutDataModel> _logger;

        public DeleteWorkoutDataModel(
            UserService userService,
            BackgroundJobService backgroundJobService,
            IHubContext<ImportProgressHub> hubContext,
            ILogger<DeleteWorkoutDataModel> logger)
        {
            _userService = userService;
            _backgroundJobService = backgroundJobService;
            _hubContext = hubContext;
            _logger = logger;
        }

        [BindProperty]
        [Required(ErrorMessage = "You must confirm that you understand the consequences of this action.")]
        public bool ConfirmDelete { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public string Message { get; set; }
        public bool Success { get; set; }
        public string JobId { get; set; }
        public string JobState { get; set; }

        public IActionResult OnGet()
        {
            // Check if there's a job ID in TempData (from previous post)
            if (TempData.ContainsKey("JobId"))
            {
                JobId = TempData["JobId"]?.ToString();
                TempData.Keep("JobId"); // Keep it for future requests
                
                if (!string.IsNullOrEmpty(JobId))
                {
                    try
                    {
                        // Get job state directly from Hangfire connection
                        var jobData = JobStorage.Current.GetConnection().GetJobData(JobId);
                        JobState = jobData?.State ?? "Unknown";
                        
                        _logger.LogDebug("Retrieved job state {JobState} for job {JobId}", JobState, JobId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error retrieving job state for job {JobId}", JobId);
                        JobState = "Error";
                    }
                }
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (!ConfirmDelete)
            {
                Message = "You must confirm the deletion by checking the checkbox.";
                Success = false;
                return Page();
            }

            try
            {
                var identityUserId = _userService.GetCurrentIdentityUserId();
                if (identityUserId == null)
                {
                    return Challenge();
                }
                
                // Get the SignalR connection ID
                string connectionId = HttpContext.Connection.Id;
                
                // Queue the background job and get its ID
                JobId = _backgroundJobService.QueueDeleteAllWorkoutData(identityUserId, connectionId);
                
                // Store job ID in TempData so it persists across requests
                TempData["JobId"] = JobId;
                
                // Get job state directly
                try
                {
                    var jobData = JobStorage.Current.GetConnection().GetJobData(JobId);
                    JobState = jobData?.State ?? "Unknown";
                }
                catch
                {
                    JobState = "Unknown";
                }

                Message = "Your workout data deletion has been queued. This process will run in the background. You can view the progress on this page.";
                Success = true;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling workout data deletion");
                Message = "An error occurred while scheduling your workout data deletion. Please try again.";
                Success = false;
                return Page();
            }
        }
        
        // Helper method to get the current state of a Hangfire job
        private string GetJobState(string jobId)
        {
            // Quick validation
            if (string.IsNullOrEmpty(jobId))
                return "Unknown";
                
            try
            {
                // Simplified approach that doesn't need direct comparisons
                if (!int.TryParse(jobId, out _))
                {
                    _logger.LogWarning("Invalid job ID format: {JobId}", jobId);
                    return "Invalid";
                }
                
                // Instead of directly comparing with job IDs, use string.Equals
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                
                // Check various job states using a simpler approach
                try 
                {
                    var job = JobStorage.Current.GetConnection().GetJobData(jobId);
                    if (job != null)
                    {
                        switch (job.State)
                        {
                            case "Processing": return "Processing";
                            case "Succeeded": return "Succeeded";
                            case "Failed": return "Failed";
                            case "Scheduled": return "Scheduled";
                            case "Enqueued": return "Enqueued";
                            default: return job.State ?? "Unknown";
                        }
                    }
                }
                catch 
                {
                    // Fall back to generic "Unknown" if we can't retrieve the job
                }
                
                return "Unknown";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking job state for job {JobId}", jobId);
                return "Error";
            }
        }
    }
}