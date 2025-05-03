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

            // Initialize volume and calories data
            TotalVolume = _volumeCalculationService.CalculateWorkoutSessionVolume(WorkoutSession);
            TotalCalories = await _calorieCalculationService.CalculateSessionCaloriesAsync(WorkoutSession.WorkoutSessionId);
            
            // Initialize VolumeByExercise - this was missing before
            VolumeByExercise = _volumeCalculationService.CalculateSessionVolume(WorkoutSession);
            
            // Initialize sort options
            SortOptions = new List<SelectListItem>
            {
                new SelectListItem { Text = "Default Order", Value = "default", Selected = true },
                new SelectListItem { Text = "Name (A-Z)", Value = "name_asc" },
                new SelectListItem { Text = "Name (Z-A)", Value = "name_desc" },
                new SelectListItem { Text = "Volume (High-Low)", Value = "volume_desc" },
                new SelectListItem { Text = "Volume (Low-High)", Value = "volume_asc" }
            };

            return Page();
        }
    }
}
