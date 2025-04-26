using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WorkoutTrackerWeb.Pages
{
    public class OfflineModel : PageModel
    {
        public void OnGet()
        {
            // This page is intentionally simple as it needs to be cached and available offline
        }
    }
}