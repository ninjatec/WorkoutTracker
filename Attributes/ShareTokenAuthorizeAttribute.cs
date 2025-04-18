using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ShareTokenAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public string RequiredPermission { get; }

        public ShareTokenAuthorizeAttribute(string requiredPermission = null)
        {
            RequiredPermission = requiredPermission;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            try
            {
                // Get token from request
                string token = GetTokenFromRequest(context.HttpContext);
                if (string.IsNullOrEmpty(token))
                {
                    context.Result = new UnauthorizedObjectResult(new { message = "Valid share token is required" });
                    return;
                }

                // Get services
                var tokenValidationService = context.HttpContext.RequestServices.GetRequiredService<ITokenValidationService>();
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ShareTokenAuthorizeAttribute>>();

                // Validate the token
                var (isValid, validationResult) = await tokenValidationService.ValidateTokenAsync(
                    token, 
                    GetClientIpAddress(context.HttpContext));

                if (!isValid)
                {
                    logger.LogWarning("Invalid share token attempt: {Message}", validationResult.Message);
                    context.Result = new UnauthorizedObjectResult(new { message = validationResult.Message });
                    return;
                }

                // If a specific permission is required, check for it
                if (!string.IsNullOrEmpty(RequiredPermission))
                {
                    bool hasPermission = tokenValidationService.CheckIsAccessAllowed(
                        validationResult.ShareToken, 
                        RequiredPermission);

                    if (!hasPermission)
                    {
                        logger.LogWarning("Share token lacks required permission: {Permission}", RequiredPermission);
                        context.Result = new ForbidResult();
                        return;
                    }
                }

                // Store the shareToken in the httpContext items for later use
                context.HttpContext.Items["ShareToken"] = validationResult.ShareToken;
                logger.LogInformation("ShareToken authorization successful for token with ID {TokenId}", validationResult.ShareToken.Id);
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ShareTokenAuthorizeAttribute>>();
                logger.LogError(ex, "Error during share token authorization");
                context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private string GetTokenFromRequest(HttpContext httpContext)
        {
            // Try to get from query string first
            if (httpContext.Request.Query.TryGetValue("token", out var queryToken) && !string.IsNullOrEmpty(queryToken))
            {
                return queryToken;
            }
            
            // Then check authorization header - Bearer token format
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader.Substring("Bearer ".Length).Trim();
            }
            
            // Finally check for a cookie
            if (httpContext.Request.Cookies.TryGetValue("share_token", out var cookieToken) && !string.IsNullOrEmpty(cookieToken))
            {
                return cookieToken;
            }
            
            return null;
        }

        private string GetClientIpAddress(HttpContext httpContext)
        {
            // First try to get from X-Forwarded-For header
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    return ips[0].Trim();
                }
            }
            
            // Fallback to connection remote IP
            return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}