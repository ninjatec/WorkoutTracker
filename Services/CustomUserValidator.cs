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
            // If the user is being created (no Id yet) and username matches email
            if (user.UserName == user.Email)
            {
                // Generate a unique username to avoid conflicts
                user.UserName = await _usernameManager.GenerateUniqueUsernameFromEmailAsync(user.Email);
                
                // Return success - username has been modified to be unique
                return IdentityResult.Success;
            }
            
            // Check if the username already exists for a different user when manually setting username
            var existingUser = await manager.FindByNameAsync(user.UserName);
            if (existingUser != null && !string.Equals(existingUser.Id, user.Id, StringComparison.Ordinal))
            {
                return IdentityResult.Failed(
                    new IdentityError { Code = "DuplicateUserName", Description = "Username is already taken." });
            }
            
            return IdentityResult.Success;
        }
    }
}