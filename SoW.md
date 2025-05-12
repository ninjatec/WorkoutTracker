# WorkoutTracker Performance and Stability Optimization

## Statement of Work

## Feature: Enhanced Workout Planning and Calendar Integration

This feature aims to provide users with robust tools for planning their workouts, visualizing their schedule, and creating structured training programs.

### Phase 1: Core Calendar and Scheduling Enhancements

1.  **Data Model Refinements & New Entities:**
    > **Note:** As of 2025-05-10, the legacy `Session` model has been fully replaced by `WorkoutSession`. All new features, services, and documentation use `WorkoutSession` exclusively. The `Session` entity is retained only for backward compatibility in data exports (see code comments).
    *   Review existing models (e.g., `WorkoutSession`, `WorkoutTemplate`) for suitability.
    *   Define a `PlannedWorkout` entity:
        *   `Id` (Primary Key)
        *   `UserId` (Foreign Key to User)
        *   `WorkoutTemplateId` (Foreign Key to `WorkoutTemplate`, optional, if planning a specific template)
        *   `CustomWorkoutTitle` (string, if not using a template)
        *   `ScheduledDateTime` (datetime)
        *   `DurationMinutes` (int, optional)
        *   `Notes` (string, optional) — now present in WorkoutSessions table
        *   `IsCompleted` (bool, default false)
        *   `CompletedDateTime` (datetime, nullable)
    *   Ensure relationships are correctly configured in the relevant `DbContext`.
    *   Create EF Core migrations: `dotnet ef migrations add AddPlannedWorkoutEntity --context ApplicationDbContext` (or appropriate context).
    *   Apply migrations: `dotnet ef database update --context ApplicationDbContext`.

2.  **Backend Services for Planned Workouts:**
    *   Create `PlannedWorkoutService` (or augment existing relevant services).
    *   Implement CRUD operations for `PlannedWorkout` entities.
        *   `CreatePlannedWorkoutAsync(PlannedWorkout workout)`
        *   `GetPlannedWorkoutsForUserAsync(Guid userId, DateTime startDate, DateTime endDate)`
        *   `GetPlannedWorkoutByIdAsync(Guid plannedWorkoutId)`
        *   `UpdatePlannedWorkoutAsync(PlannedWorkout workout)`
        *   `DeletePlannedWorkoutAsync(Guid plannedWorkoutId)`
        *   `MarkPlannedWorkoutAsCompletedAsync(Guid plannedWorkoutId, DateTime completedTime)`
    *   Ensure services use the repository pattern and appropriate `DbContext`.
    *   Implement logging for service operations.

3.  **Calendar View Razor Page:**
    *   Create a new Razor Page (e.g., `/Calendar/Index.cshtml`).
    *   PageModel (`CalendarModel.cs`):
        *   Inject `PlannedWorkoutService` and `UserManager<ApplicationUser>`.
        *   OnGetAsync: Fetch planned workouts for the current user for a given month/week.
        *   Handler methods for navigating to previous/next month/week.
    *   View (`Index.cshtml`):
        *   Display a calendar (monthly view initially).
        *   Render `PlannedWorkout` entries on the calendar.
        *   Provide UI elements for adding new planned workouts (linking to a creation page/modal).
        *   Allow clicking on a planned workout to view details or edit.
    *   Utilize Bootstrap 5 for styling.
    *   Integrate with existing output caching.

4.  **Create/Edit Planned Workout Razor Page:**
    *   Create a new Razor Page (e.g., `/Calendar/ManagePlannedWorkout.cshtml`).
    *   PageModel (`ManagePlannedWorkoutModel.cs`):
        *   Inject `PlannedWorkoutService`, `WorkoutTemplateService` (if users can select templates), and `UserManager<ApplicationUser>`.
        *   OnGetAsync: Load existing `PlannedWorkout` for editing or prepare for new entry. Populate select lists for templates if applicable.
        *   OnPostAsync: Handle creation or update of `PlannedWorkout`. Validate input.
    *   View (`ManagePlannedWorkout.cshtml`):
        *   Form for `ScheduledDateTime`, `WorkoutTemplateId` (optional dropdown), `CustomWorkoutTitle`, `DurationMinutes`, `Notes`.
        *   Use Bootstrap 5 form styling.

