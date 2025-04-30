using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;
using WorkoutTrackerWeb.Services.Validation;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Goals
{
    [Area("Coach")]
    [CoachAuthorize]
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly GoalQueryService _goalQueryService;
        private readonly ILogger<IndexModel> _logger;
        private readonly CoachingValidationService _validationService;

        public IndexModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            GoalQueryService goalQueryService,
            ILogger<IndexModel> logger,
            CoachingValidationService validationService)
        {
            _context = context;
            _userManager = userManager;
            _goalQueryService = goalQueryService;
            _logger = logger;
            _validationService = validationService;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string StatusMessageType { get; set; }

        public List<GoalViewModel> Goals { get; set; } = new List<GoalViewModel>();
        public SelectList ClientSelectList { get; set; }
        public SelectList CategorySelectList { get; set; }
        public SelectList StatusSelectList { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string ClientId { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string CategoryFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            try
            {
                // Get all coach-client relationships
                var relationships = await _context.CoachClientRelationships
                    .Where(r => r.CoachId == coachId && r.Status == RelationshipStatus.Active)
                    .Include(r => r.Client)
                    .ToListAsync();

                // Populate client select list
                await PopulateClientSelectList(relationships);

                // Populate category select list
                PopulateCategorySelectList();

                // Populate status select list
                PopulateStatusSelectList();

                // Get goals based on filters
                await PopulateGoalsList(coachId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading goals for coach {CoachId}", coachId);
                _validationService.SetError(this, "An error occurred while loading goals. Please try again.");
            }

            return Page();
        }

        private async Task PopulateClientSelectList(List<CoachClientRelationship> relationships)
        {
            var clients = relationships
                .Select(r => new
                {
                    Id = r.ClientId,
                    Name = r.Client.UserName.Split('@')[0] + " (" + r.Client.Email + ")"
                })
                .OrderBy(c => c.Name)
                .ToList();

            clients.Insert(0, new { Id = "", Name = "All Clients" });
            ClientSelectList = new SelectList(clients, "Id", "Name", ClientId);
        }

        private void PopulateCategorySelectList()
        {
            var categories = Enum.GetValues(typeof(GoalCategory))
                .Cast<GoalCategory>()
                .Select(c => new
                {
                    Id = ((int)c).ToString(),
                    Name = c.ToString()
                })
                .ToList();

            categories.Insert(0, new { Id = "", Name = "All Categories" });
            CategorySelectList = new SelectList(categories, "Id", "Name", CategoryFilter);
        }

        private void PopulateStatusSelectList()
        {
            var statuses = new List<object>
            {
                new { Id = "", Name = "All Statuses" },
                new { Id = "active", Name = "Active" },
                new { Id = "completed", Name = "Completed" }
            };

            StatusSelectList = new SelectList(statuses, "Id", "Name", StatusFilter);
        }

        private async Task PopulateGoalsList(string coachId)
        {
            IEnumerable<ClientGoal> goals;

            // First filter by client if specified
            if (!string.IsNullOrEmpty(ClientId))
            {
                goals = await _goalQueryService.GetClientGoalsForCoachAsync(coachId, ClientId);
            }
            else
            {
                goals = await _goalQueryService.GetGoalsForCoachAsync(coachId);
            }

            // Then filter by category if specified
            if (!string.IsNullOrEmpty(CategoryFilter) && int.TryParse(CategoryFilter, out int categoryValue))
            {
                var category = (GoalCategory)categoryValue;
                goals = goals.Where(g => g.Category == category);
            }

            // Finally filter by status if specified
            if (!string.IsNullOrEmpty(StatusFilter))
            {
                if (StatusFilter == "active")
                {
                    goals = goals.Where(g => g.IsActive && !g.IsCompleted && !g.CompletedDate.HasValue);
                }
                else if (StatusFilter == "completed")
                {
                    goals = goals.Where(g => g.IsCompleted || g.CompletedDate.HasValue);
                }
            }

            // Convert to view models
            foreach (var goal in goals)
            {
                var clientName = "Unknown";
                
                // Get client name if it's a coach-created goal
                if (goal.CoachClientRelationshipId.HasValue)
                {
                    var relationship = await _context.CoachClientRelationships
                        .Include(r => r.Client)
                        .FirstOrDefaultAsync(r => r.Id == goal.CoachClientRelationshipId.Value);
                    
                    if (relationship != null)
                    {
                        clientName = relationship.Client.UserName.Split('@')[0];
                    }
                }
                // Get client name from UserId if it's a user-created goal
                else if (!string.IsNullOrEmpty(goal.UserId))
                {
                    var client = await _userManager.FindByIdAsync(goal.UserId);
                    if (client != null)
                    {
                        clientName = client.UserName.Split('@')[0];
                    }
                }

                Goals.Add(new GoalViewModel
                {
                    Id = goal.Id,
                    ClientName = clientName,
                    Description = goal.Description,
                    Category = goal.Category.ToString(),
                    TargetDate = goal.TargetDate,
                    IsCompleted = goal.IsCompleted || goal.CompletedDate.HasValue,
                    Progress = goal.ProgressPercentage,
                    CreatedDate = goal.CreatedDate,
                    MeasurementType = goal.MeasurementType,
                    StartValue = goal.StartValue,
                    CurrentValue = goal.CurrentValue,
                    TargetValue = goal.TargetValue,
                    MeasurementUnit = goal.MeasurementUnit
                });
            }

            // Sort goals by target date (closest first), then by progress (highest first)
            Goals = Goals
                .OrderBy(g => g.IsCompleted)
                .ThenBy(g => g.TargetDate)
                .ThenByDescending(g => g.Progress)
                .ToList();
        }

        public async Task<IActionResult> OnPostCompleteGoalAsync(int goalId)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            try
            {
                var goal = await _context.ClientGoals
                    .FirstOrDefaultAsync(g => g.Id == goalId);

                if (goal == null)
                {
                    _validationService.SetError(this, "Goal not found.");
                    return RedirectToPage();
                }

                // Validate that this coach has access to this goal
                var hasAccess = false;
                
                if (goal.CoachClientRelationshipId.HasValue)
                {
                    // Check if the goal is associated with a relationship where this user is the coach
                    var relationship = await _context.CoachClientRelationships
                        .FirstOrDefaultAsync(r => r.Id == goal.CoachClientRelationshipId.Value && r.CoachId == coachId);
                    
                    hasAccess = relationship != null;
                }
                else if (!string.IsNullOrEmpty(goal.UserId))
                {
                    // Check if the goal's user is a client of this coach and the goal is visible to coach
                    var relationship = await _context.CoachClientRelationships
                        .FirstOrDefaultAsync(r => r.ClientId == goal.UserId && r.CoachId == coachId && r.Status == RelationshipStatus.Active);
                    
                    hasAccess = relationship != null && goal.IsVisibleToCoach;
                }

                if (!hasAccess)
                {
                    _validationService.SetError(this, "You don't have permission to modify this goal.");
                    return RedirectToPage();
                }

                // Mark as completed
                goal.IsCompleted = true;
                goal.CompletedDate = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _validationService.SetSuccess(this, "Goal marked as completed successfully.");
                _logger.LogInformation("Coach {CoachId} marked goal {GoalId} as completed", coachId, goalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing goal {GoalId} by coach {CoachId}", goalId, coachId);
                _validationService.SetError(this, "An error occurred while completing the goal. Please try again.");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteGoalAsync(int goalId)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            try
            {
                var goal = await _context.ClientGoals
                    .FirstOrDefaultAsync(g => g.Id == goalId);

                if (goal == null)
                {
                    _validationService.SetError(this, "Goal not found.");
                    return RedirectToPage();
                }

                // Validate that this coach has access to this goal and it was created by them
                var hasAccess = false;
                
                if (goal.CoachClientRelationshipId.HasValue)
                {
                    // Check if the goal is associated with a relationship where this user is the coach
                    var relationship = await _context.CoachClientRelationships
                        .FirstOrDefaultAsync(r => r.Id == goal.CoachClientRelationshipId.Value && r.CoachId == coachId);
                    
                    hasAccess = relationship != null && goal.IsCoachCreated;
                }

                if (!hasAccess)
                {
                    _validationService.SetError(this, "You don't have permission to delete this goal. You can only delete goals that you created.");
                    return RedirectToPage();
                }

                // Delete the goal
                _context.ClientGoals.Remove(goal);
                await _context.SaveChangesAsync();
                
                _validationService.SetSuccess(this, "Goal deleted successfully.");
                _logger.LogInformation("Coach {CoachId} deleted goal {GoalId}", coachId, goalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting goal {GoalId} by coach {CoachId}", goalId, coachId);
                _validationService.SetError(this, "An error occurred while deleting the goal. Please try again.");
            }

            return RedirectToPage();
        }

        public class GoalViewModel
        {
            public int Id { get; set; }
            public string ClientName { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public DateTime TargetDate { get; set; }
            public bool IsCompleted { get; set; }
            public int Progress { get; set; }
            public DateTime CreatedDate { get; set; }
            public string MeasurementType { get; set; }
            public decimal? StartValue { get; set; }
            public decimal? CurrentValue { get; set; }
            public decimal? TargetValue { get; set; }
            public string MeasurementUnit { get; set; }
        }
    }
}