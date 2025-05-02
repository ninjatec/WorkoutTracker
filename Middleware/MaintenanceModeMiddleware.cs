using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Middleware
{
    public class MaintenanceModeMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public MaintenanceModeMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
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

            // Allow access to static files like CSS and JS even in maintenance mode
            string path = context.Request.Path.ToString().ToLower();
            bool isStaticFile = path.StartsWith("/css/") || 
                               path.StartsWith("/js/") || 
                               path.StartsWith("/lib/") || 
                               path.StartsWith("/images/") ||
                               path.EndsWith(".ico");

            // Allow admin users to access the site even in maintenance mode
            bool isAdmin = context.User?.IsInRole("Admin") ?? false;

            if (isMaintenanceMode && !isStaticFile && !isAdmin)
            {
                context.Response.StatusCode = 503; // Service Unavailable
                context.Response.Headers["Retry-After"] = "3600"; // Try again in an hour
                context.Response.ContentType = "text/html";

                await context.Response.SendFileAsync(Path.Combine(
                    Directory.GetCurrentDirectory(), "Pages", "Maintenance.cshtml"));
                return;
            }

            await _next(context);
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