using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Database
{
    [Authorize(Roles = "Admin")]
    public class ConnectionPoolModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<ConnectionPoolModel> _logger;
        private readonly ConnectionStringBuilderService _connectionStringBuilder;
        private readonly DatabaseResilienceService _resilienceService;

        public ConnectionPoolModel(
            WorkoutTrackerWebContext context,
            ILogger<ConnectionPoolModel> logger,
            ConnectionStringBuilderService connectionStringBuilder,
            DatabaseResilienceService resilienceService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionStringBuilder = connectionStringBuilder ?? throw new ArgumentNullException(nameof(connectionStringBuilder));
            _resilienceService = resilienceService ?? throw new ArgumentNullException(nameof(resilienceService));
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Verify admin access
            if (!User.IsInRole("Admin"))
            {
                _logger.LogWarning("Unauthorized access attempt to connection pool monitoring page");
                return Forbid();
            }

            try
            {
                // Just check that we can connect to the database to ensure the page loads properly
                await _context.Database.CanConnectAsync();
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading connection pool monitoring page");
                TempData["ErrorMessage"] = "Error connecting to database: " + ex.Message;
                return RedirectToPage("/Index");
            }
        }
    }
}