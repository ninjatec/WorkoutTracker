using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;

namespace WorkoutTrackerWeb.Pages.Shared
{
    [OutputCache(PolicyName = "StaticContent")]
    public class TokenRequiredModel : PageModel
    {
        public string Token { get; set; }
        public string ErrorMessage { get; set; }

        public void OnGet(string token = null, string errorMessage = null)
        {
            Token = token;
            ErrorMessage = errorMessage;
        }
    }
}