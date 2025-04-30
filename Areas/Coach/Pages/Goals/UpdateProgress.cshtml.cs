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
    public class UpdateProgressModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<UpdateProgressModel> _logger;
        private readonly CoachingValidationService _validationService;

        public UpdateProgressModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ILogger<UpdateProgressModel> logger,
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
            decimal newValue,
            string progressNotes)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
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
                    return RedirectToPage("./Index");
                }

                // Validate coach has permission to update this goal's progress
                bool hasAccess = false;
                if (goal.CoachClientRelationshipId.HasValue)
                {
                    var relationship = await _context.CoachClientRelationships
                        .FirstOrDefaultAsync(r => r.Id == goal.CoachClientRelationshipId.Value && r.CoachId == coachId);
                    
                    hasAccess = relationship != null;
                }
                else if (!string.IsNullOrEmpty(goal.UserId))
                {
                    var relationship = await _context.CoachClientRelationships
                        .FirstOrDefaultAsync(r => r.ClientId == goal.UserId && r.CoachId == coachId && r.Status == RelationshipStatus.Active);
                    
                    hasAccess = relationship != null && goal.IsVisibleToCoach;
                }

                if (!hasAccess)
                {
                    _validationService.SetError(this, "You don't have permission to update this goal's progress.");
                    return RedirectToPage("./Index");
                }

                // Check if the goal has measurement tracking
                if (string.IsNullOrEmpty(goal.MeasurementType) || !goal.StartValue.HasValue || !goal.TargetValue.HasValue)
                {
                    _validationService.SetError(this, "This goal doesn't have measurement-based tracking enabled.");
                    return RedirectToPage("./Index");
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

                // If target is reached, mark as completed
                if (targetReached && !goalWasCompleted)
                {
                    goal.IsCompleted = true;
                    goal.CompletedDate = DateTime.UtcNow;
                }

                // Update notes if provided
                if (!string.IsNullOrEmpty(progressNotes))
                {
                    // Append to existing notes or create new notes
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
                _logger.LogInformation("Coach {CoachId} updated progress for goal {GoalId} to {NewValue}", coachId, goalId, newValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating progress for goal {GoalId} by coach {CoachId}", goalId, coachId);
                _validationService.SetError(this, "An error occurred while updating the goal progress. Please try again.");
            }

            return RedirectToPage("./Index");
        }
    }
}