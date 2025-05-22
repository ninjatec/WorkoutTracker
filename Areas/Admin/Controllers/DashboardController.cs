using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace WorkoutTrackerWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ILogger<DashboardController> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Display the admin dashboard with system metrics
        /// </summary>
        public IActionResult Index()
        {
            _logger.LogInformation("Admin dashboard accessed");
            return View();
        }
        
        /// <summary>
        /// Display detailed statistics about system performance
        /// </summary>
        public IActionResult Statistics()
        {
            _logger.LogInformation("System statistics page accessed");
            return View();
        }
    }
}