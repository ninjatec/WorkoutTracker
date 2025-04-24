using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using Serilog;
using ILogger = Serilog.ILogger;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Metrics
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger _logger;

        public IndexModel(WorkoutTrackerWebContext context)
        {
            _context = context;
            _logger = Log.ForContext<IndexModel>();
        }

        // System metrics properties
        public int CpuUsage { get; set; }
        public int MemoryUsage { get; set; }
        public int DiskUsage { get; set; }
        public string ProcessUptime { get; set; }
        public int ActiveConnections { get; set; }
        public int MaxConnections { get; set; }
        public double AvgQueryTime { get; set; }
        public int FailedQueries { get; set; }
        public double CacheHitRate { get; set; }
        public double CacheMemoryUsage { get; set; }
        public int CacheKeys { get; set; }
        public int CacheExpiringKeys { get; set; }
        
        // Hangfire metrics
        public int JobsEnqueued { get; set; }
        public int JobsProcessing { get; set; }
        public int JobsSucceeded { get; set; }
        public int JobsFailed { get; set; }
        
        // Exception tracking
        public List<ExceptionInfo> RecentExceptions { get; set; } = new List<ExceptionInfo>();
        
        // User metrics
        public int TotalUsers { get; set; }
        public int ActiveUsersToday { get; set; }
        public int ActiveUsersWeek { get; set; }
        public int ActiveUsersMonth { get; set; }
        public int NewUsers30Days { get; set; }
        
        // Workout metrics
        public int TotalSessions { get; set; }
        public int TotalSets { get; set; }
        public int TotalReps { get; set; }
        public int AvgSessionDuration { get; set; }
        public double AvgSetsPerSession { get; set; }
        
        // Performance metrics
        public List<EndpointPerformanceInfo> SlowestEndpoints { get; set; } = new List<EndpointPerformanceInfo>();
        
        // Health metrics
        public List<ServiceHealthInfo> ServiceHealth { get; set; } = new List<ServiceHealthInfo>();
        public List<CircuitBreakerInfo> CircuitBreakers { get; set; } = new List<CircuitBreakerInfo>();
        public double OverallSla { get; set; }
        public DateTime? LastOutage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Load basic metrics data
                await LoadMetricsDataAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading metrics dashboard");
                throw;
            }
        }

        // AJAX endpoint to refresh data
        public async Task<IActionResult> OnGetRefreshDataAsync()
        {
            await LoadMetricsDataAsync();
            return new JsonResult(new
            {
                systemMetrics = new
                {
                    CpuUsage,
                    MemoryUsage,
                    DiskUsage,
                    ProcessUptime,
                    ActiveConnections,
                    MaxConnections,
                    AvgQueryTime,
                    FailedQueries,
                    CacheHitRate,
                    CacheMemoryUsage,
                    JobsEnqueued,
                    JobsProcessing,
                    JobsSucceeded,
                    JobsFailed,
                    RecentExceptions
                },
                userMetrics = new
                {
                    TotalUsers,
                    ActiveUsersToday,
                    ActiveUsersWeek,
                    ActiveUsersMonth,
                    NewUsers30Days
                },
                workoutMetrics = new
                {
                    TotalSessions,
                    TotalSets,
                    TotalReps,
                    AvgSessionDuration,
                    AvgSetsPerSession
                },
                performanceMetrics = new
                {
                    SlowestEndpoints
                },
                healthMetrics = new
                {
                    ServiceHealth,
                    CircuitBreakers,
                    OverallSla,
                    LastOutage
                }
            });
        }

        private async Task LoadMetricsDataAsync()
        {
            // For this implementation, we're using sample data
            // In a real implementation, these would be fetched from various services
            
            // System metrics
            Random random = new Random();
            CpuUsage = random.Next(10, 60);
            MemoryUsage = random.Next(30, 70);
            DiskUsage = random.Next(20, 80);
            ProcessUptime = "3d 5h 23m 12s";
            ActiveConnections = random.Next(5, 20);
            MaxConnections = 100;
            AvgQueryTime = Math.Round(random.NextDouble() * 15, 2);
            FailedQueries = random.Next(0, 5);
            CacheHitRate = Math.Round(random.NextDouble() * 40 + 60, 2);
            CacheMemoryUsage = Math.Round(random.NextDouble() * 200, 2);
            CacheKeys = random.Next(500, 2000);
            CacheExpiringKeys = random.Next(50, 300);
            
            // Hangfire metrics
            JobsEnqueued = random.Next(0, 10);
            JobsProcessing = random.Next(0, 5);
            JobsSucceeded = random.Next(1000, 5000);
            JobsFailed = random.Next(0, 50);
            
            // Recent exceptions (sample data)
            RecentExceptions = new List<ExceptionInfo>
            {
                new ExceptionInfo { Type = "DbUpdateException", Message = "Timeout waiting for connection from pool", Time = DateTime.Now.AddMinutes(-15) },
                new ExceptionInfo { Type = "NullReferenceException", Message = "Object reference not set to an instance of an object in UserService.cs:128", Time = DateTime.Now.AddHours(-2) },
                new ExceptionInfo { Type = "InvalidOperationException", Message = "Cannot access a disposed object in HttpClient", Time = DateTime.Now.AddHours(-4) },
                new ExceptionInfo { Type = "OperationCanceledException", Message = "The operation was canceled in ImportService", Time = DateTime.Now.AddHours(-12) }
            };
            
            // User metrics (fetch actual counts from database)
            TotalUsers = await _context.User.CountAsync();
            
            var today = DateTime.Today;
            var lastWeek = today.AddDays(-7);
            var lastMonth = today.AddDays(-30);
            
            // Active users based on session creation (in a real app, would be more complex)
            ActiveUsersToday = await _context.Session
                .Where(s => s.datetime >= today)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();
                
            ActiveUsersWeek = await _context.Session
                .Where(s => s.datetime >= lastWeek)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();
                
            ActiveUsersMonth = await _context.Session
                .Where(s => s.datetime >= lastMonth)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();
            
            // Since we don't have a CreatedDate or LastUpdated field on the User model,
            // we'll use a proxy approach: users with recent sessions or identity registrations
            // We'll simulate this for now with random data
            NewUsers30Days = random.Next(5, 25);
            
            // Workout metrics (fetch actual counts)
            TotalSessions = await _context.Session.CountAsync();
            TotalSets = await _context.Set.CountAsync();
            TotalReps = await _context.Rep.CountAsync();
            
            // Average session duration - simulated
            AvgSessionDuration = random.Next(30, 90);
            
            // Average sets per session - calculated
            if (TotalSessions > 0)
            {
                AvgSetsPerSession = Math.Round((double)TotalSets / TotalSessions, 2);
            }
            else
            {
                AvgSetsPerSession = 0;
            }
            
            // Performance metrics - sample data
            SlowestEndpoints = new List<EndpointPerformanceInfo>
            {
                new EndpointPerformanceInfo { Path = "/Sessions/Index", AvgResponseTime = random.Next(30, 200), P95ResponseTime = random.Next(200, 500), RequestCount = random.Next(100, 1000), ErrorRate = random.Next(0, 2) },
                new EndpointPerformanceInfo { Path = "/Reports/Index", AvgResponseTime = random.Next(50, 300), P95ResponseTime = random.Next(300, 700), RequestCount = random.Next(50, 500), ErrorRate = random.Next(0, 3) },
                new EndpointPerformanceInfo { Path = "/Api/JobStatus/Index", AvgResponseTime = random.Next(10, 50), P95ResponseTime = random.Next(50, 150), RequestCount = random.Next(200, 2000), ErrorRate = random.Next(0, 1) },
                new EndpointPerformanceInfo { Path = "/Account/Login", AvgResponseTime = random.Next(20, 100), P95ResponseTime = random.Next(100, 300), RequestCount = random.Next(300, 3000), ErrorRate = random.Next(0, 2) },
                new EndpointPerformanceInfo { Path = "/Shared/Index", AvgResponseTime = random.Next(40, 200), P95ResponseTime = random.Next(200, 600), RequestCount = random.Next(10, 100), ErrorRate = random.Next(0, 10) }
            };
            
            // Health metrics - sample data
            ServiceHealth = new List<ServiceHealthInfo>
            {
                new ServiceHealthInfo { Name = "Database", IsHealthy = true, LastChecked = DateTime.Now.AddMinutes(-random.Next(1, 10)) },
                new ServiceHealthInfo { Name = "Redis Cache", IsHealthy = true, LastChecked = DateTime.Now.AddMinutes(-random.Next(1, 10)) },
                new ServiceHealthInfo { Name = "Hangfire", IsHealthy = true, LastChecked = DateTime.Now.AddMinutes(-random.Next(1, 10)) },
                new ServiceHealthInfo { Name = "Email Service", IsHealthy = random.Next(0, 10) > 2, LastChecked = DateTime.Now.AddMinutes(-random.Next(1, 10)) },
                new ServiceHealthInfo { Name = "External API", IsHealthy = random.Next(0, 10) > 1, LastChecked = DateTime.Now.AddMinutes(-random.Next(1, 10)) }
            };
            
            CircuitBreakers = new List<CircuitBreakerInfo>
            {
                new CircuitBreakerInfo { Name = "Database Connection", State = "Closed", LastStateChange = DateTime.Now.AddHours(-12) },
                new CircuitBreakerInfo { Name = "Redis Connection", State = "Closed", LastStateChange = DateTime.Now.AddHours(-6) },
                new CircuitBreakerInfo { Name = "Email Service", State = random.Next(0, 10) > 8 ? "Open" : "Closed", LastStateChange = DateTime.Now.AddMinutes(-random.Next(30, 180)) },
                new CircuitBreakerInfo { Name = "External API", State = random.Next(0, 10) > 8 ? "HalfOpen" : "Closed", LastStateChange = DateTime.Now.AddMinutes(-random.Next(10, 60)) }
            };
            
            OverallSla = 99.97;
            if (random.Next(0, 10) > 8)
            {
                LastOutage = DateTime.Now.AddDays(-random.Next(10, 60));
            }
        }

        // Model classes for various metrics data
        public class ExceptionInfo
        {
            public string Type { get; set; }
            public string Message { get; set; }
            public DateTime Time { get; set; }
        }
        
        public class EndpointPerformanceInfo
        {
            public string Path { get; set; }
            public double AvgResponseTime { get; set; }
            public double P95ResponseTime { get; set; }
            public int RequestCount { get; set; }
            public double ErrorRate { get; set; }
        }
        
        public class ServiceHealthInfo
        {
            public string Name { get; set; }
            public bool IsHealthy { get; set; }
            public DateTime LastChecked { get; set; }
        }
        
        public class CircuitBreakerInfo
        {
            public string Name { get; set; }
            public string State { get; set; }
            public DateTime LastStateChange { get; set; }
        }
    }
}