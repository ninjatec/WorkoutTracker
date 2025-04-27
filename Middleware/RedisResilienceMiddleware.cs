using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace WorkoutTrackerWeb.Middleware
{
    public class RedisResilienceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RedisResilienceMiddleware> _logger;

        public RedisResilienceMiddleware(RequestDelegate next, ILogger<RedisResilienceMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add anti-redirect loop detection
            int redirectCount = 0;
            string redirectCountHeader = context.Request.Headers["X-Redirect-Count"];
            if (!string.IsNullOrEmpty(redirectCountHeader) && int.TryParse(redirectCountHeader, out int count))
            {
                redirectCount = count;
            }

            // If we've already been redirected too many times and there's a Redis issue, break the loop
            if (redirectCount >= 3 && IsHomePageRequest(context))
            {
                _logger.LogWarning("Potential redirect loop detected on homepage with {RedirectCount} redirects. Breaking the loop.", redirectCount);
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsync("Service temporarily unavailable due to cache issues. Please try again in a few minutes.");
                return;
            }

            // Mark redirect count for the next request if this is the homepage
            if (IsHomePageRequest(context))
            {
                context.Response.Headers["X-Redirect-Count"] = (redirectCount + 1).ToString();
            }

            try
            {
                await _next(context);
            }
            catch (StackExchange.Redis.RedisConnectionException ex)
            {
                await HandleRedisExceptionAsync(context, ex, "Redis connection exception during request - continuing with degraded functionality");
            }
            catch (Exception ex) when (ex.GetType().Name.Contains("Redis"))
            {
                await HandleRedisExceptionAsync(context, ex, "Redis-related exception caught - continuing with degraded functionality");
            }
        }

        private async Task HandleRedisExceptionAsync(HttpContext context, Exception ex, string logMessage)
        {
            _logger.LogWarning(ex, logMessage);
            
            // For API requests, return a meaningful response
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"status\":\"error\",\"message\":\"Cache service temporarily unavailable\"}");
                return;
            }

            // Handle homepage specifically to prevent redirect loops
            if (IsHomePageRequest(context))
            {
                // Check if we're seeing multiple attempts
                string redirectCountHeader = context.Request.Headers["X-Redirect-Count"];
                if (!string.IsNullOrEmpty(redirectCountHeader) && int.TryParse(redirectCountHeader, out int count) && count >= 2)
                {
                    // Break the loop with a friendly error page
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    await context.Response.WriteAsync("The site is experiencing temporary cache issues. Please try again in a few minutes.");
                    return;
                }
            }
            
            // For regular requests, continue processing without redirect
            if (!context.Response.HasStarted)
            {
                // Mark that a Redis error occurred but don't redirect
                context.Items["RedisConnectionError"] = true;
                
                // For AJAX requests, we should return a proper status code
                if (context.Request.Headers["X-Requested-With"].ToString().Contains("XMLHttpRequest"))
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    await context.Response.WriteAsync("Cache service temporarily unavailable");
                    return;
                }

                // For non-AJAX GET requests, we'll render the page with a warning
                // This is handled by the application code, no redirect needed
                
                // If we've already started processing the response but haven't written 
                // anything yet, set a 503 status
                if (context.Response.StatusCode == StatusCodes.Status200OK && 
                    context.Response.ContentLength == null && 
                    !context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                }
            }
        }

        private bool IsHomePageRequest(HttpContext context)
        {
            return context.Request.Path.Value.Equals("/", StringComparison.OrdinalIgnoreCase) ||
                   context.Request.Path.Value.Equals("/index", StringComparison.OrdinalIgnoreCase) ||
                   context.Request.Path.Value.Equals("/home", StringComparison.OrdinalIgnoreCase) ||
                   context.Request.Path.Value.Equals("/home/index", StringComparison.OrdinalIgnoreCase);
        }
    }
    
    // Extension method for middleware registration
    public static class RedisResilienceMiddlewareExtensions
    {
        public static IApplicationBuilder UseRedisResilience(
            this IApplicationBuilder builder)
        {
            // Skip middleware in development environment
            if (builder.ApplicationServices.GetService<IHostEnvironment>()?.IsDevelopment() ?? false)
            {
                // Don't add the middleware in development
                return builder;
            }

            return builder.UseMiddleware<RedisResilienceMiddleware>();
        }
    }
}