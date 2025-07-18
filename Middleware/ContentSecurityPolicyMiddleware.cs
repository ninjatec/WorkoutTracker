using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

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
        private readonly string _permissionsPolicyHeader;

        public ContentSecurityPolicyMiddleware(RequestDelegate next, ILogger<ContentSecurityPolicyMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
            _permissionsPolicyHeader = "camera=(), microphone=(), geolocation=()";
        }

        private string GenerateNonce()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var nonceBytes = new byte[16];
                rng.GetBytes(nonceBytes);
                return Convert.ToBase64String(nonceBytes);
            }
        }

        private string BuildCspHeader(string styleNonce, string scriptNonce)
        {
            // Check if CSP is enabled in configuration
            var cspEnabled = _configuration.GetValue<bool>("Security:ContentSecurityPolicy:Enabled", true);
            if (!cspEnabled)
            {
                return string.Empty;
            }

            // Get environment-specific settings
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var isProduction = environment.Equals("Production", StringComparison.OrdinalIgnoreCase);

            // Build CSP header optimized for Cloudflare with enhanced security
            // Updated to include comprehensive Google Analytics and Google Tag Manager domains
            var cspDirectives = new List<string>
            {
                "default-src 'self'",
                
                // Script sources - including Google Analytics and inline scripts with nonce
                // Note: 'unsafe-eval' has been removed for security (blocks eval() usage)
                // If JavaScript functionality breaks, consider nonce-based CSP or library updates
                $"script-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://static.cloudflareinsights.com https://challenges.cloudflare.com https://www.googletagmanager.com https://www.google-analytics.com https://googletagmanager.com https://ssl.google-analytics.com https://tagmanager.google.com 'nonce-{scriptNonce}'",

                
                // Style sources - use nonces instead of unsafe-inline for better security
                $"style-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://fonts.googleapis.com 'nonce-{styleNonce}'",
                
                // Image sources
                "img-src 'self' data: blob: https://cdn.jsdelivr.net https://challenges.cloudflare.com https://avatars.githubusercontent.com https://app.aikido.dev https://www.google-analytics.com https://ssl.google-analytics.com https://stats.g.doubleclick.net",
                
                // Font sources
                "font-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.gstatic.com data:",
                
                // Connection sources - restrict wildcards, include Google Analytics
                $"connect-src 'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.datatables.net https://www.google-analytics.com https://analytics.google.com https://region1.google-analytics.com https://region1.analytics.google.com https://stats.g.doubleclick.net https://app.aikido.dev {GetAllowedDomains()} https://challenges.cloudflare.com {GetWebSocketUrls()}",
                
                // Frame sources
                "frame-src 'self' https://challenges.cloudflare.com https://www.youtube.com",
                
                // Frame ancestors
                $"frame-ancestors 'self' {GetAllowedDomains()}",
                
                // Form actions
                $"form-action 'self' {GetAllowedDomains()} https://challenges.cloudflare.com",
                
                // Additional security directives
                "base-uri 'self'",
                "object-src 'none'",
                "media-src 'self' https://cdn.jsdelivr.net",
                "worker-src 'self' blob:",
                "manifest-src 'self'",
                
                // Upgrade insecure requests
                "upgrade-insecure-requests"
            };

            return string.Join("; ", cspDirectives);
        }

        private string GetAllowedDomains()
        {
            return "https://wot.ninjatec.co.uk https://workouttracker.online https://www.workouttracker.online";
        }

        private string GetWebSocketUrls()
        {
            // More restrictive WebSocket URLs instead of wildcards
            return "wss://wot.ninjatec.co.uk wss://workouttracker.online wss://www.workouttracker.online ws://wot.ninjatec.co.uk ws://workouttracker.online ws://www.workouttracker.online";
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Generate nonces for this request
            var styleNonce = GenerateNonce();
            var scriptNonce = GenerateNonce();
            
            // Make nonces available to views through HttpContext.Items
            context.Items["CSP-Style-Nonce"] = styleNonce;
            context.Items["CSP-Script-Nonce"] = scriptNonce;

            // Add security headers before processing the request
            context.Response.OnStarting(() =>
            {
                var response = context.Response;

                // Check if CSP is enabled in configuration
                var cspEnabled = _configuration.GetValue<bool>("Security:ContentSecurityPolicy:Enabled", true);
                if (cspEnabled)
                {
                    // Build CSP header with generated nonces
                    var cspHeader = BuildCspHeader(styleNonce, scriptNonce);
                    
                    if (!string.IsNullOrEmpty(cspHeader) && !response.Headers.ContainsKey("Content-Security-Policy"))
                    {
                        // Add CSP reporting endpoint if configured
                        var cspWithReporting = cspHeader;
                        var reportUri = _configuration.GetValue<string>("Security:ContentSecurityPolicy:ReportUri");
                        if (!string.IsNullOrEmpty(reportUri))
                        {
                            cspWithReporting += $"; report-uri {reportUri}";
                        }

                        response.Headers["Content-Security-Policy"] = cspWithReporting;
                        _logger.LogDebug("Added Content-Security-Policy header with nonces to response for {Path}", context.Request.Path);
                    }
                }

                // Add other security headers with enhanced protection
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

                // Add Strict Transport Security for HTTPS
                if (context.Request.IsHttps && !response.Headers.ContainsKey("Strict-Transport-Security"))
                {
                    response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
                }

                // Add Cross-Origin-Embedder-Policy for enhanced security (relaxed for external badges)
                if (!response.Headers.ContainsKey("Cross-Origin-Embedder-Policy"))
                {
                    response.Headers["Cross-Origin-Embedder-Policy"] = "credentialless";
                }

                // Add Cross-Origin-Opener-Policy
                if (!response.Headers.ContainsKey("Cross-Origin-Opener-Policy"))
                {
                    response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
                }

                // Add X-XSS-Protection for legacy browser protection
                if (!response.Headers.ContainsKey("X-XSS-Protection"))
                {
                    response.Headers["X-XSS-Protection"] = "1; mode=block";
                }

                // Add Cross-Origin-Resource-Policy
                if (!response.Headers.ContainsKey("Cross-Origin-Resource-Policy"))
                {
                    response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
                }
                
                // Add security headers specifically to mitigate CSRF cookie exposure
                // These headers help protect against XSS attacks that could steal the CSRF token
                if (!response.Headers.ContainsKey("Cache-Control"))
                {
                    // Prevent caching of potentially sensitive responses
                    var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
                    if (path.Contains("login") || path.Contains("register") || path.Contains("account"))
                    {
                        response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
                        response.Headers["Pragma"] = "no-cache";
                        response.Headers["Expires"] = "0";
                    }
                }

                // Add custom headers to verify this middleware is running
                response.Headers["X-CSP-Applied"] = "true";
                response.Headers["X-CSP-Source"] = "WorkoutTracker-Middleware";
                response.Headers["X-CSP-Version"] = "2.2"; // Updated: Enhanced cookie security

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
