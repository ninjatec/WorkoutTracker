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
    public class CreateModel : SessionNamePageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;

        public CreateModel(
            WorkoutTrackerWebContext context,
            UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Populate dropdown with only current user's sessions
            await PopulateSessionNameDropDownListAsync(_context, _userService);
            return Page();
        }

        [BindProperty]
        public Excercise Excercise { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await PopulateSessionNameDropDownListAsync(_context, _userService);
                return Page();
            }

            // Verify the selected session belongs to the current user
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            var sessionBelongsToUser = await _context.Session
                .AnyAsync(s => s.SessionId == Excercise.SessionId && s.UserId == currentUserId);
                
            if (!sessionBelongsToUser)
            {
                ModelState.AddModelError(string.Empty, "You can only add exercises to your own sessions.");
                await PopulateSessionNameDropDownListAsync(_context, _userService);
                return Page();
            }

            _context.Excercise.Add(Excercise);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
    }
}
