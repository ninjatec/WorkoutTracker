using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WorkoutTrackerWeb.Attributes;
using WorkoutTrackerWeb.Data;
using WorkoutTrackerWeb.Models;
using WorkoutTrackerWeb.Models.Coaching;
using WorkoutTrackerWeb.Models.Identity;
using WorkoutTrackerWeb.ViewModels;
using WorkoutTrackerWeb.ViewModels.Coaching;

namespace WorkoutTrackerWeb.Areas.Coach.Pages.Templates
{
    [Area("Coach")]
    [CoachAuthorize]
    [OutputCache(PolicyName = "StaticContentWithId")]
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class DetailsModel : PageModel
    {
        private readonly WorkoutTrackerWebContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            WorkoutTrackerWebContext context,
            UserManager<AppUser> userManager,
            ILogger<DetailsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public WorkoutTemplate WorkoutTemplate { get; set; } = default!;
        
        public List<User> Clients { get; set; } = new List<User>();
        public List<TemplateAssignmentViewModel> RecentAssignments { get; set; } = new List<TemplateAssignmentViewModel>();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                _logger.LogInformation("[TemplatePermissionDebug] OnGetAsync called for template ID {TemplateId} for user {UserName}", 
                    id, User.Identity?.Name);
                
                // For JSON/API requests, prevent response caching
                if (Request.Headers["Accept"].ToString().Contains("application/json"))
                {
                    Response.Headers["Cache-Control"] = "no-store, max-age=0";
                    Response.Headers["Pragma"] = "no-cache";
                }

                var workoutTemplate = await _context.WorkoutTemplate
                    .Include(t => t.TemplateExercises)
                    .ThenInclude(e => e.ExerciseType)
                    .Include(t => t.TemplateExercises)
                    .ThenInclude(e => e.TemplateSets)
                    .ThenInclude(s => s.Settype)
                    .FirstOrDefaultAsync(t => t.WorkoutTemplateId == id);

                if (workoutTemplate == null)
                {
                    _logger.LogWarning("[TemplatePermissionDebug] Template with ID {TemplateId} was not found", id);
                    return Page(); // Return page with null template, view will handle this
                }

                _logger.LogInformation("[TemplatePermissionDebug] Template found: ID={TemplateId}, Name={TemplateName}, UserId={TemplateUserId}, IsPublic={IsPublic}", 
                    workoutTemplate.WorkoutTemplateId, workoutTemplate.Name, workoutTemplate.UserId, workoutTemplate.IsPublic);
                
                // Check if the template belongs to this coach or is public
                if (!workoutTemplate.IsPublic)
                {
                    // Get the coach user ID
                    var coachId = _userManager.GetUserId(User);
                    _logger.LogInformation("[TemplatePermissionDebug] Coach identity ID: {CoachId}", coachId ?? "null");
                    
                    if (string.IsNullOrEmpty(coachId))
                    {
                        _logger.LogWarning("[TemplatePermissionDebug] Coach identity ID not found for user {user}", User.Identity?.Name);
                        return Page(); // Return page with WorkoutTemplate set to null
                    }

                    // Get the coach user from the database
                    var coachUser = await _context.User
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.IdentityUserId == coachId);
                    
                    if (coachUser == null)
                    {
                        _logger.LogWarning("[TemplatePermissionDebug] Coach user not found in database for identity ID {coachId}", coachId);
                        return Page(); // Return page with WorkoutTemplate set to null
                    }
                    
                    _logger.LogInformation("[TemplatePermissionDebug] Coach user found: ID={CoachUserId}, Name={CoachName}, IdentityId={CoachIdentityId}", 
                        coachUser.UserId, coachUser.Name, coachUser.IdentityUserId);
                        
                    // Direct comparison of IDs for debugging
                    _logger.LogInformation("[TemplatePermissionDebug] Permission check: Template UserId: {TemplateUserId} (type: {TemplateUserIdType}), Coach UserId: {CoachUserId} (type: {CoachUserIdType}), Equal: {AreEqual}", 
                        workoutTemplate.UserId, workoutTemplate.UserId != null ? workoutTemplate.UserId.GetType().Name : "null", 
                        coachUser.UserId, coachUser.UserId.GetType().Name,
                        workoutTemplate.UserId == coachUser.UserId);
                    
                    // Check if the template belongs to this coach
                    if (workoutTemplate.UserId != coachUser.UserId)
                    {
                        _logger.LogWarning("[TemplatePermissionDebug] Template {TemplateId} does not belong to coach {CoachId} - UserId mismatch: {TemplateUserId} vs {CoachUserId}",
                            id, coachId, workoutTemplate.UserId, coachUser.UserId);
                        WorkoutTemplate = null;
                        return Page(); // Return page with WorkoutTemplate set to null
                    }
                    
                    // Explicitly log access was granted
                    _logger.LogInformation("[TemplatePermissionDebug] Permission check PASSED - Coach {CoachId} (UserId: {CoachUserId}) has permission to access template {TemplateId}", 
                        coachId, coachUser.UserId, id);
                    
                    // Load clients for the coach
                    // Get clients of this coach for the assignment dropdown
                    var relationships = await _context.CoachClientRelationships
                        .Where(r => r.CoachId == coachId && r.Status == RelationshipStatus.Active)
                        .ToListAsync();
                    
                    _logger.LogInformation("[TemplatePermissionDebug] Found {RelationshipCount} active coach-client relationships", relationships.Count);
                    
                    // Get the client IDs from the relationships
                    var clientIdentityIds = relationships.Select(r => r.ClientId).ToList();
                    
                    // Fetch the User records for these clients
                    Clients = await _context.User
                        .Where(u => clientIdentityIds.Contains(u.IdentityUserId))
                        .ToListAsync();
                    
                    _logger.LogInformation("[TemplatePermissionDebug] Found {ClientCount} clients with matching identity IDs", Clients.Count);
                    
                    // If no clients found, get some for testing
                    if (!Clients.Any())
                    {
                        _logger.LogWarning("[TemplatePermissionDebug] No clients found for coach {CoachId} - user ID: {CoachUserId}", coachId, coachUser.UserId);
                        
                        // Get a list of all users that aren't the coach
                        var otherUsers = await _context.User
                            .Where(u => u.IdentityUserId != coachId && u.IdentityUserId != null)
                            .Take(5)
                            .ToListAsync();
                            
                        if (otherUsers.Any())
                        {
                            _logger.LogInformation("[TemplatePermissionDebug] Adding {Count} test users for coach testing", otherUsers.Count);
                            Clients = otherUsers;
                        }
                    }
                    
                    _logger.LogInformation("[TemplatePermissionDebug] Found {Count} clients for coach {CoachId}", Clients.Count, coachId);
                    
                    // Get recent assignments of this template
                    var recentAssignments = await _context.TemplateAssignments
                        .Where(a => a.WorkoutTemplateId == id && a.CoachUserId == coachUser.UserId)
                        .Include(a => a.Client)
                        .OrderByDescending(a => a.AssignedDate)
                        .Take(5)
                        .ToListAsync();
                        
                    RecentAssignments = recentAssignments.Select(a => new TemplateAssignmentViewModel
                    {
                        Id = a.TemplateAssignmentId,
                        TemplateId = a.WorkoutTemplateId,
                        Name = workoutTemplate.Name,
                        ClientRelationshipId = (int)a.ClientRelationshipId, // Explicit cast from int? to int
                        Notes = $"Assigned on {a.AssignedDate.ToShortDateString()}"
                    }).ToList();
                }
                else
                {
                    _logger.LogInformation("[TemplatePermissionDebug] Template {TemplateId} is public, skipping permission check", id);
                }

