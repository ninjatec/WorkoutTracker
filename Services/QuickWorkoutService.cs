using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Services
{
    /// <summary>
    /// Service for managing quick workouts optimized for gym use
    /// </summary>
    public class QuickWorkoutService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;
        private readonly ExerciseTypeService _exerciseTypeService;
        private readonly ExerciseSelectionService _exerciseSelectionService;
        private readonly ILogger<QuickWorkoutService> _logger;

        public QuickWorkoutService(
            WorkoutTrackerWebContext context,
            UserService userService,
            ExerciseTypeService exerciseTypeService,
            ExerciseSelectionService exerciseSelectionService,
            ILogger<QuickWorkoutService> logger)
        {
            _context = context;
            _userService = userService;
            _exerciseTypeService = exerciseTypeService;
            _exerciseSelectionService = exerciseSelectionService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the user's recent exercise types for quick access
        /// </summary>
        public async Task<List<ExerciseTypeWithUseCount>> GetRecentExercisesAsync(int? limit = 10)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return new List<ExerciseTypeWithUseCount>();
            }

            return await _exerciseSelectionService.GetRecentlyUsedExercisesAsync(userId.Value, limit ?? 10);
        }

        /// <summary>
        /// Gets the user's favorite exercise types
        /// </summary>
        public async Task<List<ExerciseType>> GetFavoriteExercisesAsync(int? limit = 10)
        {
            // Note: For future enhancement, this could be updated to use a proper favorites table
            // Currently using the most frequently used exercises
            return await _exerciseSelectionService.GetPopularExercisesAsync(limit ?? 10);
        }
        
        /// <summary>
        /// Gets exercise types matching the muscle group
        /// </summary>
        public async Task<List<ExerciseWithMuscleGroups>> GetExercisesByMuscleGroupAsync(string muscleGroup, int? limit = 10)
        {
            return await _exerciseSelectionService.GetExercisesByMuscleGroupAsync(muscleGroup, limit ?? 10);
        }

        /// <summary>
        /// Creates a new quick workout session with a specified start time
        /// </summary>
        public async Task<WorkoutSession> CreateQuickWorkoutSessionAsync(string name = null, DateTime? startTime = null)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                throw new InvalidOperationException("No current user found");
            }

            // Create the WorkoutSession
            var workoutSession = new WorkoutSession
            {
                UserId = userId.Value,
                Name = name ?? $"Quick Workout {DateTime.Now:yyyy-MM-dd HH:mm}",
                Description = "Created using Quick Workout mode",
                StartDateTime = startTime ?? DateTime.Now,
                Status = "In Progress"
            };

            _context.WorkoutSessions.Add(workoutSession);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created quick workout session {SessionId} for user {UserId} starting at {StartTime}", 
                workoutSession.WorkoutSessionId, userId.Value, workoutSession.StartDateTime);
            
            return workoutSession;
        }

        /// <summary>
        /// Adds a workout exercise to the current workout with minimal input for quick logging
        /// </summary>
        public async Task<WorkoutExercise> AddQuickWorkoutExerciseAsync(
            int workoutSessionId,
            int exerciseTypeId,
            int settypeId,
            decimal weight,
            int numberOfReps)
        {
            // Validate the session belongs to the current user
            var userId = await _userService.GetCurrentUserIdAsync();
            var session = await _context.WorkoutSessions
                .FirstOrDefaultAsync(s => s.WorkoutSessionId == workoutSessionId && s.UserId == userId);
                
            if (session == null)
            {
                throw new InvalidOperationException("Session not found or doesn't belong to current user");
            }

            var workoutExercise = new WorkoutExercise
            {
                WorkoutSessionId = workoutSessionId,
                ExerciseTypeId = exerciseTypeId,
                SequenceNum = 1,
                StartTime = DateTime.Now
            };

            _context.WorkoutExercises.Add(workoutExercise);
            await _context.SaveChangesAsync();

            var workoutSet = new WorkoutSet
            {
                WorkoutExerciseId = workoutExercise.WorkoutExerciseId,
                SetNumber = 1,
                Weight = weight,
                Reps = numberOfReps,
                SettypeId = settypeId
            };

            _context.WorkoutSets.Add(workoutSet);
            await _context.SaveChangesAsync();

            return workoutExercise;
        }

        /// <summary>
        /// Gets a list of set types for the dropdowns
        /// </summary>
        public async Task<List<Settype>> GetSetTypesAsync()
        {
            return await _context.Settype
                .OrderBy(st => st.Name)
                .ToListAsync();
        }
        
        /// <summary>
        /// Gets the most recent quick workout session for the current user, or null if none found
        /// </summary>
        public async Task<WorkoutSession> GetLatestQuickWorkoutSessionAsync()
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return null;
            }
            
            return await _context.WorkoutSessions
                .Where(s => s.UserId == userId.Value && s.Status == "In Progress")
                .OrderByDescending(s => s.StartDateTime)
                .FirstOrDefaultAsync();
        }
        
        /// <summary>
        /// Checks if a session is currently in progress (created within the last 3 hours and not marked as completed)
        /// </summary>
        public async Task<bool> HasActiveQuickWorkoutAsync()
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return false;
            }
            
            return await _context.WorkoutSessions
                .AnyAsync(s => s.UserId == userId.Value && 
                              s.Status == "In Progress" &&
                              s.StartDateTime >= DateTime.Now.AddHours(-3));
        }

        /// <summary>
        /// Marks a quick workout session as finished by updating the status and setting the end time
        /// </summary>
        public async Task<WorkoutSession> FinishQuickWorkoutSessionAsync(int sessionId, DateTime? endTime = null)
        {
            // Validate the session belongs to the current user
            var userId = await _userService.GetCurrentUserIdAsync();
            var session = await _context.WorkoutSessions
                .FirstOrDefaultAsync(s => s.WorkoutSessionId == sessionId && s.UserId == userId);
                
            if (session == null)
            {
                throw new InvalidOperationException("Session not found or doesn't belong to current user");
            }
            
            // Set the end time and status
            session.EndDateTime = endTime ?? DateTime.Now;
            session.Status = "Completed";
            session.CompletedDate = session.EndDateTime;
            session.Duration = (int)(session.EndDateTime.Value - session.StartDateTime).TotalMinutes;
            
            _context.Update(session);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Finished quick workout session {SessionId} for user {UserId}", 
                session.WorkoutSessionId, userId);
            
            return session;
        }

        /// <summary>
        /// Checks if a session has any completed sets
        /// </summary>
        public async Task<bool> HasCompletedSetsAsync(int sessionId)
        {
            var workoutSession = await _context.WorkoutSessions
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.WorkoutSets)
                .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == sessionId);

            if (workoutSession == null)
            {
                return false;
            }

            // Check if any WorkoutSets exist and are marked as completed
            return workoutSession.WorkoutExercises
                .SelectMany(we => we.WorkoutSets)
                .Any(ws => ws.IsCompleted);
        }
    }
}