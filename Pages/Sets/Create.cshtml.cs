using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using WorkoutTrackerWeb.Pages.Reports;

namespace WorkoutTrackerWeb.Pages.Sets
{
    public class CreateModel : SetInputPageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;
        private readonly UserService _userService;
        private readonly IDistributedCache _cache;

        public CreateModel(
            WorkoutTrackerweb.Data.WorkoutTrackerWebContext context,
            UserService userService,
            IDistributedCache cache)
        {
            _context = context;
            _userService = userService;
            _cache = cache;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await PopulateDropDownListsAsync(_context, _userService);
            return Page();
        }

        [BindProperty]
        public Set Set { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropDownListsAsync(_context, _userService);
                return Page();
            }

            _context.Set.Add(Set);
            await _context.SaveChangesAsync();

            // Automatically create Rep records based on NumberReps
            if (Set.NumberReps > 0)
            {
                for (int i = 0; i < Set.NumberReps; i++)
                {
                    var rep = new Rep 
                    { 
                        SetsSetId = Set.SetId,
                        repnumber = i + 1,
                        weight = Set.Weight, // Use the Set's weight for each rep
                        success = true
                    };
                    _context.Rep.Add(rep);
                }
                await _context.SaveChangesAsync();
            }

            // Get user ID from the session to invalidate cache
            var userId = await GetUserIdFromSessionAsync();
            if (userId.HasValue)
            {
                // Invalidate the report cache since workout data has changed
                // Properly reference the Reports.IndexModel class
                Reports.IndexModel.InvalidateCache(_cache, userId.Value);
            }

            return RedirectToPage("./Index");
        }

        private async Task<int?> GetUserIdFromSessionAsync()
        {
            try
            {
                // Get the session that this set belongs to
                var session = await _context.Session
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SessionId == Set.SessionId);

                return session?.UserId;
            }
            catch (Exception)
            {
                // In case of error, we just don't invalidate the cache
                // This is not critical functionality
                return null;
            }
        }
    }
}
