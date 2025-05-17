using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Services.Blog;
using WorkoutTrackerWeb.ViewModels.Blog;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Blog.Categories
{
    public class IndexModel : PageModel
    {
        private readonly IBlogService _blogService;

        public IndexModel(IBlogService blogService)
        {
            _blogService = blogService;
        }

        public List<BlogCategoryViewModel> Categories { get; set; } = new List<BlogCategoryViewModel>();
        
        public async Task<IActionResult> OnGetAsync()
        {
            var categories = await _blogService.GetAllCategoriesAsync();
            Categories = categories.Select(c => new BlogCategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description,
                PostCount = c.BlogPostCategories.Count
            }).OrderBy(c => c.Name).ToList();
            
            return Page();
        }
    }
}
