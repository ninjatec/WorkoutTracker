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

namespace WorkoutTrackerWeb.Pages.Excercises
{
    public class EditModel : SessionNamePageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public EditModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Excercise Excercise { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var excercise = await _context.Excercise
                .Include(e => e.Session)
                .FirstOrDefaultAsync(m => m.ExcerciseId == id);

            if (excercise == null)
            {
                return NotFound();
            }

            Excercise = excercise;
            PopulateSessionNameDropDownList(_context, Excercise.SessionId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (!ModelState.IsValid)
            {
                PopulateSessionNameDropDownList(_context, Excercise.SessionId);
                return Page();
            }

            var exerciseToUpdate = await _context.Excercise.FindAsync(id);

            if (exerciseToUpdate == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync<Excercise>(
                exerciseToUpdate,
                "Excercise",
                e => e.ExcerciseName, e => e.SessionId))
            {
                await _context.SaveChangesAsync();
                return RedirectToPage("./Index");
            }

            PopulateSessionNameDropDownList(_context, exerciseToUpdate.SessionId);
            return Page();
        }

        private bool ExcerciseExists(int id)
        {
            return _context.Excercise.Any(e => e.ExcerciseId == id);
        }
    }
}
