using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using WorkoutTrackerWeb.Services.Blog;
using WorkoutTrackerWeb.ViewModels.Blog;

namespace WorkoutTrackerWeb.Pages.Blog
{
    [OutputCache(Duration = 300, VaryByQueryKeys = new[] { "slug" })]
    public class PostModel : PageModel
    {
        private readonly IBlogService _blogService;

        public PostModel(IBlogService blogService)
        {
            _blogService = blogService;
        }

        public BlogPostViewModel Post { get; set; }
        public List<BlogPostViewModel> RecentPosts { get; set; } = new List<BlogPostViewModel>();
        public List<BlogCategoryViewModel> Categories { get; set; } = new List<BlogCategoryViewModel>();
        public List<BlogTagViewModel> Tags { get; set; } = new List<BlogTagViewModel>();

        public async Task<IActionResult> OnGetAsync(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            var blogPost = await _blogService.GetBlogPostBySlugAsync(slug);
            if (blogPost == null || !blogPost.Published)
            {
                return NotFound();
            }

            // Increment view count
            await _blogService.IncrementPostViewCountAsync(blogPost.Id);

            // Map to view model
            Post = new BlogPostViewModel
            {
                Id = blogPost.Id,
                Title = blogPost.Title,
                Slug = blogPost.Slug,
                Summary = blogPost.Summary,
                Content = blogPost.Content,
                ImageUrl = blogPost.ImageUrl,
                AuthorName = blogPost.Author?.UserName,
                Published = blogPost.Published,
                PublishedOn = blogPost.PublishedOn,
                ViewCount = blogPost.ViewCount + 1, // Add 1 to account for the increment we just did
                Categories = blogPost.BlogPostCategories.Select(pc => new BlogCategoryViewModel
                {
                    Id = pc.BlogCategory.Id,
                    Name = pc.BlogCategory.Name,
                    Slug = pc.BlogCategory.Slug
                }).ToList(),
                Tags = blogPost.BlogPostTags.Select(pt => new BlogTagViewModel
                {
                    Id = pt.BlogTag.Id,
                    Name = pt.BlogTag.Name,
                    Slug = pt.BlogTag.Slug
                }).ToList()
            };

            // Get recent posts
            var recentPosts = await _blogService.GetPublishedBlogPostsAsync(1, 5);
            RecentPosts = recentPosts
                .Where(p => p.Id != blogPost.Id) // Exclude current post
                .Take(4) // Limit to 4 recent posts
                .Select(p => new BlogPostViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    PublishedOn = p.PublishedOn
                })
                .ToList();

            // Get categories with post count
            var allCategories = await _blogService.GetAllCategoriesAsync();
            Categories = allCategories
                .Select(c => new BlogCategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    PostCount = c.BlogPostCategories.Count(pc => pc.BlogPost.Published)
                })
                .Where(c => c.PostCount > 0)
                .OrderByDescending(c => c.PostCount)
                .ToList();

            // Get tags with post count
            var allTags = await _blogService.GetAllTagsAsync();
            Tags = allTags
                .Select(t => new BlogTagViewModel
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug,
                    PostCount = t.BlogPostTags.Count(pt => pt.BlogPost.Published)
                })
                .Where(t => t.PostCount > 0)
                .OrderByDescending(t => t.PostCount)
                .Take(15) // Limit to top 15 tags
                .ToList();

            return Page();
        }
    }
}
