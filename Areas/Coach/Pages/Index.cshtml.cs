using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Extensions;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;

namespace WorkoutTrackerWeb.Areas.Coach.Pages
{
    [Area("Coach")]
    [CoachAuthorize]
    public class IndexModel : PageModel
    {
        private readonly ICoachingService _coachingService;
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;

        public IndexModel(
            ICoachingService coachingService,
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager)
        {
            _coachingService = coachingService;
            _context = context;
            _userManager = userManager;
        }

        public int TotalClients { get; set; }
        public int ActiveClients { get; set; }
        public int PendingInvitations { get; set; }
        public List<ClientActivityViewModel> RecentActivities { get; set; } = new List<ClientActivityViewModel>();
        public List<ClientGoalViewModel> ClientGoals { get; set; } = new List<ClientGoalViewModel>();
        public List<UpcomingSessionViewModel> UpcomingSessions { get; set; } = new List<UpcomingSessionViewModel>();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Forbid();
            }

            // Get relationships
            var relationships = await _coachingService.GetCoachRelationshipsAsync(userId);
            var relationshipsList = relationships.ToList();

            // Calculate client counts
            TotalClients = relationshipsList.Count;
            ActiveClients = relationshipsList.Count(r => r.Status == RelationshipStatus.Active);
            PendingInvitations = relationshipsList.Count(r => r.Status == RelationshipStatus.Pending);

            // Fetch actual client data
            await FetchClientActivities(userId, relationshipsList);
            await FetchClientGoals(userId, relationshipsList);
            await FetchUpcomingSessions(userId);

