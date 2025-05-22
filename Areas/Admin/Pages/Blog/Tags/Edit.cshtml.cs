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
    public class EditModel : PageModel
    {
        private readonly IBlogService _blogService;

        public EditModel(IBlogService blogService)
        {
            _blogService = blogService;
        }

        [BindProperty]
        public BlogTag Tag { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Tag = await _blogService.GetTagByIdAsync(id);

            if (Tag == null)
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
                var existingTag = await _blogService.GetTagByIdAsync(Tag.Id);
                if (existingTag != null && existingTag.Slug != Tag.Slug)
                {
                    Tag.Slug = await _blogService.GenerateUniqueTagSlugAsync(Tag.Slug);
                }

                await _blogService.UpdateTagAsync(Tag);

                TempData["SuccessMessage"] = "Tag updated successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An error occurred while updating the tag: {ex.Message}");
                return Page();
            }
        }
    }
}
