using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;
using System;
using System.Diagnostics;

namespace WorkoutTrackerWeb.Pages.Errors
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ServerErrorModel : PageModel
    {
        public string RequestId { get; set; }

        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            var exception = exceptionHandlerPathFeature?.Error;

            if (exception != null)
            {
                // Log the error with full details for security monitoring and debugging
                // but don't expose any sensitive information to the user
                Log.Error(exception, "500 Server Error - Request ID: {RequestId}, Path: {Path}, User: {User}", 
                    RequestId, 
                    exceptionHandlerPathFeature?.Path ?? "Unknown", 
                    HttpContext.User?.Identity?.Name ?? "Anonymous");
            }
            else
            {
                // Log the error even without exception details
                Log.Warning("500 Server Error - Request ID: {RequestId}, Path: {Path}", 
                    RequestId, HttpContext.Request.Path);
            }
        }
    }
}