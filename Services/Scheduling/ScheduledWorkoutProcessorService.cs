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
                // Get active scheduled workouts
                var dueWorkouts = await GetDueWorkoutsAsync(now);
                
                _logger.LogInformation("Found {Count} workouts due for processing", dueWorkouts.Count);
                
                foreach (var workout in dueWorkouts)
                {
                    try
                    {
                        _logger.LogDebug("Processing workout {Id}: {Name}", workout.WorkoutScheduleId, workout.Name);
                        
                        // Convert the scheduled workout to an actual session
                        var result = await ConvertScheduledWorkoutToSessionAsync(workout);
                        
                        if (result != null)
                        {
                            workoutsCreated++;
                            _logger.LogInformation("Successfully created workout session {SessionId} from schedule {ScheduleId}", 
                                result.SessionId, workout.WorkoutScheduleId);
                        }
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
        /// Cleans up expired scheduled workouts that are no longer needed
        /// </summary>
        /// <returns>Number of workouts cleaned up</returns>
        public async Task<int> CleanupExpiredWorkoutsAsync()
        {
            var cleanedUpCount = 0;
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
            
            _logger.LogInformation("Starting expired workout cleanup at {Now}", now);
            
            try
            {
                // Find completed one-time workouts (not recurring and in the past)
                var expiredOneTimeWorkouts = await _context.WorkoutSchedules
                    .Where(s => 
                        (!s.IsRecurring || s.RecurrencePattern == "Once") && 
                        s.ScheduledDateTime < now.AddDays(-1) && // At least 1 day old
                        s.IsActive) // Still marked as active
                    .ToListAsync();
                    
                // Find recurring workouts that have passed their end date
                var expiredRecurringWorkouts = await _context.WorkoutSchedules
                    .Where(s => 
                        s.IsRecurring && 
                        s.RecurrencePattern != "Once" && 
                        s.EndDate.HasValue && 
                        s.EndDate.Value < now.Date.AddDays(-1) && // End date at least 1 day in the past
                        s.IsActive) // Still marked as active
                    .ToListAsync();
                    
                _logger.LogInformation("Found {OneTimeCount} expired one-time workouts and {RecurringCount} expired recurring workouts",
                    expiredOneTimeWorkouts.Count, expiredRecurringWorkouts.Count);
                    
                // Mark all expired workouts as inactive
                foreach (var workout in expiredOneTimeWorkouts.Concat(expiredRecurringWorkouts))
                {
                    workout.IsActive = false;
                    _logger.LogDebug("Marking workout {Id} '{Name}' as inactive", workout.WorkoutScheduleId, workout.Name);
                    cleanedUpCount++;
                }
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully marked {Count} expired workouts as inactive", cleanedUpCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired workouts");
                throw; // Rethrow to let Hangfire know this job failed
            }
            
            return cleanedUpCount;
        }

        /// <summary>
        /// Gets all scheduled workouts that are due for conversion to actual workout sessions
        /// </summary>
        private async Task<List<WorkoutSchedule>> GetDueWorkoutsAsync(DateTime now)
        {
            // Calculate the date range based on advance creation setting
            var endDate = now.AddHours(_options.HoursAdvanceCreation);
            var startDate = now.AddHours(-_options.MaximumHoursLate);
            
            _logger.LogDebug("Looking for workouts between {StartDate} and {EndDate}", startDate, endDate);

            var dueWorkouts = new List<WorkoutSchedule>();
            
            // Get all active scheduled workouts
            var activeWorkouts = await _context.WorkoutSchedules
                .Where(s => s.IsActive)
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
                
                if (nextOccurrence.HasValue &&
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
        /// Calculates the next occurrence of a recurring workout
        /// </summary>
        private DateTime? CalculateNextOccurrence(WorkoutSchedule workout, DateTime referenceDate)
        {
            // Check if the workout has an end date and if it's already past
            if (workout.EndDate.HasValue && workout.EndDate.Value < referenceDate.Date)
            {
                return null; // Workout has ended
            }

            // Get the time of day from the original schedule
            var timeOfDay = workout.ScheduledDateTime?.TimeOfDay ?? new TimeSpan(17, 0, 0); // Default to 5 PM
            
            // Calculate the next occurrence based on recurrence pattern
            switch (workout.RecurrencePattern)
            {
                case "Weekly":
                    return CalculateNextWeeklyOccurrence(workout, referenceDate, timeOfDay);
                    
                case "BiWeekly":
                    return CalculateNextBiWeeklyOccurrence(workout, referenceDate, timeOfDay);
                    
                case "Monthly":
                    return CalculateNextMonthlyOccurrence(workout, referenceDate, timeOfDay);
                    
                default:
                    _logger.LogWarning("Unknown recurrence pattern: {Pattern}", workout.RecurrencePattern);
                    return null;
            }
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
            
            // Find the next occurrence by checking days in order starting from today
            var date = referenceDate.Date;
            DateTime? result = null;
            
            // First pass: check the next 7 days
            for (int i = 0; i < 7; i++) 
            {
                var dayOfWeek = date.DayOfWeek;
                if (daysOfWeek.Contains(dayOfWeek))
                {
                    // For today, check if the time is in the future
                    if (date == referenceDate.Date)
                    {
                        if (timeOfDay > referenceDate.TimeOfDay)
                        {
                            // Today's occurrence is still in the future
                            result = date.Add(timeOfDay);
                            break;
                        }
                    }
                    else
                    {
                        // Future date, so we can use it
                        result = date.Add(timeOfDay);
                        break;
                    }
                }
                
                date = date.AddDays(1);
            }
            
            // If we didn't find a date, use the first configured day of next week
            if (result == null)
            {
                date = referenceDate.Date.AddDays(1);
                while (!daysOfWeek.Contains(date.DayOfWeek))
                {
                    date = date.AddDays(1);
                }
                result = date.Add(timeOfDay);
            }
            
            // Final check: make sure we respect end date
            if (workout.EndDate.HasValue && result.Value.Date > workout.EndDate.Value.Date)
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
            
            // Find the next occurrence by checking days within a two-week period
            var date = referenceDate.Date;
            DateTime? result = null;
            
            // Look ahead for the next two weeks (14 days)
            for (int i = 0; i < 14; i++)
            {
                var dayOfWeek = date.DayOfWeek;
                if (daysOfWeek.Contains(dayOfWeek))
                {
                    // Calculate weeks since the start date to determine if this is a bi-weekly occurrence
                    var weeksSinceStart = (int)Math.Round((date - startDate.Date).TotalDays / 7.0);
                    
                    // Check if this is a bi-weekly occurrence (every two weeks)
                    if (weeksSinceStart % 2 == 0)
                    {
                        // For today, we need to check if the time is still in the future
                        if (date == referenceDate.Date)
                        {
                            if (timeOfDay > referenceDate.TimeOfDay)
                            {
                                // Today's occurrence is still in the future
                                result = date.Add(timeOfDay);
                                break;
                            }
                        }
                        else
                        {
                            // Future date, so we can use it
                            result = date.Add(timeOfDay);
                            break;
                        }
                    }
                }
                
                date = date.AddDays(1);
            }
            
            // If no date found in the next two weeks, calculate the next bi-weekly occurrence
            if (result == null)
            {
                // Find the first day of the next bi-weekly cycle
                var currentWeeksSinceStart = (int)Math.Floor((referenceDate.Date - startDate.Date).TotalDays / 7.0);
                // Ensure we get the next even-number of weeks from the start date
                var weeksToAdd = currentWeeksSinceStart % 2 == 0 ? 2 : 1;
                var nextCycleStart = startDate.Date.AddDays((currentWeeksSinceStart + weeksToAdd) * 7);
                
                // Find the first matching day of week in the next cycle
                date = nextCycleStart;
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
            if (result.HasValue && workout.EndDate.HasValue && result.Value.Date > workout.EndDate.Value.Date)
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
            // Get the day of month for the workout
            int dayOfMonth = workout.RecurrenceDayOfMonth ?? workout.StartDate.Day;
            
            // Start with the current month's first day
            var currentMonth = new DateTime(referenceDate.Year, referenceDate.Month, 1);
            DateTime result;
            
            // Check if we can use the current month (day hasn't passed yet)
            if (referenceDate.Day < dayOfMonth || (referenceDate.Day == dayOfMonth && referenceDate.TimeOfDay < timeOfDay))
            {
                // Day hasn't occurred yet this month
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
                    // Handle special cases like February 29th in non-leap years
                    var daysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                    int actualDay = Math.Min(dayOfMonth, daysInMonth);
                    result = new DateTime(nextMonth.Year, nextMonth.Month, actualDay,
                        timeOfDay.Hours, timeOfDay.Minutes, timeOfDay.Seconds);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Handle any remaining edge cases by taking the last day of the month
                    var lastDayOfMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
                    result = new DateTime(nextMonth.Year, nextMonth.Month, lastDayOfMonth,
                        timeOfDay.Hours, timeOfDay.Minutes, timeOfDay.Seconds);
                    _logger.LogWarning("Monthly workout {Id} requested invalid day {RequestedDay}, using last day of month ({ActualDay})", 
                        workout.WorkoutScheduleId, dayOfMonth, lastDayOfMonth);
                }
            }
            
            // Final check: make sure we respect end date
            if (workout.EndDate.HasValue && result.Date > workout.EndDate.Value.Date)
            {
                _logger.LogInformation("Monthly workout {Id} next occurrence would be {Date}, but that's after end date {EndDate}", 
                    workout.WorkoutScheduleId, result, workout.EndDate.Value);
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
        private async Task<WorkoutTrackerWeb.Models.Session> ConvertScheduledWorkoutToSessionAsync(WorkoutSchedule workout)
        {
            // Determine which template to use (direct template or via assignment)
            var template = workout.Template ?? workout.TemplateAssignment?.WorkoutTemplate;
            
            if (template == null)
            {
                _logger.LogError("Cannot convert workout {Id} to session: no template found", workout.WorkoutScheduleId);
                return null;
            }
            
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Step 1: Create the Session (legacy model)
                var session = new WorkoutTrackerWeb.Models.Session
                {
                    Name = workout.Name,
                    datetime = workout.ScheduledDateTime.Value,
                    StartDateTime = workout.ScheduledDateTime.Value,
                    Notes = $"Automatically created from scheduled workout: {workout.Name}",
                    UserId = workout.ClientUserId
                };
                
                _context.Session.Add(session);
                await _context.SaveChangesAsync();
                
                // Step 2: Create WorkoutSession (new model with proper metadata)
                var workoutSession = new WorkoutSession
                {
                    Name = workout.Name,
                    Description = workout.Description,
                    StartDateTime = workout.ScheduledDateTime.Value,
                    UserId = workout.ClientUserId,
                    WorkoutTemplateId = template.WorkoutTemplateId,
                    TemplateAssignmentId = workout.TemplateAssignmentId,
                    TemplatesUsed = template.Name,
                    IsFromCoach = workout.CoachUserId != workout.ClientUserId,
                    Status = "Scheduled"
                };

                _context.WorkoutSessions.Add(workoutSession);
                await _context.SaveChangesAsync();
                
                // Step 3: Add sets from template to Session model (for compatibility)
                var setList = new List<Set>();
                
                foreach (var templateExercise in template.TemplateExercises.OrderBy(e => e.SequenceNum))
                {
                    foreach (var templateSet in templateExercise.TemplateSets.OrderBy(s => s.SequenceNum))
                    {
                        var set = new Set
                        {
                            SessionId = session.SessionId,
                            ExerciseTypeId = templateExercise.ExerciseTypeId,
                            SettypeId = templateSet.SettypeId,
                            Description = templateSet.Description,
                            Notes = templateSet.Notes,
                            NumberReps = templateSet.DefaultReps,
                            Weight = templateSet.DefaultWeight,
                            SequenceNum = templateSet.SequenceNum
                        };
                        
                        setList.Add(set);
                    }
                }
                
                _context.Set.AddRange(setList);
                await _context.SaveChangesAsync();
                
                // Step 4: Create exercises and sets in the WorkoutSession model
                var sequenceNum = 0;
                foreach (var templateExercise in template.TemplateExercises.OrderBy(e => e.SequenceNum))
                {
                    // Create the exercise
                    var workoutExercise = new WorkoutExercise
                    {
                        WorkoutSessionId = workoutSession.WorkoutSessionId,
                        ExerciseTypeId = templateExercise.ExerciseTypeId,
                        EquipmentId = templateExercise.EquipmentId,
                        SequenceNum = templateExercise.SequenceNum,
                        OrderIndex = sequenceNum++,
                        Notes = templateExercise.Notes,
                        // Map appropriate rest period value - for safety, handle potential null values
                        RestPeriodSeconds = templateExercise.RestSeconds
                    };
                    
                    _context.WorkoutExercises.Add(workoutExercise);
                    await _context.SaveChangesAsync();
                    
                    // Create the sets for this exercise
                    var workoutSets = new List<WorkoutSet>();
                    var setNumber = 1;
                    
                    foreach (var templateSet in templateExercise.TemplateSets.OrderBy(s => s.SequenceNum))
                    {
                        var workoutSet = new WorkoutSet
                        {
                            WorkoutExerciseId = workoutExercise.WorkoutExerciseId,
                            SettypeId = templateSet.SettypeId,
                            SequenceNum = templateSet.SequenceNum,
                            SetNumber = setNumber++,
                            Reps = templateSet.DefaultReps,
                            Weight = templateSet.DefaultWeight,
                            Notes = templateSet.Notes,
                            // Default rest period if needed
                            RestSeconds = 60, // Using standard default value
                            // Use min/max reps from template or set reasonable defaults
                            TargetMinReps = templateExercise.MinReps,
                            TargetMaxReps = templateExercise.MaxReps,
                            IsCompleted = false,
                            Timestamp = DateTime.Now
                        };
                        
                        workoutSets.Add(workoutSet);
                    }
                    
                    _context.WorkoutSets.AddRange(workoutSets);
                    await _context.SaveChangesAsync();
                }
                
                // If this is a one-time schedule, mark it as inactive
                if (!workout.IsRecurring || workout.RecurrencePattern == "Once")
                {
                    var actualWorkout = await _context.WorkoutSchedules
                        .FirstOrDefaultAsync(w => w.WorkoutScheduleId == workout.WorkoutScheduleId);
                    
                    if (actualWorkout != null)
                    {
                        actualWorkout.IsActive = false;
                        await _context.SaveChangesAsync();
                    }
                }
                
                await transaction.CommitAsync();
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting scheduled workout {Id} to session", workout.WorkoutScheduleId);
                await transaction.RollbackAsync();
                return null;
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
    }
}