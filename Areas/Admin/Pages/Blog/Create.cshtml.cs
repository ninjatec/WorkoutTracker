using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkoutTrackerWeb.Models.Blog;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Blog;
using WorkoutTrackerWeb.Utilities;
using WorkoutTrackerWeb.ViewModels.Blog;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Blog
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly IBlogService _blogService;
        private readonly UserManager<AppUser> _userManager;
        private readonly BlogImageUtility _imageUtility;

        public CreateModel(
            IBlogService blogService,
            UserManager<AppUser> userManager,
            BlogImageUtility imageUtility)
        {
            _blogService = blogService;
            _userManager = userManager;
            _imageUtility = imageUtility;
        }

        [BindProperty]
        public BlogPostViewModel BlogPost { get; set; } = new BlogPostViewModel();

        [BindProperty]
        public List<int> SelectedCategoryIds { get; set; } = new List<int>();

        [BindProperty]
        public List<int> SelectedTagIds { get; set; } = new List<int>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Load available categories and tags
            await LoadAvailableCategoriesAndTags();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadAvailableCategoriesAndTags();
                return Page();
            }

            try
            {
                // Get current user as author
                var currentUser = await _userManager.GetUserAsync(User);
                
                // Handle image upload
                string imageUrl = null;
                if (BlogPost.ImageFile != null)
                {
                    imageUrl = await _imageUtility.SaveImageAsync(BlogPost.ImageFile);
                }

                // Create blog post entity
                var blogPost = new Models.Blog.BlogPost
                {
                    Title = BlogPost.Title,
                    Slug = BlogPost.Slug, // Will be generated in service if empty
                    Content = BlogPost.Content,
                    Summary = BlogPost.Summary,
                    ImageUrl = imageUrl,
                    AuthorId = currentUser.Id,
                    Published = BlogPost.Published,
                    CreatedOn = DateTime.UtcNow,
                    PublishedOn = BlogPost.Published ? DateTime.UtcNow : null
                };

                // Save to database
                var savedPost = await _blogService.CreateBlogPostAsync(blogPost, SelectedCategoryIds, SelectedTagIds);

                TempData["SuccessMessage"] = "Blog post created successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
                await LoadAvailableCategoriesAndTags();
                return Page();
            }
        }

        private async Task LoadAvailableCategoriesAndTags()
        {
            // Load available categories
            var categories = await _blogService.GetAllCategoriesAsync();
            BlogPost.AvailableCategories = categories.Select(c => new BlogCategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug
            }).ToList();

            // Load available tags
            var tags = await _blogService.GetAllTagsAsync();
            BlogPost.AvailableTags = tags.Select(t => new BlogTagViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug
            }).ToList();
        }
    }
}
