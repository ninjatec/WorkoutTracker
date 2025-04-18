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
    public class DeleteModel : PageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public DeleteModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Rep Rep { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rep = await _context.Rep
                .Include(r => r.Set)
                    .ThenInclude(s => s.Session)
                .Include(r => r.Set)
                    .ThenInclude(s => s.ExerciseType)
                .Include(r => r.Set)
                    .ThenInclude(s => s.Settype)
                .FirstOrDefaultAsync(m => m.RepId == id);

            if (rep is not null)
            {
                Rep = rep;

                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rep = await _context.Rep.FindAsync(id);
            if (rep != null)
            {
                Rep = rep;
                _context.Rep.Remove(Rep);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
