using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace WorkoutTrackerWeb.Pages;

[OutputCache(PolicyName = "HomePagePolicy")]
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
            return RedirectToPage("/Account/Login");
        }

        return Page();
    }
}
