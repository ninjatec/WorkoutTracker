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

namespace WorkoutTrackerWeb.Pages.Sets
{
    public class EditModel : SetInputPageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public EditModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
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
                .Include(s => s.Settype)
                .Include(s => s.Exercise)
                .FirstOrDefaultAsync(m => m.SetId == id);

            if (set == null)
            {
                return NotFound();
            }

            Set = set;
            PopulateSettypeNameDropDownList(_context, Set.SettypeId);
            PopulateExerciseNameDropDownList(_context, Set.ExerciseId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (!ModelState.IsValid)
            {
                PopulateSettypeNameDropDownList(_context, Set.SettypeId);
                PopulateExerciseNameDropDownList(_context, Set.ExerciseId);
                return Page();
            }

            var setToUpdate = await _context.Set.FindAsync(id);

            if (setToUpdate == null)
            {
                return NotFound();
            }

            if (await TryUpdateModelAsync<Set>(
                setToUpdate,
                "Set",
                s => s.Description, s => s.Notes, s => s.SettypeId, s => s.ExerciseId))
            {
                await _context.SaveChangesAsync();
                return RedirectToPage("./Index");
            }

            PopulateSettypeNameDropDownList(_context, setToUpdate.SettypeId);
            PopulateExerciseNameDropDownList(_context, setToUpdate.ExerciseId);
            return Page();
        }

        private bool SetExists(int id)
        {
            return _context.Set.Any(e => e.SetId == id);
        }
    }
}
