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
    public class DetailsModel : PageModel
    {
        private readonly ILogger<DetailsModel> _logger;

        [TempData]
        public string SuccessMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public string JobId { get; set; }
        public JobDetailsDto Job { get; set; }
        public string CurrentState { get; set; }
        public IList<StateHistoryDto> History { get; set; }
        public object JobResult { get; set; }

        public DetailsModel(ILogger<DetailsModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                ErrorMessage = "Job ID is required";
                return RedirectToPage("./Index");
            }

            try
            {
                JobId = id;
                var monitor = JobStorage.Current.GetMonitoringApi();
                var jobDetails = monitor.JobDetails(id);
                
                if (jobDetails == null)
                {
                    ErrorMessage = $"Job with ID {id} not found";
                    return RedirectToPage("./Index");
                }
                
                Job = jobDetails;
                History = jobDetails.History;
                CurrentState = jobDetails.History.Count > 0 ? jobDetails.History[0].StateName : "Unknown";
                
                try
                {
                    if (jobDetails.Properties.ContainsKey("Result"))
                    {
                        JobResult = jobDetails.Properties["Result"];
                    }
                }
                catch
                {
                    // Ignore result processing errors
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job details for job ID: {JobId}", id);
                ErrorMessage = "An error occurred while retrieving job details";
                return RedirectToPage("./Index");
            }
        }

        public IActionResult OnGetRetry(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                ErrorMessage = "Job ID is required for retry";
                return RedirectToPage("./JobHistory");
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

            return RedirectToPage("./JobHistory");
        }
        
        public IActionResult OnGetDelete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                ErrorMessage = "Job ID is required for deletion";
                return RedirectToPage("./JobHistory");
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

            return RedirectToPage("./JobHistory");
        }
    }
}