using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Blog;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Blog;
using WorkoutTrackerWeb.ViewModels.Blog;

namespace WorkoutTrackerWeb.Pages;

// Add conditional output caching - don't cache for unauthenticated users
[OutputCache(PolicyName = "HomePagePolicy", NoStore = false)]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly UserManager<AppUser> _userManager;
    private readonly IBlogService _blogService;

    public IndexModel(
        ILogger<IndexModel> logger,
        UserManager<AppUser> userManager,
        IBlogService blogService)
    {
        _logger = logger;
        _userManager = userManager;
        _blogService = blogService;
    }

    public List<BlogPostViewModel> RecentBlogPosts { get; set; } = new List<BlogPostViewModel>();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!User.Identity.IsAuthenticated)
        {
            // Get 3 most recent blog posts for non-authenticated users
            var recentPosts = await _blogService.GetPublishedBlogPostsAsync(1, 3);
            RecentBlogPosts = recentPosts.Select(p => new BlogPostViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary,
                ImageUrl = p.ImageUrl,
                PublishedOn = p.PublishedOn,
                ViewCount = p.ViewCount,
                Categories = p.BlogPostCategories?.Select(pc => new BlogCategoryViewModel
                {
                    Id = pc.BlogCategory.Id,
                    Name = pc.BlogCategory.Name,
                    Slug = pc.BlogCategory.Slug
                }).ToList() ?? new List<BlogCategoryViewModel>()
            }).ToList();
        }

        return Page();
    }
}
