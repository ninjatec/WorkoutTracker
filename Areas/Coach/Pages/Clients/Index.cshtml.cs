using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;
using WorkoutTrackerWeb.Extensions;

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

        public async Task<IActionResult> OnGetAsync(bool showInvite = false)
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

            // Get client groups
            await PopulateClientGroups(userId);

            // Set flag for showing invitation form
            ShowInvite = showInvite;

            return Page();
        }

        private async Task PopulateClientGroups(string coachId)
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
                foreach (var member in members.Take(3))
                {
                    var client = await _userManager.FindByIdAsync(member.Relationship.ClientId);
                    if (client != null)
                    {
                        memberNames.Add(client.UserName.Split('@')[0]);
                    }
                }

                ClientGroups.Add(new ClientGroupViewModel
                {
                    Id = group.Id,
                    Name = group.Name,
                    Description = group.Description,
                    MemberCount = memberCount,
                    MemberNames = memberNames
                });
            }
        }

        private async Task PopulateClientLists(List<CoachClientRelationship> relationships)
        {
            foreach (var relationship in relationships)
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
                    Group = await GetClientGroupName(relationship.Id)
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

            // Sort the lists
            ActiveClients = ActiveClients.OrderBy(c => c.Name).ToList();
            PendingClients = PendingClients.OrderBy(c => c.StartDate).ToList();
            InactiveClients = InactiveClients.OrderByDescending(c => c.EndDate).ToList();
        }

        private async Task<string> GetClientGroupName(int relationshipId)
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

        public async Task<IActionResult> OnPostInviteClientAsync()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                        
                    StatusMessage = $"Error: {errors}";
                    StatusMessageType = "Error";
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
                // Log the exception details
                _logger.LogError(ex, "Error preparing client invitation: {Message}", ex.Message);
                StatusMessage = $"Error: An unexpected error occurred: {ex.Message}";
                StatusMessageType = "Error";
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

            // Validate group name
            if (string.IsNullOrEmpty(groupName))
            {
                StatusMessage = "Error: Group name is required.";
                StatusMessageType = "Error";
                return RedirectToPage();
            }

            // Check for existing group with the same name
            var existingGroup = await _context.ClientGroups
                .Where(g => g.CoachId == coachId && g.Name == groupName)
                .FirstOrDefaultAsync();
                
            if (existingGroup != null)
            {
                StatusMessage = $"Error: A group named '{groupName}' already exists.";
                StatusMessageType = "Error";
                return RedirectToPage();
            }

            // Use a transaction to ensure all operations succeed or fail together
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
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
                        StatusMessage = $"Success: Created client group '{groupName}' with {addedCount} client{(addedCount > 1 ? "s" : "")}.";
                    }
                    else
                    {
                        StatusMessage = $"Success: Created client group '{groupName}'. No valid clients were added.";
                    }
                }
                else
                {
                    StatusMessage = $"Success: Created client group '{groupName}'.";
                }
                
                StatusMessageType = "Success";
                await transaction.CommitAsync();
                
                // Redirect to the new group's page
                return RedirectToPage("./Group", new { id = newGroup.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating client group: {Message}", ex.Message);
                StatusMessage = $"Error: Failed to create client group: {ex.Message}";
                StatusMessageType = "Error";
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
            public int MemberCount { get; set; }
            public List<string> MemberNames { get; set; } = new List<string>();
            public List<string> Members { get; set; } = new List<string>();
            public List<int> MemberIds { get; set; } = new List<int>();
        }
    }
}