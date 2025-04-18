using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;
using System.Diagnostics;

namespace WorkoutTrackerWeb.Pages.Errors
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ErrorModel : PageModel
    {
        public string ErrorCode { get; set; } = "Error";
        public string ErrorMessage { get; set; } = "An error occurred.";
        public string RequestId { get; set; }

        public void OnGet(int? statusCode = null)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            if (statusCode.HasValue)
            {
                ErrorCode = statusCode.ToString();
                ErrorMessage = GetErrorMessage(statusCode.Value);
                
                // Log the error for security monitoring
                Log.Warning("Error {StatusCode}: {Path}", statusCode, HttpContext.Request.Path);
            }
            else
            {
                var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
                var exception = exceptionHandlerPathFeature?.Error;
                
                // Log the error with exception details
                Log.Error(exception, "Unhandled Exception: {Path}", HttpContext.Request.Path);
            }
        }

        private string GetErrorMessage(int statusCode)
        {
            return statusCode switch
            {
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                408 => "Request Timeout",
                429 => "Too Many Requests",
                500 => "Server Error",
                501 => "Not Implemented",
                502 => "Bad Gateway",
                503 => "Service Unavailable",
                _ => "An error occurred"
            };
        }
    }
}