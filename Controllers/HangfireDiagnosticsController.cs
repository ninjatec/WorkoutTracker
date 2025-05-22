using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Hangfire;

namespace WorkoutTrackerWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class HangfireDiagnosticsController : Controller
    {
        private readonly ILogger<HangfireDiagnosticsController> _logger;
        private readonly IHangfireInitializationService _hangfireInitService;
        private readonly BackgroundJobService _backgroundJobService;
        private readonly string _connectionString;

        public HangfireDiagnosticsController(
            ILogger<HangfireDiagnosticsController> logger,
            IHangfireInitializationService hangfireInitService,
            BackgroundJobService backgroundJobService,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _logger = logger;
            _hangfireInitService = hangfireInitService;
            _backgroundJobService = backgroundJobService;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index()
        {
            ViewBag.IsHangfireWorking = false;
            
            try
            {
                // Create a test job to verify Hangfire is working
                var jobId = BackgroundJob.Enqueue(() => Console.WriteLine("Hangfire test job executed at: " + DateTime.Now));
                ViewBag.TestJobId = jobId;
                ViewBag.IsHangfireWorking = !string.IsNullOrEmpty(jobId);

                // Get Hangfire diagnostic information
                ViewBag.DiagnosticInfo = _hangfireInitService.GetDiagnosticInfo();
                
                // Get job counts
                var jobCounts = GetJobCounts();
                ViewBag.JobCounts = jobCounts;
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Hangfire diagnostics");
                ViewBag.ErrorMessage = ex.Message;
                return View();
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RepairHangfireSchema()
        {
            try
            {
                bool success = await _hangfireInitService.RepairHangfireSchemaAsync();
                TempData["SuccessMessage"] = success 
                    ? "Hangfire schema repair completed successfully" 
                    : "Hangfire schema repair attempted but may not have fixed all issues";
                    
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error repairing Hangfire schema");
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RunTestJob()
        {
            try
            {
                // Create a simple test job since ProcessImportAsync was removed
                var jobId = BackgroundJob.Enqueue(() => Console.WriteLine($"Test job executed at: {DateTime.Now}"));
                
                TempData["SuccessMessage"] = $"Test job created successfully with ID: {jobId}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test job");
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RunDeleteTest()
        {
            try
            {
                // Create a test deletion job using a simplified approach that doesn't require conversion to int
                var identityUserId = "test-user-id";
                var connectionId = "test-connection-id";
                
                // Use direct Hangfire.BackgroundJob API instead of the service
                var jobId = BackgroundJob.Enqueue(() => Console.WriteLine($"Delete test job for user {identityUserId} from connection {connectionId}"));
                
                if (string.IsNullOrEmpty(jobId))
                {
                    throw new Exception("Failed to create test deletion job");
                }
                
                TempData["SuccessMessage"] = $"Delete test job created successfully with ID: {jobId}";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating delete test job");
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return RedirectToAction("Index");
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
                            // Use GetFieldValue<int> with defaultValue parameter to handle NULL values safely
                            if (reader.Read()) result["Enqueued"] = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            if (reader.NextResult() && reader.Read()) result["Processing"] = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            if (reader.NextResult() && reader.Read()) result["Succeeded"] = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            if (reader.NextResult() && reader.Read()) result["Failed"] = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            if (reader.NextResult() && reader.Read()) result["Scheduled"] = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            if (reader.NextResult() && reader.Read()) result["Total"] = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
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
                            
                            var result_count = checkCmd.ExecuteScalar();
                            heartbeatExists = result_count != null && Convert.ToInt32(result_count) > 0;
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
                            
                            var serverCount = command.ExecuteScalar();
                            result["ActiveServers"] = serverCount != null ? Convert.ToInt32(serverCount) : 0;
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