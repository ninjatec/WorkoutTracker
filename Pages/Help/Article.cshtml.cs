using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Help
{
    [OutputCache(PolicyName = "HelpContent")]
    public class ArticleModel : PageModel
    {
        private readonly HelpService _helpService;

        public ArticleModel(HelpService helpService)
        {
            _helpService = helpService;
        }

        public HelpArticle Article { get; set; }

        public async Task<IActionResult> OnGetAsync(int id, string slug = null)
        {
            Article = await _helpService.GetArticleByIdAsync(id);

            if (Article == null)
            {
                return NotFound();
            }

            // Redirect to the correct URL if slug doesn't match
            if (string.IsNullOrEmpty(slug) || slug != Article.Slug)
            {
                return RedirectToPage("./Article", new { id, slug = Article.Slug });
            }

            return Page();
        }
    }
}