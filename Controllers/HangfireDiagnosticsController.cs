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