using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Alerting;

namespace WorkoutTrackerWeb.ViewComponents
{
    public class NotificationBadgeViewComponent : ViewComponent
    {
        private readonly IAlertingService _alertingService;
        private readonly UserManager<AppUser> _userManager;

        public NotificationBadgeViewComponent(
            IAlertingService alertingService,
            UserManager<AppUser> userManager)
        {
            _alertingService = alertingService;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Only proceed if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                return View(0);
            }

            try
            {
                var userId = _userManager.GetUserId(HttpContext.User);
                int unreadCount = await _alertingService.GetUnreadNotificationCountForUserAsync(userId);
                
                return View(unreadCount);
            }
            catch (Exception)
            {
                // Log error but don't crash the UI
                return View(0);
            }
        }
    }
}