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
    public class DeleteModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;

        public DeleteModel(
            WorkoutTrackerWebContext context,
            UserService userService)
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

            // Get current user
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }

            // Get session with ownership check - use AsNoTracking since we're just reading
            var session = await _context.Session
                .AsNoTracking()
                .Include(s => s.User)
                .FirstOrDefaultAsync(m => m.SessionId == id && m.UserId == currentUserId);

            if (session == null)
            {
                return NotFound();
            }
            
            Session = session;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Get current user
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }

            // Use a specific select query first to verify the session exists and belongs to the user
            bool exists = await _context.Session
                .AnyAsync(s => s.SessionId == id && s.UserId == currentUserId);
                
            if (!exists)
            {
                return NotFound();
            }

            // Using direct removal approach instead of loading the full entity
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Fetch just the session reference for deletion without loading related entities
                var session = new Session { SessionId = id.Value };
                _context.Entry(session).State = EntityState.Deleted;
                
                // Execute the delete - cascading delete will handle related entities
                await _context.SaveChangesAsync();
                
                await transaction.CommitAsync();
                
                // Clear the context after a large delete operation
                _context.ChangeTracker.Clear();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }

            return RedirectToPage("./Index");
        }
    }
}
