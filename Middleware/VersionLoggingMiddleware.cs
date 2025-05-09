using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Context;
using WorkoutTrackerWeb.Services.VersionManagement;

namespace WorkoutTrackerWeb.Middleware
{
    public class VersionLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public VersionLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Get version service from the DI container
            var versionService = context.RequestServices.GetService<IVersionService>();
            
            if (versionService != null)
            {
                // Get current version string asynchronously
                var versionString = await versionService.GetVersionDisplayStringAsync();
                
                // Add version to logging context
                using (LogContext.PushProperty("AppVersion", versionString))
                {
                    // Continue processing the request with version in the log context
                    await _next(context);
                }
            }
            else
            {
                // If version service is not available, continue without adding version
                await _next(context);
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline
    public static class VersionLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseVersionLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<VersionLoggingMiddleware>();
        }
    }
}