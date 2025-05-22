using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Filters;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Pages.WorkoutTemplates
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<IndexModel> _logger;
        private readonly UserService _userService;

        public IndexModel(
            WorkoutTrackerWebContext context,
            ILogger<IndexModel> logger,
            UserService userService)
        {
            _context = context;
            _logger = logger;
            _userService = userService;
        }

        [BindProperty(SupportsGet = true)]
        public TemplateFilterModel Filter { get; set; } = new TemplateFilterModel();
        
        public List<TemplateViewModel> Templates { get; set; } = new List<TemplateViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized();
            }

            // Load categories for filter dropdown
            await Filter.LoadCategoriesAsync(_context, userId);

            // Get templates owned by the user or public templates
            var query = _context.WorkoutTemplate.AsQueryable();

            // Apply standardized filters
            query = Filter.ApplyFilters(query, userId);

            // Get available templates
            var availableTemplates = await query
                .Select(t => new TemplateViewModel
                {
                    WorkoutTemplateId = t.WorkoutTemplateId,
                    Name = t.Name,
                    Description = t.Description,
                    Category = t.Category,
                    Tags = t.Tags,
                    CreatedDate = t.CreatedDate,
                    LastModifiedDate = t.LastModifiedDate,
                    IsPublic = t.IsPublic,
                    IsOwner = t.UserId == userId,
                    ExerciseCount = _context.WorkoutTemplateExercise
                        .Count(e => e.WorkoutTemplateId == t.WorkoutTemplateId)
                })
                .OrderBy(t => t.Name)
                .ToListAsync();

            // Get templates assigned to the user by coaches
            var assignedTemplates = await _context.TemplateAssignments
                .Where(a => a.ClientUserId == userId && a.IsActive)
                .Include(a => a.WorkoutTemplate)
                .Where(a => a.WorkoutTemplate != null)
                .Select(a => new TemplateViewModel
                {
                    WorkoutTemplateId = a.WorkoutTemplateId,
                    Name = a.WorkoutTemplate.Name,
                    Description = a.WorkoutTemplate.Description,
                    Category = a.WorkoutTemplate.Category,
                    Tags = a.WorkoutTemplate.Tags,
                    CreatedDate = a.WorkoutTemplate.CreatedDate,
                    LastModifiedDate = a.WorkoutTemplate.LastModifiedDate,
                    IsPublic = false,
                    IsOwner = false,
                    IsAssigned = true,
                    AssignedBy = "Coach",
                    ExerciseCount = _context.WorkoutTemplateExercise
                        .Count(e => e.WorkoutTemplateId == a.WorkoutTemplateId)
                })
                .ToListAsync();

            // Apply filters to assigned templates
            if (!string.IsNullOrEmpty(Filter.Category))
            {
                assignedTemplates = assignedTemplates
                    .Where(t => t.Category == Filter.Category)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(Filter.SearchTerm))
            {
                assignedTemplates = assignedTemplates
                    .Where(t => t.Name.Contains(Filter.SearchTerm, StringComparison.OrdinalIgnoreCase) || 
                             t.Description.Contains(Filter.SearchTerm, StringComparison.OrdinalIgnoreCase) || 
                             (t.Tags != null && t.Tags.Contains(Filter.SearchTerm, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // Combine both lists, avoiding duplicates
            var existingIds = availableTemplates.Select(t => t.WorkoutTemplateId).ToHashSet();
            foreach (var template in assignedTemplates)
            {
                if (!existingIds.Contains(template.WorkoutTemplateId))
                {
                    availableTemplates.Add(template);
                }
            }

            Templates = availableTemplates.OrderBy(t => t.Name).ToList();
            return Page();
        }

        public async Task<JsonResult> OnGetTemplatesJsonAsync()
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return new JsonResult(new { error = "Unauthorized" }) { StatusCode = 401 };
            }

            // Reuse the same logic as the GET handler
            await OnGetAsync();
            
            return new JsonResult(Templates);
        }
    }

    public class TemplateViewModel
    {
        public int WorkoutTemplateId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Tags { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public bool IsPublic { get; set; }
        public bool IsOwner { get; set; }
        public bool IsAssigned { get; set; }
        public string AssignedBy { get; set; }
        public int ExerciseCount { get; set; }
    }
}