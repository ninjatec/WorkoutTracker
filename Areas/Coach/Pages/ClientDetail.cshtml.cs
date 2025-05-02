using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Areas.Coach.Pages
{
    public class ClientDetailModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;

        public ClientDetailModel(WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public IList<WorkoutSession> WorkoutSessions { get; set; }

        public async Task<IActionResult> OnGetAsync(string clientUserId)
        {
            if (string.IsNullOrEmpty(clientUserId))
            {
                return NotFound();
            }

            // Get the client's user ID from our application User model
            var clientAppUser = await _context.User
                .FirstOrDefaultAsync(u => u.IdentityUserId == clientUserId);

            if (clientAppUser == null)
            {
                return NotFound();
            }

            // Now use the application User ID for queries
            var clientWorkoutSessions = await _context.WorkoutSessions
                .Where(ws => ws.UserId == clientAppUser.UserId)
                .OrderByDescending(ws => ws.StartDateTime)
                .Take(5)
                .ToListAsync();

            WorkoutSessions = clientWorkoutSessions;

            return Page();
        }
    }
}