using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace WorkoutTrackerWeb.Pages.Shared
{
    [OutputCache(PolicyName = "StaticContent")]
    public class InvalidTokenModel : PageModel
    {
        public void OnGet()
        {
            // Simple page model - no additional functionality needed
        }
    }
}