using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services.Coaching;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Coaches
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminRole")]
    public class IndexModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ICoachingService _coachingService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            UserManager<AppUser> userManager,
            ICoachingService coachingService,
            ILogger<IndexModel> logger)
        {
            _userManager = userManager;
            _coachingService = coachingService;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public string UserEmail { get; set; }
        
        public IList<AppUser> Coaches { get; set; } = new List<AppUser>();
        public SelectList UserSelectList { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Get all coaches (users in Coach role)
            var usersInCoachRole = await _userManager.GetUsersInRoleAsync("Coach");
            Coaches = usersInCoachRole.ToList();

            // Get regular users (not in Coach role) for promotion dropdown
            var allUsers = await _userManager.Users.ToListAsync();
            var regularUsers = new List<AppUser>();
            
            foreach (var user in allUsers)
            {
                if (!await _userManager.IsInRoleAsync(user, "Coach") &&
                    !await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    regularUsers.Add(user);
                }
            }
            
            UserSelectList = new SelectList(regularUsers, "Email", "Email");

            return Page();
        }

        public async Task<IActionResult> OnPostPromoteUserAsync()
        {
            if (string.IsNullOrEmpty(UserEmail))
            {
                StatusMessage = "Error: No user selected.";
                return RedirectToPage();
            }

            var user = await _userManager.FindByEmailAsync(UserEmail);
            if (user == null)
            {
                StatusMessage = "Error: User not found.";
                return RedirectToPage();
            }

            // Attempt to promote the user to Coach
            var result = await _coachingService.PromoteUserToCoachAsync(user.Id);
            if (result)
            {
                StatusMessage = $"Success: {user.Email} has been promoted to Coach.";
                _logger.LogInformation("Admin {AdminUser} promoted user {UserEmail} to Coach", User.Identity.Name, user.Email);
            }
            else
            {
                StatusMessage = $"Error: Failed to promote {user.Email} to Coach.";
                _logger.LogWarning("Admin {AdminUser} failed to promote user {UserEmail} to Coach", User.Identity.Name, user.Email);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDemoteCoachAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                StatusMessage = "Error: No coach ID provided.";
                return RedirectToPage();
            }

            var coach = await _userManager.FindByIdAsync(userId);
            if (coach == null)
            {
                StatusMessage = "Error: Coach not found.";
                return RedirectToPage();
            }

            // Attempt to demote the coach to regular user
            var result = await _coachingService.DemoteCoachToUserAsync(coach.Id);
            if (result)
            {
                StatusMessage = $"Success: {coach.Email} has been demoted to regular user.";
                _logger.LogInformation("Admin {AdminUser} demoted coach {UserEmail} to regular user", User.Identity.Name, coach.Email);
            }
            else
            {
                StatusMessage = $"Error: Failed to demote {coach.Email}.";
                _logger.LogWarning("Admin {AdminUser} failed to demote coach {UserEmail}", User.Identity.Name, coach.Email);
            }

            return RedirectToPage();
        }
    }
}