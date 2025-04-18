using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WorkoutTrackerWeb.Dtos;

namespace WorkoutTrackerWeb.Services
{
    public interface ITokenValidationService
    {
        Task<(bool IsValid, ShareTokenValidationResponse ValidationResult)> ValidateTokenAsync(string token, string ipAddress);
        Task<bool> ValidateTokenPermissionAsync(string token, string permission, HttpContext httpContext);
        bool CheckIsAccessAllowed(ShareTokenDto shareToken, string permission);
        Task ClearCacheForTokenAsync(string token);
    }

    public class TokenValidationService : ITokenValidationService
    {
        private readonly IShareTokenService _shareTokenService;
        private readonly ITokenRateLimiter _rateLimiter;
        private readonly IDistributedCache _cache;
        private readonly ILogger<TokenValidationService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenValidationService(
            IShareTokenService shareTokenService,
            ITokenRateLimiter rateLimiter,
            IDistributedCache cache,
            ILogger<TokenValidationService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _shareTokenService = shareTokenService;
            _rateLimiter = rateLimiter;
            _cache = cache;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(bool IsValid, ShareTokenValidationResponse ValidationResult)> ValidateTokenAsync(string token, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Empty token validation attempt from {IpAddress}", ipAddress);
                return (false, new ShareTokenValidationResponse 
                { 
                    IsValid = false, 
                    Message = "Token is required" 
                });
            }

            // Apply rate limiting based on IP address
            if (_rateLimiter.ShouldLimit(ipAddress))
            {
                _logger.LogWarning("Token validation rate limit exceeded for {IpAddress}", ipAddress);
                return (false, new ShareTokenValidationResponse 
                { 
                    IsValid = false, 
                    Message = "Rate limit exceeded. Please try again later." 
                });
            }

            // Check cache first
            string cacheKey = $"sharetoken:{token}";
            var cachedValidationResult = await GetCachedValidationResultAsync(cacheKey);

            if (cachedValidationResult != null)
            {
                _logger.LogInformation("Using cached validation result for token from {IpAddress}", ipAddress);
                return (cachedValidationResult.IsValid, cachedValidationResult);
            }

            // If not in cache, validate from database
            try
            {
                var validationResult = await _shareTokenService.ValidateTokenAsync(token);
                
                // Cache the result for 1 minute (short time to avoid stale data but reduce DB load)
                if (validationResult.IsValid)
                {
                    await CacheValidationResultAsync(cacheKey, validationResult);
                }

                return (validationResult.IsValid, validationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token from {IpAddress}", ipAddress);
                return (false, new ShareTokenValidationResponse 
                { 
                    IsValid = false, 
                    Message = "An error occurred during validation" 
                });
            }
        }

        public async Task<bool> ValidateTokenPermissionAsync(string token, string permission, HttpContext httpContext)
        {
            var ipAddress = GetClientIpAddress(httpContext);
            var (isValid, validationResult) = await ValidateTokenAsync(token, ipAddress);

            if (!isValid || validationResult?.ShareToken == null)
            {
                return false;
            }

            // Check if the token has the requested permission
            return CheckIsAccessAllowed(validationResult.ShareToken, permission);
        }

        public bool CheckIsAccessAllowed(ShareTokenDto shareToken, string permission)
        {
            if (shareToken == null)
            {
                return false;
            }

            switch (permission.ToLowerInvariant())
            {
                case "session":
                    return shareToken.AllowSessionAccess;
                case "report":
                    return shareToken.AllowReportAccess;
                case "calculator":
                    return shareToken.AllowCalculatorAccess;
                default:
                    _logger.LogWarning("Unknown permission check: {Permission}", permission);
                    return false;
            }
        }

        public async Task ClearCacheForTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return;

            string cacheKey = $"sharetoken:{token}";
            await _cache.RemoveAsync(cacheKey);
            _logger.LogInformation("Cache cleared for token {TokenKey}", cacheKey);
        }

        private async Task<ShareTokenValidationResponse> GetCachedValidationResultAsync(string cacheKey)
        {
            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (string.IsNullOrEmpty(cachedData))
                return null;

            try
            {
                return JsonSerializer.Deserialize<ShareTokenValidationResponse>(cachedData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing cached validation result for {CacheKey}", cacheKey);
                return null;
            }
        }

        private async Task CacheValidationResultAsync(string cacheKey, ShareTokenValidationResponse validationResult)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                };

                var serializedData = JsonSerializer.Serialize(validationResult);
                await _cache.SetStringAsync(cacheKey, serializedData, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching validation result for {CacheKey}", cacheKey);
            }
        }

        private string GetClientIpAddress(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                httpContext = _httpContextAccessor.HttpContext;
            }

            if (httpContext == null)
            {
                return "unknown";
            }
            
            // First try to get from X-Forwarded-For header (for clients behind load balancers or proxies)
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // The X-Forwarded-For header can contain multiple IPs, we want the first one (client IP)
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