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
        /// Creates a new quick workout session
        /// </summary>
        public async Task<WorkoutTrackerWeb.Models.Session> CreateQuickWorkoutSessionAsync(string name = null)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                throw new InvalidOperationException("User not found");
            }

            // Only generate default name if one wasn't provided
            if (string.IsNullOrWhiteSpace(name))
            {
                var now = DateTime.Now;
                var dayOfWeek = now.DayOfWeek.ToString();
                
                // Determine time of day
                string timeOfDay;
                var hour = now.Hour;
                if (hour >= 5 && hour < 12)
                    timeOfDay = "Morning";
                else if (hour >= 12 && hour < 17)
                    timeOfDay = "Afternoon";
                else if (hour >= 17 && hour < 21)
                    timeOfDay = "Evening";
                else
                    timeOfDay = "Night";
                
                name = $"{dayOfWeek} - {timeOfDay} Workout";
            }

            // Create a new session with the current date/time
            var session = new WorkoutTrackerWeb.Models.Session
            {
                Name = name,
                datetime = DateTime.Now,
                UserId = userId.Value,
                Notes = "Created using Quick Workout mode"
            };

            _context.Session.Add(session);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Created quick workout session {SessionId} for user {UserId}", 
                session.SessionId, userId.Value);
            
            return session;
        }
        
        /// <summary>
        /// Adds a set to the current workout with minimal input for quick logging
        /// </summary>
        public async Task<Set> AddQuickSetAsync(
            int sessionId, 
            int exerciseTypeId, 
            int settypeId,
            decimal weight, 
            int numberOfReps)
        {
            // Validate the session belongs to the current user
            var userId = await _userService.GetCurrentUserIdAsync();
            var session = await _context.Session
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId);
                
            if (session == null)
            {
                throw new InvalidOperationException("Session not found or doesn't belong to current user");
            }
            
            // Get the next sequence number - Modified to use ToList() for client evaluation
            var existingSequenceNumbers = await _context.Set
                .Where(s => s.SessionId == sessionId)
                .Select(s => s.SequenceNum)
                .ToListAsync();
            
            int nextSequenceNum = existingSequenceNumbers.Any() 
                ? existingSequenceNumbers.Max() + 1 
                : 0;
            
            // Create a new set
            var set = new Set
            {
                SessionId = sessionId,
                ExerciseTypeId = exerciseTypeId,
                SettypeId = settypeId,
                Weight = weight,
                NumberReps = numberOfReps,
                SequenceNum = nextSequenceNum
            };
            
            _context.Set.Add(set);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Added quick set {SetId} to session {SessionId}", 
                set.SetId, sessionId);
            
            return set;
        }
        
        /// <summary>
        /// Adds reps to a set in the current quick workout
        /// </summary>
        public async Task<List<Rep>> AddRepsToSetAsync(int setId, decimal weight, int numberOfReps, bool allSuccessful = true)
        {
            var set = await _context.Set
                .Include(s => s.Session)
                .FirstOrDefaultAsync(s => s.SetId == setId);
                
            if (set == null)
            {
                throw new InvalidOperationException("Set not found");
            }
            
            // Validate the set belongs to the current user
            var userId = await _userService.GetCurrentUserIdAsync();
            if (set.Session.UserId != userId)
            {
                throw new InvalidOperationException("Set doesn't belong to current user");
            }
            
            var reps = new List<Rep>();
            
            // Create the reps
            for (int i = 0; i < numberOfReps; i++)
            {
                var rep = new Rep
                {
                    SetsSetId = setId,
                    weight = weight,
                    repnumber = i + 1,
                    success = allSuccessful
                };
                
                reps.Add(rep);
            }
            
            _context.Rep.AddRange(reps);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Added {RepCount} reps to set {SetId}", 
                reps.Count, setId);
            
            return reps;
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
        public async Task<WorkoutTrackerWeb.Models.Session> GetLatestQuickWorkoutSessionAsync()
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return null;
            }
            
            return await _context.Session
                .Where(s => s.UserId == userId.Value && s.Notes.Contains("Quick Workout"))
                .OrderByDescending(s => s.datetime)
                .FirstOrDefaultAsync();
        }
        
        /// <summary>
        /// Checks if a session is currently in progress (created within the last 3 hours and not marked as completed)
        /// </summary>
        public async Task<bool> HasActiveQuickWorkoutAsync()
        {
            var latestSession = await GetLatestQuickWorkoutSessionAsync();
            if (latestSession == null)
            {
                return false;
            }
            
            // Check if session is marked as completed
            if (latestSession.Notes != null && latestSession.Notes.Contains("Completed at"))
            {
                return false;
            }
            
            // Consider a session "active" if it was created within the last 3 hours
            var activeTimeWindow = TimeSpan.FromHours(3);
            return DateTime.Now - latestSession.datetime < activeTimeWindow;
        }

        /// <summary>
        /// Marks a quick workout session as finished by updating the notes
        /// </summary>
        public async Task<WorkoutTrackerWeb.Models.Session> FinishQuickWorkoutSessionAsync(int sessionId)
        {
            // Validate the session belongs to the current user
            var userId = await _userService.GetCurrentUserIdAsync();
            var session = await _context.Session
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId);
                
            if (session == null)
            {
                throw new InvalidOperationException("Session not found or doesn't belong to current user");
            }
            
            // Update the notes to indicate the session is completed
            session.Notes = $"{session.Notes} - Completed at {DateTime.Now:yyyy-MM-dd HH:mm}";
            
            _context.Update(session);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Finished quick workout session {SessionId} for user {UserId}", 
                session.SessionId, userId);
            
            return session;
        }
    }
}