using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Dtos;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.API.ShareToken
{
    [Authorize]
    public class RevokeModel : PageModel
    {
        private readonly IShareTokenService _shareTokenService;
        private readonly ITokenValidationService _tokenValidationService;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<RevokeModel> _logger;

        public RevokeModel(
            IShareTokenService shareTokenService,
            ITokenValidationService tokenValidationService,
            WorkoutTrackerWebContext context,
            ILogger<RevokeModel> logger)
        {
            _shareTokenService = shareTokenService;
            _tokenValidationService = tokenValidationService;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                // Get the token first to clear the cache
                var token = await _shareTokenService.GetShareTokenByIdAsync(id, userId.Value);
                if (token == null)
                {
                    return NotFound();
                }

                var result = await _shareTokenService.RevokeShareTokenAsync(id, userId.Value);
                if (!result)
                {
                    return NotFound();
                }

                // Clear the cache for this token
                await _tokenValidationService.ClearCacheForTokenAsync(token.Token);
                
                return new JsonResult(new { message = "Share token revoked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking share token {TokenId}", id);
                return StatusCode(500, new { error = "An error occurred while revoking the share token" });
            }
        }

        private async Task<int?> GetCurrentUserIdAsync()
        {
            var identityUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(identityUserId))
            {
                return null;
            }

            var user = await _context.User.FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
            return user?.UserId;
        }
    }
}