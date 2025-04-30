using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;
using WorkoutTrackerWeb.Pages.Goals;

namespace WorkoutTrackerWeb.Pages;

// Add conditional output caching - don't cache for unauthenticated users
[OutputCache(PolicyName = "HomePagePolicy", NoStore = false)]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(
        ILogger<IndexModel> logger,
        UserManager<AppUser> userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }

    public IActionResult OnGet()
    {
        return Page();
    }
}
