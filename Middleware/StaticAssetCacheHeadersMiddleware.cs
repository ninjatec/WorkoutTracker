using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Middleware
{
    /// <summary>
    /// Middleware to add cache control headers to static assets
    /// </summary>
    public class StaticAssetCacheHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<StaticAssetCacheHeadersMiddleware> _logger;
        private readonly Dictionary<string, TimeSpan> _extensionCacheDurations;

        public StaticAssetCacheHeadersMiddleware(RequestDelegate next, ILogger<StaticAssetCacheHeadersMiddleware> logger)
        {
            _next = next;
            _logger = logger;

            _extensionCacheDurations = new Dictionary<string, TimeSpan>(StringComparer.OrdinalIgnoreCase)
            {
                // JavaScript files
                [".js"] = TimeSpan.FromDays(7),
                [".min.js"] = TimeSpan.FromDays(30),
                
                // CSS files
                [".css"] = TimeSpan.FromDays(7),
                [".min.css"] = TimeSpan.FromDays(30),
                
                // Images
                [".png"] = TimeSpan.FromDays(30),
                [".jpg"] = TimeSpan.FromDays(30),
                [".jpeg"] = TimeSpan.FromDays(30),
                [".gif"] = TimeSpan.FromDays(30),
                [".webp"] = TimeSpan.FromDays(30),
                [".svg"] = TimeSpan.FromDays(30),
                [".ico"] = TimeSpan.FromDays(30),
                
                // Fonts
                [".woff"] = TimeSpan.FromDays(365),
                [".woff2"] = TimeSpan.FromDays(365),
                [".ttf"] = TimeSpan.FromDays(365),
                [".eot"] = TimeSpan.FromDays(365),
                [".otf"] = TimeSpan.FromDays(365)
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Save the original body stream position
            var originalBodyStream = context.Response.Body;

            try
            {
                // Call the next middleware in the pipeline
                await _next(context);
                
                // Only process GET or HEAD requests for static files
                if ((context.Request.Method == HttpMethods.Get || context.Request.Method == HttpMethods.Head) &&
                    context.Response.StatusCode == StatusCodes.Status200OK)
                {
                    string path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
                    
                    if (path.StartsWith("/css/") || 
                        path.StartsWith("/js/") || 
                        path.StartsWith("/lib/") || 
                        path.StartsWith("/fonts/") || 
                        path.StartsWith("/images/"))
                    {
                        // Get file extension
                        string extension = System.IO.Path.GetExtension(path);
                        
                        // Apply cache headers if we have a recognized extension
                        if (!string.IsNullOrEmpty(extension) && _extensionCacheDurations.TryGetValue(extension, out var cacheDuration))
                        {
                            if (!context.Response.Headers.ContainsKey(HeaderNames.CacheControl))
                            {
                                context.Response.Headers[HeaderNames.CacheControl] = 
                                    new StringValues($"public, max-age={cacheDuration.TotalSeconds}");
                                
                                _logger.LogDebug("Applied cache-control header to {Path} with duration {Duration}", 
                                    context.Request.Path, cacheDuration);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StaticAssetCacheHeadersMiddleware");
                throw;
            }
        }
    }
}