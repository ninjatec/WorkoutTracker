using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Data;

namespace WorkoutTrackerWeb.Pages.ExerciseTypes
{
    public class DeleteModel : PageModel
    {
        private readonly WorkoutTrackerWeb.Data.WorkoutTrackerWebContext _context;

        public DeleteModel(WorkoutTrackerWeb.Data.WorkoutTrackerWebContext context)
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
            else 
            {
                ExerciseType = exercisetype;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null || _context.ExerciseType == null)
            {
                return NotFound();
            }
            var exercisetype = await _context.ExerciseType.FindAsync(id);

            if (exercisetype != null)
            {
                ExerciseType = exercisetype;
                _context.ExerciseType.Remove(ExerciseType);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}