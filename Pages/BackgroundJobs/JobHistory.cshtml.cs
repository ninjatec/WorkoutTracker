using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WorkoutTrackerWeb.Pages.BackgroundJobs
{
    [Authorize(Roles = "Admin")]
    public class JobHistoryModel : PageModel
    {
        private readonly ILogger<JobHistoryModel> _logger;

        [TempData]
        public string SuccessMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public JobList<SucceededJobDto> SucceededJobs { get; set; }
        public JobList<FailedJobDto> FailedJobs { get; set; }

        public JobHistoryModel(ILogger<JobHistoryModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            try
            {
                var monitor = JobStorage.Current.GetMonitoringApi();
                
                SucceededJobs = monitor.SucceededJobs(0, 100);
                FailedJobs = monitor.FailedJobs(0, 100);
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job history");
                ErrorMessage = "An error occurred while retrieving job history";
                
                // Initialize with empty collections instead of calling constructor directly
                SucceededJobs = new JobList<SucceededJobDto>(Enumerable.Empty<KeyValuePair<string, SucceededJobDto>>());
                FailedJobs = new JobList<FailedJobDto>(Enumerable.Empty<KeyValuePair<string, FailedJobDto>>());
                
                return Page();
            }
        }

        public IActionResult OnGetRetry(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                ErrorMessage = "Job ID is required for retry";
                return RedirectToPage();
            }

            try
            {
                BackgroundJob.Requeue(id);
                SuccessMessage = $"Job {id} has been requeued successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requeuing job: {JobId}", id);
                ErrorMessage = $"Error requeuing job: {ex.Message}";
            }

            return RedirectToPage();
        }
        
        public IActionResult OnGetDelete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                ErrorMessage = "Job ID is required for deletion";
                return RedirectToPage();
            }

            try
            {
                BackgroundJob.Delete(id);
                SuccessMessage = $"Job {id} has been deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job: {JobId}", id);
                ErrorMessage = $"Error deleting job: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}