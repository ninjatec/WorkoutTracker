using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Validation;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Goals
{
    [Area("Coach")]
    [CoachAuthorize]
    public class EditModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<EditModel> _logger;
        private readonly CoachingValidationService _validationService;

        public EditModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ILogger<EditModel> logger,
            CoachingValidationService validationService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _validationService = validationService;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string StatusMessageType { get; set; }

        public IActionResult OnGet()
        {
            // This page is only accessed via form submission from Index
            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostAsync(
            int goalId,
            string description,
            int category,
            DateTime targetDate,
            string measurementType,
            string measurementUnit,
            decimal? startValue,
            decimal? targetValue)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            if (!_validationService.ValidateGoalDescription(description, this))
            {
                return RedirectToPage("./Index");
            }

            try
            {
                // Get the goal
                var goal = await _context.ClientGoals
                    .FirstOrDefaultAsync(g => g.Id == goalId);

                if (goal == null)
                {
                    _validationService.SetError(this, "Goal not found.");
                    return RedirectToPage("./Index");
                }

                // Validate coach has permission to edit this goal
                bool hasAccess = false;
                if (goal.CoachClientRelationshipId.HasValue)
                {
                    var relationship = await _context.CoachClientRelationships
                        .FirstOrDefaultAsync(r => r.Id == goal.CoachClientRelationshipId.Value && r.CoachId == coachId);
                    
                    hasAccess = relationship != null && goal.IsCoachCreated;
                }

                if (!hasAccess)
                {
                    _validationService.SetError(this, "You don't have permission to edit this goal.");
                    return RedirectToPage("./Index");
                }

                // Validate measurement data if provided
                if (!string.IsNullOrEmpty(measurementType))
                {
                    if (string.IsNullOrEmpty(measurementUnit))
                    {
                        _validationService.SetError(this, "Measurement unit is required when measurement type is specified.");
                        return RedirectToPage("./Index");
                    }

                    if (!startValue.HasValue || !targetValue.HasValue)
                    {
                        _validationService.SetError(this, "Start value and target value are required when measurement type is specified.");
                        return RedirectToPage("./Index");
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

                // If measurement changed but was previously set, reset current value to start value
                if (goal.MeasurementType != measurementType)
                {
                    goal.CurrentValue = startValue;
                }

                // Save changes
                await _context.SaveChangesAsync();

                _validationService.SetSuccess(this, "Goal updated successfully.");
                _logger.LogInformation("Coach {CoachId} updated goal {GoalId}", coachId, goalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating goal {GoalId} by coach {CoachId}", goalId, coachId);
                _validationService.SetError(this, "An error occurred while updating the goal. Please try again.");
            }

            return RedirectToPage("./Index");
        }
    }
}