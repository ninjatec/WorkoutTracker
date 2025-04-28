using System;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Services.Email;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Clients
{
    [CoachAuthorize]
    public class InviteModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<InviteModel> _logger;

        public InviteModel(
            WorkoutTrackerWebContext context,
            UserManager<IdentityUser> userManager,
            IEmailSender emailSender,
            ILogger<InviteModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [BindProperty]
        public string ClientEmail { get; set; }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostInviteClient()
        {
            try 
            {
                _logger.LogInformation("Starting client invitation process for email: {ClientEmail}", ClientEmail);
                
                if (string.IsNullOrEmpty(ClientEmail))
                {
                    _logger.LogWarning("Client invitation failed: Email is empty");
                    ModelState.AddModelError("", "Email address is required.");
                    return Page();
                }

                var coach = await _userManager.GetUserAsync(User);
                if (coach == null)
                {
                    _logger.LogError("Client invitation failed: Coach user not found");
                    ModelState.AddModelError("", "Unable to identify the current coach.");
                    return Page();
                }
                
                _logger.LogInformation("Coach identified: {CoachId} ({CoachEmail})", coach.Id, coach.Email);

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(ClientEmail);
                _logger.LogInformation("Existing user check result: {UserExists}", existingUser != null);

                IdentityUser clientUser;
                bool isNewUser = false;

                if (existingUser == null)
                {
                    _logger.LogInformation("Creating new user account for client {ClientEmail}", ClientEmail);
                    // Create new user account
                    var tempPassword = GenerateRandomPassword();
                    clientUser = new IdentityUser
                    {
                        UserName = ClientEmail,
                        Email = ClientEmail,
                        EmailConfirmed = false
                    };

                    var result = await _userManager.CreateAsync(clientUser, tempPassword);
                    if (!result.Succeeded)
                    {
                        _logger.LogError("Failed to create client user account: {Errors}", 
                            string.Join(", ", result.Errors.Select(e => e.Description)));
                        ModelState.AddModelError("", "Failed to create user account: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                        return Page();
                    }

                    // Add to client role
                    await _userManager.AddToRoleAsync(clientUser, "Client");
                    isNewUser = true;
                    _logger.LogInformation("New client user created with ID: {ClientId}", clientUser.Id);
                }
                else
                {
                    clientUser = existingUser;
                    _logger.LogInformation("Using existing client user with ID: {ClientId}", clientUser.Id);
                }

                // Check if relation already exists
                _logger.LogInformation("Checking if relationship already exists between coach {CoachId} and client {ClientId}", coach.Id, clientUser.Id);
                var existingRelation = await _context.CoachClientRelationships
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.CoachId == coach.Id && r.ClientId == clientUser.Id);

                if (existingRelation != null)
                {
                    _logger.LogWarning("Relationship already exists with status: {Status}", existingRelation.Status);
                    
                    if (existingRelation.Status == RelationshipStatus.Active)
                    {
                        ModelState.AddModelError("", "This client is already in your client list.");
                        return Page();
                    }
                    else if (existingRelation.Status == RelationshipStatus.Pending)
                    {
                        ModelState.AddModelError("", "You have already sent an invitation to this client.");
                        return Page();
                    }
                    else if (existingRelation.Status == RelationshipStatus.Rejected)
                    {
                        // Allow resending if previously declined
                        _logger.LogInformation("Relationship was previously rejected, creating new invitation");
                    }
                }

                // Create or update relationship
                _logger.LogInformation("Creating new coach-client relationship");
                var relationship = new CoachClientRelationship
                {
                    CoachId = coach.Id,
                    ClientId = clientUser.Id,
                    CreatedDate = DateTime.UtcNow,
                    LastModifiedDate = DateTime.UtcNow,
                    Status = RelationshipStatus.Pending,
                    InvitationToken = Guid.NewGuid().ToString(),
                    InvitationExpiryDate = DateTime.UtcNow.AddDays(7)
                };

                _logger.LogInformation("Generated invitation token: {Token}", relationship.InvitationToken);

                // Create default permissions
                _logger.LogInformation("Creating default permissions for the relationship");
                var permissions = new CoachClientPermission
                {
                    CanViewWorkouts = true,
                    CanCreateWorkouts = true,
                    CanModifyWorkouts = true,
                    CanDeleteWorkouts = false,
                    CanEditWorkouts = true,
                    CanMessage = true,
                    CanViewReports = true,
                    CanViewPersonalInfo = true,
                    CanCreateTemplates = true,
                    CanAssignTemplates = true,
                    CanCreateGoals = true,
                    LastModifiedDate = DateTime.UtcNow
                };

                relationship.Permissions = permissions;

                // Save to database
                try 
                {
                    _logger.LogInformation("Attempting to add relationship to database");
                    _context.CoachClientRelationships.Add(relationship);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully saved relationship with ID: {RelationshipId}", relationship.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save relationship to database");
                    ModelState.AddModelError("", "Failed to create relationship: " + ex.Message);
                    return Page();
                }

                // Send invitation email
                if (isNewUser)
                {
                    _logger.LogInformation("Sending new user invitation email to {ClientEmail}", ClientEmail);
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(clientUser);
                    var inviteUrl = Url.Page(
                        "/Account/RegisterFromInvite",
                        pageHandler: null,
                        values: new { area = "Identity", userId = clientUser.Id, token, relationshipToken = relationship.InvitationToken },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(
                        ClientEmail,
                        "You've been invited to join WorkoutTracker",
                        $"You've been invited to join WorkoutTracker as a client. " +
                        $"<a href='{HtmlEncoder.Default.Encode(inviteUrl)}'>Click here to accept the invitation and set up your account.</a>");
                }
                else
                {
                    _logger.LogInformation("Sending existing user invitation email to {ClientEmail}", ClientEmail);
                    var inviteUrl = Url.Page(
                        "/Account/AcceptCoachInvitation",
                        pageHandler: null,
                        values: new { area = "Identity", relationshipId = relationship.Id, token = relationship.InvitationToken },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(
                        ClientEmail,
                        "Coaching invitation",
                        $"You have been invited to connect as a client on WorkoutTracker. " +
                        $"<a href='{HtmlEncoder.Default.Encode(inviteUrl)}'>Click here to accept the invitation.</a>");
                }

                _logger.LogInformation("Client invitation process completed successfully for {ClientEmail}", ClientEmail);
                TempData["StatusMessage"] = "Invitation sent successfully to " + ClientEmail;
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in OnPostInviteClient");
                ModelState.AddModelError("", "An unexpected error occurred: " + ex.Message);
                return Page();
            }
        }

        private string GenerateRandomPassword()
        {
            // Generate a secure random password for initial account creation
            // This is just temporary as the user will reset it through the invitation link
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}