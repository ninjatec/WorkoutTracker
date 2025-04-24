using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Dtos;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.API.ShareToken
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IShareTokenService _shareTokenService;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IShareTokenService shareTokenService,
            WorkoutTrackerWebContext context,
            ILogger<IndexModel> logger)
        {
            _shareTokenService = shareTokenService;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var tokens = await _shareTokenService.GetUserShareTokensAsync(userId.Value);
                return new JsonResult(tokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving share tokens");
                return StatusCode(500, new { error = "An error occurred while retrieving share tokens" });
            }
        }

        public async Task<IActionResult> OnGetByIdAsync(int id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var token = await _shareTokenService.GetShareTokenByIdAsync(id, userId.Value);
                if (token == null)
                {
                    return NotFound();
                }

                return new JsonResult(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving share token {TokenId}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the share token" });
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