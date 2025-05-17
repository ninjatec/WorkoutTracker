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
    [OutputCache(Duration = 600, VaryByQueryKeys = new[] { "pageNumber", "category", "tag", "q" })]
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
        public int PageSize { get; set; } = 5; // Fewer posts per page for the public blog
        public int TotalPosts { get; set; }
        public int TotalPages => (TotalPosts + PageSize - 1) / PageSize;
        
        public string CategorySlug { get; set; }
        public string CategoryName { get; set; }
        public string TagSlug { get; set; }
        public string TagName { get; set; }
        public string SearchTerm { get; set; }

        public async Task<IActionResult> OnGetAsync(int pageNumber = 1, string category = null, string tag = null, string q = null)
        {
            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            CurrentPage = pageNumber;
            CategorySlug = category;
            TagSlug = tag;
            SearchTerm = q;

            List<Models.Blog.BlogPost> blogPosts;
            
            // Get blog posts based on filters
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                blogPosts = await _blogService.SearchBlogPostsAsync(SearchTerm, 1, 1000); // Get all for count
                TotalPosts = blogPosts.Count;
                blogPosts = await _blogService.SearchBlogPostsAsync(SearchTerm, CurrentPage, PageSize);
            }
            else if (!string.IsNullOrEmpty(CategorySlug))
            {
                var categoryEntity = await _blogService.GetCategoryBySlugAsync(CategorySlug);
                if (categoryEntity != null)
                {
                    CategoryName = categoryEntity.Name;
                    blogPosts = await _blogService.GetBlogPostsByCategoryAsync(CategorySlug, 1, 1000); // Get all for count
                    TotalPosts = blogPosts.Count;
                    blogPosts = await _blogService.GetBlogPostsByCategoryAsync(CategorySlug, CurrentPage, PageSize);
                }
                else
                {
                    return NotFound();
                }
            }
            else if (!string.IsNullOrEmpty(TagSlug))
            {
                var tagEntity = await _blogService.GetTagBySlugAsync(TagSlug);
                if (tagEntity != null)
                {
                    TagName = tagEntity.Name;
                    blogPosts = await _blogService.GetBlogPostsByTagAsync(TagSlug, 1, 1000); // Get all for count
                    TotalPosts = blogPosts.Count;
                    blogPosts = await _blogService.GetBlogPostsByTagAsync(TagSlug, CurrentPage, PageSize);
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                TotalPosts = await _blogService.GetTotalPublishedBlogPostCountAsync();
                blogPosts = await _blogService.GetPublishedBlogPostsAsync(CurrentPage, PageSize);
            }

            // Map to view models
            Posts = blogPosts.Select(p => new BlogPostViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary,
                Content = p.Content,
                ImageUrl = p.ImageUrl,
                AuthorName = p.Author?.UserName,
                Published = p.Published,
                PublishedOn = p.PublishedOn,
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
            }).ToList();

            // Get categories with post count
            var allCategories = await _blogService.GetAllCategoriesAsync();
            Categories = allCategories
                .Select(c => new BlogCategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Description = c.Description,
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
                .ToList();

            return Page();
        }
    }
}
