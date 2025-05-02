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
using WorkoutTrackerWeb.Data;

namespace WorkoutTrackerWeb.Services
{
    /// <summary>
    /// Defines the interface for share token management, allowing users to create and manage tokens
    /// that provide limited access to their workout data.
    /// </summary>
    public interface IShareTokenService
    {
        /// <summary>
        /// Retrieves all share tokens created by a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user whose tokens to retrieve.</param>
        /// <returns>A list of share token DTOs for the specified user.</returns>
        Task<List<ShareTokenDto>> GetUserShareTokensAsync(int userId);
        
        /// <summary>
        /// Retrieves a specific share token by its ID.
        /// </summary>
        /// <param name="id">The ID of the share token to retrieve.</param>
        /// <param name="userId">The ID of the user who owns the token.</param>
        /// <returns>The share token DTO if found and owned by the user; otherwise null.</returns>
        Task<ShareTokenDto> GetShareTokenByIdAsync(int id, int userId);
        
        /// <summary>
        /// Creates a new share token for a user based on the provided request.
        /// </summary>
        /// <param name="request">The token creation request containing parameters like permissions and expiry.</param>
        /// <param name="userId">The ID of the user creating the token.</param>
        /// <returns>The newly created share token DTO.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the user or session doesn't exist.</exception>
        Task<ShareTokenDto> CreateShareTokenAsync(CreateShareTokenRequest request, int userId);
        
        /// <summary>
        /// Validates a share token to determine if it can be used to access shared content.
        /// </summary>
        /// <param name="token">The token string to validate.</param>
        /// <param name="incrementAccessCount">Whether to increment the access count on successful validation.</param>
        /// <returns>A validation response indicating if the token is valid and providing details.</returns>
        Task<ShareTokenValidationResponse> ValidateTokenAsync(string token, bool incrementAccessCount = true);
        
        /// <summary>
        /// Updates an existing share token with new settings.
        /// </summary>
        /// <param name="id">The ID of the share token to update.</param>
        /// <param name="request">The update request containing the new token settings.</param>
        /// <param name="userId">The ID of the user who owns the token.</param>
        /// <returns>The updated share token DTO if successful; otherwise null.</returns>
        Task<ShareTokenDto> UpdateShareTokenAsync(int id, UpdateShareTokenRequest request, int userId);
        
        /// <summary>
        /// Permanently deletes a share token.
        /// </summary>
        /// <param name="id">The ID of the share token to delete.</param>
        /// <param name="userId">The ID of the user who owns the token.</param>
        /// <returns>True if the token was successfully deleted; otherwise false.</returns>
        Task<bool> DeleteShareTokenAsync(int id, int userId);
        
        /// <summary>
        /// Revokes a share token, making it invalid for future use without deleting it.
        /// </summary>
        /// <param name="id">The ID of the share token to revoke.</param>
        /// <param name="userId">The ID of the user who owns the token.</param>
        /// <returns>True if the token was successfully revoked; otherwise false.</returns>
        Task<bool> RevokeShareTokenAsync(int id, int userId);
    }

