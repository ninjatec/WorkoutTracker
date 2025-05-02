using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Areas.Admin.ViewModels;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using System.Text.Json.Serialization;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Extensions;

namespace WorkoutTrackerWeb.Areas.Admin.Pages
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<IndexModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DatabaseResilienceService _databaseResilienceService;

        public IndexModel(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            WorkoutTrackerWebContext context,
            ILogger<IndexModel> logger,
            IHttpClientFactory httpClientFactory,
            DatabaseResilienceService databaseResilienceService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _databaseResilienceService = databaseResilienceService;
        }

        // User statistics
        public int UserCount { get; set; }
        public int ActiveUsersToday { get; set; }
        public int AdminCount { get; set; }
        public int CoachCount { get; set; }
        public int CoachClientRelationshipCount { get; set; }
        
        // Workout statistics
        public int SessionCount { get; set; }
        public int SetCount { get; set; }
        public int RepCount { get; set; }
        
        // System health status
        public bool DatabaseStatus { get; set; } = true;
        public bool ApiStatus { get; set; } = true;
        public bool EmailStatus { get; set; } = true;
        public bool RedisStatus { get; set; } = true;
        
        // Database connection pooling and resilience info
        public bool IsCircuitBreakerOpen { get; set; }
        public DateTime? CircuitBreakerLastStateChange { get; set; }
        public Dictionary<string, string> ConnectionPoolInfo { get; set; } = new Dictionary<string, string>();
        
        // Recent users
        public List<UserViewModel> RecentUsers { get; set; } = new List<UserViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDashboardDataAsync();
            return Page();
        }

        private async Task LoadDashboardDataAsync()
        {
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);
            
            // Active Users (users who have logged in within the last 30 days)
            ActiveUsersToday = await _context.LoginHistory
                .Where(lh => lh.LoginTime >= thirtyDaysAgo)
                .Select(lh => lh.IdentityUserId)
                .Distinct()
                .CountAsync();

            // Last 30 Days Stats
            var lastThirtyDaysSessions = await _context.WorkoutSessions
                .Where(s => s.StartDateTime >= thirtyDaysAgo)
                .CountAsync();

            var totalSetsLast30Days = await _context.WorkoutSets
                .Include(s => s.WorkoutExercise)
                    .ThenInclude(e => e.WorkoutSession)
                .Where(s => s.WorkoutExercise.WorkoutSession.StartDateTime >= thirtyDaysAgo)
                .CountAsync();

            // Total Stats
            SessionCount = await _context.WorkoutSessions.CountAsync();
            SetCount = await _context.WorkoutSets.CountAsync();
            RepCount = await _context.WorkoutSets
                .Where(s => s.Reps.HasValue)
                .SumAsync(s => s.Reps.Value);

            // Database Statistics
            var dbStatistics = await _context.GetDatabaseStatisticsAsync();
            ConnectionPoolInfo = dbStatistics;

            // Recent Activity
            var recentActivity = await _context.WorkoutSessions
                .Include(s => s.User)
                .OrderByDescending(s => s.StartDateTime)
                .Take(10)
                .Select(s => new RecentActivityViewModel 
                { 
                    Username = s.User.Name,
                    ActivityType = "Workout Session",
                    ActivityDescription = $"Completed workout session: {s.Name}",
                    Timestamp = s.EndDateTime ?? s.StartDateTime
                })
                .ToListAsync();
        }

        private async Task CheckHealthStatusAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                
                // Check database health
                var dbResponse = await client.GetAsync(new Uri($"{Request.Scheme}://{Request.Host}/health/database"));
                DatabaseStatus = dbResponse.IsSuccessStatusCode;
                
                if (dbResponse.IsSuccessStatusCode)
                {
                    // Parse the detailed health check response
                    var responseContent = await dbResponse.Content.ReadAsStringAsync();
                    try 
                    {
                        var healthData = JsonSerializer.Deserialize<HealthCheckResponse>(responseContent);
                        
                        // Extract connection pool information if available
                        if (healthData?.Entries != null)
                        {
                            // Look for the database_connection_pool health check
                            if (healthData.Entries.TryGetValue("database_connection_pool", out var poolEntry) && 
                                poolEntry.Data != null)
                            {
                                foreach (var item in poolEntry.Data)
                                {
                                    ConnectionPoolInfo[item.Key] = item.Value?.ToString() ?? "N/A";
                                }
                            }
                            
                            // Also try the direct database health check as a fallback
                            else if (healthData.Entries.TryGetValue("database_health_check", out var dbEntry) && 
                                     dbEntry.Data != null)
                            {
                                foreach (var item in dbEntry.Data)
                                {
                                    ConnectionPoolInfo[item.Key] = item.Value?.ToString() ?? "N/A";
                                }
                            }
                            
                            // Also look for sql_health_check information
                            else if (healthData.Entries.TryGetValue("sql_health_check", out var sqlEntry) && 
                                     sqlEntry.Data != null)
                            {
                                foreach (var item in sqlEntry.Data)
                                {
                                    ConnectionPoolInfo[item.Key] = item.Value?.ToString() ?? "N/A";
                                }
                            }
                            
                            if (ConnectionPoolInfo.Count == 0)
                            {
                                // Add basic connection status if no detailed info is available
                                ConnectionPoolInfo["Status"] = "Connected";
                                ConnectionPoolInfo["Note"] = "Detailed pool information not available";
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Error parsing health check response");
                        ConnectionPoolInfo["Error"] = "Failed to parse health data: " + ex.Message;
                    }
                }
                
                // Check Redis health (only in production)
                if (!HttpContext.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Use the dedicated Redis health check endpoint
                        var redisResponse = await client.GetAsync(new Uri($"{Request.Scheme}://{Request.Host}/health/redis"));
                        RedisStatus = redisResponse.IsSuccessStatusCode;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error checking Redis health status");
                        RedisStatus = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking service health status");
                // In case of errors, default to false
                DatabaseStatus = false;
                ApiStatus = false;
                EmailStatus = false;
                RedisStatus = false;
            }
        }
        
        // Health check response models
        private class HealthCheckResponse
        {
            public string Status { get; set; }
            public Dictionary<string, HealthCheckEntry> Entries { get; set; }
        }
        
        private class HealthCheckEntry
        {
            public string Status { get; set; }
            [JsonExtensionData]
            public Dictionary<string, object> Data { get; set; }
        }
    }
}