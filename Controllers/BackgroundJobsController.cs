using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.AspNetCore.Authorization;
using WorkoutTrackerWeb.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace WorkoutTrackerWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BackgroundJobsController : Controller
    {
        private readonly BackgroundJobService _backgroundJobService;
        private readonly ILogger<BackgroundJobsController> _logger;

        public BackgroundJobsController(BackgroundJobService backgroundJobService, ILogger<BackgroundJobsController> logger)
        {
            _backgroundJobService = backgroundJobService;
            _logger = logger;
        }

        // GET: /BackgroundJobs
        public IActionResult Index()
        {
            // Get statistics from Hangfire
            IMonitoringApi monitoringApi = JobStorage.Current.GetMonitoringApi();
            var stats = monitoringApi.GetStatistics();
            
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
            var sortedJobs = recentJobs
                .Take(10)
                .ToDictionary(x => x.Key, x => x.Value);

            ViewBag.Statistics = stats;
            ViewBag.RecentJobs = sortedJobs;
            
            return View();
        }

        // GET: /BackgroundJobs/Details/jobId
        public IActionResult Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Job ID is required");
            }

            try
            {
                var monitor = JobStorage.Current.GetMonitoringApi();
                var job = monitor.JobDetails(id);
                
                if (job == null)
                {
                    return NotFound($"Job with ID {id} not found");
                }
                
                return View(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job details for job ID: {JobId}", id);
                return StatusCode(500, "An error occurred while retrieving job details");
            }
        }

        // Add this method for direct access to the Hangfire dashboard
        public IActionResult HangfireDashboard()
        {
            // Redirect to the Hangfire dashboard
            return Redirect("/hangfire");
        }
        
        // GET: /BackgroundJobs/ServerStatus
        public IActionResult ServerStatus()
        {
            try
            {
                // Get monitoring API
                var monitor = JobStorage.Current.GetMonitoringApi();
                
                // Get server list
                var servers = monitor.Servers();
                
                var serverDetailsList = new List<ServerStatusViewModel>();
                foreach (var server in servers)
                {
                    var serverQueues = server.Queues.Select(q => new QueueViewModel
                    {
                        Name = q,
                        Length = GetQueueLength(monitor, q),
                        IsPaused = false // We don't have direct access to this info
                    }).ToList();
                    
                    // Parse server name to extract pod/node info if available
                    var serverNameParts = server.Name.Split(':');
                    string serverType = serverNameParts.Length > 0 ? serverNameParts[0] : "unknown";
                    string nodeName = serverNameParts.Length > 1 ? serverNameParts[1] : "unknown";
                    string podName = serverNameParts.Length > 2 ? serverNameParts[2] : "unknown";
                    
                    bool isActive = server.Heartbeat.HasValue && 
                        (DateTime.UtcNow - server.Heartbeat.Value).TotalMinutes < 1;
                    
                    var serverDetails = new ServerStatusViewModel
                    {
                        Id = server.Name,
                        ServerType = serverType,
                        NodeName = nodeName,
                        PodName = podName,
                        WorkersCount = server.WorkersCount,
                        Queues = serverQueues,
                        StartedAt = server.StartedAt,
                        Heartbeat = server.Heartbeat,
                        IsActive = isActive
                    };
                    
                    serverDetailsList.Add(serverDetails);
                }
                
                ViewBag.Servers = serverDetailsList;
                ViewBag.ServerCount = servers.Count;
                
                // Get queue stats
                var queueList = monitor.Queues()
                    .Select(q => new QueueViewModel
                    {
                        Name = q.Name,
                        Length = (int)q.Length,  // Cast to int
                        IsPaused = q.Name.StartsWith("!") // Simple check for paused queues
                    })
                    .ToList();
                ViewBag.Queues = queueList;
                
                // Get overall stats
                var stats = monitor.GetStatistics();
                ViewBag.JobStats = stats;
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Hangfire server status");
                return StatusCode(500, "An error occurred while retrieving server status");
            }
        }
        
        // Helper to get queue length for given queue name
        private int GetQueueLength(IMonitoringApi api, string queueName)
        {
            try
            {
                var queues = api.Queues();
                var queue = queues.FirstOrDefault(q => q.Name == queueName);
                return queue != null ? (int)queue.Length : 0;  // Cast to int
            }
            catch
            {
                return 0;
            }
        }
        
        // POST: /BackgroundJobs/EnqueueJob
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EnqueueJob(string jobType, string jobData)
        {
            if (string.IsNullOrEmpty(jobType))
            {
                return BadRequest("Job type is required");
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
                                // Just queue a sample import job for demo purposes
                                jobId = BackgroundJob.Enqueue(() => 
                                    _backgroundJobService.ProcessImportAsync("Sample Import"));
                            }
                            else
                            {
                                // Parse the job data
                                var importData = JsonConvert.DeserializeObject<TrainAIImportData>(jobData);
                                if (importData == null)
                                {
                                    return BadRequest("Invalid import data format");
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
                            return BadRequest("Invalid import data format");
                        }
                        break;
                        
                    case "DeleteAllData":
                        try {
                            if (string.IsNullOrEmpty(jobData))
                            {
                                return BadRequest("User ID is required for delete operation");
                            }
                            
                            var deleteData = JsonConvert.DeserializeObject<DeleteAllDataParams>(jobData);
                            if (deleteData == null || string.IsNullOrEmpty(deleteData.UserId))
                            {
                                return BadRequest("Invalid delete data format or missing user ID");
                            }
                            
                            // Use the actual implementation
                            jobId = _backgroundJobService.QueueDeleteAllWorkoutData(
                                deleteData.UserId,
                                deleteData.ConnectionId ?? "");
                        }
                        catch (JsonException jex) {
                            _logger.LogError(jex, "Failed to parse Delete All Data parameters");
                            return BadRequest("Invalid parameters for delete operation");
                        }
                        break;
                        
                    case "GenerateReport":
                        // Queue a report generation job
                        jobId = BackgroundJob.Enqueue(() => 
                            _backgroundJobService.ProcessReportAsync("Sample Report"));
                        break;
                        
                    case "DataCleanup":
                        // Queue a data cleanup job
                        jobId = BackgroundJob.Enqueue(() => 
                            _backgroundJobService.ProcessDataCleanupAsync(DateTime.Now.AddMonths(-3)));
                        break;
                        
                    default:
                        return BadRequest($"Unsupported job type: {jobType}");
                }
                
                TempData["SuccessMessage"] = $"Job queued successfully with ID: {jobId}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enqueueing job of type: {JobType}", jobType);
                TempData["ErrorMessage"] = $"Error enqueueing job: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // GET: /BackgroundJobs/JobHistory
        public IActionResult JobHistory()
        {
            var monitor = JobStorage.Current.GetMonitoringApi();
            
            ViewBag.SucceededJobs = monitor.SucceededJobs(0, 100);
            ViewBag.FailedJobs = monitor.FailedJobs(0, 100);
            
            return View();
        }
        
        // Add a Retry action for failed jobs
        public IActionResult Retry(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Job ID is required for retry";
                return RedirectToAction("JobHistory");
            }

            try
            {
                BackgroundJob.Requeue(id);
                TempData["SuccessMessage"] = $"Job {id} has been requeued successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requeuing job: {JobId}", id);
                TempData["ErrorMessage"] = $"Error requeuing job: {ex.Message}";
            }

            return RedirectToAction("JobHistory");
        }
        
        // Add a Delete action for jobs
        public IActionResult Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["ErrorMessage"] = "Job ID is required for deletion";
                return RedirectToAction("JobHistory");
            }

            try
            {
                BackgroundJob.Delete(id);
                TempData["SuccessMessage"] = $"Job {id} has been deleted successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job: {JobId}", id);
                TempData["ErrorMessage"] = $"Error deleting job: {ex.Message}";
            }

            return RedirectToAction("JobHistory");
        }
        
        // Check Hangfire Status
        public IActionResult CheckHangfireStatus()
        {
            bool isConfigured = _backgroundJobService.ValidateHangfireConfiguration();
            
            return Json(new { 
                isConfigured = isConfigured, 
                message = isConfigured ? "Hangfire is properly configured" : "Hangfire is not properly configured" 
            });
        }
    }

    // View Models for Server Status
    public class ServerStatusViewModel
    {
        public string Id { get; set; }
        public string ServerType { get; set; }
        public string NodeName { get; set; }
        public string PodName { get; set; }
        public int WorkersCount { get; set; }
        public List<QueueViewModel> Queues { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? Heartbeat { get; set; }
        public bool IsActive { get; set; }
    }

    public class QueueViewModel
    {
        public string Name { get; set; }
        public int Length { get; set; }
        public bool IsPaused { get; set; }
    }
    
    // Parameter model for TrainAI import
    public class TrainAIImportData
    {
        public int UserId { get; set; }
        public List<TrainAIWorkout> Workouts { get; set; }
        public string ConnectionId { get; set; }
    }
    
    // Parameter model for Delete All Data
    public class DeleteAllDataParams
    {
        public string UserId { get; set; }
        public string ConnectionId { get; set; }
    }
}