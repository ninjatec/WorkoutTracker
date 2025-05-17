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
    public class EditModel : PageModel
    {
        private readonly IBlogService _blogService;

        public EditModel(IBlogService blogService)
        {
            _blogService = blogService;
        }

        [BindProperty]
        public BlogCategory Category { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Category = await _blogService.GetCategoryByIdAsync(id);

            if (Category == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Ensure the slug is unique (but allow it to keep the same slug if unchanged)
                var existingCategory = await _blogService.GetCategoryByIdAsync(Category.Id);
                if (existingCategory != null && existingCategory.Slug != Category.Slug)
                {
                    Category.Slug = await _blogService.GenerateUniqueCategorySlugAsync(Category.Slug);
                }

                await _blogService.UpdateCategoryAsync(Category);

                TempData["SuccessMessage"] = "Category updated successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred while updating the category: {ex.Message}");
                return Page();
            }
        }
    }
}