    /// <summary>
    /// Implements token sharing functionality, allowing users to securely share access 
    /// to their workout data with others.
    /// </summary>
    /// <remarks>
    /// Share tokens can grant different levels of access (session, reports, calculator)
    /// and can have expiration dates and usage limits for security.
    /// </remarks>
    public class ShareTokenService : IShareTokenService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<ShareTokenService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShareTokenService"/> class.
        /// </summary>
        /// <param name="context">The database context for accessing share token data.</param>
        /// <param name="logger">The logger for recording service activities.</param>
        public ShareTokenService(WorkoutTrackerWebContext context, ILogger<ShareTokenService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<List<ShareTokenDto>> GetUserShareTokensAsync(int userId)
        {
            try
            {
                var shareTokens = await _context.ShareToken
                    .Include(st => st.User)
                    .Include(st => st.WorkoutSession)
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

        /// <inheritdoc />
        public async Task<ShareTokenDto> GetShareTokenByIdAsync(int id, int userId)
        {
            try
            {
                var shareToken = await _context.ShareToken
                    .Include(st => st.User)
                    .Include(st => st.WorkoutSession)
                    .FirstOrDefaultAsync(st => st.Id == id && st.UserId == userId);

                return shareToken != null ? MapToDto(shareToken) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting share token {TokenId} for user {UserId}", id, userId);
                throw;
            }
        }

        /// <inheritdoc />
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

                WorkoutSession workoutSession = null;
                if (request.WorkoutSessionId.HasValue)
                {
                    workoutSession = await _context.WorkoutSessions
                        .FirstOrDefaultAsync(ws => ws.WorkoutSessionId == request.WorkoutSessionId.Value && ws.UserId == userId);
                    
                    if (workoutSession == null)
                    {
                        throw new InvalidOperationException($"WorkoutSession with ID {request.WorkoutSessionId.Value} not found or does not belong to user");
                    }
                }

                // Ensure at least session access is allowed
                if (!request.AllowSessionAccess)
                {
                    _logger.LogWarning("Forcing AllowSessionAccess to true because it's required for shared access");
                    request.AllowSessionAccess = true;
                }

                var shareToken = new ShareToken
                {
                    Token = token,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(request.ExpiryDays),
                    IsActive = true,
                    AccessCount = 0,
                    MaxAccessCount = request.MaxAccessCount,
                    AllowSessionAccess = request.AllowSessionAccess, // Now guaranteed to be true
                    AllowReportAccess = request.AllowReportAccess,
                    AllowCalculatorAccess = request.AllowCalculatorAccess,
                    UserId = userId,
                    WorkoutSessionId = request.WorkoutSessionId,
                    Name = request.Name ?? $"Share {DateTime.UtcNow:yyyy-MM-dd}",
                    Description = request.Description
                };

                _context.ShareToken.Add(shareToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created share token {TokenId} for user {UserId} with permissions: AllowSessionAccess={AllowSessionAccess}, AllowReportAccess={AllowReportAccess}, AllowCalculatorAccess={AllowCalculatorAccess}", 
                    shareToken.Id, userId, shareToken.AllowSessionAccess, shareToken.AllowReportAccess, shareToken.AllowCalculatorAccess);
                
                return MapToDto(shareToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating share token for user {UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ShareTokenValidationResponse> ValidateTokenAsync(string token, bool incrementAccessCount = true)
        {
            try
            {
                _logger.LogInformation("Validating token: {Token}", token);
                
                var shareToken = await _context.ShareToken
                    .Include(st => st.User)
                    .Include(st => st.WorkoutSession)
                    .FirstOrDefaultAsync(st => st.Token == token);

                if (shareToken == null)
                {
                    _logger.LogWarning("Token validation failed: Token not found in database: {Token}", token);
                    return new ShareTokenValidationResponse
                    {
                        IsValid = false,
                        Message = "Invalid share token"
                    };
                }

                _logger.LogInformation("Token found: ID={Id}, UserId={UserId}, Created={Created}, Expires={Expires}, IsActive={IsActive}, AccessCount={AccessCount}, MaxAccessCount={MaxAccessCount}, AllowSessionAccess={AllowSessionAccess}, AllowReportAccess={AllowReportAccess}, AllowCalculatorAccess={AllowCalculatorAccess}", 
                    shareToken.Id, shareToken.UserId, shareToken.CreatedAt, shareToken.ExpiresAt, shareToken.IsActive, shareToken.AccessCount, shareToken.MaxAccessCount, shareToken.AllowSessionAccess, shareToken.AllowReportAccess, shareToken.AllowCalculatorAccess);

                // Check if token is still valid
                if (!shareToken.IsValid)
                {
                    string reason = "Token is invalid";
                    if (!shareToken.IsActive)
                    {
                        reason = "Token has been revoked";
                        _logger.LogWarning("Token validation failed: Token has been revoked: ID={Id}", shareToken.Id);
                    }
                    else if (shareToken.ExpiresAt <= DateTime.UtcNow)
                    {
                        reason = "Token has expired";
                        _logger.LogWarning("Token validation failed: Token has expired: ID={Id}, Expired={Expired}, CurrentTime={CurrentTime}", 
                            shareToken.Id, shareToken.ExpiresAt, DateTime.UtcNow);
                    }
                    else if (shareToken.MaxAccessCount.HasValue && shareToken.AccessCount >= shareToken.MaxAccessCount.Value)
                    {
                        reason = "Token has reached maximum usage limit";
                        _logger.LogWarning("Token validation failed: Token reached usage limit: ID={Id}, AccessCount={AccessCount}, MaxAccessCount={MaxAccessCount}", 
                            shareToken.Id, shareToken.AccessCount, shareToken.MaxAccessCount);
                    }
                    else
                    {
                        _logger.LogWarning("Token validation failed: Unknown reason: ID={Id}, IsValid={IsValid}", shareToken.Id, shareToken.IsValid);
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
                    _logger.LogInformation("Access count incremented for share token {TokenId}, new count: {AccessCount}", shareToken.Id, shareToken.AccessCount);
                }

                _logger.LogInformation("Token validation successful: ID={Id}", shareToken.Id);
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

        /// <inheritdoc />
        public async Task<ShareTokenDto> UpdateShareTokenAsync(int id, UpdateShareTokenRequest request, int userId)
        {
            try
            {
                var shareToken = await _context.ShareToken
                    .Include(st => st.User)
                    .Include(st => st.WorkoutSession)
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <summary>
        /// Generates a cryptographically secure random token for sharing access.
        /// </summary>
        /// <returns>A URL-safe base64 encoded random token string.</returns>
        private string GenerateSecureToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32]; // 256 bits
            rng.GetBytes(bytes);
            
            var token = Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
                
            return token;
        }

        /// <summary>
        /// Maps a ShareToken entity to its corresponding DTO representation.
        /// </summary>
        /// <param name="shareToken">The share token entity to map.</param>
        /// <returns>A DTO representing the share token, or null if the input is null.</returns>
        private static ShareTokenDto MapToDto(ShareToken shareToken)
        {
            if (shareToken == null) return null;
            
            var dto = new ShareTokenDto
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
                WorkoutSessionId = shareToken.WorkoutSessionId,
                WorkoutSessionName = shareToken.WorkoutSession?.Name,
                Name = shareToken.Name,
                Description = shareToken.Description,
                IsValid = shareToken.IsValid,
                DaysUntilExpiration = shareToken.DaysUntilExpiration,
                HasUsageLimits = shareToken.HasUsageLimits,
                RemainingUses = shareToken.RemainingUses
            };
            
            Console.WriteLine($"MapToDto: AllowSessionAccess={dto.AllowSessionAccess}, AllowReportAccess={dto.AllowReportAccess}, AllowCalculatorAccess={dto.AllowCalculatorAccess}");
            
            return dto;
        }

        /// <summary>
        /// Creates a new share token for a user.
        /// </summary>
        /// <param name="userId">The ID of the user creating the token.</param>
        /// <param name="workoutSessionId">The ID of the workout session to associate with the token, if any.</param>
        /// <returns>The newly created share token DTO.</returns>
        public async Task<ShareTokenDto> CreateTokenAsync(int userId, int? workoutSessionId = null)
        {
            // Validate access to session if specified
            if (workoutSessionId.HasValue)
            {
                var session = await _context.WorkoutSessions
                    .FirstOrDefaultAsync(s => s.WorkoutSessionId == workoutSessionId && s.UserId == userId);

                if (session == null)
                {
                    throw new InvalidOperationException("Cannot create share token - session not found or access denied");
                }
            }

            var token = new ShareToken
            {
                Token = GenerateSecureToken(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30), // DefaultTokenValidityDays
                UserId = userId,
                WorkoutSessionId = workoutSessionId,
                IsActive = true
            };

            _context.ShareToken.Add(token);
            await _context.SaveChangesAsync();

            return MapToDto(token);
        }
    }
}