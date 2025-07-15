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

        /// <summary>
        /// Test endpoint specifically for Google Analytics CSP verification
        /// </summary>
        [HttpGet("google-analytics-csp-test")]
        [AllowAnonymous]
        public IActionResult GoogleAnalyticsCspTest()
        {
            _logger.LogInformation("Google Analytics CSP test endpoint called from {UserAgent} at {Timestamp}", 
                Request.Headers["User-Agent"].ToString(), DateTime.UtcNow);

            // Get the current CSP header that would be applied
            var cspHeader = Response.Headers["Content-Security-Policy"].ToString();
            
            // Check if Google Analytics domains are included
            var gaDomainsSupported = new Dictionary<string, bool>
            {
                ["www.googletagmanager.com"] = cspHeader.Contains("www.googletagmanager.com"),
                ["googletagmanager.com"] = cspHeader.Contains("googletagmanager.com"),
                ["www.google-analytics.com"] = cspHeader.Contains("www.google-analytics.com"),
                ["ssl.google-analytics.com"] = cspHeader.Contains("ssl.google-analytics.com"),
                ["tagmanager.google.com"] = cspHeader.Contains("tagmanager.google.com"),
                ["analytics.google.com"] = cspHeader.Contains("analytics.google.com"),
                ["stats.g.doubleclick.net"] = cspHeader.Contains("stats.g.doubleclick.net")
            };

            var allSupported = gaDomainsSupported.Values.All(supported => supported);

            return Ok(new
            {
                message = "Google Analytics CSP compatibility test",
                timestamp = DateTime.UtcNow,
                allGoogleAnalyticsDomainsSupported = allSupported,
                domainSupport = gaDomainsSupported,
                cspHeaderApplied = !string.IsNullOrEmpty(cspHeader),
                cspHeader = cspHeader,
                recommendations = allSupported ? 
                    "Google Analytics should work with current CSP policy" : 
                    "Some Google Analytics domains may be blocked by CSP",
                testInstructions = new
                {
                    step1 = "Check browser console for CSP violations",
                    step2 = "Monitor /api/CspReport/violations for blocked requests",
                    step3 = "Test Google Analytics tracking in browser developer tools"
                }
            });
        }
    }
}
