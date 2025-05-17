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
            // Debug incoming values
            System.Diagnostics.Debug.WriteLine($"OnPostAsync - Incoming Published: {BlogPost.Published}");
            
            // Check for model errors
            if (!ModelState.IsValid)
            {
                Categories = await _blogService.GetAllCategoriesAsync();
                Tags = await _blogService.GetAllTagsAsync();
                return Page();
            }

            // Get the existing blog post to ensure we're not losing data
            var existingPost = await _blogService.GetBlogPostByIdAsync(BlogPost.Id);
            if (existingPost == null)
            {
                return NotFound();
            }
            
            // Debug existing post
            System.Diagnostics.Debug.WriteLine($"OnPostAsync - Existing Post Published: {existingPost.Published}, New Published: {BlogPost.Published}");

            // Keep the original author ID and creation date
            BlogPost.AuthorId = existingPost.AuthorId;
            BlogPost.CreatedOn = existingPost.CreatedOn;

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
            else
            {
                // Keep the existing image if it wasn't changed
                BlogPost.ImageUrl = existingPost.ImageUrl;
            }

            // Update the modified timestamp
            BlogPost.UpdatedOn = DateTime.UtcNow;

            // Handle PublishedOn date logic
            if (BlogPost.Published && !existingPost.Published)
            {
                // If publishing for the first time, set the published date
                BlogPost.PublishedOn = DateTime.UtcNow;
            }
            else if (BlogPost.Published && existingPost.Published)
            {
                // Keep the existing published date if already published
                BlogPost.PublishedOn = existingPost.PublishedOn;
            }
            else if (!BlogPost.Published)
            {
                // If unpublishing, keep the published date for when it gets republished
                BlogPost.PublishedOn = existingPost.PublishedOn;
            }

            try
            {
                // Debug before service call
                System.Diagnostics.Debug.WriteLine($"OnPostAsync - Before Service Call Published: {BlogPost.Published}");

                var updatedPost = await _blogService.UpdateBlogPostAsync(BlogPost, SelectedCategories, SelectedTags);
                
                // Debug after service call
                System.Diagnostics.Debug.WriteLine($"OnPostAsync - After Service Call Published: {updatedPost?.Published}");
                
                if (updatedPost != null)
                {
                    TempData["SuccessMessage"] = "Blog post updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update blog post.";
                }
                
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error updating post: {ex.Message}");
                Categories = await _blogService.GetAllCategoriesAsync();
                Tags = await _blogService.GetAllTagsAsync();
                return Page();
            }
        }
    }
}
