using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Models;
using System.Security.Claims;

namespace WorkoutTrackerWeb.Services
{
    public class UserService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(
            WorkoutTrackerWebContext context,
            UserManager<IdentityUser> userManager,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        // Get the current identity user ID
        public string GetCurrentIdentityUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // Get or create the current user in our app's context
        public async Task<User> GetOrCreateCurrentUserAsync()
        {
            string identityUserId = GetCurrentIdentityUserId();
            if (string.IsNullOrEmpty(identityUserId))
            {
                return null;
            }

            // Try to find existing user
            var user = await _context.User.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
            
            // If not found, create a new one
            if (user == null)
            {
                var identityUser = await _userManager.FindByIdAsync(identityUserId);
                if (identityUser == null)
                {
                    return null;
                }

                user = new User
                {
                    Name = identityUser.UserName,
                    IdentityUserId = identityUserId
                };
                
                _context.User.Add(user);
                await _context.SaveChangesAsync();
            }
            
            return user;
        }

        // Get current user's ID
        public async Task<int?> GetCurrentUserIdAsync()
        {
            var user = await GetOrCreateCurrentUserAsync();
            return user?.UserId;
        }
    }
}