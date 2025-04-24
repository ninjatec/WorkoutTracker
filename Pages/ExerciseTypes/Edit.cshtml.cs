using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Data;

namespace WorkoutTrackerWeb.Pages.ExerciseTypes
{
    public class EditModel : PageModel
    {
        private readonly WorkoutTrackerWeb.Data.WorkoutTrackerWebContext _context;

        public EditModel(WorkoutTrackerWeb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ExerciseType ExerciseType { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.ExerciseType == null)
            {
                return NotFound();
            }

            var exercisetype = await _context.ExerciseType.FirstOrDefaultAsync(m => m.ExerciseTypeId == id);
            if (exercisetype == null)
            {
                return NotFound();
            }
            ExerciseType = exercisetype;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(ExerciseType).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExerciseTypeExists(ExerciseType.ExerciseTypeId))
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

        private bool ExerciseTypeExists(int id)
        {
            return (_context.ExerciseType?.Any(e => e.ExerciseTypeId == id)).GetValueOrDefault();
        }
    }
}