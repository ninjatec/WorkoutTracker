using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Areas.Coach.Pages.ErrorHandling;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;
using WorkoutTrackerWeb.Services.Validation;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Clients
{
    [Area("Coach")]
    [CoachAuthorize]
    public class GroupModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ICoachingService _coachingService;
        private readonly ILogger<GroupModel> _logger;
        private readonly CoachingValidationService _validationService;

        public GroupModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ICoachingService coachingService,
            ILogger<GroupModel> logger,
            CoachingValidationService validationService)
        {
            _context = context;
            _userManager = userManager;
            _coachingService = coachingService;
            _logger = logger;
            _validationService = validationService;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string StatusMessageType { get; set; }

        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string GroupDescription { get; set; }
        public string ColorCode { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int MemberCount { get; set; }
        
        public List<GroupMemberViewModel> Members { get; set; } = new List<GroupMemberViewModel>();
        public List<ClientViewModel> AvailableClients { get; set; } = new List<ClientViewModel>();
        public List<TemplateViewModel> AvailableTemplates { get; set; } = new List<TemplateViewModel>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            _logger.LogInformation("Getting client group with ID {GroupId} for coach {CoachId}", id, coachId);

            // Get the client group
            var group = await _context.ClientGroups
                .Where(g => g.Id == id && g.CoachId == coachId)
                .FirstOrDefaultAsync();

            if (group == null)
            {
                _logger.LogWarning("Client group {GroupId} not found for coach {CoachId}", id, coachId);
                return RedirectToPage("./Index", new { 
                    errorMessage = "The requested client group was not found or you don't have access to it."
                });
            }

            // Set group properties
            GroupId = group.Id;
            GroupName = group.Name;
            GroupDescription = group.Description;
            ColorCode = group.ColorCode ?? "#0d6efd"; // Default to blue if not set
            CreatedDate = group.CreatedDate;

            try
            {
                // Get group members
                var memberRelationships = await _context.ClientGroupMembers
                    .Where(m => m.ClientGroupId == id)
                    .Join(_context.CoachClientRelationships,
                        m => m.CoachClientRelationshipId,
                        r => r.Id,
                        (m, r) => new { Member = m, Relationship = r })
                    .ToListAsync();

                MemberCount = memberRelationships.Count;

                // Load member details
                foreach (var item in memberRelationships)
                {
                    var client = await _userManager.FindByIdAsync(item.Relationship.ClientId);
                    if (client != null)
                    {
                        Members.Add(new GroupMemberViewModel
                        {
                            ClientRelationshipId = item.Relationship.Id,
                            Name = client.UserName.Split('@')[0],
                            Email = client.Email,
                            AddedDate = item.Member.AddedDate
                        });
                    }
                }

                // Sort members by name initially
                Members = Members.OrderBy(m => m.Name).ToList();

                // Get available clients (active clients not in this group)
                var activeRelationships = await _context.CoachClientRelationships
                    .Where(r => r.CoachId == coachId && r.Status == RelationshipStatus.Active)
                    .ToListAsync();

                var memberRelationshipIds = memberRelationships.Select(m => m.Relationship.Id).ToList();
                
                foreach (var relationship in activeRelationships)
                {
                    if (!memberRelationshipIds.Contains(relationship.Id))
                    {
                        var client = await _userManager.FindByIdAsync(relationship.ClientId);
                        if (client != null)
                        {
                            AvailableClients.Add(new ClientViewModel
                            {
                                RelationshipId = relationship.Id,
                                Name = client.UserName.Split('@')[0],
                                Email = client.Email
                            });
                        }
                    }
                }

                // Sort available clients by name
                AvailableClients = AvailableClients.OrderBy(c => c.Name).ToList();

                // Get available templates
                var templates = await _context.WorkoutTemplate
                    .Where(t => t.UserId == int.Parse(coachId) || t.IsPublic)
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                foreach (var template in templates)
                {
                    AvailableTemplates.Add(new TemplateViewModel
                    {
                        TemplateId = template.WorkoutTemplateId,
                        Name = template.Name,
                        IsOwner = template.UserId == int.Parse(coachId)
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorUtils.HandleException(_logger, ex, this, 
                    "An error occurred while loading the client group details.",
                    $"loading client group {id}");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateGroupAsync(int groupId, string groupName, string groupDescription, string colorCode)
        {
            if (!ModelState.IsValid)
            {
                _validationService.HandleInvalidModelState(this);
                return RedirectToPage(new { id = groupId });
            }

            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Validate group name and description
            if (!_validationService.ValidateGroupName(groupName, this))
                return RedirectToPage(new { id = groupId });
            
            if (!_validationService.ValidateGroupDescription(groupDescription, this))
                return RedirectToPage(new { id = groupId });

            try
            {
                var group = await _context.ClientGroups
                    .Where(g => g.Id == groupId && g.CoachId == coachId)
                    .FirstOrDefaultAsync();

                if (group == null)
                {
                    _validationService.SetError(this, "Group not found or you don't have access to it.");
                    return RedirectToPage("./Index");
                }

                // Check for duplicate name (excluding current group)
                var existingGroup = await _context.ClientGroups
                    .Where(g => g.CoachId == coachId && g.Name == groupName && g.Id != groupId)
                    .FirstOrDefaultAsync();
                    
                if (existingGroup != null)
                {
                    _validationService.SetError(this, $"A group named '{groupName}' already exists.");
                    return RedirectToPage(new { id = groupId });
                }

                // Update group details
                group.Name = groupName;
                group.Description = groupDescription;
                group.ColorCode = colorCode;
                group.LastModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Coach {CoachId} updated group {GroupId}: {GroupName}", 
                    coachId, groupId, groupName);
                    
                _validationService.SetSuccess(this, "Group details updated successfully.");
            }
            catch (Exception ex)
            {
                _validationService.HandleException(_logger, ex, this, 
                    "An error occurred while updating the group details.",
                    $"updating group {groupId}");
            }

            return RedirectToPage(new { id = groupId });
        }

        public async Task<IActionResult> OnPostAddMembersAsync(int groupId, List<int> selectedClients)
        {
            if (!ModelState.IsValid)
            {
                _validationService.HandleInvalidModelState(this);
                return RedirectToPage(new { id = groupId });
            }

            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            if (!_validationService.ValidateClientSelection(selectedClients, this))
            {
                return RedirectToPage(new { id = groupId });
            }

            try
            {
                var group = await _context.ClientGroups
                    .Where(g => g.Id == groupId && g.CoachId == coachId)
                    .FirstOrDefaultAsync();

                if (group == null)
                {
                    _validationService.SetError(this, "Group not found or you don't have access to it.");
                    return RedirectToPage("./Index");
                }

                // Get existing members
                var existingMembers = await _context.ClientGroupMembers
                    .Where(m => m.ClientGroupId == groupId)
                    .Select(m => m.CoachClientRelationshipId)
                    .ToListAsync();

                // Add new members
                int addedCount = 0;
                foreach (var relationshipId in selectedClients)
                {
                    // Skip if already a member
                    if (existingMembers.Contains(relationshipId))
                    {
                        continue;
                    }

                    // Verify relationship exists and belongs to this coach
                    var relationship = await _context.CoachClientRelationships
                        .Where(r => r.Id == relationshipId && r.CoachId == coachId && r.Status == RelationshipStatus.Active)
                        .FirstOrDefaultAsync();

                    if (relationship != null)
                    {
                        _context.ClientGroupMembers.Add(new ClientGroupMember
                        {
                            ClientGroupId = groupId,
                            CoachClientRelationshipId = relationshipId,
                            AddedDate = DateTime.UtcNow
                        });
                        addedCount++;
                    }
                }

                if (addedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Added {Count} clients to group {GroupId}", addedCount, groupId);
                    _validationService.SetSuccess(this, $"Successfully added {addedCount} client{(addedCount > 1 ? "s" : "")} to the group.");
                }
                else
                {
                    _validationService.SetInfo(this, "No new clients were added to the group.");
                }
            }
            catch (Exception ex)
            {
                _validationService.HandleException(_logger, ex, this,
                    "An error occurred while adding members to the group.",
                    $"adding members to group {groupId}");
            }

            return RedirectToPage(new { id = groupId });
        }

        public async Task<IActionResult> OnPostRemoveMemberAsync(int relationshipId, int groupId)
        {
            if (!ModelState.IsValid)
            {
                _validationService.HandleInvalidModelState(this);
                return RedirectToPage(new { id = groupId });
            }

            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            try
            {
                // Verify group belongs to coach
                var group = await _context.ClientGroups
                    .Where(g => g.Id == groupId && g.CoachId == coachId)
                    .FirstOrDefaultAsync();

                if (group == null)
                {
                    _validationService.SetError(this, "Group not found or you don't have access to it.");
                    return RedirectToPage("./Index");
                }

                // Find and remove the member
                var member = await _context.ClientGroupMembers
                    .Where(m => m.ClientGroupId == groupId && m.CoachClientRelationshipId == relationshipId)
                    .FirstOrDefaultAsync();

                if (member != null)
                {
                    _context.ClientGroupMembers.Remove(member);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Coach {CoachId} removed client {RelationshipId} from group {GroupId}", 
                        coachId, relationshipId, groupId);
                    _validationService.SetSuccess(this, "Client removed from group successfully.");
                }
                else
                {
                    _validationService.SetError(this, "Client not found in this group.");
                }
            }
            catch (Exception ex)
            {
                _validationService.HandleException(_logger, ex, this,
                    "An error occurred while removing the client from the group.",
                    $"removing client {relationshipId} from group {groupId}");
            }

            return RedirectToPage(new { id = groupId });
        }

        public async Task<IActionResult> OnPostDeleteGroupAsync(int groupId)
        {
            if (!ModelState.IsValid)
            {
                _validationService.HandleInvalidModelState(this);
                return RedirectToPage(new { id = groupId });
            }

            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Use a transaction to ensure all operations succeed or fail together
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var group = await _context.ClientGroups
                    .Where(g => g.Id == groupId && g.CoachId == coachId)
                    .FirstOrDefaultAsync();

                if (group == null)
                {
                    _validationService.SetError(this, "Group not found or you don't have access to it.");
                    return RedirectToPage("./Index");
                }

                // Delete all group members first
                var members = await _context.ClientGroupMembers
                    .Where(m => m.ClientGroupId == groupId)
                    .ToListAsync();

                _context.ClientGroupMembers.RemoveRange(members);
                
                // Delete the group
                _context.ClientGroups.Remove(group);
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogInformation("Coach {CoachId} deleted group {GroupId}: {GroupName} with {MemberCount} members", 
                    coachId, groupId, group.Name, members.Count);
                    
                _validationService.SetSuccess(this, $"Group '{group.Name}' deleted successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _validationService.HandleException(_logger, ex, this,
                    "An error occurred while deleting the group.",
                    $"deleting group {groupId}");
            }

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostAssignTemplateAsync(int groupId, int templateId, string assignmentName, string notes)
        {
            if (!ModelState.IsValid)
            {
                _validationService.HandleInvalidModelState(this);
                return RedirectToPage(new { id = groupId });
            }

            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Validate template assignment inputs
            if (!_validationService.ValidateRequiredField(assignmentName, "assignment name", this))
            {
                return RedirectToPage(new { id = groupId });
            }

            try
            {
                // Verify group belongs to coach
                var group = await _context.ClientGroups
                    .Where(g => g.Id == groupId && g.CoachId == coachId)
                    .FirstOrDefaultAsync();

                if (group == null)
                {
                    _validationService.SetError(this, "Group not found or you don't have access to it.");
                    return RedirectToPage("./Index");
                }

                // Verify template exists and coach has access to it
                var template = await _context.WorkoutTemplate
                    .Where(t => t.WorkoutTemplateId == templateId && (t.UserId == int.Parse(coachId) || t.IsPublic))
                    .FirstOrDefaultAsync();

                if (template == null)
                {
                    _validationService.SetError(this, "Template not found or you don't have access to it.");
                    return RedirectToPage(new { id = groupId });
                }

                // Get group members
                var members = await _context.ClientGroupMembers
                    .Where(m => m.ClientGroupId == groupId)
                    .Join(_context.CoachClientRelationships,
                        m => m.CoachClientRelationshipId,
                        r => r.Id,
                        (m, r) => new { Member = m, Relationship = r })
                    .Where(x => x.Relationship.Status == RelationshipStatus.Active)
                    .ToListAsync();

                if (!members.Any())
                {
                    _validationService.SetError(this, "This group has no active members to assign the template to.");
                    return RedirectToPage(new { id = groupId });
                }

                // Use a transaction to ensure all assignments succeed or fail together
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    int assignedCount = 0;
                    int alreadyAssignedCount = 0;
                    
                    foreach (var member in members)
                    {
                        // Check if client already has this template assigned
                        var existingAssignment = await _context.TemplateAssignments
                            .Where(a => a.ClientRelationshipId == member.Relationship.Id && 
                                   a.WorkoutTemplateId == templateId && 
                                   a.IsActive)
                            .FirstOrDefaultAsync();

                        if (existingAssignment != null)
                        {
                            alreadyAssignedCount++;
                            continue;
                        }

                        // Create new assignment
                        var assignment = new TemplateAssignment
                        {
                            WorkoutTemplateId = templateId,
                            ClientRelationshipId = member.Relationship.Id,
                            Name = assignmentName,
                            Notes = notes,
                            AssignedDate = DateTime.UtcNow,
                            IsActive = true,
                            ClientUserId = int.Parse(member.Relationship.ClientId),
                            CoachUserId = int.Parse(coachId)
                        };

                        _context.TemplateAssignments.Add(assignment);
                        assignedCount++;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    
                    _logger.LogInformation("Coach {CoachId} assigned template {TemplateId} to {AssignedCount} clients in group {GroupId}, {AlreadyAssignedCount} already had it", 
                        coachId, templateId, assignedCount, groupId, alreadyAssignedCount);

                    if (assignedCount > 0)
                    {
                        _validationService.SetSuccess(this, $"Template '{template.Name}' assigned to {assignedCount} client{(assignedCount > 1 ? "s" : "")} in group '{group.Name}'.");
                    }
                    else if (alreadyAssignedCount > 0)
                    {
                        _validationService.SetInfo(this, $"No new assignments created. All clients in this group already have the template '{template.Name}' assigned.");
                    }
                    else
                    {
                        _validationService.SetInfo(this, "No template assignments were created.");
                    }
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _validationService.HandleException(_logger, ex, this,
                        "An error occurred while assigning the template to the group.",
                        $"assigning template {templateId} to group {groupId}");
                }
            }
            catch (Exception ex)
            {
                _validationService.HandleException(_logger, ex, this,
                    "An error occurred while verifying the group or template.",
                    $"verifying group {groupId} or template {templateId}");
            }

            return RedirectToPage(new { id = groupId });
        }

        public class GroupMemberViewModel
        {
            public int ClientRelationshipId { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public DateTime? AddedDate { get; set; }
        }

        public class ClientViewModel
        {
            public int RelationshipId { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
        }

        public class TemplateViewModel
        {
            public int TemplateId { get; set; }
            public string Name { get; set; }
            public bool IsOwner { get; set; }
        }
    }
}