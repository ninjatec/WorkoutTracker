using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Shared
{
    [OutputCache(PolicyName = "StaticContent")]
    public class PrivacyModel : SharedPageModel
    {
        public PrivacyModel(
            IShareTokenService shareTokenService,
            ILogger<PrivacyModel> logger)
            : base(shareTokenService, logger)
        {
        }

        public async Task<IActionResult> OnGetAsync(string token = null)
        {
            // Privacy page is accessible without a valid token
            // But if a token is provided, validate and set the username for display
            if (!string.IsNullOrEmpty(token) || HttpContext.Request.Cookies.ContainsKey("share_token"))
            {
                await ValidateTokenAsync(token, null);
            }

            return Page();
        }
    }
}