using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services.Calculations;

namespace WorkoutTrackerWeb.Pages.Sessions
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IVolumeCalculationService _volumeCalculationService;
        private readonly ICalorieCalculationService _calorieCalculationService;

        public DetailsModel(
            WorkoutTrackerWebContext context,
            IVolumeCalculationService volumeCalculationService,
            ICalorieCalculationService calorieCalculationService)
        {
            _context = context;
            _volumeCalculationService = volumeCalculationService;
            _calorieCalculationService = calorieCalculationService;
        }

        public WorkoutSession WorkoutSession { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalCalories { get; set; }
        public Dictionary<string, decimal> VolumeByExercise { get; set; }
        public List<SelectListItem> SortOptions { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            WorkoutSession = await _context.WorkoutSessions
                .Include(s => s.User)
                .Include(s => s.WorkoutExercises)
                    .ThenInclude(e => e.ExerciseType)
                .Include(s => s.WorkoutExercises)
                    .ThenInclude(e => e.WorkoutSets)
                .FirstOrDefaultAsync(s => s.WorkoutSessionId == id);

            if (WorkoutSession == null)
            {
                return NotFound();
            }

            TotalVolume = _volumeCalculationService.CalculateWorkoutSessionVolume(WorkoutSession);
            TotalCalories = await _calorieCalculationService.CalculateSessionCaloriesAsync(WorkoutSession.WorkoutSessionId);

            return Page();
        }
    }
}
