using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Pages.Templates
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;

        public CreateModel(WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public List<string> Categories { get; set; } = new List<string>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Get existing categories for autocomplete
            Categories = await _context.WorkoutTemplate
                .Where(t => !string.IsNullOrEmpty(t.Category))
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return Page();
        }

        [BindProperty]
        public WorkoutTemplate WorkoutTemplate { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Repopulate categories list on validation error
                Categories = await _context.WorkoutTemplate
                    .Where(t => !string.IsNullOrEmpty(t.Category))
                    .Select(t => t.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
                    
                return Page();
            }

            // Get the current user for the template
            var user = await _context.GetCurrentUserAsync();
            if (user == null)
            {
                return RedirectToPage("/Account/Login");
            }

            // Set the user ID and timestamps
            WorkoutTemplate.UserId = user.UserId;
            WorkoutTemplate.CreatedDate = DateTime.Now;
            WorkoutTemplate.LastModifiedDate = DateTime.Now;

            // Sanitize tags input
            if (!string.IsNullOrEmpty(WorkoutTemplate.Tags))
            {
                // Remove any extra spaces around commas and trim the string
                WorkoutTemplate.Tags = string.Join(", ", 
                    WorkoutTemplate.Tags
                        .Split(',')
                        .Select(tag => tag.Trim())
                        .Where(tag => !string.IsNullOrEmpty(tag)));
            }

            _context.WorkoutTemplate.Add(WorkoutTemplate);
            await _context.SaveChangesAsync();

            // Redirect to edit page to add exercises
            return RedirectToPage("./Edit", new { id = WorkoutTemplate.WorkoutTemplateId });
        }
    }
}