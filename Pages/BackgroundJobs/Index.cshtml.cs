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
using WorkoutTrackerWeb.Services;
using Newtonsoft.Json;

namespace WorkoutTrackerWeb.Pages.BackgroundJobs
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly BackgroundJobService _backgroundJobService;
        private readonly ILogger<IndexModel> _logger;

        [TempData]
        public string SuccessMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public Dictionary<string, dynamic> RecentJobs { get; set; }
        public Hangfire.Storage.Monitoring.StatisticsDto Statistics { get; set; }
        public int? ServerCount { get; set; }
        public int? QueueCount { get; set; }

        public IndexModel(BackgroundJobService backgroundJobService, ILogger<IndexModel> logger)
        {
            _backgroundJobService = backgroundJobService;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            try
            {
                // Get statistics from Hangfire
                IMonitoringApi monitoringApi = JobStorage.Current.GetMonitoringApi();
                Statistics = monitoringApi.GetStatistics();
                
                // Get the most recent jobs
                var succeededJobs = monitoringApi.SucceededJobs(0, 5);
                var failedJobs = monitoringApi.FailedJobs(0, 5);
                var processingJobs = monitoringApi.ProcessingJobs(0, 5);
                
                // Combine different types of jobs into a single dictionary
                var recentJobs = new Dictionary<string, dynamic>();
                
                foreach (var job in succeededJobs)
                    recentJobs[job.Key] = job.Value;
                    
                foreach (var job in failedJobs)
                    recentJobs[job.Key] = job.Value;
                    
                foreach (var job in processingJobs)
                    recentJobs[job.Key] = job.Value;
                
                // Sort by creation date and take 10
                RecentJobs = recentJobs
                    .Take(10)
                    .ToDictionary(x => x.Key, x => x.Value);

                // Get server and queue counts
                var servers = monitoringApi.Servers();
                ServerCount = servers.Count;
                
                var queues = monitoringApi.Queues();
                QueueCount = queues.Count;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading background jobs dashboard");
                ErrorMessage = "Error loading dashboard: " + ex.Message;
                RecentJobs = new Dictionary<string, dynamic>();
                return Page();
            }
        }

        public IActionResult OnPostEnqueueJob(string jobType, string jobData)
        {
            if (string.IsNullOrEmpty(jobType))
            {
                ErrorMessage = "Job type is required";
                return RedirectToPage();
            }

            try
            {
                string jobId;
                
                switch (jobType)
                {
                    case "ImportTrainAI":
                        try {
                            if (string.IsNullOrEmpty(jobData))
                            {
                                // Return a message that this demo function has been removed
                                ErrorMessage = "Sample import functionality has been removed.";
                                return RedirectToPage();
                            }
                            else
                            {
                                // Parse the job data
                                var importData = JsonConvert.DeserializeObject<TrainAIImportData>(jobData);
                                if (importData == null)
                                {
                                    ErrorMessage = "Invalid import data format";
                                    return RedirectToPage();
                                }
                                
                                // Use the actual implementation
                                jobId = _backgroundJobService.QueueTrainAIImport(
                                    importData.UserId, 
                                    importData.Workouts, 
                                    importData.ConnectionId);
                            }
                        }
                        catch (JsonException jex) {
                            _logger.LogError(jex, "Failed to parse TrainAI import data");
                            ErrorMessage = "Invalid import data format";
                            return RedirectToPage();
                        }
                        break;
                        
                    case "DeleteAllData":
                        try {
                            if (string.IsNullOrEmpty(jobData))
                            {
                                ErrorMessage = "User ID is required for delete operation";
                                return RedirectToPage();
                            }
                            
                            var deleteData = JsonConvert.DeserializeObject<DeleteAllDataParams>(jobData);
                            if (deleteData == null || string.IsNullOrEmpty(deleteData.UserId))
                            {
                                ErrorMessage = "Invalid delete data format or missing user ID";
                                return RedirectToPage();
                            }
                            
                            // Use the actual implementation
                            jobId = _backgroundJobService.QueueDeleteAllWorkoutData(
                                deleteData.UserId,
                                deleteData.ConnectionId ?? "");
                        }
                        catch (JsonException jex) {
                            _logger.LogError(jex, "Failed to parse Delete All Data parameters");
                            ErrorMessage = "Invalid parameters for delete operation";
                            return RedirectToPage();
                        }
                        break;
                        
                    case "GenerateReport":
                        // This demo function has been removed
                        ErrorMessage = "Report generation functionality has been removed.";
                        return RedirectToPage();
                        
                    case "DataCleanup":
                        // Queue a data cleanup job
                        jobId = BackgroundJob.Enqueue(() => 
                            _backgroundJobService.ProcessDataCleanupAsync(DateTime.Now.AddMonths(-3)));
                        break;
                        
                    default:
                        ErrorMessage = $"Unsupported job type: {jobType}";
                        return RedirectToPage();
                }
                
                SuccessMessage = $"Job queued successfully with ID: {jobId}";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueueing job of type: {JobType}", jobType);
                ErrorMessage = $"Error enqueueing job: {ex.Message}";
                return RedirectToPage();
            }
        }

        // Hangfire Status Check for AJAX calls
        public IActionResult OnGetCheckHangfireStatus()
        {
            bool isConfigured = false;
            
            try {
                var storage = JobStorage.Current;
                var monitoringApi = storage.GetMonitoringApi();
                var servers = monitoringApi.Servers();
                
                isConfigured = servers.Count > 0;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error checking Hangfire status");
                isConfigured = false;
            }
            
            return new JsonResult(new { 
                isConfigured = isConfigured, 
                message = isConfigured ? "Hangfire is properly configured" : "Hangfire is not properly configured" 
            });
        }
    }
}