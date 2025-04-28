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
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;

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

        public GroupModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ICoachingService coachingService,
            ILogger<GroupModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _coachingService = coachingService;
            _logger = logger;
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
                StatusMessage = "Error: Group not found.";
                StatusMessageType = "Error";
                return RedirectToPage("./Index");
            }

            // Set group properties
            GroupId = group.Id;
            GroupName = group.Name;
            GroupDescription = group.Description;
            ColorCode = group.ColorCode ?? "#0d6efd"; // Default to blue if not set
            CreatedDate = group.CreatedDate;

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
                        Name = client.UserName.Split('@')[0],  // Use username as name or better use a profile name if available
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

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateGroupAsync(int groupId, string groupName, string groupDescription, string colorCode)
        {
            if (!ModelState.IsValid)
            {
                StatusMessage = "Error: " + string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                StatusMessageType = "Error";
                return RedirectToPage(new { id = groupId });
            }

            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Validate inputs
            if (string.IsNullOrEmpty(groupName))
            {
                StatusMessage = "Error: Group name is required.";
                StatusMessageType = "Error";
                return RedirectToPage(new { id = groupId });
            }

            if (groupName.Length > 100)
            {
                StatusMessage = "Error: Group name cannot exceed 100 characters.";
                StatusMessageType = "Error";
                return RedirectToPage(new { id = groupId });
            }

            if (groupDescription?.Length > 500)
            {
                StatusMessage = "Error: Group description cannot exceed 500 characters.";
                StatusMessageType = "Error";
                return RedirectToPage(new { id = groupId });
            }

            var group = await _context.ClientGroups
                .Where(g => g.Id == groupId && g.CoachId == coachId)
                .FirstOrDefaultAsync();

            if (group == null)
            {
                _logger.LogWarning("Group {GroupId} not found for coach {CoachId}", groupId, coachId);
                StatusMessage = "Error: Group not found.";
                StatusMessageType = "Error";
                return RedirectToPage("./Index");
            }

            // Check for duplicate name (excluding current group)
            var existingGroup = await _context.ClientGroups
                .Where(g => g.CoachId == coachId && g.Name == groupName && g.Id != groupId)
                .FirstOrDefaultAsync();
                
            if (existingGroup != null)
            {
                StatusMessage = $"Error: A group named '{groupName}' already exists.";
                StatusMessageType = "Error";
                return RedirectToPage(new { id = groupId });
            }

            try
            {
                // Update group details
                group.Name = groupName;
                group.Description = groupDescription;
                group.ColorCode = colorCode;
                group.LastModifiedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Coach {CoachId} updated group {GroupId}: {GroupName}", 
                    coachId, groupId, groupName);
                    
                StatusMessage = "Group details updated successfully.";
                StatusMessageType = "Success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group {GroupId}: {Message}", groupId, ex.Message);
                StatusMessage = $"Error updating group: {ex.Message}";
                StatusMessageType = "Error";
            }

            return RedirectToPage(new { id = groupId });
        }

        public async Task<IActionResult> OnPostAddMembersAsync(int groupId, List<int> selectedClients)
        {
            if (!ModelState.IsValid)
            {
                StatusMessage = "Error: " + string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                StatusMessageType = "Error";
                return RedirectToPage(new { id = groupId });
            }

            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            if (selectedClients == null || !selectedClients.Any())
            {
                StatusMessage = "Error: No clients selected.";
                StatusMessageType = "Error";
                return RedirectToPage(new { id = groupId });
            }

            var group = await _context.ClientGroups
                .Where(g => g.Id == groupId && g.CoachId == coachId)
                .FirstOrDefaultAsync();

            if (group == null)
            {
                _logger.LogWarning("Group {GroupId} not found for coach {CoachId}", groupId, coachId);
                StatusMessage = "Error: Group not found.";
                StatusMessageType = "Error";
                return RedirectToPage("./Index");
            }

            // Get existing members
            var existingMembers = await _context.ClientGroupMembers
                .Where(m => m.ClientGroupId == groupId)
                .Select(m => m.CoachClientRelationshipId)
                .ToListAsync();

            try
            {
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
                    StatusMessage = $"Successfully added {addedCount} client{(addedCount > 1 ? "s" : "")} to the group.";
                    StatusMessageType = "Success";
                }
                else
                {
                    StatusMessage = "No new clients were added to the group.";
                    StatusMessageType = "Info";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding members to group {GroupId}: {Message}", groupId, ex.Message);
                StatusMessage = $"Error adding members to group: {ex.Message}";
                StatusMessageType = "Error";
            }

            return RedirectToPage(new { id = groupId });
        }

        public async Task<IActionResult> OnPostRemoveMemberAsync(int relationshipId, int groupId)
        {
            if (!ModelState.IsValid)
            {
                StatusMessage = "Error: " + string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                StatusMessageType = "Error";
                return RedirectToPage(new { id = groupId });
            }

            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Verify group belongs to coach
            var group = await _context.ClientGroups
                .Where(g => g.Id == groupId && g.CoachId == coachId)
                .FirstOrDefaultAsync();

            if (group == null)
            {
                _logger.LogWarning("Group {GroupId} not found for coach {CoachId}", groupId, coachId);
                StatusMessage = "Error: Group not found.";
                StatusMessageType = "Error";
                return RedirectToPage("./Index");
            }

            try
            {
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
                    StatusMessage = "Client removed from group successfully.";
                    StatusMessageType = "Success";
                }
                else
                {
                    _logger.LogWarning("Client {RelationshipId} not found in group {GroupId}", relationshipId, groupId);
                    StatusMessage = "Error: Client not found in this group.";
                    StatusMessageType = "Error";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing client {RelationshipId} from group {GroupId}: {Message}", 
                    relationshipId, groupId, ex.Message);
                StatusMessage = $"Error removing client from group: {ex.Message}";
                StatusMessageType = "Error";
            }

            return RedirectToPage(new { id = groupId });
        }

        public async Task<IActionResult> OnPostDeleteGroupAsync(int groupId)
        {
            if (!ModelState.IsValid)
            {
                StatusMessage = "Error: " + string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                StatusMessageType = "Error";
                return RedirectToPage(new { id = groupId });
            }

            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            var group = await _context.ClientGroups
                .Where(g => g.Id == groupId && g.CoachId == coachId)
                .FirstOrDefaultAsync();

            if (group == null)
            {
                _logger.LogWarning("Group {GroupId} not found for coach {CoachId}", groupId, coachId);
                StatusMessage = "Error: Group not found.";
                StatusMessageType = "Error";
                return RedirectToPage("./Index");
            }

            // Use a transaction to ensure all operations succeed or fail together
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if there are any template assignments for this group's members
                var groupMemberRelationships = await _context.ClientGroupMembers
                    .Where(m => m.ClientGroupId == groupId)
                    .Select(m => m.CoachClientRelationshipId)
                    .ToListAsync();

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
                    
                StatusMessage = $"Group '{group.Name}' deleted successfully.";
                StatusMessageType = "Success";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting group {GroupId}: {Message}", groupId, ex.Message);
                StatusMessage = $"Error deleting group: {ex.Message}";
                StatusMessageType = "Error";
            }

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostAssignTemplateAsync(int groupId, int templateId, string assignmentName, string notes)
        {
            if (!ModelState.IsValid)
            {
                StatusMessage = "Error: " + string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                StatusMessageType = "Error";
                return RedirectToPage(new { id = groupId });
            }

            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Validate inputs
            if (string.IsNullOrEmpty(assignmentName))
            {
                StatusMessage = "Error: Assignment name is required.";
                StatusMessageType = "Error";
                return RedirectToPage(new { id = groupId });
            }

            // Verify group belongs to coach
            var group = await _context.ClientGroups
                .Where(g => g.Id == groupId && g.CoachId == coachId)
                .FirstOrDefaultAsync();

            if (group == null)
            {
                _logger.LogWarning("Group {GroupId} not found for coach {CoachId}", groupId, coachId);
                StatusMessage = "Error: Group not found.";
                StatusMessageType = "Error";
                return RedirectToPage("./Index");
            }

            // Verify template exists and coach has access to it
            var template = await _context.WorkoutTemplate
                .Where(t => t.WorkoutTemplateId == templateId && (t.UserId == int.Parse(coachId) || t.IsPublic))
                .FirstOrDefaultAsync();

            if (template == null)
            {
                _logger.LogWarning("Template {TemplateId} not found or not accessible by coach {CoachId}", templateId, coachId);
                StatusMessage = "Error: Template not found or you don't have access to it.";
                StatusMessageType = "Error";
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
                _logger.LogWarning("Group {GroupId} has no active members to assign templates to", groupId);
                StatusMessage = "Error: This group has no active members to assign the template to.";
                StatusMessageType = "Error";
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
                    StatusMessage = $"Template '{template.Name}' assigned to {assignedCount} client{(assignedCount > 1 ? "s" : "")} in group '{group.Name}'.";
                    StatusMessageType = "Success";
                }
                else if (alreadyAssignedCount > 0)
                {
                    StatusMessage = $"No new assignments created. All clients in this group already have the template '{template.Name}' assigned.";
                    StatusMessageType = "Info";
                }
                else
                {
                    StatusMessage = "No template assignments were created.";
                    StatusMessageType = "Info";
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error assigning template {TemplateId} to group {GroupId}: {Message}", templateId, groupId, ex.Message);
                StatusMessage = $"Error assigning template: {ex.Message}";
                StatusMessageType = "Error";
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