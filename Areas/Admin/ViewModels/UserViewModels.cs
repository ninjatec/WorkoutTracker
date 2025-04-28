using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WorkoutTrackerWeb.Areas.Admin.ViewModels
{
    // ViewModel for displaying user information in lists and details
    public class UserViewModel
    {
        public string Id { get; set; }
        
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        [Display(Name = "Username")]
        public string UserName { get; set; }
        
        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }
        
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
        
        [Display(Name = "Two-Factor Authentication")]
        public bool TwoFactorEnabled { get; set; }
        
        [Display(Name = "Account Lockout")]
        public bool LockoutEnabled { get; set; }
        
        [Display(Name = "Lockout End")]
        public DateTimeOffset? LockoutEnd { get; set; }
        
        [Display(Name = "Failed Attempts")]
        public int AccessFailedCount { get; set; }
        
        [Display(Name = "Roles")]
        public List<string> Roles { get; set; } = new List<string>();
        
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }
        
        [Display(Name = "Last Modified")]
        public DateTime LastModifiedDate { get; set; }
        
        public bool IsLockedOut => LockoutEnd != null && LockoutEnd > DateTimeOffset.UtcNow;
    }
    
    // ViewModel for editing user information
    public class UserEditViewModel
    {
        public string Id { get; set; }
        
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        [Required]
        [Display(Name = "Username")]
        public string UserName { get; set; }
        
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
        
        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }
        
        [Display(Name = "Two-Factor Authentication")]
        public bool TwoFactorEnabled { get; set; }
        
        [Display(Name = "Account Lockout")]
        public bool LockoutEnabled { get; set; }
        
        [Display(Name = "Roles")]
        public List<string> SelectedRoles { get; set; } = new List<string>();
        
        public List<IdentityRole> AvailableRoles { get; set; } = new List<IdentityRole>();
    }
    
    // ViewModel for creating new users
    public class UserCreateViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        [Required]
        [Display(Name = "Username")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Username can only contain letters, numbers, and the symbols . - _")]
        public string UserName { get; set; }
        
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
        
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
        
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
        
        [Display(Name = "Email Confirmed")]
        public bool EmailConfirmed { get; set; }
        
        [Display(Name = "Roles")]
        public List<string> SelectedRoles { get; set; } = new List<string>();
        
        public List<IdentityRole> AvailableRoles { get; set; } = new List<IdentityRole>();
    }
    
    // ViewModel for resetting user passwords
    public class ResetPasswordViewModel
    {
        public string Id { get; set; }
        
        public string Email { get; set; }
        
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string Password { get; set; }
        
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}