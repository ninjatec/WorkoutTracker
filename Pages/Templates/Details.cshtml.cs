using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Hosting;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Pages.Templates
{
    [Authorize]
    [OutputCache(Duration = 300, VaryByQueryKeys = new[] { "id" })]
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IWebHostEnvironment _environment;

        public DetailsModel(WorkoutTrackerWebContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public WorkoutTemplate WorkoutTemplate { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Adjust cache duration for development environment
            if (_environment.IsDevelopment())
            {
                HttpContext.Response.Headers["X-Template-Detail-Cache"] = "Development-Extended";
            }

            var workoutTemplate = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises.OrderBy(e => e.SequenceNum))
                .ThenInclude(e => e.ExerciseType)
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.TemplateSets.OrderBy(s => s.SequenceNum))
                .ThenInclude(s => s.Settype)
                .AsNoTracking()
                .AsSplitQuery() // Split into multiple SQL queries for better performance
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