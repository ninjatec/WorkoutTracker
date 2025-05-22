using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;

namespace WorkoutTrackerWeb.Pages.Errors
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class NotFoundModel : PageModel
    {
        public void OnGet()
        {
            // Log the 404 event for security monitoring
            Log.Warning("404 Not Found: {Path}", HttpContext.Request.Path);
        }
    }
}