using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Areas.Admin.ViewModels;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Users
{
    [Authorize(Roles = "Admin")]
    public class DetailsModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly WorkoutTrackerWebContext _context;
        private readonly LoginHistoryService _loginHistoryService;

        public DetailsModel(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            WorkoutTrackerWebContext context,
            LoginHistoryService loginHistoryService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _loginHistoryService = loginHistoryService;
        }

        public UserViewModel User { get; set; }
        public List<LoginHistoryViewModel> LoginHistory { get; set; } = new List<LoginHistoryViewModel>();
        public List<string> AvailableRoles { get; set; } = new();
        public string StatusMessage { get; set; }
        public bool IsSuccess { get; set; }
        public int SessionCount { get; set; }
        public DateTime? LastActive { get; set; }

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

            await LoadUserDetailsAsync(user);
            await LoadAvailableRolesAsync();
            await LoadLoginHistoryAsync(id);

            return Page();
        }

        public async Task<IActionResult> OnPostConfirmEmailAsync(string id)
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

            user.EmailConfirmed = true;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                StatusMessage = "Email confirmed successfully.";
                IsSuccess = true;
            }
            else
            {
                StatusMessage = "Error confirming email: " + string.Join(", ", result.Errors.Select(e => e.Description));
                IsSuccess = false;
            }

            await LoadUserDetailsAsync(user);
            await LoadAvailableRolesAsync();
            await LoadLoginHistoryAsync(id);

            return Page();
        }

        public async Task<IActionResult> OnPostUnlockUserAsync(string id)
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

            var result = await _userManager.SetLockoutEndDateAsync(user, null);

            if (result.Succeeded)
            {
                StatusMessage = "User unlocked successfully.";
                IsSuccess = true;
            }
            else
            {
                StatusMessage = "Error unlocking user: " + string.Join(", ", result.Errors.Select(e => e.Description));
                IsSuccess = false;
            }

            await LoadUserDetailsAsync(user);
            await LoadAvailableRolesAsync();
            await LoadLoginHistoryAsync(id);

            return Page();
        }

        public async Task<IActionResult> OnPostAddToRoleAsync(string id, string role)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(role))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                StatusMessage = $"Role '{role}' does not exist.";
                IsSuccess = false;
                await LoadUserDetailsAsync(user);
                await LoadAvailableRolesAsync();
                await LoadLoginHistoryAsync(id);
                return Page();
            }

            if (await _userManager.IsInRoleAsync(user, role))
            {
                StatusMessage = $"User is already in role '{role}'.";
                IsSuccess = false;
                await LoadUserDetailsAsync(user);
                await LoadAvailableRolesAsync();
                await LoadLoginHistoryAsync(id);
                return Page();
            }

            var result = await _userManager.AddToRoleAsync(user, role);

            if (result.Succeeded)
            {
                StatusMessage = $"Added user to role '{role}' successfully.";
                IsSuccess = true;
            }
            else
            {
                StatusMessage = "Error adding user to role: " + string.Join(", ", result.Errors.Select(e => e.Description));
                IsSuccess = false;
            }

            await LoadUserDetailsAsync(user);
            await LoadAvailableRolesAsync();
            await LoadLoginHistoryAsync(id);

            return Page();
        }

        public async Task<IActionResult> OnPostRemoveFromRoleAsync(string id, string role)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(role))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Check if this is the last admin
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1 && admins.Any(a => a.Id == id))
                {
                    StatusMessage = "Cannot remove the last admin user from the Admin role.";
                    IsSuccess = false;
                    await LoadUserDetailsAsync(user);
                    await LoadAvailableRolesAsync();
                    await LoadLoginHistoryAsync(id);
                    return Page();
                }
            }

            var result = await _userManager.RemoveFromRoleAsync(user, role);

            if (result.Succeeded)
            {
                StatusMessage = $"Removed user from role '{role}' successfully.";
                IsSuccess = true;
            }
            else
            {
                StatusMessage = "Error removing user from role: " + string.Join(", ", result.Errors.Select(e => e.Description));
                IsSuccess = false;
            }

            await LoadUserDetailsAsync(user);
            await LoadAvailableRolesAsync();
            await LoadLoginHistoryAsync(id);

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(string id)
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

            // Don't allow deleting current user
            if (HttpContext.User.Identity.Name == user.UserName)
            {
                StatusMessage = "You cannot delete your own account.";
                IsSuccess = false;
                await LoadUserDetailsAsync(user);
                await LoadAvailableRolesAsync();
                await LoadLoginHistoryAsync(id);
                return Page();
            }

            // Check if this is the last admin
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Count <= 1)
                {
                    StatusMessage = "Cannot delete the last admin user.";
                    IsSuccess = false;
                    await LoadUserDetailsAsync(user);
                    await LoadAvailableRolesAsync();
                    await LoadLoginHistoryAsync(id);
                    return Page();
                }
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return RedirectToPage("./Index", new { message = "User deleted successfully." });
            }

            StatusMessage = "Error deleting user: " + string.Join(", ", result.Errors.Select(e => e.Description));
            IsSuccess = false;
            await LoadUserDetailsAsync(user);
            await LoadAvailableRolesAsync();
            await LoadLoginHistoryAsync(id);
            return Page();
        }

        private async Task LoadUserDetailsAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            User = new UserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LockoutEnabled = user.LockoutEnabled,
                LockoutEnd = user.LockoutEnd,
                AccessFailedCount = user.AccessFailedCount,
                CreatedDate = user.CreatedDate,
                LastModifiedDate = user.LastModifiedDate,
                Roles = roles.ToList()
            };

            // Get the user's workout session count
            // Use string comparison for UserId
            SessionCount = await _context.Session
                .CountAsync(s => s.UserId.Equals(user.Id));

            // Get the user's last active time (last session date)
            var lastSession = await _context.Session
                .Where(s => s.UserId.Equals(user.Id))
                .OrderByDescending(s => s.datetime)
                .FirstOrDefaultAsync();
                
            LastActive = lastSession?.datetime;
        }

        private async Task LoadAvailableRolesAsync()
        {
            var userRoles = User.Roles;
            var allRoles = await _roleManager.Roles
                .Select(r => r.Name)
                .OrderBy(r => r)
                .ToListAsync();

            AvailableRoles = allRoles.Except(userRoles, StringComparer.OrdinalIgnoreCase).ToList();
        }
        
        private async Task LoadLoginHistoryAsync(string userId)
        {
            var loginHistory = await _loginHistoryService.GetUserLoginHistoryAsync(userId, 20);
            
            LoginHistory = loginHistory.Select(h => new LoginHistoryViewModel
            {
                Id = h.Id,
                LoginTime = h.LoginTime,
                IpAddress = h.IpAddress,
                DeviceType = h.DeviceType,
                Platform = h.Platform,
                IsSuccessful = h.IsSuccessful
            }).ToList();
        }
    }
}