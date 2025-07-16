using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;
using System.Diagnostics;

namespace WorkoutTrackerWeb.Pages.Errors
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
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
                
                // Log the error for security monitoring but don't expose sensitive details
                Log.Warning("HTTP {StatusCode} Error: {Path} - Request ID: {RequestId}", 
                    statusCode, HttpContext.Request.Path, RequestId);
            }
            else
            {
                var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
                var exception = exceptionHandlerPathFeature?.Error;
                
                if (exception != null)
                {
                    // Log the error with full details for debugging but don't expose to user
                    Log.Error(exception, "Unhandled Exception: {Path} - Request ID: {RequestId}", 
                        HttpContext.Request.Path, RequestId);
                    
                    // Set safe error information for user display
                    ErrorCode = "500";
                    ErrorMessage = "Internal Server Error";
                }
            }
        }

        private string GetErrorMessage(int statusCode)
        {
            return statusCode switch
            {
                400 => "Bad Request - The server cannot process your request.",
                401 => "Unauthorized - You need to log in to access this resource.",
                403 => "Forbidden - You don't have permission to access this resource.",
                404 => "Not Found - The page you're looking for doesn't exist.",
                408 => "Request Timeout - Your request took too long to process.",
                429 => "Too Many Requests - Please try again later.",
                500 => "Internal Server Error - Something went wrong on our servers.",
                501 => "Not Implemented - This feature is not available.",
                502 => "Bad Gateway - The server received an invalid response.",
                503 => "Service Unavailable - The service is temporarily unavailable.",
                _ => "An error occurred while processing your request."
            };
        }
    }
}