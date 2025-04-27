using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;

namespace WorkoutTrackerWeb.Pages.Templates
{
    [Authorize]
    [OutputCache(Duration = 300, VaryByQueryKeys = new[] { "id" })]
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<DetailsModel> _logger;
        private readonly UserManager<User> _userManager;

        public DetailsModel(WorkoutTrackerWebContext context, 
                           IWebHostEnvironment environment, 
                           ILogger<DetailsModel> logger,
                           UserManager<User> userManager)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _userManager = userManager;
        }

        public WorkoutTemplate WorkoutTemplate { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            // Adjust cache duration for development environment
            if (_environment.IsDevelopment())
            {
                HttpContext.Response.Headers["X-Template-Detail-Cache"] = "Development-Extended";
            }

            var workoutTemplate = await _context.WorkoutTemplate
                .Include(t => t.TemplateExercises.OrderBy(e => e.SequenceNum))
                .ThenInclude(e => e.ExerciseType)
                .Include(t => t.TemplateExercises)
                .ThenInclude(e => e.TemplateSets.OrderBy(s => s.SequenceNum))
                .ThenInclude(s => s.Settype)
                .AsNoTracking()
                .AsSplitQuery() // Split into multiple SQL queries for better performance
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == id);

            if (workoutTemplate == null)
            {
                return NotFound();
            }

            WorkoutTemplate = workoutTemplate;
            return Page();
        }

        public async Task<IActionResult> OnPostScheduleWorkoutAsync(int templateId, string name, string description, 
                                                             DateTime startDate, DateTime? endDate,
                                                             string recurrencePattern = "Once",
                                                             List<string> daysOfWeek = null, int? dayOfMonth = null,
                                                             string workoutTime = null, bool sendReminder = true,
                                                             int reminderHoursBefore = 3)
        {
            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Verify the template exists and belongs to the user
            var template = await _context.WorkoutTemplate
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId && 
                                         (t.UserId == user.UserId || t.IsPublic));

            if (template == null)
            {
                TempData["ErrorMessage"] = "Template not found.";
                return RedirectToPage(new { id = templateId });
            }

            try
            {
                // Parse workout time
                TimeSpan workoutTimeOfDay = TimeSpan.Parse(workoutTime ?? "17:00");
                var scheduledDateTime = startDate.Date.Add(workoutTimeOfDay);

                // Create the workout schedule
                var workoutSchedule = new WorkoutSchedule
                {
                    TemplateId = templateId,
                    ClientUserId = user.UserId,
                    CoachUserId = user.UserId, // Self-scheduling, so user is both client and coach
                    Name = name,
                    Description = description,
                    StartDate = startDate,
                    EndDate = endDate,
                    ScheduledDateTime = scheduledDateTime,
                    IsActive = true,
                    SendReminder = sendReminder,
                    ReminderHoursBefore = reminderHoursBefore
                };

                // Handle recurrence pattern
                if (recurrencePattern != "Once")
                {
                    workoutSchedule.IsRecurring = true;
                    workoutSchedule.RecurrencePattern = recurrencePattern;
                    
                    // For weekly recurrence, set the day of week
                    if (recurrencePattern == "Weekly" || recurrencePattern == "BiWeekly")
                    {
                        if (daysOfWeek != null && daysOfWeek.Any())
                        {
                            // Parse the day of week string to the enum and store as int
                            DayOfWeek day = Enum.Parse<DayOfWeek>(daysOfWeek.First());
                            workoutSchedule.RecurrenceDayOfWeek = (int)day;
                        }
                        else
                        {
                            // Use the day of week from the start date
                            workoutSchedule.RecurrenceDayOfWeek = (int)startDate.DayOfWeek;
                        }
                    }
                    // For monthly recurrence, set the day of month
                    else if (recurrencePattern == "Monthly" && dayOfMonth.HasValue)
                    {
                        workoutSchedule.RecurrenceDayOfMonth = dayOfMonth.Value;
                    }
                    else if (recurrencePattern == "Monthly")
                    {
                        workoutSchedule.RecurrenceDayOfMonth = startDate.Day;
                    }
                }

                _context.WorkoutSchedules.Add(workoutSchedule);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Workout scheduled successfully!";
                return RedirectToPage(new { id = templateId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling workout for template {TemplateId} by user {UserId}", templateId, user.UserId);
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return RedirectToPage(new { id = templateId });
            }
        }
    }
}