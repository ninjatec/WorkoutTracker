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
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Pages.Account.Manage
{
    [Authorize]
    public class AccountManagementModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<AccountManagementModel> _logger;

        public AccountManagementModel(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            WorkoutTrackerWebContext context,
            ILogger<AccountManagementModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
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
            public string Password { get; set; }

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
            
            // Unit preferences for individual measurements
            [Display(Name = "Height Unit")]
            public string HeightUnit { get; set; } = "Metric";
            
            [Display(Name = "Weight Unit")]
            public string WeightUnit { get; set; } = "Metric";

            public DateTime CreatedDate { get; set; }
            public DateTime LastModifiedDate { get; set; }
        }

        private async Task LoadAsync(AppUser user)
        {
            try
            {
                var userName = await _userManager.GetUserNameAsync(user);
                var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

                // Load user preferences
                var appUser = await _context.User
                    .FirstOrDefaultAsync(u => u.IdentityUserId == user.Id);

                // Initialize input model with basic identity info
                Input = new InputModel
                {
                    PhoneNumber = phoneNumber,
                    UserName = userName,
                    // Use TempData for unit preferences if available (e.g. after a form submit),
                    // otherwise use the defaults
                    HeightUnit = TempData["HeightUnit"]?.ToString() ?? "Metric",
                    WeightUnit = TempData["WeightUnit"]?.ToString() ?? "Metric"
                };

                // Load measurements if available
                if (appUser != null)
                {
                    _logger.LogInformation("Loading measurements for user {UserId}", user.Id);

                    if (appUser.HeightCm.HasValue)
                    {
                        if (Input.HeightUnit == "Imperial")
                        {
                            // Convert cm to feet and inches
                            decimal totalInches = appUser.HeightCm.Value / 2.54m;
                            Input.HeightFeet = (int)(totalInches / 12);
                            Input.HeightInches = (int)(totalInches % 12);
                        }
                        else
                        {
                            Input.HeightCm = decimal.Round(appUser.HeightCm.Value, 2);
                        }
                    }

                    if (appUser.WeightKg.HasValue)
                    {
                        if (Input.WeightUnit == "Imperial")
                        {
                            // Convert kg to lbs
                            Input.WeightLbs = decimal.Round(appUser.WeightKg.Value * 2.20462m, 2);
                        }
                        else
                        {
                            Input.WeightKg = decimal.Round(appUser.WeightKg.Value, 2);
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("No existing measurements found for user {UserId}", user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user data for user {UserId}", user?.Id);
                ModelState.AddModelError(string.Empty, "An error occurred while loading your profile. Please try again.");
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

            // Log the input values immediately to verify what we're receiving
            _logger.LogInformation("Received form values - Height Unit: {HeightUnit}, Weight Unit: {WeightUnit}", 
                Input.HeightUnit, Input.WeightUnit);
            _logger.LogInformation("Height values - Metric: {HeightCm}cm, Imperial: {HeightFeet}ft {HeightInches}in",
                Input.HeightCm, Input.HeightFeet, Input.HeightInches);
            _logger.LogInformation("Weight values - Metric: {WeightKg}kg, Imperial: {WeightLbs}lbs",
                Input.WeightKg, Input.WeightLbs);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid. Errors: {Errors}", 
                    string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)));
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            // Store unit preferences for next page load
            TempData["HeightUnit"] = Input.HeightUnit;
            TempData["WeightUnit"] = Input.WeightUnit;

            try 
            {
                // Get or create the User record within the transaction
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
                    try
                    {
                        _logger.LogInformation("Starting transaction to update measurements for user {UserId}", user.Id);
                        
                        // Track the entity state
                        var appUser = await _context.User
                            .AsNoTracking() // Get clean entity
                            .FirstOrDefaultAsync(u => u.IdentityUserId == user.Id);

                        bool isNewUser = false;

                        if (appUser == null)
                        {
                            isNewUser = true;
                            appUser = new Models.User 
                            { 
                                IdentityUserId = user.Id,
                                Name = user.UserName
                            };
                            _logger.LogInformation("Creating new User record for IdentityUserId {IdentityUserId}", user.Id);
                            _context.User.Add(appUser);
                        }
                        else 
                        {
                            _logger.LogInformation("Found existing User record for IdentityUserId {IdentityUserId}", user.Id);
                            _context.User.Attach(appUser);
                        }

                        bool updateHeight = false;
                        bool updateWeight = false;
                        decimal? newHeight = null;
                        decimal? newWeight = null;

                        // Convert and store height with detailed logging
                        if (Input.HeightUnit == "Imperial" && (Input.HeightFeet.HasValue || Input.HeightInches.HasValue))
                        {
                            var feet = Input.HeightFeet ?? 0;
                            var inches = Input.HeightInches ?? 0;
                            if (feet > 0 || inches > 0) // Only set if we have actual values
                            {
                                _logger.LogInformation("Converting imperial height: {Feet}ft {Inches}in to cm", feet, inches);
                                var totalCm = (feet * 30.48m) + (inches * 2.54m);
                                newHeight = decimal.Round(totalCm, 2);
                                _logger.LogInformation("Converted height to {HeightCm}cm", newHeight);
                            }
                        }
                        else if (Input.HeightUnit == "Metric" && Input.HeightCm.HasValue)
                        {
                            if (Input.HeightCm > 0) // Only set if we have actual values
                            {
                                newHeight = decimal.Round(Input.HeightCm.Value, 2);
                                _logger.LogInformation("Using metric height: {HeightCm}cm", newHeight);
                            }
                        }

                        // Convert and store weight with detailed logging
                        if (Input.WeightUnit == "Imperial" && Input.WeightLbs.HasValue)
                        {
                            if (Input.WeightLbs > 0) // Only set if we have actual values
                            {
                                _logger.LogInformation("Converting imperial weight: {WeightLbs}lbs to kg", Input.WeightLbs);
                                newWeight = decimal.Round(Input.WeightLbs.Value / 2.20462m, 2);
                                _logger.LogInformation("Converted weight to {WeightKg}kg", newWeight);
                            }
                        }
                        else if (Input.WeightUnit == "Metric" && Input.WeightKg.HasValue)
                        {
                            if (Input.WeightKg > 0) // Only set if we have actual values
                            {
                                newWeight = decimal.Round(Input.WeightKg.Value, 2);
                                _logger.LogInformation("Using metric weight: {WeightKg}kg", newWeight);
                            }
                        }

                        // Debug log the current state
                        _logger.LogInformation("Current values in DB - Height: {OldHeight}cm, Weight: {OldWeight}kg", 
                            appUser.HeightCm, appUser.WeightKg);
                        _logger.LogInformation("New values to save - Height: {NewHeight}cm, Weight: {NewWeight}kg", 
                            newHeight, newWeight);

                        // Always update if we have valid new values
                        if (newHeight.HasValue)
                        {
                            _logger.LogInformation("Updating height for user {UserId} from {OldHeight} to {NewHeight} cm", 
                                user.Id, appUser.HeightCm?.ToString() ?? "null", newHeight);
                            appUser.HeightCm = newHeight;
                            updateHeight = true;
                            _context.Entry(appUser).Property(x => x.HeightCm).IsModified = true;
                        }

                        if (newWeight.HasValue)
                        {
                            _logger.LogInformation("Updating weight for user {UserId} from {OldWeight} to {NewWeight} kg", 
                                user.Id, appUser.WeightKg?.ToString() ?? "null", newWeight);
                            appUser.WeightKg = newWeight;
                            updateWeight = true;
                            _context.Entry(appUser).Property(x => x.WeightKg).IsModified = true;
                        }

                        // Only save if there are actual changes
                        if (isNewUser || updateHeight || updateWeight)
                        {
                            _logger.LogInformation("About to save changes - Entity State: {State}", _context.Entry(appUser).State);
                            await _context.SaveChangesAsync();

                            // Verify the save worked by querying the database again
                            var savedUser = await _context.User
                                .AsNoTracking()
                                .FirstOrDefaultAsync(u => u.IdentityUserId == user.Id);
                            _logger.LogInformation("After save - Height: {Height}cm, Weight: {Weight}kg", 
                                savedUser?.HeightCm, savedUser?.WeightKg);

                            await transaction.CommitAsync();
                            
                            StatusMessage = isNewUser ? 
                                "Your profile has been created with measurements" : 
                                "Your measurements have been updated";
                            _logger.LogInformation("Successfully saved measurements for user {UserId}", user.Id);
                        }
                        else 
                        {
                            _logger.LogInformation("No measurement changes detected for user {UserId}", user.Id);
                            StatusMessage = "No changes were made to your measurements";
                            await transaction.CommitAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving measurements for user {UserId}. Rolling back transaction.", user.Id);
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnPostAsync for user {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "An error occurred while saving your measurements. Please try again.");
                await LoadAsync(user);
                return Page();
            }

            return RedirectToPage();
        }
    }
}