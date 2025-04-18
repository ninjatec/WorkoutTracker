using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Dtos;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Account.Manage
{
    [Authorize]
    public class ShareTokensModel : PageModel
    {
        private readonly IShareTokenService _shareTokenService;
        private readonly ITokenValidationService _tokenValidationService;
        private readonly UserService _userService;
        private readonly ILogger<ShareTokensModel> _logger;

        public ShareTokensModel(
            IShareTokenService shareTokenService,
            ITokenValidationService tokenValidationService,
            UserService userService,
            ILogger<ShareTokensModel> logger)
        {
            _shareTokenService = shareTokenService;
            _tokenValidationService = tokenValidationService;
            _userService = userService;
            _logger = logger;
        }

        public List<ShareTokenDto> UserTokens { get; set; } = new List<ShareTokenDto>();
        
        public IEnumerable<SelectListItem> SessionItems { get; set; } = new List<SelectListItem>();
        
        [BindProperty]
        public CreateTokenInputModel CreateTokenInput { get; set; }
        
        [BindProperty]
        public EditTokenInputModel EditTokenInput { get; set; }
        
        [TempData]
        public string StatusMessage { get; set; }

        public class CreateTokenInputModel
        {
            [Required]
            [Range(1, 365, ErrorMessage = "Expiry days must be between 1 and 365")]
            [Display(Name = "Token expiry (days)")]
            public int ExpiryDays { get; set; } = 7;
            
            [Display(Name = "Share specific session only")]
            public int? SessionId { get; set; }
            
            [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
            [Display(Name = "Name (optional)")]
            public string Name { get; set; }
            
            [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
            [Display(Name = "Description (optional)")]
            public string Description { get; set; }
            
            [Display(Name = "Maximum number of uses (optional)")]
            public int? MaxAccessCount { get; set; }
            
            [Display(Name = "Allow access to sessions")]
            public bool AllowSessionAccess { get; set; } = true;
            
            [Display(Name = "Allow access to reports")]
            public bool AllowReportAccess { get; set; } = true;
            
            [Display(Name = "Allow access to calculator")]
            public bool AllowCalculatorAccess { get; set; } = true;
        }

        public class EditTokenInputModel
        {
            [Required]
            public int Id { get; set; }
            
            [Required]
            [Range(0, 365, ErrorMessage = "Expiry days must be between 0 and 365")]
            [Display(Name = "Extra days until expiry")]
            public int ExpiryDays { get; set; } = 7;
            
            [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
            [Display(Name = "Name")]
            public string Name { get; set; }
            
            [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
            [Display(Name = "Description")]
            public string Description { get; set; }
            
            [Display(Name = "Maximum number of uses")]
            public int? MaxAccessCount { get; set; }
            
            [Display(Name = "Allow access to sessions")]
            public bool AllowSessionAccess { get; set; } = true;
            
            [Display(Name = "Allow access to reports")]
            public bool AllowReportAccess { get; set; } = true;
            
            [Display(Name = "Allow access to calculator")]
            public bool AllowCalculatorAccess { get; set; } = true;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                // Get user's sessions for the dropdown
                await LoadSessionsAsync(userId.Value);
                
                // Get user's share tokens
                UserTokens = await _shareTokenService.GetUserShareTokensAsync(userId.Value);
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading share tokens page");
                StatusMessage = "Error: An error occurred while loading your share tokens.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                if (!ModelState.IsValid)
                {
                    await LoadSessionsAsync(userId.Value);
                    UserTokens = await _shareTokenService.GetUserShareTokensAsync(userId.Value);
                    return Page();
                }

                var createRequest = new CreateShareTokenRequest
                {
                    ExpiryDays = CreateTokenInput.ExpiryDays,
                    SessionId = CreateTokenInput.SessionId,
                    Name = CreateTokenInput.Name,
                    Description = CreateTokenInput.Description,
                    MaxAccessCount = CreateTokenInput.MaxAccessCount,
                    AllowSessionAccess = CreateTokenInput.AllowSessionAccess,
                    AllowReportAccess = CreateTokenInput.AllowReportAccess,
                    AllowCalculatorAccess = CreateTokenInput.AllowCalculatorAccess
                };

                var createdToken = await _shareTokenService.CreateShareTokenAsync(createRequest, userId.Value);
                StatusMessage = "Success: Share token created successfully.";
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating share token");
                StatusMessage = "Error: Failed to create share token.";
                
                var userId = await GetCurrentUserIdAsync();
                if (userId.HasValue)
                {
                    await LoadSessionsAsync(userId.Value);
                    UserTokens = await _shareTokenService.GetUserShareTokensAsync(userId.Value);
                }
                
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                if (!ModelState.IsValid)
                {
                    await LoadSessionsAsync(userId.Value);
                    UserTokens = await _shareTokenService.GetUserShareTokensAsync(userId.Value);
                    return Page();
                }

                // Get the token to make sure it belongs to the user
                var existingToken = await _shareTokenService.GetShareTokenByIdAsync(EditTokenInput.Id, userId.Value);
                if (existingToken == null)
                {
                    StatusMessage = "Error: Share token not found.";
                    return RedirectToPage();
                }

                var updateRequest = new UpdateShareTokenRequest
                {
                    ExpiryDays = EditTokenInput.ExpiryDays,
                    Name = EditTokenInput.Name,
                    Description = EditTokenInput.Description,
                    MaxAccessCount = EditTokenInput.MaxAccessCount,
                    AllowSessionAccess = EditTokenInput.AllowSessionAccess,
                    AllowReportAccess = EditTokenInput.AllowReportAccess,
                    AllowCalculatorAccess = EditTokenInput.AllowCalculatorAccess
                };

                var updatedToken = await _shareTokenService.UpdateShareTokenAsync(EditTokenInput.Id, updateRequest, userId.Value);
                StatusMessage = "Success: Share token updated successfully.";
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating share token");
                StatusMessage = "Error: Failed to update share token.";
                
                var userId = await GetCurrentUserIdAsync();
                if (userId.HasValue)
                {
                    await LoadSessionsAsync(userId.Value);
                    UserTokens = await _shareTokenService.GetUserShareTokensAsync(userId.Value);
                }
                
                return Page();
            }
        }

        public async Task<IActionResult> OnPostRevokeAsync(int id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                // Get the token to make sure it belongs to the user
                var existingToken = await _shareTokenService.GetShareTokenByIdAsync(id, userId.Value);
                if (existingToken == null)
                {
                    StatusMessage = "Error: Share token not found.";
                    return RedirectToPage();
                }

                var result = await _shareTokenService.RevokeShareTokenAsync(id, userId.Value);
                if (result)
                {
                    // Clear the token from cache
                    await _tokenValidationService.ClearCacheForTokenAsync(existingToken.Token);
                    
                    StatusMessage = "Success: Share token revoked successfully.";
                }
                else
                {
                    StatusMessage = "Error: Failed to revoke share token.";
                }
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking share token");
                StatusMessage = "Error: Failed to revoke share token.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                // Get the token to make sure it belongs to the user
                var existingToken = await _shareTokenService.GetShareTokenByIdAsync(id, userId.Value);
                if (existingToken == null)
                {
                    StatusMessage = "Error: Share token not found.";
                    return RedirectToPage();
                }

                var result = await _shareTokenService.DeleteShareTokenAsync(id, userId.Value);
                if (result)
                {
                    // Clear the token from cache
                    await _tokenValidationService.ClearCacheForTokenAsync(existingToken.Token);
                    
                    StatusMessage = "Success: Share token deleted successfully.";
                }
                else
                {
                    StatusMessage = "Error: Failed to delete share token.";
                }
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting share token");
                StatusMessage = "Error: Failed to delete share token.";
                return RedirectToPage();
            }
        }
        
        private async Task<int?> GetCurrentUserIdAsync()
        {
            var user = await _userService.GetOrCreateCurrentUserAsync();
            return user?.UserId;
        }
        
        private async Task LoadSessionsAsync(int userId)
        {
            try
            {
                // Get the last 50 sessions for the dropdown
                var sessions = await _userService.GetUserSessionsAsync(userId, 50);
                
                SessionItems = sessions.Select(s => new SelectListItem
                {
                    Value = s.SessionId.ToString(),
                    Text = $"{s.Name} - {s.datetime:g}"
                }).ToList();
                
                // Add an empty item at the beginning
                var emptyItem = new SelectListItem
                {
                    Value = "",
                    Text = "--- All sessions ---",
                    Selected = true
                };
                
                SessionItems = new[] { emptyItem }.Concat(SessionItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sessions for share token page");
                // Don't throw - just leave the sessions list empty
                SessionItems = new List<SelectListItem>();
            }
        }
    }
}