using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
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
        Task<CoachClientRelationship> CreateCoachClientRelationshipAsync(string coachId, string clientId, int expiryDays = 0);
        Task<CoachClientRelationship> InviteUserByEmailAsync(string coachId, string email, int expiryDays = 0);
        Task<CoachClientRelationship> GetCoachClientRelationshipAsync(string coachId, string clientId);
        Task<IEnumerable<CoachClientRelationship>> GetCoachRelationshipsAsync(string coachId);
        Task<IEnumerable<CoachClientRelationship>> GetClientRelationshipsAsync(string clientId);
        Task<bool> UpdateCoachPermissionsAsync(int relationshipId, CoachClientPermission permissions);
        Task<bool> UpdateRelationshipStatusAsync(int relationshipId, RelationshipStatus status);
        Task<(bool exists, int count)> VerifyRelationshipExistsAsync(string coachId, string clientId);
        Task<bool> ResendInvitationAsync(int relationshipId, string message, int expiryDays);
        Task<bool> ReactivateRelationshipAsync(int relationshipId);
    }

    public class CoachingService : ICoachingService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<CoachingService> _logger;
        private readonly IConfiguration _configuration;

        public CoachingService(
            UserManager<AppUser> userManager,
            WorkoutTrackerWebContext context,
            ILogger<CoachingService> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _configuration = configuration;
            
            // Log connection string (masked for security)
            var connString = _configuration.GetConnectionString("DefaultConnection") ?? "";
            if (!string.IsNullOrEmpty(connString))
            {
                var maskedConnString = "Server=" + connString.Split(';')
                    .FirstOrDefault(s => s.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))?.Substring(7) ?? "[not found]";
                _logger.LogInformation("CoachingService initialized with connection string to server: {Server}", maskedConnString);
            }
            else
            {
                _logger.LogWarning("CoachingService initialized with null or empty connection string");
            }
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
        /// <param name="expiryDays">Optional number of days until the invitation expires</param>
        /// <returns>The created relationship or null if failed</returns>
        public async Task<CoachClientRelationship> CreateCoachClientRelationshipAsync(string coachId, string clientId, int expiryDays = 0)
        {
            try
            {
                // First, ensure the coach exists in the AppUser table
                var coach = await _userManager.FindByIdAsync(coachId);
                if (coach == null)
                {
                    _logger.LogError("Cannot create relationship with non-existent coach ID {CoachId}", coachId);
                    return null;
                }
                
                // Verify the coach has the Coach role
                if (!await IsCoachAsync(coachId))
                {
                    _logger.LogWarning("Attempted to create relationship with non-coach user {CoachId}", coachId);
                    return null;
                }
                
                _logger.LogInformation("Verified coach role for user {CoachId}", coachId);
                
                // Verify the client user exists in the database
                var client = await _userManager.FindByIdAsync(clientId);
                if (client == null)
                {
                    _logger.LogError("Cannot create relationship with non-existent client {ClientId}", clientId);
                    return null;
                }
                
                _logger.LogInformation("Verified client exists with ID {ClientId}", clientId);

                // Get the execution strategy from the database context
                var strategy = _context.Database.CreateExecutionStrategy();
                
                // Execute the database operations within the execution strategy
                return await strategy.ExecuteAsync(async () =>
                {
                    // Log current relationship count for debugging
                    var beforeCount = await _context.CoachClientRelationships.CountAsync();
                    _logger.LogInformation("Current relationship count before creation: {Count}", beforeCount);
                    
                    // Check if relationship already exists using direct SQL to bypass any potential query filters
                    var verification = await VerifyRelationshipExistsAsync(coachId, clientId);
                    _logger.LogInformation("Pre-verification: Coach {CoachId} has {Count} relationships and {SpecificCount} with client {ClientId}",
                        coachId, verification.count, verification.exists ? 1 : 0, clientId);

                    // Check if we're already in a transaction
                    var hasExistingTransaction = _context.Database.CurrentTransaction != null;
                    _logger.LogInformation("Current transaction status: {HasTransaction}", 
                        hasExistingTransaction ? "Already in transaction" : "No active transaction");
                    
                    // Only create a new transaction if one doesn't already exist
                    Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = null;
                    
                    if (!hasExistingTransaction)
                    {
                        transaction = await _context.Database.BeginTransactionAsync();
                        _logger.LogInformation("Started new transaction for relationship creation");
                    }
                    else
                    {
                        _logger.LogInformation("Using existing transaction for relationship creation");
                    }
                    
                    try
                    {
                        if (verification.exists)
                        {
                            // Retrieve the existing relationship with tracking enabled
                            var existingRelationship = await _context.CoachClientRelationships
                                .Include(r => r.Notes)
                                .Include(r => r.Permissions)
                                .IgnoreQueryFilters() // Bypass any query filters
                                .FirstOrDefaultAsync(r => r.CoachId == coachId && r.ClientId == clientId);
                            
                            if (existingRelationship != null)
                            {
                                _logger.LogInformation("Found existing relationship with ID {Id} and status {Status}", 
                                    existingRelationship.Id, existingRelationship.Status);
                                
                                // If relationship exists but ended, reactivate it
                                if (existingRelationship.Status == RelationshipStatus.Ended)
                                {
                                    existingRelationship.Status = RelationshipStatus.Pending;
                                    existingRelationship.LastModifiedDate = DateTime.UtcNow;
                                    
                                    // Set expiry if specified
                                    if (expiryDays > 0)
                                    {
                                        existingRelationship.InvitationExpiryDate = DateTime.UtcNow.AddDays(expiryDays);
                                    }

                                    // Generate a new invitation token
                                    existingRelationship.InvitationToken = Guid.NewGuid().ToString("N");

                                    if (existingRelationship.Notes == null)
                                    {
                                        existingRelationship.Notes = new List<CoachNote>();
                                    }

                                    existingRelationship.Notes.Add(new CoachNote
                                    {
                                        Content = $"Relationship reactivated on {DateTime.UtcNow:g}.",
                                        CreatedDate = DateTime.UtcNow,
                                        IsVisibleToClient = false,
                                        CoachClientRelationshipId = existingRelationship.Id
                                    });

                                    _context.Update(existingRelationship);
                                    await _context.SaveChangesAsync();
                                    
                                    // Only commit if we created the transaction
                                    if (transaction != null)
                                    {
                                        await transaction.CommitAsync();
                                    }
                                    
                                    _logger.LogInformation("Reactivated coach-client relationship with ID {Id} between {CoachId} and {ClientId}", 
                                        existingRelationship.Id, coachId, clientId);
                                    
                                    return existingRelationship;
                                }

                                _logger.LogInformation("Coach-client relationship between {CoachId} and {ClientId} already exists with status {Status}",
                                    coachId, clientId, existingRelationship.Status);
                                
                                // Only commit if we created the transaction
                                if (transaction != null)
                                {
                                    await transaction.CommitAsync();
                                }
                                
                                return existingRelationship;
                            }
                        }

                        // Create a new relationship with default permissions
                        var relationship = new CoachClientRelationship
                        {
                            CoachId = coachId,
                            ClientId = clientId,
                            Status = RelationshipStatus.Pending,
                            CreatedDate = DateTime.UtcNow,
                            LastModifiedDate = DateTime.UtcNow,
                            InvitationToken = Guid.NewGuid().ToString("N"),
                            Notes = new List<CoachNote>(), // Initialize the Notes collection
                            Permissions = new CoachClientPermission() // Initialize the Permissions object
                        };
                        
                        // Set expiry if specified
                        if (expiryDays > 0)
                        {
                            relationship.InvitationExpiryDate = DateTime.UtcNow.AddDays(expiryDays);
                        }

                        // Initialize permissions properties
                        relationship.Permissions.CoachClientRelationshipId = 0; // This will be updated after save
                        relationship.Permissions.CanViewWorkouts = true;
                        relationship.Permissions.CanCreateWorkouts = false;
                        relationship.Permissions.CanEditWorkouts = false;
                        relationship.Permissions.CanDeleteWorkouts = false;
                        relationship.Permissions.CanViewReports = true;
                        relationship.Permissions.CanCreateTemplates = true;
                        relationship.Permissions.CanAssignTemplates = true;
                        relationship.Permissions.CanViewPersonalInfo = false;
                        relationship.Permissions.CanCreateGoals = true;
                        relationship.Permissions.LastModifiedDate = DateTime.UtcNow;

                        // Add relationship explicitly
                        _context.CoachClientRelationships.Add(relationship);
                        
                        // Create a note for relationship creation
                        var note = new CoachNote
                        {
                            Content = $"Relationship created on {DateTime.UtcNow:g}.",
                            CreatedDate = DateTime.UtcNow,
                            IsVisibleToClient = false
                        };
                        
                        // Add note to relationship
                        relationship.Notes.Add(note);
                        
                        // Log before saving
                        _logger.LogInformation("About to save new relationship. CoachId: {CoachId}, ClientId: {ClientId}", coachId, clientId);
                        
                        // Save to get the relationship ID
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Created new relationship with ID {Id}", relationship.Id);
                        
                        // Log after first save
                        _logger.LogInformation("After first save. Relationship ID: {Id}, Notes count: {NotesCount}, Has permissions: {HasPermissions}", 
                            relationship.Id, 
                            relationship.Notes?.Count ?? 0, 
                            relationship.Permissions != null);

                        // Update the relationship's permission ID now that we have the relationship ID
                        if (relationship.Permissions != null)
                        {
                            relationship.Permissions.CoachClientRelationshipId = relationship.Id;
                            await _context.SaveChangesAsync();
                        }
                        
                        // Verify relationship was created using direct SQL
                        var postVerification = await VerifyRelationshipExistsAsync(coachId, clientId);
                        _logger.LogInformation("Post-verification: Coach {CoachId} has {Count} relationships and {SpecificCount} with client {ClientId}",
                            coachId, postVerification.count, postVerification.exists ? 1 : 0, clientId);
                            
                        if (!postVerification.exists)
                        {
                            _logger.LogError("Verification failed - relationship doesn't exist in database despite successful creation");
                            if (transaction != null)
                            {
                                await transaction.RollbackAsync();
                            }
                            return null;
                        }

                        // Force EF to reload the relationship with all navigational properties
                        _context.Entry(relationship).State = EntityState.Detached;
                        var completeRelationship = await _context.CoachClientRelationships
                            .Include(r => r.Permissions)
                            .Include(r => r.Notes)
                            .FirstOrDefaultAsync(r => r.Id == relationship.Id);

                        // Only commit if we created the transaction
                        if (transaction != null)
                        {
                            await transaction.CommitAsync();
                        }
                        
                        if (completeRelationship == null)
                        {
                            _logger.LogError("Failed to retrieve committed relationship with ID {Id}", relationship.Id);
                            // Even though we failed to retrieve it, it should be in the database
                            // Return the original relationship object
                            return relationship;
                        }
                        
                        _logger.LogInformation("Successfully created and persisted coach-client relationship between {CoachId} and {ClientId}", 
                            coachId, clientId);
                            
                        return completeRelationship;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during transaction for coach-client relationship between {CoachId} and {ClientId}. Message: {Message}", 
                            coachId, clientId, ex.Message);
                        try
                        {
                            if (transaction != null)
                            {
                                await transaction.RollbackAsync();
                                _logger.LogInformation("Transaction rolled back successfully");
                            }
                        }
                        catch (Exception rollbackEx)
                        {
                            _logger.LogError(rollbackEx, "Error rolling back transaction. Message: {Message}", rollbackEx.Message);
                        }
                        return null;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating coach-client relationship between {CoachId} and {ClientId}. Message: {Message}", 
                    coachId, clientId, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Invites a user by email address, creating a pending relationship even if they aren't registered yet
        /// </summary>
        /// <param name="coachId">The coach's Identity ID</param>
        /// <param name="email">The email address of the user to invite</param>
        /// <param name="expiryDays">Optional number of days until the invitation expires</param>
        /// <returns>The created relationship or null if failed</returns>
        public async Task<CoachClientRelationship> InviteUserByEmailAsync(string coachId, string email, int expiryDays = 0)
        {
            try
            {
                // First, ensure the coach exists in the AppUser table
                var coach = await _userManager.FindByIdAsync(coachId);
                if (coach == null)
                {
                    _logger.LogError("Cannot create invitation with non-existent coach ID {CoachId}", coachId);
                    return null;
                }
                
                // Verify the coach has the Coach role
                if (!await IsCoachAsync(coachId))
                {
                    _logger.LogWarning("Attempted to create relationship with non-coach user {CoachId}", coachId);
                    return null;
                }
                
                _logger.LogInformation("Verified coach role for user {CoachId}", coachId);
                
                // Check if email is valid
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogError("Cannot invite user with empty email address");
                    return null;
                }
                
                // First, check if a user with this email already exists
                var existingUser = await _userManager.FindByEmailAsync(email);
                
                // If user exists, use the standard method to create relationship
                if (existingUser != null)
                {
                    _logger.LogInformation("Found existing user with email {Email}, ID: {UserId}", email, existingUser.Id);
                    return await CreateCoachClientRelationshipAsync(coachId, existingUser.Id, expiryDays);
                }
                
                _logger.LogInformation("No existing user found with email {Email}, creating invitation for unregistered user", email);

                // Get the execution strategy from the database context
                var strategy = _context.Database.CreateExecutionStrategy();
                
                // Execute the database operations within the execution strategy
                return await strategy.ExecuteAsync(async () =>
                {
                    // Check if relationship with invited email already exists
                    var existingRelationship = await _context.CoachClientRelationships
                        .Include(r => r.Notes)
                        .Include(r => r.Permissions)
                        .FirstOrDefaultAsync(r => r.CoachId == coachId && r.InvitedEmail == email);
                    
                    if (existingRelationship != null)
                    {
                        _logger.LogInformation("Found existing invitation relationship with ID {Id} and status {Status}", 
                            existingRelationship.Id, existingRelationship.Status);
                        
                        // If relationship exists but ended or rejected, reactivate it
                        if (existingRelationship.Status == RelationshipStatus.Ended || 
                            existingRelationship.Status == RelationshipStatus.Rejected)
                        {
                            existingRelationship.Status = RelationshipStatus.Pending;
                            existingRelationship.LastModifiedDate = DateTime.UtcNow;
                            
                            // Set expiry if specified
                            if (expiryDays > 0)
                            {
                                existingRelationship.InvitationExpiryDate = DateTime.UtcNow.AddDays(expiryDays);
                            }

                            // Generate a new invitation token
                            existingRelationship.InvitationToken = Guid.NewGuid().ToString("N");

                            if (existingRelationship.Notes == null)
                            {
                                existingRelationship.Notes = new List<CoachNote>();
                            }

                            existingRelationship.Notes.Add(new CoachNote
                            {
                                Content = $"Invitation resent on {DateTime.UtcNow:g}.",
                                CreatedDate = DateTime.UtcNow,
                                IsVisibleToClient = false,
                                CoachClientRelationshipId = existingRelationship.Id
                            });

                            _context.Update(existingRelationship);
                            await _context.SaveChangesAsync();
                            
                            _logger.LogInformation("Reactivated invitation relationship with ID {Id} between coach {CoachId} and email {Email}", 
                                existingRelationship.Id, coachId, email);
                            
                            return existingRelationship;
                        }

                        _logger.LogInformation("Invitation relationship between coach {CoachId} and email {Email} already exists with status {Status}",
                            coachId, email, existingRelationship.Status);
                        
                        return existingRelationship;
                    }

                    // Check if we're already in a transaction
                    var hasExistingTransaction = _context.Database.CurrentTransaction != null;
                    _logger.LogInformation("Current transaction status: {HasTransaction}", 
                        hasExistingTransaction ? "Already in transaction" : "No active transaction");
                    
                    // Only create a new transaction if one doesn't already exist
                    Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = null;
                    
                    if (!hasExistingTransaction)
                    {
                        transaction = await _context.Database.BeginTransactionAsync();
                        _logger.LogInformation("Started new transaction for relationship creation");
                    }
                    else
                    {
                        _logger.LogInformation("Using existing transaction for relationship creation");
                    }
                    
                    try
                    {
                        // Create a new relationship with default permissions
                        var relationship = new CoachClientRelationship
                        {
                            CoachId = coachId,
                            ClientId = null, // No client ID yet
                            InvitedEmail = email, // Store the email address
                            Status = RelationshipStatus.Pending,
                            CreatedDate = DateTime.UtcNow,
                            LastModifiedDate = DateTime.UtcNow,
                            InvitationToken = Guid.NewGuid().ToString("N"),
                            Notes = new List<CoachNote>(),
                            Permissions = new CoachClientPermission()
                        };
                        
                        // Set expiry if specified
                        if (expiryDays > 0)
                        {
                            relationship.InvitationExpiryDate = DateTime.UtcNow.AddDays(expiryDays);
                        }

                        // Initialize permissions properties
                        relationship.Permissions.CoachClientRelationshipId = 0; // This will be updated after save
                        relationship.Permissions.CanViewWorkouts = true;
                        relationship.Permissions.CanCreateWorkouts = false;
                        relationship.Permissions.CanEditWorkouts = false;
                        relationship.Permissions.CanDeleteWorkouts = false;
                        relationship.Permissions.CanViewReports = true;
                        relationship.Permissions.CanCreateTemplates = true;
                        relationship.Permissions.CanAssignTemplates = true;
                        relationship.Permissions.CanViewPersonalInfo = false;
                        relationship.Permissions.CanCreateGoals = true;
                        relationship.Permissions.LastModifiedDate = DateTime.UtcNow;

                        // Add relationship explicitly
                        _context.CoachClientRelationships.Add(relationship);
                        
                        // Create a note for relationship creation
                        var note = new CoachNote
                        {
                            Content = $"Invitation created for {email} on {DateTime.UtcNow:g}.",
                            CreatedDate = DateTime.UtcNow,
                            IsVisibleToClient = false
                        };
                        
                        // Add note to relationship
                        relationship.Notes.Add(note);
                        
                        _logger.LogInformation("About to save new invitation relationship for email {Email}", email);
                        
                        // Save to get the relationship ID
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Created new invitation relationship with ID {Id}", relationship.Id);

                        // Update the relationship's permission ID now that we have the relationship ID
                        if (relationship.Permissions != null)
                        {
                            relationship.Permissions.CoachClientRelationshipId = relationship.Id;
                            await _context.SaveChangesAsync();
                        }

                        // Force EF to reload the relationship with all navigational properties
                        _context.Entry(relationship).State = EntityState.Detached;
                        var completeRelationship = await _context.CoachClientRelationships
                            .Include(r => r.Permissions)
                            .Include(r => r.Notes)
                            .FirstOrDefaultAsync(r => r.Id == relationship.Id);

                        // Only commit if we created the transaction
                        if (transaction != null)
                        {
                            await transaction.CommitAsync();
                        }
                        
                        if (completeRelationship == null)
                        {
                            _logger.LogError("Failed to retrieve committed invitation relationship with ID {Id}", relationship.Id);
                            // Even though we failed to retrieve it, it should be in the database
                            return relationship;
                        }
                        
                        _logger.LogInformation("Successfully created and persisted invitation relationship for email {Email}", email);
                            
                        return completeRelationship;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during transaction for invitation relationship for email {Email}. Message: {Message}", 
                            email, ex.Message);
                        try
                        {
                            if (transaction != null)
                            {
                                await transaction.RollbackAsync();
                                _logger.LogInformation("Transaction rolled back successfully");
                            }
                        }
                        catch (Exception rollbackEx)
                        {
                            _logger.LogError(rollbackEx, "Error rolling back transaction. Message: {Message}", rollbackEx.Message);
                        }
                        return null;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invitation relationship for email {Email}. Message: {Message}", 
                    email, ex.Message);
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

        /// <summary>
        /// Resends an invitation for a pending relationship
        /// </summary>
        /// <param name="relationshipId">The relationship ID</param>
        /// <param name="message">Optional custom message to include in the invitation</param>
        /// <param name="expiryDays">Number of days until the invitation expires</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ResendInvitationAsync(int relationshipId, string message, int expiryDays)
        {
            try
            {
                var relationship = await _context.CoachClientRelationships
                    .Include(r => r.Notes)
                    .Include(r => r.Client)
                    .Include(r => r.Coach)
                    .FirstOrDefaultAsync(r => r.Id == relationshipId);

                if (relationship == null)
                {
                    _logger.LogWarning("Attempted to resend invitation for non-existent relationship {RelationshipId}", relationshipId);
                    return false;
                }

                if (relationship.Status != RelationshipStatus.Pending)
                {
                    _logger.LogWarning("Attempted to resend invitation for non-pending relationship {RelationshipId}", relationshipId);
                    return false;
                }

                // Update invitation details
                relationship.InvitationToken = Guid.NewGuid().ToString("N");
                relationship.LastModifiedDate = DateTime.UtcNow;
                
                // Set new expiry date if specified
                if (expiryDays > 0)
                {
                    relationship.InvitationExpiryDate = DateTime.UtcNow.AddDays(expiryDays);
                }

                // Add a note about the resent invitation
                if (relationship.Notes == null)
                {
                    relationship.Notes = new List<CoachNote>();
                }

                relationship.Notes.Add(new CoachNote
                {
                    Content = $"Invitation resent on {DateTime.UtcNow:g}.",
                    CreatedDate = DateTime.UtcNow,
                    IsVisibleToClient = false
                });

                // Add the custom message if provided
                if (!string.IsNullOrEmpty(message))
                {
                    relationship.Notes.Add(new CoachNote
                    {
                        Content = $"Invitation message: {message}",
                        CreatedDate = DateTime.UtcNow,
                        IsVisibleToClient = true
                    });
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully resent invitation for relationship {RelationshipId}", relationshipId);
                
                // In a real implementation, you'd send an actual email here with the invitation token

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending invitation for relationship {RelationshipId}", relationshipId);
                return false;
            }
        }

        /// <summary>
        /// Reactivates a previously ended or paused relationship
        /// </summary>
        /// <param name="relationshipId">The relationship ID</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> ReactivateRelationshipAsync(int relationshipId)
        {
            try
            {
                var relationship = await _context.CoachClientRelationships
                    .Include(r => r.Notes)
                    .FirstOrDefaultAsync(r => r.Id == relationshipId);

                if (relationship == null)
                {
                    _logger.LogWarning("Attempted to reactivate non-existent relationship {RelationshipId}", relationshipId);
                    return false;
                }

                if (relationship.Status == RelationshipStatus.Active)
                {
                    // Already active, nothing to do
                    _logger.LogInformation("Relationship {RelationshipId} is already active", relationshipId);
                    return true;
                }

                // Update relationship status
                relationship.Status = RelationshipStatus.Active;
                relationship.LastModifiedDate = DateTime.UtcNow;
                
                // If this is the first activation, set the start date
                if (!relationship.StartDate.HasValue)
                {
                    relationship.StartDate = DateTime.UtcNow;
                }
                
                // Clear the end date since relationship is now active
                relationship.EndDate = null;

                // Add a note about the reactivation
                if (relationship.Notes == null)
                {
                    relationship.Notes = new List<CoachNote>();
                }

                relationship.Notes.Add(new CoachNote
                {
                    Content = $"Relationship reactivated on {DateTime.UtcNow:g}.",
                    CreatedDate = DateTime.UtcNow,
                    IsVisibleToClient = true
                });

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully reactivated relationship {RelationshipId}", relationshipId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reactivating relationship {RelationshipId}", relationshipId);
                return false;
            }
        }

        /// <summary>
        /// Directly confirms whether a relationship was persisted via SQL query
        /// </summary>
        /// <param name="coachId">The coach's Identity ID</param>
        /// <param name="clientId">The client's Identity ID</param>
        /// <returns>A tuple with existence status and record count</returns>
        public async Task<(bool exists, int count)> VerifyRelationshipExistsAsync(string coachId, string clientId)
        {
            try
            {
                // Use raw SQL to bypass EF Core filters and check if relationships exist
                var connection = _context.Database.GetDbConnection();
                
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                using var command = connection.CreateCommand();
                
                // Get the schema information from Entity Framework
                var tableMapping = _context.Model.FindEntityType(typeof(CoachClientRelationship));
                var schema = tableMapping.GetSchema();
                var tableName = tableMapping.GetTableName();
                
                var fullTableName = string.IsNullOrEmpty(schema) ? tableName : $"{schema}.{tableName}";
                
                _logger.LogInformation("Using table name {TableName} for SQL verification", fullTableName);
                
                // Direct SQL to count relationships
                command.CommandText = $@"
                    SELECT COUNT(*) 
                    FROM {fullTableName} 
                    WHERE CoachId = @coachId";
                    
                var coachIdParam = command.CreateParameter();
                coachIdParam.ParameterName = "@coachId";
                coachIdParam.Value = coachId;
                command.Parameters.Add(coachIdParam);
                
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());
                
                // Check for specific relationship
                command.CommandText = $@"
                    SELECT COUNT(*) 
                    FROM {fullTableName} 
                    WHERE CoachId = @coachId 
                    AND ClientId = @clientId";
                    
                var clientIdParam = command.CreateParameter();
                clientIdParam.ParameterName = "@clientId";
                clientIdParam.Value = clientId;
                command.Parameters.Add(clientIdParam);
                
                var specificCount = Convert.ToInt32(await command.ExecuteScalarAsync());
                
                _logger.LogInformation("Direct SQL verification: Coach {CoachId} has {Count} total relationships and {SpecificCount} with client {ClientId}",
                    coachId, count, specificCount, clientId);
                    
                return (specificCount > 0, count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error directly verifying relationship between {CoachId} and {ClientId}", coachId, clientId);
                return (false, 0);
            }
        }
    }
}