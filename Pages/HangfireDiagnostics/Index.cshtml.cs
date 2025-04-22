using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Hangfire;

namespace WorkoutTrackerWeb.Pages.HangfireDiagnostics
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IHangfireInitializationService _hangfireInitService;
        private readonly BackgroundJobService _backgroundJobService;
        private readonly string _connectionString;

        public bool IsHangfireWorking { get; private set; }
        public string TestJobId { get; private set; }
        public Dictionary<string, int> JobCounts { get; private set; }
        public string DiagnosticInfo { get; private set; }
        public string ErrorMessage { get; private set; }

        public IndexModel(
            ILogger<IndexModel> logger,
            IHangfireInitializationService hangfireInitService,
            BackgroundJobService backgroundJobService,
            IConfiguration configuration)
        {
            _logger = logger;
            _hangfireInitService = hangfireInitService;
            _backgroundJobService = backgroundJobService;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult OnGet()
        {
            IsHangfireWorking = false;
            
            try
            {
                // Create a test job to verify Hangfire is working
                TestJobId = BackgroundJob.Enqueue(() => Console.WriteLine("Hangfire test job executed at: " + DateTime.Now));
                IsHangfireWorking = !string.IsNullOrEmpty(TestJobId);

                // Get Hangfire diagnostic information
                DiagnosticInfo = _hangfireInitService.GetDiagnosticInfo();
                
                // Get job counts
                JobCounts = GetJobCounts();
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Hangfire diagnostics");
                ErrorMessage = ex.Message;
                return Page();
            }
        }
        
        public async Task<IActionResult> OnPostRepairHangfireSchemaAsync()
        {
            try
            {
                bool success = await _hangfireInitService.RepairHangfireSchemaAsync();
                TempData["SuccessMessage"] = success 
                    ? "Hangfire schema repair completed successfully" 
                    : "Hangfire schema repair attempted but may not have fixed all issues";
                    
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error repairing Hangfire schema");
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return RedirectToPage();
            }
        }
        
        public IActionResult OnPostRunTestJob()
        {
            try
            {
                // Create a simple test job
                var jobId = BackgroundJob.Enqueue(() => Console.WriteLine($"Test job executed at: {DateTime.Now}"));
                
                TempData["SuccessMessage"] = $"Test job created successfully with ID: {jobId}";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test job");
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return RedirectToPage();
            }
        }
        
        public IActionResult OnPostRunDeleteTest()
        {
            try
            {
                // Create a test deletion job 
                var jobId = _backgroundJobService.QueueDeleteAllWorkoutData(
                    "test-user-id", 
                    "test-connection-id");
                
                if (string.IsNullOrEmpty(jobId))
                {
                    throw new Exception("QueueDeleteAllWorkoutData returned null or empty job ID");
                }
                
                TempData["SuccessMessage"] = $"Delete test job created successfully with ID: {jobId}";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating delete test job");
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return RedirectToPage();
            }
        }
        
        private Dictionary<string, int> GetJobCounts()
        {
            var result = new Dictionary<string, int>();
            
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Get job counts
                    string sql = @"
                        SELECT COUNT(*) FROM [HangFire].[Job] WHERE StateName = 'Enqueued';
                        SELECT COUNT(*) FROM [HangFire].[Job] WHERE StateName = 'Processing';
                        SELECT COUNT(*) FROM [HangFire].[Job] WHERE StateName = 'Succeeded';
                        SELECT COUNT(*) FROM [HangFire].[Job] WHERE StateName = 'Failed';
                        SELECT COUNT(*) FROM [HangFire].[Job] WHERE StateName = 'Scheduled';
                        SELECT COUNT(*) FROM [HangFire].[Job];
                    ";
                    
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read()) result["Enqueued"] = reader.GetInt32(0);
                            if (reader.NextResult() && reader.Read()) result["Processing"] = reader.GetInt32(0);
                            if (reader.NextResult() && reader.Read()) result["Succeeded"] = reader.GetInt32(0);
                            if (reader.NextResult() && reader.Read()) result["Failed"] = reader.GetInt32(0);
                            if (reader.NextResult() && reader.Read()) result["Scheduled"] = reader.GetInt32(0);
                            if (reader.NextResult() && reader.Read()) result["Total"] = reader.GetInt32(0);
                        }
                    }
                    
                    // Check for active servers
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM [HangFire].[Server] WHERE Heartbeat > DATEADD(MINUTE, -5, GETUTCDATE())";
                        var activeServers = (int)command.ExecuteScalar();
                        result["ActiveServers"] = activeServers;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job counts");
                result["Error"] = 1;
            }
            
            return result;
        }
    }
}