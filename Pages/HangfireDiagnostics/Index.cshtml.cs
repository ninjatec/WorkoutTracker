using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
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
        private readonly WorkoutTrackerWeb.Services.Hangfire.HangfireServerConfiguration _serverConfiguration;
        private readonly string _connectionString;

        public bool IsHangfireWorking { get; private set; }
        public string TestJobId { get; private set; }
        public Dictionary<string, int> JobCounts { get; private set; }
        public string DiagnosticInfo { get; private set; }
        public string ErrorMessage { get; private set; }
        public bool IsProcessingEnabled { get; private set; }
        public int WorkerCount { get; private set; }
        public string ServerName { get; private set; }
        public string[] Queues { get; private set; }

        public IndexModel(
            ILogger<IndexModel> logger,
            IHangfireInitializationService hangfireInitService,
            BackgroundJobService backgroundJobService,
            WorkoutTrackerWeb.Services.Hangfire.HangfireServerConfiguration serverConfiguration,
            IConfiguration configuration)
        {
            _logger = logger;
            _hangfireInitService = hangfireInitService;
            _backgroundJobService = backgroundJobService;
            _serverConfiguration = serverConfiguration;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult OnGet()
        {
            IsHangfireWorking = false;
            
            try
            {
                // Get server configuration information
                IsProcessingEnabled = _serverConfiguration.IsProcessingEnabled;
                WorkerCount = _serverConfiguration.WorkerCount;
                // Use MachineName as ServerName if ServerName property doesn't exist
                ServerName = Environment.MachineName; 
                Queues = _serverConfiguration.Queues;
                
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
                // Create a test deletion job using a simplified approach that doesn't require conversion
                var identityUserId = "test-user-id";
                var connectionId = "test-connection-id";
                
                // Use direct Hangfire.BackgroundJob API instead of the service
                var jobId = BackgroundJob.Enqueue(() => Console.WriteLine($"Delete test job for user {identityUserId} from connection {connectionId}"));
                
                if (string.IsNullOrEmpty(jobId))
                {
                    throw new Exception("Failed to create test deletion job");
                }
                
                // Display the job ID as a string, no conversion needed
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
                    try 
                    {
                        // Check if the Heartbeat column exists
                        bool heartbeatExists = false;
                        using (var checkCmd = connection.CreateCommand())
                        {
                            checkCmd.CommandText = @"
                                SELECT COUNT(*)
                                FROM INFORMATION_SCHEMA.COLUMNS
                                WHERE TABLE_SCHEMA = 'HangFire'
                                  AND TABLE_NAME = 'Server'
                                  AND COLUMN_NAME = 'Heartbeat'";
                            
                            heartbeatExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                        }
                        
                        // Use appropriate query based on schema version
                        using (var command = connection.CreateCommand())
                        {
                            if (heartbeatExists)
                            {
                                command.CommandText = "SELECT COUNT(*) FROM [HangFire].[Server] WHERE Heartbeat > DATEADD(MINUTE, -5, GETUTCDATE())";
                            }
                            else
                            {
                                // Older Hangfire versions use LastHeartbeat column
                                command.CommandText = "SELECT COUNT(*) FROM [HangFire].[Server] WHERE LastHeartbeat > DATEADD(MINUTE, -5, GETUTCDATE())";
                            }
                            
                            var activeServers = Convert.ToInt32(command.ExecuteScalar());
                            result["ActiveServers"] = activeServers;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error checking active servers: {Message}", ex.Message);
                        result["ActiveServers"] = -1; // Indicate an error occurred
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