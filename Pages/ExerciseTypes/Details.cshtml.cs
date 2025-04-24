using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.ExerciseTypes
{
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ExerciseTypeService _exerciseService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            WorkoutTrackerWebContext context,
            ExerciseTypeService exerciseService,
            ILogger<DetailsModel> logger)
        {
            _context = context;
            _exerciseService = exerciseService;
            _logger = logger;
        }

        public ExerciseType ExerciseType { get; set; } = default!;
        
        public List<ExerciseType> RelatedExercises { get; set; } = new List<ExerciseType>();
        
        [TempData]
        public string StatusMessage { get; set; }

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
            
            // Get related exercises that target the same muscle group, if available
            if (!string.IsNullOrEmpty(exercisetype.Muscle))
            {
                RelatedExercises = await _context.ExerciseType
                    .Where(e => e.Muscle == exercisetype.Muscle && e.ExerciseTypeId != exercisetype.ExerciseTypeId)
                    .OrderBy(e => e.Name)
                    .Take(5) // Limit to 5 related exercises
                    .ToListAsync();
            }
            
            return Page();
        }
        
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            
            var exercise = await _exerciseService.EnrichExistingExerciseFromApiAsync(id.Value);
            
            if (exercise == null)
            {
                StatusMessage = "Error: Could not find matching exercise data in API.";
                return RedirectToPage("./Details", new { id });
            }
            
            StatusMessage = "Exercise data has been enriched from the API.";
            return RedirectToPage("./Details", new { id });
        }
    }
}