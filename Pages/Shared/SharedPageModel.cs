using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Dtos;
using WorkoutTrackerWeb.Extensions;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Shared
{
    public abstract class SharedPageModel : PageModel
    {
        protected readonly IShareTokenService _shareTokenService;
        protected readonly ILogger _logger;

        public SharedPageModel(
            IShareTokenService shareTokenService,
            ILogger logger)
        {
            _shareTokenService = shareTokenService;
            _logger = logger;
        }

        public ShareTokenDto ShareToken { get; protected set; }
        public string UserName { get; protected set; }

        protected async Task<bool> ValidateTokenAsync(string token, string requiredPermission)
        {
            // Get token from query string or cookie
            string tokenValue = token;
            if (string.IsNullOrEmpty(tokenValue) && HttpContext.Request.Cookies.ContainsKey("share_token"))
            {
                tokenValue = HttpContext.Request.Cookies["share_token"];
            }

            if (string.IsNullOrEmpty(tokenValue))
            {
                _logger.LogWarning("No token provided for shared page access");
                return false;
            }

            try
            {
                // Validate token
                var result = await _shareTokenService.ValidateTokenAsync(tokenValue);
                if (!result.IsValid)
                {
                    _logger.LogWarning("Invalid token provided: {Error}", result.Message);
                    
                    // Redirect to invalid token page with error message
                    if (this.GetType() != typeof(InvalidTokenModel))
                    {
                        Response.Redirect($"/Shared/InvalidToken?error={Uri.EscapeDataString(result.Message)}");
                    }
                    
                    return false;
                }

                // Store token in cookie for future requests if not already there
                if (string.IsNullOrEmpty(token) && !HttpContext.Request.Cookies.ContainsKey("share_token"))
                {
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTimeOffset.UtcNow.AddDays(7) // Cookie expires in 7 days
                    };
                    Response.Cookies.Append("share_token", tokenValue, cookieOptions);
                }

                // Store token and set access data
                ShareToken = result.ShareToken;
                UserName = result.ShareToken?.UserName;

                // Check permission if required
                if (!string.IsNullOrEmpty(requiredPermission))
                {
                    bool hasPermission = false;
                    
                    // Normalize permission check
                    string normalizedPermission = requiredPermission.ToLowerInvariant();
                    
                    // Check for the permission with more flexible matching
                    if (normalizedPermission == "session" || normalizedPermission == "sessionaccess")
                    {
                        hasPermission = ShareToken.AllowSessionAccess;
                    }
                    else if (normalizedPermission == "report" || normalizedPermission == "reportaccess")
                    {
                        hasPermission = ShareToken.AllowReportAccess;
                    }
                    else if (normalizedPermission == "calculator" || normalizedPermission == "calculatoraccess")
                    {
                        hasPermission = ShareToken.AllowCalculatorAccess;
                    }
                    
                    if (!hasPermission)
                    {
                        _logger.LogWarning("Token lacks required permission: {Permission}", requiredPermission);
                        
                        // Redirect to access denied page with missing permission
                        if (this.GetType() != typeof(AccessDeniedModel))
                        {
                            Response.Redirect($"/Shared/AccessDenied?permission={Uri.EscapeDataString(requiredPermission)}");
                        }
                        
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating share token");
                return false;
            }
        }
    }
}