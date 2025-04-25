using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Help
{
    [OutputCache(PolicyName = "HelpContent")]
    public class CategoryModel : PageModel
    {
        private readonly HelpService _helpService;

        public CategoryModel(HelpService helpService)
        {
            _helpService = helpService;
        }

        public HelpCategory Category { get; set; }
        public List<HelpArticle> Articles { get; set; }

        public async Task<IActionResult> OnGetAsync(int id, string slug = null)
        {
            // Load the category with its child categories
            var categories = await _helpService.GetCategoriesAsync();
            Category = categories.Find(c => c.Id == id);

            if (Category == null)
            {
                return NotFound();
            }

            // Redirect to the correct URL if slug doesn't match
            if (string.IsNullOrEmpty(slug) || slug != Category.Slug)
            {
                return RedirectToPage("./Category", new { id, slug = Category.Slug });
            }

            // Get all articles for this category
            Articles = await _helpService.GetArticlesByCategoryAsync(id);

            return Page();
        }
    }
}