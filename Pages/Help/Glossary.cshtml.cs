using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Help
{
    [OutputCache(PolicyName = "GlossaryContent")]
    public class GlossaryModel : PageModel
    {
        private readonly HelpService _helpService;

        public GlossaryModel(HelpService helpService)
        {
            _helpService = helpService;
        }

        [BindProperty(SupportsGet = true)]
        public string SelectedCategory { get; set; }

        public List<GlossaryTerm> GlossaryTerms { get; set; } = new List<GlossaryTerm>();
        public List<string> Categories { get; set; } = new List<string>();
        public List<string> Alphabet { get; set; } = new List<string>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Get categories for filtering
            Categories = await _helpService.GetGlossaryCategoriesAsync();

            // Get terms based on selected category or all terms
            if (string.IsNullOrEmpty(SelectedCategory))
            {
                GlossaryTerms = await _helpService.GetGlossaryTermsAsync();
            }
            else
            {
                GlossaryTerms = await _helpService.GetGlossaryTermsByCategoryAsync(SelectedCategory);
            }

            // Generate alphabet for navigation based on available terms
            if (GlossaryTerms.Any())
            {
                Alphabet = GlossaryTerms
                    .Select(t => t.Term.Substring(0, 1).ToUpper())
                    .Distinct()
                    .OrderBy(l => l)
                    .ToList();
            }
            else
            {
                // Default alphabet if no terms
                Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(c => c.ToString()).ToList();
            }

            return Page();
        }
    }
}