using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;

namespace WorkoutTrackerWeb.Pages
{
    [Authorize]
    public class TemplatesModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ICoachingService _coachingService;

        public TemplatesModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ICoachingService coachingService)
        {
            _context = context;
            _userManager = userManager;
            _coachingService = coachingService;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Category { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IncludePublic { get; set; } = true;

        public List<WorkoutTemplate> Templates { get; set; } = new List<WorkoutTemplate>();
        public List<string> Categories { get; set; } = new List<string>();
        public List<TemplateAssignment> AssignedTemplates { get; set; } = new List<TemplateAssignment>();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            // Get distinct categories for filter dropdown
            Categories = await _context.WorkoutTemplate
                .Where(t => t.UserId == int.Parse(userId) || t.IsPublic)
                .Select(t => t.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            // Load templates with filtering
            var query = _context.WorkoutTemplate.AsQueryable();

            // Filter by owner and public status
            if (IncludePublic)
            {
                query = query.Where(t => t.UserId == int.Parse(userId) || t.IsPublic);
            }
            else
            {
                query = query.Where(t => t.UserId == int.Parse(userId));
            }

            // Apply search term filter if provided
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(t => 
                    t.Name.Contains(SearchTerm) || 
                    (t.Description != null && t.Description.Contains(SearchTerm)));
            }

            // Apply category filter if provided
            if (!string.IsNullOrEmpty(Category))
            {
                query = query.Where(t => t.Category == Category);
            }

            // Get the filtered list of templates
            Templates = await query
                .OrderBy(t => t.Name)
                .ToListAsync();

            // Load templates assigned to the user by coaches
            AssignedTemplates = await _context.TemplateAssignments
                .Include(a => a.WorkoutTemplate)
                .Include(a => a.Coach)
                .Where(a => a.ClientUserId == int.Parse(userId) && a.IsActive)
                .OrderBy(a => a.WorkoutTemplate.Name)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnGetJsonAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            // Load templates with filtering
            var query = _context.WorkoutTemplate.AsQueryable();

            // Filter by owner and public status
            if (IncludePublic)
            {
                query = query.Where(t => t.UserId == int.Parse(userId) || t.IsPublic);
            }
            else
            {
                query = query.Where(t => t.UserId == int.Parse(userId));
            }

            // Apply search term filter if provided
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                query = query.Where(t => 
                    t.Name.Contains(SearchTerm) || 
                    (t.Description != null && t.Description.Contains(SearchTerm)));
            }

            // Apply category filter if provided
            if (!string.IsNullOrEmpty(Category))
            {
                query = query.Where(t => t.Category == Category);
            }

            // Get the filtered list of templates
            var templates = await query
                .OrderBy(t => t.Name)
                .Select(t => new
                {
                    id = t.WorkoutTemplateId,
                    name = t.Name,
                    description = t.Description,
                    category = t.Category,
                    isPublic = t.IsPublic,
                    isOwner = t.UserId == int.Parse(userId)
                })
                .ToListAsync();

            // Load templates assigned to the user by coaches
            var assignedTemplates = await _context.TemplateAssignments
                .Include(a => a.WorkoutTemplate)
                .Include(a => a.Coach)
                .Where(a => a.ClientUserId == int.Parse(userId) && a.IsActive)
                .OrderBy(a => a.WorkoutTemplate.Name)
                .Select(a => new
                {
                    id = a.WorkoutTemplateId,
                    assignmentId = a.TemplateAssignmentId,
                    name = a.WorkoutTemplate.Name,
                    description = a.WorkoutTemplate.Description,
                    category = a.WorkoutTemplate.Category,
                    isAssigned = true,
                    coachName = a.Coach.Name,
                    assignmentDate = a.AssignedDate
                })
                .ToListAsync();

            // Combine both lists for the result
            var result = new
            {
                templates = templates,
                assignedTemplates = assignedTemplates
            };

            return new JsonResult(result);
        }
    }
}