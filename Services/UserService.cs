using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Linq;

namespace WorkoutTrackerWeb.Services
{
    public class UserService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);

        public UserService(
            WorkoutTrackerWebContext context,
            UserManager<IdentityUser> userManager,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
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

            // Try to get from cache first
            string cacheKey = $"User_Identity_{identityUserId}";
            if (_cache.TryGetValue(cacheKey, out User cachedUser))
            {
                return cachedUser;
            }

            // Try to find existing user with AsNoTracking for read-only operations
            var user = await _context.User
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
            
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
                
                // Use a new scope to handle the insert operation separately
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.User.Add(user);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    
                    // Double-check if another thread created the user already
                    user = await _context.User
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
                        
                    if (user == null)
                    {
                        throw; // Re-throw if it's a different error
                    }
                }
            }
            
            // Cache the result to reduce database hits
            _cache.Set(cacheKey, user, _cacheExpiry);
            
            return user;
        }

        // Get current user's ID
        public async Task<int?> GetCurrentUserIdAsync()
        {
            // Try to get just the ID from cache first
            string identityUserId = GetCurrentIdentityUserId();
            if (string.IsNullOrEmpty(identityUserId))
            {
                return null;
            }
            
            string idCacheKey = $"UserId_Identity_{identityUserId}";
            if (_cache.TryGetValue(idCacheKey, out int cachedUserId))
            {
                return cachedUserId;
            }
            
            // If not in cache, get the full user object
            var user = await GetOrCreateCurrentUserAsync();
            if (user?.UserId > 0)
            {
                _cache.Set(idCacheKey, user.UserId, _cacheExpiry);
            }
            
            return user?.UserId;
        }

        // Get a specific user's sessions (limited to a certain count)
        public async Task<List<Models.Session>> GetUserSessionsAsync(int userId, int limit = 20)
        {
            // Get the most recent sessions for this user
            var sessions = await _context.Session
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.datetime)
                .Take(limit)
                .AsNoTracking()
                .ToListAsync();
                
            return sessions;
        }
    }
}