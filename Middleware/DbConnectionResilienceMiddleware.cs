using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace WorkoutTrackerWeb.Middleware
{
    /// <summary>
    /// Middleware that handles database connection resilience by catching 
    /// database connection errors and providing graceful degradation.
    /// </summary>
    public class DbConnectionResilienceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DbConnectionResilienceMiddleware> _logger;

        public DbConnectionResilienceMiddleware(RequestDelegate next, ILogger<DbConnectionResilienceMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (SqlException ex) when (IsTransientError(ex))
            {
                _logger.LogWarning(ex, "Transient SQL exception caught - connection will be retried");
                
                // For API requests, return a meaningful response
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"status\":\"error\",\"message\":\"Database service temporarily unavailable. Please retry your request.\"}");
                    return;
                }
                
                // For regular requests, redirect to an error page
                if (!context.Response.HasStarted)
                {
                    context.Items["DatabaseConnectionError"] = true;
                    context.Response.Redirect("/Errors/DatabaseError");
                }
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && IsTransientError(sqlEx))
            {
                _logger.LogWarning(ex, "Transient database update exception caught");
                
                // For API requests, return a meaningful response
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"status\":\"error\",\"message\":\"Database service temporarily unavailable. Please retry your request.\"}");
                    return;
                }
                
                // For regular requests, redirect to an error page
                if (!context.Response.HasStarted)
                {
                    context.Items["DatabaseConnectionError"] = true;
                    context.Response.Redirect("/Errors/DatabaseError");
                }
            }
        }
        
        // Check if the SQL exception is a transient error that can be retried
        private bool IsTransientError(SqlException ex)
        {
            // SQL error codes that are considered transient:
            // 4060: Cannot open database
            // 40197: Error processing request. Retry request.
            // 40501: The service is currently busy
            // 40613: Database is currently unavailable
            // 49918: Cannot process request. Not enough resources
            // 4221: Login failed due to timeout
            // 1205: Deadlock victim
            // 233: Connection terminated
            // 64: SQL Server terminated
            // -2: Timeout
            int[] transientErrorCodes = { 4060, 40197, 40501, 40613, 49918, 4221, 1205, 233, 64, -2 };
            
            return Array.IndexOf(transientErrorCodes, ex.Number) >= 0;
        }
    }
    
    // Extension method for middleware registration
    public static class DbConnectionResilienceMiddlewareExtensions
    {
        public static IApplicationBuilder UseDbConnectionResilience(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DbConnectionResilienceMiddleware>();
        }
    }
}