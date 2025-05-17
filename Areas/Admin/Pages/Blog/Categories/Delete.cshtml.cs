using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkoutTrackerWeb.Models.Blog;
using WorkoutTrackerWeb.Services.Blog;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Blog.Categories
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly IBlogService _blogService;

        public DeleteModel(IBlogService blogService)
        {
            _blogService = blogService;
        }

        [BindProperty]
        public BlogCategory Category { get; set; } = default!;
        
        public int AssociatedPostCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Category = await _blogService.GetCategoryByIdAsync(id);

            if (Category == null)
            {
                return NotFound();
            }

            // Count how many posts are using this category
            var allPosts = await _blogService.GetAllBlogPostsAsync(true);
            AssociatedPostCount = allPosts.Count(p => p.BlogPostCategories != null && 
                                                     p.BlogPostCategories.Any(pc => pc.BlogCategoryId == id));

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var category = await _blogService.GetCategoryByIdAsync(Category.Id);

            if (category == null)
            {
                return NotFound();
            }

            await _blogService.DeleteCategoryAsync(Category.Id);

            TempData["SuccessMessage"] = "Category deleted successfully.";
            return RedirectToPage("./Index");
        }
    }
}
