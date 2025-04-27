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

        public IndexModel(
            ICoachingService coachingService,
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager)
        {
            _coachingService = coachingService;
            _context = context;
            _userManager = userManager;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string StatusMessageType { get; set; } = "Success";

        [BindProperty(SupportsGet = true)]
        public bool ShowInvite { get; set; }

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

            // Pending invitations (using placeholder data for demo)
            PendingClients = relationships
                .Where(r => r.Status == RelationshipStatus.Pending)
                .Select(r => new PendingInvitationViewModel
                {
                    Id = r.Id,
                    Email = r.Client?.Email ?? "pending@example.com",
                    InvitationDate = r.CreatedDate,
                    ExpiryDate = r.CreatedDate.AddDays(14)
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

        public async Task<IActionResult> OnPostInviteClientAsync(string clientEmail, string invitationMessage, int expiryDays, List<string> permissions)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Check if the user already exists
            var client = await _userManager.FindByEmailAsync(clientEmail);
            string clientId;

            if (client != null)
            {
                clientId = client.Id;
                
                // Check if a relationship already exists between coach and client
                var existingRelationship = await _coachingService.GetCoachClientRelationshipAsync(coachId, clientId);
                if (existingRelationship != null)
                {
                    if (existingRelationship.Status == RelationshipStatus.Active)
                    {
                        StatusMessage = $"Error: {clientEmail} is already your active client.";
                        StatusMessageType = "Error";
                        return RedirectToPage();
                    }
                    else if (existingRelationship.Status == RelationshipStatus.Pending)
                    {
                        StatusMessage = $"Error: An invitation to {clientEmail} is already pending.";
                        StatusMessageType = "Error";
                        return RedirectToPage();
                    }
                }
            }
            else
            {
                // In a real implementation, you'd need to create a temporary user record or store the invitation somewhere else
                StatusMessage = $"Note: {clientEmail} is not a registered user. An invitation email will be sent.";
                clientId = $"pending_{Guid.NewGuid()}"; // Use a placeholder for now
            }

            // Create the coach-client relationship
            var relationship = await _coachingService.CreateCoachClientRelationshipAsync(coachId, clientId);
            if (relationship != null)
            {
                // Set expiry
                if (expiryDays > 0)
                {
                    // In a real implementation, you'd store this in the database
                }

                // Set permissions based on form selection
                if (relationship.Permissions != null)
                {
                    // In a real implementation, you'd update permissions here
                    // based on the selected values in the form
                }

                // In a real implementation, you'd send an actual email here
                StatusMessage = $"Success: Invitation sent to {clientEmail}.";
                StatusMessageType = "Success";
            }
            else
            {
                StatusMessage = $"Error: Failed to create relationship with {clientEmail}.";
                StatusMessageType = "Error";
            }

            return RedirectToPage();
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

        public async Task<IActionResult> OnPostCreateGroupAsync(string groupName, string groupDescription, List<int> selectedClients)
        {
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