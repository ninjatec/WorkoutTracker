using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.Services.Coaching;
using WorkoutTrackerWeb.Pages.Goals;

namespace WorkoutTrackerWeb.Pages;

// Add conditional output caching - don't cache for unauthenticated users
[OutputCache(PolicyName = "HomePagePolicy", NoStore = false)]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly UserManager<AppUser> _userManager;
    private readonly WorkoutTrackerWebContext _context;
    private readonly GoalQueryService _goalQueryService;

    public IndexModel(
        ILogger<IndexModel> logger,
        UserManager<AppUser> userManager,
        WorkoutTrackerWebContext context,
        GoalQueryService goalQueryService)
    {
        _logger = logger;
        _userManager = userManager;
        _context = context;
        _goalQueryService = goalQueryService;
    }
    
    public List<Goals.IndexModel.GoalViewModel> ActiveGoals { get; set; } = new List<Goals.IndexModel.GoalViewModel>();
    public int StreakCount { get; set; }
    public string RecentMilestone { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // If user is not authenticated, redirect to login page
        if (!User.Identity.IsAuthenticated)
        {
            // Add cache control headers to prevent caching this redirect
            Response.Headers.Append("Cache-Control", "no-store, no-cache, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            
            // Log the redirect to help with debugging
            _logger.LogDebug("Redirecting unauthenticated user from home page to login page");
            
            // Include return URL for better user experience
            return RedirectToPage("/Account/Login", new { area = "Identity", ReturnUrl = "/" });
        }

        // Load user's active goals for dashboard
        await LoadActiveGoalsAsync();
        
        // Calculate streak and find recent milestones
        await CalculateStreakAsync();
        await FindRecentMilestoneAsync();
        
        // Mark that this is a successful page load
        _logger.LogDebug("Home page successfully loaded for authenticated user");
        return Page();
    }
    
    private async Task LoadActiveGoalsAsync()
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }
            
            // Get user's goals using the existing service
            var goals = await _goalQueryService.GetGoalsForUserAsync(userId);
            
            // Filter to active goals and sort by target date (closest first)
            var activeGoals = goals
                .Where(g => !g.IsCompleted || (g.CompletedDate.HasValue && g.CompletedDate.Value > DateTime.UtcNow.AddDays(-7)))
                .OrderBy(g => g.IsCompleted)
                .ThenBy(g => g.TargetDate)
                .ThenByDescending(g => g.ProgressPercentage)
                .Take(3)
                .ToList();
                
            // Convert to view models
            foreach (var goal in activeGoals)
            {
                ActiveGoals.Add(new Goals.IndexModel.GoalViewModel
                {
                    Id = goal.Id,
                    Description = goal.Description,
                    Category = goal.Category.ToString(),
                    TargetDate = goal.TargetDate,
                    IsCompleted = goal.IsCompleted || goal.CompletedDate.HasValue,
                    Progress = goal.ProgressPercentage,
                    MeasurementType = goal.MeasurementType,
                    StartValue = goal.StartValue,
                    CurrentValue = goal.CurrentValue,
                    TargetValue = goal.TargetValue,
                    MeasurementUnit = goal.MeasurementUnit
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading active goals for dashboard");
        }
    }
    
    private async Task CalculateStreakAsync()
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }
            
            // Calculate streak based on consecutive days with completed goals or workouts
            var today = DateTime.UtcNow.Date;
            var streak = 0;
            var currentDate = today;
            
            while (true)
            {
                // Check if there are completed goals or workouts on this date
                bool hasActivity = false;
                
                // Check for goal completions on this date
                var goalCompletions = await _context.ClientGoals
                    .Where(g => g.UserId == userId && 
                           g.CompletedDate.HasValue && 
                           g.CompletedDate.Value.Date == currentDate)
                    .AnyAsync();
                           
                // Check for workouts on this date
                var workouts = await _context.Session
                    .Where(s => s.UserId == int.Parse(userId) && 
                           s.datetime.Date == currentDate)
                    .AnyAsync();
                
                hasActivity = goalCompletions || workouts;
                
                if (hasActivity)
                {
                    streak++;
                    currentDate = currentDate.AddDays(-1);
                }
                else
                {
                    // Streak is broken
                    break;
                }
            }
            
            StreakCount = streak;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating streak");
        }
    }
    
    private async Task FindRecentMilestoneAsync()
    {
        try
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }
            
            // Find recently completed goals (within the last 7 days)
            var recentCompletions = await _context.ClientGoals
                .Where(g => g.UserId == userId && 
                       g.CompletedDate.HasValue && 
                       g.CompletedDate.Value > DateTime.UtcNow.AddDays(-7))
                .OrderByDescending(g => g.CompletedDate)
                .FirstOrDefaultAsync();
                
            if (recentCompletions != null)
            {
                RecentMilestone = $"You completed your '{recentCompletions.Description}' goal!";
                return;
            }
            
            // Count total completed goals
            var totalCompleted = await _context.ClientGoals
                .CountAsync(g => g.UserId == userId && g.IsCompleted);
                
            if (totalCompleted > 0 && totalCompleted % 5 == 0)
            {
                // Milestone for every 5 completed goals
                RecentMilestone = $"You've completed {totalCompleted} goals so far. Great job!";
                return;
            }
            
            // Check for workout milestones
            var totalWorkouts = await _context.Session
                .CountAsync(s => s.UserId == int.Parse(userId));
                
            if (totalWorkouts > 0 && totalWorkouts % 10 == 0)
            {
                // Milestone for every 10 workouts
                RecentMilestone = $"You've completed {totalWorkouts} workouts so far. Keep it up!";
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding recent milestones");
        }
    }
}
