using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Services;
using Microsoft.AspNetCore.Authorization;

namespace WorkoutTrackerWeb.Pages.Excercises
{
    [Authorize]
    public class EditModel : SessionNamePageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;

        public EditModel(
            WorkoutTrackerWebContext context,
            UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        [BindProperty]
        public Excercise Excercise { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Get current user ID
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }

            // Get exercise with ownership check via Session
            var exercise = await _context.Excercise
                .Include(e => e.Session)
                .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(m => 
                    m.ExcerciseId == id && 
                    m.Session.UserId == currentUserId);

            if (exercise == null)
            {
                return NotFound();
            }

            Excercise = exercise;
            
            // Only show sessions belonging to current user
            await PopulateSessionNameDropDownListAsync(_context, _userService, Excercise.SessionId);
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (!ModelState.IsValid)
            {
                await PopulateSessionNameDropDownListAsync(_context, _userService, Excercise.SessionId);
                return Page();
            }

            // Get current user ID
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }

            // Verify the selected session belongs to the current user
            var sessionBelongsToUser = await _context.Session
                .AnyAsync(s => s.SessionId == Excercise.SessionId && s.UserId == currentUserId);

            if (!sessionBelongsToUser)
            {
                ModelState.AddModelError(string.Empty, "You can only use your own sessions.");
                await PopulateSessionNameDropDownListAsync(_context, _userService, Excercise.SessionId);
                return Page();
            }

            // Get the exercise with ownership check
            var exerciseToUpdate = await _context.Excercise
                .Include(e => e.Session)
                .FirstOrDefaultAsync(e => 
                    e.ExcerciseId == id && 
                    e.Session.UserId == currentUserId);

            if (exerciseToUpdate == null)
            {
                return NotFound();
            }

            // Update only allowed fields
            exerciseToUpdate.ExcerciseName = Excercise.ExcerciseName;
            exerciseToUpdate.SessionId = Excercise.SessionId;
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExcerciseExists(Excercise.ExcerciseId))
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

        private bool ExcerciseExists(int id)
        {
            return _context.Excercise.Any(e => e.ExcerciseId == id);
        }
    }
}