5.  **UI Enhancements for Calendar:**
    *   Implement basic drag-and-drop functionality for rescheduling `PlannedWorkout` entries on the calendar view.
        *   This will likely involve JavaScript to capture drag events and make AJAX calls to a new handler method in `CalendarModel.cs` to update the `ScheduledDateTime` of the `PlannedWorkout`.
    *   Ensure the calendar view is responsive.

### Phase 2: iCalendar Export

1.  **iCal Generation Service:**
    *   Add a NuGet package for iCalendar generation (e.g., `Ical.Net`).
    *   Create `ICalExportService`.
    *   Method: `GenerateICalForUserAsync(Guid userId, DateTime startDate, DateTime endDate)`:
        *   Fetch `PlannedWorkout` entries for the user within the date range.
        *   Convert these entries into iCalendar event objects.
        *   Return the iCalendar data as a string or stream.

2.  **iCal Export Razor Page Endpoint:**
    *   Add a handler method to an appropriate PageModel (e.g., `CalendarModel.cs` or a dedicated `ExportModel.cs`).
    *   `OnGetExportICalAsync()`:
        *   Call `ICalExportService` to generate the iCalendar data.
        *   Return a `FileContentResult` with the `text/calendar` MIME type and a `.ics` file extension.
    *   Add a button/link on the Calendar page to trigger this export.

### Phase 3: Workout Program Builder (Basic)

1.  **Data Models for Workout Programs:**
    *   `WorkoutProgram` entity:
        *   `Id` (Primary Key)
        *   `UserId` (Foreign Key to User, creator of the program)
        *   `Name` (string) — now present in WorkoutExercises table
        *   `Description` (string, optional)
        *   `DurationWeeks` (int)
        *   `IsPublic` (bool, for potential future sharing)
    *   `ProgramWeek` entity:
        *   `Id` (Primary Key)
        *   `WorkoutProgramId` (Foreign Key to `WorkoutProgram`)
        *   `WeekNumber` (int)
    *   `ScheduledProgramItem` entity:
        *   `Id` (Primary Key)
        *   `ProgramWeekId` (Foreign Key to `ProgramWeek`)
        *   `DayOfWeek` (enum or int representing Monday-Sunday)
        *   `WorkoutTemplateId` (Foreign Key to `WorkoutTemplate`)
        *   `Notes` (string, optional)
    *   Create EF Core migrations and update the database.

2.  **Backend Services for Workout Programs:**
    *   Create `WorkoutProgramService`.
    *   Implement CRUD operations for `WorkoutProgram`, `ProgramWeek`, and `ScheduledProgramItem`.
    *   Method: `ApplyProgramToScheduleAsync(Guid programId, Guid userId, DateTime programStartDate)`:
        *   Iterate through the `WorkoutProgram` structure.
        *   For each `ScheduledProgramItem`, create a corresponding `PlannedWorkout` entry in the user's schedule, calculating the correct `ScheduledDateTime` based on the `programStartDate`, `WeekNumber`, and `DayOfWeek`.

3.  **Workout Program Management Razor Pages:**
    *   `/Programs/Index.cshtml`: List user's created workout programs.
    *   `/Programs/Create.cshtml` (and `Edit.cshtml`):
        *   Form to define `WorkoutProgram` details (Name, Description, DurationWeeks).
        *   Interface to add/remove `WorkoutTemplate`s to specific days within each week of the program (e.g., using nested loops for weeks and days, with dropdowns to select templates).
    *   PageModels to handle data operations via `WorkoutProgramService`.

