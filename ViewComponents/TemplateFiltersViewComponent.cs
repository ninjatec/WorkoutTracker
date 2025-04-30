using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Filters;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.ViewComponents
{
    public class TemplateFiltersViewComponent : ViewComponent
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserService _userService;

        public TemplateFiltersViewComponent(WorkoutTrackerWebContext context, UserService userService)
        {
            _context = context;
            _userService = userService;
        }

        public async Task<IViewComponentResult> InvokeAsync(TemplateFilterModel filter)
        {
            // If categories haven't been loaded yet, load them
            if (filter.Categories.Count == 0)
            {
                var userId = await _userService.GetCurrentUserIdAsync();
                await filter.LoadCategoriesAsync(_context, userId);
            }
            
            return View(filter);
        }
    }
}