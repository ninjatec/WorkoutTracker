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
            try
            {
                await _next(context);
            }
            catch (StackExchange.Redis.RedisConnectionException ex)
            {
                _logger.LogWarning(ex, "Redis connection exception during request - continuing with degraded functionality");
                
                // For API requests, return a meaningful response
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"status\":\"error\",\"message\":\"Cache service temporarily unavailable\"}");
                    return;
                }
                
                // For regular requests, continue processing (sessions will operate in degraded mode)
                if (!context.Response.HasStarted)
                {
                    // If we can still write to the response, redirect to the original destination
                    context.Items["RedisConnectionError"] = true;
                    
                    // Continue or redirect based on the type of request
                    if (context.Request.Method == "GET" && !context.Request.Headers["X-Requested-With"].ToString().Contains("XMLHttpRequest"))
                    {
                        context.Response.Redirect(context.Request.Path + context.Request.QueryString);
                    }
                }
            }
            catch (Exception ex) when (ex.GetType().Name.Contains("Redis"))
            {
                // Catch any Redis-related exception by name
                _logger.LogWarning(ex, "Redis-related exception caught - continuing with degraded functionality");
                
                // For API requests, return a meaningful response
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"status\":\"error\",\"message\":\"Cache service temporarily unavailable\"}");
                    return;
                }
                
                // For regular requests, continue processing (sessions will operate in degraded mode)
                if (!context.Response.HasStarted)
                {
                    // If we can still write to the response, redirect to the original destination
                    context.Items["RedisConnectionError"] = true;
                    
                    // Continue or redirect based on the type of request
                    if (context.Request.Method == "GET" && !context.Request.Headers["X-Requested-With"].ToString().Contains("XMLHttpRequest"))
                    {
                        context.Response.Redirect(context.Request.Path + context.Request.QueryString);
                    }
                }
            }
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