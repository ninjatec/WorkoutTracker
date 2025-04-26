using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OutputCaching;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Pages.Templates
{
    [Authorize]
    [OutputCache(Duration = 60, VaryByQueryKeys = new[] { "searchString", "category", "isPublic" })]
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;

        public IndexModel(WorkoutTrackerWebContext context)
        {
            _context = context;
        }

        public IList<WorkoutTemplate> Templates { get;set; } = default!;
        public List<string> Categories { get; set; } = new List<string>();
        
        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string Category { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public bool? IsPublic { get; set; }

        public async Task OnGetAsync()
        {
            IQueryable<WorkoutTemplate> templatesQuery = _context.WorkoutTemplate
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.ExerciseType)
                .OrderByDescending(t => t.LastModifiedDate);

            // Get all distinct categories for the filter dropdown
            Categories = await _context.WorkoutTemplate
                .Where(t => !string.IsNullOrEmpty(t.Category))
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Apply search filter
            if (!string.IsNullOrEmpty(SearchString))
            {
                templatesQuery = templatesQuery.Where(t => 
                    t.Name.Contains(SearchString) || 
                    t.Description.Contains(SearchString) || 
                    t.Tags.Contains(SearchString));
            }

            // Apply category filter
            if (!string.IsNullOrEmpty(Category))
            {
                templatesQuery = templatesQuery.Where(t => t.Category == Category);
            }

            // Apply visibility filter
            if (IsPublic.HasValue)
            {
                templatesQuery = templatesQuery.Where(t => t.IsPublic == IsPublic.Value);
            }

            Templates = await templatesQuery.ToListAsync();
        }
    }
}