using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Services.Calculations;
using WorkoutTrackerWeb.ViewModels;

namespace WorkoutTrackerWeb.Pages.Sessions
{    
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IVolumeCalculationService _volumeCalculationService;
        private readonly ICalorieCalculationService _calorieCalculationService;
        private readonly IUserService _userService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            WorkoutTrackerWebContext context,
            IVolumeCalculationService volumeCalculationService,
            ICalorieCalculationService calorieCalculationService,
            IUserService userService,
            ILogger<DetailsModel> logger)
        {
            _context = context;
            _volumeCalculationService = volumeCalculationService;
            _calorieCalculationService = calorieCalculationService;
            _userService = userService;
            _logger = logger;
        }

        public WorkoutSession WorkoutSession { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalCalories { get; set; }
        public Dictionary<string, decimal> VolumeByExercise { get; set; }
        public List<SelectListItem> SortOptions { get; set; }
        public List<ExerciseSummary> ExerciseSummaries { get; set; }
        public List<Settype> AvailableSetTypes { get; set; }
        
        [BindProperty]
        public WorkoutSession EditableWorkoutSession { get; set; }
        
        [TempData]
        public string StatusMessage { get; set; }        public async Task<IActionResult> OnGetAsync(int? id, string sort = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }
            
            // Load available set types
            AvailableSetTypes = await _context.Set<Settype>().ToListAsync();

            // Prepare sort options
            SortOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "sequence", Text = "Original Order", Selected = string.IsNullOrEmpty(sort) || sort == "sequence" },
                new SelectListItem { Value = "name", Text = "Exercise Name", Selected = sort == "name" },
                new SelectListItem { Value = "volume", Text = "Volume (High to Low)", Selected = sort == "volume" }
            };

            // Fetch the workout session without including the related entities
            var workoutSession = await _context.WorkoutSessions
                .FirstOrDefaultAsync(m => m.WorkoutSessionId == id && m.UserId == currentUserId);
                
            if (workoutSession == null)
            {
                return NotFound();
            }
            
            // Now fetch the related entities separately to avoid the EF Core relation issues
            // Get all workout exercises for this session
            var workoutExercises = await _context.WorkoutExercises
                .Where(we => we.WorkoutSessionId == workoutSession.WorkoutSessionId)
                .OrderBy(we => we.OrderIndex)
                .ToListAsync();
                
            // Get all exercise types for these exercises
            var exerciseTypeIds = workoutExercises
                .Select(we => we.ExerciseTypeId)
                .Distinct()
                .ToList();
                
            var exerciseTypes = await _context.ExerciseType
                .Where(et => exerciseTypeIds.Contains(et.ExerciseTypeId))
                .ToListAsync();
                
            // Get all workout sets for these exercises
            var workoutExerciseIds = workoutExercises
                .Select(we => we.WorkoutExerciseId)
                .ToList();
                
            var workoutSets = await _context.WorkoutSets
                .Where(ws => workoutExerciseIds.Contains(ws.WorkoutExerciseId))
                .OrderBy(ws => ws.SetNumber)
                .ToListAsync();
                
            // Get all equipment used in these exercises
            var equipmentIds = workoutExercises
                .Where(we => we.EquipmentId.HasValue)
                .Select(we => we.EquipmentId.Value)
                .Distinct()
                .ToList();
                
            var equipment = await _context.Set<Equipment>()
                .Where(e => equipmentIds.Contains(e.EquipmentId))
                .ToListAsync();
                
            // Now manually connect the entities
            foreach (var exercise in workoutExercises)
            {
                // Link exercise type
                exercise.ExerciseType = exerciseTypes
                    .FirstOrDefault(et => et.ExerciseTypeId == exercise.ExerciseTypeId);
                    
                // Link equipment
                if (exercise.EquipmentId.HasValue)
                {
                    exercise.Equipment = equipment
                        .FirstOrDefault(e => e.EquipmentId == exercise.EquipmentId);
                }
                
                // Link workout sets
                exercise.WorkoutSets = workoutSets
                    .Where(ws => ws.WorkoutExerciseId == exercise.WorkoutExerciseId)
                    .ToList();
            }
              // Finally, connect exercises to session
            workoutSession.WorkoutExercises = workoutExercises;
            WorkoutSession = workoutSession;
            
            // Sort exercises based on sort parameter
            if (!string.IsNullOrEmpty(sort))
            {
                switch (sort)
                {
                    case "name":
                        workoutSession.WorkoutExercises = workoutSession.WorkoutExercises
                            .OrderBy(we => we.ExerciseType?.Name ?? "Unknown")
                            .ToList();
                        break;
                    case "volume":
                        var exerciseVolumes = new Dictionary<int, decimal>();
                        foreach (var ex in workoutSession.WorkoutExercises)
                        {
                            exerciseVolumes[ex.WorkoutExerciseId] = ex.WorkoutSets.Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0));
                        }
                        workoutSession.WorkoutExercises = workoutSession.WorkoutExercises
                            .OrderByDescending(we => exerciseVolumes[we.WorkoutExerciseId])
                            .ToList();
                        break;
                    default:
                        // Default sort by sequence number
                        workoutSession.WorkoutExercises = workoutSession.WorkoutExercises
                            .OrderBy(we => we.SequenceNum)
                            .ToList();
                        break;
                }
            }
            
            // Calculate total volume
            TotalVolume = _volumeCalculationService.CalculateWorkoutSessionVolume(workoutSession);

            // Calculate volume by exercise
            VolumeByExercise = _volumeCalculationService.CalculateSessionVolume(workoutSession);

            // Calculate total calories
            TotalCalories = _calorieCalculationService.CalculateCalories(workoutSession);
                
            // Create a summary of the exercises in this session
            ExerciseSummaries = workoutSession.WorkoutExercises
                .Select(we => new ExerciseSummary
                {
                    ExerciseName = we.ExerciseType?.Name ?? "Unknown Exercise",
                    TotalSets = we.WorkoutSets.Count,
                    MaxWeight = we.WorkoutSets.Any(s => s.Weight.HasValue) 
                        ? we.WorkoutSets.Max(s => s.Weight ?? 0) 
                        : 0,
                    TotalReps = we.WorkoutSets.Sum(s => s.Reps ?? 0),
                    VolumeLifted = we.WorkoutSets.Sum(s => (s.Weight ?? 0) * (s.Reps ?? 0))
                })
                .ToList();
                
            return Page();
        }
          public async Task<IActionResult> OnPostUpdateDetails(int id, string name, DateTime startDateTime, DateTime? endDateTime, string description)
        {
            var currentUserId = await _userService.GetCurrentUserIdAsync();
            if (currentUserId == null)
            {
                return Challenge();
            }

            // Get the workout session with ownership check
            var workoutSession = await _context.WorkoutSessions
                .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == id && ws.UserId == currentUserId);

            if (workoutSession == null)
            {
                return NotFound();
            }            // Log the current state before any changes
            _logger.LogInformation("Before update - Session ID: {SessionId}, Status: {Status}, EndDateTime: {EndDateTime}, CompletedDate: {CompletedDate}", 
                workoutSession.WorkoutSessionId, workoutSession.Status, workoutSession.EndDateTime, workoutSession.CompletedDate);

            // Update the fields
            workoutSession.Name = name;
            workoutSession.StartDateTime = startDateTime;
            workoutSession.EndDateTime = endDateTime;
            workoutSession.Description = description;

            // Update duration if end time is set
            if (workoutSession.EndDateTime.HasValue)
            {
                workoutSession.Duration = (int)(workoutSession.EndDateTime.Value - workoutSession.StartDateTime).TotalMinutes;
                
                // Update status to "Completed" when end date is set
                workoutSession.Status = "Completed";
                workoutSession.CompletedDate = workoutSession.EndDateTime;
                
                // Explicitly set IsCompleted flag as well
                workoutSession.IsCompleted = true;
                
                _logger.LogInformation("Setting workout to completed - EndDateTime: {EndDateTime}, Status: {Status}, CompletedDate: {CompletedDate}, IsCompleted: {IsCompleted}", 
                    workoutSession.EndDateTime, workoutSession.Status, workoutSession.CompletedDate, workoutSession.IsCompleted);
            }            try
            {
                // Explicitly mark the entity as modified
                _context.Entry(workoutSession).State = EntityState.Modified;
                
                // Log the entity state before saving
                _logger.LogInformation("Entity State: {EntityState}, Tracked: {IsTracked}", 
                    _context.Entry(workoutSession).State, 
                    _context.Entry(workoutSession).IsKeySet);
                
                // Save changes
                var rowsAffected = await _context.SaveChangesAsync();
                
                _logger.LogInformation("SaveChanges completed - Rows affected: {RowsAffected}", rowsAffected);
                
                // Verify the data was actually updated by retrieving it again
                var verifySession = await _context.WorkoutSessions
                    .AsNoTracking() // No tracking to get fresh data
                    .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == id);
                
                if (verifySession != null)
                {
                    _logger.LogInformation("After SaveChanges - Session ID: {SessionId}, Status: {Status}, EndDateTime: {EndDateTime}, CompletedDate: {CompletedDate}, IsCompleted: {IsCompleted}",
                        verifySession.WorkoutSessionId, verifySession.Status, verifySession.EndDateTime, 
                        verifySession.CompletedDate, verifySession.IsCompleted);
                }
                
                StatusMessage = "Workout details updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating workout session {SessionId}", id);
                StatusMessage = "Error updating workout details.";
            }

            return RedirectToPage(new { id });
        }
    }
}
