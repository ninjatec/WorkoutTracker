using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Clients
{
    [Area("Coach")]
    [CoachAuthorize]
    public class IndexModel : PageModel
    {
        private readonly ICoachingService _coachingService;
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ICoachingService coachingService,
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ILogger<IndexModel> logger)
        {
            _coachingService = coachingService;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string StatusMessageType { get; set; } = "Success";

        [BindProperty(SupportsGet = true)]
        public bool ShowInvite { get; set; }

        [BindProperty]
        public string ClientEmail { get; set; }

        [BindProperty]
        public string InvitationMessage { get; set; }

        [BindProperty]
        public int ExpiryDays { get; set; } = 14;

        [BindProperty]
        public string[] Permissions { get; set; }

        public List<ClientViewModel> ActiveClients { get; set; } = new List<ClientViewModel>();
        public List<PendingInvitationViewModel> PendingClients { get; set; } = new List<PendingInvitationViewModel>();
        public List<ClientViewModel> InactiveClients { get; set; } = new List<ClientViewModel>();
        public List<ClientGroupViewModel> ClientGroups { get; set; } = new List<ClientGroupViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            // Get all coach-client relationships
            var relationships = await _coachingService.GetCoachRelationshipsAsync(userId);
            var relationshipsList = relationships.ToList();

            // Populate client lists
            await PopulateClientLists(relationshipsList);

            // For the demo, populate groups with sample data
            PopulateSampleClientGroups();

            return Page();
        }

        private async Task PopulateClientLists(List<CoachClientRelationship> relationships)
        {
            // Active clients
            ActiveClients = relationships
                .Where(r => r.Status == RelationshipStatus.Active)
                .Select(r => new ClientViewModel
                {
                    Id = r.Id,
                    Name = r.Client?.UserName?.Split('@')[0] ?? "Client",
                    Email = r.Client?.Email ?? "N/A",
                    StartDate = r.StartDate ?? r.CreatedDate,
                    // Sample data for demo - in real app these would come from the database
                    ActiveGoalsCount = new Random().Next(0, 5),
                    WorkoutsLast30Days = new Random().Next(0, 20),
                    LastWorkout = DateTime.Now.AddDays(-new Random().Next(0, 14))
                })
                .ToList();

            // Pending invitations
            PendingClients = relationships
                .Where(r => r.Status == RelationshipStatus.Pending)
                .Select(r => new PendingInvitationViewModel
                {
                    Id = r.Id,
                    Email = r.Client?.Email ?? "pending@example.com",
                    InvitationDate = r.CreatedDate,
                    ExpiryDate = r.InvitationExpiryDate
                })
                .ToList();

            // Inactive clients (paused or ended relationships)
            InactiveClients = relationships
                .Where(r => r.Status == RelationshipStatus.Paused || r.Status == RelationshipStatus.Ended)
                .Select(r => new ClientViewModel
                {
                    Id = r.Id,
                    Name = r.Client?.UserName?.Split('@')[0] ?? "Former Client",
                    Email = r.Client?.Email ?? "N/A",
                    StartDate = r.StartDate ?? r.CreatedDate,
                    EndDate = r.EndDate,
                    Status = r.Status.ToString()
                })
                .ToList();
        }

        private void PopulateSampleClientGroups()
        {
            // In a real implementation, these would be fetched from the database
            var groupNames = new string[] { "Strength Training", "Weight Loss", "Marathon Prep", "Beginners" };
            var groupDescriptions = new string[]
            {
                "Clients focused on building strength and muscle mass",
                "Clients with primary goal of weight loss",
                "Runners training for upcoming marathons",
                "New clients just starting their fitness journey"
            };

            var random = new Random();

            // Create sample groups
            for (int i = 0; i < groupNames.Length; i++)
            {
                // Only create the group if we have active clients
                if (ActiveClients.Any())
                {
                    var group = new ClientGroupViewModel
                    {
                        Id = i + 1,
                        Name = groupNames[i],
                        Description = groupDescriptions[i],
                        Members = new List<string>(),
                        MemberIds = new List<int>()
                    };

                    // Randomly assign some active clients to this group
                    foreach (var client in ActiveClients.Where(_ => random.Next(0, 3) == 0).Take(random.Next(1, 5)))
                    {
                        group.Members.Add(client.Name);
                        group.MemberIds.Add(client.Id);
                        
                        // Assign the group to the client for display
                        if (string.IsNullOrEmpty(client.Group) && random.Next(0, 2) == 0)
                        {
                            client.Group = group.Name;
                        }
                    }

                    group.MemberCount = group.Members.Count;
                    
                    if (group.MemberCount > 0)
                    {
                        ClientGroups.Add(group);
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostInviteClientAsync()
        {
            // Redirect to the new parameter-less handler method
            return await OnPostInviteClient();
        }

        public async Task<IActionResult> OnPostInviteClient()
        {
            try
            {
                _logger.LogInformation("=== INVITATION PROCESS STARTED ===");
                
                // Log all form values for diagnostic purposes
                var formValues = new Dictionary<string, string>();
                foreach (var key in Request.Form.Keys)
                {
                    formValues[key] = string.Join(", ", Request.Form[key].ToArray());
                }
                
                _logger.LogInformation("Client invitation form submission: {@FormValues}", formValues);
                
                if (string.IsNullOrWhiteSpace(ClientEmail))
                {
                    _logger.LogWarning("Error: Email address is required.");
                    StatusMessage = "Error: Email address is required.";
                    StatusMessageType = "Error";
                    return RedirectToPage();
                }
                
                var coachId = _userManager.GetUserId(User);
                _logger.LogInformation("Current user identity: {UserId}", User?.Identity?.Name);
                
                if (string.IsNullOrEmpty(coachId))
                {
                    _logger.LogCritical("CRITICAL ERROR: User ID is null when attempting to create invitation");
                    return Forbid();
                }

                _logger.LogInformation("Coach ID retrieved: {CoachId}", coachId);

                // Verify coach role first (as a double-check)
                var isCoach = await _coachingService.IsCoachAsync(coachId);
                _logger.LogInformation("Is user a coach? {IsCoach}", isCoach);
                
                if (!isCoach)
                {
                    _logger.LogWarning("User {CoachId} is not a coach but attempted to invite client", coachId);
                    StatusMessage = "Error: You must be a coach to invite clients.";
                    StatusMessageType = "Error";
                    return RedirectToPage();
                }

                // Check if the user already exists
                var client = await _userManager.FindByEmailAsync(ClientEmail);
                string clientId;
                _logger.LogInformation("Checking if client with email {Email} exists", ClientEmail);

                if (client != null)
                {
                    clientId = client.Id;
                    _logger.LogInformation("Using existing user with ID {ClientId} for invitation", clientId);
                    
                    // Check if a relationship already exists between coach and client
                    var existingRelationship = await _coachingService.GetCoachClientRelationshipAsync(coachId, clientId);
                    if (existingRelationship != null)
                    {
                        _logger.LogInformation("Found existing relationship with status {Status}", existingRelationship.Status);
                        
                        if (existingRelationship.Status == RelationshipStatus.Active)
                        {
                            StatusMessage = $"Error: {ClientEmail} is already your active client.";
                            StatusMessageType = "Error";
                            return RedirectToPage();
                        }
                        else if (existingRelationship.Status == RelationshipStatus.Pending)
                        {
                            StatusMessage = $"Error: An invitation to {ClientEmail} is already pending.";
                            StatusMessageType = "Error";
                            return RedirectToPage();
                        }
                    }
                }
                else
                {
                    // Create a temporary AppUser record for the invited email
                    var pendingUser = new AppUser
                    {
                        UserName = ClientEmail,
                        Email = ClientEmail,
                        EmailConfirmed = false
                    };
                    
                    _logger.LogInformation("Creating new temporary user for {Email}", ClientEmail);
                    
                    // Store in database with a placeholder password they'll never use
                    // (they'll set their own password when they register)
                    var result = await _userManager.CreateAsync(pendingUser, Guid.NewGuid().ToString());
                    
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to create temporary user for invitation: {Errors}", errors);
                        StatusMessage = $"Error: Unable to create invitation: {errors}";
                        StatusMessageType = "Error";
                        return RedirectToPage();
                    }
                    
                    clientId = pendingUser.Id;
                    _logger.LogInformation("Created temporary user with ID {UserId} for invitation email {Email}", clientId, ClientEmail);
                    
                    // Double check user was created
                    var createdUser = await _userManager.FindByIdAsync(clientId);
                    _logger.LogInformation("Verification - Created user exists: {Exists}", createdUser != null);
                }

                // Diagnostic database check
                try 
                {
                    _logger.LogInformation("=== DIAGNOSTIC DATABASE CHECK ===");
                    var connection = _context.Database.GetDbConnection();
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                        _logger.LogInformation("Successfully opened database connection");
                    }
                    
                    var dbState = _context.Database.CanConnect();
                    _logger.LogInformation("Database connection check - Can connect: {CanConnect}", dbState);
                    
                    // Check current tables
                    var tableMapping = _context.Model.FindEntityType(typeof(CoachClientRelationship));
                    var schema = tableMapping.GetSchema();
                    var tableName = tableMapping.GetTableName();
                    _logger.LogInformation("Table information - Schema: {Schema}, Table: {Table}", schema, tableName);
                    
                    await connection.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error performing diagnostic database check");
                }

                // Log relationship counts before creation 
                var beforeCount = await _context.CoachClientRelationships.CountAsync();
                _logger.LogInformation("Relationship count before creation: {Count}", beforeCount);

                // Convert permissions to List for compatibility with existing code
                var permissionsList = Permissions != null ? new List<string>(Permissions) : new List<string>();
                _logger.LogInformation("Permissions selected: {Permissions}", string.Join(", ", permissionsList));

                // Using a transaction for creating the relationship to ensure atomicity
                _logger.LogInformation("Beginning transaction for relationship creation");
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create the coach-client relationship with the expiry days parameter
                    _logger.LogInformation("Calling CreateCoachClientRelationshipAsync with coach {CoachId}, client {ClientId}, expiry {ExpiryDays}", 
                        coachId, clientId, ExpiryDays);
                        
                    var relationship = await _coachingService.CreateCoachClientRelationshipAsync(coachId, clientId, ExpiryDays);
                    
                    if (relationship != null)
                    {
                        _logger.LogInformation("Relationship created with ID: {RelationshipId}", relationship.Id);
                        
                        // Set permissions based on form selection
                        if (relationship.Permissions != null)
                        {
                            _logger.LogInformation("Setting custom permissions for relationship {RelationshipId}", relationship.Id);
                            
                            var permissionsToUpdate = relationship.Permissions;
                            
                            // Update each permission based on checkbox selection
                            permissionsToUpdate.CanViewWorkouts = permissionsList.Contains("canViewWorkouts");
                            permissionsToUpdate.CanCreateWorkouts = permissionsList.Contains("canCreateWorkouts");
                            permissionsToUpdate.CanEditWorkouts = permissionsList.Contains("canEditWorkouts");
                            permissionsToUpdate.CanDeleteWorkouts = permissionsList.Contains("canDeleteWorkouts");
                            permissionsToUpdate.CanViewReports = permissionsList.Contains("canViewReports");
                            permissionsToUpdate.CanCreateTemplates = permissionsList.Contains("canCreateTemplates");
                            permissionsToUpdate.CanAssignTemplates = permissionsList.Contains("canAssignTemplates");
                            permissionsToUpdate.CanViewPersonalInfo = permissionsList.Contains("canViewPersonalInfo");
                            permissionsToUpdate.CanCreateGoals = permissionsList.Contains("canCreateGoals");
                            
                            // Save the updated permissions
                            var permissionUpdateSuccess = await _coachingService.UpdateCoachPermissionsAsync(relationship.Id, permissionsToUpdate);
                            _logger.LogInformation("Permission update success: {Success}", permissionUpdateSuccess);
                        }

                        // Store invitation message if provided
                        if (!string.IsNullOrEmpty(InvitationMessage))
                        {
                            _logger.LogInformation("Adding invitation message to relationship {RelationshipId}", relationship.Id);
                            
                            // Add a note with the invitation message
                            var note = new CoachNote
                            {
                                CoachClientRelationshipId = relationship.Id,
                                Content = $"Invitation message: {InvitationMessage}",
                                CreatedDate = DateTime.UtcNow,
                                IsVisibleToClient = true
                            };
                            
                            _context.CoachNotes.Add(note);
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Note added successfully");
                        }

                        // Direct SQL verification that bypasses EF filters
                        _logger.LogInformation("Performing direct SQL verification");
                        var verification = await _coachingService.VerifyRelationshipExistsAsync(coachId, clientId);
                        _logger.LogInformation("SQL verification: Relationship exists: {Exists}, Total count: {Count}", 
                            verification.exists, verification.count);

                        // If verification fails but EF says it succeeded, something is wrong with query filters
                        if (!verification.exists)
                        {
                            _logger.LogWarning("Relationship was not found in database despite successful creation. Possible filter issue.");
                        }

                        // Verify relationship has been created
                        var verifyRelationship = await _context.CoachClientRelationships.FindAsync(relationship.Id);
                        _logger.LogInformation("Relationship verification using EF Find: {Found}", verifyRelationship != null);
                        
                        if (verifyRelationship == null)
                        {
                            _logger.LogError("Failed to find relationship with ID {Id} after creation", relationship.Id);
                        }

                        // Count relationships after creation
                        var afterCount = await _context.CoachClientRelationships.CountAsync();
                        _logger.LogInformation("Relationship count after creation: {Count}", afterCount);
                        _logger.LogInformation("Difference in relationship count: {Diff}", afterCount - beforeCount);

                        // Commit the transaction
                        await transaction.CommitAsync();
                        _logger.LogInformation("Transaction committed successfully");

                        // In a real implementation, you'd send an actual email here
                        // Send email with the invitation link that includes the token
                        // Example link: /register?invite=relationship.InvitationToken

                        StatusMessage = $"Success: Invitation sent to {ClientEmail}.";
                        StatusMessageType = "Success";
                        
                        // Reload data to ensure the pending invitations list is updated
                        _logger.LogInformation("Reloading coach relationships data");
                        var relationships = await _coachingService.GetCoachRelationshipsAsync(coachId);
                        var relationshipList = relationships.ToList();
                        _logger.LogInformation("Retrieved {Count} relationships for coach {CoachId} after creation", 
                            relationshipList.Count, coachId);
                        
                        var pendingCount = relationshipList.Count(r => r.Status == RelationshipStatus.Pending);
                        _logger.LogInformation("Pending relationships count: {Count}", pendingCount);
                        
                        await PopulateClientLists(relationshipList);
                        
                        // Switch to the Pending tab
                        ViewData["ActiveTab"] = "pending";

                        // Log DB stats for debugging
                        var stats = new
                        {
                            CoachClientRelationships = await _context.CoachClientRelationships.CountAsync(),
                            CoachClientPermissions = await _context.CoachClientPermissions.CountAsync(),
                            CoachNotes = await _context.CoachNotes.CountAsync(),
                            UserCount = await _userManager.Users.CountAsync()
                        };
                        _logger.LogInformation("Database statistics: {@Stats}", stats);
                        _logger.LogInformation("=== INVITATION PROCESS COMPLETED SUCCESSFULLY ===");
                    }
                    else
                    {
                        _logger.LogError("CreateCoachClientRelationshipAsync returned null - Failed to create relationship");
                        await transaction.RollbackAsync();
                        StatusMessage = $"Error: Failed to create relationship with {ClientEmail}.";
                        StatusMessageType = "Error";
                        _logger.LogInformation("=== INVITATION PROCESS FAILED ===");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during transaction for creating relationship");
                    await transaction.RollbackAsync();
                    StatusMessage = $"Error: Transaction failed: {ex.Message}";
                    StatusMessageType = "Error";
                    _logger.LogInformation("=== INVITATION PROCESS FAILED WITH EXCEPTION ===");
                }

                return RedirectToPage(new { showInvite = false });
            }
            catch (Exception ex)
            {
                // Log the exception details
                _logger.LogError(ex, "Unhandled exception during client invitation: {Message}", ex.Message);
                StatusMessage = $"Error: An unexpected error occurred: {ex.Message}";
                StatusMessageType = "Error";
                _logger.LogInformation("=== INVITATION PROCESS FAILED WITH UNHANDLED EXCEPTION ===");
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostCancelInvitationAsync(int clientId)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Get the relationship
            var relationship = await _context.CoachClientRelationships
                .FirstOrDefaultAsync(r => r.Id == clientId && r.CoachId == coachId);

            if (relationship != null && relationship.Status == RelationshipStatus.Pending)
            {
                // Update status to ended
                bool success = await _coachingService.UpdateRelationshipStatusAsync(relationship.Id, RelationshipStatus.Ended);
                if (success)
                {
                    StatusMessage = "Success: Invitation cancelled.";
                    StatusMessageType = "Success";
                }
                else
                {
                    StatusMessage = "Error: Failed to cancel invitation.";
                    StatusMessageType = "Error";
                }
            }
            else
            {
                StatusMessage = "Error: Invitation not found or cannot be cancelled.";
                StatusMessageType = "Error";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostReactivateClientAsync(int clientId)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Get the relationship
            var relationship = await _context.CoachClientRelationships
                .FirstOrDefaultAsync(r => r.Id == clientId && r.CoachId == coachId);

            if (relationship != null && relationship.Status == RelationshipStatus.Paused)
            {
                // Update status to active
                bool success = await _coachingService.UpdateRelationshipStatusAsync(relationship.Id, RelationshipStatus.Active);
                if (success)
                {
                    StatusMessage = "Success: Client reactivated.";
                    StatusMessageType = "Success";
                }
                else
                {
                    StatusMessage = "Error: Failed to reactivate client.";
                    StatusMessageType = "Error";
                }
            }
            else
            {
                StatusMessage = "Error: Client relationship not found or cannot be reactivated.";
                StatusMessageType = "Error";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateGroupAsync(string groupName, string groupDescription, List<int> selectedClients = null)
        {
            selectedClients = selectedClients ?? new List<int>();
            
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            if (string.IsNullOrEmpty(groupName))
            {
                StatusMessage = "Error: Group name is required.";
                StatusMessageType = "Error";
                return RedirectToPage();
            }

            // In a real implementation, you would create the group in the database
            // For now, just return a success message
            StatusMessage = $"Success: Client group '{groupName}' created.";
            StatusMessageType = "Success";

            return RedirectToPage();
        }

        public class ClientViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string Status { get; set; }
            public string Group { get; set; }
            public int ActiveGoalsCount { get; set; }
            public int WorkoutsLast30Days { get; set; }
            public DateTime? LastWorkout { get; set; }
        }

        public class PendingInvitationViewModel
        {
            public int Id { get; set; }
            public string Email { get; set; }
            public DateTime InvitationDate { get; set; }
            public DateTime? ExpiryDate { get; set; }
        }

        public class ClientGroupViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public int MemberCount { get; set; }
            public List<string> Members { get; set; } = new List<string>();
            public List<int> MemberIds { get; set; } = new List<int>();
        }
    }
}