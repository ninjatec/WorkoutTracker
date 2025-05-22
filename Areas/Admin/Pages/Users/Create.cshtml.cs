using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Areas.Admin.ViewModels;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Users
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly WorkoutTrackerWebContext _applicationDbContext;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            WorkoutTrackerWebContext applicationDbContext,
            ILogger<CreateModel> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _applicationDbContext = applicationDbContext;
            _logger = logger;
        }

        [BindProperty]
        public UserCreateViewModel UserCreate { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            UserCreate = new UserCreateViewModel
            {
                EmailConfirmed = true, // Default to email confirmed for admin-created accounts
                AvailableRoles = await _roleManager.Roles.ToListAsync()
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                UserCreate.AvailableRoles = await _roleManager.Roles.ToListAsync();
                return Page();
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(UserCreate.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("UserCreate.Email", "This email is already registered.");
                UserCreate.AvailableRoles = await _roleManager.Roles.ToListAsync();
                return Page();
            }

            // Check if username already exists
            if (!string.IsNullOrEmpty(UserCreate.UserName))
            {
                var existingUserName = await _userManager.FindByNameAsync(UserCreate.UserName);
                if (existingUserName != null)
                {
                    ModelState.AddModelError("UserCreate.UserName", "This username is already taken.");
                    UserCreate.AvailableRoles = await _roleManager.Roles.ToListAsync();
                    return Page();
                }
            }

            // Use execution strategy for database operations
            var executionStrategy = _applicationDbContext.Database.CreateExecutionStrategy();

            await executionStrategy.ExecuteAsync(async () =>
            {
                // Use a transaction to ensure data consistency across both databases
                using var transaction = await _applicationDbContext.Database.BeginTransactionAsync();
                try
                {
                    // Step 1: Create the Identity user (AspNetUsers table)
                    var user = new AppUser
                    {
                        UserName = UserCreate.UserName,
                        Email = UserCreate.Email,
                        PhoneNumber = UserCreate.PhoneNumber,
                        EmailConfirmed = UserCreate.EmailConfirmed,
                        CreatedDate = DateTime.UtcNow,
                        LastModifiedDate = DateTime.UtcNow
                    };

                    var result = await _userManager.CreateAsync(user, UserCreate.Password);

                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        throw new ApplicationException("Failed to create Identity user");
                    }

                    // Get the newly created user ID
                    var userId = await _userManager.GetUserIdAsync(user);
                    _logger.LogInformation("Created new Identity user with ID: {UserId}", userId);

                    // Step 2: Create the application user in the User table
                    var appUser = new WorkoutTrackerWeb.Models.User
                    {
                        IdentityUserId = userId,
                        Name = UserCreate.UserName // Use username as name or add a separate field for name
                    };

                    _applicationDbContext.User.Add(appUser);
                    await _applicationDbContext.SaveChangesAsync();
                    
                    _logger.LogInformation("Created application user record with UserId: {AppUserId}, linked to Identity user: {IdentityUserId}",
                        appUser.UserId, appUser.IdentityUserId);

                    // Step 3: Add the user to selected roles
                    if (UserCreate.SelectedRoles != null && UserCreate.SelectedRoles.Count > 0)
                    {
                        result = await _userManager.AddToRolesAsync(user, UserCreate.SelectedRoles);

                        if (!result.Succeeded)
                        {
                            foreach (var error in result.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                            throw new ApplicationException("Failed to assign roles to user");
                        }
                    }

                    // Commit the transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("User creation transaction committed successfully for user ID: {UserId}", user.Id);
                    
                    // Store the user ID for the redirect
                    TempData["CreatedUserId"] = user.Id;
                }
                catch (Exception ex)
                {
                    // Roll back transaction and log error
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error creating user: {ErrorMessage}", ex.Message);
                    
                    if (ex is ApplicationException)
                    {
                        // These are our own validation errors that have already been added to ModelState
                        UserCreate.AvailableRoles = await _roleManager.Roles.ToListAsync();
                        return;
                    }
                    
                    // Add a generic error if it's not our own exception
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the user. Please try again.");
                    UserCreate.AvailableRoles = await _roleManager.Roles.ToListAsync();
                    return;
                }
            });

            // If we got to this point with valid ModelState, the operation was successful
            if (ModelState.IsValid && TempData.ContainsKey("CreatedUserId"))
            {
                // Redirect to the user details page
                return RedirectToPage("./Details", new { id = TempData["CreatedUserId"] });
            }
            else
            {
                // Something went wrong, reload the form
                UserCreate.AvailableRoles = await _roleManager.Roles.ToListAsync();
                return Page();
            }
        }
    }
}