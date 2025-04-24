using System;
using System.Collections.Generic;
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
    public class HistoryModel : PageModel
    {
        private readonly IAlertingService _alertingService;
        private readonly ILogger<HistoryModel> _logger;

        public HistoryModel(
            IAlertingService alertingService,
            ILogger<HistoryModel> logger)
        {
            _alertingService = alertingService;
            _logger = logger;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public IEnumerable<AlertHistory> AlertHistory { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int MaxResults { get; set; } = 100;

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Set default date range if not provided
                if (!FromDate.HasValue)
                {
                    FromDate = DateTime.UtcNow.AddDays(-7);
                }

                if (!ToDate.HasValue)
                {
                    ToDate = DateTime.UtcNow;
                }

                // Ensure MaxResults is within reasonable bounds
                if (MaxResults < 10)
                {
                    MaxResults = 10;
                }
                else if (MaxResults > 1000)
                {
                    MaxResults = 1000;
                }

                // Get alert history with the specified filters
                AlertHistory = await _alertingService.GetAlertHistoryAsync(FromDate, ToDate, MaxResults);
                
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alert history");
                StatusMessage = $"Error: Unable to load alert history. {ex.Message}";
                AlertHistory = new List<AlertHistory>();
                return Page();
            }
        }
    }
}