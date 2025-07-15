using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace WorkoutTrackerWeb.Controllers
{
    /// <summary>
    /// Controller for testing CSP headers and security configurations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SecurityTestController : ControllerBase
    {
        private readonly ILogger<SecurityTestController> _logger;

        public SecurityTestController(ILogger<SecurityTestController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Test endpoint to verify CSP headers are being applied
        /// </summary>
        [HttpGet("csp-test")]
        [AllowAnonymous]
        public IActionResult TestCsp()
        {
            _logger.LogInformation("CSP test endpoint called from {UserAgent} at {Timestamp}", 
                Request.Headers["User-Agent"].ToString(), DateTime.UtcNow);

            var responseHeaders = new Dictionary<string, string>();
            
            // Capture all response headers that have been set
            foreach (var header in Response.Headers)
            {
                responseHeaders[header.Key] = string.Join(", ", header.Value.ToArray());
            }

            return Ok(new
            {
                message = "CSP test endpoint response",
                timestamp = DateTime.UtcNow,
                headers = responseHeaders,
                requestPath = Request.Path,
                userAgent = Request.Headers["User-Agent"].ToString(),
                cloudflareHeaders = new
                {
                    cfRay = Request.Headers["CF-Ray"].ToString(),
                    cfConnectingIp = Request.Headers["CF-Connecting-IP"].ToString(),
                    cfIpCountry = Request.Headers["CF-IPCountry"].ToString(),
                    cfVisitor = Request.Headers["CF-Visitor"].ToString()
                }
            });
        }

        /// <summary>
        /// Test endpoint specifically for Cloudflare to verify CSP
        /// </summary>
        [HttpGet("cloudflare-csp-check")]
        [AllowAnonymous]
        public IActionResult CloudflareCspCheck()
        {
            // Force explicit CSP header for this endpoint
            Response.Headers["Content-Security-Policy"] = 
                "default-src 'self'; " +
                "script-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://static.cloudflareinsights.com https://challenges.cloudflare.com 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.3/font/bootstrap-icons.css 'unsafe-inline'; " +
                "img-src 'self' data: blob: https://cdn.jsdelivr.net https://challenges.cloudflare.com; " +
                "font-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com data:; " +
                "connect-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://wot.ninjatec.co.uk https://workouttracker.online https://www.workouttracker.online https://challenges.cloudflare.com wss://wot.ninjatec.co.uk wss://workouttracker.online wss://www.workouttracker.online ws://wot.ninjatec.co.uk ws://workouttracker.online ws://www.workouttracker.online wss://* ws://*/; " +
                "frame-src 'self' https://challenges.cloudflare.com; " +
                "frame-ancestors 'self' https://wot.ninjatec.co.uk https://workouttracker.online https://www.workouttracker.online; " +
                "form-action 'self' https://wot.ninjatec.co.uk https://workouttracker.online https://www.workouttracker.online https://challenges.cloudflare.com; " +
                "base-uri 'self'; " +
                "object-src 'none'; " +
                "upgrade-insecure-requests";

            Response.Headers["X-CSP-Test"] = "Cloudflare-Specific";
            Response.Headers["X-Test-Timestamp"] = DateTime.UtcNow.ToString("O");

            return Ok(new
            {
                message = "Cloudflare CSP verification endpoint",
                cspApplied = true,
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                headers = Response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value.ToArray()))
            });
        }
    }
}
