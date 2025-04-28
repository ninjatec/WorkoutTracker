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
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Users
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EditModel(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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
                AvailableRoles = allRoles
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

            // Check if the username is being changed
            if (user.UserName != UserEdit.UserName)
            {
                // Ensure username isn't already taken
                var existingUser = await _userManager.FindByNameAsync(UserEdit.UserName);
                if (existingUser != null && existingUser.Id != UserEdit.Id)
                {
                    ModelState.AddModelError("UserEdit.UserName", "This username is already taken.");
                    UserEdit.AvailableRoles = await _roleManager.Roles.ToListAsync();
                    return Page();
                }
                
                user.UserName = UserEdit.UserName;
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

            // Save user changes
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                
                UserEdit.AvailableRoles = await _roleManager.Roles.ToListAsync();
                return Page();
            }

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
                    ModelState.AddModelError(string.Empty, "Cannot remove Admin role from the last admin user.");
                    UserEdit.SelectedRoles = currentRoles.ToList(); // Reset to current roles
                    UserEdit.AvailableRoles = await _roleManager.Roles.ToListAsync();
                    return Page();
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
                    
                    UserEdit.AvailableRoles = await _roleManager.Roles.ToListAsync();
                    return Page();
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
                    
                    UserEdit.AvailableRoles = await _roleManager.Roles.ToListAsync();
                    return Page();
                }
            }

            // Redirect to user details page
            return RedirectToPage("./Details", new { id = user.Id });
        }
    }
}