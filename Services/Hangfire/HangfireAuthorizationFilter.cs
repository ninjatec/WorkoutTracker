using Hangfire.Dashboard;
using System;

namespace WorkoutTrackerWeb.Services.Hangfire
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" &&
               (httpContext.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || 
                httpContext.Request.Host.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            
            return httpContext.User.Identity?.IsAuthenticated == true && 
                   httpContext.User.IsInRole("Admin");
        }
    }
}