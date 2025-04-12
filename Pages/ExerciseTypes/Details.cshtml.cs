using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;

namespace WorkoutTrackerWeb.Pages.ExerciseTypes
{
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public DetailsModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

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
    }
}