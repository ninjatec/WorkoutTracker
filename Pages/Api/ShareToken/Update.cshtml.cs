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
    public class UpdateModel : PageModel
    {
        private readonly IShareTokenService _shareTokenService;
        private readonly ITokenValidationService _tokenValidationService;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<UpdateModel> _logger;

        public UpdateModel(
            IShareTokenService shareTokenService,
            ITokenValidationService tokenValidationService,
            WorkoutTrackerWebContext context,
            ILogger<UpdateModel> logger)
        {
            _shareTokenService = shareTokenService;
            _tokenValidationService = tokenValidationService;
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public UpdateShareTokenRequest TokenRequest { get; set; }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var token = await _shareTokenService.UpdateShareTokenAsync(id, TokenRequest, userId.Value);
                if (token == null)
                {
                    return NotFound();
                }

                // Clear the cache for this token since it was modified
                await _tokenValidationService.ClearCacheForTokenAsync(token.Token);
                
                return new JsonResult(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating share token {TokenId}", id);
                return StatusCode(500, new { error = "An error occurred while updating the share token" });
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