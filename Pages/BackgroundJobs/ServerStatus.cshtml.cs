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
    public class ServerStatusModel : PageModel
    {
        private readonly ILogger<ServerStatusModel> _logger;

        [TempData]
        public string SuccessMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public List<ServerStatusViewModel> Servers { get; set; }
        public int ServerCount { get; set; }
        public List<QueueViewModel> Queues { get; set; }
        public StatisticsDto JobStats { get; set; }

        public ServerStatusModel(ILogger<ServerStatusModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            try
            {
                // Get monitoring API
                var monitor = JobStorage.Current.GetMonitoringApi();
                
                // Get server list
                var hangfireServers = monitor.Servers();
                
                var serverDetailsList = new List<ServerStatusViewModel>();
                foreach (var server in hangfireServers)
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
                
                Servers = serverDetailsList;
                ServerCount = hangfireServers.Count;
                
                // Get queue stats
                var queueList = monitor.Queues()
                    .Select(q => new QueueViewModel
                    {
                        Name = q.Name,
                        Length = (int)q.Length,  // Cast to int
                        IsPaused = q.Name.StartsWith("!") // Simple check for paused queues
                    })
                    .ToList();
                Queues = queueList;
                
                // Get overall stats
                JobStats = monitor.GetStatistics();
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Hangfire server status");
                ErrorMessage = "An error occurred while retrieving server status";
                Servers = new List<ServerStatusViewModel>();
                Queues = new List<QueueViewModel>();
                return Page();
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
    }
}