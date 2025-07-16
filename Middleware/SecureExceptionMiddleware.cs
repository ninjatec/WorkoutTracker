using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using System.Net;
using System.Text.Json;

namespace WorkoutTrackerWeb.Middleware
{
    /// <summary>
    /// Middleware to handle exceptions and ensure no sensitive information is exposed to users
    /// while maintaining comprehensive security logging for DAST compliance
    /// </summary>
    public class SecureExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<SecureExceptionMiddleware> _logger;

        public SecureExceptionMiddleware(RequestDelegate next, IWebHostEnvironment env, ILogger<SecureExceptionMiddleware> logger)
        {
            _next = next;
            _env = env;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var requestId = context.TraceIdentifier;
            var requestPath = context.Request.Path.ToString();
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var referer = context.Request.Headers["Referer"].ToString();
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var userId = context.User?.Identity?.Name ?? "Anonymous";

            // Comprehensive security logging for monitoring and incident response
            Log.Error(exception, 
                "SECURITY ALERT - Unhandled Exception: RequestId={RequestId}, " +
                "Path={Path}, User={User}, IP={IP}, UserAgent={UserAgent}, " +
                "Referer={Referer}, ExceptionType={ExceptionType}",
                requestId, requestPath, userId, ipAddress, userAgent, referer, exception.GetType().Name);

            // Additional structured logging for security monitoring
            _logger.LogError(exception,
                "Exception occurred. RequestId: {RequestId}, Path: {Path}, User: {User}",
                requestId, requestPath, userId);

            context.Response.Clear();
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // Ensure no sensitive information is exposed to the user
            // Always redirect to secure error pages, never return exception details
            if (!context.Response.HasStarted)
            {
                // For AJAX requests, return a clean JSON response
                if (IsAjaxRequest(context.Request))
                {
                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = new
                    {
                        error = "Internal Server Error",
                        message = "An error occurred while processing your request.",
                        requestId = requestId,
                        timestamp = DateTime.UtcNow
                    };

                    var jsonResponse = JsonSerializer.Serialize(errorResponse);
                    await context.Response.WriteAsync(jsonResponse);
                }
                else
                {
                    // For regular requests, redirect to error page
                    context.Response.Redirect("/Errors/Error?statusCode=500");
                }
            }
        }

        private static bool IsAjaxRequest(HttpRequest request)
        {
            return request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   request.Headers.Accept.ToString().Contains("application/json") ||
                   request.Path.StartsWithSegments("/api");
        }
    }

    /// <summary>
    /// Extension method to register the secure exception middleware
    /// </summary>
    public static class SecureExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecureExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecureExceptionMiddleware>();
        }
    }
}
