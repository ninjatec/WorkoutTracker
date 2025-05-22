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
    public class CreateModel : PageModel
    {
        private readonly IAlertingService _alertingService;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            IAlertingService alertingService,
            ILogger<CreateModel> logger)
        {
            _alertingService = alertingService;
            _logger = logger;
        }

        [BindProperty]
        public AlertThreshold AlertThreshold { get; set; } = new AlertThreshold();

        public void OnGet()
        {
            // Initialize with default values
            AlertThreshold.IsEnabled = true;
            AlertThreshold.EmailEnabled = true;
            AlertThreshold.NotificationEnabled = true;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var userName = User.Identity.Name;
                await _alertingService.CreateAlertThresholdAsync(AlertThreshold, userName);

                _logger.LogInformation("Alert threshold created for {MetricName} by {UserName}", 
                    AlertThreshold.MetricName, userName);

                TempData["StatusMessage"] = $"Alert threshold for {AlertThreshold.MetricName} created successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert threshold");
                ModelState.AddModelError(string.Empty, $"Error creating alert threshold: {ex.Message}");
                return Page();
            }
        }
    }
}