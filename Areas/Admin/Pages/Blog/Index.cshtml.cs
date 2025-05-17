using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkoutTrackerWeb.Services.Blog;
using WorkoutTrackerWeb.ViewModels.Blog;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Blog
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IBlogService _blogService;

        public IndexModel(IBlogService blogService)
        {
            _blogService = blogService;
        }

        public List<BlogPostViewModel> Posts { get; set; } = new List<BlogPostViewModel>();
        public List<BlogCategoryViewModel> Categories { get; set; } = new List<BlogCategoryViewModel>();
        public List<BlogTagViewModel> Tags { get; set; } = new List<BlogTagViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPosts { get; set; }
        public int TotalPages => (TotalPosts + PageSize - 1) / PageSize;

        public async Task<IActionResult> OnGetAsync(int pageNumber = 1)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            CurrentPage = pageNumber;

            // Get blog posts
            var blogPosts = await _blogService.GetAllBlogPostsAsync(includeUnpublished: true);
            TotalPosts = blogPosts.Count;

            // Apply pagination
            Posts = blogPosts
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .Select(p => new BlogPostViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Slug = p.Slug,
                    Summary = p.Summary,
                    Published = p.Published,
                    PublishedOn = p.PublishedOn,
                    CreatedOn = p.CreatedOn,
                    ViewCount = p.ViewCount,
                    Categories = p.BlogPostCategories.Select(pc => new BlogCategoryViewModel
                    {
                        Id = pc.BlogCategory.Id,
                        Name = pc.BlogCategory.Name,
                        Slug = pc.BlogCategory.Slug
                    }).ToList(),
                    Tags = p.BlogPostTags.Select(pt => new BlogTagViewModel
                    {
                        Id = pt.BlogTag.Id,
                        Name = pt.BlogTag.Name,
                        Slug = pt.BlogTag.Slug
                    }).ToList()
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
                    Description = c.Description,
                    PostCount = c.BlogPostCategories.Count
                })
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
                    PostCount = t.BlogPostTags.Count
                })
                .OrderByDescending(t => t.PostCount)
                .ToList();

            return Page();
        }
    }
}
