using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Help
{
    [OutputCache(PolicyName = "StaticContent", NoStore = true)]
    public class SearchModel : PageModel
    {
        private readonly HelpService _helpService;

        public SearchModel(HelpService helpService)
        {
            _helpService = helpService;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        public List<HelpArticle> Articles { get; set; } = new List<HelpArticle>();
        public List<GlossaryTerm> GlossaryTerms { get; set; } = new List<GlossaryTerm>();

        public async Task<IActionResult> OnGetAsync()
        {
            if (!string.IsNullOrEmpty(SearchQuery))
            {
                Articles = await _helpService.SearchArticlesAsync(SearchQuery);
                GlossaryTerms = await _helpService.SearchGlossaryAsync(SearchQuery);
            }

            return Page();
        }
    }
}