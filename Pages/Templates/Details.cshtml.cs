using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OutputCaching;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Pages.Templates
{
    [Authorize]
    [OutputCache(Duration = 60, VaryByQueryKeys = new[] { "id" })]
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;

        public DetailsModel(WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public WorkoutTemplate WorkoutTemplate { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var workoutTemplate = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.ExerciseType)
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.TemplateSets)
                .ThenInclude(s => s.Settype)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == id);

            if (workoutTemplate == null)
            {
                return NotFound();
            }

            WorkoutTemplate = workoutTemplate;
            return Page();
        }
    }
}