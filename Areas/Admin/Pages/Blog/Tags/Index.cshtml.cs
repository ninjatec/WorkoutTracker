using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkoutTrackerWeb.Models.Blog;
using WorkoutTrackerWeb.Services.Blog;
using WorkoutTrackerWeb.ViewModels.Blog;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Blog.Tags
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IBlogService _blogService;

        public IndexModel(IBlogService blogService)
        {
            _blogService = blogService;
        }

        public List<TagViewModel> Tags { get; set; } = new List<TagViewModel>();
        
        public string CurrentSort { get; set; }
        public string NameSort { get; set; }
        public string CurrentFilter { get; set; }
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }
        
        public async Task OnGetAsync(string sortOrder, string searchString, int? pageIndex)
        {
            CurrentSort = sortOrder;
            NameSort = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            
            if (searchString != null)
            {
                pageIndex = 1;
            }
            else
            {
                searchString = CurrentFilter;
            }
            
            CurrentFilter = searchString;
            
            // Get all tags
            var tags = await _blogService.GetAllTagsAsync();
            
            // Get all posts to count usage
            var allPosts = await _blogService.GetAllBlogPostsAsync(true);
            
            // Filter by search string if provided
            if (!string.IsNullOrEmpty(searchString))
            {
                tags = tags.Where(t => t.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            
            // Sort the tags
            tags = sortOrder switch
            {
                "name_desc" => tags.OrderByDescending(t => t.Name).ToList(),
                _ => tags.OrderBy(t => t.Name).ToList()
            };
            
            // Create view models with post counts
            var tagViewModels = tags.Select(tag => new TagViewModel
            {
                Id = tag.Id,
                Name = tag.Name,
                Slug = tag.Slug,
                PostCount = allPosts.Count(p => p.BlogPostTags != null && p.BlogPostTags.Any(pt => pt.BlogTagId == tag.Id))
            }).ToList();
            
            // Pagination
            int pageSize = 10;
            PageIndex = pageIndex ?? 1;
            TotalPages = (int)Math.Ceiling(tagViewModels.Count / (double)pageSize);
            
            Tags = tagViewModels
                .Skip((PageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }
    }
    
    public class TagViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public int PostCount { get; set; }
    }
}
