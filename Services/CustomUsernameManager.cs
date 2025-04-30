using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Models.Identity;
using System.Linq;

namespace WorkoutTrackerWeb.Services
{
    /// <summary>
    /// Custom username manager service that ensures unique username generation for new users
    /// even when email addresses are used as usernames.
    /// </summary>
    public class CustomUsernameManager
    {
        private readonly IServiceProvider _serviceProvider;
        
        public CustomUsernameManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        
        /// <summary>
        /// Generates a unique username based on email address
        /// </summary>
        /// <param name="email">The email address to use as a basis for the username</param>
        /// <returns>A unique username based on the email</returns>
        public async Task<string> GenerateUniqueUsernameFromEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email cannot be null or empty", nameof(email));
            }
            
            // Base username is the email part before the @ symbol
            string baseUsername = email.Split('@')[0].ToLowerInvariant();
            
            // Remove any characters not allowed in usernames
            baseUsername = new string(baseUsername
                .Where(c => char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_')
                .ToArray());
            
            // Ensure the username starts with a letter
            if (baseUsername.Length == 0 || !char.IsLetter(baseUsername[0]))
            {
                baseUsername = "user" + baseUsername;
            }
            
            // Use a scoped service to check usernames to avoid circular dependency
            using var scope = _serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            
            // Check if the base username is already in use
            var existingUser = await userManager.FindByNameAsync(baseUsername);
            if (existingUser == null)
            {
                return baseUsername; // Username is available
            }
            
            // Try with random suffix until we find an available username
            for (int i = 1; i <= 100; i++)
            {
                string suffixedUsername = $"{baseUsername}{i}";
                existingUser = await userManager.FindByNameAsync(suffixedUsername);
                
                if (existingUser == null)
                {
                    return suffixedUsername; // Username with suffix is available
                }
            }
            
            // If we couldn't find an available username with incremental suffixes,
            // generate one with a random suffix
            string randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 8);
            return $"{baseUsername}{randomSuffix}";
        }
    }
}