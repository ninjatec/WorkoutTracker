using System.Security.Claims;
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Extensions
{
    public static class AppUserExtensions
    {
        /// <summary>
        /// Gets the full name of the user, or returns the username if no full name is available
        /// </summary>
        /// <param name="user">The AppUser instance</param>
        /// <returns>The user's full name or username</returns>
        public static string FullName(this AppUser user)
        {
            if (user == null)
            {
                return string.Empty;
            }

            // Check if we already have a name stored in user claims or properties
            // For now, just return the username as a fallback
            return user.UserName ?? string.Empty;
        }

        /// <summary>
        /// Gets the user ID from the current user's claims
        /// </summary>
        /// <param name="user">The ClaimsPrincipal representing the current user</param>
        /// <returns>The user ID or null if not found</returns>
        public static string GetUserId(this ClaimsPrincipal user)
        {
            return user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}