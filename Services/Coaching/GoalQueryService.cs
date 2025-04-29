using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;

namespace WorkoutTrackerWeb.Services.Coaching
{
    /// <summary>
    /// Service for querying and filtering goals with proper visibility controls
    /// </summary>
    public class GoalQueryService
    {
        private readonly WorkoutTrackerWebContext _context;

        public GoalQueryService(WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all goals visible to a user, including:
        /// - Goals created by the user
        /// - Goals created by their coach(es) for them
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>Collection of goals visible to the user</returns>
        public async Task<IEnumerable<ClientGoal>> GetGoalsForUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException(nameof(userId));

            // Find all coach-client relationships for this user as client
            var relationships = await _context.CoachClientRelationships
                .Where(r => r.ClientId == userId && r.Status == RelationshipStatus.Active)
                .Select(r => r.Id)
                .ToListAsync();

            // Combine user-created goals and coach-created goals for this user
            var goals = await _context.ClientGoals
                .Where(g => 
                    // Goals created by the user
                    (g.UserId == userId && g.IsActive) ||
                    // Goals created by a coach for this user
                    (relationships.Contains(g.CoachClientRelationshipId ?? -1) && g.IsActive))
                .OrderByDescending(g => g.CreatedDate)
                .ToListAsync();

            return goals;
        }

        /// <summary>
        /// Gets all goals visible to a coach, including:
        /// - Goals created by the coach for their clients
        /// - Goals created by clients that are marked as visible to coach
        /// </summary>
        /// <param name="coachId">The ID of the coach</param>
        /// <returns>Collection of goals visible to the coach</returns>
        public async Task<IEnumerable<ClientGoal>> GetGoalsForCoachAsync(string coachId)
        {
            if (string.IsNullOrEmpty(coachId))
                throw new ArgumentNullException(nameof(coachId));

            // Find all coach-client relationships for this coach
            var relationships = await _context.CoachClientRelationships
                .Where(r => r.CoachId == coachId && r.Status == RelationshipStatus.Active)
                .Select(r => new { r.Id, r.ClientId })
                .ToListAsync();

            var relationshipIds = relationships.Select(r => r.Id).ToList();
            var clientIds = relationships.Select(r => r.ClientId).ToList();

            // Get all relevant goals
            var goals = await _context.ClientGoals
                .Where(g => 
                    // Goals created by the coach for clients
                    (relationshipIds.Contains(g.CoachClientRelationshipId ?? -1) && g.IsCoachCreated && g.IsActive) ||
                    // Goals created by clients that are visible to coach
                    (clientIds.Contains(g.UserId) && g.IsVisibleToCoach && g.IsActive))
                .OrderByDescending(g => g.CreatedDate)
                .ToListAsync();

            return goals;
        }

        /// <summary>
        /// Gets goals for a specific client that are visible to their coach
        /// </summary>
        /// <param name="coachId">The ID of the coach</param>
        /// <param name="clientId">The ID of the client</param>
        /// <returns>Collection of goals for the specific client visible to the coach</returns>
        public async Task<IEnumerable<ClientGoal>> GetClientGoalsForCoachAsync(string coachId, string clientId)
        {
            if (string.IsNullOrEmpty(coachId))
                throw new ArgumentNullException(nameof(coachId));
            
            if (string.IsNullOrEmpty(clientId))
                throw new ArgumentNullException(nameof(clientId));

            // Find the relationship between this coach and client
            var relationship = await _context.CoachClientRelationships
                .Where(r => r.CoachId == coachId && r.ClientId == clientId && r.Status == RelationshipStatus.Active)
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (relationship == 0) // No relationship exists
                return new List<ClientGoal>();

            // Get all visible goals for this client
            var goals = await _context.ClientGoals
                .Where(g => 
                    // Goals created by the coach for this client
                    (g.CoachClientRelationshipId == relationship && g.IsCoachCreated && g.IsActive) ||
                    // Goals created by the client that are visible to coach
                    (g.UserId == clientId && g.IsVisibleToCoach && g.IsActive))
                .OrderByDescending(g => g.CreatedDate)
                .ToListAsync();

            return goals;
        }

        /// <summary>
        /// Gets goals filtered by category
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <param name="category">The goal category to filter by</param>
        /// <returns>Collection of goals in the specified category</returns>
        public async Task<IEnumerable<ClientGoal>> GetGoalsByCategoryAsync(string userId, GoalCategory category)
        {
            var goals = await GetGoalsForUserAsync(userId);
            return goals.Where(g => g.Category == category);
        }

        /// <summary>
        /// Gets active goals (not completed)
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>Collection of active goals</returns>
        public async Task<IEnumerable<ClientGoal>> GetActiveGoalsAsync(string userId)
        {
            var goals = await GetGoalsForUserAsync(userId);
            return goals.Where(g => g.IsActive && !g.IsCompleted && !g.CompletedDate.HasValue);
        }

        /// <summary>
        /// Gets completed goals
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns>Collection of completed goals</returns>
        public async Task<IEnumerable<ClientGoal>> GetCompletedGoalsAsync(string userId)
        {
            var goals = await GetGoalsForUserAsync(userId);
            return goals.Where(g => g.IsCompleted || g.CompletedDate.HasValue);
        }
    }
}