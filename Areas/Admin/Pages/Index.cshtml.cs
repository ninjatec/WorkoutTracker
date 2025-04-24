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

namespace WorkoutTrackerWeb.Areas.Admin.Pages
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<IndexModel> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DatabaseResilienceService _databaseResilienceService;

        public IndexModel(
            UserManager<IdentityUser> userManager,
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
            // Get total user count
            UserCount = await _userManager.Users.CountAsync();
            
            // Get admin users count
            var adminRole = await _roleManager.FindByNameAsync("Admin");
            if (adminRole != null)
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                AdminCount = adminUsers.Count;
            }
            
            // Get users active today based on sessions
            var today = DateTime.Today;
            var activeSessions = await _context.Session
                .Where(s => s.datetime >= today)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();
            ActiveUsersToday = activeSessions;
            
            // Get total session count
            SessionCount = await _context.Session.CountAsync();
            
            // Get total set count
            SetCount = await _context.Set.CountAsync();
            
            // Get total rep count
            RepCount = await _context.Rep.CountAsync();
            
            // Check health status of services
            await CheckHealthStatusAsync();
            
            // Get database resilience status
            var (isOpen, lastStateChange) = _databaseResilienceService.GetCircuitBreakerState();
            IsCircuitBreakerOpen = isOpen;
            CircuitBreakerLastStateChange = lastStateChange;
            
            // Get 5 most recent users
            var recentIdentityUsers = await _userManager.Users
                .OrderByDescending(u => u.Id)  // Using ID as a proxy for creation date
                .Take(5)
                .ToListAsync();
                
            foreach (var user in recentIdentityUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                
                RecentUsers.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    Roles = roles.ToList(),
                    CreatedDate = DateTime.Now.AddDays(-new Random().Next(1, 30)) // Placeholder since we don't have CreatedDate
                });
            }
            
            return Page();
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