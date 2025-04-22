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
    public class DeleteModel : PageModel
    {
        private readonly IShareTokenService _shareTokenService;
        private readonly ITokenValidationService _tokenValidationService;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(
            IShareTokenService shareTokenService,
            ITokenValidationService tokenValidationService,
            WorkoutTrackerWebContext context,
            ILogger<DeleteModel> logger)
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

                var result = await _shareTokenService.DeleteShareTokenAsync(id, userId.Value);
                if (!result)
                {
                    return NotFound();
                }

                // Clear the cache for this token
                await _tokenValidationService.ClearCacheForTokenAsync(token.Token);
                
                return new NoContentResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting share token {TokenId}", id);
                return StatusCode(500, new { error = "An error occurred while deleting the share token" });
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