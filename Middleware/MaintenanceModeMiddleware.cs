using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Middleware
{
    public class MaintenanceModeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ITokenRateLimiter _tokenRateLimiter;

        public MaintenanceModeMiddleware(
            RequestDelegate next, 
            IConfiguration configuration,
            ITokenRateLimiter tokenRateLimiter)
        {
            _next = next;
            _configuration = configuration;
            _tokenRateLimiter = tokenRateLimiter;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            bool isMaintenanceMode = false;
            
            // Check environment variable first
            string? maintenanceModeEnv = Environment.GetEnvironmentVariable("MAINTENANCE_MODE");
            if (!string.IsNullOrEmpty(maintenanceModeEnv) && 
                (maintenanceModeEnv.Equals("true", StringComparison.OrdinalIgnoreCase) || maintenanceModeEnv == "1"))
            {
                isMaintenanceMode = true;
            }
            
            // Also check configuration in case it's set there
            if (!isMaintenanceMode)
            {
                isMaintenanceMode = _configuration.GetValue<bool>("MaintenanceMode");
            }

            if (!isMaintenanceMode)
            {
                await _next(context);
                return;
            }

            // Store the maintenance mode state to make it available to other middleware and views
            context.Items["MaintenanceMode"] = true;

            // Allow access to static files like CSS and JS even in maintenance mode
            string path = context.Request.Path.ToString().ToLower();
            bool isStaticFile = path.StartsWith("/css/") || 
                                path.StartsWith("/js/") || 
                                path.StartsWith("/lib/") || 
                                path.StartsWith("/images/") ||
                                path.EndsWith(".ico");

            // Get client IP address
            string? clientIp = GetClientIpAddress(context);
            
            // Check if user is admin or IP is whitelisted
            bool isAdmin = context.User?.IsInRole("Admin") ?? false;
            bool isWhitelisted = !string.IsNullOrEmpty(clientIp) && _tokenRateLimiter.IsIpWhitelisted(clientIp);
            
            // Access is allowed if:
            // 1. It's a static file
            // 2. User is an admin
            // 3. IP is whitelisted
            // Otherwise, redirect to maintenance page
            bool canAccess = isStaticFile || isAdmin || isWhitelisted;

            if (!canAccess && !path.Equals("/maintenance"))
            {
                // Redirect to maintenance page
                context.Response.StatusCode = 503; // Service Unavailable
                context.Response.Headers["Retry-After"] = "3600"; // Try again in an hour
                
                // Use built-in redirect instead of serving the file directly
                context.Response.Redirect("/Maintenance");
                return;
            }

            await _next(context);
        }

        private string? GetClientIpAddress(HttpContext context)
        {
            string? ip = null;
            
            // Try to get IP from standard headers
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                ip = forwardedFor.FirstOrDefault()?.Split(',')[0].Trim();
            }
            
            // If not found in X-Forwarded-For, try X-Real-IP
            if (string.IsNullOrEmpty(ip) && context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
            {
                ip = realIp.FirstOrDefault();
            }
            
            // Fall back to connection remote IP if headers not available
            if (string.IsNullOrEmpty(ip))
            {
                ip = context.Connection.RemoteIpAddress?.ToString();
            }
            
            return ip;
        }
    }

    // Extension method for the middleware
    public static class MaintenanceModeMiddlewareExtensions
    {
        public static IApplicationBuilder UseMaintenanceMode(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MaintenanceModeMiddleware>();
        }
    }
}