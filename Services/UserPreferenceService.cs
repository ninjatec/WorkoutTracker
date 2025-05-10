using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace WorkoutTrackerWeb.Services
{
    public class UserPreferenceService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<UserPreferenceService> _logger;

        public UserPreferenceService(WorkoutTrackerWebContext context, ILogger<UserPreferenceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> GetThemePreferenceAsync(string userId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                return user?.ThemePreference ?? "light";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving theme preference for user {UserId}", userId);
                throw;
            }
        }

        public async Task SetThemePreferenceAsync(string userId, string theme)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    user.ThemePreference = theme;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting theme preference for user {UserId}", userId);
                throw;
            }
        }
    }
}
