using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Pages.Account.Manage
{
    [Authorize]
    public class AccountManagementModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly WorkoutTrackerWebContext _context;

        public AccountManagementModel(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            WorkoutTrackerWebContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }
            
            [Required]
            [Display(Name = "Username")]
            public string UserName { get; set; }

            [Display(Name = "Phone Number")]
            [Phone]
            public string PhoneNumber { get; set; }

            [Display(Name = "Current Password")]
            [DataType(DataType.Password)]
            public string Password { get; set; } // This will be used only for verification in the future

            // Height in metric (centimeters)
            [Display(Name = "Height (cm)")]
            [Range(0, 300)]
            public decimal? HeightCm { get; set; }
            
            // Height in imperial (feet)
            [Display(Name = "Height (ft)")]
            [Range(0, 9)]
            public int? HeightFeet { get; set; }
            
            // Height in imperial (inches)
            [Display(Name = "Height (in)")]
            [Range(0, 11)]
            public int? HeightInches { get; set; }
            
            // Weight in metric (kilograms)
            [Display(Name = "Weight (kg)")]
            [Range(0, 500)]
            public decimal? WeightKg { get; set; }
            
            // Weight in imperial (pounds)
            [Display(Name = "Weight (lbs)")]
            [Range(0, 1200)]
            public decimal? WeightLbs { get; set; }
            
            // Unit preference for display
            [Display(Name = "Preferred Units")]
            public string PreferredUnits { get; set; } = "Metric";

            [Display(Name = "Account Created")]
            [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
            public DateTime CreatedDate { get; set; }

            [Display(Name = "Last Updated")]
            [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
            public DateTime LastModifiedDate { get; set; }
        }

        private async Task LoadAsync(AppUser user)
        {
            var email = await Task.FromResult(user.Email);
            var userName = await Task.FromResult(user.UserName);
            var phoneNumber = await Task.FromResult(user.PhoneNumber);
            
            // Get the User record with height/weight data
            var appUser = await _context.User
                .FirstOrDefaultAsync(u => u.IdentityUserId == user.Id);
            
            Input = new InputModel
            {
                Email = email,
                UserName = userName,
                PhoneNumber = phoneNumber,
                CreatedDate = user.CreatedDate,
                LastModifiedDate = user.LastModifiedDate
            };
            
            // Add height and weight if the application User record exists
            if (appUser != null)
            {
                Input.HeightCm = appUser.HeightCm;
                Input.WeightKg = appUser.WeightKg;
                
                // Convert metric to imperial for display if needed
                if (appUser.HeightCm.HasValue)
                {
                    // Convert cm to feet and inches (1 foot = 30.48 cm)
                    var totalInches = appUser.HeightCm.Value / 2.54m;
                    Input.HeightFeet = (int)(totalInches / 12);
                    Input.HeightInches = (int)(totalInches % 12);
                }
                
                if (appUser.WeightKg.HasValue)
                {
                    // Convert kg to lbs (1 kg = 2.20462 lbs)
                    Input.WeightLbs = appUser.WeightKg.Value * 2.20462m;
                }
            }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            if (Input.Email != user.Email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, Input.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var error in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await LoadAsync(user);
                    return Page();
                }
            }

            if (Input.UserName != user.UserName)
            {
                var setUsernameResult = await _userManager.SetUserNameAsync(user, Input.UserName);
                if (!setUsernameResult.Succeeded)
                {
                    foreach (var error in setUsernameResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await LoadAsync(user);
                    return Page();
                }
            }

            if (Input.PhoneNumber != user.PhoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    foreach (var error in setPhoneResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    await LoadAsync(user);
                    return Page();
                }
            }

            // Update the LastModifiedDate field
            user.LastModifiedDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            
            // Save height and weight data
            var appUser = await _context.User.FirstOrDefaultAsync(u => u.IdentityUserId == user.Id);
            if (appUser == null)
            {
                // Create a new User record if it doesn't exist
                appUser = new Models.User
                {
                    IdentityUserId = user.Id,
                    Name = user.UserName // Default name to username
                };
                _context.User.Add(appUser);
            }
            
            // Now we know we have a valid User record
            // Use stored metric values directly or convert from imperial based on user preference
            if (Input.PreferredUnits == "Imperial" && Input.HeightFeet.HasValue && Input.HeightInches.HasValue)
            {
                // Convert feet/inches to cm (1 foot = 30.48 cm, 1 inch = 2.54 cm)
                var totalCm = (Input.HeightFeet.Value * 30.48m) + (Input.HeightInches.Value * 2.54m);
                appUser.HeightCm = decimal.Round(totalCm, 2);
            }
            else if (Input.HeightCm.HasValue)
            {
                // Use metric height as entered
                appUser.HeightCm = Input.HeightCm;
            }
            
            if (Input.PreferredUnits == "Imperial" && Input.WeightLbs.HasValue)
            {
                // Convert pounds to kg (1 lb = 0.453592 kg)
                var weightInKg = Input.WeightLbs.Value * 0.453592m;
                appUser.WeightKg = decimal.Round(weightInKg, 2);
            }
            else if (Input.WeightKg.HasValue)
            {
                // Use metric weight as entered
                appUser.WeightKg = Input.WeightKg;
            }
            
            await _context.SaveChangesAsync();

            // Note: We don't actually change the password here since this field is 
            // currently being used only for verification that the user knows their current password
            // A proper password change should include a new password field

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}