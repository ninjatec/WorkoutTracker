using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace WorkoutTrackerWeb.Services
{
    public class UserPreferenceService
    {
        private readonly IDbContextFactory<WorkoutTrackerWebContext> _contextFactory;
        private readonly ILogger<UserPreferenceService> _logger;

        public UserPreferenceService(IDbContextFactory<WorkoutTrackerWebContext> contextFactory, ILogger<UserPreferenceService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<string> GetThemePreferenceAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogDebug("User ID is null or empty, defaulting to light theme");
                return "light";
            }

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                return user?.ThemePreference ?? "light";
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error retrieving theme preference for user {UserId}, defaulting to light theme", userId);
                return "light"; // Gracefully handle database errors without breaking the application
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving theme preference for user {UserId}, defaulting to light theme", userId);
                return "light"; // Gracefully handle all errors without breaking the application
            }
        }

        public async Task SetThemePreferenceAsync(string userId, string theme)
        {
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Cannot set theme preference for null or empty user ID");
                return;
            }

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    user.ThemePreference = theme;
                    await context.SaveChangesAsync();
                    _logger.LogDebug("Theme preference set to {Theme} for user {UserId}", theme, userId);
                }
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error setting theme preference for user {UserId}", userId);
                // Don't rethrow - gracefully handle database errors without breaking the application
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting theme preference for user {UserId}", userId);
                // Don't rethrow - gracefully handle all errors without breaking the application
            }
        }
    }
}
