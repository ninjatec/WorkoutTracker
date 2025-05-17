using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkoutTrackerWeb.Models.Blog;
using WorkoutTrackerWeb.Services.Blog;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Blog.Tags
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
        public BlogTag Tag { get; set; } = default!;
        
        public int AssociatedPostCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Tag = await _blogService.GetTagByIdAsync(id);

            if (Tag == null)
            {
                return NotFound();
            }

            // Count how many posts are using this tag
            var allPosts = await _blogService.GetAllBlogPostsAsync(true);
            AssociatedPostCount = allPosts.Count(p => p.BlogPostTags != null && 
                                                    p.BlogPostTags.Any(pt => pt.BlogTagId == id));

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var tag = await _blogService.GetTagByIdAsync(Tag.Id);

            if (tag == null)
            {
                return NotFound();
            }

            await _blogService.DeleteTagAsync(Tag.Id);

            TempData["SuccessMessage"] = "Tag deleted successfully.";
            return RedirectToPage("./Index");
        }
    }
}
