using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;

namespace WorkoutTrackerWeb.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterFromInviteModel : PageModel
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUserStore<AppUser> _userStore;
        private readonly IUserEmailStore<AppUser> _emailStore;
        private readonly ILogger<RegisterFromInviteModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ICoachingService _coachingService;

        public RegisterFromInviteModel(
            UserManager<AppUser> userManager,
            IUserStore<AppUser> userStore,
            SignInManager<AppUser> signInManager,
            ILogger<RegisterFromInviteModel> logger,
            IEmailSender emailSender,
            WorkoutTrackerWebContext context,
            ICoachingService coachingService)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
            _coachingService = coachingService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }
        
        public string Email { get; set; }
        
        public string Token { get; set; }
        
        public int RelationshipId { get; set; }
        
        public string CoachName { get; set; }
        
        public string InvitationMessage { get; set; }
        
        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            [Display(Name = "Username")]
            public string UserName { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
            
            [Required]
            [StringLength(50, ErrorMessage = "Name must be between 2 and 50 characters.", MinimumLength = 2)]
            [Display(Name = "Your Name")]
            public string FullName { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string email, string token, int relationshipId, string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            
            // Store these values for the form
            Email = email;
            Token = token;
            RelationshipId = relationshipId;
            
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) || relationshipId <= 0)
            {
                ErrorMessage = "Invalid invitation link. Please contact your coach for a new invitation.";
                return RedirectToPage("./Login");
            }
            
            // Check if the email is already registered
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                // Email is already registered - redirect to AcceptCoachInvitation
                _logger.LogInformation("Existing user {Email} accessed invitation registration page - redirecting to appropriate page", email);
                ErrorMessage = $"The email '{email}' is already registered. Please log in to accept the invitation.";
                return RedirectToPage("./Login", new { ReturnUrl = $"/Identity/Account/AcceptCoachInvitation?relationshipId={relationshipId}&token={token}" });
            }
            
            // Check if the relationship exists and is valid
            var relationship = await _context.CoachClientRelationships
                .Include(r => r.Coach)
                .Include(r => r.Notes)
                .FirstOrDefaultAsync(r => r.Id == relationshipId && 
                                         r.InvitationToken == token && 
                                         r.InvitedEmail == email &&
                                         r.Status == RelationshipStatus.Pending);
                                         
            if (relationship == null)
            {
                ErrorMessage = "The invitation link is invalid or has expired. Please contact your coach for a new invitation.";
                return RedirectToPage("./Login");
            }
            
            // Check if the invitation has expired
            if (relationship.InvitationExpiryDate.HasValue && relationship.InvitationExpiryDate.Value < DateTime.UtcNow)
            {
                ErrorMessage = "This invitation has expired. Please contact your coach for a new invitation.";
                return RedirectToPage("./Login");
            }
            
            // Get coach name for display
            if (relationship.Coach != null)
            {
                CoachName = relationship.Coach.UserName;
            }
            
            // Get invitation message if available
            if (relationship.Notes != null && relationship.Notes.Count > 0)
            {
                var messageNote = relationship.Notes.FirstOrDefault(n => n.Content.StartsWith("Invitation message:"));
                if (messageNote != null)
                {
                    InvitationMessage = messageNote.Content.Replace("Invitation message: ", "");
                }
            }
            
            // Pre-populate the input model with the email
            Input = new InputModel { Email = email };
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string email, string token, int relationshipId, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            
            // Store these values in case we need to redisplay the form
            Email = email;
            Token = token;
            RelationshipId = relationshipId;
            
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) || relationshipId <= 0)
            {
                ErrorMessage = "Invalid invitation parameters. Please contact your coach for a new invitation.";
                return RedirectToPage("./Login");
            }
            
            // Validate the invitation before proceeding
            var relationship = await _context.CoachClientRelationships
                .Include(r => r.Coach)
                .FirstOrDefaultAsync(r => r.Id == relationshipId && 
                                         r.InvitationToken == token && 
                                         r.InvitedEmail == email &&
                                         r.Status == RelationshipStatus.Pending);
                                         
            if (relationship == null)
            {
                ErrorMessage = "The invitation link is invalid or has expired. Please contact your coach for a new invitation.";
                return RedirectToPage("./Login");
            }
            
            // Check if the invitation has expired
            if (relationship.InvitationExpiryDate.HasValue && relationship.InvitationExpiryDate.Value < DateTime.UtcNow)
            {
                ErrorMessage = "This invitation has expired. Please contact your coach for a new invitation.";
                return RedirectToPage("./Login");
            }
            
            // Pre-check: Verify if the email is already registered
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                // Email is already registered - provide a clear message and redirect to login
                _logger.LogInformation("Registration attempt with existing email: {Email}", email);
                ErrorMessage = $"The email '{email}' is already registered. Please use the login page and accept the invitation there.";
                return RedirectToPage("./Login");
            }
            
            // Check if the username is already taken
            var existingUserName = await _userManager.FindByNameAsync(Input.UserName);
            if (existingUserName != null)
            {
                ModelState.AddModelError("Input.UserName", "This username is already taken. Please choose a different one.");
                return Page();
            }
            
            if (ModelState.IsValid)
            {
                // Use a transaction to ensure all operations succeed or fail together
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    // Create the user account
                    var user = new AppUser
                    {
                        CreatedDate = DateTime.UtcNow,
                        LastModifiedDate = DateTime.UtcNow
                    };
                    
                    await _userStore.SetUserNameAsync(user, Input.UserName, CancellationToken.None);
                    await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                    var result = await _userManager.CreateAsync(user, Input.Password);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created a new account with password through coach invitation.");

                        // Get the user ID from the identity store
                        var userId = await _userManager.GetUserIdAsync(user);
                        
                        // Create the workout tracker user profile
                        var appUser = new Models.User
                        {
                            IdentityUserId = userId,
                            Name = Input.FullName
                        };
                        
                        _context.User.Add(appUser);
                        await _context.SaveChangesAsync();
                        
                        // Now update the relationship with the new user ID
                        relationship.ClientId = userId;
                        relationship.Status = RelationshipStatus.Active;
                        relationship.StartDate = DateTime.UtcNow;
                        relationship.LastModifiedDate = DateTime.UtcNow;
                        
                        _context.CoachClientRelationships.Update(relationship);
                        await _context.SaveChangesAsync();
                        
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        
                        // Automatically confirm the email since it was validated through the invitation
                        await _userManager.ConfirmEmailAsync(user, code);
                        
                        // Commit the transaction
                        await transaction.CommitAsync();
                        
                        // Sign in the user
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        
                        // Redirect to the client dashboard
                        return LocalRedirect(returnUrl);
                    }
                    
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception
                    _logger.LogError(ex, "Error during user registration from invitation");
                    
                    // Roll back the transaction
                    await transaction.RollbackAsync();
                    
                    // Add a friendly error message
                    ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again or contact support.");
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private IUserEmailStore<AppUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<AppUser>)_userStore;
        }
    }
}