                WorkoutTemplate = workoutTemplate;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TemplatePermissionDebug] Error loading template details for template {TemplateId}", id);
                // Ensure WorkoutTemplate is null so the view can handle it properly
                WorkoutTemplate = null;
                return Page();
            }
        }
        
        public async Task<IActionResult> OnPostAssign(
            int templateId, 
            int clientId, 
            string name, 
            string notes, 
            [FromForm(Name = "startDate")] string startDateStr, 
            [FromForm(Name = "endDate")] string endDateStr, 
            bool scheduleWorkouts = false,
            string recurrencePattern = "Once",
            List<string> daysOfWeek = null,
            int? dayOfMonth = null,
            string workoutTime = "17:00",
            bool sendReminder = false,
            int reminderHoursBefore = 3)
        {
            try
            {
                _logger.LogInformation("[TemplateAssignDebug] ⭐️ Starting assignment process for template {TemplateId} to client {ClientId}", 
                    templateId, clientId);
                
                _logger.LogInformation("[TemplateAssignDebug] User identity name: {UserName}, Is authenticated: {IsAuthenticated}", 
                    User.Identity?.Name, User.Identity?.IsAuthenticated);
                    
                // Dump all claims for debugging
                var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
                _logger.LogInformation("[TemplateAssignDebug] User claims: {@Claims}", claims);
                
                // Enable request debugging
                var formData = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
                _logger.LogInformation("[TemplateAssignDebug] Form data: {@FormData}", formData);
                
                _logger.LogInformation("[TemplateAssignDebug] StartDate: {StartDate}, EndDate: {EndDate}", 
                    startDateStr, endDateStr);
                
                // Parse dates from form inputs
                if (string.IsNullOrEmpty(startDateStr))
                {
                    _logger.LogWarning("[TemplateAssignDebug] ❌ Start date is null or empty");
                    ModelState.AddModelError("startDate", "Start date is required");
                    
                    // Load necessary data before returning the page
                    await ReloadPageDataForValidationError(templateId);
                    return Page();
                }
                
                if (!DateTime.TryParse(startDateStr, out DateTime startDate))
                {
                    _logger.LogWarning("[TemplateAssignDebug] ❌ Invalid start date format: {StartDateStr}", startDateStr);
                    ModelState.AddModelError("startDate", "Invalid start date format");
                    
                    await ReloadPageDataForValidationError(templateId);
                    return Page();
                }
                
                _logger.LogInformation("[TemplateAssignDebug] ✅ Date validation passed: StartDate={StartDate}", startDate);
                
                DateTime? endDate = null;
                if (!string.IsNullOrEmpty(endDateStr) && DateTime.TryParse(endDateStr, out DateTime parsedEndDate))
                {
                    endDate = parsedEndDate;
                    _logger.LogInformation("[TemplateAssignDebug] End date parsed: {EndDate}", endDate);
                }
                
                // Disable output caching for this action
                Response.Headers["Cache-Control"] = "no-store, max-age=0";
                Response.Headers["Pragma"] = "no-cache";
                
                // Get the identity user ID of the coach
                var coachIdentityId = _userManager.GetUserId(User);
                _logger.LogInformation("[TemplateAssignDebug] 🔍 Coach identity ID: {CoachIdentityId}", 
                    coachIdentityId ?? "null");
                
                if (string.IsNullOrEmpty(coachIdentityId))
                {
                    _logger.LogWarning("[TemplateAssignDebug] ❌ Coach identity ID not found for user {UserName}", 
                        User.Identity?.Name);
                    return Forbid();
                }
                
                // Get the coach user from the database
                var coachUser = await _context.User
                    .AsNoTracking() // Use AsNoTracking to avoid tracking issues
                    .FirstOrDefaultAsync(u => u.IdentityUserId == coachIdentityId);
                
                if (coachUser == null)
                {
                    _logger.LogWarning("[TemplateAssignDebug] ❌ Coach user not found in database for identity ID {CoachIdentityId}", 
                        coachIdentityId);
                    return Forbid();
                }
                
                _logger.LogInformation("[TemplateAssignDebug] ✅ Coach user found: ID={CoachUserId}, Name={CoachName}, IdentityId={IdentityId}", 
                    coachUser.UserId, coachUser.Name, coachUser.IdentityUserId);
                
                // Get the template from the database with tracking disabled
                var template = await _context.WorkoutTemplate
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId);
                    
                if (template == null)
                {
                    _logger.LogWarning("[TemplateAssignDebug] ❌ Template {TemplateId} not found", templateId);
                    return NotFound("Template not found");
                }
                
                _logger.LogInformation("[TemplateAssignDebug] ✅ Template found: ID={TemplateId}, Name={TemplateName}, UserId={TemplateUserId}, IsPublic={IsPublic}", 
                    template.WorkoutTemplateId, template.Name, template.UserId, template.IsPublic);
                
                // Check if the template belongs to this coach or is public
                var templateUserIdType = template.UserId != null ? template.UserId.GetType().Name : "null";
                var coachUserIdType = coachUser.UserId.GetType().Name;
                var areEqual = template.UserId == coachUser.UserId;
                
                _logger.LogInformation("[TemplateAssignDebug] 🔍 Template ownership check details:");
                _logger.LogInformation("[TemplateAssignDebug] - Template UserId: {TemplateUserId} (type: {TemplateUserIdType})", 
                    template.UserId, templateUserIdType);
                _logger.LogInformation("[TemplateAssignDebug] - Coach UserId: {CoachUserId} (type: {CoachUserIdType})", 
                    coachUser.UserId, coachUserIdType);
                _logger.LogInformation("[TemplateAssignDebug] - IsPublic: {IsPublic}", template.IsPublic);
                _logger.LogInformation("[TemplateAssignDebug] - Equal (template.UserId == coachUser.UserId): {AreEqual}", areEqual);
                _logger.LogInformation("[TemplateAssignDebug] - Permission check result: {HasPermission}", 
                    template.IsPublic || template.UserId == coachUser.UserId);
                    
                if (!template.IsPublic && template.UserId != coachUser.UserId)
                {
                    _logger.LogWarning("[TemplateAssignDebug] ❌ Template {TemplateId} does not belong to coach {CoachId} - UserId mismatch: {TemplateUserId} vs {CoachUserId}", 
                        templateId, coachIdentityId, template.UserId, coachUser.UserId);
                    return Forbid();
                }
                
                _logger.LogInformation("[TemplateAssignDebug] ✅ Template ownership check PASSED");
                
                // Get the client from the database
                var client = await _context.User
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == clientId);
                    
                if (client == null)
                {
                    _logger.LogWarning("[TemplateAssignDebug] ❌ Client with ID {ClientId} not found", clientId);
                    return NotFound("Client not found");
                }
                
                _logger.LogInformation("[TemplateAssignDebug] ✅ Client found: ID={ClientId}, Name={ClientName}, IdentityId={ClientIdentityId}", 
                    client.UserId, client.Name, client.IdentityUserId);

                // Find the relationship between the coach and client
                if (string.IsNullOrEmpty(client.IdentityUserId))
                {
                    _logger.LogWarning("[TemplateAssignDebug] ❌ Client has no identity ID associated with user ID {ClientId}", clientId);
                    return NotFound("Client identity not found");
                }
                
                _logger.LogInformation("[TemplateAssignDebug] 🔍 Looking for relationship between Coach ID {CoachId} and Client ID {ClientId}", 
                    coachIdentityId, client.IdentityUserId);
                
                var relationship = await _context.CoachClientRelationships
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.CoachId == coachIdentityId && r.ClientId == client.IdentityUserId);
                    
                if (relationship == null)
                {
                    _logger.LogWarning("[TemplateAssignDebug] ❌ Relationship not found between coach {CoachId} and client {ClientId}", 
                        coachIdentityId, client.IdentityUserId);
                        
                    // Let's check if there are any relationships at all for this coach
                    var allCoachRelationships = await _context.CoachClientRelationships
                        .Where(r => r.CoachId == coachIdentityId)
                        .ToListAsync();
                        
                    _logger.LogInformation("[TemplateAssignDebug] Found {Count} relationships for coach {CoachId}: {@Relationships}", 
                        allCoachRelationships.Count, 
                        coachIdentityId, 
                        allCoachRelationships.Select(r => new { r.Id, r.CoachId, r.ClientId, r.Status }));
                        
                    return NotFound("Coach-client relationship not found");
                }
                
                _logger.LogInformation("[TemplateAssignDebug] ✅ Coach-client relationship found: ID={RelationshipId}, Status={RelationshipStatus}", 
                    relationship.Id, relationship.Status);
                
                // Create the template assignment
                var assignment = new TemplateAssignment
                {
                    WorkoutTemplateId = templateId,
                    ClientUserId = clientId,
                    CoachUserId = coachUser.UserId,
                    ClientRelationshipId = relationship.Id,
                    Name = name,
                    Notes = notes,
                    AssignedDate = DateTime.UtcNow,
                    StartDate = startDate,
                    EndDate = endDate,
                    IsActive = true
                };
                
                _logger.LogInformation("[TemplateAssignDebug] 📝 Created template assignment object: {@Assignment}", 
                    new { 
                        assignment.WorkoutTemplateId, 
                        assignment.ClientUserId, 
                        assignment.CoachUserId, 
                        assignment.ClientRelationshipId,
                        assignment.Name,
                        assignment.StartDate,
                        assignment.EndDate,
                        assignment.IsActive
                    });
                
                try {
                    _context.TemplateAssignments.Add(assignment);
                    
                    _logger.LogInformation("[TemplateAssignDebug] Starting database save...");
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("[TemplateAssignDebug] ✅ Successfully saved template assignment with ID={AssignmentId}", 
                        assignment.TemplateAssignmentId);
                } catch (Exception dbEx) {
                    _logger.LogError(dbEx, "[TemplateAssignDebug] ❌ Database error while saving template assignment: {ErrorMessage}", 
                        dbEx.Message);
                    
                    // Let's try to get more detailed SQL exception info if available
                    if (dbEx.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx) {
                        _logger.LogError("[TemplateAssignDebug] SQL error details: Number={Number}, State={State}, Message={Message}", 
                            sqlEx.Number, sqlEx.State, sqlEx.Message);
                    }
                    
                    throw; // Re-throw to be caught by the outer try/catch
                }
                
                // If scheduling workouts is requested, create the workout schedules
                if (scheduleWorkouts)
                {
                    _logger.LogInformation("[TemplateAssignDebug] 📅 Scheduling workouts for template assignment {AssignmentId} with pattern {RecurrencePattern}", 
                        assignment.TemplateAssignmentId, recurrencePattern);
                    
                    try {
                        // Parse workout time
                        TimeSpan workoutTimeOfDay = TimeSpan.Parse(workoutTime ?? "17:00");
                        
                        // Create the workout schedule
                        var schedule = new WorkoutSchedule
                        {
                            TemplateAssignmentId = assignment.TemplateAssignmentId,
                            ClientUserId = clientId,
                            CoachUserId = coachUser.UserId,
                            Name = name,
                            Description = notes,
                            StartDate = startDate,
                            EndDate = endDate,
                            ScheduledDateTime = startDate.Date.Add(workoutTimeOfDay),
                            IsRecurring = recurrencePattern != "Once",
                            RecurrencePattern = recurrencePattern,
                            SendReminder = sendReminder,
                            ReminderHoursBefore = reminderHoursBefore,
                            IsActive = true
                        };
                        
                        // Set recurrence specifics
                        if ((recurrencePattern == "Weekly" || recurrencePattern == "BiWeekly") && daysOfWeek != null && daysOfWeek.Any())
                        {
                            // Parse the day of week string to the enum and then to int
                            DayOfWeek dayOfWeek = Enum.Parse<DayOfWeek>(daysOfWeek.First());
                            schedule.RecurrenceDayOfWeek = (int)dayOfWeek;
                            
                            // Store all days in the MultipleDaysOfWeek property if multiple days are selected
                            if (daysOfWeek.Count > 1)
                            {
                                schedule.MultipleDaysOfWeek = string.Join(",", daysOfWeek.Select(d => (int)Enum.Parse<DayOfWeek>(d)));
                            }
                        }
                        else if ((recurrencePattern == "Weekly" || recurrencePattern == "BiWeekly"))
                        {
                            // Default to the day of the start date
                            schedule.RecurrenceDayOfWeek = (int)startDate.DayOfWeek;
                        }
                        // For monthly recurrence, set the day of month
                        else if (recurrencePattern == "Monthly" && dayOfMonth.HasValue)
                        {
                            schedule.RecurrenceDayOfMonth = dayOfMonth.Value;
                        }
                        else if (recurrencePattern == "Monthly")
                        {
                            // Default to the day of the month from the start date
                            schedule.RecurrenceDayOfMonth = startDate.Day;
                        }
                        
                        _context.WorkoutSchedules.Add(schedule);
                        _logger.LogInformation("[TemplateAssignDebug] Adding workout schedule: {@Schedule}", 
                            new { 
                                schedule.TemplateAssignmentId,
                                schedule.ClientUserId,
                                schedule.Name,
                                schedule.StartDate,
                                schedule.ScheduledDateTime,
                                schedule.IsRecurring,
                                schedule.RecurrencePattern
                            });
                        
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("[TemplateAssignDebug] ✅ Successfully saved workout schedule with ID={ScheduleId}", 
                            schedule.WorkoutScheduleId);
                    }
                    catch (Exception scheduleEx)
                    {
                        _logger.LogError(scheduleEx, "[TemplateAssignDebug] ❌ Error creating workout schedule: {ErrorMessage}", 
                            scheduleEx.Message);
                        
                        // We don't want to fail the entire assignment if scheduling fails
                        // Just log the error and continue
                    }
                }
                
                _logger.LogInformation("[TemplateAssignDebug] ✅ Template assignment process completed successfully. Redirecting to client workout schedules.");
                return RedirectToPage("/WorkoutSchedules/Client", new { area = "Coach", clientId = clientId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TemplateAssignDebug] ❌ Error in OnPostAssign for template {TemplateId}, client {ClientId}: {ErrorMessage}", 
                    templateId, clientId, ex.Message);
                
                // Get stack trace for more detailed debugging
                _logger.LogError("[TemplateAssignDebug] Stack trace: {StackTrace}", ex.StackTrace);
                
                // Try to load template data for error display
                await ReloadPageDataForValidationError(templateId);
                
                ModelState.AddModelError("", $"An error occurred while assigning the template: {ex.Message}");
                return Page();
            }
        }

        // Direct handler specifically for the form named "Assign"
        public async Task<IActionResult> OnPost(
            int templateId, 
            int clientId,
            string name,
            string notes,
            string startDate,
            string endDate,
            bool scheduleWorkouts = false)
        {
            _logger.LogInformation("[TemplateAssignDebug] 🚨 Form submitted via OnPost with templateId={TemplateId}, clientId={ClientId}", 
                templateId, clientId);
                
            // Log all form values for debugging
            var formData = Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString());
            _logger.LogInformation("[TemplateAssignDebug] 🔍 Raw form data submitted: {@FormData}", formData);
            
            // Delegate to the specific handler
            return await OnPostAssign(
                templateId,
                clientId,
                name,
                notes,
                startDate,
                endDate,
                scheduleWorkouts);
        }

        // Alternative handler name to ensure the form is captured correctly
        public async Task<IActionResult> OnPostAssignAsync(
            int templateId, 
            int clientId, 
            string name, 
            string notes, 
            [FromForm(Name = "startDate")] string startDateStr, 
            [FromForm(Name = "endDate")] string endDateStr, 
            bool scheduleWorkouts = false,
            string recurrencePattern = "Once",
            List<string> daysOfWeek = null,
            int? dayOfMonth = null,
            string workoutTime = "17:00",
            bool sendReminder = false,
            int reminderHoursBefore = 3)
        {
            _logger.LogInformation("[TemplateAssignDebug] OnPostAssignAsync called - delegating to OnPostAssign");
            return await OnPostAssign(
                templateId, clientId, name, notes, startDateStr, endDateStr, 
                scheduleWorkouts, recurrencePattern, daysOfWeek, dayOfMonth, 
                workoutTime, sendReminder, reminderHoursBefore);
        }
        
        // Helper method to reload page data in case of validation error
        private async Task ReloadPageDataForValidationError(int templateId)
        {
            try
            {
                _logger.LogInformation("[TemplateAssignDebug] Reloading page data for validation error display");
                
                var templateObj = await _context.WorkoutTemplate
                    .Include(t => t.TemplateExercises)
                    .ThenInclude(e => e.ExerciseType)
                    .Include(t => t.TemplateExercises)
                    .ThenInclude(e => e.TemplateSets)
                    .ThenInclude(s => s.Settype)
                    .FirstOrDefaultAsync(t => t.WorkoutTemplateId == templateId);
                
                if (templateObj == null)
                {
                    _logger.LogWarning("[TemplateAssignDebug] Template {TemplateId} not found during validation error reload", templateId);
                    return;
                }
                
                WorkoutTemplate = templateObj;
                
                // Load clients data
                var coachIdFromIdentity = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(coachIdFromIdentity))
                {
                    var coachUserObj = await _context.User
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.IdentityUserId == coachIdFromIdentity);
                    
                    if (coachUserObj != null)
                    {
                        var relationships = await _context.CoachClientRelationships
                            .Where(r => r.CoachId == coachIdFromIdentity && r.Status == RelationshipStatus.Active)
                            .ToListAsync();
                        
                        var clientIdentityIds = relationships.Select(r => r.ClientId).ToList();
                        
                        Clients = await _context.User
                            .Where(u => clientIdentityIds.Contains(u.IdentityUserId))
                            .ToListAsync();
                        
                        if (!Clients.Any())
                        {
                            var otherUsers = await _context.User
                                .Where(u => u.IdentityUserId != coachIdFromIdentity && u.IdentityUserId != null)
                                .Take(5)
                                .ToListAsync();
                                
                            if (otherUsers.Any())
                            {
                                Clients = otherUsers;
                            }
                        }
                        
                        // Load recent assignments for this template
                        var recentAssignments = await _context.TemplateAssignments
                            .Where(a => a.WorkoutTemplateId == templateId && a.CoachUserId == coachUserObj.UserId)
                            .Include(a => a.Client)
                            .OrderByDescending(a => a.AssignedDate)
                            .Take(5)
                            .ToListAsync();
                            
                        RecentAssignments = recentAssignments.Select(a => new TemplateAssignmentViewModel
                        {
                            Id = a.TemplateAssignmentId,
                            TemplateId = a.WorkoutTemplateId,
                            Name = templateObj.Name,
                            ClientRelationshipId = (int)a.ClientRelationshipId,
                            Notes = $"Assigned on {a.AssignedDate.ToShortDateString()}"
                        }).ToList();
                    }
                }
                
                _logger.LogInformation("[TemplateAssignDebug] ✅ Successfully reloaded page data for validation error display");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TemplateAssignDebug] Error reloading page data for validation error display");
            }
        }
        
        // Helper method to calculate the next occurrence of a specific day of week on or after a given date
        private DateTime CalculateNextDayOfWeek(DateTime startDate, DayOfWeek targetDayOfWeek)
        {
            int daysToAdd = ((int)targetDayOfWeek - (int)startDate.DayOfWeek + 7) % 7;
            return startDate.AddDays(daysToAdd);
        }
    }
}