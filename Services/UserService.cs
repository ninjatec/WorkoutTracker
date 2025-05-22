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
using WorkoutTrackerWeb.Models.Identity;

namespace WorkoutTrackerWeb.Services
{
    public interface IUserService
    {
        string GetCurrentIdentityUserId();
        Task<User> GetOrCreateCurrentUserAsync();
        Task<int?> GetCurrentUserIdAsync();
        Task<List<Models.WorkoutSession>> GetUserWorkoutSessionsAsync(int userId, int limit = 20);
        Task<List<Models.WorkoutSession>> GetUserSessionsAsync(int userId, int limit = 20);
    }

    public class UserService : IUserService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(10);

        public UserService(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
        }

        public string GetCurrentIdentityUserId()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public async Task<User> GetOrCreateCurrentUserAsync()
        {
            string identityUserId = GetCurrentIdentityUserId();
            if (string.IsNullOrEmpty(identityUserId))
            {
                return null;
            }

            string cacheKey = $"User_Identity_{identityUserId}";
            if (_cache.TryGetValue(cacheKey, out User cachedUser))
            {
                return cachedUser;
            }

            var user = await _context.User
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
            
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
                    
                    user = await _context.User
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
                        
                    if (user == null)
                    {
                        throw;
                    }
                }
            }
            
            _cache.Set(cacheKey, user, _cacheExpiry);
            
            return user;
        }

        public async Task<int?> GetCurrentUserIdAsync()
        {
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
            
            var user = await GetOrCreateCurrentUserAsync();
            if (user?.UserId > 0)
            {
                _cache.Set(idCacheKey, user.UserId, _cacheExpiry);
            }
            
            return user?.UserId;
        }

        public async Task<List<Models.WorkoutSession>> GetUserWorkoutSessionsAsync(int userId, int limit = 20)
        {
            return await _context.WorkoutSessions
                .Where(ws => ws.UserId == userId)
                .OrderByDescending(ws => ws.StartDateTime)
                .Take(limit)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Models.WorkoutSession>> GetUserSessionsAsync(int userId, int limit = 20)
        {
            return await GetUserWorkoutSessionsAsync(userId, limit);
        }
    }
}