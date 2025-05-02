using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Areas.Coach.ViewModels;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Reports
{
    [Area("Coach")]
    [CoachAuthorize]
    public class ClientDetailModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<ClientDetailModel> _logger;

        public ClientDetailModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ILogger<ClientDetailModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public string ClientName { get; set; }
        public int TotalSessions { get; set; }
        public int ActiveGoals { get; set; }
        public decimal ConsistencyRate { get; set; }
        public decimal AverageGoalProgress { get; set; }
        public List<GoalViewModel> Goals { get; set; } = new();
        public object ChartData { get; set; }
        public WorkoutStatsViewModel WorkoutStats { get; set; }

        private DateTime GetStartDateByPeriod(int period)
        {
            var now = DateTime.UtcNow;
            return period switch
            {
                1 => now.AddDays(-30),
                3 => now.AddDays(-90),
                6 => now.AddMonths(-6),
                12 => now.AddYears(-1),
                _ => now.AddDays(-30)
            };
        }

        public async Task<IActionResult> OnGetAsync(string clientId, int period = 1)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Parse the string ID to int for Session model compatibility
            if (!int.TryParse(clientId, out int clientIdInt))
            {
                return NotFound();
            }

            // Verify coach has access to this client
            var relationship = await _context.CoachClientRelationships
                .Include(r => r.Client)
                .FirstOrDefaultAsync(r => r.CoachId == coachId && r.ClientId == clientId && r.Status == RelationshipStatus.Active);

            if (relationship == null)
            {
                return NotFound();
            }

            ClientName = relationship.Client?.UserName?.Split('@')[0] ?? "Unknown";
            var startDate = GetStartDateByPeriod(period);

            // Get sessions in the period using the parsed int ID
            var sessions = await _context.WorkoutSessions
                .Where(s => s.UserId == clientIdInt && s.StartDateTime >= startDate)
                .ToListAsync();

            TotalSessions = sessions.Count;

            // Get active goals
            var goals = await _context.ClientGoals
                .Where(g => (g.UserId == clientId || 
                       (g.CoachClientRelationshipId.HasValue && g.CoachClientRelationshipId.Value == relationship.Id)) &&
                       g.IsActive && !g.IsCompleted)
                .Include(g => g.Category)
                .Include(g => g.Relationship)
                    .ThenInclude(r => r.Coach)
                .ToListAsync();

            Goals = goals.Select(g => new GoalViewModel
            {
                Id = g.Id,
                Description = g.Description,
                Category = g.Category.ToString(),
                TargetDate = g.TargetDate,
                Progress = g.ProgressPercentage,
                CreatedBy = g.IsCoachCreated ? 
                    g.Relationship?.Coach?.UserName?.Split('@')[0] ?? "Unknown" : 
                    "Self"
            }).ToList();

            ActiveGoals = goals.Count(g => g.IsActive && !g.IsCompleted);

            return Page();
        }

        public async Task<IActionResult> OnPostSendGoalFeedbackAsync(int goalId, string feedbackType, string feedbackMessage, bool sendNotification = true)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            // Find the goal and verify access
            var goal = await _context.ClientGoals
                .Include(g => g.Relationship)
                    .ThenInclude(r => r.Client)
                .FirstOrDefaultAsync(g => g.Id == goalId);

            if (goal == null)
            {
                TempData["ErrorMessage"] = "Goal not found.";
                return RedirectToPage();
            }

            // Verify coach has access to this client's goals
            var hasAccess = await _context.CoachClientRelationships
                .AnyAsync(r => r.CoachId == coachId && r.ClientId == goal.UserId && r.Status == RelationshipStatus.Active);

            if (!hasAccess)
            {
                return Forbid();
            }

            // Create and save the feedback
            var feedback = new GoalFeedback
            {
                GoalId = goalId,
                CoachId = coachId,
                FeedbackType = feedbackType,
                Message = feedbackMessage,
                CreatedDate = DateTime.UtcNow
            };

            _context.GoalFeedback.Add(feedback);

            // Create an activity entry
            var activity = new ClientActivity
            {
                ClientId = goal.UserId,
                CoachId = coachId,
                ActivityType = "GoalFeedback",
                Description = $"Received feedback on goal: {goal.Description}",
                ActivityDate = DateTime.UtcNow,
                RelatedEntityType = "Goal",
                RelatedEntityId = goalId.ToString()
            };

            _context.ClientActivities.Add(activity);
            await _context.SaveChangesAsync();

            // Send email notification if requested
            if (sendNotification && goal.Relationship?.Client?.Email != null)
            {
                _logger.LogInformation("Would send email to {Email} about goal feedback", goal.Relationship.Client.Email);
            }

            TempData["SuccessMessage"] = "Feedback sent successfully.";
            return RedirectToPage(new { id = goal.UserId });
        }

        public class GoalViewModel
        {
            public int Id { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public DateTime TargetDate { get; set; }
            public decimal Progress { get; set; }
            public string CreatedBy { get; set; }
            public string Status { get; set; }
        }

        public class WorkoutStatsViewModel
        {
            public int TotalSessions { get; set; }
            public int CompletedSessions { get; set; }
            public decimal AverageDuration { get; set; }
            public int TotalExercises { get; set; }
        }
    }
}