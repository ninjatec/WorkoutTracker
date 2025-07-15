using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace WorkoutTrackerWeb.Controllers
{
    /// <summary>
    /// Controller for handling CSP violation reports
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CspReportController : ControllerBase
    {
        private readonly ILogger<CspReportController> _logger;

        public CspReportController(ILogger<CspReportController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Endpoint to receive CSP violation reports
        /// </summary>
        [HttpPost("violations")]
        [AllowAnonymous]
        public async Task<IActionResult> ReceiveViolationReport()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var reportJson = await reader.ReadToEndAsync();

                if (string.IsNullOrEmpty(reportJson))
                {
                    return BadRequest("Empty report");
                }

                // Parse the CSP violation report
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var report = JsonSerializer.Deserialize<CspViolationReport>(reportJson, options);
                
                if (report?.CspReport != null)
                {
                    _logger.LogWarning("CSP Violation: {ViolatedDirective} - {BlockedUri} on {DocumentUri} - UserAgent: {UserAgent}", 
                        report.CspReport.ViolatedDirective,
                        report.CspReport.BlockedUri,
                        report.CspReport.DocumentUri,
                        Request.Headers.UserAgent.ToString());

                    // Log additional details for debugging
                    _logger.LogDebug("CSP Violation Details: {@CspReport}", report.CspReport);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CSP violation report");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Get recent CSP violations summary (for debugging)
        /// </summary>
        [HttpGet("violations/summary")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetViolationsSummary()
        {
            // This endpoint could be enhanced to return stored violation data
            // For now, it just confirms the endpoint is working
            return Ok(new
            {
                message = "CSP violation reporting is active",
                endpoint = "/api/CspReport/violations",
                timestamp = DateTime.UtcNow,
                note = "Check application logs for violation details"
            });
        }
    }

    /// <summary>
    /// Model for CSP violation reports
    /// </summary>
    public class CspViolationReport
    {
        public CspReportDetails? CspReport { get; set; }
    }

    /// <summary>
    /// Details of a CSP violation
    /// </summary>
    public class CspReportDetails
    {
        public string? DocumentUri { get; set; }
        public string? Referrer { get; set; }
        public string? ViolatedDirective { get; set; }
        public string? EffectiveDirective { get; set; }
        public string? OriginalPolicy { get; set; }
        public string? BlockedUri { get; set; }
        public int? LineNumber { get; set; }
        public int? ColumnNumber { get; set; }
        public string? SourceFile { get; set; }
        public string? StatusCode { get; set; }
        public string? ScriptSample { get; set; }
    }
}
