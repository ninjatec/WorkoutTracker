using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Services
{
    public interface IWorkoutIterationService
    {
        Task<WorkoutSession> StartNextIterationAsync(int workoutSessionId);
        Task<bool> CanStartNextIterationAsync(int workoutSessionId);
    }

    public class WorkoutIterationService : IWorkoutIterationService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IUserService _userService;

        public WorkoutIterationService(
            WorkoutTrackerWebContext context,
            IUserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<bool> CanStartNextIterationAsync(int workoutSessionId)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (!userId.HasValue) return false;
            
            var session = await _context.WorkoutSessions
                .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == workoutSessionId && ws.UserId == userId.Value);

            return session != null && 
                   session.IsCompleted && 
                   session.NextIterationId == null;
        }

        public async Task<WorkoutSession> StartNextIterationAsync(int workoutSessionId)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (!userId.HasValue)
            {
                throw new InvalidOperationException("Cannot start next iteration: user not found");
            }
            
            var currentSession = await _context.WorkoutSessions
                .Include(ws => ws.WorkoutExercises)
                    .ThenInclude(we => we.WorkoutSets)
                .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == workoutSessionId && ws.UserId == userId.Value);

            if (currentSession == null || !currentSession.IsCompleted)
            {
                throw new InvalidOperationException("Cannot start next iteration: current session not found or not completed");
            }

            if (currentSession.NextIterationId.HasValue)
            {
                throw new InvalidOperationException("Next iteration already exists");
            }

            // Create new session as next iteration
            var nextSession = new WorkoutSession
            {
                UserId = userId.Value,
                Name = $"{currentSession.Name} (Iteration {currentSession.IterationNumber + 1})",
                Description = currentSession.Description,
                WorkoutTemplateId = currentSession.WorkoutTemplateId,
                TemplateAssignmentId = currentSession.TemplateAssignmentId,
                IsFromCoach = currentSession.IsFromCoach,
                Status = "In Progress",
                StartDateTime = DateTime.Now,
                IterationNumber = currentSession.IterationNumber + 1,
                PreviousIterationId = currentSession.WorkoutSessionId
            };

            _context.WorkoutSessions.Add(nextSession);
            await _context.SaveChangesAsync();

            // Copy exercises and sets
            foreach (var exercise in currentSession.WorkoutExercises)
            {
                var newExercise = new WorkoutExercise
                {
                    WorkoutSessionId = nextSession.WorkoutSessionId,
                    ExerciseTypeId = exercise.ExerciseTypeId,
                    EquipmentId = exercise.EquipmentId,
                    SequenceNum = exercise.SequenceNum,
                    OrderIndex = exercise.OrderIndex,
                    Notes = exercise.Notes,
                    RestPeriodSeconds = exercise.RestPeriodSeconds
                };

                _context.WorkoutExercises.Add(newExercise);
                await _context.SaveChangesAsync();

                foreach (var set in exercise.WorkoutSets)
                {
                    var newSet = new WorkoutSet
                    {
                        WorkoutExerciseId = newExercise.WorkoutExerciseId,
                        SettypeId = set.SettypeId,
                        SequenceNum = set.SequenceNum,
                        SetNumber = set.SetNumber,
                        TargetMinReps = set.TargetMinReps,
                        TargetMaxReps = set.TargetMaxReps,
                        Weight = set.Weight,
                        Notes = set.Notes
                    };

                    _context.WorkoutSets.Add(newSet);
                }
            }

            // Update the link from the previous session
            currentSession.NextIterationId = nextSession.WorkoutSessionId;
            _context.Update(currentSession);
            
            await _context.SaveChangesAsync();
            return nextSession;
        }
    }
}