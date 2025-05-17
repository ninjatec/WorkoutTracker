using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using WorkoutTrackerWeb.Models.Blog;
using WorkoutTrackerWeb.Services.Blog;
using WorkoutTrackerWeb.Utilities;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Blog
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly IBlogService _blogService;
        private readonly BlogImageUtility _imageUtility;

        public EditModel(IBlogService blogService, BlogImageUtility imageUtility)
        {
            _blogService = blogService;
            _imageUtility = imageUtility;
        }

        [BindProperty]
        public BlogPost BlogPost { get; set; } = default!;

        [BindProperty]
        public IFormFile FeaturedImage { get; set; }

        [BindProperty]
        public bool RemoveImage { get; set; }

        [BindProperty]
        public List<int> SelectedCategories { get; set; } = new List<int>();

        [BindProperty]
        public List<int> SelectedTags { get; set; } = new List<int>();

        public List<BlogCategory> Categories { get; set; } = new List<BlogCategory>();
        public List<BlogTag> Tags { get; set; } = new List<BlogTag>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            BlogPost = await _blogService.GetBlogPostByIdAsync(id);

            if (BlogPost == null)
            {
                return NotFound();
            }

            // Load categories and tags
            Categories = await _blogService.GetAllCategoriesAsync();
            Tags = await _blogService.GetAllTagsAsync();

            // Get selected categories
            if (BlogPost.BlogPostCategories != null)
            {
                SelectedCategories = BlogPost.BlogPostCategories
                    .Select(pc => pc.BlogCategoryId)
                    .ToList();
            }

            // Get selected tags
            if (BlogPost.BlogPostTags != null)
            {
                SelectedTags = BlogPost.BlogPostTags
                    .Select(pt => pt.BlogTagId)
                    .ToList();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Categories = await _blogService.GetAllCategoriesAsync();
                Tags = await _blogService.GetAllTagsAsync();
                return Page();
            }

            // Handle image upload or removal
            if (FeaturedImage != null)
            {
                // Upload new image
                var imagePath = await _imageUtility.UploadBlogImageAsync(FeaturedImage);
                if (!string.IsNullOrEmpty(imagePath))
                {
                    BlogPost.ImageUrl = imagePath;
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Failed to upload image.");
                    Categories = await _blogService.GetAllCategoriesAsync();
                    Tags = await _blogService.GetAllTagsAsync();
                    return Page();
                }
            }
            else if (RemoveImage)
            {
                // Remove the current image
                if (!string.IsNullOrEmpty(BlogPost.ImageUrl))
                {
                    await _imageUtility.DeleteBlogImageAsync(BlogPost.ImageUrl);
                    BlogPost.ImageUrl = null;
                }
            }

            // Update the modified timestamp
            BlogPost.UpdatedOn = DateTime.UtcNow;

            // If publishing for the first time, set the published date
            if (BlogPost.Published && !BlogPost.PublishedOn.HasValue)
            {
                BlogPost.PublishedOn = DateTime.UtcNow;
            }

            await _blogService.UpdateBlogPostAsync(BlogPost, SelectedCategories, SelectedTags);

            TempData["SuccessMessage"] = "Blog post updated successfully.";
            return RedirectToPage("./Index");
        }
    }
}