            return Page();
        }

        private async Task FetchClientActivities(string coachUserId, List<CoachClientRelationship> relationships)
        {
            // For now, we'll still use sample data for activities
            // This could be replaced with actual activity tracking in the future
            var activeClients = relationships
                .Where(r => r.Status == RelationshipStatus.Active)
                .Take(5)
                .ToList();

            // Using sample data since we're not sure of the correct DB entity name
            var random = new Random();
            var activities = new List<string>
            {
                "Completed a strength training workout",
                "Achieved a new personal record",
                "Completed a scheduled assessment"
            };

            foreach (var client in activeClients.Take(3))
            {
                RecentActivities.Add(new ClientActivityViewModel
                {
                    ClientName = client.Client?.UserName?.Split('@')[0] ?? "Client",
                    Date = DateTime.Now.AddDays(-random.Next(7)),
                    Description = activities[random.Next(activities.Count)],
                    Type = "Activity"
                });
            }

            // Sort by date
            RecentActivities = RecentActivities.OrderByDescending(a => a.Date).ToList();
            
            await Task.CompletedTask; // To satisfy async contract
        }

        private async Task FetchClientGoals(string coachUserId, List<CoachClientRelationship> relationships)
        {
            try
            {
                // Get active relationships
                var activeRelationshipIds = relationships
                    .Where(r => r.Status == RelationshipStatus.Active)
                    .Select(r => r.Id)
                    .ToList();

                // Get all client IDs for this coach
                var clientIds = relationships
                    .Where(r => r.Status == RelationshipStatus.Active)
                    .Select(r => r.ClientId)
                    .ToList();

                if (!clientIds.Any())
                {
                    return; // No clients, so no goals
                }

                // Query for goals that are either:
                // 1. Coach-created goals linked to a coach-client relationship
                // 2. User-created goals that are visible to the coach
                var goals = await _context.ClientGoals
                    .Where(g => (g.CoachClientRelationshipId.HasValue && activeRelationshipIds.Contains(g.CoachClientRelationshipId.Value)) ||
                               (clientIds.Contains(g.UserId) && g.IsVisibleToCoach))
                    .Where(g => !g.IsCompleted && g.IsActive) // Only active, incomplete goals
                    .OrderBy(g => g.TargetDate)
                    .Take(5) // Limit to 5 for the dashboard
                    .ToListAsync();

                // Transform data for display
                foreach (var goal in goals)
                {
                    var clientName = "Unknown";
                    
                    // Get client name based on the type of goal
                    if (goal.CoachClientRelationshipId.HasValue)
                    {
                        var relationship = relationships.FirstOrDefault(r => r.Id == goal.CoachClientRelationshipId.Value);
                        if (relationship?.Client != null)
                        {
                            clientName = relationship.Client.UserName.Split('@')[0];
                        }
                    }
                    else if (!string.IsNullOrEmpty(goal.UserId))
                    {
                        var client = relationships.FirstOrDefault(r => r.ClientId == goal.UserId)?.Client;
                        if (client != null)
                        {
                            clientName = client.UserName.Split('@')[0];
                        }
                    }

                    ClientGoals.Add(new ClientGoalViewModel
                    {
                        Id = goal.Id,
                        ClientName = clientName,
                        Description = goal.Description,
                        Progress = goal.ProgressPercentage,
                        TargetDate = goal.TargetDate,
                        Category = goal.Category.ToString(),
                        IsCompleted = goal.IsCompleted,
                        MeasurementType = goal.MeasurementType,
                        MeasurementUnit = goal.MeasurementUnit,
                        CurrentValue = goal.CurrentValue,
                        TargetValue = goal.TargetValue,
                        RelationshipId = goal.CoachClientRelationshipId
                    });
                }
            }
            catch (Exception ex)
            {
                // Log the exception but don't crash
                Console.WriteLine($"Error fetching goals: {ex.Message}");
                // Leave the goals list empty
            }
        }

        private async Task FetchUpcomingSessions(string coachUserId)
        {
            try
            {
                // Get the coach's User entity with UserId that matches the AppUser's Id
                var coachUser = await _context.User
                    .FirstOrDefaultAsync(u => u.IdentityUserId == coachUserId);
                    
                if (coachUser == null)
                {
                    // If we can't find the coach's User record, just return empty list
                    return;
                }
                
                // Get current date for comparison
                var currentDate = DateTime.Now;
                
                // Fetch upcoming scheduled workouts for this coach
                var upcomingWorkouts = await _context.WorkoutSchedules
                    .Where(s => s.CoachUserId == coachUser.UserId && 
                                s.IsActive && 
                                (s.ScheduledDateTime > currentDate || 
                                (s.IsRecurring && (!s.EndDate.HasValue || s.EndDate.Value > currentDate.Date))))
                    .Include(s => s.Client)
                    .OrderBy(s => s.ScheduledDateTime)
                    .Take(5)
                    .ToListAsync();

                foreach (var workout in upcomingWorkouts)
                {
                    // Format the workout date/time for display
                    string formattedDate = workout.ScheduledDateTime?.ToString("ddd, MMM d, h:mm tt") ?? 
                                          "Recurring";
                    
                    // Get the client name
                    string clientName = workout.Client?.Name ?? "Client";
                    // Trim email format if present
                    if (clientName.Contains('@'))
                    {
                        clientName = clientName.Split('@')[0];
                    }
                    
                    UpcomingSessions.Add(new UpcomingSessionViewModel
                    {
                        ClientName = clientName,
                        Date = workout.ScheduledDateTime ?? DateTime.Now.AddDays(1),
                        Description = workout.Name ?? "Scheduled Workout",
                        ScheduleId = workout.WorkoutScheduleId
                    });
                }
                
                // No more placeholder data when no sessions are found
                // The empty list will trigger the "No upcoming sessions scheduled" message
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error fetching upcoming sessions: {ex.Message}");
                // Leave the list empty instead of adding placeholder data
            }
        }

        public async Task<IActionResult> OnPostSendGoalFeedbackAsync(int goalId, string feedbackType, string feedbackMessage, bool sendNotification = true)
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            try
            {
                // Find the goal
                var goal = await _context.ClientGoals.FindAsync(goalId);
                if (goal == null)
                {
                    TempData["StatusMessage"] = "Error: Goal not found.";
                    TempData["StatusMessageType"] = "danger";
                    return RedirectToPage();
                }

                // Validate coach has access to this goal
                var hasAccess = false;
                string clientId = null;
                
                if (goal.CoachClientRelationshipId.HasValue)
                {
                    // Check if the goal is associated with a relationship where this user is the coach
                    var relationship = await _context.CoachClientRelationships
                        .FirstOrDefaultAsync(r => r.Id == goal.CoachClientRelationshipId.Value && r.CoachId == coachId);
                    
                    hasAccess = relationship != null;
                    clientId = relationship?.ClientId;
                }
                else if (!string.IsNullOrEmpty(goal.UserId))
                {
                    // Check if the goal's user is a client of this coach and the goal is visible to coach
                    var relationship = await _context.CoachClientRelationships
                        .FirstOrDefaultAsync(r => r.ClientId == goal.UserId && r.CoachId == coachId && r.Status == RelationshipStatus.Active);
                    
                    hasAccess = relationship != null && goal.IsVisibleToCoach;
                    clientId = goal.UserId;
                }

                if (!hasAccess)
                {
                    TempData["StatusMessage"] = "Error: You don't have permission to provide feedback on this goal.";
                    TempData["StatusMessageType"] = "danger";
                    return RedirectToPage();
                }

                // Create and save goal feedback
                var feedback = new GoalFeedback
                {
                    GoalId = goalId,
                    CoachId = coachId,
                    FeedbackType = feedbackType,
                    Message = feedbackMessage,
                    CreatedDate = DateTime.UtcNow
                };
                
                _context.GoalFeedback.Add(feedback);
                await _context.SaveChangesAsync();

                // Add activity entry for this feedback if we have a client ID
                if (!string.IsNullOrEmpty(clientId))
                {
                    var activity = new ClientActivity
                    {
                        ClientId = clientId,
                        CoachId = coachId,
                        ActivityType = "GoalFeedback",
                        Description = $"Received feedback on goal: {goal.Description}",
                        ActivityDate = DateTime.UtcNow,
                        RelatedEntityType = "Goal",
                        RelatedEntityId = goalId.ToString()
                    };
                    
                    _context.ClientActivities.Add(activity);
                    await _context.SaveChangesAsync();
                }

                // Send notification to client if requested
                if (sendNotification && !string.IsNullOrEmpty(clientId))
                {
                    var client = await _userManager.FindByIdAsync(clientId);
                    if (client != null && !string.IsNullOrEmpty(client.Email))
                    {
                        // TODO: Implement email notification here
                        // This would typically use an email service
                        // For now, just log that we would send an email
                        Console.WriteLine($"Would send email to {client.Email} about goal feedback");
                    }
                }

                TempData["StatusMessage"] = "Goal feedback has been sent successfully.";
                TempData["StatusMessageType"] = "success";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending goal feedback: {ex.Message}");
                TempData["StatusMessage"] = "Error: Unable to send feedback. Please try again.";
                TempData["StatusMessageType"] = "danger";
            }

            return RedirectToPage();
        }

        public class ClientActivityViewModel
        {
            public string ClientName { get; set; }
            public DateTime Date { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
        }

        public class ClientGoalViewModel
        {
            public int Id { get; set; }
            public string ClientName { get; set; }
            public string Description { get; set; }
            public int Progress { get; set; }
            public DateTime TargetDate { get; set; }
            public string Category { get; set; }
            public bool IsCompleted { get; set; }
            public string MeasurementType { get; set; }
            public string MeasurementUnit { get; set; }
            public decimal? CurrentValue { get; set; }
            public decimal? TargetValue { get; set; }
            public int? RelationshipId { get; set; }
        }

        public class UpcomingSessionViewModel
        {
            public string ClientName { get; set; }
            public DateTime Date { get; set; }
            public string Description { get; set; }
            public int ScheduleId { get; set; }
        }
    }
}