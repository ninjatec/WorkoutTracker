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
        public WorkoutSession WorkoutSession { get; set; } = new WorkoutSession
        {
            StartDateTime = DateTime.Now,
            Status = "Created",
            IsCompleted = false
        };

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Get the current user ID
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (!currentUserId.HasValue)
            {
                return Challenge(); // Redirect to login if user not authenticated
            }

            // Set up the workout session
            WorkoutSession.UserId = currentUserId.Value;
            
            // Set initial properties if not already set
            if (WorkoutSession.StartDateTime == default)
            {
                WorkoutSession.StartDateTime = DateTime.Now;
            }
            if (string.IsNullOrEmpty(WorkoutSession.Status))
            {
                WorkoutSession.Status = "Created";
            }

            _context.WorkoutSessions.Add(WorkoutSession);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
