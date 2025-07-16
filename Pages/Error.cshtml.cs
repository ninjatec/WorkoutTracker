using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;

namespace WorkoutTrackerWeb.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    private readonly ILogger<ErrorModel> _logger;

    public ErrorModel(ILogger<ErrorModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        
        // Get exception details for logging but don't expose to user
        var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;
        
        if (exception != null)
        {
            // Log the detailed error for development/debugging but don't expose to user
            _logger.LogError(exception, "Unhandled exception occurred. Request ID: {RequestId}, Path: {Path}", 
                RequestId, exceptionHandlerPathFeature?.Path);
            
            // Additional security logging for DAST compliance
            Log.Error(exception, "Security Alert - Exception Handler Triggered: {RequestId} on {Path}", 
                RequestId, exceptionHandlerPathFeature?.Path ?? "Unknown");
        }
    }
}

