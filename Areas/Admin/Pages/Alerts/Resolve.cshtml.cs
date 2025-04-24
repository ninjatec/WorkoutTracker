using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Models.Alerting;
using WorkoutTrackerWeb.Services.Alerting;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Alerts
{
    [Authorize(Policy = "RequireAdminRole")]
    public class ResolveModel : PageModel
    {
        private readonly IAlertingService _alertingService;
        private readonly ILogger<ResolveModel> _logger;

        public ResolveModel(
            IAlertingService alertingService,
            ILogger<ResolveModel> logger)
        {
            _alertingService = alertingService;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public Alert Alert { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Alert = await _alertingService.GetAlertAsync(id);

            if (Alert == null)
            {
                return NotFound($"Alert with ID {id} not found.");
            }

            if (Alert.ResolvedAt.HasValue)
            {
                StatusMessage = "This alert has already been resolved.";
                return RedirectToPage("./Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var success = await _alertingService.ResolveAlertAsync(Alert.Id);

                if (success)
                {
                    _logger.LogInformation("Alert {AlertId} resolved by {UserName}", Alert.Id, User.Identity.Name);
                    StatusMessage = "Alert resolved successfully and moved to history.";
                    return RedirectToPage("./Index");
                }
                else
                {
                    _logger.LogWarning("Failed to resolve alert {AlertId}", Alert.Id);
                    StatusMessage = "Failed to resolve alert. It may have been deleted or already resolved.";
                    return RedirectToPage("./Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving alert {AlertId}", Alert.Id);
                StatusMessage = $"Error: {ex.Message}";
                Alert = await _alertingService.GetAlertAsync(Alert.Id);
                return Page();
            }
        }
    }
}