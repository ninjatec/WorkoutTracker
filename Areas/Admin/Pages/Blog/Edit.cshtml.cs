using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<EditModel> _logger;

        public EditModel(IBlogService blogService, BlogImageUtility imageUtility, ILogger<EditModel> logger)
        {
            _blogService = blogService;
            _imageUtility = imageUtility;
            _logger = logger;
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
            _logger.LogInformation("Loading blog post {id} for editing", id);
            BlogPost = await _blogService.GetBlogPostByIdAsync(id);

            if (BlogPost == null)
            {
                _logger.LogWarning("Blog post {id} not found", id);
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

            _logger.LogInformation("Successfully loaded blog post {id} with {categoryCount} categories and {tagCount} tags", 
                id, SelectedCategories.Count, SelectedTags.Count);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Attempting to update blog post {id}", BlogPost.Id);
            _logger.LogInformation("Summary field value: {summary}", BlogPost.Summary);

            // Check for model errors
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed for blog post {id}. Errors: {errors}",
                    BlogPost.Id,
                    string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)));

                _logger.LogInformation("ModelState values for debugging:");
                foreach (var modelStateEntry in ModelState)
                {
                    _logger.LogInformation("Key: {key}, Value: {value}", 
                        modelStateEntry.Key, 
                        modelStateEntry.Value.RawValue);
                }

                Categories = await _blogService.GetAllCategoriesAsync();
                Tags = await _blogService.GetAllTagsAsync();
                return Page();
            }

            // Get the existing blog post to ensure we're not losing data
            var existingPost = await _blogService.GetBlogPostByIdAsync(BlogPost.Id);
            if (existingPost == null)
            {
                _logger.LogError("Blog post {id} not found when attempting to update", BlogPost.Id);
                return NotFound();
            }
            
            _logger.LogInformation("Blog post {id} found. Current published state: {isPublished}, New published state: {willBePublished}", 
                BlogPost.Id, 
                existingPost.Published, 
                BlogPost.Published);

            // Keep the original author ID and creation date
            BlogPost.AuthorId = existingPost.AuthorId;
            BlogPost.CreatedOn = existingPost.CreatedOn;

            // Handle image upload or removal
            if (FeaturedImage != null)
            {
                _logger.LogInformation("Uploading new image for blog post {id}", BlogPost.Id);
                // Upload new image
                var imagePath = await _imageUtility.UploadBlogImageAsync(FeaturedImage);
                if (!string.IsNullOrEmpty(imagePath))
                {
                    BlogPost.ImageUrl = imagePath;
                    _logger.LogInformation("Successfully uploaded new image for blog post {id}: {path}", BlogPost.Id, imagePath);
                }
                else
                {
                    _logger.LogError("Failed to upload image for blog post {id}", BlogPost.Id);
                    ModelState.AddModelError(string.Empty, "Failed to upload image.");
                    Categories = await _blogService.GetAllCategoriesAsync();
                    Tags = await _blogService.GetAllTagsAsync();
                    return Page();
                }
            }
            else if (RemoveImage)
            {
                _logger.LogInformation("Removing image from blog post {id}", BlogPost.Id);
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
                _logger.LogInformation("Publishing blog post {id} for the first time", BlogPost.Id);
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
                _logger.LogInformation("Unpublishing blog post {id}", BlogPost.Id);
            }

            try
            {
                _logger.LogInformation("Calling BlogService to update blog post {id}", BlogPost.Id);
                var updatedPost = await _blogService.UpdateBlogPostAsync(BlogPost, SelectedCategories, SelectedTags);
                
                if (updatedPost != null)
                {
                    _logger.LogInformation("Successfully updated blog post {id}", BlogPost.Id);
                    TempData["SuccessMessage"] = "Blog post updated successfully.";
                }
                else
                {
                    _logger.LogError("Failed to update blog post {id} - service returned null", BlogPost.Id);
                    TempData["ErrorMessage"] = "Failed to update blog post.";
                }
                
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating blog post {id}", BlogPost.Id);
                ModelState.AddModelError(string.Empty, $"Error updating post: {ex.Message}");
                Categories = await _blogService.GetAllCategoriesAsync();
                Tags = await _blogService.GetAllTagsAsync();
                return Page();
            }
        }
    }
}
