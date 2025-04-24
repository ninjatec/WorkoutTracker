using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Data;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Services;
using Microsoft.AspNetCore.Authorization;

namespace WorkoutTrackerWeb.Pages.Sessions
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;

        public CreateModel(
            WorkoutTrackerWebContext context,
            UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Ensure the user has a record in our system
            await _userService.GetOrCreateCurrentUserAsync();
            return Page();
        }

        [BindProperty]
        public Session Session { get; set; } = new Session();

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Get the current user ID only (not the User entity)
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (!currentUserId.HasValue)
            {
                return Challenge(); // Redirect to login if user not authenticated
            }

            // Only assign the user ID, not the whole User entity
            Session.UserId = currentUserId.Value;

            _context.Session.Add(Session);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
