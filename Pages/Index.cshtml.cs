using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Authorization;

namespace WorkoutTrackerWeb.Pages;

// Add conditional output caching - don't cache for unauthenticated users
[OutputCache(PolicyName = "HomePagePolicy", NoStore = false)]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        // If user is not authenticated, redirect to login page
        if (!User.Identity.IsAuthenticated)
        {
            // Add cache control headers to prevent caching this redirect
            Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            
            // Log the redirect to help with debugging
            _logger.LogDebug("Redirecting unauthenticated user from home page to login page");
            
            // Include return URL for better user experience
            return RedirectToPage("/Account/Login", new { area = "Identity", ReturnUrl = "/" });
        }

        // Mark that this is a successful page load
        _logger.LogDebug("Home page successfully loaded for authenticated user");
        return Page();
    }
}
