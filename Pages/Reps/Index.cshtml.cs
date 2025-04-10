using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;

namespace WorkoutTrackerWeb.Pages.Reps
{
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public IndexModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public IList<Rep> Rep { get;set; } = default!;
        public IList<Set> AvailableSets { get; set; } = default!;

        public async Task OnGetAsync()
        {
            Rep = await _context.Rep
                .Include(r => r.Sets)
                    .ThenInclude(s => s.Session)
                .Include(r => r.Sets)
                    .ThenInclude(s => s.ExerciseType)
                .ToListAsync();

            AvailableSets = await _context.Set
                .Include(s => s.ExerciseType)
                .Include(s => s.Session)
                .OrderBy(s => s.SetId)
                .ToListAsync();
        }
    }
}
