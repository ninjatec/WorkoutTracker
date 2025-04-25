using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace WorkoutTrackerWeb.Pages.Shared
{
    [OutputCache(PolicyName = "StaticContent")]
    public class AccessDeniedModel : PageModel
    {
        public void OnGet()
        {
            // Simple page model - no additional functionality needed
        }
    }
}