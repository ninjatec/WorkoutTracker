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
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public DetailsModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public Rep Rep { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rep = await _context.Rep.FirstOrDefaultAsync(m => m.RepId == id);

            if (rep is not null)
            {
                Rep = rep;

                return Page();
            }

            return NotFound();
        }
    }
}
