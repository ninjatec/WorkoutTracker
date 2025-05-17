using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)]
            [DataType(DataType.Password)]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
                ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one number and one special character")]
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
            
            // Add detailed logging for debugging
            _logger.LogInformation("Checking if email exists: {Email}", email);
            
            try
            {
                // Run diagnostics and log the results
                var diagnosticResults = await DiagnoseEmailExistenceIssue(email);
                _logger.LogInformation("Email diagnostics:\n{DiagnosticResults}", diagnosticResults);
                
                // Check if the email is already registered with direct database queries for more accurate results
                var existingUserInIdentity = await _userManager.FindByEmailAsync(email);
                
                // Query the Users table directly using WorkoutTrackerWebContext since it now contains all Identity tables
                var identityUserIds = await _context.Users
                    .Where(au => au.Email == email)
                    .Select(au => au.Id)
                    .ToListAsync();
                    
                // Then query the application users with those IDs
                var existingUserInAppDB = identityUserIds.Any() 
                    ? await _context.User.FirstOrDefaultAsync(u => u.IdentityUserId != null && 
                                                                identityUserIds.Contains(u.IdentityUserId))
                    : null;
                
                _logger.LogInformation("Email check results - Identity: {ExistsInIdentity}, AppDB: {ExistsInAppDB}", 
                    existingUserInIdentity != null, existingUserInAppDB != null);
                
                // Check if the user truly exists in either database
                bool userExists = existingUserInIdentity != null || existingUserInAppDB != null;
                
                // Explicitly clear any existing user warning if the user doesn't exist
                if (!userExists)
                {
                    TempData.Remove("UserExistsWarning");
                    ErrorMessage = null;
                }
                
                // Only redirect if the user actually exists
                if (userExists)
                {
                    // Email is already registered in Identity - redirect to AcceptCoachInvitation
                    _logger.LogInformation("Existing user {Email} found in Identity store", email);
                    TempData["UserExistsWarning"] = $"The email '{email}' is already registered. Please log in to accept the invitation.";
                    return RedirectToPage("./Login", new { ReturnUrl = $"/Identity/Account/AcceptCoachInvitation?relationshipId={relationshipId}&token={token}" });
                }
                
                // Continue if user doesn't exist
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
                    _logger.LogWarning("Invalid relationship for invitation - Email: {Email}, Token: {Token}, RelationshipId: {RelationshipId}", 
                        email, token, relationshipId);
                    ErrorMessage = "The invitation link is invalid or has expired. Please contact your coach for a new invitation.";
                    return RedirectToPage("./Login");
                }
                
                // Check if the invitation has expired
                if (relationship.InvitationExpiryDate.HasValue && relationship.InvitationExpiryDate.Value < DateTime.UtcNow)
                {
                    _logger.LogInformation("Invitation expired - Email: {Email}, ExpiryDate: {ExpiryDate}", 
                        email, relationship.InvitationExpiryDate);
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
                
                // Log success for debugging
                _logger.LogInformation("Registration page loaded successfully for {Email}, user does not exist", email);
                
                return Page();
            }
            catch (Exception ex)
            {
                // Log any exceptions during the email check process
                _logger.LogError(ex, "Error checking email existence for {Email}", email);
                
                // In case of error, let's allow the user to continue
                // This is safer than falsely rejecting valid registrations
                TempData["EmailCheckError"] = "There was an issue verifying your email. Please continue with registration.";
                
                // Get the relationship details for the form
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
            
            // Add detailed logging for debugging
            _logger.LogInformation("Processing invitation registration for email: {Email}", email);
            
            // Validate the invitation before proceeding
            var relationship = await _context.CoachClientRelationships
                .Include(r => r.Coach)
                .FirstOrDefaultAsync(r => r.Id == relationshipId && 
                                         r.InvitationToken == token && 
                                         r.InvitedEmail == email &&
                                         r.Status == RelationshipStatus.Pending);
                                         
            if (relationship == null)
            {
                _logger.LogWarning("Invalid relationship for invitation - Email: {Email}, Token: {Token}, RelationshipId: {RelationshipId}", 
                    email, token, relationshipId);
                ErrorMessage = "The invitation link is invalid or has expired. Please contact your coach for a new invitation.";
                return RedirectToPage("./Login");
            }
            
            // Check if the invitation has expired
            if (relationship.InvitationExpiryDate.HasValue && relationship.InvitationExpiryDate.Value < DateTime.UtcNow)
            {
                _logger.LogInformation("Invitation expired - Email: {Email}, ExpiryDate: {ExpiryDate}", 
                    email, relationship.InvitationExpiryDate);
                ErrorMessage = "This invitation has expired. Please contact your coach for a new invitation.";
                return RedirectToPage("./Login");
            }
            
            // Enhanced email existence check with logging
            var existingUserInIdentity = await _userManager.FindByEmailAsync(email);
            if (existingUserInIdentity != null)
            {
                // Email is already registered - provide a clear message and redirect to login
                _logger.LogInformation("Registration attempt with existing email in Identity: {Email}", email);
                TempData["UserExistsWarning"] = $"The email '{email}' is already registered. Please use the login page and accept the invitation there.";
                return RedirectToPage("./Login", new { ReturnUrl = $"/Identity/Account/AcceptCoachInvitation?relationshipId={relationshipId}&token={token}" });
            }
            
            // Check if the username is already taken
            if (!string.IsNullOrEmpty(Input.UserName))
            {
                var existingUserName = await _userManager.FindByNameAsync(Input.UserName);
                if (existingUserName != null)
                {
                    _logger.LogInformation("Registration attempt with existing username: {Username}", Input.UserName);
                    ModelState.AddModelError("Input.UserName", "This username is already taken. Please choose a different one.");
                    return Page();
                }
            }
            
            if (ModelState.IsValid)
            {
                try
                {
                    // Use the execution strategy to properly handle retries with transactions
                    var executionStrategy = _context.Database.CreateExecutionStrategy();
                    
                    await executionStrategy.ExecuteAsync(async () => 
                    {
                        // Create a transaction that will work with the execution strategy
                        using var transaction = await _context.Database.BeginTransactionAsync();
                        try
                        {
                            // Step 1: Create the Identity user account (AspNetUsers table)
                            var user = new AppUser
                            {
                                CreatedDate = DateTime.UtcNow,
                                LastModifiedDate = DateTime.UtcNow
                            };
                            
                            await _userStore.SetUserNameAsync(user, Input.UserName, CancellationToken.None);
                            await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                            var result = await _userManager.CreateAsync(user, Input.Password);

                            if (!result.Succeeded)
                            {
                                foreach (var error in result.Errors)
                                {
                                    _logger.LogWarning("User creation error: {Error}", error.Description);
                                    ModelState.AddModelError(string.Empty, error.Description);
                                }
                                
                                // Roll back on validation errors
                                await transaction.RollbackAsync();
                                return;
                            }
                            
                            _logger.LogInformation("Identity user created successfully with ID: {UserId}", user.Id);

                            // Step 2: Get the user ID from the identity store and confirm it exists in AspNetUsers
                            var userId = await _userManager.GetUserIdAsync(user);
                            
                            // Verify the user was created in the Identity database
                            var createdUser = await _userManager.FindByIdAsync(userId);
                            if (createdUser == null)
                            {
                                _logger.LogError("Newly created Identity user not found in database. ID: {UserId}", userId);
                                ModelState.AddModelError(string.Empty, "Error creating user account. Please try again.");
                                await transaction.RollbackAsync();
                                return;
                            }
                            
                            _logger.LogInformation("Identity user verified in database with ID: {UserId}", userId);
                            
                            // Step 3: Create the application user (User table) with reference to the Identity user
                            var appUser = new Models.User
                            {
                                IdentityUserId = userId,
                                Name = Input.FullName
                            };
                            
                            _context.User.Add(appUser);
                            
                            // Save now to generate the UserId in the User table
                            await _context.SaveChangesAsync();
                            
                            _logger.LogInformation("Application user created with UserId: {AppUserId}, linked to Identity user: {IdentityUserId}", 
                                appUser.UserId, appUser.IdentityUserId);
                            
                            // Step 4: Now update the relationship with the Identity user ID
                            relationship.ClientId = userId; // This is the IdentityUserId, not the application UserId
                            relationship.Status = RelationshipStatus.Active;
                            relationship.StartDate = DateTime.UtcNow;
                            relationship.LastModifiedDate = DateTime.UtcNow;
                            
                            // Insert a verification step to check if the AppUser record exists in the AppUser table in WorkoutTrackerWebContext
                            // (not in ApplicationDbContext, since we need it in both places)
                            
                            // The AppUser table in WorkoutTrackerWebContext should mirror the AspNetUsers table in ApplicationDbContext
                            var appUserExists = await _context.Database.ExecuteSqlRawAsync(
                                "SELECT COUNT(1) FROM AppUser WHERE Id = {0}", userId) > 0;
                            
                            if (!appUserExists)
                            {
                                _logger.LogError("AppUser record not found in AppUser table for ID: {UserId}", userId);
                                // We need to create the AppUser record in the AppUser table to satisfy the foreign key constraint
                                await _context.Database.ExecuteSqlRawAsync(
                                    @"INSERT INTO AppUser (
                                        Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                                        PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, 
                                        TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, CreatedDate, LastModifiedDate
                                    ) 
                                    SELECT 
                                        Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
                                        PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed,
                                        TwoFactorEnabled, LockoutEnd, LockoutEnabled, AccessFailedCount, {1}, {2}
                                    FROM AspNetUsers 
                                    WHERE Id = {0}",
                                    userId, DateTime.UtcNow, DateTime.UtcNow);
                                    
                                _logger.LogInformation("Created missing AppUser record in AppUser table for ID: {UserId}", userId);
                            }
                            
                            _context.CoachClientRelationships.Update(relationship);
                            
                            // Log detailed information for debugging foreign key issues
                            _logger.LogInformation(
                                "Updating coach-client relationship - RelationshipId: {RelationshipId}, ClientId: {ClientId}, CoachId: {CoachId}",
                                relationship.Id, relationship.ClientId, relationship.CoachId);
                            
                            await _context.SaveChangesAsync();
                            
                            // Step 5: Confirm email automatically since it was validated through the invitation
                            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                            await _userManager.ConfirmEmailAsync(user, code);
                            
                            // Commit the transaction
                            await transaction.CommitAsync();
                            
                            // Step 6: Sign in the user
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            
                            _logger.LogInformation("User registration from invitation completed successfully - UserId: {UserId}, Email: {Email}", 
                                userId, email);
                            
                            // The transaction was successful, set a flag to redirect after the execution strategy completes
                            TempData["RegistrationSuccessful"] = true;
                        }
                        catch (DbUpdateException ex)
                        {
                            // Log specific database update exceptions with inner exception details
                            if (ex.InnerException != null)
                            {
                                _logger.LogError(ex, "Database update error during registration: {ErrorMessage}, Inner: {InnerError}", 
                                    ex.Message, ex.InnerException.Message);
                            }
                            else
                            {
                                _logger.LogError(ex, "Database update error during registration: {ErrorMessage}", ex.Message);
                            }
                            
                            // Roll back the transaction
                            await transaction.RollbackAsync();
                            
                            ModelState.AddModelError(string.Empty, "A database error occurred during registration. Please try again.");
                            
                            // Don't rethrow - we'll show the error to the user
                        }
                        catch (Exception ex)
                        {
                            // Log general exceptions
                            _logger.LogError(ex, "Transaction error during user registration - Email: {Email}", email);
                            
                            // Roll back the transaction
                            await transaction.RollbackAsync();
                            
                            ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again.");
                            
                            // Don't rethrow - we'll show the error to the user
                        }
                    });
                    
                    // If registration was successful, redirect to the dashboard
                    if (TempData.ContainsKey("RegistrationSuccessful"))
                    {
                        return LocalRedirect(returnUrl);
                    }
                }
                catch (Exception ex)
                {
                    // This catches any exceptions that occurred during the execution strategy
                    _logger.LogError(ex, "Error during user registration from invitation - Email: {Email}", email);
                    
                    // Add a friendly error message
                    ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again or contact support.");
                }
            }
            else
            {
                // Log validation errors
                var validationErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);
                
                _logger.LogInformation("Model validation failed during registration - Email: {Email}, Errors: {Errors}", 
                    email, string.Join(", ", validationErrors));
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        public async Task<IActionResult> OnPostResetEmailCheckAsync(string email, string token, int relationshipId)
        {
            _logger.LogWarning("Developer override: Forcing email check bypass for {Email}", email);
            
            // Store these values for the form
            Email = email;
            Token = token;
            RelationshipId = relationshipId;
            
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token) || relationshipId <= 0)
            {
                ErrorMessage = "Invalid invitation link. Please contact your coach for a new invitation.";
                return RedirectToPage("./Login");
            }
            
            // Check if the relationship exists and is valid without email existence checks
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
            
            // Log a special flag to mark this as an override
            _logger.LogInformation("EMAIL_CHECK_OVERRIDE: Developer bypassed email check for {Email}", email);
            
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
            
            TempData["EmailCheckOverride"] = true;
            
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

        // Add this helper method for debugging email issues
        private async Task<string> DiagnoseEmailExistenceIssue(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return "Email is null or empty";
            }
            
            var sb = new StringBuilder();
            sb.AppendLine($"Diagnosing email existence for: '{email}'");
            
            // Check with exact match
            var exactMatch = await _userManager.FindByEmailAsync(email);
            sb.AppendLine($"Exact match in UserManager: {(exactMatch != null ? "Found" : "Not found")}");
            
            // Check with lowercase
            var lowercaseMatch = await _userManager.FindByEmailAsync(email.ToLowerInvariant());
            sb.AppendLine($"Lowercase match in UserManager: {(lowercaseMatch != null ? "Found" : "Not found")}");
            
            // Direct query to Users table in WorkoutTrackerWebContext
            var directMatches = await _context.Users
                .Where(u => u.NormalizedEmail == email.ToUpperInvariant() || u.Email == email)
                .ToListAsync();
            
            sb.AppendLine($"Direct query matches: {directMatches.Count}");
            foreach (var match in directMatches)
            {
                sb.AppendLine($"- ID: {match.Id}, Email: {match.Email}, NormalizedEmail: {match.NormalizedEmail}");
            }
            
            // Check all users for similar emails
            var similarEmails = await _context.Users
                .Where(u => u.Email.Contains(email) || email.Contains(u.Email))
                .ToListAsync();
            
            if (similarEmails.Count > 0 && similarEmails.Count < 10) // Limit to avoid huge logs
            {
                sb.AppendLine($"Similar emails found: {similarEmails.Count}");
                foreach (var similar in similarEmails)
                {
                    sb.AppendLine($"- ID: {similar.Id}, Email: {similar.Email}, NormalizedEmail: {similar.NormalizedEmail}");
                }
            }
            
            return sb.ToString();
        }
    }
}