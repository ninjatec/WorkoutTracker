using System;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Services.Users
{
    /// <summary>
    /// Interface for user-related operations
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Gets the current user's ID
        /// </summary>
        /// <returns>The user ID or null if not authenticated</returns>
        Task<int?> GetCurrentUserIdAsync();
        
        /// <summary>
        /// Gets the identity user ID (AppUser) for the current user
        /// </summary>
        /// <returns>The identity user ID or null if not authenticated</returns>
        Task<string> GetCurrentIdentityUserIdAsync();
    }
}