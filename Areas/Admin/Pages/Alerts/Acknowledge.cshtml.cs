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
    public class AcknowledgeModel : PageModel
    {
        private readonly IAlertingService _alertingService;
        private readonly ILogger<AcknowledgeModel> _logger;

        public AcknowledgeModel(
            IAlertingService alertingService,
            ILogger<AcknowledgeModel> logger)
        {
            _alertingService = alertingService;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public Alert Alert { get; set; }

        [BindProperty]
        public string AcknowledgementNote { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Alert = await _alertingService.GetAlertAsync(id);

            if (Alert == null)
            {
                return NotFound($"Alert with ID {id} not found.");
            }

            if (Alert.IsAcknowledged)
            {
                StatusMessage = "This alert has already been acknowledged.";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(AcknowledgementNote))
            {
                ModelState.AddModelError(nameof(AcknowledgementNote), "Please provide an acknowledgement note.");
                Alert = await _alertingService.GetAlertAsync(Alert.Id);
                return Page();
            }

            try
            {
                var userName = User.Identity.Name;
                var success = await _alertingService.AcknowledgeAlertAsync(Alert.Id, userName, AcknowledgementNote);

                if (success)
                {
                    _logger.LogInformation("Alert {AlertId} acknowledged by {UserName}", Alert.Id, userName);
                    StatusMessage = "Alert acknowledged successfully.";
                    return RedirectToPage("./Index");
                }
                else
                {
                    _logger.LogWarning("Failed to acknowledge alert {AlertId}", Alert.Id);
                    StatusMessage = "Failed to acknowledge alert. It may have been deleted or already resolved.";
                    return RedirectToPage("./Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging alert {AlertId}", Alert.Id);
                StatusMessage = $"Error: {ex.Message}";
                Alert = await _alertingService.GetAlertAsync(Alert.Id);
                return Page();
            }
        }
    }
}