using System;
using System.Collections.Generic;
using System.Linq;
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
    public class IndexModel : PageModel
    {
        private readonly IAlertingService _alertingService;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IAlertingService alertingService,
            ILogger<IndexModel> logger)
        {
            _alertingService = alertingService;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public IEnumerable<AlertThreshold> AlertThresholds { get; set; }
        
        public IEnumerable<Alert> ActiveAlerts { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                AlertThresholds = await _alertingService.GetAlertThresholdsAsync();
                ActiveAlerts = await _alertingService.GetActiveAlertsAsync();
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alert thresholds and active alerts");
                StatusMessage = $"Error: Unable to load alerts. {ex.Message}";
                return Page();
            }
        }
    }
}