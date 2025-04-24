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
    public class DeleteModel : PageModel
    {
        private readonly WorkoutTrackerWeb.Data.WorkoutTrackerWebContext _context;

        public DeleteModel(WorkoutTrackerWeb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Set Set { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var set = await _context.Set
                .Include(s => s.Session)
                .Include(s => s.ExerciseType)
                .Include(s => s.Settype)
                .FirstOrDefaultAsync(m => m.SetId == id);

            if (set is not null)
            {
                Set = set;
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

            var set = await _context.Set.FindAsync(id);
            if (set != null)
            {
                Set = set;
                _context.Set.Remove(Set);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
