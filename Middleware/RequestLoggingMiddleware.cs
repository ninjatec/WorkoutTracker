using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;

namespace WorkoutTrackerWeb.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Serilog.ILogger _logger;
        private readonly bool _isProduction;

        public RequestLoggingMiddleware(RequestDelegate next, Serilog.ILogger logger, bool isProduction)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isProduction = isProduction;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Don't log health checks and metrics endpoints to avoid flooding logs
            string path = context.Request.Path.ToString().ToLower();
            if (path.StartsWith("/health") || path.StartsWith("/metrics"))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            
            // Capture request details
            var request = context.Request;
            var userAgent = request.Headers.ContainsKey("User-Agent") ? request.Headers["User-Agent"].ToString() : "unknown";
            var referer = request.Headers.ContainsKey("Referer") ? request.Headers["Referer"].ToString() : "none";
            
            // Extract IP address considering proxies
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            if (request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ip = request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
            }

            try
            {
                // Continue with the request
                await _next(context);
                stopwatch.Stop();

                // Only log in production or for non-successful status codes
                if (_isProduction || context.Response.StatusCode >= 400)
                {
                    // Add properties to the log context
                    using (LogContext.PushProperty("RequestMethod", request.Method))
                    using (LogContext.PushProperty("RequestPath", request.Path))
                    using (LogContext.PushProperty("StatusCode", context.Response.StatusCode))
                    using (LogContext.PushProperty("ClientIP", ip))
                    using (LogContext.PushProperty("UserAgent", userAgent))
                    using (LogContext.PushProperty("Referer", referer))
                    using (LogContext.PushProperty("RequestTime", stopwatch.ElapsedMilliseconds))
                    {
                        var level = context.Response.StatusCode >= 500 ? Serilog.Events.LogEventLevel.Error :
                                   context.Response.StatusCode >= 400 ? Serilog.Events.LogEventLevel.Warning :
                                   Serilog.Events.LogEventLevel.Information;

                        _logger.Write(level, "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {RequestTime}ms");
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                // Log exception with request context
                using (LogContext.PushProperty("RequestMethod", request.Method))
                using (LogContext.PushProperty("RequestPath", request.Path))
                using (LogContext.PushProperty("ClientIP", ip))
                using (LogContext.PushProperty("UserAgent", userAgent))
                using (LogContext.PushProperty("RequestTime", stopwatch.ElapsedMilliseconds))
                {
                    _logger.Error(ex, "Request failed: {RequestMethod} {RequestPath}");
                }
                
                // Re-throw to let the global exception handler deal with it
                throw;
            }
        }
    }

    // Extension method to add request logging middleware
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder, bool isProduction)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>(Log.Logger, isProduction);
        }
    }
}