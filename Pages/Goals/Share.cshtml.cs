using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Pages.Goals
{
    // Allow caching and anonymous access since this is a shareable public page
    [OutputCache(Duration = 3600)]
    [AllowAnonymous]
    public class ShareModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<ShareModel> _logger;

        public ShareModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ILogger<ShareModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public ClientGoal Goal { get; set; }
        public string UserName { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                // Find the goal
                Goal = await _context.ClientGoals
                    .FirstOrDefaultAsync(g => g.Id == id && (g.IsCompleted || g.CompletedDate.HasValue));

                if (Goal == null)
                {
                    _logger.LogWarning("Shared goal with ID {GoalId} not found or not completed", id);
                    return Page();
                }

                // Get the user who created the goal
                var user = await _userManager.FindByIdAsync(Goal.UserId);
                if (user != null)
                {
                    UserName = !string.IsNullOrEmpty(user.UserName) ? user.UserName : "a Workout Tracker user";
                }
                else
                {
                    UserName = "a Workout Tracker user";
                }

                _logger.LogInformation("Shared goal {GoalId} viewed", id);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading shared goal {GoalId}", id);
                return Page();
            }
        }
    }
}