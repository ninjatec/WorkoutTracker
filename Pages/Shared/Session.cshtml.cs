using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;
using Microsoft.AspNetCore.OutputCaching;

namespace WorkoutTrackerWeb.Pages.Shared
{
    [OutputCache(PolicyName = "StaticContentWithId")]
    public class SessionModel : SharedPageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IWorkoutIterationService _workoutIterationService;

        public SessionModel(
            WorkoutTrackerWebContext context,
            ITokenValidationService tokenValidationService,
            IWorkoutIterationService workoutIterationService,
            ILogger<SessionModel> logger)
            : base(tokenValidationService, logger)
        {
            _context = context;
            _workoutIterationService = workoutIterationService;
        }

        public WorkoutSession WorkoutSession { get; set; }
        public Dictionary<string, List<WorkoutSet>> ExerciseSets { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id, string token = null)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Set the SessionId for validation
            SessionId = id.Value;
            
            // Validate the token provided in the request
            Token = token;
            var isValid = await ValidateShareTokenAsync();
            if (!isValid)
            {
                return RedirectToPage("./TokenRequired", new { token, errorMessage = "Invalid or expired token" });
            }
            
            // Check if token allows session access
            if (!SharedTokenData.IsValid)
            {
                _logger.LogWarning("Token does not have permission to access sessions");
                return RedirectToPage("./AccessDenied", new { Message = "Your share token does not have permission to view workout sessions." });
            }

            // Get the workout session
            WorkoutSession = await _context.WorkoutSessions
                .Include(ws => ws.User)
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.WorkoutSets)
                    .ThenInclude(ws => ws.Settype)
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.ExerciseType)
                .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == id && ws.UserId == SharedTokenData.UserId);

            if (WorkoutSession == null)
            {
                return NotFound();
            }

            // Group sets by exercise
            foreach (var exercise in WorkoutSession.WorkoutExercises.OrderBy(we => we.SequenceNum))
            {
                string exerciseName = exercise.ExerciseType?.Name ?? "Unknown Exercise";
                
                if (!ExerciseSets.ContainsKey(exerciseName))
                {
                    ExerciseSets[exerciseName] = new List<WorkoutSet>();
                }
                
                ExerciseSets[exerciseName].AddRange(
                    exercise.WorkoutSets.OrderBy(ws => ws.SequenceNum)
                );
            }

            return Page();
        }

        public async Task<IActionResult> OnPostStartNextIterationAsync(int id)
        {
            var canStart = await _workoutIterationService.CanStartNextIterationAsync(id);
            if (!canStart)
            {
                return RedirectToPage("./Error", new { message = "Cannot start next iteration" });
            }

            var nextSession = await _workoutIterationService.StartNextIterationAsync(id);
            return RedirectToPage("./Session", new { id = nextSession.WorkoutSessionId });
        }
    }
}