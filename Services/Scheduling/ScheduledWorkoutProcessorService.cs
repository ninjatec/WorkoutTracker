using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;

namespace WorkoutTrackerWeb.Services.Scheduling
{
    /// <summary>
    /// Service responsible for converting scheduled workouts into actual workout sessions
    /// </summary>
    public class ScheduledWorkoutProcessorService
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly ILogger<ScheduledWorkoutProcessorService> _logger;
        private readonly ScheduledWorkoutProcessorOptions _options;
        private readonly TimeZoneInfo _timeZone;

        public ScheduledWorkoutProcessorService(
            WorkoutTrackerWebContext context,
            ILogger<ScheduledWorkoutProcessorService> logger,
            IOptions<ScheduledWorkoutProcessorOptions> options)
        {
            _context = context;
            _logger = logger;
            _options = options.Value;
            
            // Determine the timezone to use (default to UTC if not specified)
            _timeZone = _options.UseLocalTimeZone 
                ? TimeZoneInfo.Local 
                : TimeZoneInfo.Utc;
                
            _logger.LogInformation("ScheduledWorkoutProcessorService initialized with timezone: {TimeZone}", 
                _timeZone.DisplayName);
        }

        /// <summary>
        /// Processes all scheduled workouts that need to be converted to actual workout sessions
        /// </summary>
        /// <returns>Number of workouts created</returns>
        public async Task<int> ProcessScheduledWorkoutsAsync()
        {
            var workoutsCreated = 0;
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
            
            _logger.LogInformation("Starting scheduled workout processing at {Now}", now);
            
            try
            {
                // Get all due workouts
                var dueWorkouts = await GetDueWorkoutsAsync(now);
                
                _logger.LogInformation("Found {Count} due workouts that need processing", dueWorkouts.Count);
                
                // Process each due workout
                foreach (var workout in dueWorkouts)
                {
                    try
                    {
                        _logger.LogDebug("Processing workout {Id}: {Name}", workout.WorkoutScheduleId, workout.Name);
                        
                        // Convert the scheduled workout to an actual session
                        await ProcessScheduledWorkoutAsync(workout);
                        workoutsCreated++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing scheduled workout {Id}", workout.WorkoutScheduleId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessScheduledWorkoutsAsync");
            }
            
            _logger.LogInformation("Completed scheduled workout processing. Created {Count} workouts", workoutsCreated);
            return workoutsCreated;
        }

        /// <summary>
        /// Processes scheduled workouts that were missed (not generated in time)
        /// </summary>
        /// <returns>Number of missed workouts processed</returns>
        public async Task<int> ProcessMissedWorkoutsAsync()
        {
            // Skip if the feature is disabled
            if (!_options.CreateMissedWorkouts)
            {
                _logger.LogInformation("Missed workout processing is disabled. Skipping.");
                return 0;
            }

            var workoutsCreated = 0;
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
            
            _logger.LogInformation("Starting missed workout processing at {Now}", now);
            
            try
            {
                // Calculate date range for missed workouts
                var startDate = now.AddDays(-_options.MaxDaysForMissedWorkouts);
                var endDate = now.AddHours(-_options.MaximumHoursLate); // Don't overlap with regular processing
                
                // Skip if range is invalid
                if (startDate >= endDate)
                {
                    _logger.LogInformation("Invalid date range for missed workouts. Skipping.");
                    return 0;
                }
                
                _logger.LogInformation("Looking for missed workouts between {StartDate} and {EndDate}", 
                    startDate, endDate);
                
                // Get all active workout schedules
                var activeWorkouts = await _context.WorkoutSchedules
                    .Where(s => s.IsActive && s.WorkoutScheduleId > 0)
                    .Include(s => s.Template)
                        .ThenInclude(t => t.TemplateExercises)
                            .ThenInclude(e => e.TemplateSets)
                    .Include(s => s.TemplateAssignment)
                        .ThenInclude(a => a.WorkoutTemplate)
                            .ThenInclude(t => t.TemplateExercises)
                                .ThenInclude(e => e.TemplateSets)
                    .ToListAsync();
                
                // Find missed occurrences for each schedule
                var missedWorkouts = new List<WorkoutSchedule>();
                
                foreach (var workout in activeWorkouts)
                {
                    // Skip workouts without templates
                    var template = workout.Template ?? workout.TemplateAssignment?.WorkoutTemplate;
                    if (template == null)
                    {
                        continue;
                    }
                    
                    await FindMissedRecurringWorkouts(workout, startDate, endDate, missedWorkouts, now);
                }
                
                _logger.LogInformation("Found {Count} missed workouts that need processing", missedWorkouts.Count);
                
                // Process the missed workouts
                foreach (var workout in missedWorkouts)
                {
                    try
                    {
                        _logger.LogDebug("Processing missed workout {Id}: {Name} scheduled for {Date}", 
                            workout.WorkoutScheduleId, workout.Name, workout.ScheduledDateTime);
                        
                        // Special handling for missed workouts
                        if (_options.MarkMissedWorkoutsAsLate)
                        {
                            var result = await ConvertMissedWorkoutToSessionAsync(workout);
                            
                            if (result != null)
                            {
                                workoutsCreated++;
                                _logger.LogInformation("Successfully created MISSED workout session {SessionId} from schedule {ScheduleId} (originally scheduled for {Date})", 
                                    result.WorkoutSessionId, workout.WorkoutScheduleId, workout.ScheduledDateTime);
                            }
                        }
                        else
                        {
                            // Process normally
                            var result = await ConvertScheduledWorkoutToSessionAsync(workout);
                            
                            if (result != null)
                            {
                                workoutsCreated++;
                                _logger.LogInformation("Successfully created workout session {SessionId} from missed schedule {ScheduleId}", 
                                    result.WorkoutSessionId, workout.WorkoutScheduleId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing missed workout {Id}", workout.WorkoutScheduleId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessMissedWorkoutsAsync");
            }
            
            _logger.LogInformation("Completed missed workout processing. Created {Count} workouts", workoutsCreated);
            return workoutsCreated;
        }

        /// <summary>
        /// Find missed occurrences of a recurring workout
        /// </summary>
        private async Task FindMissedRecurringWorkouts(
            WorkoutSchedule workout, 
            DateTime startDate, 
            DateTime endDate, 
            List<WorkoutSchedule> missedWorkouts,
            DateTime referenceDate)
        {
            // Skip non-recurring workouts
            if (!workout.IsRecurring || workout.RecurrencePattern == "Once")
            {
                return;
            }
            
            // Get the workout time of day (default to now if not specified)
            var timeOfDay = workout.ScheduledDateTime?.TimeOfDay ?? TimeSpan.Zero;

            // Calculate all occurrences within range based on recurrence pattern
            var occurrences = new List<DateTime>();
            
            switch (workout.RecurrencePattern)
            {
                case "Weekly":
                    await GetWeeklyOccurrencesInRangeAsync(workout, startDate, endDate, timeOfDay, occurrences);
                    break;
                    
                case "BiWeekly":
                    await GetBiWeeklyOccurrencesInRangeAsync(workout, startDate, endDate, timeOfDay, occurrences);
                    break;
                    
                case "Monthly":
                    await GetMonthlyOccurrencesInRangeAsync(workout, startDate, endDate, timeOfDay, occurrences);
                    break;
                    
                default:
                    _logger.LogWarning("Unknown recurrence pattern for workout {Id}: {Pattern}", 
                        workout.WorkoutScheduleId, workout.RecurrencePattern);
                    return;
            }
            
            // Check which occurrences were not processed
            foreach (var occurrence in occurrences)
            {
                var wasProcessed = await WasWorkoutOccurrenceProcessedAsync(workout, occurrence);
                
                if (!wasProcessed)
                {
                    // Clone the workout with this occurrence date
                    var missedWorkout = CloneWorkoutWithDate(workout, occurrence);
                    
                    // Skip if this occurrence is in the future
                    if (occurrence > referenceDate)
                    {
                        continue;
                    }
                    
                    missedWorkouts.Add(missedWorkout);
                    
                    _logger.LogInformation("Found missed occurrence of workout {Id} scheduled for {Date}", 
                        workout.WorkoutScheduleId, occurrence);
                }
            }
        }

        /// <summary>
        /// Get all weekly occurrences that fall within the date range
        /// </summary>
        private async Task GetWeeklyOccurrencesInRangeAsync(
            WorkoutSchedule workout, 
            DateTime startDate, 
            DateTime endDate, 
            TimeSpan timeOfDay, 
            List<DateTime> occurrences)
        {
            // Get all days of week this workout occurs on
            var daysOfWeek = GetWorkoutDaysOfWeek(workout);
            
            if (!daysOfWeek.Any())
            {
                _logger.LogWarning("Weekly workout {Id} has no days of week specified", workout.WorkoutScheduleId);
                return;
            }
            
            // Start at the beginning of the range and go forward by days
            var currentDate = startDate.Date;
            var workoutStartDate = workout.StartDate.Date;
            
            // Don't look before the workout start date
            if (currentDate < workoutStartDate)
            {
                currentDate = workoutStartDate;
            }
            
            // Loop through each day in the range
            while (currentDate <= endDate.Date)
            {
                // Check if this day matches one of our days of week
                if (daysOfWeek.Contains(currentDate.DayOfWeek))
                {
                    var occurrence = currentDate.Add(timeOfDay);
                    
                    // Skip if occurrence is before start date (could happen with time of day)
                    if (occurrence < startDate)
                    {
                        currentDate = currentDate.AddDays(1);
                        continue;
                    }
                    
                    // Check end date of workout if it has one
                    if (workout.EndDate.HasValue && occurrence.Date > workout.EndDate.Value.Date)
                    {
                        break;
                    }
                    
                    occurrences.Add(occurrence);
                }
                
                currentDate = currentDate.AddDays(1);
            }
        }
        
        /// <summary>
        /// Get all bi-weekly occurrences that fall within the date range
        /// </summary>
        private async Task GetBiWeeklyOccurrencesInRangeAsync(
            WorkoutSchedule workout, 
            DateTime startDate, 
            DateTime endDate, 
            TimeSpan timeOfDay, 
            List<DateTime> occurrences)
        {
            // Get all days of week this workout occurs on
            var daysOfWeek = GetWorkoutDaysOfWeek(workout);
            
            if (!daysOfWeek.Any())
            {
                _logger.LogWarning("BiWeekly workout {Id} has no days of week specified", workout.WorkoutScheduleId);
                return;
            }
            
            // Start at the beginning of the range and go forward by days
            var currentDate = startDate.Date;
            var workoutStartDate = workout.StartDate.Date;
            
            // Don't look before the workout start date
            if (currentDate < workoutStartDate)
            {
                currentDate = workoutStartDate;
            }
            
            // Loop through each day in the range
            while (currentDate <= endDate.Date)
            {
                // Check if this day matches one of our days of week
                if (daysOfWeek.Contains(currentDate.DayOfWeek))
                {
                    // Calculate weeks since the start date to determine if this is a bi-weekly occurrence
                    var weeksSinceStart = (int)Math.Round((currentDate - workoutStartDate).TotalDays / 7.0);
                    
                    // Check if this is a bi-weekly occurrence (every two weeks)
                    if (weeksSinceStart % 2 == 0)
                    {
                        var occurrence = currentDate.Add(timeOfDay);
                        
                        // Skip if occurrence is before start date (could happen with time of day)
                        if (occurrence < startDate)
                        {
                            currentDate = currentDate.AddDays(1);
                            continue;
                        }
                        
                        // Check end date of workout if it has one
                        if (workout.EndDate.HasValue && occurrence.Date > workout.EndDate.Value.Date)
                        {
                            break;
                        }
                        
                        occurrences.Add(occurrence);
                    }
                }
                
                currentDate = currentDate.AddDays(1);
            }
        }
        
        /// <summary>
        /// Get all monthly occurrences that fall within the date range
        /// </summary>
        private async Task GetMonthlyOccurrencesInRangeAsync(
            WorkoutSchedule workout, 
            DateTime startDate, 
            DateTime endDate, 
            TimeSpan timeOfDay, 
            List<DateTime> occurrences)
        {
            // Get the day of month this workout occurs on
            var dayOfMonth = workout.RecurrenceDayOfMonth ?? workout.StartDate.Day;
            
            // Start at the beginning of the range
            var currentDate = startDate.Date;
            var workoutStartDate = workout.StartDate.Date;
            
            // Don't look before the workout start date
            if (currentDate < workoutStartDate)
            {
                currentDate = workoutStartDate;
            }
            
            // Move to the first of the month
            var currentMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            
            // Loop through each month in the range
            while (currentMonth <= endDate.Date)
            {
                try
                {
                    // Handle months with fewer days
                    var daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
                    var actualDayOfMonth = Math.Min(dayOfMonth, daysInMonth);
                    
                    var occurrenceDate = new DateTime(currentMonth.Year, currentMonth.Month, actualDayOfMonth);
                    
                    // Skip if occurrence is before our start date or start date of workout
                    if (occurrenceDate >= currentDate && occurrenceDate <= endDate.Date)
                    {
                        // Check end date of workout if it has one
                        if (workout.EndDate.HasValue && occurrenceDate > workout.EndDate.Value.Date)
                        {
                            break;
                        }
                        
                        var occurrence = occurrenceDate.Add(timeOfDay);
                        occurrences.Add(occurrence);
                    }
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    _logger.LogError(ex, "Error calculating monthly occurrence for workout {Id} in month {Month}", 
                        workout.WorkoutScheduleId, currentMonth);
                }
                
                // Move to next month
                currentMonth = currentMonth.AddMonths(1);
            }
        }

        /// <summary>
        /// Check if a specific occurrence of a workout was already processed
        /// </summary>
        private async Task<bool> WasWorkoutOccurrenceProcessedAsync(WorkoutSchedule workout, DateTime occurrence)
        {
            // Check if the specific day already has a workout session from this schedule
            var date = occurrence.Date;
            
            return await _context.WorkoutSessions
                .AnyAsync(s => 
                    s.WorkoutTemplateId == workout.TemplateId && 
                    s.UserId == workout.ClientUserId && 
                    s.StartDateTime.Date == date);
        }

        /// <summary>
        /// Converts a missed workout to an actual workout session with appropriate status
        /// </summary>
        private async Task<WorkoutSession> ConvertMissedWorkoutToSessionAsync(WorkoutSchedule workout)
        {
            var template = workout.Template ?? workout.TemplateAssignment?.WorkoutTemplate;
            if (template == null)
            {
                _logger.LogError("Cannot convert missed workout {Id} without a template", workout.WorkoutScheduleId);
                return null;
            }

            var workoutSession = new WorkoutSession
            {
                Name = workout.Name,
                Description = workout.Description,
                UserId = workout.ClientUserId,
                StartDateTime = workout.ScheduledDateTime ?? DateTime.UtcNow,
                Status = "Missed", // Special status for missed workouts
                IsFromCoach = true,
                WorkoutTemplateId = workout.TemplateId,
                TemplateAssignmentId = workout.TemplateAssignmentId
            };

            _context.WorkoutSessions.Add(workoutSession);
            await _context.SaveChangesAsync();

            // Create exercises and sets from template
            foreach (var templateExercise in template.TemplateExercises.OrderBy(e => e.OrderIndex))
            {
                var workoutExercise = new WorkoutExercise
                {
                    WorkoutSessionId = workoutSession.WorkoutSessionId,
                    ExerciseTypeId = templateExercise.ExerciseTypeId,
                    SequenceNum = templateExercise.OrderIndex,
                    OrderIndex = templateExercise.OrderIndex,
                    Notes = templateExercise.Notes
                };

                _context.WorkoutExercises.Add(workoutExercise);
                await _context.SaveChangesAsync();

                foreach (var templateSet in templateExercise.TemplateSets.OrderBy(s => s.SequenceNum))
                {
                    var workoutSet = new WorkoutSet
                    {
                        WorkoutExerciseId = workoutExercise.WorkoutExerciseId,
                        SettypeId = templateSet.SettypeId,
                        SequenceNum = templateSet.SequenceNum,
                        SetNumber = templateSet.SequenceNum,
                        Reps = templateSet.DefaultReps,
                        Weight = templateSet.DefaultWeight,
                        Notes = templateSet.Notes,
                        IsCompleted = false  // Mark as not completed since it was missed
                    };

                    _context.WorkoutSets.Add(workoutSet);
                }
            }
            await _context.SaveChangesAsync();

            // Update the workout schedule status
            if (workout.WorkoutScheduleId > 0) // Only update if this is a real schedule
            {
                // Find the actual workout schedule in the database and update its status
                var scheduleToUpdate = await _context.WorkoutSchedules.FindAsync(workout.WorkoutScheduleId);
                if (scheduleToUpdate != null)
                {
                    scheduleToUpdate.LastGenerationStatus = "Processed";
                    scheduleToUpdate.LastGeneratedWorkoutDate = DateTime.UtcNow;
                    scheduleToUpdate.LastGeneratedSessionId = workoutSession.WorkoutSessionId;
                    await _context.SaveChangesAsync();
                }
            }

            return workoutSession;
        }

        /// <summary>
        /// Cleans up expired scheduled workouts that are no longer needed
        /// </summary>
        public async Task<int> CleanupExpiredWorkoutsAsync()
        {
            var now = DateTime.UtcNow;
            var threshold = now.AddDays(-30); // Keep 30 days history
            
            var expiredWorkouts = await _context.WorkoutSchedules
                .Where(s => !s.IsRecurring && s.LastGenerationStatus == "Processed" && 
                            s.LastGeneratedWorkoutDate != null && s.LastGeneratedWorkoutDate < threshold)
                .ToListAsync();
                
            var count = expiredWorkouts.Count;
            if (count > 0)
            {
                _logger.LogInformation("Cleaning up {Count} expired workout schedules", count);
                _context.WorkoutSchedules.RemoveRange(expiredWorkouts);
                await _context.SaveChangesAsync();
            }
            
            return count;
        }

        /// <summary>
        /// Calculates the next occurrence for a recurring workout
        /// </summary>
        private DateTime? CalculateNextOccurrence(WorkoutSchedule workout, DateTime referenceDate)
        {
            // Get the workout time of day (default to now if not specified)
            var timeOfDay = workout.ScheduledDateTime?.TimeOfDay ?? TimeSpan.Zero;
            
            switch (workout.RecurrencePattern)
            {
                case "Weekly":
                    return CalculateNextWeeklyOccurrence(workout, referenceDate, timeOfDay);
                    
                case "BiWeekly":
                    return CalculateNextBiWeeklyOccurrence(workout, referenceDate, timeOfDay);
                    
                case "Monthly":
                    return CalculateNextMonthlyOccurrence(workout, referenceDate, timeOfDay);
                    
                default:
                    _logger.LogWarning("Unknown recurrence pattern for workout {Id}: {Pattern}", 
                        workout.WorkoutScheduleId, workout.RecurrencePattern);
                    return null;
            }
        }

        /// <summary>
        /// Gets all scheduled workouts that are due for conversion to actual workout sessions
        /// </summary>
        private async Task<List<WorkoutSchedule>> GetDueWorkoutsAsync(DateTime now)
        {
            var dueWorkouts = new List<WorkoutSchedule>();
            
            // Calculate the date range for due workouts
            var startDate = now; // Current time
            var endDate = now.AddHours(_options.HoursAdvanceCreation); // Look ahead by configured hours
            
            _logger.LogInformation("Looking for due workouts between {StartDate} and {EndDate}", 
                startDate, endDate);
            
            // Get all active workout schedules
            var activeWorkouts = await _context.WorkoutSchedules
                .Where(s => s.IsActive && s.WorkoutScheduleId > 0)
                .Include(s => s.Template)
                    .ThenInclude(t => t.TemplateExercises)
                        .ThenInclude(e => e.TemplateSets)
                .Include(s => s.TemplateAssignment)
                    .ThenInclude(a => a.WorkoutTemplate)
                        .ThenInclude(t => t.TemplateExercises)
                            .ThenInclude(e => e.TemplateSets)
                .ToListAsync();
            
            foreach (var workout in activeWorkouts)
            {
                // Skip workouts without templates
                var template = workout.Template ?? workout.TemplateAssignment?.WorkoutTemplate;
                if (template == null)
                {
                    _logger.LogWarning("Skipping workout {Id} without a template", workout.WorkoutScheduleId);
                    continue;
                }
                
                // Handle one-time workouts
                if (!workout.IsRecurring || workout.RecurrencePattern == "Once")
                {
                    if (workout.ScheduledDateTime.HasValue &&
                        workout.ScheduledDateTime >= startDate &&
                        workout.ScheduledDateTime <= endDate)
                    {
                        dueWorkouts.Add(workout);
                    }
                    continue;
                }
                
                // Handle recurring workouts
                var nextOccurrence = CalculateNextOccurrence(workout, now);
                
                if (nextOccurrence != null &&
                    nextOccurrence >= startDate && 
                    nextOccurrence <= endDate)
                {
                    // Clone the workout with the calculated occurrence date
                    var workoutClone = CloneWorkoutWithDate(workout, nextOccurrence.Value);
                    dueWorkouts.Add(workoutClone);
                }
            }
            
            return dueWorkouts;
        }

        /// <summary>
        /// Calculates the next occurrence for a weekly recurring workout
        /// </summary>
        private DateTime? CalculateNextWeeklyOccurrence(WorkoutSchedule workout, DateTime referenceDate, TimeSpan timeOfDay)
        {
            // Get all days of week this workout occurs on
            var daysOfWeek = GetWorkoutDaysOfWeek(workout);
            
            if (!daysOfWeek.Any())
            {
                _logger.LogWarning("Weekly workout {Id} has no days of week specified", workout.WorkoutScheduleId);
                return null;
            }
            
            // Sort by day of week to ensure we find the earliest next occurrence
            var orderedDaysOfWeek = daysOfWeek.OrderBy(d => (((int)d - (int)referenceDate.DayOfWeek) + 7) % 7).ToList();
            
            DateTime? result = null;
            
            // First check if today is one of our days and the time hasn't passed yet
            if (daysOfWeek.Contains(referenceDate.DayOfWeek) && timeOfDay > referenceDate.TimeOfDay)
            {
                // Today is a match and the time hasn't passed
                result = referenceDate.Date.Add(timeOfDay);
            }
            else
            {
                // Find the next matching day of week
                var date = referenceDate.Date.AddDays(1); // Start with tomorrow
                while (!daysOfWeek.Contains(date.DayOfWeek))
                {
                    date = date.AddDays(1);
                }
                result = date.Add(timeOfDay);
            }
            
            // Final check: make sure we respect end date
            if (result != null && workout.EndDate.HasValue && result.Value.Date > workout.EndDate.Value.Date)
            {
                _logger.LogInformation("Weekly workout {Id} next occurrence would be {Date}, but that's after end date {EndDate}", 
                    workout.WorkoutScheduleId, result.Value, workout.EndDate.Value);
                return null;
            }
            
            return result;
        }

        /// <summary>
        /// Calculates the next occurrence for a bi-weekly recurring workout
        /// </summary>
        private DateTime? CalculateNextBiWeeklyOccurrence(WorkoutSchedule workout, DateTime referenceDate, TimeSpan timeOfDay)
        {
            // Get all days of week this workout occurs on
            var daysOfWeek = GetWorkoutDaysOfWeek(workout);
            
            if (!daysOfWeek.Any())
            {
                _logger.LogWarning("BiWeekly workout {Id} has no days of week specified", workout.WorkoutScheduleId);
                return null;
            }
            
            // Get the start date as the reference for the bi-weekly pattern
            var startDate = workout.StartDate;
            
            // Sort days by closest upcoming day of week
            var orderedDaysOfWeek = daysOfWeek.OrderBy(d => (((int)d - (int)referenceDate.DayOfWeek) + 7) % 7).ToList();
            
            DateTime? result = null;
            
            // First check if today is one of our days, the time hasn't passed yet, and it's a bi-weekly occurrence
            if (daysOfWeek.Contains(referenceDate.DayOfWeek))
            {
                // Calculate weeks since the start date to determine if this is a bi-weekly occurrence
                var weeksSinceStart = (int)Math.Round((referenceDate.Date - startDate.Date).TotalDays / 7.0);
                
                // Check if this is a bi-weekly occurrence (every two weeks)
                if (weeksSinceStart % 2 == 0)
                {
                    // For today, we need to check if the time is still in the future
                    if (timeOfDay > referenceDate.TimeOfDay)
                    {
                        result = referenceDate.Date.Add(timeOfDay);
                    }
                }
            }
            
            // If today wasn't a match, find the next matching day
            if (result == null)
            {
                // Calculate current weeks since start
                var currentWeeksSinceStart = (int)Math.Floor((referenceDate.Date - startDate.Date).TotalDays / 7.0);
                // Ensure we get the next even-number of weeks from the start date
                var weeksToAdd = currentWeeksSinceStart % 2 == 0 ? 2 : 1;
                var nextCycleStart = startDate.Date.AddDays((currentWeeksSinceStart + weeksToAdd) * 7);
                
                // Find the first matching day of week in the next cycle
                var date = nextCycleStart;
                for (int i = 0; i < 7; i++)
                {
                    if (daysOfWeek.Contains(date.DayOfWeek))
                    {
                        result = date.Add(timeOfDay);
                        break;
                    }
                    date = date.AddDays(1);
                }
            }
            
            // Final check: make sure we respect end date
            if (result != null && workout.EndDate.HasValue && result.Value.Date > workout.EndDate.Value.Date)
            {
                _logger.LogInformation("BiWeekly workout {Id} next occurrence would be {Date}, but that's after end date {EndDate}", 
                    workout.WorkoutScheduleId, result.Value, workout.EndDate.Value);
                return null;
            }
            
            return result;
        }

        /// <summary>
        /// Calculates the next occurrence for a monthly recurring workout
        /// </summary>
        private DateTime? CalculateNextMonthlyOccurrence(WorkoutSchedule workout, DateTime referenceDate, TimeSpan timeOfDay)
        {
            // Get the day of month this workout occurs on (use start date day if not specified)
            var dayOfMonth = workout.RecurrenceDayOfMonth ?? workout.StartDate.Day;
            
            DateTime? result = null;
            
            // Check if the specified day in the current month is still in the future
            var currentMonth = new DateTime(referenceDate.Year, referenceDate.Month, 1);
            
            if (dayOfMonth >= referenceDate.Day)
            {
                // The day in this month is still coming up
                try
                {
                    // Handle special cases like February 29th in non-leap years
                    var daysInMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
                    int actualDay = Math.Min(dayOfMonth, daysInMonth);
                    result = new DateTime(currentMonth.Year, currentMonth.Month, actualDay, 
                        timeOfDay.Hours, timeOfDay.Minutes, timeOfDay.Seconds);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Handle any remaining edge cases by taking the last day of the month
                    var lastDayOfMonth = DateTime.DaysInMonth(currentMonth.Year, currentMonth.Month);
                    result = new DateTime(currentMonth.Year, currentMonth.Month, lastDayOfMonth,
                        timeOfDay.Hours, timeOfDay.Minutes, timeOfDay.Seconds);
                    _logger.LogWarning("Monthly workout {Id} requested invalid day {RequestedDay}, using last day of month ({ActualDay})", 
                        workout.WorkoutScheduleId, dayOfMonth, lastDayOfMonth);
                }
            }
            else
            {
                // Move to next month
                var nextMonth = currentMonth.AddMonths(1);
                try
                {
                    var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                    int actualDay = Math.Min(dayOfMonth, daysInMonth);
                    result = new DateTime(nextMonth.Year, nextMonth.Month, actualDay,
                        timeOfDay.Hours, timeOfDay.Minutes, timeOfDay.Seconds);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Handle edge cases with last day of the month
                    var lastDayOfMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                    result = new DateTime(nextMonth.Year, nextMonth.Month, lastDayOfMonth,
                        timeOfDay.Hours, timeOfDay.Minutes, timeOfDay.Seconds);
                    _logger.LogWarning("Monthly workout {Id} requested invalid day {RequestedDay}, using last day of month ({ActualDay})", 
                        workout.WorkoutScheduleId, dayOfMonth, lastDayOfMonth);
                }
            }
            
            // Final check: make sure we respect end date
            if (result != null && workout.EndDate.HasValue && result.Value.Date > workout.EndDate.Value.Date)
            {
                _logger.LogInformation("Monthly workout {Id} next occurrence would be {Date}, but that's after end date {EndDate}", 
                    workout.WorkoutScheduleId, result.Value, workout.EndDate.Value);
                return null;
            }
            
            return result;
        }

        /// <summary>
        /// Gets all days of the week a workout should occur on
        /// </summary>
        private List<DayOfWeek> GetWorkoutDaysOfWeek(WorkoutSchedule workout)
        {
            var result = new List<DayOfWeek>();
            
            // Add the primary day of week if present
            if (workout.RecurrenceDayOfWeek.HasValue)
            {
                // Validate the primary day value is within range
                if (Enum.IsDefined(typeof(DayOfWeek), workout.RecurrenceDayOfWeek.Value))
                {
                    result.Add((DayOfWeek)workout.RecurrenceDayOfWeek.Value);
                }
                else
                {
                    _logger.LogWarning("Workout {Id} has invalid RecurrenceDayOfWeek value: {Value}", 
                        workout.WorkoutScheduleId, workout.RecurrenceDayOfWeek.Value);
                }
            }
            
            // Add additional days if any
            if (!string.IsNullOrEmpty(workout.MultipleDaysOfWeek))
            {
                foreach (var dayValue in workout.MultipleDaysOfWeek.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(dayValue, out int dayInt))
                    {
                        // Validate the day value is within range
                        if (Enum.IsDefined(typeof(DayOfWeek), dayInt))
                        {
                            var dayOfWeek = (DayOfWeek)dayInt;
                            if (!result.Contains(dayOfWeek))
                            {
                                result.Add(dayOfWeek);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Workout {Id} has invalid day of week value in MultipleDaysOfWeek: {Value}", 
                                workout.WorkoutScheduleId, dayInt);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Workout {Id} has non-integer value in MultipleDaysOfWeek: {Value}", 
                            workout.WorkoutScheduleId, dayValue);
                    }
                }
            }
            
            // If no valid days found but it's a weekly or bi-weekly pattern, use the start date's day of week as fallback
            if (!result.Any() && (workout.RecurrencePattern == "Weekly" || workout.RecurrencePattern == "BiWeekly"))
            {
                var startDateDayOfWeek = workout.StartDate.DayOfWeek;
                result.Add(startDateDayOfWeek);
                _logger.LogInformation("Workout {Id} had no valid days of week, using start date's day of week ({DayOfWeek}) as fallback", 
                    workout.WorkoutScheduleId, startDateDayOfWeek);
            }
            
            return result;
        }

        /// <summary>
        /// Creates a copy of a workout with a specific scheduled date
        /// </summary>
        private WorkoutSchedule CloneWorkoutWithDate(WorkoutSchedule workout, DateTime scheduledDateTime)
        {
            // Create a shallow copy
            var clone = new WorkoutSchedule
            {
                WorkoutScheduleId = workout.WorkoutScheduleId,
                TemplateId = workout.TemplateId,
                TemplateAssignmentId = workout.TemplateAssignmentId,
                ClientUserId = workout.ClientUserId,
                CoachUserId = workout.CoachUserId,
                Name = workout.Name,
                Description = workout.Description,
                StartDate = workout.StartDate,
                EndDate = workout.EndDate,
                ScheduledDateTime = scheduledDateTime,
                IsRecurring = workout.IsRecurring,
                RecurrencePattern = workout.RecurrencePattern,
                RecurrenceDayOfWeek = workout.RecurrenceDayOfWeek,
                RecurrenceDayOfMonth = workout.RecurrenceDayOfMonth,
                MultipleDaysOfWeek = workout.MultipleDaysOfWeek,
                SendReminder = workout.SendReminder,
                ReminderHoursBefore = workout.ReminderHoursBefore,
                IsActive = workout.IsActive,
                Template = workout.Template,
                TemplateAssignment = workout.TemplateAssignment
            };
            
            return clone;
        }

        /// <summary>
        /// Converts a scheduled workout to an actual workout session
        /// </summary>
        private async Task<WorkoutSession> ConvertScheduledWorkoutToSessionAsync(WorkoutSchedule workout)
        {
            var template = workout.Template ?? workout.TemplateAssignment?.WorkoutTemplate;
            if (template == null)
            {
                _logger.LogError("Cannot convert scheduled workout {Id} without a template", workout.WorkoutScheduleId);
                return null;
            }

            var workoutSession = new WorkoutSession
            {
                Name = workout.Name,
                Description = workout.Description,
                UserId = workout.ClientUserId,
                StartDateTime = workout.ScheduledDateTime ?? DateTime.UtcNow,
                Status = "Scheduled",
                IsFromCoach = true,
                WorkoutTemplateId = workout.TemplateId,
                TemplateAssignmentId = workout.TemplateAssignmentId
            };

            _context.WorkoutSessions.Add(workoutSession);
            await _context.SaveChangesAsync();

            // Create exercises and sets from template
            foreach (var templateExercise in template.TemplateExercises.OrderBy(e => e.OrderIndex))
            {
                var workoutExercise = new WorkoutExercise
                {
                    WorkoutSessionId = workoutSession.WorkoutSessionId,
                    ExerciseTypeId = templateExercise.ExerciseTypeId,
                    SequenceNum = templateExercise.OrderIndex,
                    OrderIndex = templateExercise.OrderIndex,
                    Notes = templateExercise.Notes
                };

                _context.WorkoutExercises.Add(workoutExercise);
                await _context.SaveChangesAsync();

                foreach (var templateSet in templateExercise.TemplateSets.OrderBy(s => s.SequenceNum))
                {
                    var workoutSet = new WorkoutSet
                    {
                        WorkoutExerciseId = workoutExercise.WorkoutExerciseId,
                        SettypeId = templateSet.SettypeId,
                        SequenceNum = templateSet.SequenceNum,
                        SetNumber = templateSet.SequenceNum,
                        Reps = templateSet.DefaultReps,
                        Weight = templateSet.DefaultWeight,
                        Notes = templateSet.Notes
                    };

                    _context.WorkoutSets.Add(workoutSet);
                }
            }
            await _context.SaveChangesAsync();

            // Update the workout schedule status
            if (workout.WorkoutScheduleId > 0) // Only update if this is a real schedule
            {
                // Find the actual workout schedule in the database and update its status
                var scheduleToUpdate = await _context.WorkoutSchedules.FindAsync(workout.WorkoutScheduleId);
                if (scheduleToUpdate != null)
                {
                    scheduleToUpdate.LastGenerationStatus = "Processed";
                    scheduleToUpdate.LastGeneratedWorkoutDate = DateTime.UtcNow;
                    scheduleToUpdate.LastGeneratedSessionId = workoutSession.WorkoutSessionId;
                    await _context.SaveChangesAsync();
                }
            }

            return workoutSession;
        }

        /// <summary>
        /// Processes a single scheduled workout
        /// </summary>
        private async Task ProcessScheduledWorkoutAsync(WorkoutSchedule schedule)
        {
            if (schedule == null || schedule.Template == null)
            {
                _logger.LogError("Invalid schedule or missing template");
                return;
            }

            try
            {
                // Create new workout session from template
                var workoutSession = new WorkoutSession
                {
                    UserId = schedule.ClientUserId,
                    Name = schedule.Template.Name,
                    Description = schedule.Template.Description,
                    StartDateTime = schedule.ScheduledDateTime ?? DateTime.UtcNow,
                    Status = "Scheduled",
                    IsFromCoach = true,
                    WorkoutTemplateId = schedule.TemplateId
                };

                _context.WorkoutSessions.Add(workoutSession);
                await _context.SaveChangesAsync();

                // Copy exercises from template
                foreach (var templateExercise in schedule.Template.TemplateExercises.OrderBy(x => x.OrderIndex))
                {
                    var workoutExercise = new WorkoutExercise
                    {
                        WorkoutSessionId = workoutSession.WorkoutSessionId,
                        ExerciseTypeId = templateExercise.ExerciseTypeId,
                        SequenceNum = templateExercise.OrderIndex,
                        OrderIndex = templateExercise.OrderIndex,
                        Notes = templateExercise.Notes
                    };

                    _context.WorkoutExercises.Add(workoutExercise);
                    await _context.SaveChangesAsync();

                    // Copy sets from template
                    foreach (var templateSet in templateExercise.TemplateSets.OrderBy(s => s.SequenceNum))
                    {
                        var workoutSet = new WorkoutSet
                        {
                            WorkoutExerciseId = workoutExercise.WorkoutExerciseId,
                            SettypeId = templateSet.SettypeId,
                            SequenceNum = templateSet.SequenceNum,
                            SetNumber = templateSet.SequenceNum,
                            Reps = templateSet.DefaultReps,
                            Weight = templateSet.DefaultWeight,
                            Notes = templateSet.Notes
                        };

                        _context.WorkoutSets.Add(workoutSet);
                    }
                }
                await _context.SaveChangesAsync();
                
                // Update schedule status
                if (schedule.WorkoutScheduleId > 0) // Only update if this is a real schedule
                {
                    // Find the actual workout schedule in the database and update its status
                    var scheduleToUpdate = await _context.WorkoutSchedules.FindAsync(schedule.WorkoutScheduleId);
                    if (scheduleToUpdate != null)
                    {
                        scheduleToUpdate.LastGenerationStatus = "Processed";
                        scheduleToUpdate.LastGeneratedWorkoutDate = DateTime.UtcNow;
                        scheduleToUpdate.LastGeneratedSessionId = workoutSession.WorkoutSessionId;
                    }
                }

                await _context.SaveChangesAsync();
                
                _logger.LogInformation(
                    "Successfully processed scheduled workout {ScheduleId} for user {UserId}", 
                    schedule.WorkoutScheduleId, 
                    schedule.ClientUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error processing scheduled workout {ScheduleId} for user {UserId}", 
                    schedule.WorkoutScheduleId, 
                    schedule.ClientUserId);
                throw;
            }
        }
    }

    /// <summary>
    /// Configuration options for the ScheduledWorkoutProcessorService
    /// </summary>
    public class ScheduledWorkoutProcessorOptions
    {
        /// <summary>
        /// Number of hours in advance to create workouts (default: 24)
        /// </summary>
        public int HoursAdvanceCreation { get; set; } = 24;
        
        /// <summary>
        /// Maximum number of hours a workout can be late to still be created (default: 1)
        /// This helps handle missed processing cycles
        /// </summary>
        public int MaximumHoursLate { get; set; } = 1;
        
        /// <summary>
        /// Whether to use the local time zone or UTC (default: true)
        /// </summary>
        public bool UseLocalTimeZone { get; set; } = true;

        /// <summary>
        /// Whether to create workouts that were missed beyond the MaximumHoursLate window (default: false)
        /// </summary>
        public bool CreateMissedWorkouts { get; set; } = false;

        /// <summary>
        /// Maximum number of days to look back for missed workouts (default: 7)
        /// Only applies if CreateMissedWorkouts is true
        /// </summary>
        public int MaxDaysForMissedWorkouts { get; set; } = 7;

        /// <summary>
        /// Whether to mark missed workouts with a special status (default: true)
        /// </summary>
        public bool MarkMissedWorkoutsAsLate { get; set; } = true;
    }
}