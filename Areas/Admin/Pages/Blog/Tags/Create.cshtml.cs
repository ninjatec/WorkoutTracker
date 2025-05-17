using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WorkoutTrackerWeb.Models.Blog;
using WorkoutTrackerWeb.Services.Blog;

namespace WorkoutTrackerWeb.Areas.Admin.Pages.Blog.Tags
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
        public BlogTag Tag { get; set; } = new BlogTag();

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
            if (string.IsNullOrEmpty(Tag.Slug))
            {
                Tag.Slug = await _blogService.GenerateUniqueTagSlugAsync(Tag.Name);
            }
            else
            {
                // Ensure the slug is unique
                Tag.Slug = await _blogService.GenerateUniqueTagSlugAsync(Tag.Slug);
            }

            await _blogService.CreateTagAsync(Tag);

            TempData["SuccessMessage"] = "Tag created successfully.";
            return RedirectToPage("./Index");
        }
    }
}
