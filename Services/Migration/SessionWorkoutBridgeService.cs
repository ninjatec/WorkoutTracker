using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Services.Migration
{
    public interface ISessionWorkoutBridgeService
    {
        Task<Models.Session> GetSessionFromWorkoutSessionAsync(int workoutSessionId);
        Task<Models.WorkoutSession> GetWorkoutSessionFromSessionAsync(int sessionId);
        Task<List<Models.Session>> GetSessionsFromWorkoutSessionsAsync(IEnumerable<Models.WorkoutSession> workoutSessions);
        Task<List<Models.WorkoutSession>> GetWorkoutSessionsFromSessionsAsync(IEnumerable<Models.Session> sessions);
    }

    public class SessionWorkoutBridgeService : ISessionWorkoutBridgeService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<SessionWorkoutBridgeService> _logger;

        public SessionWorkoutBridgeService(
            WorkoutTrackerWebContext context,
            ILogger<SessionWorkoutBridgeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Converts a WorkoutSession to a Session model for backward compatibility
        /// </summary>
        public async Task<Models.Session> GetSessionFromWorkoutSessionAsync(int workoutSessionId)
        {
            try
            {
                // First, check if this WorkoutSession maps to an existing Session via the SessionId field
                var workoutSession = await _context.WorkoutSessions
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.WorkoutSets)
                    .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == workoutSessionId);

                if (workoutSession == null)
                {
                    _logger.LogWarning("WorkoutSession {WorkoutSessionId} not found", workoutSessionId);
                    return null;
                }

                // If this WorkoutSession maps to an existing Session, return that Session
                if (workoutSession.SessionId.HasValue)
                {
                    var existingSession = await _context.Session
                        .Include(s => s.Sets)
                            .ThenInclude(s => s.ExerciseType)
                        .Include(s => s.Sets)
                            .ThenInclude(s => s.Settype)
                        .Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.SessionId == workoutSession.SessionId);

                    if (existingSession != null)
                    {
                        // Set the WorkoutSessionStatus for display in the UI
                        existingSession.WorkoutSessionStatus = workoutSession.Status;
                        return existingSession;
                    }
                }

                // If not found or no mapping exists, create a virtual Session model
                var session = new Models.Session
                {
                    SessionId = -workoutSession.WorkoutSessionId, // Negative ID to indicate virtual session
                    Name = workoutSession.Name,
                    datetime = workoutSession.StartDateTime,
                    StartDateTime = workoutSession.StartDateTime,
                    endtime = workoutSession.EndDateTime,
                    UserId = workoutSession.UserId,
                    Notes = workoutSession.Description,
                    WorkoutSessionStatus = workoutSession.Status,
                    
                    // Create the Sets collection from WorkoutExercises/WorkoutSets
                    Sets = workoutSession.WorkoutExercises?
                        .SelectMany(we => we.WorkoutSets?
                            .Select(ws => new Models.Set
                            {
                                SetId = -ws.WorkoutSetId, // Negative ID to indicate virtual set
                                ExerciseTypeId = we.ExerciseTypeId,
                                ExerciseType = we.ExerciseType,
                                SettypeId = ws.SettypeId ?? 1, // Default to standard set if null
                                Description = ws.Notes,
                                Notes = ws.Notes,
                                NumberReps = ws.Reps ?? 0,
                                Weight = ws.Weight ?? 0,
                                SequenceNum = ws.SequenceNum
                            }) ?? Array.Empty<Models.Set>())
                        .ToList() ?? new List<Models.Set>()
                };

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting WorkoutSession {WorkoutSessionId} to Session", workoutSessionId);
                return null;
            }
        }

        /// <summary>
        /// Retrieves the WorkoutSession associated with a Session, or creates a virtual one
        /// </summary>
        public async Task<Models.WorkoutSession> GetWorkoutSessionFromSessionAsync(int sessionId)
        {
            try
            {
                // Check if there's a WorkoutSession with a reference to this Session
                var workoutSession = await _context.WorkoutSessions
                    .Include(ws => ws.WorkoutExercises)
                        .ThenInclude(we => we.WorkoutSets)
                    .FirstOrDefaultAsync(ws => ws.SessionId == sessionId);
                
                if (workoutSession != null)
                {
                    return workoutSession;
                }

                // If no WorkoutSession found, get the Session and create a virtual WorkoutSession
                var session = await _context.Session
                    .Include(s => s.Sets)
                        .ThenInclude(s => s.ExerciseType)
                    .Include(s => s.Sets)
                        .ThenInclude(s => s.Settype)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);

                if (session == null)
                {
                    _logger.LogWarning("Session {SessionId} not found", sessionId);
                    return null;
                }

                // Create a virtual WorkoutSession
                var virtualWorkoutSession = new Models.WorkoutSession
                {
                    WorkoutSessionId = -session.SessionId, // Negative ID to indicate virtual session
                    Name = session.Name,
                    Description = session.Notes,
                    UserId = session.UserId,
                    StartDateTime = session.datetime,
                    EndDateTime = session.endtime,
                    Duration = session.endtime.HasValue 
                        ? (int)Math.Ceiling((session.endtime.Value - session.datetime).TotalMinutes)
                        : 0,
                    Status = DetermineSessionStatus(session),
                    SessionId = session.SessionId
                };

                // Group sets by exercise type to create WorkoutExercises
                var exerciseGroups = session.Sets?
                    .GroupBy(s => s.ExerciseTypeId)
                    .ToList() ?? new List<IGrouping<int, Models.Set>>();

                virtualWorkoutSession.WorkoutExercises = new List<WorkoutExercise>();

                foreach (var group in exerciseGroups)
                {
                    if (group.Key == 0) continue; // Skip invalid exercise types

                    var firstSet = group.First();
                    
                    var workoutExercise = new WorkoutExercise
                    {
                        WorkoutSessionId = virtualWorkoutSession.WorkoutSessionId,
                        ExerciseTypeId = group.Key,
                        ExerciseType = firstSet.ExerciseType,
                        SequenceNum = group.Min(s => s.SequenceNum),
                        WorkoutSets = group.Select(s => new WorkoutSet
                        {
                            SettypeId = s.SettypeId,
                            Settype = s.Settype,
                            Reps = s.NumberReps,
                            Weight = s.Weight,
                            Notes = s.Notes,
                            SequenceNum = s.SequenceNum,
                            SetNumber = s.SequenceNum
                        }).ToList()
                    };

                    virtualWorkoutSession.WorkoutExercises.Add(workoutExercise);
                }

                return virtualWorkoutSession;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting Session {SessionId} to WorkoutSession", sessionId);
                return null;
            }
        }

        /// <summary>
        /// Converts a list of WorkoutSessions to Session models
        /// </summary>
        public async Task<List<Models.Session>> GetSessionsFromWorkoutSessionsAsync(IEnumerable<Models.WorkoutSession> workoutSessions)
        {
            var sessions = new List<Models.Session>();
            
            foreach (var workoutSession in workoutSessions)
            {
                var session = await GetSessionFromWorkoutSessionAsync(workoutSession.WorkoutSessionId);
                if (session != null)
                {
                    sessions.Add(session);
                }
            }
            
            return sessions;
        }

        /// <summary>
        /// Converts a list of Sessions to WorkoutSession models
        /// </summary>
        public async Task<List<Models.WorkoutSession>> GetWorkoutSessionsFromSessionsAsync(IEnumerable<Models.Session> sessions)
        {
            var workoutSessions = new List<Models.WorkoutSession>();
            
            foreach (var session in sessions)
            {
                var workoutSession = await GetWorkoutSessionFromSessionAsync(session.SessionId);
                if (workoutSession != null)
                {
                    workoutSessions.Add(workoutSession);
                }
            }
            
            return workoutSessions;
        }

        /// <summary>
        /// Determines the status of a session based on its properties
        /// </summary>
        private string DetermineSessionStatus(Models.Session session)
        {
            if (session.endtime.HasValue)
            {
                return "Completed";
            }
            
            if (session.datetime > DateTime.Now)
            {
                return "Scheduled";
            }
            
            if (session.datetime.Date == DateTime.Today)
            {
                return "InProgress";
            }
            
            // Session was scheduled in the past but not completed
            return "Missed";
        }
    }
}
