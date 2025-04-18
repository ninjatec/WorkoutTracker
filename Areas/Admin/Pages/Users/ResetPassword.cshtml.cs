using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkoutTrackerWeb.Areas.Admin.ViewModels;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Users
{
    [Authorize(Roles = "Admin")]
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public ResetPasswordModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public ResetPasswordViewModel ResetPassword { get; set; }

        public string UserEmail { get; set; }

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

            UserEmail = user.Email;

            ResetPassword = new ResetPasswordViewModel
            {
                Id = user.Id,
                Email = user.Email
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(ResetPassword.Id);
                if (user != null)
                {
                    UserEmail = user.Email;
                }
                return Page();
            }

            var userToUpdate = await _userManager.FindByIdAsync(ResetPassword.Id);

            if (userToUpdate == null)
            {
                // Add an error and return to the page
                ModelState.AddModelError(string.Empty, "User not found.");
                return Page();
            }

            UserEmail = userToUpdate.Email;

            // Remove the existing password
            var removeResult = await _userManager.RemovePasswordAsync(userToUpdate);
            if (!removeResult.Succeeded)
            {
                foreach (var error in removeResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            // Add the new password
            var addResult = await _userManager.AddPasswordAsync(userToUpdate, ResetPassword.Password);
            if (!addResult.Succeeded)
            {
                foreach (var error in addResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return Page();
            }

            // Reset security stamp to invalidate existing sessions/tokens
            await _userManager.UpdateSecurityStampAsync(userToUpdate);

            // Redirect to user details page
            return RedirectToPage("./Details", new { id = ResetPassword.Id, message = "Password reset successfully." });
        }
    }
}