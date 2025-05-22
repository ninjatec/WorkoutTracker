using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Services
{
    /// <summary>
    /// Custom validator for ASP.NET Identity that handles username validation
    /// to avoid "Username is already taken" errors when emails are unique.
    /// </summary>
    public class CustomUserValidator : IUserValidator<AppUser>
    {
        private readonly CustomUsernameManager _usernameManager;
        
        public CustomUserValidator(CustomUsernameManager usernameManager)
        {
            _usernameManager = usernameManager;
        }
        
        public async Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user)
        {
            // For existing users (those being edited), don't modify the username
            if (!string.IsNullOrEmpty(user.Id))
            {
                // Only check if the username is taken by another user
                var existingUser = await manager.FindByNameAsync(user.UserName);
                if (existingUser != null && !string.Equals(existingUser.Id, user.Id, StringComparison.Ordinal))
                {
                    return IdentityResult.Failed(
                        new IdentityError { Code = "DuplicateUserName", Description = "Username is already taken." });
                }
                return IdentityResult.Success;
            }
            
            // For new users only: if username matches email, generate a unique one
            if (string.IsNullOrEmpty(user.Id) && user.UserName == user.Email)
            {
                user.UserName = await _usernameManager.GenerateUniqueUsernameFromEmailAsync(user.Email);
                return IdentityResult.Success;
            }
            
            return IdentityResult.Success;
        }
    }
}