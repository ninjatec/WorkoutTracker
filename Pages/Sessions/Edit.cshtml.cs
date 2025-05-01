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
                // Update fields
                sessionToUpdate.Name = WorkoutSession.Name;
                sessionToUpdate.StartDateTime = WorkoutSession.StartDateTime;
                sessionToUpdate.EndDateTime = WorkoutSession.EndDateTime;
                sessionToUpdate.Description = WorkoutSession.Description;
                
                // Update duration if end time is set
                if (sessionToUpdate.EndDateTime.HasValue)
                {
                    sessionToUpdate.Duration = (int)(sessionToUpdate.EndDateTime.Value - sessionToUpdate.StartDateTime).TotalMinutes;
                }
                
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WorkoutSessionExists(WorkoutSession.WorkoutSessionId))
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

        private bool WorkoutSessionExists(int id)
        {
            return _context.WorkoutSessions.Any(ws => ws.WorkoutSessionId == id);
        }
    }
}
