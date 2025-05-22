using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Services.VersionManagement;

namespace WorkoutTrackerWeb.ViewComponents
{
    public class VersionInfoViewComponent : ViewComponent
    {
        private readonly IVersionService _versionService;

        public VersionInfoViewComponent(IVersionService versionService)
        {
            _versionService = versionService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var version = await _versionService.GetCurrentVersionAsync();
            return View(version);
        }
    }
}