4.  **"Apply Program" Functionality:**
    *   On the `/Programs/Index.cshtml` or a program details page, provide a button "Apply to My Schedule".
    *   This will trigger a handler that asks for a start date and then calls `WorkoutProgramService.ApplyProgramToScheduleAsync`.

### Phase 4: Integration and Refinements

1.  **Link Planned Workouts to Actual Sessions:**
    *   When a user starts a workout, if there's a `PlannedWorkout` for that time/day, offer to link it or start it.
    *   When a `WorkoutSession` is completed, if it was linked to a `PlannedWorkout`, update `PlannedWorkout.IsCompleted` and `PlannedWorkout.CompletedDateTime`.

2.  **Testing:**
    *   Unit tests for all new services and complex PageModel logic.
    *   Integration tests for program creation, application to schedule, and iCal export.
    *   Manual UI testing for calendar interactions and program management.

3.  **Documentation Updates:**
    *   Update `README.md` to reflect the new workout planning and calendar features.
    *   Update `inventory.md` with new entities, services, and Razor Pages.

## Feature: Dark Mode / Theming Toggle

### Status: Complete

This feature allows users to toggle between light and dark themes. The implementation includes:
- A `ThemePreference` property in the `AppUser` model.
- A `UserPreferenceService` to manage theme preferences.
- A theme toggle control in the navigation bar.
- Dynamic theme switching using JavaScript and Razor Pages.
- Dark mode styles integrated with Bootstrap 5.
- Output caching support for theme preferences.

## 2025-05-09
- Security: Updated Content Security Policy to allow https://static.cloudflareinsights.com in script-src for Cloudflare Insights compatibility.

## 2025-05-10
- Session to WorkoutSession migration complete
- All code, pages, and services now use WorkoutSession exclusively
- Obsolete MVC view removed
- SessionExport class marked as legacy only (see code comments)
- See logs/remove_obsolete_session_view_20250510.txt
- All hardcoded and environment-specific configuration removed from source code. Configuration is now managed via appsettings.json, environment variables, and user secrets.

## 2025-05-11
- Added Exercise Types and Set Types links to the Data dropdown in navigation menu for easier access
- Fixed syntax error in QuickWorkout.cshtml that was causing build failures

## 2025-05-10: UI Consistency & Output Caching
- All Razor Pages reviewed for Bootstrap 5 and output caching consistency.
- Reports/Index updated to use [OutputCache] with a 5-minute duration and policy.
- Bootstrap 5 usage and layout confirmed consistent in Reports/Index and shared layouts.

## 2025-05-11: Interactive Progress Dashboard
- Fixed dashboard display issues affecting Volume Progress, Exercise Distribution, and Personal Bests sections
- Implemented proper JavaScript initialization for dashboard charts and data loading
- Added workout frequency visualization to dashboard display
- Enhanced dashboard data loading with better error handling and diagnostics
- Dashboard now correctly uses the period selector for filtering data
- Added debug logging for dashboard data service

## 2025-05-11: UI/UX Enhancement Implementation
### Status: In Progress
- Implemented mobile-optimized bottom navigation bar for critical actions (Home, Workout, Schedule, Progress, More)
- Enhanced main navigation with improved organization, visual hierarchy and mobile adaptations
- Added loading indicator overlay for async operations to provide better user feedback
- Implemented toast notification system for non-intrusive user messages
- Created comprehensive form enhancements for improved UX across all device sizes:
  - Touch-friendly inputs and controls for mobile
  - Clear visual feedback for form validation
  - Enhanced accessibility for form elements
  - Multi-step form support for complex data entry
- Established consistent visual hierarchy with standardized typography scales and spacing
- Optimized layout components for both desktop and mobile views
- Updated README.md and inventory.md to reflect UI/UX enhancements

### 2025-05-12 - Background Job Bug Fixes
- Fixed issue with Hangfire scheduled job creating duplicate workout sessions.
- Enhanced workout session duplicate detection to correctly handle workouts that have been edited or completed.
- Improved logging in the workout scheduling service for better diagnostic information.

