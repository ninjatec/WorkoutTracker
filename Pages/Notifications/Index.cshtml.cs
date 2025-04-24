using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Models.Alerting;
using WorkoutTrackerWeb.Services.Alerting;

namespace WorkoutTrackerWeb.Pages.Notifications
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IAlertingService _alertingService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IAlertingService alertingService,
            UserManager<IdentityUser> userManager,
            ILogger<IndexModel> logger)
        {
            _alertingService = alertingService;
            _userManager = userManager;
            _logger = logger;
        }

        public IEnumerable<Notification> Notifications { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        public int UnreadCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IncludeRead { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                
                // Get notifications
                Notifications = await _alertingService.GetNotificationsForUserAsync(userId, IncludeRead);
                
                // Get count of unread notifications for display
                UnreadCount = await _alertingService.GetUnreadNotificationCountForUserAsync(userId);
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                StatusMessage = $"Error: Unable to load notifications. {ex.Message}";
                Notifications = new List<Notification>();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostMarkReadAsync(int notificationId)
        {
            try
            {
                var success = await _alertingService.MarkNotificationAsReadAsync(notificationId);
                
                if (success)
                {
                    StatusMessage = "Notification marked as read.";
                }
                else
                {
                    StatusMessage = "Notification not found or already marked as read.";
                }
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                StatusMessage = $"Error: {ex.Message}";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostMarkAllReadAsync()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var success = await _alertingService.MarkAllNotificationsAsReadAsync(userId);
                
                if (success)
                {
                    StatusMessage = "All notifications marked as read.";
                }
                else
                {
                    StatusMessage = "No unread notifications to mark as read.";
                }
                
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                StatusMessage = $"Error: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}