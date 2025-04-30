using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Services;
using Microsoft.AspNetCore.Authorization;

namespace WorkoutTrackerWeb.Pages.Sessions
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;

        public EditModel(WorkoutTrackerWebContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        [BindProperty]
        public Session Session { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Get the current user
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }

            // Get the session with ownership check
            var session = await _context.Session
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.SessionId == id && m.UserId == currentUserId);

            if (session == null)
            {
                return NotFound();
            }
            
            Session = session;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Get the current user
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }

            // Verify ownership
            var sessionToUpdate = await _context.Session
                .FirstOrDefaultAsync(s => s.SessionId == Session.SessionId && s.UserId == currentUserId);

            if (sessionToUpdate == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Ensure UserId isn't changed
            Session.UserId = currentUserId.Value;

            try
            {
                // Update only allowed fields
                sessionToUpdate.Name = Session.Name;
                sessionToUpdate.datetime = Session.datetime;
                sessionToUpdate.endtime = Session.endtime;
                // UserId is preserved from the original record
                
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SessionExists(Session.SessionId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool SessionExists(int id)
        {
            return _context.Session.Any(e => e.SessionId == id);
        }
    }
}
