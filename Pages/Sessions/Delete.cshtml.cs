using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Pages.Sessions
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(
            WorkoutTrackerWebContext context,
            UserService userService,
            ILogger<DeleteModel> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        [BindProperty]
        public Session Session { get; set; } = default!;
        
        [TempData]
        public string ErrorMessage { get; set; }

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

            try
            {
                // First, find the session with all related entities to ensure it exists and belongs to user
                var session = await _context.Session
                    .Include(s => s.Sets)
                        .ThenInclude(s => s.Reps)
                    .FirstOrDefaultAsync(s => s.SessionId == id && s.UserId == currentUserId);
                    
                if (session == null)
                {
                    return NotFound();
                }
                
                // Get the execution strategy from the context
                var strategy = _context.Database.CreateExecutionStrategy();
                
                await strategy.ExecuteAsync(async () =>
                {
                    // Delete reps first to avoid foreign key constraint issues
                    foreach (var set in session.Sets ?? Enumerable.Empty<Set>())
                    {
                        if (set.Reps != null && set.Reps.Any())
                        {
                            _context.Rep.RemoveRange(set.Reps);
                        }
                    }
                    
                    // Now delete sets
                    if (session.Sets != null && session.Sets.Any())
                    {
                        _context.Set.RemoveRange(session.Sets);
                    }
                    
                    // Finally delete the session
                    _context.Session.Remove(session);
                    
                    // Save all changes
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Successfully deleted session {SessionId} for user {UserId}", 
                        id, currentUserId);
                    
                    // Clear the change tracker to release memory
                    _context.ChangeTracker.Clear();
                });
                
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete session {SessionId}", id);
                ErrorMessage = "An error occurred while deleting the session. Please try again.";
                
                // Re-load the session for display
                var session = await _context.Session
                    .AsNoTracking()
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(m => m.SessionId == id && m.UserId == currentUserId);
                
                if (session != null)
                {
                    Session = session;
                    return Page();
                }
                
                return RedirectToPage("./Index", new { error = "Failed to delete session" });
            }
        }
    }
}
