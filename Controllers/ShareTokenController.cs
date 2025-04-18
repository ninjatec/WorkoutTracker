using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerweb.Data;
using WorkoutTrackerWeb.Dtos;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShareTokenController : ControllerBase
    {
        private readonly IShareTokenService _shareTokenService;
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<ShareTokenController> _logger;

        public ShareTokenController(
            IShareTokenService shareTokenService,
            WorkoutTrackerWebContext context,
            ILogger<ShareTokenController> logger)
        {
            _shareTokenService = shareTokenService;
            _context = context;
            _logger = logger;
        }

        // GET: api/ShareToken
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ShareTokenDto>>> GetShareTokens()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var tokens = await _shareTokenService.GetUserShareTokensAsync(userId.Value);
                return Ok(tokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving share tokens");
                return StatusCode(500, "An error occurred while retrieving share tokens");
            }
        }

        // GET: api/ShareToken/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<ShareTokenDto>> GetShareToken(int id)
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

                return Ok(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving share token {TokenId}", id);
                return StatusCode(500, "An error occurred while retrieving the share token");
            }
        }

        // POST: api/ShareToken/validate
        [HttpPost("validate")]
        [AllowAnonymous]
        public async Task<ActionResult<ShareTokenValidationResponse>> ValidateToken([FromBody] ShareTokenValidationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Token))
                {
                    return BadRequest("Token is required");
                }

                var result = await _shareTokenService.ValidateTokenAsync(request.Token);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating share token");
                return StatusCode(500, "An error occurred while validating the share token");
            }
        }

        // POST: api/ShareToken
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ShareTokenDto>> CreateShareToken([FromBody] CreateShareTokenRequest request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var token = await _shareTokenService.CreateShareTokenAsync(request, userId.Value);
                return CreatedAtAction(nameof(GetShareToken), new { id = token.Id }, token);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating share token");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating share token");
                return StatusCode(500, "An error occurred while creating the share token");
            }
        }

        // PUT: api/ShareToken/5
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateShareToken(int id, [FromBody] UpdateShareTokenRequest request)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var token = await _shareTokenService.UpdateShareTokenAsync(id, request, userId.Value);
                if (token == null)
                {
                    return NotFound();
                }

                return Ok(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating share token {TokenId}", id);
                return StatusCode(500, "An error occurred while updating the share token");
            }
        }

        // DELETE: api/ShareToken/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteShareToken(int id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var result = await _shareTokenService.DeleteShareTokenAsync(id, userId.Value);
                if (!result)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting share token {TokenId}", id);
                return StatusCode(500, "An error occurred while deleting the share token");
            }
        }

        // POST: api/ShareToken/5/revoke
        [HttpPost("{id}/revoke")]
        [Authorize]
        public async Task<IActionResult> RevokeShareToken(int id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                var result = await _shareTokenService.RevokeShareTokenAsync(id, userId.Value);
                if (!result)
                {
                    return NotFound();
                }

                return Ok(new { message = "Share token revoked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking share token {TokenId}", id);
                return StatusCode(500, "An error occurred while revoking the share token");
            }
        }

        // Helper method to get the current user ID
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