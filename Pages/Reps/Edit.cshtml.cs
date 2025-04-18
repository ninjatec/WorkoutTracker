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
                .Include(r => r.Set)
                    .ThenInclude(s => s.Session)
                .Include(r => r.Set)
                    .ThenInclude(s => s.ExerciseType)
                .Include(r => r.Set)
                    .ThenInclude(s => s.Settype)
                .FirstOrDefaultAsync(m => m.RepId == id);

            if (rep == null)
            {
                return NotFound();
            }
            Rep = rep;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Get the existing rep to preserve its relationships
            var repToUpdate = await _context.Rep
                .Include(r => r.Set)
                .FirstOrDefaultAsync(r => r.RepId == Rep.RepId);

            if (repToUpdate == null)
            {
                return NotFound();
            }

            // Update only the editable properties while preserving the relationship
            repToUpdate.weight = Rep.weight;
            repToUpdate.repnumber = Rep.repnumber;
            repToUpdate.success = Rep.success;
            // SetsSetId is preserved from the original record

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
