using Microsoft.AspNetCore.Builder;

namespace WorkoutTrackerWeb.Middleware
{
    /// <summary>
    /// Extension methods for the CompressionAnalyticsMiddleware
    /// </summary>
    public static class CompressionAnalyticsMiddlewareExtensions
    {
        /// <summary>
        /// Adds middleware to track compression analytics for response compression
        /// </summary>
        public static IApplicationBuilder UseCompressionAnalytics(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CompressionAnalyticsMiddleware>();
        }
    }
}