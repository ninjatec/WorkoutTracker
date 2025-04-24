using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Data;

namespace WorkoutTrackerWeb.Pages.Sets
{
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWeb.Data.WorkoutTrackerWebContext _context;

        public DetailsModel(WorkoutTrackerWeb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public Set Set { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var set = await _context.Set
                .AsNoTracking() // Add AsNoTracking for read-only data
                .Include(s => s.Session)
                .Include(s => s.ExerciseType)
                .Include(s => s.Settype)
                .Include(s => s.Reps.OrderBy(r => r.repnumber))
                .FirstOrDefaultAsync(m => m.SetId == id);
                
            if (set is not null)
            {
                Set = set;
                return Page();
            }

            return NotFound();
        }
    }
}
