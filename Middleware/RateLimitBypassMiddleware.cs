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
            "81.101.135.243", // Your IP from the logs
            "127.0.0.1",      // Localhost for development
            "::1"             // IPv6 localhost
        };
        
        // Pages that might trigger auto-whitelisting when accessed frequently
        private readonly string[] _sensitivePages = new[]
        {
            "/",
            "/Index",
            "/Home",
            "/Home/Index",
            "/Account/Login",
            "/Account/Register",
            "/Sessions",
            "/Sessions/Details",
            "/Reports",
            "/api/"
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

            // Always check for trusted IPs regardless of the page being accessed
            if (IsValidIpAddress(clientIp))
            {
                // Auto-whitelist trusted IPs on any page
                if (ShouldAutoWhitelist(clientIp) && !_rateLimiter.IsIpWhitelisted(clientIp))
                {
                    try
                    {
                        _logger.LogInformation("Auto-whitelisting trusted IP {IP} site-wide", clientIp);
                        
                        await _rateLimiter.AddIpToWhitelistAsync(
                            clientIp,
                            $"Auto-whitelisted trusted IP on {DateTime.UtcNow:yyyy-MM-dd}",
                            "System");
                            
                        context.Response.Headers["X-Rate-Limit-Bypass"] = "Enabled";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to auto-whitelist trusted IP {IP}", clientIp);
                    }
                }
                
                // Special handling for rate-limited IPs on sensitive pages
                // This helps users who aren't in the trusted IPs list but are hitting rate limits on important pages
                bool isSensitivePage = IsSensitivePage(context.Request.Path.Value);
                if (isSensitivePage && !_rateLimiter.IsIpWhitelisted(clientIp))
                {
                    // Check if we've seen 429 responses for this IP recently
                    // We'll store this in the current request as a hint for potential future whitelisting
                    string headerStatus = context.Request.Headers["X-Previous-Status-Code"].ToString();
                    if (headerStatus == "429")
                    {
                        _logger.LogWarning("IP {IP} previously hit rate limit accessing {Path}, considering for whitelist", 
                            clientIp, context.Request.Path.Value);
                            
                        try
                        {
                            await _rateLimiter.AddIpToWhitelistAsync(
                                clientIp,
                                $"Auto-whitelisted after rate limit on sensitive page {context.Request.Path.Value} on {DateTime.UtcNow:yyyy-MM-dd}",
                                "System");
                                
                            context.Response.Headers["X-Rate-Limit-Bypass"] = "Enabled";
                            _logger.LogInformation("Added IP {IP} to whitelist after detecting rate limit issues", clientIp);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to whitelist IP {IP} after rate limit issues", clientIp);
                        }
                    }
                }
            }

            await _next(context);
            
            // Check for rate limit response and store for future requests
            if (context.Response.StatusCode == 429)
            {
                _logger.LogWarning("Rate limit (429) triggered for IP {IP} on {Path}", 
                    clientIp, context.Request.Path.Value);
                
                // Add a cookie to track that this IP hit a rate limit
                // This will be used on the next request to potentially whitelist the IP
                context.Response.Cookies.Append("X-Previous-Status-Code", "429", new CookieOptions 
                { 
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    MaxAge = TimeSpan.FromMinutes(5),
                    IsEssential = true,
                    Path = "/"
                });
            }
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

        private bool IsSensitivePage(string path)
        {
            foreach (var sensitivePage in _sensitivePages)
            {
                if (path.StartsWith(sensitivePage, StringComparison.OrdinalIgnoreCase))
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