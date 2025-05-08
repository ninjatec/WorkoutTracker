using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Services.Users
{
    /// <summary>
    /// Service for user-related operations
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<AppUser> _userManager;
        private readonly WorkoutTrackerWebContext _context;

        public UserService(
            IHttpContextAccessor httpContextAccessor,
            UserManager<AppUser> userManager,
            WorkoutTrackerWebContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _context = context;
        }

        /// <inheritdoc />
        public async Task<int?> GetCurrentUserIdAsync()
        {
            var identityUserId = await GetCurrentIdentityUserIdAsync();
            if (string.IsNullOrEmpty(identityUserId))
                return null;

            // Find the User record associated with the identity user
            var user = await _context.User
                .Where(u => u.IdentityUserId == identityUserId)
                .FirstOrDefaultAsync();

            return user?.UserId;
        }

        /// <inheritdoc />
        public Task<string> GetCurrentIdentityUserIdAsync()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity.IsAuthenticated)
                return Task.FromResult<string>(null);

            return Task.FromResult(user.FindFirstValue(ClaimTypes.NameIdentifier));
        }
    }
}