using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace WorkoutTrackerWeb.Pages.BackgroundJobs
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Redirect directly to Hangfire dashboard with server-side redirect
            // This properly maintains the authentication context
            return Redirect("/hangfire");
        }
    }
}