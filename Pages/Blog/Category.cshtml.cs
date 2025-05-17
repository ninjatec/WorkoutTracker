using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using WorkoutTrackerWeb.Models.Blog;
using WorkoutTrackerWeb.Services.Blog;
using WorkoutTrackerWeb.ViewModels.Blog;

namespace WorkoutTrackerWeb.Pages.Blog
{
    [OutputCache(Duration = 60)]
    public class CategoryModel : PageModel
    {
        private readonly IBlogService _blogService;

        public CategoryModel(IBlogService blogService)
        {
            _blogService = blogService;
        }

        public BlogCategory Category { get; set; }
        public List<BlogPostViewModel> BlogPosts { get; set; } = new List<BlogPostViewModel>();
        public List<BlogCategoryViewModel> AllCategories { get; set; } = new List<BlogCategoryViewModel>();
        public List<BlogTagViewModel> PopularTags { get; set; } = new List<BlogTagViewModel>();
        public List<BlogPostViewModel> RecentPosts { get; set; } = new List<BlogPostViewModel>();
        
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }

        public async Task<IActionResult> OnGetAsync(string slug, int pageIndex = 1)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            Category = await _blogService.GetCategoryBySlugAsync(slug);
            
            if (Category == null)
            {
                return NotFound();
            }

            // Set page index and size
            PageIndex = pageIndex < 1 ? 1 : pageIndex;
            int pageSize = 10;

            // Get posts for this category
            var posts = await _blogService.GetBlogPostsByCategoryAsync(slug, PageIndex, pageSize);
            
            // Get total post count for pagination
            var allPosts = await _blogService.GetAllBlogPostsAsync();
            var categoryPosts = allPosts.Where(p => 
                p.BlogPostCategories != null && 
                p.BlogPostCategories.Any(pc => 
                    pc.BlogCategoryId == Category.Id)).ToList();
            
            TotalPages = (int)Math.Ceiling(categoryPosts.Count / (double)pageSize);

            // Convert to view models
            BlogPosts = await GetBlogPostViewModelsAsync(posts);
            
            // Get sidebar data
            await LoadSidebarDataAsync();

            return Page();
        }

        private async Task LoadSidebarDataAsync()
        {
            // Get all categories with post counts
            var categories = await _blogService.GetAllCategoriesAsync();
            var allPosts = await _blogService.GetAllBlogPostsAsync();
            
            AllCategories = categories.Select(c => new BlogCategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                PostCount = allPosts.Count(p => 
                    p.BlogPostCategories != null && 
                    p.BlogPostCategories.Any(pc => 
                        pc.BlogCategoryId == c.Id))
            }).OrderByDescending(c => c.PostCount).ToList();
            
            // Get popular tags
            var tags = await _blogService.GetAllTagsAsync();
            
            PopularTags = tags.Select(t => new BlogTagViewModel
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                PostCount = allPosts.Count(p => 
                    p.BlogPostTags != null && 
                    p.BlogPostTags.Any(pt => 
                        pt.BlogTagId == t.Id))
            })
            .OrderByDescending(t => t.PostCount)
            .Take(10)
            .ToList();
            
            // Get recent posts
            var recentPosts = await _blogService.GetPublishedBlogPostsAsync(1, 5);
            RecentPosts = await GetBlogPostViewModelsAsync(recentPosts);
        }

        private async Task<List<BlogPostViewModel>> GetBlogPostViewModelsAsync(List<BlogPost> posts)
        {
            var categories = await _blogService.GetAllCategoriesAsync();
            var tags = await _blogService.GetAllTagsAsync();
            
            return posts.Select(p => new BlogPostViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary,
                Content = p.Content,
                ImageUrl = p.ImageUrl,
                Published = p.Published,
                PublishedOn = p.PublishedOn,
                CreatedOn = p.CreatedOn,
                UpdatedOn = p.UpdatedOn,
                ViewCount = p.ViewCount,
                Categories = categories
                    .Where(c => p.BlogPostCategories != null && 
                               p.BlogPostCategories.Any(pc => 
                                   pc.BlogCategoryId == c.Id))
                    .Select(c => new BlogCategoryViewModel
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Slug = c.Slug
                    }).ToList(),
                Tags = tags
                    .Where(t => p.BlogPostTags != null && 
                               p.BlogPostTags.Any(pt => 
                                   pt.BlogTagId == t.Id))
                    .Select(t => new BlogTagViewModel
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Slug = t.Slug
                    }).ToList()
            }).ToList();
        }
    }
}
