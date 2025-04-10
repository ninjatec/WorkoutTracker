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
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerweb.Data.WorkoutTrackerWebContext _context;

        public IndexModel(WorkoutTrackerweb.Data.WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public IList<ExerciseType> ExerciseTypes { get;set; } = default!;

        public async Task OnGetAsync()
        {
            if (_context.ExerciseType != null)
            {
                ExerciseTypes = await _context.ExerciseType.OrderBy(e => e.Name).ToListAsync();
            }
        }
    }
}