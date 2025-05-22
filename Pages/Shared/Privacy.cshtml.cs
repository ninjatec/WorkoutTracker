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
            ITokenValidationService tokenValidationService,
            ILogger<PrivacyModel> logger)
            : base(tokenValidationService, logger)
        {
        }

        public async Task<IActionResult> OnGetAsync(string token = null)
        {
            // Update request token
            Token = token;
            
            // If token is provided, validate it
            if (!string.IsNullOrEmpty(token))
            {
                var isValid = await ValidateShareTokenAsync();
                if (isValid)
                {
                    ViewData["TokenIsValid"] = true;
                }
            }

            return Page();
        }
    }
}