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

namespace WorkoutTrackerWeb.Pages.Reps
{
    public class EditModel : PageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public EditModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
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
                .Include(r => r.Sets)
                    .ThenInclude(s => s.Exercise)
                        .ThenInclude(e => e.Session)
                .FirstOrDefaultAsync(m => m.RepId == id);

            if (rep == null)
            {
                return NotFound();
            }
            Rep = rep;
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(Rep).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RepExists(Rep.RepId))
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

        private bool RepExists(int id)
        {
            return _context.Rep.Any(e => e.RepId == id);
        }
    }
}
