using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.DataPortability
{
    [Authorize]
    public class ExportModel : PageModel
    {
        private readonly WorkoutDataPortabilityService _portabilityService;
        private readonly UserService _userService;

        public ExportModel(
            WorkoutDataPortabilityService portabilityService,
            UserService userService)
        {
            _portabilityService = portabilityService;
            _userService = userService;
        }

        [BindProperty]
        public DateTime? StartDate { get; set; }

        [BindProperty]
        public DateTime? EndDate { get; set; }

        public string Message { get; set; }
        public bool Success { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var userId = await _userService.GetCurrentUserIdAsync();
                if (!userId.HasValue)
                {
                    throw new InvalidOperationException("User not found");
                }

                // Get the export data from the service
                var exportData = await _portabilityService.ExportUserDataAsync(
                    userId.Value, StartDate, EndDate);
                
                // Convert to JSON
                var jsonData = System.Text.Json.JsonSerializer.Serialize(exportData, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                var fileName = $"workout_data_{DateTime.UtcNow:yyyyMMddHHmm}.json";
                
                return File(System.Text.Encoding.UTF8.GetBytes(jsonData), 
                    "application/json", 
                    fileName);
            }
            catch (Exception ex)
            {
                Message = $"Export failed: {ex.Message}";
                Success = false;
                return Page();
            }
        }
    }
}