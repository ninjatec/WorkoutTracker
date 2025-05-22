using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace WorkoutTrackerWeb.Pages
{
    public class MaintenanceModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public MaintenanceModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public string EstimatedCompletionTime { get; private set; }

        public void OnGet()
        {
            EstimatedCompletionTime = _configuration["MaintenanceMode:EstimatedCompletionTime"] ?? "Soon";
        }
    }
}