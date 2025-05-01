using System;
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
        public WorkoutSession WorkoutSession { get; set; } = default!;
        
        [TempData]
        public string ErrorMessage { get; set; }

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

            // Get workout session with ownership check
            var workoutSession = await _context.WorkoutSessions
                .AsNoTracking()
                .Include(ws => ws.User)
                .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == id && ws.UserId == currentUserId);

            if (workoutSession == null)
            {
                return NotFound();
            }
            
            WorkoutSession = workoutSession;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
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

            try
            {
                // Find the workout session with all related entities
                var workoutSession = await _context.WorkoutSessions
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.WorkoutSets)
                    .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == id && ws.UserId == currentUserId);
                    
                if (workoutSession == null)
                {
                    return NotFound();
                }
                
                // Get the execution strategy from the context
                var strategy = _context.Database.CreateExecutionStrategy();
                
                await strategy.ExecuteAsync(async () =>
                {
                    // Delete workout sets first
                    foreach (var exercise in workoutSession.WorkoutExercises ?? Enumerable.Empty<WorkoutExercise>())
                    {
                        if (exercise.WorkoutSets != null && exercise.WorkoutSets.Any())
                        {
                            _context.WorkoutSets.RemoveRange(exercise.WorkoutSets);
                        }
                    }
                    
                    // Delete workout exercises
                    if (workoutSession.WorkoutExercises != null && workoutSession.WorkoutExercises.Any())
                    {
                        _context.WorkoutExercises.RemoveRange(workoutSession.WorkoutExercises);
                    }
                    
                    // Finally delete the workout session
                    _context.WorkoutSessions.Remove(workoutSession);
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Successfully deleted workout session {WorkoutSessionId} for user {UserId}", 
                        id, currentUserId);
                    
                    // Clear the change tracker
                    _context.ChangeTracker.Clear();
                });
                
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete workout session {WorkoutSessionId}", id);
                ErrorMessage = "An error occurred while deleting the workout session. Please try again.";
                
                // Re-load the session for display
                var workoutSession = await _context.WorkoutSessions
                    .AsNoTracking()
                    .Include(ws => ws.User)
                    .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == id && ws.UserId == currentUserId);
                
                if (workoutSession != null)
                {
                    WorkoutSession = workoutSession;
                    return Page();
                }
                
                return RedirectToPage("./Index", new { error = "Failed to delete workout session" });
            }
        }
    }
}
