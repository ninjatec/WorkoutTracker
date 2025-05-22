using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkoutTrackerWeb.Models.Blog;
using WorkoutTrackerWeb.Services.Blog;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Blog.Categories
{
    [Authorize(Roles = "Admin")]
    public class CreateModel : PageModel
    {
        private readonly IBlogService _blogService;

        public CreateModel(IBlogService blogService)
        {
            _blogService = blogService;
        }

        [BindProperty]
        public BlogCategory Category { get; set; } = new BlogCategory();

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Generate a unique slug if not provided
            if (string.IsNullOrEmpty(Category.Slug))
            {
                Category.Slug = await _blogService.GenerateUniqueCategorySlugAsync(Category.Name);
            }
            else
            {
                // Ensure the slug is unique
                Category.Slug = await _blogService.GenerateUniqueCategorySlugAsync(Category.Slug);
            }

            await _blogService.CreateCategoryAsync(Category);

            TempData["SuccessMessage"] = "Category created successfully.";
            return RedirectToPage("./Index");
        }
    }
}
