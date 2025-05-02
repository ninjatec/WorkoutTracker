using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.ViewModels;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Pages.Shared
{
    public abstract class SharedPageModel : PageModel
    {
        protected readonly ITokenValidationService _tokenValidationService;
        protected readonly ILogger _logger;

        [BindProperty(SupportsGet = true)]
        public string Token { get; set; }

        public ShareTokenValidationResult SharedTokenData { get; private set; }

        public int? SessionId { get; set; }

        protected SharedPageModel(ITokenValidationService tokenValidationService, ILogger logger)
        {
            _tokenValidationService = tokenValidationService;
            _logger = logger;
        }

        protected async Task<bool> ValidateShareTokenAsync()
        {
            // Validate token if present in URL
            if (!string.IsNullOrEmpty(Token))
            {
                var tokenData = await _tokenValidationService.ValidateShareTokenAsync(Token);
                if (tokenData != null && tokenData.IsValid)
                {
                    _logger.LogInformation("Valid share token used: {Token}", Token);
                    
                    // Store token data for later use
                    SharedTokenData = tokenData;
                    
                    // Validate session-specific token if applicable
                    if (SessionId.HasValue && SessionId.Value > 0 && tokenData.SessionId > 0 && SessionId.Value != tokenData.SessionId)
                    {
                        _logger.LogWarning("Session-specific token used for wrong session. Expected: {Expected}, Actual: {Actual}", 
                            tokenData.SessionId, SessionId);
                        return false;
                    }
                    
                    return true;
                }
                
                _logger.LogWarning("Invalid share token used: {Token}", Token);
            }
            
            return false;
        }
    }
}