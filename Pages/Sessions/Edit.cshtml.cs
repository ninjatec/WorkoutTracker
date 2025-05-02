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
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Pages.Sessions
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;
        private readonly ILogger<EditModel> _logger;

        public EditModel(WorkoutTrackerWebContext context, UserService userService, ILogger<EditModel> logger)
        {
            _context = context;
            _userService = userService;
            _logger = logger;
        }

        [BindProperty]
        public WorkoutSession WorkoutSession { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }

            // Get the workout session with ownership check
            var workoutSession = await _context.WorkoutSessions
                .Include(ws => ws.User)
                .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == id && ws.UserId == currentUserId);

            if (workoutSession == null)
            {
                return NotFound();
            }
            
            WorkoutSession = workoutSession;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }

            // Verify ownership
            var sessionToUpdate = await _context.WorkoutSessions
                .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == WorkoutSession.WorkoutSessionId && ws.UserId == currentUserId);

            if (sessionToUpdate == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Ensure UserId isn't changed
            WorkoutSession.UserId = currentUserId.Value;

            try
            {
                // Create execution strategy for the DB context
                var strategy = _context.Database.CreateExecutionStrategy();
                
                await strategy.ExecuteAsync(async () =>
                {
                    // Detach current entity to avoid tracking conflicts
                    _context.Entry(sessionToUpdate).State = EntityState.Detached;
                    
                    // Update with the edited entity and mark as modified
                    _context.Attach(WorkoutSession);
                    _context.Entry(WorkoutSession).State = EntityState.Modified;
                    
                    // Update duration if end time is set
                    if (WorkoutSession.EndDateTime.HasValue)
                    {
                        WorkoutSession.Duration = (int)(WorkoutSession.EndDateTime.Value - WorkoutSession.StartDateTime).TotalMinutes;
                        
                        // Update status to "Completed" when end date is set
                        WorkoutSession.Status = "Completed";
                        WorkoutSession.CompletedDate = WorkoutSession.EndDateTime;
                    }
                    
                    // Execute the save within the strategy
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Session {SessionId} updated successfully", WorkoutSession.WorkoutSessionId);
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!WorkoutSessionExists(WorkoutSession.WorkoutSessionId))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error updating session {SessionId}", WorkoutSession.WorkoutSessionId);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating session {SessionId}", WorkoutSession.WorkoutSessionId);
                throw;
            }

            return RedirectToPage("./Index");
        }

        private bool WorkoutSessionExists(int id)
        {
            return _context.WorkoutSessions.Any(ws => ws.WorkoutSessionId == id);
        }
    }
}
