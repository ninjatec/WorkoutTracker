using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Services;

namespace WorkoutTrackerWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WorkoutScheduleController : ControllerBase
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<WorkoutScheduleController> _logger;
        private readonly UserService _userService;

        public WorkoutScheduleController(WorkoutTrackerWebContext context, 
                                        ILogger<WorkoutScheduleController> logger,
                                        UserService userService)
        {
            _context = context;
            _logger = logger;
            _userService = userService;
        }

        // GET: api/WorkoutSchedule
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WorkoutSchedule>>> GetWorkoutSchedules()
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized();
            }

            return await _context.WorkoutSchedules
                .Where(s => s.ClientUserId == userId && s.IsActive)
                .OrderBy(s => s.ScheduledDateTime)
                .ToListAsync();
        }

        // GET: api/WorkoutSchedule/5
        [HttpGet("{id}")]
        public async Task<ActionResult<WorkoutSchedule>> GetWorkoutSchedule(int id)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized();
            }

            var workoutSchedule = await _context.WorkoutSchedules
                .FirstOrDefaultAsync(s => s.WorkoutScheduleId == id && s.ClientUserId == userId);

            if (workoutSchedule == null)
            {
                return NotFound();
            }

            return workoutSchedule;
        }

        // POST: api/WorkoutSchedule
        [HttpPost]
        public async Task<ActionResult<WorkoutSchedule>> CreateWorkoutSchedule([FromBody] WorkoutScheduleRequest request)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized();
            }

            try
            {
                // Check if this is a self-scheduled workout or a coach-assigned workout
                if (request.TemplateId.HasValue)
                {
                    // Self-scheduled workout (user is scheduling their own workout from their template)
                    return await CreateSelfScheduledWorkout(request, userId.Value);
                }
                else if (request.AssignmentId.HasValue)
                {
                    // Coach-assigned workout (user is scheduling from a template assigned by a coach)
                    return await CreateAssignmentWorkout(request, userId.Value);
                }
                else
                {
                    return BadRequest("Either templateId or assignmentId must be provided");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating workout schedule for user {userId}");
                return BadRequest($"Error creating schedule: {ex.Message}");
            }
        }

        private async Task<ActionResult<WorkoutSchedule>> CreateSelfScheduledWorkout(WorkoutScheduleRequest request, int userId)
        {
            // Validate template exists and belongs to the user
            var template = await _context.WorkoutTemplate
                .FirstOrDefaultAsync(t => t.WorkoutTemplateId == request.TemplateId && 
                                          (t.UserId == userId || t.IsPublic));

            if (template == null)
            {
                return BadRequest("Template not found or you don't have access to it");
            }

            // Parse schedule date and time
            var scheduleDate = DateTime.Parse(request.ScheduleDate);
            var scheduleTime = TimeSpan.Parse(request.ScheduleTime ?? "17:00");
            var scheduledDateTime = scheduleDate.Date.Add(scheduleTime);

            // Create the workout schedule
            var workoutSchedule = new WorkoutSchedule
            {
                ClientUserId = userId,
                CoachUserId = userId, // Self-scheduling, so the user is both client and coach
                Name = request.ScheduleName ?? template.Name,
                Description = request.Description ?? string.Empty,
                StartDate = scheduleDate,
                ScheduledDateTime = scheduledDateTime,
                IsActive = true
            };

            // Set template information
            workoutSchedule.TemplateId = template.WorkoutTemplateId;

            // Handle recurrence pattern
            ConfigureRecurrencePattern(workoutSchedule, request, scheduleDate);

            // Add reminder settings
            workoutSchedule.SendReminder = true;
            workoutSchedule.ReminderHoursBefore = request.ReminderHoursBefore ?? 3;

            _context.WorkoutSchedules.Add(workoutSchedule);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} created self-scheduled workout {workoutSchedule.WorkoutScheduleId}");

            return CreatedAtAction(nameof(GetWorkoutSchedule), new { id = workoutSchedule.WorkoutScheduleId }, workoutSchedule);
        }

        private async Task<ActionResult<WorkoutSchedule>> CreateAssignmentWorkout(WorkoutScheduleRequest request, int userId)
        {
            // Validate template assignment exists and belongs to the user
            var assignment = await _context.TemplateAssignments
                .FirstOrDefaultAsync(a => a.TemplateAssignmentId == request.AssignmentId && 
                                         a.ClientUserId == userId && 
                                         a.IsActive);

            if (assignment == null)
            {
                return BadRequest("Template assignment not found or inactive");
            }

            // Parse schedule date and time
            var scheduleDate = DateTime.Parse(request.ScheduleDate);
            var scheduleTime = TimeSpan.Parse(request.ScheduleTime ?? "17:00");
            var scheduledDateTime = scheduleDate.Date.Add(scheduleTime);

            // Create the workout schedule
            var workoutSchedule = new WorkoutSchedule
            {
                TemplateAssignmentId = request.AssignmentId,
                ClientUserId = userId,
                CoachUserId = assignment.CoachUserId,
                Name = request.ScheduleName ?? assignment.Name,
                Description = request.Description ?? string.Empty,
                StartDate = scheduleDate,
                ScheduledDateTime = scheduledDateTime,
                IsActive = true
            };

            // Handle recurrence pattern
            ConfigureRecurrencePattern(workoutSchedule, request, scheduleDate);

            // Add reminder settings
            workoutSchedule.SendReminder = request.SendReminder ?? true;
            workoutSchedule.ReminderHoursBefore = request.ReminderHoursBefore ?? 3;

            _context.WorkoutSchedules.Add(workoutSchedule);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} created assignment workout schedule {workoutSchedule.WorkoutScheduleId}");

            return CreatedAtAction(nameof(GetWorkoutSchedule), new { id = workoutSchedule.WorkoutScheduleId }, workoutSchedule);
        }

        private void ConfigureRecurrencePattern(WorkoutSchedule workoutSchedule, WorkoutScheduleRequest request, DateTime scheduleDate)
        {
            if (!string.IsNullOrEmpty(request.RecurrenceType) && request.RecurrenceType != "none")
            {
                workoutSchedule.IsRecurring = true;
                workoutSchedule.RecurrencePattern = request.RecurrenceType;
                
                // Set recurrence end date if provided
                if (!string.IsNullOrEmpty(request.RecurrenceEndDate))
                {
                    workoutSchedule.EndDate = DateTime.Parse(request.RecurrenceEndDate);
                }

                // For weekly recurrence, set the day of week
                if (request.RecurrenceType == "weekly" || request.RecurrenceType == "biweekly")
                {
                    workoutSchedule.RecurrenceDayOfWeek = (int)scheduleDate.DayOfWeek;
                }
                // For monthly recurrence, set the day of month
                else if (request.RecurrenceType == "monthly")
                {
                    workoutSchedule.RecurrenceDayOfMonth = scheduleDate.Day;
                }
            }
        }

        // PUT: api/WorkoutSchedule/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWorkoutSchedule(int id, WorkoutSchedule workoutSchedule)
        {
            if (id != workoutSchedule.WorkoutScheduleId)
            {
                return BadRequest();
            }

            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized();
            }

            // Verify the schedule belongs to the user
            var existingSchedule = await _context.WorkoutSchedules
                .FirstOrDefaultAsync(s => s.WorkoutScheduleId == id && s.ClientUserId == userId);

            if (existingSchedule == null)
            {
                return NotFound();
            }

            try
            {
                // Update only allowed fields (protection against overposting)
                existingSchedule.Name = workoutSchedule.Name;
                existingSchedule.Description = workoutSchedule.Description;
                existingSchedule.ScheduledDateTime = workoutSchedule.ScheduledDateTime;
                existingSchedule.StartDate = workoutSchedule.StartDate;
                existingSchedule.EndDate = workoutSchedule.EndDate;
                existingSchedule.IsRecurring = workoutSchedule.IsRecurring;
                existingSchedule.RecurrencePattern = workoutSchedule.RecurrencePattern;
                existingSchedule.RecurrenceDayOfWeek = workoutSchedule.RecurrenceDayOfWeek;
                existingSchedule.RecurrenceDayOfMonth = workoutSchedule.RecurrenceDayOfMonth;
                existingSchedule.SendReminder = workoutSchedule.SendReminder;
                existingSchedule.ReminderHoursBefore = workoutSchedule.ReminderHoursBefore;
                existingSchedule.IsActive = workoutSchedule.IsActive;

                _context.Entry(existingSchedule).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} updated workout schedule {id}");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!WorkoutScheduleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, $"Concurrency error updating workout schedule {id} for user {userId}");
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/WorkoutSchedule/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWorkoutSchedule(int id)
        {
            var userId = await _userService.GetCurrentUserIdAsync();
            if (userId == null)
            {
                return Unauthorized();
            }

            var workoutSchedule = await _context.WorkoutSchedules
                .FirstOrDefaultAsync(s => s.WorkoutScheduleId == id && s.ClientUserId == userId);
                
            if (workoutSchedule == null)
            {
                return NotFound();
            }

            _context.WorkoutSchedules.Remove(workoutSchedule);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User {userId} deleted workout schedule {id}");

            return NoContent();
        }

        private bool WorkoutScheduleExists(int id)
        {
            return _context.WorkoutSchedules.Any(e => e.WorkoutScheduleId == id);
        }
    }

    // View model for workout schedule requests
    public class WorkoutScheduleRequest
    {
        public int? AssignmentId { get; set; }
        public int? TemplateId { get; set; }  // For self-scheduling
        public string ScheduleName { get; set; }
        public string Description { get; set; }
        public string ScheduleDate { get; set; }
        public string ScheduleTime { get; set; }
        public string RecurrenceType { get; set; }
        public string RecurrenceEndDate { get; set; }
        public bool? SendReminder { get; set; }
        public int? ReminderHoursBefore { get; set; }
    }
}