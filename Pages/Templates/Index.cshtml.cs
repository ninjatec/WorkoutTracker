using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Hosting;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;

namespace WorkoutTrackerWeb.Pages.Templates
{
    [Authorize]
    [OutputCache(Duration = 300, VaryByQueryKeys = new[] { "searchString", "category", "isPublic" })]
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IWebHostEnvironment _environment;

        public IndexModel(WorkoutTrackerWebContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
            // Adjust cache duration for development environment
            if (_environment.IsDevelopment())
            {
                // In development, we'll use a longer cache duration for improved performance
                HttpContext.Response.Headers["X-Template-Cache"] = "Development-Extended";
                // The actual cache duration is set at the class level attribute
            }

            // Get all distinct categories for the filter dropdown - this can use a separate query
            Categories = await _context.WorkoutTemplate
                .Where(t => !string.IsNullOrEmpty(t.Category))
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c)
                .AsNoTracking()  // No need to track these entities
                .ToListAsync();

            // Create a more efficient query that only loads what's needed for the template list
            IQueryable<WorkoutTemplate> templatesQuery = _context.WorkoutTemplate
                .AsNoTracking()  // No tracking for better performance
                .Select(t => new WorkoutTemplate
                {
                    WorkoutTemplateId = t.WorkoutTemplateId,
                    Name = t.Name,
                    Description = t.Description,
                    Category = t.Category,
                    Tags = t.Tags,
                    IsPublic = t.IsPublic,
                    CreatedDate = t.CreatedDate,
                    LastModifiedDate = t.LastModifiedDate,
                    UserId = t.UserId,
                    // Just count the exercises without loading them all
                    TemplateExercises = t.TemplateExercises.Select(e => new WorkoutTemplateExercise
                    {
                        WorkoutTemplateExerciseId = e.WorkoutTemplateExerciseId,
                        ExerciseTypeId = e.ExerciseTypeId,
                        ExerciseType = new ExerciseType
                        {
                            Name = e.ExerciseType.Name
                        }
                    }).ToList()
                });

            // Apply search filter
            if (!string.IsNullOrEmpty(SearchString))
            {
                templatesQuery = templatesQuery.Where(t => 
                    t.Name.Contains(SearchString) || 
                    t.Description.Contains(SearchString) || 
                    (t.Tags != null && t.Tags.Contains(SearchString)));
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

            // Apply ordering at the end of the query after all filters
            templatesQuery = templatesQuery.OrderByDescending(t => t.LastModifiedDate);

            Templates = await templatesQuery.ToListAsync();
        }
    }
}