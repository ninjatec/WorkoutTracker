using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Services;
using Microsoft.AspNetCore.Authorization;

namespace WorkoutTrackerWeb.Pages.Sessions
{
    [Authorize] // Require authentication
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;

        public IndexModel(
            WorkoutTrackerWebContext context,
            UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public IList<Session> Session { get;set; } = default!;

        public async Task OnGetAsync()
        {
            // Get the current user's ID
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            
            if (currentUserId != null)
            {
                // Get only sessions belonging to the current user
                Session = await _context.Session
                    .Include(s => s.User)
                    .Where(s => s.UserId == currentUserId)
                    .ToListAsync();
            }
            else
            {
                // Fallback if no current user (shouldn't happen with [Authorize] attribute)
                Session = new List<Session>();
            }
        }
    }
}
