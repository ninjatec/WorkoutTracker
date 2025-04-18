using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Services
{
    public class IdentityEventHandlers
    {
        private readonly LoginHistoryService _loginHistoryService;
        
        public IdentityEventHandlers(LoginHistoryService loginHistoryService)
        {
            _loginHistoryService = loginHistoryService;
        }
        
        public async Task OnLoginFailure(string username, string failureReason)
        {
            // Since we don't have the user ID at this point (login failed), we'll log it separately
            // This will be handled in the PasswordSignInAsync method
        }
    }
}