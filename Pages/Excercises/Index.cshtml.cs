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

namespace WorkoutTrackerWeb.Pages.Excercises
{
    [Authorize]
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

        public IList<Excercise> Excercise { get;set; } = default!;

        public async Task OnGetAsync()
        {
            // Get current user ID
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            
            if (currentUserId != null)
            {
                // Get only exercises from sessions owned by the current user
                Excercise = await _context.Excercise
                    .Include(e => e.Session)
                    .ThenInclude(s => s.User)
                    .Where(e => e.Session.UserId == currentUserId)
                    .ToListAsync();
            }
            else
            {
                // Fallback to empty list if not authenticated
                Excercise = new List<Excercise>();
            }
        }
    }
}
