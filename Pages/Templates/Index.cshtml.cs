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
using Microsoft.AspNetCore.Identity;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Filters;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.Templates
{
    [Authorize]
    [OutputCache(Duration = 300, VaryByQueryKeys = new[] { "Filter.SearchTerm", "Filter.Category", "Filter.IncludePublic" })]
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<AppUser> _userManager;
        private readonly UserService _userService;

        public IndexModel(
            WorkoutTrackerWebContext context, 
            IWebHostEnvironment environment,
            UserManager<AppUser> userManager,
            UserService userService)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
            _userService = userService;
        }

        public IList<WorkoutTemplate> Templates { get; set; } = default!;
        
        [BindProperty(SupportsGet = true)]
        public TemplateFilterModel Filter { get; set; } = new TemplateFilterModel();

        public async Task OnGetAsync()
        {
            // Adjust cache duration for development environment
            if (_environment.IsDevelopment())
            {
                // In development, we'll use a longer cache duration for improved performance
                HttpContext.Response.Headers["X-Template-Cache"] = "Development-Extended";
                // The actual cache duration is set at the class level attribute
            }

            // Get current user ID for template filtering
            var userId = await _userService.GetCurrentUserIdAsync();

            // Load categories for filter dropdown
            await Filter.LoadCategoriesAsync(_context, userId);

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

            // Apply the standardized template filters
            templatesQuery = Filter.ApplyFilters(templatesQuery, userId);

            // Apply ordering at the end of the query after all filters
            templatesQuery = templatesQuery.OrderByDescending(t => t.LastModifiedDate);

            Templates = await templatesQuery.ToListAsync();
        }
    }
}