using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.ViewComponents
{
    public class MaintenanceBannerViewComponent : ViewComponent
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITokenRateLimiter _tokenRateLimiter;

        public MaintenanceBannerViewComponent(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ITokenRateLimiter tokenRateLimiter)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _tokenRateLimiter = tokenRateLimiter;
        }

        public IViewComponentResult Invoke()
        {
            var isMaintenanceMode = IsInMaintenanceMode();
            if (!isMaintenanceMode)
            {
                // Not in maintenance mode, don't show the banner
                return Content(string.Empty);
            }

            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                return Content(string.Empty);
            }

            // If we're already on the maintenance page, don't show the banner
            if (context.Request.Path.Value?.ToLower() == "/maintenance")
            {
                return Content(string.Empty);
            }

            // This banner is only shown when the user/IP is allowed to access the site during maintenance
            bool isAdmin = context.User?.IsInRole("Admin") ?? false;
            
            string? clientIp = GetClientIpAddress(context);
            bool isWhitelisted = !string.IsNullOrEmpty(clientIp) && _tokenRateLimiter.IsIpWhitelisted(clientIp);

            bool showBanner = isMaintenanceMode && (isAdmin || isWhitelisted);

            var model = new MaintenanceBannerViewModel
            {
                ShowBanner = showBanner,
                EstimatedCompletionTime = _configuration["MaintenanceMode:EstimatedCompletionTime"] ?? "Soon"
            };

            return View(model);
        }

        private bool IsInMaintenanceMode()
        {
            // Check environment variable first
            string? maintenanceModeEnv = Environment.GetEnvironmentVariable("MAINTENANCE_MODE");
            if (!string.IsNullOrEmpty(maintenanceModeEnv) && 
                (maintenanceModeEnv.Equals("true", StringComparison.OrdinalIgnoreCase) || maintenanceModeEnv == "1"))
            {
                return true;
            }
            
            // Also check configuration
            return _configuration.GetValue<bool>("MaintenanceMode");
        }

        private string? GetClientIpAddress(HttpContext context)
        {
            string? ip = null;
            
            // Try to get IP from standard headers
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                ip = forwardedFor[0]?.Split(',')[0].Trim();
            }
            
            // If not found in X-Forwarded-For, try X-Real-IP
            if (string.IsNullOrEmpty(ip) && context.Request.Headers.TryGetValue("X-Real-IP", out var realIp))
            {
                ip = realIp[0];
            }
            
            // Fall back to connection remote IP if headers not available
            if (string.IsNullOrEmpty(ip))
            {
                ip = context.Connection.RemoteIpAddress?.ToString();
            }
            
            return ip;
        }
    }

    public class MaintenanceBannerViewModel
    {
        public bool ShowBanner { get; set; }
        public string EstimatedCompletionTime { get; set; }
    }
}