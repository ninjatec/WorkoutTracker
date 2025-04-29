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
    public class CreateModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<CreateModel> _logger;
        private readonly CoachingValidationService _validationService;

        public CreateModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ILogger<CreateModel> logger,
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
            string clientId, 
            string description, 
            int category, 
            DateTime targetDate, 
            string measurementType, 
            string measurementUnit, 
            decimal? startValue, 
            decimal? currentValue, 
            decimal? targetValue,
            string notes)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Validation
            if (string.IsNullOrEmpty(clientId))
            {
                _validationService.SetError(this, "Please select a client.");
                return RedirectToPage("./Index");
            }

            if (!_validationService.ValidateGoalDescription(description, this))
            {
                return RedirectToPage("./Index");
            }

            try
            {
                // Check if client relationship exists
                var relationship = await _context.CoachClientRelationships
                    .FirstOrDefaultAsync(r => r.CoachId == coachId && r.ClientId == clientId && r.Status == RelationshipStatus.Active);

                if (relationship == null)
                {
                    _validationService.SetError(this, "Client relationship not found or inactive.");
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

                    // Set current value to start value if not provided
                    if (!currentValue.HasValue)
                    {
                        currentValue = startValue;
                    }
                }

                // Create new goal
                var goal = new ClientGoal
                {
                    CoachClientRelationshipId = relationship.Id,
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
                    IsCoachCreated = true,
                    IsActive = true,
                    IsVisibleToCoach = true,
                    UserId = clientId // Set for proper query filtering
                };

                // Save to database
                _context.ClientGoals.Add(goal);
                await _context.SaveChangesAsync();

                _validationService.SetSuccess(this, "Goal created successfully.");
                _logger.LogInformation("Coach {CoachId} created goal {GoalId} for client {ClientId}", coachId, goal.Id, clientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating goal by coach {CoachId} for client {ClientId}", coachId, clientId);
                _validationService.SetError(this, "An error occurred while creating the goal. Please try again.");
            }

            return RedirectToPage("./Index");
        }
    }
}