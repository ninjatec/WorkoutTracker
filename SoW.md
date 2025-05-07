# WorkoutTracker Performance and Stability Optimization

## Statement of Work

## Feature: Enhanced Workout Planning and Calendar Integration

This feature aims to provide users with robust tools for planning their workouts, visualizing their schedule, and creating structured training programs.

### Phase 1: Core Calendar and Scheduling Enhancements

1.  **Data Model Refinements & New Entities:**
    *   Review existing models (e.g., `WorkoutSession`, `WorkoutTemplate`) for suitability.
    *   Define a `PlannedWorkout` entity:
        *   `Id` (Primary Key)
        *   `UserId` (Foreign Key to User)
        *   `WorkoutTemplateId` (Foreign Key to `WorkoutTemplate`, optional, if planning a specific template)
        *   `CustomWorkoutTitle` (string, if not using a template)
        *   `ScheduledDateTime` (datetime)
        *   `DurationMinutes` (int, optional)
        *   `Notes` (string, optional)
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
        *   `Name` (string)
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

## Feature: Interactive Progress Dashboard (COMPLETED)

### Overview
Provide users with a dynamic dashboard visualizing workout metrics—volume, consistency, intensity—over time using Chart.js or D3.

1. Data Model & Persistence: ✓
   * Created `WorkoutMetric` entity to track aggregated metrics with `UserId`, `MetricType`, `Date`, and `Value` fields.
   * Updated `WorkoutTrackerWebContext` with a `DbSet<WorkoutMetric>`.
   * Added and applied EF Core migration.

2. Service Layer: ✓
   * Created `ProgressDashboardService` with:
     - `Task<IEnumerable<WorkoutMetric>> GetVolumeSeriesAsync(Guid userId, DateTime start, DateTime end)`
     - `Task<IEnumerable<WorkoutMetric>> GetIntensitySeriesAsync(Guid userId, DateTime start, DateTime end)`
     - `Task<IEnumerable<WorkoutMetric>> GetConsistencySeriesAsync(Guid userId, DateTime start, DateTime end)`
     - `Task CalculateAndStoreMissingMetricsAsync()`
   * Registered service in DI (Program.cs).

3. Razor Page & API Handler: ✓
   * Added new Razor Page `/Progress/Index.cshtml` and `ProgressModel.cs`:
     - `OnGetAsync()` renders page shell and handles date range parameters.
     - `OnGetDataAsync()` returns JSON via `JsonResult` with output caching.
   * Added navigation link in the Reporting dropdown menu.

4. UI & Chart Integration: ✓
   * Used existing Chart.js integration.
   * Implemented `<canvas>` elements for each chart type (volume, intensity, consistency).
   * Created JavaScript code to:
     - Fetch data via API handler.
     - Initialize Chart.js instances for each metric type.
     - Implemented date-range picker controls for filtering data.

5. Styling & Layout: ✓
   * Used Bootstrap 5 grid to arrange charts responsively.
   * Implemented responsive design for all screen sizes.
   * Styled charts with appropriate colors and tooltips.

6. Caching & Performance: ✓
   * Implemented output caching for the data endpoint with a 5-minute duration.
   * Added cache invalidation when metrics are recalculated.

7. Documentation Updates: ✓
   * Updated `README.md` with feature description.
   * Updated `inventory.md` with new components and models.
   * Updated this `SoW.md` to mark completion.

## Feature: Dark Mode / Theming Toggle

### Overview
Implement a user-selectable dark/light theme using Bootstrap 5 utilities and persist preference in the user profile.

1. Data Model & Persistence:
   * Extend `ApplicationUser` model in `ApplicationDbContext` to include `ThemePreference` (string or enum: "light" | "dark").
   * Create EF Core migration:
     ```bash
     dotnet ef migrations add AddThemePreferenceToUser --context ApplicationDbContext
     dotnet ef database update --context ApplicationDbContext
     ```

2. Service Layer:
   * Add or extend `UserPreferenceService` with:
     - `Task<string> GetThemePreferenceAsync(Guid userId)`
     - `Task SetThemePreferenceAsync(Guid userId, string theme)`
   * Register the service in DI (e.g., in `Program.cs`).

3. UI & Razor Layout:
   * In `Views/Shared/_Layout.cshtml`, add a theme toggle control (e.g., switch or icon button) in the navbar.
   * Render the current theme on `<body>` via a `data-theme` attribute bound to the user preference.

4. JavaScript & UI Behavior:
   * Include a small JS module to:
     - Handle toggle clicks: switch `data-theme` on `<body>` and update Bootstrap utility classes.
     - Call an MVC/Razor Page handler via AJAX to persist the new preference.
   * On page load, read the `data-theme` attribute and apply any necessary CSS classes.

5. Styling & Bootstrap Integration:
   * Define CSS custom properties in `wwwroot/css/site.css` under `[data-theme="dark"]` selector to override colors.
   * Leverage Bootstrap 5 utility classes (`.bg-dark`, `.text-light`, etc.) conditionally based on `data-theme`.

6. Caching & Performance:
   * Use existing output cache for pages; ensure theme changes invalidate cached entries when appropriate.

7. Documentation Updates:
   * Update `README.md`, `inventory.md`, and this `SoW.md` to reflect the new dark mode feature.

