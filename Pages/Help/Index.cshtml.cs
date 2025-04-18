using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Help
{
    public class IndexModel : PageModel
    {
        private readonly HelpService _helpService;

        public IndexModel(HelpService helpService)
        {
            _helpService = helpService;
        }

        public List<HelpCategory> RootCategories { get; set; }
        public List<HelpArticle> FeaturedArticles { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            RootCategories = await _helpService.GetRootCategoriesAsync();
            FeaturedArticles = await _helpService.GetFeaturedArticlesAsync();
            return Page();
        }
    }
}