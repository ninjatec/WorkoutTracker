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

            // For the sake of demonstration, populate with sample data
            // In a real implementation, you would fetch this data from your database
            PopulateSampleDashboardData(relationshipsList);

            return Page();
        }

        private void PopulateSampleDashboardData(List<CoachClientRelationship> relationships)
        {
            // This method would be replaced with actual database queries in a full implementation
            
            // Sample recent activities
            if (relationships.Any())
            {
                var random = new Random();
                var activities = new List<string>
                {
                    "Completed a strength training workout",
                    "Achieved a new personal record",
                    "Completed a scheduled assessment",
                    "Missed a scheduled workout",
                    "Updated their profile information"
                };

                var activityTypes = new List<string>
                {
                    "Workout", "Achievement", "Assessment", "Missed Session", "Profile Update"
                };

                foreach (var relationship in relationships.Where(r => r.Status == RelationshipStatus.Active).Take(5))
                {
                    var activityIndex = random.Next(activities.Count);
                    RecentActivities.Add(new ClientActivityViewModel
                    {
                        ClientName = relationship.Client?.UserName?.Split('@')[0] ?? "Client",
                        Date = DateTime.Now.AddDays(-random.Next(7)),
                        Description = activities[activityIndex],
                        Type = activityTypes[activityIndex]
                    });
                }
            }

            // Sample client goals
            if (relationships.Any())
            {
                var random = new Random();
                var goals = new List<string>
                {
                    "Increase bench press by 10%",
                    "Lose 5kg of body weight",
                    "Run 5k under 25 minutes",
                    "Complete 10 pull-ups",
                    "Reduce body fat percentage to 18%"
                };

                foreach (var relationship in relationships.Where(r => r.Status == RelationshipStatus.Active).Take(5))
                {
                    ClientGoals.Add(new ClientGoalViewModel
                    {
                        ClientName = relationship.Client?.UserName?.Split('@')[0] ?? "Client",
                        Description = goals[random.Next(goals.Count)],
                        Progress = random.Next(10, 100)
                    });
                }
            }

            // Sample upcoming sessions
            if (relationships.Any())
            {
                var random = new Random();
                var sessionTypes = new List<string>
                {
                    "Strength Training Session",
                    "Progress Assessment",
                    "Goal Setting Meeting",
                    "Technique Review",
                    "Endurance Training"
                };

                foreach (var relationship in relationships.Where(r => r.Status == RelationshipStatus.Active).Take(3))
                {
                    UpcomingSessions.Add(new UpcomingSessionViewModel
                    {
                        ClientName = relationship.Client?.UserName?.Split('@')[0] ?? "Client",
                        Date = DateTime.Now.AddDays(random.Next(1, 10)).AddHours(random.Next(9, 17)),
                        Description = sessionTypes[random.Next(sessionTypes.Count)]
                    });
                }

                // Sort by date
                UpcomingSessions = UpcomingSessions.OrderBy(s => s.Date).ToList();
            }
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
            public string ClientName { get; set; }
            public string Description { get; set; }
            public int Progress { get; set; }
        }

        public class UpcomingSessionViewModel
        {
            public string ClientName { get; set; }
            public DateTime Date { get; set; }
            public string Description { get; set; }
        }
    }
}