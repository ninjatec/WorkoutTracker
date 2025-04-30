using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;
using WorkoutTrackerWeb.Services.Validation;

namespace WorkoutTrackerWeb.Pages.Goals
{
    [Authorize]
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
        public SelectList CategorySelectList { get; set; }
        public SelectList StatusSelectList { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string CategoryFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            try
            {
                // Populate category select list
                PopulateCategorySelectList();

                // Populate status select list
                PopulateStatusSelectList();

                // Get goals based on filters
                await PopulateGoalsList(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading goals for user {UserId}", userId);
                _validationService.SetError(this, "An error occurred while loading goals. Please try again.");
            }

            return Page();
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

        private async Task PopulateGoalsList(string userId)
        {
            IEnumerable<ClientGoal> goals = await _goalQueryService.GetGoalsForUserAsync(userId);

            // Filter by category if specified
            if (!string.IsNullOrEmpty(CategoryFilter) && int.TryParse(CategoryFilter, out int categoryValue))
            {
                var category = (GoalCategory)categoryValue;
                goals = goals.Where(g => g.Category == category);
            }

            // Filter by status if specified
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
                Goals.Add(new GoalViewModel
                {
                    Id = goal.Id,
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
                    MeasurementUnit = goal.MeasurementUnit,
                    Notes = goal.Notes,
                    IsCoachCreated = goal.IsCoachCreated
                });
            }

            // Sort goals by target date (closest first), then by progress (highest first)
            Goals = Goals
                .OrderBy(g => g.IsCompleted)
                .ThenBy(g => g.TargetDate)
                .ThenByDescending(g => g.Progress)
                .ToList();
        }

        public async Task<IActionResult> OnPostCreateGoalAsync(
            string description, 
            int category, 
            DateTime targetDate, 
            string measurementType, 
            string measurementUnit, 
            decimal? startValue, 
            decimal? currentValue, 
            decimal? targetValue,
            string notes,
            bool isVisibleToCoach)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            // Basic validation
            if (string.IsNullOrEmpty(description))
            {
                _validationService.SetError(this, "Goal description is required.");
                return RedirectToPage();
            }

            try
            {
                // Validate measurement data if provided
                if (!string.IsNullOrEmpty(measurementType))
                {
                    if (string.IsNullOrEmpty(measurementUnit))
                    {
                        _validationService.SetError(this, "Measurement unit is required when measurement type is specified.");
                        return RedirectToPage();
                    }

                    if (!startValue.HasValue || !targetValue.HasValue)
                    {
                        _validationService.SetError(this, "Start value and target value are required when measurement type is specified.");
                        return RedirectToPage();
                    }

                    // Set current value to start value if not provided
                    if (!currentValue.HasValue)
                    {
                        currentValue = startValue;
                    }
                }

                // Create new goal
                var goal = new ClientGoal
                {
                    UserId = userId,
                    Description = description,
                    Category = (GoalCategory)category,
                    TargetDate = targetDate,
                    MeasurementType = measurementType,
                    MeasurementUnit = measurementUnit,
                    StartValue = startValue,
                    CurrentValue = currentValue,
                    TargetValue = targetValue,
                    Notes = notes,
                    CreatedDate = DateTime.UtcNow,
                    IsCoachCreated = false,
                    IsActive = true,
                    IsVisibleToCoach = isVisibleToCoach
                };

                // Save to database
                _context.ClientGoals.Add(goal);
                await _context.SaveChangesAsync();

                _validationService.SetSuccess(this, "Goal created successfully.");
                _logger.LogInformation("User {UserId} created personal goal {GoalId}", userId, goal.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating goal by user {UserId}", userId);
                _validationService.SetError(this, "An error occurred while creating the goal. Please try again.");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateProgressAsync(
            int goalId,
            decimal newValue,
            string progressNotes)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            try
            {
                // Get the goal
                var goal = await _context.ClientGoals
                    .FirstOrDefaultAsync(g => g.Id == goalId);

                if (goal == null)
                {
                    _validationService.SetError(this, "Goal not found.");
                    return RedirectToPage();
                }

                // Validate user has permission to update this goal's progress
                bool hasAccess = false;
                
                if (!string.IsNullOrEmpty(goal.UserId) && goal.UserId == userId)
                {
                    hasAccess = true;
                }

                if (!hasAccess)
                {
                    _validationService.SetError(this, "You don't have permission to update this goal's progress.");
                    return RedirectToPage();
                }

                // Check if the goal has measurement tracking
                if (string.IsNullOrEmpty(goal.MeasurementType) || !goal.StartValue.HasValue || !goal.TargetValue.HasValue)
                {
                    _validationService.SetError(this, "This goal doesn't have measurement-based tracking enabled.");
                    return RedirectToPage();
                }

                // Update goal progress
                goal.CurrentValue = newValue;
                goal.LastProgressUpdate = DateTime.UtcNow;

                // Check if goal is now completed based on the target
                bool goalWasCompleted = goal.IsCompleted;
                bool targetReached = false;

                // Different logic based on whether we're trying to increase or decrease the value
                if (goal.StartValue < goal.TargetValue) // Increasing
                {
                    targetReached = goal.CurrentValue >= goal.TargetValue;
                }
                else // Decreasing
                {
                    targetReached = goal.CurrentValue <= goal.TargetValue;
                }

                if (targetReached && !goalWasCompleted)
                {
                    goal.IsCompleted = true;
                    goal.CompletedDate = DateTime.UtcNow;
                }

                // Add progress notes if provided
                if (!string.IsNullOrEmpty(progressNotes))
                {
                    if (string.IsNullOrEmpty(goal.Notes))
                    {
                        goal.Notes = $"[{DateTime.UtcNow:yyyy-MM-dd}] Progress Update: {progressNotes}";
                    }
                    else
                    {
                        goal.Notes += $"\n\n[{DateTime.UtcNow:yyyy-MM-dd}] Progress Update: {progressNotes}";
                    }
                }

                // Save changes
                await _context.SaveChangesAsync();

                string successMessage = targetReached && !goalWasCompleted 
                    ? "Goal progress updated. Goal completed successfully!"
                    : "Goal progress updated successfully.";
                    
                _validationService.SetSuccess(this, successMessage);
                _logger.LogInformation("User {UserId} updated progress for goal {GoalId} to {NewValue}", userId, goalId, newValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress for goal {GoalId} by user {UserId}", goalId, userId);
                _validationService.SetError(this, "An error occurred while updating the goal progress. Please try again.");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCompleteGoalAsync(int goalId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
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

                // Validate user has permission to complete this goal
                bool hasAccess = false;
                
                if (!string.IsNullOrEmpty(goal.UserId) && goal.UserId == userId)
                {
                    hasAccess = true;
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
                _logger.LogInformation("User {UserId} marked goal {GoalId} as completed", userId, goalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing goal {GoalId} by user {UserId}", goalId, userId);
                _validationService.SetError(this, "An error occurred while completing the goal. Please try again.");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditGoalAsync(
            int goalId,
            string description,
            int category,
            DateTime targetDate,
            string measurementType,
            string measurementUnit,
            decimal? startValue,
            decimal? targetValue,
            bool isVisibleToCoach)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            if (string.IsNullOrEmpty(description))
            {
                _validationService.SetError(this, "Goal description is required.");
                return RedirectToPage();
            }

            try
            {
                // Get the goal
                var goal = await _context.ClientGoals
                    .FirstOrDefaultAsync(g => g.Id == goalId);

                if (goal == null)
                {
                    _validationService.SetError(this, "Goal not found.");
                    return RedirectToPage();
                }

                // Validate user has permission to edit this goal
                bool hasAccess = false;
                
                if (!string.IsNullOrEmpty(goal.UserId) && goal.UserId == userId && !goal.IsCoachCreated)
                {
                    hasAccess = true;
                }

                if (!hasAccess)
                {
                    _validationService.SetError(this, "You don't have permission to edit this goal.");
                    return RedirectToPage();
                }

                // Validate measurement data if provided
                if (!string.IsNullOrEmpty(measurementType))
                {
                    if (string.IsNullOrEmpty(measurementUnit))
                    {
                        _validationService.SetError(this, "Measurement unit is required when measurement type is specified.");
                        return RedirectToPage();
                    }

                    if (!startValue.HasValue || !targetValue.HasValue)
                    {
                        _validationService.SetError(this, "Start value and target value are required when measurement type is specified.");
                        return RedirectToPage();
                    }
                }

                // Update goal
                goal.Description = description;
                goal.Category = (GoalCategory)category;
                goal.TargetDate = targetDate;
                goal.MeasurementType = measurementType;
                goal.MeasurementUnit = measurementUnit;
                goal.StartValue = startValue;
                goal.TargetValue = targetValue;
                goal.IsVisibleToCoach = isVisibleToCoach;

                // If measurement changed but was previously set, reset current value to start value
                if (goal.MeasurementType != measurementType)
                {
                    goal.CurrentValue = startValue;
                }

                // Save changes
                await _context.SaveChangesAsync();

                _validationService.SetSuccess(this, "Goal updated successfully.");
                _logger.LogInformation("User {UserId} updated goal {GoalId}", userId, goalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating goal {GoalId} by user {UserId}", goalId, userId);
                _validationService.SetError(this, "An error occurred while updating the goal. Please try again.");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteGoalAsync(int goalId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
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

                // Validate that this user has access to this goal and it was created by them
                bool hasAccess = false;
                
                if (!string.IsNullOrEmpty(goal.UserId) && goal.UserId == userId && !goal.IsCoachCreated)
                {
                    hasAccess = true;
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
                _logger.LogInformation("User {UserId} deleted goal {GoalId}", userId, goalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting goal {GoalId} by user {UserId}", goalId, userId);
                _validationService.SetError(this, "An error occurred while deleting the goal. Please try again.");
            }

            return RedirectToPage();
        }

        public class GoalViewModel
        {
            public int Id { get; set; }
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
            public string Notes { get; set; }
            public bool IsCoachCreated { get; set; }
        }
    }
}