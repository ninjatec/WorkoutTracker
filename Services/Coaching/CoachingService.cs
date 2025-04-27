using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Services.Coaching
{
    public interface ICoachingService
    {
        Task<bool> PromoteUserToCoachAsync(string userId);
        Task<bool> DemoteCoachToUserAsync(string userId);
        Task<bool> IsCoachAsync(string userId);
        Task<CoachClientRelationship> CreateCoachClientRelationshipAsync(string coachId, string clientId);
        Task<CoachClientRelationship> GetCoachClientRelationshipAsync(string coachId, string clientId);
        Task<IEnumerable<CoachClientRelationship>> GetCoachRelationshipsAsync(string coachId);
        Task<IEnumerable<CoachClientRelationship>> GetClientRelationshipsAsync(string clientId);
        Task<bool> UpdateCoachPermissionsAsync(int relationshipId, CoachClientPermission permissions);
        Task<bool> UpdateRelationshipStatusAsync(int relationshipId, RelationshipStatus status);
    }

    public class CoachingService : ICoachingService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<CoachingService> _logger;

        public CoachingService(
            UserManager<AppUser> userManager,
            WorkoutTrackerWebContext context,
            ILogger<CoachingService> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Promotes a regular user to a coach by assigning the Coach role
        /// </summary>
        /// <param name="userId">The user's Identity ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> PromoteUserToCoachAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Attempted to promote non-existent user {UserId} to coach", userId);
                    return false;
                }

                // Check if the user is already a coach
                if (await _userManager.IsInRoleAsync(user, "Coach"))
                {
                    _logger.LogInformation("User {UserId} is already a coach", userId);
                    return true;
                }

                // Add the user to the Coach role
                var result = await _userManager.AddToRoleAsync(user, "Coach");
                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully promoted user {UserId} to coach", userId);
                    return true;
                }
                else
                {
                    var errors = string.Join(", ", result.Errors);
                    _logger.LogError("Failed to promote user {UserId} to coach. Errors: {Errors}", userId, errors);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting user {UserId} to coach", userId);
                return false;
            }
        }

        /// <summary>
        /// Demotes a coach to a regular user by removing the Coach role
        /// </summary>
        /// <param name="userId">The coach's Identity ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DemoteCoachToUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Attempted to demote non-existent coach {UserId}", userId);
                    return false;
                }

                // Check if the user is already not a coach
                if (!await _userManager.IsInRoleAsync(user, "Coach"))
                {
                    _logger.LogInformation("User {UserId} is not a coach", userId);
                    return true;
                }

                // Remove the user from the Coach role
                var result = await _userManager.RemoveFromRoleAsync(user, "Coach");
                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully demoted coach {UserId} to regular user", userId);

                    // End all active coaching relationships for this coach
                    var relationships = await _context.CoachClientRelationships
                        .Where(r => r.CoachId == userId && r.Status == RelationshipStatus.Active)
                        .ToListAsync();

                    foreach (var relationship in relationships)
                    {
                        relationship.Status = RelationshipStatus.Ended;
                        relationship.EndDate = DateTime.UtcNow;
                        relationship.LastModifiedDate = DateTime.UtcNow;

                        if (relationship.Notes == null)
                        {
                            relationship.Notes = new List<CoachNote>();
                        }

                        relationship.Notes.Add(new CoachNote
                        {
                            Content = $"Relationship ended automatically on {DateTime.UtcNow:g} due to coach demotion.",
                            CreatedDate = DateTime.UtcNow,
                            IsVisibleToClient = false
                        });
                    }

                    await _context.SaveChangesAsync();
                    return true;
                }
                else
                {
                    var errors = string.Join(", ", result.Errors);
                    _logger.LogError("Failed to demote coach {UserId}. Errors: {Errors}", userId, errors);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error demoting coach {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Checks if a user has the Coach role
        /// </summary>
        /// <param name="userId">The user's Identity ID</param>
        /// <returns>True if the user is a coach, false otherwise</returns>
        public async Task<bool> IsCoachAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                return await _userManager.IsInRoleAsync(user, "Coach");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user {UserId} is a coach", userId);
                return false;
            }
        }

        /// <summary>
        /// Creates a coach-client relationship with default permissions
        /// </summary>
        /// <param name="coachId">The coach's Identity ID</param>
        /// <param name="clientId">The client's Identity ID</param>
        /// <returns>The created relationship or null if failed</returns>
        public async Task<CoachClientRelationship> CreateCoachClientRelationshipAsync(string coachId, string clientId)
        {
            try
            {
                // Verify the coach has the Coach role
                if (!await IsCoachAsync(coachId))
                {
                    _logger.LogWarning("Attempted to create relationship with non-coach user {CoachId}", coachId);
                    return null;
                }

                // Check if relationship already exists
                var existingRelationship = await _context.CoachClientRelationships
                    .Include(r => r.Notes)
                    .FirstOrDefaultAsync(r => r.CoachId == coachId && r.ClientId == clientId);

                if (existingRelationship != null)
                {
                    // If relationship exists but ended, reactivate it
                    if (existingRelationship.Status == RelationshipStatus.Ended)
                    {
                        existingRelationship.Status = RelationshipStatus.Pending;
                        existingRelationship.LastModifiedDate = DateTime.UtcNow;

                        existingRelationship.Notes.Add(new CoachNote
                        {
                            Content = $"Relationship reactivated on {DateTime.UtcNow:g}.",
                            CreatedDate = DateTime.UtcNow,
                            IsVisibleToClient = false
                        });

                        await _context.SaveChangesAsync();
                        return existingRelationship;
                    }

                    _logger.LogInformation("Coach-client relationship between {CoachId} and {ClientId} already exists", coachId, clientId);
                    return existingRelationship;
                }

                // Create a new relationship with default permissions
                var relationship = new CoachClientRelationship
                {
                    CoachId = coachId,
                    ClientId = clientId,
                    Status = RelationshipStatus.Pending,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    Notes = new List<CoachNote>
                    {
                        new CoachNote
                        {
                            Content = $"Relationship created on {DateTime.UtcNow:g}.",
                            CreatedDate = DateTime.UtcNow,
                            IsVisibleToClient = false
                        }
                    }
                };

                // Create default permissions
                var permissions = new CoachClientPermission
                {
                    CanViewWorkouts = true,
                    CanCreateWorkouts = false,
                    CanEditWorkouts = false,
                    CanDeleteWorkouts = false,
                    CanViewReports = true,
                    CanCreateTemplates = true,
                    CanAssignTemplates = true,
                    CanViewPersonalInfo = false,
                    CanCreateGoals = true,
                    LastModifiedDate = DateTime.UtcNow
                };

                // Add the relationship to the context
                _context.CoachClientRelationships.Add(relationship);
                await _context.SaveChangesAsync();

                // Set the relationship ID on the permissions and add to context
                permissions.CoachClientRelationshipId = relationship.Id;
                relationship.Permissions = permissions;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created coach-client relationship between {CoachId} and {ClientId}", coachId, clientId);
                return relationship;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating coach-client relationship between {CoachId} and {ClientId}", coachId, clientId);
                return null;
            }
        }

        /// <summary>
        /// Retrieves a specific coach-client relationship
        /// </summary>
        /// <param name="coachId">The coach's Identity ID</param>
        /// <param name="clientId">The client's Identity ID</param>
        /// <returns>The relationship or null if not found</returns>
        public async Task<CoachClientRelationship> GetCoachClientRelationshipAsync(string coachId, string clientId)
        {
            try
            {
                return await _context.CoachClientRelationships
                    .Include(r => r.Permissions)
                    .Include(r => r.Coach)
                    .Include(r => r.Client)
                    .Include(r => r.Notes)
                    .FirstOrDefaultAsync(r => r.CoachId == coachId && r.ClientId == clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving coach-client relationship between {CoachId} and {ClientId}", coachId, clientId);
                return null;
            }
        }

        /// <summary>
        /// Retrieves all relationships for a coach
        /// </summary>
        /// <param name="coachId">The coach's Identity ID</param>
        /// <returns>A list of relationships or empty list if none found</returns>
        public async Task<IEnumerable<CoachClientRelationship>> GetCoachRelationshipsAsync(string coachId)
        {
            try
            {
                return await _context.CoachClientRelationships
                    .Include(r => r.Permissions)
                    .Include(r => r.Client)
                    .Include(r => r.Notes)
                    .Where(r => r.CoachId == coachId)
                    .OrderByDescending(r => r.Status == RelationshipStatus.Active)
                    .ThenByDescending(r => r.Status == RelationshipStatus.Pending)
                    .ThenByDescending(r => r.LastModifiedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving relationships for coach {CoachId}", coachId);
                return new List<CoachClientRelationship>();
            }
        }

        /// <summary>
        /// Retrieves all relationships for a client
        /// </summary>
        /// <param name="clientId">The client's Identity ID</param>
        /// <returns>A list of relationships or empty list if none found</returns>
        public async Task<IEnumerable<CoachClientRelationship>> GetClientRelationshipsAsync(string clientId)
        {
            try
            {
                return await _context.CoachClientRelationships
                    .Include(r => r.Permissions)
                    .Include(r => r.Coach)
                    .Include(r => r.Notes)
                    .Where(r => r.ClientId == clientId)
                    .OrderByDescending(r => r.Status == RelationshipStatus.Active)
                    .ThenByDescending(r => r.Status == RelationshipStatus.Pending)
                    .ThenByDescending(r => r.LastModifiedDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving relationships for client {ClientId}", clientId);
                return new List<CoachClientRelationship>();
            }
        }

        /// <summary>
        /// Updates coach permissions for a specific relationship
        /// </summary>
        /// <param name="relationshipId">The relationship ID</param>
        /// <param name="permissions">The updated permissions</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateCoachPermissionsAsync(int relationshipId, CoachClientPermission permissions)
        {
            try
            {
                var relationship = await _context.CoachClientRelationships
                    .Include(r => r.Permissions)
                    .FirstOrDefaultAsync(r => r.Id == relationshipId);

                if (relationship == null)
                {
                    _logger.LogWarning("Attempted to update permissions for non-existent relationship {RelationshipId}", relationshipId);
                    return false;
                }

                // Update existing permissions
                var existingPermissions = relationship.Permissions;
                existingPermissions.CanViewWorkouts = permissions.CanViewWorkouts;
                existingPermissions.CanCreateWorkouts = permissions.CanCreateWorkouts;
                existingPermissions.CanEditWorkouts = permissions.CanEditWorkouts;
                existingPermissions.CanDeleteWorkouts = permissions.CanDeleteWorkouts;
                existingPermissions.CanViewReports = permissions.CanViewReports;
                existingPermissions.CanCreateTemplates = permissions.CanCreateTemplates;
                existingPermissions.CanAssignTemplates = permissions.CanAssignTemplates;
                existingPermissions.CanViewPersonalInfo = permissions.CanViewPersonalInfo;
                existingPermissions.CanCreateGoals = permissions.CanCreateGoals;
                existingPermissions.LastModifiedDate = DateTime.UtcNow;

                // Update the relationship last modified date
                relationship.LastModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated permissions for relationship {RelationshipId}", relationshipId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permissions for relationship {RelationshipId}", relationshipId);
                return false;
            }
        }

        /// <summary>
        /// Updates the status of a coach-client relationship
        /// </summary>
        /// <param name="relationshipId">The relationship ID</param>
        /// <param name="status">The new status</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> UpdateRelationshipStatusAsync(int relationshipId, RelationshipStatus status)
        {
            try
            {
                var relationship = await _context.CoachClientRelationships
                    .Include(r => r.Notes)
                    .FirstOrDefaultAsync(r => r.Id == relationshipId);

                if (relationship == null)
                {
                    _logger.LogWarning("Attempted to update status for non-existent relationship {RelationshipId}", relationshipId);
                    return false;
                }

                // Update status and relevant fields
                relationship.Status = status;
                relationship.LastModifiedDate = DateTime.UtcNow;

                // Set start date if becoming active
                if (status == RelationshipStatus.Active && !relationship.StartDate.HasValue)
                {
                    relationship.StartDate = DateTime.UtcNow;

                    if (relationship.Notes == null)
                    {
                        relationship.Notes = new List<CoachNote>();
                    }

                    relationship.Notes.Add(new CoachNote
                    {
                        Content = $"Relationship activated on {DateTime.UtcNow:g}.",
                        CreatedDate = DateTime.UtcNow,
                        IsVisibleToClient = false
                    });
                }

                // Set end date if ending
                if (status == RelationshipStatus.Ended && !relationship.EndDate.HasValue)
                {
                    relationship.EndDate = DateTime.UtcNow;

                    if (relationship.Notes == null)
                    {
                        relationship.Notes = new List<CoachNote>();
                    }

                    relationship.Notes.Add(new CoachNote
                    {
                        Content = $"Relationship ended on {DateTime.UtcNow:g}.",
                        CreatedDate = DateTime.UtcNow,
                        IsVisibleToClient = false
                    });
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated status for relationship {RelationshipId} to {Status}", relationshipId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for relationship {RelationshipId}", relationshipId);
                return false;
            }
        }
    }
}