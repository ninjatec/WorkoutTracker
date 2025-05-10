using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WorkoutTrackerWeb.Pages
{
    public class OfflineModel : PageModel
    {
        public void OnGet()
        {
            // This page is for clients who attempt to access the site without an internet connection
            // It's no longer designed to work offline as that functionality has been removed
        }
    }
}