using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services;
using System.Text.RegularExpressions;

namespace WorkoutTrackerWeb.Middleware
{
    public class RateLimitBypassMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitBypassMiddleware> _logger;
        private readonly ITokenRateLimiter _rateLimiter;
        private readonly Regex _ipPattern = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$");
        
        // Known IPs that should be whitelisted when they hit rate limits
        private readonly string[] _trustedIpsToAutoWhitelist = new[]
        {
            "81.101.135.243" // Your IP from the logs
        };

        public RateLimitBypassMiddleware(
            RequestDelegate next,
            ILogger<RateLimitBypassMiddleware> logger,
            ITokenRateLimiter rateLimiter)
        {
            _next = next;
            _logger = logger;
            _rateLimiter = rateLimiter;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string clientIp = GetClientIpAddress(context);

            // Only process for homepage or known problem pages
            bool isHomepage = context.Request.Path.Value == "/" || 
                              context.Request.Path.Value == "/Index" ||
                              context.Request.Path.Value == "/Home" ||
                              context.Request.Path.Value == "/Home/Index";

            if (isHomepage && IsValidIpAddress(clientIp) && ShouldAutoWhitelist(clientIp))
            {
                // Check if this IP is already whitelisted
                if (!_rateLimiter.IsIpWhitelisted(clientIp))
                {
                    try
                    {
                        _logger.LogInformation("Auto-whitelisting IP {IP} due to homepage access", clientIp);
                        
                        // Add IP to whitelist
                        await _rateLimiter.AddIpToWhitelistAsync(
                            clientIp,
                            $"Auto-whitelisted after potential homepage redirect loop on {DateTime.UtcNow:yyyy-MM-dd}",
                            "System");
                            
                        // Set a header so the client knows they've been whitelisted
                        context.Response.Headers["X-Rate-Limit-Bypass"] = "Enabled";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to auto-whitelist IP {IP}", clientIp);
                    }
                }
            }

            await _next(context);
        }

        private string GetClientIpAddress(HttpContext context)
        {
            string ipAddress = context.Connection.RemoteIpAddress?.ToString();
            
            // Check for X-Forwarded-For header (common with proxies)
            string forwardedIp = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedIp) && forwardedIp.Contains(","))
            {
                // Get the first IP in the list (client's original IP)
                ipAddress = forwardedIp.Split(',')[0].Trim();
            }
            else if (!string.IsNullOrEmpty(forwardedIp))
            {
                ipAddress = forwardedIp.Trim();
            }
            
            // Check for Cloudflare-specific header
            string cfIp = context.Request.Headers["CF-Connecting-IP"].ToString();
            if (!string.IsNullOrEmpty(cfIp))
            {
                ipAddress = cfIp.Trim();
            }
            
            return ipAddress;
        }

        private bool IsValidIpAddress(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
                return false;
                
            return _ipPattern.IsMatch(ipAddress);
        }

        private bool ShouldAutoWhitelist(string ipAddress)
        {
            // Check if the IP is in our trusted list for auto-whitelisting
            foreach (var trustedIp in _trustedIpsToAutoWhitelist)
            {
                if (ipAddress.Equals(trustedIp, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }
    }

    // Extension method for easier middleware registration
    public static class RateLimitBypassMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimitBypass(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitBypassMiddleware>();
        }
    }
}