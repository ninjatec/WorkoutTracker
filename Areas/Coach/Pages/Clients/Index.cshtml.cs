using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Areas.Coach.Pages.ErrorHandling;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Extensions;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;
using WorkoutTrackerWeb.Services.Validation;

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
        private readonly CoachingValidationService _validationService;

        public IndexModel(
            ICoachingService coachingService,
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ILogger<IndexModel> logger,
            CoachingValidationService validationService)
        {
            _coachingService = coachingService;
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _validationService = validationService;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string StatusMessageType { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string ClientEmail { get; set; }

        [BindProperty]
        public string InvitationMessage { get; set; }

        [BindProperty]
        public int ExpiryDays { get; set; } = 14;

        [BindProperty]
        public List<string> Permissions { get; set; } = new List<string>();

        public List<ClientViewModel> ActiveClients { get; set; } = new List<ClientViewModel>();
        public List<ClientViewModel> PendingClients { get; set; } = new List<ClientViewModel>();
        public List<ClientViewModel> InactiveClients { get; set; } = new List<ClientViewModel>();
        public List<ClientGroupViewModel> ClientGroups { get; set; } = new List<ClientGroupViewModel>();
        public bool ShowInvite { get; set; }

        public async Task<IActionResult> OnGetAsync(bool showInvite = false, string errorMessage = null)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            // If an error message was passed in the query string, display it
            if (!string.IsNullOrEmpty(errorMessage))
            {
                _validationService.SetError(this, errorMessage);
            }

            try
            {
                // Get all coach-client relationships
                var relationships = await _coachingService.GetCoachRelationshipsAsync(userId);
                var relationshipsList = relationships.ToList();

                // Populate client lists
                await PopulateClientLists(relationshipsList);

                // Get client groups
                await PopulateClientGroups(userId);

                // Set flag for showing invitation form
                ShowInvite = showInvite;
            }
            catch (Exception ex)
            {
                _validationService.HandleException(_logger, ex, this, 
                    "An error occurred while loading your client data.",
                    "loading client dashboard");
            }

            return Page();
        }

        private async Task PopulateClientGroups(string coachId)
        {
            try
            {
                // Get all client groups for this coach
                var groups = await _context.ClientGroups
                    .Where(g => g.CoachId == coachId)
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                foreach (var group in groups)
                {
                    // Get member count
                    var memberCount = await _context.ClientGroupMembers
                        .CountAsync(m => m.ClientGroupId == group.Id);

                    // Get up to 3 member names for display
                    var members = await _context.ClientGroupMembers
                        .Where(m => m.ClientGroupId == group.Id)
                        .Take(4)
                        .Join(_context.CoachClientRelationships,
                            m => m.CoachClientRelationshipId,
                            r => r.Id,
                            (m, r) => new { Member = m, Relationship = r })
                        .ToListAsync();

                    var memberNames = new List<string>();
                    var memberIds = new List<int>();
                    foreach (var member in members.Take(3))
                    {
                        var client = await _userManager.FindByIdAsync(member.Relationship.ClientId);
                        if (client != null)
                        {
                            memberNames.Add(client.UserName.Split('@')[0]);
                            memberIds.Add(member.Relationship.Id);
                        }
                    }

                    ClientGroups.Add(new ClientGroupViewModel
                    {
                        Id = group.Id,
                        Name = group.Name,
                        Description = group.Description,
                        ColorCode = group.ColorCode,
                        MemberCount = memberCount,
                        Members = memberNames,
                        MemberIds = memberIds
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading client groups for coach {CoachId}", coachId);
                // Continue without groups rather than failing the whole page
            }
        }

        private async Task PopulateClientLists(List<CoachClientRelationship> relationships)
        {
            foreach (var relationship in relationships)
            {
                try
                {
                    var client = await _userManager.FindByIdAsync(relationship.ClientId);
                    if (client == null) continue;

                    var clientViewModel = new ClientViewModel
                    {
                        Id = relationship.Id,
                        RelationshipId = relationship.Id,
                        Name = client.FullName() ?? client.UserName.Split('@')[0],
                        Email = client.Email,
                        Status = relationship.Status.ToString(),
                        StartDate = relationship.CreatedDate,
                        EndDate = relationship.EndDate,
                        Group = await GetClientGroupName(relationship.Id),
                        InvitationDate = relationship.Status == RelationshipStatus.Pending ? relationship.CreatedDate : null,
                        ExpiryDate = relationship.InvitationExpiryDate
                    };

                    switch (relationship.Status)
                    {
                        case RelationshipStatus.Active:
                            ActiveClients.Add(clientViewModel);
                            break;
                        case RelationshipStatus.Pending:
                            PendingClients.Add(clientViewModel);
                            break;
                        default:
                            InactiveClients.Add(clientViewModel);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing client relationship {RelationshipId}", relationship.Id);
                    // Continue processing other relationships
                }
            }

            // Sort the lists
            ActiveClients = ActiveClients.OrderBy(c => c.Name).ToList();
            PendingClients = PendingClients.OrderBy(c => c.StartDate).ToList();
            InactiveClients = InactiveClients.OrderByDescending(c => c.EndDate).ToList();
        }

        private async Task<string> GetClientGroupName(int relationshipId)
        {
            try
            {
                var groupMembership = await _context.ClientGroupMembers
                    .Where(m => m.CoachClientRelationshipId == relationshipId)
                    .Join(_context.ClientGroups,
                        m => m.ClientGroupId,
                        g => g.Id,
                        (m, g) => g.Name)
                    .FirstOrDefaultAsync();

                return groupMembership;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting group name for relationship {RelationshipId}", relationshipId);
                return null;
            }
        }

        public async Task<IActionResult> OnPostInviteClientAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _validationService.HandleInvalidModelState(this);
                    return RedirectToPage(new { showInvite = true });
                }

                // Store invitation parameters in TempData for the Invite page to use
                TempData["ClientEmail"] = ClientEmail;
                TempData["InvitationMessage"] = InvitationMessage;
                TempData["ExpiryDays"] = ExpiryDays;
                TempData["Permissions"] = Permissions;

                // Redirect to the standardized invitation page with the client email
                return RedirectToPage("./Invite", new { email = ClientEmail });
            }
            catch (Exception ex)
            {
                // Handle the exception using our validation service
                _validationService.HandleException(_logger, ex, this, 
                    "An error occurred while preparing the client invitation.",
                    "preparing client invitation");
                return RedirectToPage(new { showInvite = true });
            }
        }

        public async Task<IActionResult> OnPostCancelInvitationAsync(int clientId)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            try
            {
                // Get the relationship
                var relationship = await _context.CoachClientRelationships
                    .FirstOrDefaultAsync(r => r.Id == clientId && r.CoachId == coachId);

                if (relationship == null)
                {
                    _validationService.SetError(this, "Invitation not found or you don't have permission to cancel it.");
                    return RedirectToPage();
                }

                if (relationship.Status != RelationshipStatus.Pending)
                {
                    _validationService.SetError(this, "This invitation cannot be cancelled because it is not in pending status.");
                    return RedirectToPage();
                }

                // Update status to ended
                bool success = await _coachingService.UpdateRelationshipStatusAsync(relationship.Id, RelationshipStatus.Ended);
                if (success)
                {
                    _validationService.SetSuccess(this, "Invitation cancelled successfully.");
                }
                else
                {
                    _validationService.SetError(this, "Failed to cancel invitation. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _validationService.HandleException(_logger, ex, this, 
                    "An error occurred while cancelling the invitation.",
                    $"cancelling invitation {clientId}");
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

            try
            {
                // Get the relationship
                var relationship = await _context.CoachClientRelationships
                    .FirstOrDefaultAsync(r => r.Id == clientId && r.CoachId == coachId);

                if (relationship == null)
                {
                    _validationService.SetError(this, "Client relationship not found or you don't have permission to reactivate it.");
                    return RedirectToPage();
                }

                if (relationship.Status != RelationshipStatus.Paused)
                {
                    _validationService.SetError(this, "This client cannot be reactivated because they are not in paused status.");
                    return RedirectToPage();
                }

                // Update status to active
                bool success = await _coachingService.UpdateRelationshipStatusAsync(relationship.Id, RelationshipStatus.Active);
                if (success)
                {
                    _validationService.SetSuccess(this, "Client reactivated successfully.");
                }
                else
                {
                    _validationService.SetError(this, "Failed to reactivate client. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _validationService.HandleException(_logger, ex, this, 
                    "An error occurred while reactivating the client.",
                    $"reactivating client {clientId}");
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

            // Validate group name and description using our validation service
            if (!_validationService.ValidateGroupName(groupName, this))
            {
                return RedirectToPage();
            }
            
            if (!_validationService.ValidateGroupDescription(groupDescription, this))
            {
                return RedirectToPage();
            }

            try
            {
                // Check for existing group with the same name
                var existingGroup = await _context.ClientGroups
                    .Where(g => g.CoachId == coachId && g.Name == groupName)
                    .FirstOrDefaultAsync();
                    
                if (existingGroup != null)
                {
                    _validationService.SetError(this, $"A group named '{groupName}' already exists.");
                    return RedirectToPage();
                }

                // Use a transaction to ensure all operations succeed or fail together
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                // Create the new group
                var newGroup = new ClientGroup
                {
                    CoachId = coachId,
                    Name = groupName,
                    Description = groupDescription,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow
                };

                _context.ClientGroups.Add(newGroup);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Coach {CoachId} created new client group {GroupId}: {GroupName}", 
                    coachId, newGroup.Id, newGroup.Name);

                // Add selected clients to the group if any were selected
                if (selectedClients.Any())
                {
                    int addedCount = 0;
                    
                    foreach (var relationshipId in selectedClients)
                    {
                        // Verify relationship exists and belongs to this coach
                        var relationship = await _context.CoachClientRelationships
                            .Where(r => r.Id == relationshipId && r.CoachId == coachId && r.Status == RelationshipStatus.Active)
                            .FirstOrDefaultAsync();

                        if (relationship != null)
                        {
                            _context.ClientGroupMembers.Add(new ClientGroupMember
                            {
                                ClientGroupId = newGroup.Id,
                                CoachClientRelationshipId = relationshipId,
                                AddedDate = DateTime.UtcNow
                            });
                            addedCount++;
                        }
                    }

                    if (addedCount > 0)
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Added {Count} clients to group {GroupId}", addedCount, newGroup.Id);
                        _validationService.SetSuccess(this, $"Created client group '{groupName}' with {addedCount} client{(addedCount > 1 ? "s" : "")}.");
                    }
                    else
                    {
                        _validationService.SetSuccess(this, $"Created client group '{groupName}'. No valid clients were added.");
                    }
                }
                else
                {
                    _validationService.SetSuccess(this, $"Created client group '{groupName}'.");
                }
                
                await transaction.CommitAsync();
                
                // Redirect to the new group's page
                return RedirectToPage("./Group", new { id = newGroup.Id });
            }
            catch (Exception ex)
            {
                _validationService.HandleException(_logger, ex, this, 
                    "An error occurred while creating the client group.",
                    $"creating client group '{groupName}'");
                return RedirectToPage();
            }
        }

        public class ClientViewModel
        {
            public int Id { get; set; }
            public int RelationshipId { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime? EndDate { get; set; }
            public string Status { get; set; }
            public string Group { get; set; }
            public int ActiveGoalsCount { get; set; }
            public int WorkoutsLast30Days { get; set; }
            public DateTime? LastWorkout { get; set; }
            public DateTime? InvitationDate { get; set; }
            public DateTime? ExpiryDate { get; set; }
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
            public string ColorCode { get; set; }
            public int MemberCount { get; set; }
            public List<string> Members { get; set; } = new List<string>();
            public List<int> MemberIds { get; set; } = new List<int>();
        }
    }
}