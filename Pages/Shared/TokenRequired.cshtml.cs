using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WorkoutTrackerWeb.Pages.Shared
{
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