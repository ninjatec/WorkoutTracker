using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Dtos;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerweb.Data;

namespace WorkoutTrackerWeb.Services
{
    public interface IShareTokenService
    {
        Task<List<ShareTokenDto>> GetUserShareTokensAsync(int userId);
        Task<ShareTokenDto> GetShareTokenByIdAsync(int id, int userId);
        Task<ShareTokenDto> CreateShareTokenAsync(CreateShareTokenRequest request, int userId);
        Task<ShareTokenValidationResponse> ValidateTokenAsync(string token, bool incrementAccessCount = true);
        Task<ShareTokenDto> UpdateShareTokenAsync(int id, UpdateShareTokenRequest request, int userId);
        Task<bool> DeleteShareTokenAsync(int id, int userId);
        Task<bool> RevokeShareTokenAsync(int id, int userId);
    }

    public class ShareTokenService : IShareTokenService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<ShareTokenService> _logger;

        public ShareTokenService(WorkoutTrackerWebContext context, ILogger<ShareTokenService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<ShareTokenDto>> GetUserShareTokensAsync(int userId)
        {
            try
            {
                var shareTokens = await _context.ShareToken
                    .Include(st => st.User)
                    .Include(st => st.Session)
                    .Where(st => st.UserId == userId)
                    .OrderByDescending(st => st.CreatedAt)
                    .ToListAsync();

                return shareTokens.Select(st => MapToDto(st)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user share tokens for user {UserId}", userId);
                throw;
            }
        }

        public async Task<ShareTokenDto> GetShareTokenByIdAsync(int id, int userId)
        {
            try
            {
                var shareToken = await _context.ShareToken
                    .Include(st => st.User)
                    .Include(st => st.Session)
                    .FirstOrDefaultAsync(st => st.Id == id && st.UserId == userId);

                return shareToken != null ? MapToDto(shareToken) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting share token {TokenId} for user {UserId}", id, userId);
                throw;
            }
        }

        public async Task<ShareTokenDto> CreateShareTokenAsync(CreateShareTokenRequest request, int userId)
        {
            try
            {
                // Generate a secure random token
                var token = GenerateSecureToken();
                
                var user = await _context.User.FindAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException($"User with ID {userId} not found");
                }

                Session session = null;
                if (request.SessionId.HasValue)
                {
                    session = await _context.Session
                        .FirstOrDefaultAsync(s => s.SessionId == request.SessionId.Value && s.UserId == userId);
                    
                    if (session == null)
                    {
                        throw new InvalidOperationException($"Session with ID {request.SessionId.Value} not found or does not belong to user");
                    }
                }

                var shareToken = new ShareToken
                {
                    Token = token,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(request.ExpiryDays),
                    IsActive = true,
                    AccessCount = 0,
                    MaxAccessCount = request.MaxAccessCount,
                    AllowSessionAccess = request.AllowSessionAccess,
                    AllowReportAccess = request.AllowReportAccess,
                    AllowCalculatorAccess = request.AllowCalculatorAccess,
                    UserId = userId,
                    SessionId = request.SessionId,
                    Name = request.Name ?? $"Share {DateTime.UtcNow:yyyy-MM-dd}",
                    Description = request.Description
                };

                _context.ShareToken.Add(shareToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created share token {TokenId} for user {UserId}", shareToken.Id, userId);
                
                return MapToDto(shareToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating share token for user {UserId}", userId);
                throw;
            }
        }

        public async Task<ShareTokenValidationResponse> ValidateTokenAsync(string token, bool incrementAccessCount = true)
        {
            try
            {
                var shareToken = await _context.ShareToken
                    .Include(st => st.User)
                    .Include(st => st.Session)
                    .FirstOrDefaultAsync(st => st.Token == token);

                if (shareToken == null)
                {
                    return new ShareTokenValidationResponse
                    {
                        IsValid = false,
                        Message = "Invalid share token"
                    };
                }

                // Check if token is still valid
                if (!shareToken.IsValid)
                {
                    string reason = "Token is invalid";
                    if (!shareToken.IsActive)
                    {
                        reason = "Token has been revoked";
                    }
                    else if (shareToken.ExpiresAt <= DateTime.UtcNow)
                    {
                        reason = "Token has expired";
                    }
                    else if (shareToken.MaxAccessCount.HasValue && shareToken.AccessCount >= shareToken.MaxAccessCount.Value)
                    {
                        reason = "Token has reached maximum usage limit";
                    }

                    return new ShareTokenValidationResponse
                    {
                        IsValid = false,
                        Message = reason,
                        ShareToken = MapToDto(shareToken)
                    };
                }

                // Increment access count if requested
                if (incrementAccessCount)
                {
                    shareToken.AccessCount++;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Access count incremented for share token {TokenId}", shareToken.Id);
                }

                return new ShareTokenValidationResponse
                {
                    IsValid = true,
                    Message = "Token is valid",
                    ShareToken = MapToDto(shareToken)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating share token {Token}", token);
                throw;
            }
        }

        public async Task<ShareTokenDto> UpdateShareTokenAsync(int id, UpdateShareTokenRequest request, int userId)
        {
            try
            {
                var shareToken = await _context.ShareToken
                    .Include(st => st.User)
                    .Include(st => st.Session)
                    .FirstOrDefaultAsync(st => st.Id == id && st.UserId == userId);

                if (shareToken == null)
                {
                    return null;
                }

                // Update properties
                shareToken.ExpiresAt = DateTime.UtcNow.AddDays(request.ExpiryDays);
                shareToken.MaxAccessCount = request.MaxAccessCount;
                shareToken.AllowSessionAccess = request.AllowSessionAccess;
                shareToken.AllowReportAccess = request.AllowReportAccess;
                shareToken.AllowCalculatorAccess = request.AllowCalculatorAccess;
                shareToken.Name = request.Name;
                shareToken.Description = request.Description;

                _context.ShareToken.Update(shareToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated share token {TokenId} for user {UserId}", id, userId);
                
                return MapToDto(shareToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating share token {TokenId} for user {UserId}", id, userId);
                throw;
            }
        }

        public async Task<bool> DeleteShareTokenAsync(int id, int userId)
        {
            try
            {
                var shareToken = await _context.ShareToken
                    .FirstOrDefaultAsync(st => st.Id == id && st.UserId == userId);

                if (shareToken == null)
                {
                    return false;
                }

                _context.ShareToken.Remove(shareToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted share token {TokenId} for user {UserId}", id, userId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting share token {TokenId} for user {UserId}", id, userId);
                throw;
            }
        }

        public async Task<bool> RevokeShareTokenAsync(int id, int userId)
        {
            try
            {
                var shareToken = await _context.ShareToken
                    .FirstOrDefaultAsync(st => st.Id == id && st.UserId == userId);

                if (shareToken == null)
                {
                    return false;
                }

                shareToken.IsActive = false;
                _context.ShareToken.Update(shareToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Revoked share token {TokenId} for user {UserId}", id, userId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking share token {TokenId} for user {UserId}", id, userId);
                throw;
            }
        }

        private string GenerateSecureToken()
        {
            // Generate a cryptographically secure random token
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32]; // 256 bits
            rng.GetBytes(bytes);
            
            // Convert to URL-safe base64 string
            var token = Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
                
            return token;
        }

        private static ShareTokenDto MapToDto(ShareToken shareToken)
        {
            if (shareToken == null) return null;
            
            return new ShareTokenDto
            {
                Id = shareToken.Id,
                Token = shareToken.Token,
                CreatedAt = shareToken.CreatedAt,
                ExpiresAt = shareToken.ExpiresAt,
                IsActive = shareToken.IsActive,
                AccessCount = shareToken.AccessCount,
                MaxAccessCount = shareToken.MaxAccessCount,
                AllowSessionAccess = shareToken.AllowSessionAccess,
                AllowReportAccess = shareToken.AllowReportAccess,
                AllowCalculatorAccess = shareToken.AllowCalculatorAccess,
                UserId = shareToken.UserId,
                UserName = shareToken.User?.Name,
                SessionId = shareToken.SessionId,
                SessionName = shareToken.Session?.Name,
                Name = shareToken.Name,
                Description = shareToken.Description,
                IsValid = shareToken.IsValid,
                DaysUntilExpiration = shareToken.DaysUntilExpiration,
                HasUsageLimits = shareToken.HasUsageLimits,
                RemainingUses = shareToken.RemainingUses
            };
        }
    }
}