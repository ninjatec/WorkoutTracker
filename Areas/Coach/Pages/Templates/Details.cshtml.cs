using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.ViewModels;
using WorkoutTrackerWeb.ViewModels.Coaching;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Templates
{
    [Area("Coach")]
    [CoachAuthorize]
    [OutputCache(PolicyName = "StaticContentWithId")]
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ILogger<DetailsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public WorkoutTemplate WorkoutTemplate { get; set; } = default!;
        
        public List<User> Clients { get; set; } = new List<User>();
        public List<TemplateAssignmentViewModel> RecentAssignments { get; set; } = new List<TemplateAssignmentViewModel>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // For JSON/API requests, prevent response caching
            if (Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                Response.Headers["Cache-Control"] = "no-store, max-age=0";
                Response.Headers["Pragma"] = "no-cache";
            }

            var workoutTemplate = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.ExerciseType)
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.TemplateSets)
                .ThenInclude(s => s.Settype)
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == id);

            if (workoutTemplate == null)
            {
                return NotFound();
            }

            // Check if the template belongs to this coach or is public
            if (!workoutTemplate.IsPublic)
            {
                // Get the coach user ID
                var coachId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(coachId))
                {
                    return Forbid();
                }

                // Get the coach user ID as an integer from the database
                var coachUser = await _context.User
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.IdentityUserId == coachId);
                
                if (coachUser == null || workoutTemplate.UserId != coachUser.UserId)
                {
                    return Forbid();
                }
                
                // Load clients for the coach if template belongs to this coach
                if (coachUser != null && workoutTemplate.UserId == coachUser.UserId)
                {
                    // Get clients of this coach for the assignment dropdown
                    var relationships = await _context.CoachClientRelationships
                        .Where(r => r.CoachId == coachId && r.Status == RelationshipStatus.Active)
                        .ToListAsync();
                    
                    // Get the client IDs from the relationships
                    var clientIdentityIds = relationships.Select(r => r.ClientId).ToList();
                    
                    // Fetch the User records for these clients
                    Clients = await _context.User
                        .Where(u => clientIdentityIds.Contains(u.IdentityUserId))
                        .ToListAsync();
                    
                    // If no clients found, use demo data
                    if (!Clients.Any())
                    {
                        _logger.LogWarning("No actual clients found. Providing demo data for coach {coachId}", coachId);
                        
                        // Get a list of all users that aren't the coach
                        var otherUsers = await _context.User
                            .Where(u => u.IdentityUserId != coachId && u.IdentityUserId != null)
                            .Take(5)
                            .ToListAsync();
                            
                        if (otherUsers.Any())
                        {
                            _logger.LogInformation("Adding {count} demo clients for testing", otherUsers.Count);
                            Clients = otherUsers;
                        }
                    }
                    
                    _logger.LogInformation("Found {count} clients for coach {coachId}", Clients.Count, coachId);
                    
                    // Get recent assignments of this template
                    var recentAssignments = await _context.TemplateAssignments
                        .Where(a => a.WorkoutTemplateId == id && a.CoachUserId == coachUser.UserId)
                        .Include(a => a.Client)
                        .OrderByDescending(a => a.AssignedDate)
                        .Take(5)
                        .ToListAsync();
                        
                    RecentAssignments = recentAssignments.Select(a => new TemplateAssignmentViewModel
                    {
                        Id = a.TemplateAssignmentId,
                        TemplateId = a.WorkoutTemplateId,
                        Name = workoutTemplate.Name,
                        ClientRelationshipId = (int)a.ClientRelationshipId, // Explicit cast from int? to int
                        Notes = $"Assigned on {a.AssignedDate.ToShortDateString()}"
                    }).ToList();
                }
            }

            WorkoutTemplate = workoutTemplate;
            return Page();
        }
    }
}