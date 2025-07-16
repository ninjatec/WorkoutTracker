using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;
using System.Diagnostics;

namespace WorkoutTrackerWeb.Pages.Errors
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class NotFoundModel : PageModel
    {
        public string RequestId { get; set; }

        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            
            // Log 404 errors for security monitoring and analytics
            Log.Information("404 Not Found - Request ID: {RequestId}, Path: {Path}, Referer: {Referer}, UserAgent: {UserAgent}", 
                RequestId, 
                HttpContext.Request.Path,
                HttpContext.Request.Headers["Referer"].ToString(),
                HttpContext.Request.Headers["User-Agent"].ToString());
        }
    }
}