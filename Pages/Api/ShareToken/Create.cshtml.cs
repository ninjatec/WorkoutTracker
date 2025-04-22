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
    public class CreateModel : PageModel
    {
        private readonly IShareTokenService _shareTokenService;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            IShareTokenService shareTokenService,
            WorkoutTrackerWebContext context,
            ILogger<CreateModel> logger)
        {
            _shareTokenService = shareTokenService;
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public CreateShareTokenRequest TokenRequest { get; set; }

        public async Task<IActionResult> OnPostAsync()
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

                var token = await _shareTokenService.CreateShareTokenAsync(TokenRequest, userId.Value);
                return new JsonResult(token) { StatusCode = 201 }; // Created
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating share token");
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating share token");
                return StatusCode(500, new { error = "An error occurred while creating the share token" });
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