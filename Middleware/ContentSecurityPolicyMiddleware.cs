using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Middleware
{
    /// <summary>
    /// Middleware to add Content Security Policy headers to all responses with Cloudflare compatibility
    /// </summary>
    public class ContentSecurityPolicyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ContentSecurityPolicyMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _cspHeader;
        private readonly string _permissionsPolicyHeader;

        public ContentSecurityPolicyMiddleware(RequestDelegate next, ILogger<ContentSecurityPolicyMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;

            // Build CSP header with Cloudflare optimization
            _cspHeader = BuildCspHeader();
            _permissionsPolicyHeader = "camera=(), microphone=(), geolocation=()";
        }

        private string BuildCspHeader()
        {
            // Check if CSP is enabled in configuration
            var cspEnabled = _configuration.GetValue<bool>("Security:ContentSecurityPolicy:Enabled", true);
            if (!cspEnabled)
            {
                return string.Empty;
            }

            // Build CSP header optimized for Cloudflare
            var cspDirectives = new[]
            {
                "default-src 'self'",
                "script-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://static.cloudflareinsights.com https://challenges.cloudflare.com 'unsafe-inline' 'unsafe-eval'",
                "style-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.3/font/bootstrap-icons.css 'unsafe-inline'",
                "img-src 'self' data: blob: https://cdn.jsdelivr.net https://challenges.cloudflare.com",
                "font-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com data:",
                "connect-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://wot.ninjatec.co.uk https://workouttracker.online https://www.workouttracker.online https://challenges.cloudflare.com wss://wot.ninjatec.co.uk wss://workouttracker.online wss://www.workouttracker.online ws://wot.ninjatec.co.uk ws://workouttracker.online ws://www.workouttracker.online wss://* ws://*",
                "frame-src 'self' https://challenges.cloudflare.com",
                "frame-ancestors 'self' https://wot.ninjatec.co.uk https://workouttracker.online https://www.workouttracker.online",
                "form-action 'self' https://wot.ninjatec.co.uk https://workouttracker.online https://www.workouttracker.online https://challenges.cloudflare.com",
                "base-uri 'self'",
                "object-src 'none'",
                "upgrade-insecure-requests"
            };

            return string.Join("; ", cspDirectives);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers before processing the request
            context.Response.OnStarting(() =>
            {
                var response = context.Response;

                // Add CSP header if not already present and CSP is not empty
                if (!string.IsNullOrEmpty(_cspHeader) && !response.Headers.ContainsKey("Content-Security-Policy"))
                {
                    response.Headers["Content-Security-Policy"] = _cspHeader;
                    _logger.LogDebug("Added Content-Security-Policy header to response for {Path}", context.Request.Path);
                }

                // Add other security headers
                if (!response.Headers.ContainsKey("Permissions-Policy"))
                {
                    response.Headers["Permissions-Policy"] = _permissionsPolicyHeader;
                }

                if (!response.Headers.ContainsKey("X-Content-Type-Options"))
                {
                    response.Headers["X-Content-Type-Options"] = "nosniff";
                }

                if (!response.Headers.ContainsKey("X-Frame-Options"))
                {
                    response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                }

                if (!response.Headers.ContainsKey("Referrer-Policy"))
                {
                    response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                }

                // Add Cloudflare-specific headers
                if (!response.Headers.ContainsKey("X-Content-Type-Options"))
                {
                    response.Headers["X-Content-Type-Options"] = "nosniff";
                }

                // Add a custom header to verify this middleware is running
                response.Headers["X-CSP-Applied"] = "true";
                response.Headers["X-CSP-Source"] = "WorkoutTracker-Middleware";

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }

    /// <summary>
    /// Extension method for adding the CSP middleware to the pipeline
    /// </summary>
    public static class ContentSecurityPolicyMiddlewareExtensions
    {
        public static IApplicationBuilder UseContentSecurityPolicy(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ContentSecurityPolicyMiddleware>();
        }
    }
}
