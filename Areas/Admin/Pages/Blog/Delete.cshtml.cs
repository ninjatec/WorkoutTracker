using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkoutTrackerWeb.Models.Blog;
using WorkoutTrackerWeb.Services.Blog;
using WorkoutTrackerWeb.Utilities;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Blog
{
    [Authorize(Roles = "Admin")]
    public class DeleteModel : PageModel
    {
        private readonly IBlogService _blogService;
        private readonly BlogImageUtility _imageUtility;

        public DeleteModel(IBlogService blogService, BlogImageUtility imageUtility)
        {
            _blogService = blogService;
            _imageUtility = imageUtility;
        }

        [BindProperty]
        public BlogPost BlogPost { get; set; } = default!;
        
        public List<BlogCategory> Categories { get; set; } = new List<BlogCategory>();
        public List<BlogTag> Tags { get; set; } = new List<BlogTag>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            BlogPost = await _blogService.GetBlogPostByIdAsync(id);

            if (BlogPost == null)
            {
                return NotFound();
            }

            // Load categories and tags for this post
            var fullPost = await _blogService.GetBlogPostByIdAsync(id);
            
            if (fullPost?.BlogPostCategories != null)
            {
                var categoryIds = fullPost.BlogPostCategories.Select(pc => pc.BlogCategoryId).ToList();
                Categories = (await _blogService.GetAllCategoriesAsync())
                    .Where(c => categoryIds.Contains(c.Id))
                    .ToList();
            }
            
            if (fullPost?.BlogPostTags != null)
            {
                var tagIds = fullPost.BlogPostTags.Select(pt => pt.BlogTagId).ToList();
                Tags = (await _blogService.GetAllTagsAsync())
                    .Where(t => tagIds.Contains(t.Id))
                    .ToList();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var post = await _blogService.GetBlogPostByIdAsync(BlogPost.Id);

            if (post == null)
            {
                return NotFound();
            }

            // Delete the featured image if it exists
            if (!string.IsNullOrEmpty(post.ImageUrl))
            {
                await _imageUtility.DeleteBlogImageAsync(post.ImageUrl);
            }

            await _blogService.DeleteBlogPostAsync(BlogPost.Id);

            TempData["SuccessMessage"] = "Blog post deleted successfully.";
            return RedirectToPage("./Index");
        }
    }
}
