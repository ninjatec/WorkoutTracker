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
using WorkoutTrackerWeb.Models.Filters;
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
        public TemplateFilterModel Filter { get; set; } = new TemplateFilterModel();

        public List<WorkoutTemplate> Templates { get; set; } = new List<WorkoutTemplate>();
        public List<TemplateAssignment> AssignedTemplates { get; set; } = new List<TemplateAssignment>();

        public async Task<IActionResult> OnGetAsync()
        {
            var identityUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(identityUserId))
            {
                return Forbid();
            }

            // Get the application User entity corresponding to the identity user
            var appUser = await _context.User
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
            
            if (appUser == null)
            {
                // The user doesn't have a User record in the system yet
                return RedirectToPage("/Account/CompleteRegistration");
            }

            int userId = appUser.UserId;

            // Load categories for filter dropdown
            await Filter.LoadCategoriesAsync(_context, userId);

            // Load templates with filtering
            var query = _context.WorkoutTemplate.AsQueryable();

            // Apply standardized filters
            query = Filter.ApplyFilters(query, userId);

            // Get the filtered list of templates
            Templates = await query
                .OrderBy(t => t.Name)
                .ToListAsync();

            // Load templates assigned to the user by coaches
            AssignedTemplates = await _context.TemplateAssignments
                .Include(a => a.WorkoutTemplate)
                .Include(a => a.Coach)
                .Where(a => a.ClientUserId == userId && a.IsActive)
                .OrderBy(a => a.WorkoutTemplate.Name)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnGetJsonAsync()
        {
            var identityUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(identityUserId))
            {
                return Forbid();
            }

            // Get the application User entity corresponding to the identity user
            var appUser = await _context.User
                .FirstOrDefaultAsync(u => u.IdentityUserId == identityUserId);
            
            if (appUser == null)
            {
                // The user doesn't have a User record in the system yet
                return new JsonResult(new { error = "User profile not found" });
            }

            int userId = appUser.UserId;

            // Load templates with filtering
            var query = _context.WorkoutTemplate.AsQueryable();

            // Apply standardized filters
            query = Filter.ApplyFilters(query, userId);

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
                    isOwner = t.UserId == userId
                })
                .ToListAsync();

            // Apply filter to assigned templates as well
            var assignedTemplatesQuery = _context.TemplateAssignments
                .Include(a => a.WorkoutTemplate)
                .Include(a => a.Coach)
                .Where(a => a.ClientUserId == userId && a.IsActive)
                .AsQueryable();

            // Filter by category if provided
            if (!string.IsNullOrEmpty(Filter.Category))
            {
                assignedTemplatesQuery = assignedTemplatesQuery
                    .Where(a => a.WorkoutTemplate.Category == Filter.Category);
            }

            // Filter by search term if provided
            if (!string.IsNullOrEmpty(Filter.SearchTerm))
            {
                assignedTemplatesQuery = assignedTemplatesQuery
                    .Where(a => a.WorkoutTemplate.Name.Contains(Filter.SearchTerm) ||
                               (a.WorkoutTemplate.Description != null && 
                                a.WorkoutTemplate.Description.Contains(Filter.SearchTerm)));
            }

            // Get the filtered list of assigned templates
            var assignedTemplates = await assignedTemplatesQuery
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