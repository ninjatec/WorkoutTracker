using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace WorkoutTrackerWeb.Pages.Errors
{
    public class DatabaseErrorModel : PageModel
    {
        private readonly ILogger<DatabaseErrorModel> _logger;
        public string ErrorId { get; private set; }

        public DatabaseErrorModel(ILogger<DatabaseErrorModel> logger)
        {
            _logger = logger;
            ErrorId = Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        public void OnGet()
        {
            _logger.LogWarning("Database error page displayed for user. Error ID: {ErrorId}", ErrorId);
        }
    }
}