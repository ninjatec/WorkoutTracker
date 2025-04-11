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
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;

        public DetailsModel(WorkoutTrackerWebContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public Session Session { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Get the current user id
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge(); // Redirect to login if not authenticated
            }

            // Get the session with ownership check and include Sets and Reps
            var session = await _context.Session
                .Include(s => s.User)
                .Include(s => s.Sets)
                    .ThenInclude(set => set.ExerciseType)
                .Include(s => s.Sets)
                    .ThenInclude(set => set.Settype)
                .Include(s => s.Sets)
                    .ThenInclude(set => set.Reps.OrderBy(r => r.repnumber))
                .FirstOrDefaultAsync(m => m.SessionId == id && m.UserId == currentUserId);

            if (session == null)
            {
                return NotFound();
            }

            Session = session;
            return Page();
        }
    }
}
