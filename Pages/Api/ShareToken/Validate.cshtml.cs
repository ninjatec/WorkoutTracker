using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Dtos;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.API.ShareToken
{
    [AllowAnonymous]
    public class ValidateModel : PageModel
    {
        private readonly ITokenValidationService _tokenValidationService;
        private readonly ILogger<ValidateModel> _logger;

        public ValidateModel(
            ITokenValidationService tokenValidationService,
            ILogger<ValidateModel> logger)
        {
            _tokenValidationService = tokenValidationService;
            _logger = logger;
        }

        [BindProperty]
        public ShareTokenValidationRequest ValidationRequest { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(ValidationRequest?.Token))
                {
                    return BadRequest(new { error = "Token is required" });
                }

                // Get client IP for rate limiting purposes
                string ipAddress = GetClientIpAddress();
                
                // Use the enhanced validation service
                var (isValid, validationResult) = await _tokenValidationService.ValidateTokenAsync(ValidationRequest.Token, ipAddress);
                
                // For security reasons, we still return a 200 response with detailed information
                // This avoids leaking information through status codes (which is better for brute force attacks)
                
                if (isValid)
                {
                    // Log successful validations for security audit
                    _logger.LogInformation(
                        "Token validation successful: TokenID={TokenId}, IP={IpAddress}, UserID={UserId}, SessionId={SessionId}",
                        validationResult.ShareToken.Id,
                        ipAddress,
                        validationResult.ShareToken.UserId,
                        validationResult.ShareToken.SessionId);
                }
                
                return new JsonResult(validationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating share token");
                return StatusCode(500, new { error = "An error occurred while validating the share token" });
            }
        }

        public async Task<IActionResult> OnGetPermissionAsync(string permission, string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return BadRequest(new { error = "Token is required" });
                }

                if (string.IsNullOrEmpty(permission))
                {
                    return BadRequest(new { error = "Permission is required" });
                }

                bool hasPermission = await _tokenValidationService.ValidateTokenPermissionAsync(token, permission, HttpContext);
                return new JsonResult(hasPermission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token permission");
                return StatusCode(500, new { error = "An error occurred while validating the token permission" });
            }
        }

        private string GetClientIpAddress()
        {
            // First try to get from X-Forwarded-For header
            var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    return ips[0].Trim();
                }
            }
            
            // Fallback to connection remote IP
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}