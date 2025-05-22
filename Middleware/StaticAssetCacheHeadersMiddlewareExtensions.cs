using Microsoft.AspNetCore.Builder;

namespace WorkoutTrackerWeb.Middleware
{
    /// <summary>
    /// Extension methods to add the static asset cache headers middleware to the pipeline
    /// </summary>
    public static class StaticAssetCacheHeadersMiddlewareExtensions
    {
        /// <summary>
        /// Adds middleware to apply cache control headers to static assets
        /// </summary>
        public static IApplicationBuilder UseStaticAssetCacheHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<StaticAssetCacheHeadersMiddleware>();
        }
    }
}