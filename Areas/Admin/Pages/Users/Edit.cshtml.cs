using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Areas.Admin.ViewModels;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Users
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly WorkoutTrackerWebContext _applicationDbContext;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            WorkoutTrackerWebContext applicationDbContext,
            ILogger<EditModel> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _applicationDbContext = applicationDbContext;
            _logger = logger;
        }

        [BindProperty]
        public UserEditViewModel UserEdit { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Get corresponding application user if it exists
            var appUser = await _applicationDbContext.User
                .FirstOrDefaultAsync(u => u.IdentityUserId == user.Id);
            
            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            UserEdit = new UserEditViewModel
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnabled = user.LockoutEnabled,
                SelectedRoles = userRoles.ToList(),
                AvailableRoles = allRoles,
                // Include the application user data if it exists
                Name = appUser?.Name ?? user.UserName,
                // Store the application UserId if it exists for reference during post
                ApplicationUserId = appUser?.UserId
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Reload available roles if model validation fails
                UserEdit.AvailableRoles = await _roleManager.Roles.ToListAsync();
                return Page();
            }

            var user = await _userManager.FindByIdAsync(UserEdit.Id);

            if (user == null)
            {
                return NotFound();
            }

            // Handle username change
            if (user.UserName != UserEdit.UserName)
            {
                // Check if the new username is already taken by another user
                var existingUser = await _userManager.FindByNameAsync(UserEdit.UserName);
                if (existingUser != null && existingUser.Id != UserEdit.Id)
                {
                    ModelState.AddModelError("UserEdit.UserName", "This username is already taken.");
                    UserEdit.AvailableRoles = await _roleManager.Roles.ToListAsync();
                    return Page();
                }
                
                // Update the username and normalized username directly to preserve the full email address
                user.UserName = UserEdit.UserName;
                user.NormalizedUserName = UserEdit.UserName.ToUpperInvariant();
            }

            // Check if email is being changed
            if (user.Email != UserEdit.Email)
            {
                // Ensure email isn't already taken
                var existingUser = await _userManager.FindByEmailAsync(UserEdit.Email);
                if (existingUser != null && existingUser.Id != UserEdit.Id)
                {
                    ModelState.AddModelError("UserEdit.Email", "This email is already registered.");
                    UserEdit.AvailableRoles = await _roleManager.Roles.ToListAsync();
                    return Page();
                }
                
                user.Email = UserEdit.Email;
            }

            // Update user properties
            user.PhoneNumber = UserEdit.PhoneNumber;
            user.EmailConfirmed = UserEdit.EmailConfirmed;
            user.TwoFactorEnabled = UserEdit.TwoFactorEnabled;
            user.LockoutEnabled = UserEdit.LockoutEnabled;
            user.LastModifiedDate = DateTime.UtcNow;  // Update the LastModifiedDate

            // Use execution strategy for database operations
            var executionStrategy = _applicationDbContext.Database.CreateExecutionStrategy();
            
            await executionStrategy.ExecuteAsync(async () => 
            {
                // Use a transaction to ensure data consistency across both databases
                using var transaction = await _applicationDbContext.Database.BeginTransactionAsync();
                try
                {
                    // Save user changes to Identity database
                    var result = await _userManager.UpdateAsync(user);

                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        throw new ApplicationException("Failed to update Identity user");
                    }

                    // Now update or create the application user in the User table
                    var appUser = await _applicationDbContext.User
                        .FirstOrDefaultAsync(u => u.IdentityUserId == user.Id);

                    if (appUser != null)
                    {
                        // Update existing application user
                        appUser.Name = UserEdit.Name ?? user.UserName;
                        _applicationDbContext.User.Update(appUser);
                        _logger.LogInformation("Updating application user record with ID: {AppUserId}, linked to Identity user: {IdentityUserId}", 
                            appUser.UserId, appUser.IdentityUserId);
                    }
                    else
                    {
                        // Create a new application user if one doesn't exist
                        appUser = new Models.User
                        {
                            IdentityUserId = user.Id,
                            Name = UserEdit.Name ?? user.UserName
                        };
                        _applicationDbContext.User.Add(appUser);
                        _logger.LogInformation("Creating new application user record linked to Identity user: {IdentityUserId}", user.Id);
                    }

                    // Save changes to application database
                    await _applicationDbContext.SaveChangesAsync();

                    // Update roles
                    // Get current roles
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    
                    // Roles to remove (in current but not in selected)
                    var rolesToRemove = currentRoles.Where(r => !UserEdit.SelectedRoles.Contains(r)).ToList();
                    
                    // Roles to add (in selected but not in current)
                    var rolesToAdd = UserEdit.SelectedRoles.Where(r => !currentRoles.Contains(r)).ToList();

                    // Special case: prevent removing Admin role from the last admin
                    if (rolesToRemove.Contains("Admin"))
                    {
                        var admins = await _userManager.GetUsersInRoleAsync("Admin");
                        if (admins.Count <= 1 && admins.Any(a => a.Id == user.Id))
                        {
                            throw new ApplicationException("Cannot remove Admin role from the last admin user.");
                        }
                    }

                    // Remove roles
                    if (rolesToRemove.Any())
                    {
                        result = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                        if (!result.Succeeded)
                        {
                            foreach (var error in result.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                            throw new ApplicationException("Failed to remove roles");
                        }
                    }

                    // Add roles
                    if (rolesToAdd.Any())
                    {
                        result = await _userManager.AddToRolesAsync(user, rolesToAdd);
                        if (!result.Succeeded)
                        {
                            foreach (var error in result.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                            throw new ApplicationException("Failed to add roles");
                        }
                    }

                    // Commit the transaction
                    await transaction.CommitAsync();
                    _logger.LogInformation("User edit transaction committed successfully for user ID: {UserId}", user.Id);
                }
                catch (Exception ex)
                {
                    // Roll back transaction and log error
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error updating user: {UserId}, {ErrorMessage}", user.Id, ex.Message);
                    
                    if (ex is ApplicationException)
                    {
                        // These are our own validation errors that have already been added to ModelState
                        UserEdit.AvailableRoles = await _roleManager.Roles.ToListAsync();
                        return;
                    }
                    
                    // Add a generic error if it's not our own exception
                    ModelState.AddModelError(string.Empty, "An error occurred while updating the user. Please try again.");
                    UserEdit.AvailableRoles = await _roleManager.Roles.ToListAsync();
                    return;
                }
            });

            // If we got to this point with valid ModelState, the operation was successful
            if (ModelState.IsValid)
            {
                // Redirect to user details page
                return RedirectToPage("./Details", new { id = user.Id });
            }
            else
            {
                // Something went wrong, reload the form
                UserEdit.AvailableRoles = await _roleManager.Roles.ToListAsync();
                return Page();
            }
        }
    }
}