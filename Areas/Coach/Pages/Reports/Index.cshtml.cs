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
    public class IndexModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public int ActiveClientCount { get; set; }
        public int TotalSessions { get; set; }
        public int ActiveGoalCount { get; set; }
        public decimal AverageConsistency { get; set; }
        public List<ClientReportViewModel> Clients { get; set; } = new();
        public object ChartData { get; set; }
        public WorkoutStatsViewModel OverviewStats { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var coachId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(coachId))
            {
                return Forbid();
            }

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            // Get all active client relationships
            var relationships = await _context.CoachClientRelationships
                .Include(r => r.Client)
                .Where(r => r.CoachId == coachId && r.Status == RelationshipStatus.Active)
                .ToListAsync();

            ActiveClientCount = relationships.Count;

            // Get client IDs and create a mapping between string and int IDs
            var clientIds = relationships.Select(r => r.ClientId).ToList();
            var clientIdInts = clientIds.Select(id => int.TryParse(id, out int intId) ? intId : -1)
                                       .Where(id => id != -1)
                                       .ToList();

            // Get all sessions for these clients in the last 30 days
            var sessions = await _context.Session
                .Where(s => clientIdInts.Contains(s.UserId) && s.datetime >= thirtyDaysAgo)
                .ToListAsync();

            TotalSessions = sessions.Count();

            // Get all active goals
            var goals = await _context.ClientGoals
                .Where(g => g.IsActive && !g.IsCompleted)
                .ToListAsync();

            // Filter goals after fetching
            goals = goals.Where(g => 
                clientIds.Contains(g.UserId) || 
                (g.CoachClientRelationshipId.HasValue && 
                relationships.Any(r => r.Id == g.CoachClientRelationshipId.Value)))
                .ToList();

            ActiveGoalCount = goals.Count();

            // Calculate client metrics
            foreach (var rel in relationships)
            {
                // Parse client ID to int for session queries
                if (!int.TryParse(rel.ClientId, out int clientIdInt))
                {
                    continue;
                }

                var clientSessions = sessions.Where(s => s.UserId == clientIdInt).ToList();
                var clientGoals = goals.Where(g => g.UserId == rel.ClientId || 
                    (g.CoachClientRelationshipId.HasValue && g.CoachClientRelationshipId.Value == rel.Id)).ToList();

                // Calculate consistency (sessions vs expected sessions)
                var expectedSessions = 12; // Assuming 3 sessions per week
                var actualSessions = clientSessions.Count();
                var consistency = actualSessions * 100.0M / expectedSessions;

                var client = new ClientReportViewModel
                {
                    Id = rel.ClientId,
                    Name = rel.Client?.UserName?.Split('@')[0] ?? "Unknown",
                    ActiveGoalCount = clientGoals.Count(),
                    RecentSessionCount = actualSessions,
                    ConsistencyRate = Math.Min(100, Math.Round(consistency, 1)),
                    AverageGoalProgress = clientGoals.Any() 
                        ? Math.Round((decimal)clientGoals.Average(g => g.ProgressPercentage), 1)
                        : 0
                };

                Clients.Add(client);
            }

            // Calculate average consistency
            AverageConsistency = Clients.Any() 
                ? Math.Round(Clients.Average(c => c.ConsistencyRate), 1)
                : 0;

            // Prepare chart data
            var sessionsByDate = sessions
                .GroupBy(s => s.datetime.Date)
                .OrderBy(g => g.Key)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToList();

            var completedGoals = await _context.ClientGoals
                .CountAsync(g => clientIds.Contains(g.UserId) && g.IsCompleted);
            var inProgressGoals = goals.Count();
            var pendingGoals = await _context.ClientGoals
                .CountAsync(g => clientIds.Contains(g.UserId) && !g.IsActive && !g.IsCompleted);

            ChartData = new
            {
                clientProgress = new
                {
                    labels = Clients.Select(c => c.Name).ToList(),
                    datasets = new[]
                    {
                        new
                        {
                            label = "Goal Progress",
                            data = Clients.Select(c => c.AverageGoalProgress).ToList(),
                            backgroundColor = "rgba(75, 192, 192, 0.2)",
                            borderColor = "rgba(75, 192, 192, 1)"
                        },
                        new
                        {
                            label = "Consistency Rate",
                            data = Clients.Select(c => c.ConsistencyRate).ToList(),
                            backgroundColor = "rgba(54, 162, 235, 0.2)",
                            borderColor = "rgba(54, 162, 235, 1)"
                        }
                    }
                },
                goals = new
                {
                    labels = new[] { "Completed", "In Progress", "Pending" },
                    data = new[] { completedGoals, inProgressGoals, pendingGoals }
                },
                sessions = new
                {
                    labels = sessionsByDate.Select(s => s.Date.ToString("MM/dd")).ToList(),
                    data = sessionsByDate.Select(s => s.Count).ToList()
                }
            };

            return Page();
        }

        public class ClientReportViewModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int ActiveGoalCount { get; set; }
            public int RecentSessionCount { get; set; }
            public decimal ConsistencyRate { get; set; }
            public decimal AverageGoalProgress { get; set; }
        }
    }
}