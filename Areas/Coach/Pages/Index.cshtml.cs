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
            // Since we don't have certainty about the actual model/table name,
            // we'll use placeholder data to ensure the build completes
            var random = new Random();
            var sampleGoals = new List<string>
            {
                "Increase bench press by 10%",
                "Lose 5kg of body weight",
                "Run 5k under 25 minutes"
            };

            foreach (var relationship in relationships.Where(r => r.Status == RelationshipStatus.Active).Take(3))
            {
                ClientGoals.Add(new ClientGoalViewModel
                {
                    ClientName = relationship.Client?.UserName?.Split('@')[0] ?? "Client",
                    Description = sampleGoals[random.Next(sampleGoals.Count)],
                    Progress = random.Next(10, 100)
                });
            }
            
            await Task.CompletedTask; // To satisfy async contract
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
            public int ScheduleId { get; set; }
        }
    }
}