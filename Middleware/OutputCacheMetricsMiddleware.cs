using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.OutputCaching;
using WorkoutTrackerWeb.Services.Cache;
using Microsoft.Extensions.Primitives;

namespace WorkoutTrackerWeb.Middleware
{
    /// <summary>
    /// Middleware to track output cache metrics and enable cache partitioning
    /// </summary>
    public class OutputCacheMetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<OutputCacheMetricsMiddleware> _logger;
        private readonly OutputCacheBackgroundRefresher _backgroundRefresher;
        
        public OutputCacheMetricsMiddleware(
            RequestDelegate next, 
            ILogger<OutputCacheMetricsMiddleware> logger,
            OutputCacheBackgroundRefresher backgroundRefresher = null)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backgroundRefresher = backgroundRefresher; // Optional service
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            // Skip for background refresh requests to avoid recursion
            if (context.Items.ContainsKey("OutputCache_BackgroundRefresh"))
            {
                await _next(context);
                return;
            }
            
            // Add custom cache tags for common content types - these can be used for targeted invalidation
            var path = context.Request.Path.Value?.ToLowerInvariant();
            
            // Get user ID if authenticated for user-specific cache partitioning
            int? userId = null;
            if (context.User?.Identity?.IsAuthenticated ?? false)
            {
                var userIdClaim = context.User.FindFirst("sub") ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var parsedUserId))
                {
                    userId = parsedUserId;
                    
                    // Add user-specific cache tag for easier invalidation per user
                    context.AddOutputCacheTags($"user:{parsedUserId}");
                }
            }
            
            // Add content-type specific tags for easier invalidation
            if (path != null)
            {
                if (path.StartsWith("/workout") || path.Contains("/workout/"))
                {
                    context.AddOutputCacheTags("content:workouts");
                    
                    // Add user-specific content tag if authenticated
                    if (userId.HasValue)
                    {
                        context.AddOutputCacheTags($"content:workouts:user:{userId.Value}");
                    }
                }
                else if (path.StartsWith("/reports") || path.Contains("/reports/"))
                {
                    context.AddOutputCacheTags("content:reports");
                    
                    if (userId.HasValue)
                    {
                        context.AddOutputCacheTags($"content:reports:user:{userId.Value}");
                    }
                }
                else if (path == "/" || path == "/index" || path == "/home" || path == "/home/index")
                {
                    context.AddOutputCacheTags("content:home");
                }
            }
            
            // Record start time to measure response time
            var startTime = DateTime.UtcNow;
            
            // Replace the response body with a wrapper to detect if response came from cache
            var wasCacheHit = false;
            context.Response.OnStarting(() => {
                // Check for cache hit based on headers or response time
                wasCacheHit = context.Response.Headers.TryGetValue("X-Cache", out var cacheStatus) && 
                             (cacheStatus.Count > 0 && (cacheStatus[0] == "HIT" || cacheStatus[0].Contains("HIT")));
                
                // If we can't determine from headers, use timing as a heuristic
                if (!wasCacheHit)
                {
                    var responseTime = DateTime.UtcNow - startTime;
                    wasCacheHit = responseTime.TotalMilliseconds < 5; // Extremely fast responses are likely from cache
                }
                
                // Add header indicating cache status for debugging
                if (!context.Response.Headers.ContainsKey("X-Cache"))
                {
                    context.Response.Headers["X-Cache"] = wasCacheHit ? "HIT" : "MISS";
                }
                
                return Task.CompletedTask;
            });

            try
            {
                // Call the next middleware in the pipeline
                await _next(context);
                
                // Track the request for potential background refresh if it's a cacheable response
                if (_backgroundRefresher != null && 
                    context.Response.StatusCode == 200 && 
                    (context.Response.Headers.CacheControl.Count > 0 && context.Response.Headers.CacheControl[0].Contains("max-age=") || 
                     context.Response.Headers.ContainsKey("X-Cache")))
                {
                    DateTime? expires = null;
                    
                    // Try to parse cache expiration from headers
                    if (context.Response.Headers.TryGetValue("Expires", out var expiresHeader) && expiresHeader.Count > 0)
                    {
                        if (DateTime.TryParse(expiresHeader[0], out var parsedExpires))
                        {
                            expires = parsedExpires;
                        }
                    }
                    else if (context.Response.Headers.TryGetValue("Cache-Control", out var cacheControl) && cacheControl.Count > 0)
                    {
                        var maxAgeMatch = System.Text.RegularExpressions.Regex.Match(
                            cacheControl[0], @"max-age=(\d+)");
                        
                        if (maxAgeMatch.Success && int.TryParse(maxAgeMatch.Groups[1].Value, out var maxAgeSeconds))
                        {
                            expires = DateTime.UtcNow.AddSeconds(maxAgeSeconds);
                        }
                    }
                    
                    _backgroundRefresher.TrackResourceAccess(
                        context.Request.Path.Value,
                        context.Request.QueryString.Value,
                        expires);
                }
            }
            finally
            {
                // Record cache metrics if the response was cacheable
                if (context.Response.StatusCode == 200 && 
                    !context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                    !context.Request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) &&
                    !context.Request.Method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    var cachePrefix = "output:unknown";
                    
                    if (path != null)
                    {
                        if (path.StartsWith("/workout"))
                        {
                            cachePrefix = "output:workouts";
                        }
                        else if (path.StartsWith("/reports"))
                        {
                            cachePrefix = "output:reports";
                        }
                        else if (path == "/" || path == "/index" || path == "/home" || path == "/home/index")
                        {
                            cachePrefix = "output:home";
                        }
                    }
                    
                    try
                    {
                        if (wasCacheHit)
                        {
                            CacheMetrics.RecordHit(cachePrefix);
                        }
                        else
                        {
                            CacheMetrics.RecordMiss(cachePrefix);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to record cache metrics");
                    }
                }
            }
        }
    }
    
    // Extension methods for the OutputCacheMetricsMiddleware
    public static class OutputCacheMetricsMiddlewareExtensions
    {
        /// <summary>
        /// Adds the output cache metrics middleware to the application pipeline
        /// </summary>
        public static IApplicationBuilder UseOutputCacheMetrics(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<OutputCacheMetricsMiddleware>();
        }
    }
}