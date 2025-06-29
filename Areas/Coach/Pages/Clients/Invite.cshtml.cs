using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
using WorkoutTrackerWeb.Areas.Coach.Pages.ErrorHandling;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;
using WorkoutTrackerWeb.Services.Email;
using WorkoutTrackerWeb.Services.Validation;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Clients
{
    [Area("Coach")]
    [CoachAuthorize]
    public class InviteModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<InviteModel> _logger;
        private readonly ICoachingService _coachingService;
        private readonly CoachingValidationService _validationService;

        public InviteModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            IEmailSender emailSender,
            ILogger<InviteModel> logger,
            ICoachingService coachingService,
            CoachingValidationService validationService)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
            _coachingService = coachingService;
            _validationService = validationService;
        }

        [BindProperty(SupportsGet = false)]
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [DisplayName("Email Address")]
        public string ClientEmail { get; set; }

        [BindProperty]
        public string InvitationMessage { get; set; }

        [BindProperty]
        public int ExpiryDays { get; set; } = 14;

        [BindProperty]
        public List<string> Permissions { get; set; } = new List<string>() { 
            "canViewWorkouts", "canCreateWorkouts", "canEditWorkouts", 
            "canViewReports", "canCreateGoals" 
        };

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string StatusMessageType { get; set; }

        public IActionResult OnGet(string email = null)
        {
            // Initialize from query parameters or TempData
            if (!string.IsNullOrEmpty(email))
            {
                ClientEmail = email;
            }
            else if (TempData["ClientEmail"] != null)
            {
                ClientEmail = TempData["ClientEmail"].ToString();
            }

            if (TempData["InvitationMessage"] != null)
            {
                InvitationMessage = TempData["InvitationMessage"].ToString();
            }

            if (TempData["ExpiryDays"] != null && int.TryParse(TempData["ExpiryDays"].ToString(), out int days))
            {
                ExpiryDays = days;
            }

            if (TempData["Permissions"] != null && TempData["Permissions"] is List<string> permissions)
            {
                Permissions = permissions;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try 
            {
                // Log values from form for debugging
                _logger.LogInformation("Form values received - ClientEmail: {Email}, ExpiryDays: {ExpiryDays}, Permissions Count: {PermissionsCount}", 
                    ClientEmail, 
                    ExpiryDays, 
                    Permissions?.Count ?? 0);
                
                // Manual check for form values before ModelState validation
                if (string.IsNullOrEmpty(ClientEmail))
                {
                    _logger.LogWarning("Client email is null or empty during form submission");
                    
                    // Try to get it from the raw form data
                    if (Request.Form.ContainsKey("ClientEmail"))
                    {
                        string email = Request.Form["ClientEmail"].ToString();
                        _logger.LogInformation("Retrieved email from Request.Form: {Email}", email);
                        
                        if (!string.IsNullOrEmpty(email))
                        {
                            ClientEmail = email;
                        }
                    }
                    
                    // If still empty, add model error
                    if (string.IsNullOrEmpty(ClientEmail))
                    {
                        ModelState.AddModelError("ClientEmail", "Email address is required");
                        _validationService.HandleInvalidModelState(this);
                        return Page();
                    }
                }

                _logger.LogInformation("Starting client invitation process for email: {ClientEmail}", ClientEmail);
                
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState is invalid: {Errors}", 
                        string.Join(", ", ModelState.Where(x => x.Value.Errors.Any())
                            .Select(x => $"{x.Key}: {string.Join(", ", x.Value.Errors.Select(e => e.ErrorMessage))}")));
                            
                    _validationService.HandleInvalidModelState(this);
                    return Page();
                }

                var coachId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(coachId))
                {
                    return Forbid();
                }
                
                // Validate the invitation message
                if (!_validationService.ValidateInvitationMessage(InvitationMessage, this))
                {
                    return Page();
                }

                // Get the execution strategy from the database context
                _logger.LogInformation("Creating execution strategy for invitation process");
                var strategy = _context.Database.CreateExecutionStrategy();
                
                // Use the execution strategy for the database operations
                IActionResult result = null;
                await strategy.ExecuteAsync(async () => 
                {
                    // Using a transaction for creating the relationship to ensure atomicity
                    _logger.LogInformation("Beginning transaction for invitation process");
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    
                    try
                    {
                        // Convert permissions to List for compatibility with existing code
                        var permissionsList = Permissions ?? new List<string>();
                        _logger.LogInformation("Permissions selected: {Permissions}", string.Join(", ", permissionsList));

                        // Create or get the coach-client relationship using our new method
                        _logger.LogInformation("Calling InviteUserByEmailAsync with coach {CoachId}, email {Email}, expiry {ExpiryDays}", 
                            coachId, ClientEmail, ExpiryDays);
                            
                        var relationship = await _coachingService.InviteUserByEmailAsync(coachId, ClientEmail, ExpiryDays);
                        
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

                            // Commit the transaction
                            await transaction.CommitAsync();
                            _logger.LogInformation("Transaction committed successfully");

                            try
                            {
                                // In a real implementation, you'd send an actual email here with the invitation token
                                // The invitation URL depends on whether the user already exists or not
                                if (string.IsNullOrEmpty(relationship.ClientId))
                                {
                                    // This is a new user who needs to register
                                    var inviteUrl = Url.Page(
                                        "/Account/RegisterFromInvite",
                                        pageHandler: null,
                                        values: new { 
                                            area = "Identity", 
                                            email = relationship.InvitedEmail, 
                                            token = relationship.InvitationToken, 
                                            relationshipId = relationship.Id 
                                        },
                                        protocol: Request.Scheme);

                                    await _emailSender.SendEmailAsync(
                                        ClientEmail,
                                        "You've been invited to join WorkoutTracker",
                                        $"You've been invited to join WorkoutTracker as a client. " +
                                        $"<a href='{HtmlEncoder.Default.Encode(inviteUrl)}'>Click here to accept the invitation and set up your account.</a>");
                                }
                                else
                                {
                                    // This is an existing user who just needs to accept the invitation
                                    var inviteUrl = Url.Page(
                                        "/Account/AcceptCoachInvitation",
                                        pageHandler: null,
                                        values: new { 
                                            area = "Identity", 
                                            relationshipId = relationship.Id, 
                                            token = relationship.InvitationToken 
                                        },
                                        protocol: Request.Scheme);

                                    await _emailSender.SendEmailAsync(
                                        ClientEmail,
                                        "Coaching invitation",
                                        $"You have been invited to connect as a client on WorkoutTracker. " +
                                        $"<a href='{HtmlEncoder.Default.Encode(inviteUrl)}'>Click here to accept the invitation.</a>");
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log email error but don't fail the whole process - the invite is still created
                                _logger.LogError(ex, "Error sending invitation email to {Email}", ClientEmail);
                                _validationService.SetSuccess(this, $"Invitation created for {ClientEmail}, but there was an error sending the email.");
                                // Continue with the process
                            }

                            _validationService.SetSuccess(this, $"Invitation sent to {ClientEmail}.");
                            
                            // Add flag to switch to the Pending tab
                            TempData["ActiveTab"] = "pending";
                            
                            _logger.LogInformation("=== INVITATION PROCESS COMPLETED SUCCESSFULLY ===");
                        }
                        else
                        {
                            _logger.LogError("InviteUserByEmailAsync returned null - Failed to create relationship");
                            await transaction.RollbackAsync();
                            _validationService.SetError(this, $"Failed to create relationship with {ClientEmail}.");
                            _logger.LogInformation("=== INVITATION PROCESS FAILED ===");
                            result = Page();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during transaction for creating relationship");
                        await transaction.RollbackAsync();
                        _validationService.HandleException(_logger, ex, this,
                            "An error occurred while creating the client relationship.",
                            $"creating relationship with {ClientEmail}");
                        _logger.LogInformation("=== INVITATION PROCESS FAILED WITH EXCEPTION ===");
                        result = Page();
                        return;
                    }

                    result = RedirectToPage("./Index");
                });

                // Return the result that was set inside the execution strategy
                return result ?? RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                // Handle unhandled exceptions using our utility
                _validationService.HandleException(_logger, ex, this,
                    "An unexpected error occurred during the invitation process. Please try again.",
                    $"unhandled exception in client invitation");
                _logger.LogInformation("=== INVITATION PROCESS FAILED WITH UNHANDLED EXCEPTION ===");
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