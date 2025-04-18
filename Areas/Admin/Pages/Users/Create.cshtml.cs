using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Areas.Admin.ViewModels;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Users
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CreateModel(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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

            // Create the user with the provided email
            var user = new IdentityUser
            {
                UserName = UserCreate.Email,
                Email = UserCreate.Email,
                PhoneNumber = UserCreate.PhoneNumber,
                EmailConfirmed = UserCreate.EmailConfirmed
            };

            var result = await _userManager.CreateAsync(user, UserCreate.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                
                UserCreate.AvailableRoles = await _roleManager.Roles.ToListAsync();
                return Page();
            }

            // Add user to selected roles
            if (UserCreate.SelectedRoles != null && UserCreate.SelectedRoles.Count > 0)
            {
                result = await _userManager.AddToRolesAsync(user, UserCreate.SelectedRoles);

                if (!result.Succeeded)
                {
                    // If we can't add the roles, show the errors but don't delete the user
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    
                    UserCreate.AvailableRoles = await _roleManager.Roles.ToListAsync();
                    return Page();
                }
            }

            // Redirect to the user details page
            return RedirectToPage("./Details", new { id = user.Id });
        }
    }
}