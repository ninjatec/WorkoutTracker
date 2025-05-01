using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Calculations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WorkoutTrackerWeb.Pages.Sessions
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;
        private readonly IVolumeCalculationService _volumeCalculationService;
        private readonly ICalorieCalculationService _calorieCalculationService;

        public DetailsModel(
            WorkoutTrackerWebContext context, 
            UserService userService,
            IVolumeCalculationService volumeCalculationService,
            ICalorieCalculationService calorieCalculationService)
        {
            _context = context;
            _userService = userService;
            _volumeCalculationService = volumeCalculationService;
            _calorieCalculationService = calorieCalculationService;
        }

        public WorkoutSession WorkoutSession { get; set; } = default!;
        
        [BindProperty(SupportsGet = true)]
        public string SortField { get; set; } = "Default";
        
        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; } = "asc";
        
        public string CurrentSort { get; set; }
        
        // Volume and calorie metrics
        public double TotalVolume { get; set; }
        public double TotalCalories { get; set; }
        public Dictionary<string, double> VolumeByExercise { get; set; } = new Dictionary<string, double>();
        public Dictionary<int, double> SetVolumes { get; set; } = new Dictionary<int, double>();
        
        public List<SelectListItem> SortOptions { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "Default", Text = "Default" },
            new SelectListItem { Value = "ExerciseType", Text = "Exercise Type Only" },
            new SelectListItem { Value = "Intensity", Text = "Intensity" },
            new SelectListItem { Value = "Reps", Text = "Number of Reps" },
            new SelectListItem { Value = "Weight", Text = "Weight (kg)" },
            new SelectListItem { Value = "SetID", Text = "Set ID" }
        };

        public async Task<IActionResult> OnGetAsync(int? id, string sortField, string sortOrder)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Apply sorting parameters if provided
            if (!string.IsNullOrEmpty(sortField))
            {
                SortField = sortField;
            }
            
            if (!string.IsNullOrEmpty(sortOrder))
            {
                SortOrder = sortOrder;
            }
            
            CurrentSort = SortField;

            // Get the current user id
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }

            // Get the workout session with ownership check and include related data
            var workoutSession = await _context.WorkoutSessions
                .Include(ws => ws.User)
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.ExerciseType)
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.WorkoutSets)
                .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == id && ws.UserId == currentUserId);

            if (workoutSession == null)
            {
                return NotFound();
            }

            // Apply sorting to the workout exercises and their sets
            if (workoutSession.WorkoutExercises != null && workoutSession.WorkoutExercises.Any())
            {
                foreach (var exercise in workoutSession.WorkoutExercises)
                {
                    if (exercise.WorkoutSets != null)
                    {
                        exercise.WorkoutSets = SortOrder.ToLower() == "asc" 
                            ? SortSetsAscending(exercise.WorkoutSets, SortField).ToList()
                            : SortSetsDescending(exercise.WorkoutSets, SortField).ToList();
                    }
                }
            }

            WorkoutSession = workoutSession;

            // Calculate volume and calories for the session
            if (id.HasValue)
            {
                TotalVolume = await _volumeCalculationService.CalculateSessionVolumeAsync(id.Value);
                TotalCalories = await _calorieCalculationService.CalculateSessionCaloriesAsync(id.Value);
                
                // Calculate volume by exercise type
                VolumeByExercise = await _volumeCalculationService.CalculateVolumeByExerciseTypeAsync(id.Value);
                
                // Calculate volume for each set
                SetVolumes = new Dictionary<int, double>();
                if (workoutSession.WorkoutExercises != null)
                {
                    foreach (var exercise in workoutSession.WorkoutExercises)
                    {
                        foreach (var set in exercise.WorkoutSets ?? Enumerable.Empty<WorkoutSet>())
                        {
                            var weight = set.Weight ?? 0;
                            var reps = set.Reps ?? 0;
                            double volume = (double)(weight * reps);
                            if (set.Distance.HasValue)
                            {
                                volume *= (double)set.Distance.Value;
                            }
                            SetVolumes[set.WorkoutSetId] = volume;
                        }
                    }
                }
            }

            return Page();
        }

        private IEnumerable<WorkoutSet> SortSetsAscending(IEnumerable<WorkoutSet> sets, string sortField)
        {
            return sortField switch
            {
                "Default" => sets.OrderBy(s => s.WorkoutSetId),
                "Intensity" => sets.OrderBy(s => s.Intensity),
                "Reps" => sets.OrderBy(s => s.Reps),
                "Weight" => sets.OrderBy(s => s.Weight),
                "SetID" => sets.OrderBy(s => s.WorkoutSetId),
                _ => sets.OrderBy(s => s.WorkoutSetId)
            };
        }

        private IEnumerable<WorkoutSet> SortSetsDescending(IEnumerable<WorkoutSet> sets, string sortField)
        {
            return sortField switch
            {
                "Default" => sets.OrderByDescending(s => s.WorkoutSetId),
                "Intensity" => sets.OrderByDescending(s => s.Intensity),
                "Reps" => sets.OrderByDescending(s => s.Reps),
                "Weight" => sets.OrderByDescending(s => s.Weight),
                "SetID" => sets.OrderByDescending(s => s.WorkoutSetId),
                _ => sets.OrderByDescending(s => s.WorkoutSetId)
            };
        }
    }
}
